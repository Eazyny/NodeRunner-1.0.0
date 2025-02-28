using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NodeRunner
{
    public partial class MainWindow : Window
    {
        private Process? _homeProcess;
        private decimal _pendingBalance = 0M;
        private string? _scriptsFolderPath;

        // UI Elements
        private Border? _statusBar;
        private TextBlock? _statusText;
        private ScrollViewer? _resultScreenScrollViewer;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            ResultScreen = this.FindControl<TextBox>("ResultScreen");
            HomeButtonText = this.FindControl<TextBlock>("HomeButtonText");
            PendingBalance = this.FindControl<TextBox>("PendingBalance");

            _statusBar = this.FindControl<Border>("StatusBar");
            _statusText = this.FindControl<TextBlock>("StatusText");
            _resultScreenScrollViewer = this.FindControl<ScrollViewer>("ResultScreenScrollViewer");

            UpdateStatus(false);
        }

        private async void HomeButton_Click(object? sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_scriptsFolderPath))
            {
                AppendToLog("\nERROR: No folder selected.");
                return;
            }

            if (_homeProcess == null || _homeProcess.HasExited)
            {
                try
                {
                    AppendToLog("\nStarting nodes...");
                    _homeProcess = await StartScriptAsync("start.bat");
                    HomeButtonText.Text = "Stop Nodes";
                    UpdateStatus(true);
                }
                catch (Exception ex)
                {
                    AppendToLog("\nERROR: " + ex.Message);
                }
            }
            else
            {
                try
                {
                    AppendToLog("\nStopping nodes...");
                    await Task.Run(() =>
                    {
                        KillProcessTree(_homeProcess);
                        _homeProcess.WaitForExit();
                    });

                    _homeProcess = null;
                    HomeButtonText.Text = "Run Nodes";
                    UpdateStatus(false);
                    AppendToLog("\nNodes stopped.");
                }
                catch (Exception ex)
                {
                    AppendToLog("\nERROR: " + ex.Message);
                }
            }
        }

        private void NodesButton_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                _pendingBalance = 0M;
                PendingBalance.Text = "Pending Balance: 0";
                RunScriptAsync("check.bat");
            }
            catch (Exception ex)
            {
                AppendToLog("\nERROR: " + ex.Message);
            }
        }

        private void BalancesButton_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                RunScriptAsync("claim.bat");
                _pendingBalance = 0M;
                PendingBalance.Text = "Pending Balance: 0";
            }
            catch (Exception ex)
            {
                AppendToLog("\nERROR: " + ex.Message);
            }
        }

        private async Task<Process> StartScriptAsync(string scriptFileName)
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c \"{_scriptsFolderPath}\\{scriptFileName}\"",
                WorkingDirectory = _scriptsFolderPath,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            var process = new Process { StartInfo = processInfo };

            process.OutputDataReceived += (sender, args) => AppendToLog(CleanLog(args.Data));
            process.ErrorDataReceived += (sender, args) => AppendToLog("\nERROR: " + CleanLog(args.Data));

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await Task.Delay(500);
            return process;
        }

        private async void RunScriptAsync(string scriptFileName)
        {
            try
            {
                await Task.Run(() =>
                {
                    var processInfo = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/c \"{_scriptsFolderPath}\\{scriptFileName}\"",
                        WorkingDirectory = _scriptsFolderPath,
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };

                    var process = new Process { StartInfo = processInfo };

                    process.OutputDataReceived += (sender, args) =>
                    {
                        if (!string.IsNullOrWhiteSpace(args.Data))
                        {
                            AppendToLog(CleanLog(args.Data));
                            UpdatePendingBalance(args.Data);
                        }
                    };

                    process.Start();
                    process.BeginOutputReadLine();
                    process.WaitForExit();
                });
            }
            catch (Exception ex)
            {
                AppendToLog("\nERROR: " + ex.Message);
            }
        }

        private void AppendToLog(string? message)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    ResultScreen.Text += "\n" + message;

                    // Scroll to bottom
                    _resultScreenScrollViewer?.ScrollToEnd();
                });
            }
        }

        private string CleanLog(string? logMessage)
        {
            if (string.IsNullOrWhiteSpace(logMessage))
                return string.Empty;

            // Remove milliseconds from timestamps
            return Regex.Replace(logMessage, @"\[\d{2}:\d{2}:\d{2}\.\d{3}\]", match =>
            {
                return match.Value.Substring(0, match.Value.Length - 4) + "]"; // Trim the last 4 characters (".123")
            });
        }

        private void UpdatePendingBalance(string output)
        {
            var match = Regex.Match(output, @"Rewards to claim for NodeKey \(\d+\): (\d+)");
            if (match.Success)
            {
                if (decimal.TryParse(match.Groups[1].Value, out decimal rawBalance))
                {
                    decimal correctedBalance = Math.Floor(rawBalance / 1_000_000_000_000_000_000M);
                    _pendingBalance += correctedBalance;
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        PendingBalance.Text = $"Pending Balance: {_pendingBalance:F0}";
                    });
                }
            }
        }

        private void UpdateStatus(bool nodesRunning)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                if (_statusBar != null && _statusText != null)
                {
                    _statusBar.Background = nodesRunning ? Avalonia.Media.Brushes.Green : Avalonia.Media.Brushes.Red;
                    _statusText.Text = nodesRunning ? "Nodes Running" : "Nodes Stopped";
                }
            });
        }

        private void KillProcessTree(Process process)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "taskkill",
                Arguments = $"/T /F /PID {process.Id}",
                CreateNoWindow = true,
                UseShellExecute = false
            };

            using (var killProcess = new Process { StartInfo = startInfo })
            {
                killProcess.Start();
                killProcess.WaitForExit();
            }
        }

        private async void SelectScriptsFolder_Click(object? sender, RoutedEventArgs e)
        {
            var storageProvider = this.StorageProvider;
            if (storageProvider != null)
            {
                var result = await storageProvider.OpenFolderPickerAsync(new Avalonia.Platform.Storage.FolderPickerOpenOptions());
                if (result.Count > 0)
                {
                    _scriptsFolderPath = result[0].Path.LocalPath;
                }
            }
        }
    }
}
