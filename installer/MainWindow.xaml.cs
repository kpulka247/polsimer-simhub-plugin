using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Versioning;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace Polsimer.Installer
{
    [SupportedOSPlatform("windows")]
    public partial class MainWindow : Window
    {
        private readonly BrushConverter _bc = new();

        public MainWindow()
        {
            InitializeComponent();
            TxtPath.Text = DetectSimHubPath();
            RefreshStatus();
        }

        private string DetectSimHubPath()
        {
            string[] registryKeys = new[]
            {
                @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\SimHub_is1",
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\SimHub_is1"
            };

            foreach (var keyPath in registryKeys)
            {
                using var key = Registry.LocalMachine.OpenSubKey(keyPath) ?? Registry.CurrentUser.OpenSubKey(keyPath);
                if (key?.GetValue("InstallLocation") is string loc && Directory.Exists(loc))
                {
                    if (File.Exists(Path.Combine(loc, "SimHubWPF.exe"))) return loc.TrimEnd('\\');
                }
            }

            using (var appPathKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\SimHubWPF.exe"))
            {
                if (appPathKey?.GetValue("Path") is string loc && Directory.Exists(loc))
                {
                    if (File.Exists(Path.Combine(loc, "SimHubWPF.exe"))) return loc.TrimEnd('\\');
                }
            }

            var runningProcesses = Process.GetProcessesByName("SimHubWPF");
            if (runningProcesses.Length > 0)
            {
                try
                {
                    var procPath = runningProcesses[0].MainModule?.FileName;
                    if (!string.IsNullOrEmpty(procPath))
                    {
                        var dir = Path.GetDirectoryName(procPath);
                        if (!string.IsNullOrEmpty(dir) && File.Exists(Path.Combine(dir, "SimHubWPF.exe")))
                            return dir;
                    }
                }
                catch { }
            }

            string[] candidateDrives = { "D", "C", "E", "F" };
            string[] candidateFolders = { @"Program Files (x86)\SimHub", "SimHub", @"Games\SimHub" };

            foreach (var drive in candidateDrives)
            {
                foreach (var folder in candidateFolders)
                {
                    var testPath = Path.Combine($"{drive}:\\", folder);
                    if (File.Exists(Path.Combine(testPath, "SimHubWPF.exe")))
                        return testPath;
                }
            }

            return string.Empty;
        }

        private void RefreshStatus()
        {
            string dir = TxtPath.Text.Trim();

            if (string.IsNullOrWhiteSpace(dir))
            {
                LblStatus.Text = "Please select your SimHub directory";
                LblStatus.Foreground = (Brush)_bc.ConvertFromString("#FFA500")!;
                BtnInstall.IsEnabled = false;
                BtnUninstall.IsEnabled = false;
                return;
            }

            if (!File.Exists(Path.Combine(dir, "SimHubWPF.exe")))
            {
                LblStatus.Text = "SimHubWPF.exe not found in this directory";
                LblStatus.Foreground = (Brush)_bc.ConvertFromString("#E06C75")!;
                BtnInstall.IsEnabled = false;
                BtnUninstall.IsEnabled = false;
                return;
            }

            BtnInstall.IsEnabled = true;
            string dll = Path.Combine(dir, "Polsimer.SimHub.Plugin.dll");

            if (File.Exists(dll))
            {
                LblStatus.Text = "Installed";
                LblStatus.Foreground = (Brush)_bc.ConvertFromString("#98C379")!;
                BtnUninstall.IsEnabled = true;
            }
            else
            {
                LblStatus.Text = "Not installed";
                LblStatus.Foreground = (Brush)_bc.ConvertFromString("#888888")!;
                BtnUninstall.IsEnabled = false;
            }
        }

        private void TxtPath_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            RefreshStatus();
        }

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select SimHubWPF.exe inside your SimHub folder",
                Filter = "SimHub Executable (SimHubWPF.exe)|SimHubWPF.exe|All Files (*.*)|*.*",
                FileName = "SimHubWPF.exe"
            };

            if (Directory.Exists(TxtPath.Text))
            {
                dialog.InitialDirectory = TxtPath.Text;
            }

            if (dialog.ShowDialog(this) == true)
            {
                TxtPath.Text = Path.GetDirectoryName(dialog.FileName) ?? string.Empty;
                RefreshStatus();
            }
        }

        private void CloseRunningSimHub()
        {
            foreach (var proc in Process.GetProcessesByName("SimHubWPF"))
            {
                try
                {
                    LblMessage.Text = "Closing active SimHub process...";
                    proc.Kill();
                    proc.WaitForExit(2000);
                }
                catch { }
            }
        }

        private void BtnInstall_Click(object sender, RoutedEventArgs e)
        {
            string dir = TxtPath.Text.Trim();
            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            string sourceDll = Path.Combine(appDir, "Polsimer.SimHub.Plugin.dll");

            if (!File.Exists(sourceDll))
            {
                MessageBox.Show(this, "Could not find 'Polsimer.SimHub.Plugin.dll' next to setup.exe!", "Missing file", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            CloseRunningSimHub();

            try
            {
                File.Copy(sourceDll, Path.Combine(dir, "Polsimer.SimHub.Plugin.dll"), true);

                RefreshStatus();
                LblMessage.Foreground = (Brush)_bc.ConvertFromString("#98C379")!;
                LblMessage.Text = "Installation successful! Open SimHub, enable 'Polsimer F74LED' under 'Add/remove features', then click 'Import profile' and select Polsimer_F74LED.ledsprofile.";
            }
            catch (Exception ex)
            {
                LblMessage.Foreground = (Brush)_bc.ConvertFromString("#E06C75")!;
                LblMessage.Text = $"Write error: {ex.Message}";
            }
        }

        private void BtnUninstall_Click(object sender, RoutedEventArgs e)
        {
            string dir = TxtPath.Text.Trim();

            var result = MessageBox.Show(this, "Are you sure you want to remove the Polsimer F74LED plugin from SimHub?", "Confirm removal", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            CloseRunningSimHub();

            try
            {
                string targetDll = Path.Combine(dir, "Polsimer.SimHub.Plugin.dll");
                string settingsFile = Path.Combine(dir, @"PluginsData\Common\PolsimerF74LedsSettings.json");

                if (File.Exists(targetDll)) File.Delete(targetDll);
                if (File.Exists(settingsFile)) File.Delete(settingsFile);

                RefreshStatus();
                LblMessage.Foreground = (Brush)_bc.ConvertFromString("#ABB2BF")!;
                LblMessage.Text = "Plugin and configuration files have been completely uninstalled.";
            }
            catch (Exception ex)
            {
                LblMessage.Foreground = (Brush)_bc.ConvertFromString("#E06C75")!;
                LblMessage.Text = $"Uninstall error: {ex.Message}";
            }
        }
    }
}