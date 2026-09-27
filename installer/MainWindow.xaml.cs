using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;

namespace Polsimer.Installer
{
    public partial class MainWindow : Window
    {
        private readonly BrushConverter _bc = new();

        public MainWindow()
        {
            InitializeComponent();
            TxtPath.Text = GetCommandLinePath() ?? DetectSimHubPath();
            RefreshStatus();
        }

        private static string? GetCommandLinePath()
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 1; i + 1 < args.Length; i++)
            {
                if (string.Equals(args[i], "--simhub-path-b64", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        return Encoding.UTF8.GetString(Convert.FromBase64String(args[i + 1]));
                    }
                    catch (FormatException)
                    {
                        return null;
                    }
                }
            }

            return null;
        }

        private string DetectSimHubPath()
        {
            string[] registryKeys =
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

            foreach (var process in Process.GetProcessesByName("SimHubWPF"))
            {
                using (process)
                {
                    try
                    {
                        var procPath = process.MainModule?.FileName;
                        if (!string.IsNullOrEmpty(procPath))
                        {
                            var dir = Path.GetDirectoryName(procPath);
                            if (!string.IsNullOrEmpty(dir) && File.Exists(Path.Combine(dir, "SimHubWPF.exe")))
                                return dir;
                        }
                    }
                    catch
                    {
                        // The user can select the folder if process inspection is unavailable.
                    }
                }
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

            if (string.IsNullOrWhiteSpace(dir) || !File.Exists(Path.Combine(dir, "SimHubWPF.exe")))
            {
                LblStatus.Text = string.IsNullOrWhiteSpace(dir)
                    ? "Please select your SimHub directory"
                    : "SimHubWPF.exe not found in this directory";
                LblStatus.Foreground = (Brush)_bc.ConvertFromString(string.IsNullOrWhiteSpace(dir) ? "#FFA500" : "#E06C75")!;
                BtnInstall.IsEnabled = false;
                BtnUninstall.IsEnabled = false;
                return;
            }

            BtnInstall.IsEnabled = true;
            string dll = Path.Combine(dir, "Polsimer.SimHub.Plugin.dll");
            bool installed = File.Exists(dll);

            if (installed)
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

        private static string GetSettingsPath(string simHubDirectory)
        {
            return Path.Combine(simHubDirectory, @"PluginsData\Common\PolsimerF74LedsSettings.json");
        }

        private bool _simHubWasForceClosed;

        private async Task<bool> CloseTargetSimHubAsync(string simHubDirectory)
        {
            _simHubWasForceClosed = false;
            string expectedExe = Path.GetFullPath(Path.Combine(simHubDirectory, "SimHubWPF.exe"));
            var targetProcesses = new List<Process>();
            bool inspectionFailed = false;

            foreach (var process in Process.GetProcessesByName("SimHubWPF"))
            {
                try
                {
                    string? runningExe = process.MainModule?.FileName;
                    if (string.IsNullOrWhiteSpace(runningExe))
                    {
                        inspectionFailed = true;
                        process.Dispose();
                        continue;
                    }

                    if (string.Equals(Path.GetFullPath(runningExe), expectedExe, StringComparison.OrdinalIgnoreCase))
                        targetProcesses.Add(process);
                    else
                        process.Dispose();
                }
                catch
                {
                    inspectionFailed = true;
                    process.Dispose();
                }
            }

            if (inspectionFailed)
            {
                foreach (var process in targetProcesses)
                    process.Dispose();

                MessageBox.Show(this,
                    "Setup could not verify which SimHub instances are running. Close all SimHub windows and try again.",
                    "Close SimHub", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (targetProcesses.Count == 0)
                return true;



            BtnInstall.IsEnabled = false;
            BtnUninstall.IsEnabled = false;
            BtnBrowse.IsEnabled = false;
            TxtPath.IsEnabled = false;
            LblMessage.Text = "Closing the selected SimHub instance...";
            LblMessage.Foreground = (Brush)_bc.ConvertFromString("#AAAAAA")!;

            try
            {
                foreach (var process in targetProcesses)
                {
                    try
                    {
                        if (process.HasExited)
                            continue;

                        process.CloseMainWindow();
                        bool exited = await Task.Run(() => process.WaitForExit(2000));
                        if (!exited && !process.HasExited)
                        {
                            process.Kill();
                            exited = await Task.Run(() => process.WaitForExit(5000));
                            if (!exited)
                            {
                                MessageBox.Show(this,
                                    "Setup could not close the selected SimHub instance. Close it manually and try again.",
                                    "Could not close SimHub", MessageBoxButton.OK, MessageBoxImage.Error);
                                return false;
                            }

                            _simHubWasForceClosed = true;
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        // SimHub may exit while Setup is waiting for it.
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(this,
                            "Setup could not close SimHub: " + ex.Message,
                            "Could not close SimHub", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }
                    finally
                    {
                        process.Dispose();
                    }
                }

                return true;
            }
            finally
            {
                BtnBrowse.IsEnabled = true;
                TxtPath.IsEnabled = true;
                RefreshStatus();
            }
        }
        private void TxtPath_TextChanged(object sender, TextChangedEventArgs e)
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
                dialog.InitialDirectory = TxtPath.Text;

            if (dialog.ShowDialog(this) == true)
            {
                TxtPath.Text = Path.GetDirectoryName(dialog.FileName) ?? string.Empty;
                RefreshStatus();
            }
        }

        private void TryRestartAsAdministrator(string simHubDirectory)
        {
            var result = MessageBox.Show(this,
                "Windows denied access to the selected SimHub folder. Restart Setup as administrator? The folder will stay selected.",
                "Administrator permission required", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                LblMessage.Foreground = (Brush)_bc.ConvertFromString("#E06C75")!;
                LblMessage.Text = "Access was denied. Run Setup as administrator to modify this SimHub folder.";
                return;
            }

            try
            {
                string? executable;
                using (var currentProcess = Process.GetCurrentProcess())
                    executable = currentProcess.MainModule?.FileName;
                if (string.IsNullOrWhiteSpace(executable))
                    throw new InvalidOperationException("Could not locate the Setup executable.");

                string encodedPath = Convert.ToBase64String(Encoding.UTF8.GetBytes(simHubDirectory));
                var startInfo = new ProcessStartInfo
                {
                    FileName = executable,
                    Arguments = "--simhub-path-b64 " + encodedPath,
                    UseShellExecute = true,
                    Verb = "runas"
                };
                using var elevatedProcess = Process.Start(startInfo);
                if (elevatedProcess == null)
                    throw new InvalidOperationException("Windows did not start the elevated Setup process.");

                Application.Current.Shutdown();
            }
            catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
            {
                LblMessage.Foreground = (Brush)_bc.ConvertFromString("#E06C75")!;
                LblMessage.Text = "Administrator request was cancelled. No changes were made.";
            }
            catch (Exception ex)
            {
                LblMessage.Foreground = (Brush)_bc.ConvertFromString("#E06C75")!;
                LblMessage.Text = "Could not restart Setup as administrator: " + ex.Message;
            }
        }

        private async void BtnInstall_Click(object sender, RoutedEventArgs e)
        {
            string dir = TxtPath.Text.Trim();
            string sourceDll = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Polsimer.SimHub.Plugin.dll");

            if (!File.Exists(sourceDll))
            {
                MessageBox.Show(this, "Could not find 'Polsimer.SimHub.Plugin.dll' next to setup.exe!", "Missing file", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!await CloseTargetSimHubAsync(dir))
                return;

            string targetDll = Path.Combine(dir, "Polsimer.SimHub.Plugin.dll");
            string tempDll = Path.Combine(dir, ".Polsimer.SimHub.Plugin." + Guid.NewGuid().ToString("N") + ".tmp");

            try
            {
                File.Copy(sourceDll, tempDll, false);
                if (File.Exists(targetDll))
                    File.Replace(tempDll, targetDll, null);
                else
                    File.Move(tempDll, targetDll);

                RefreshStatus();
                LblMessage.Foreground = (Brush)_bc.ConvertFromString("#98C379")!;
                LblMessage.Text = (_simHubWasForceClosed ? "SimHub had to be force-closed; unsaved changes may not have been saved. " : string.Empty) + "Installation successful. Open SimHub, enable 'Polsimer F74LED' under 'Add/remove features', then import Polsimer_F74LED.ledsprofile if desired.";
            }
            catch (UnauthorizedAccessException)
            {
                TryRestartAsAdministrator(dir);
            }
            catch (Exception ex)
            {
                LblMessage.Foreground = (Brush)_bc.ConvertFromString("#E06C75")!;
                LblMessage.Text = "Installation failed: " + ex.Message;
            }
            finally
            {
                try
                {
                    if (File.Exists(tempDll)) File.Delete(tempDll);
                }
                catch
                {
                    // Keep the original plugin intact if cleanup is blocked.
                }
            }
        }

        private async void BtnUninstall_Click(object sender, RoutedEventArgs e)
        {
            string dir = TxtPath.Text.Trim();
            var result = MessageBox.Show(this,
                "Remove the Polsimer F74LED plugin and its saved settings from SimHub?",
                "Confirm removal", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
                return;

            if (!await CloseTargetSimHubAsync(dir))
                return;

            try
            {
                string targetDll = Path.Combine(dir, "Polsimer.SimHub.Plugin.dll");
                if (File.Exists(targetDll))
                    File.Delete(targetDll);

                File.Delete(GetSettingsPath(dir));

                RefreshStatus();
                LblMessage.Foreground = (Brush)_bc.ConvertFromString("#ABB2BF")!;
                LblMessage.Text = (_simHubWasForceClosed ? "SimHub had to be force-closed; unsaved changes may not have been saved. " : string.Empty) + "Plugin and saved settings uninstalled.";
            }
            catch (UnauthorizedAccessException)
            {
                TryRestartAsAdministrator(dir);
            }
            catch (Exception ex)
            {
                LblMessage.Foreground = (Brush)_bc.ConvertFromString("#E06C75")!;
                LblMessage.Text = "Uninstallation failed: " + ex.Message;
            }
        }
    }
}
