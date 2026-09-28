using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using GameReaderCommon;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SimHub.Plugins;
using SimHub.Plugins.OutputPlugins.GraphicalDash.LedModules;

namespace Polsimer.SimHub.Plugin
{
    [PluginDescription("Native SimHub LED Manager driver for Polsimer F74LED steering wheel")]
    [PluginAuthor("kpulka247")]
    [PluginName("Polsimer F74LED")]
    public class PolsimerF74LedPlugin : IPlugin, IDataPlugin, IWPFSettingsV2
    {
        public PluginManager PluginManager { get; set; }

        public string LeftMenuTitle => "Polsimer F74LED";
        public ImageSource PictureIcon => null;

        private LedModuleSettings<PolsimerLedsManager> _ledSettings;
        private string _storagePath;
        private DateTime _lastAutosave = DateTime.MinValue;
        private bool _isDirty = false;

        private DispatcherTimer _hardwarePollTimer;
        private bool _wasConnected = false;
        private int _connectionId = 0;

        public void Init(PluginManager pluginManager)
        {
            pluginManager.AddProperty("Connected", GetType(), false);
            pluginManager.AddProperty("ConnectionId", GetType(), 0);

            _storagePath = pluginManager.GetCommonStoragePath("PolsimerF74LedsSettings.json");

            _ledSettings = new LedModuleSettings<PolsimerLedsManager>(
                "-",
                "Polsimer F74LED",
                12
            );

            LoadSettings();

            if (_ledSettings.GlobalBrightnessPreset != null)
            {
                _ledSettings.GlobalBrightnessPreset.PropertyChanged += (s, e) =>
                {
                    _isDirty = true;
                };
            }

            _ledSettings.IsEnabled = true;

            // Monitor connection status and update ConnectionId on reconnect
            _hardwarePollTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromMilliseconds(50)
            };
            _hardwarePollTimer.Tick += (sender, args) =>
            {
                bool isConnected = _ledSettings?.IsConnected ?? false;
                if (isConnected)
                {
                    if (!_wasConnected)
                    {
                        _wasConnected = true;
                        _connectionId++;
                        pluginManager.SetPropertyValue("ConnectionId", GetType(), _connectionId);
                    }
                    pluginManager.SetPropertyValue("Connected", GetType(), true);
                }
                else
                {
                    _wasConnected = false;
                    pluginManager.SetPropertyValue("Connected", GetType(), false);
                }
            };
            _hardwarePollTimer.Start();
        }

        public void DataUpdate(PluginManager pluginManager, ref GameData data)
        {
            if (_ledSettings != null)
            {
                _ledSettings.IsEnabled = true;
                _ledSettings.Display();

                if (_isDirty && (DateTime.Now - _lastAutosave).TotalSeconds >= 3)
                {
                    _lastAutosave = DateTime.Now;
                    _isDirty = false;
                    SavePluginData();
                }
            }
        }

        public Control GetWPFSettingsControl(PluginManager pluginManager)
        {
            return _ledSettings?.EditControl;
        }

        public void End(PluginManager pluginManager)
        {
            try
            {
                _hardwarePollTimer?.Stop();
                SavePluginData();
                _ledSettings?.FinalizeModule();
            }
            catch { }
        }

        private void LoadSettings()
        {
            if (File.Exists(_storagePath))
            {
                try
                {
                    string json = File.ReadAllText(_storagePath);
                    var container = JObject.Parse(json);

                    if (container["Settings"] is JObject settingsObj)
                    {
                        var dict = settingsObj.ToObject<Dictionary<string, JToken>>();
                        if (dict != null)
                        {
                            _ledSettings.SetSettings(dict, false);
                        }
                    }

                    if (container["GlobalBrightnessPreset"] is JObject presetObj)
                    {
                        JsonConvert.PopulateObject(presetObj.ToString(), _ledSettings.GlobalBrightnessPreset);
                    }
                    else if (container["Brightness"] != null)
                    {
                        _ledSettings.GlobalBrightnessPreset.Brightness = (double)container["Brightness"];
                    }
                    return;
                }
                catch
                {
                    // Fallback to defaults
                }
            }

            _ledSettings.LoadDefaults();
            SavePluginData();
        }

        private void SavePluginData()
        {
            if (_ledSettings == null || string.IsNullOrEmpty(_storagePath)) return;

            try
            {
                var payload = new JObject
                {
                    ["GlobalBrightnessPreset"] = JToken.FromObject(_ledSettings.GlobalBrightnessPreset),
                    ["Settings"] = JToken.FromObject(_ledSettings.GetSettings(false, false))
                };

                File.WriteAllText(_storagePath, payload.ToString(Formatting.Indented));
            }
            catch { }
        }
    }
}