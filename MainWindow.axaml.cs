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
        private readonly ScriptService _scriptService;
        private string? _scriptsFolderPath;

        // UI Elements
        private Border? _statusBar;
        private TextBlock? _statusText;
        private ScrollViewer? _resultScreenScrollViewer;

        // Additional fields for wallet and node info
        private string _walletLast4 = "";
        private int _nodeCount = 0;
        private decimal _currentBalance = 0;

        public MainWindow()
        {
            InitializeComponent();
            // Pass the UpdateWalletInfo delegate to ScriptService.
            _scriptService = new ScriptService(AppendToLog, UpdatePendingBalance, UpdateStatus, RestartHomeProcessAsync, UpdateWalletInfo);
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

            try
            {
                if (_scriptService.IsHomeProcessRunning)
                {
                    AppendToLog("\nStopping nodes...");
                    await _scriptService.StopHomeProcessAsync();
                    HomeButtonText.Text = "Run Nodes";
                    AppendToLog("\nNodes stopped.");
                }
                else
                {
                    AppendToLog("\nStarting nodes...");
                    // Use the long-running script method for start.bat.
                    await _scriptService.StartHomeProcessAsync(_scriptsFolderPath, "start.bat");
                    HomeButtonText.Text = "Stop Nodes";
                }
            }
            catch (Exception ex)
            {
                AppendToLog("\nERROR: " + ex.Message);
                HomeButtonText.Text = _scriptService.IsHomeProcessRunning ? "Stop Nodes" : "Run Nodes";
            }
        }

        private async void NodesButton_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                _scriptService.ResetPendingBalance();
                _currentBalance = 0;
                PendingBalance.Text = $"Pending Balance: 0\nWallet: {_walletLast4}\nNodes: {_nodeCount}";
                AppendToLog("Checking Balance...");
                await _scriptService.RunScriptAsync(_scriptsFolderPath, "check.bat");
            }
            catch (Exception ex)
            {
                AppendToLog("\nERROR: " + ex.Message);
            }
        }

        private async void BalancesButton_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                _scriptService.ResetPendingBalance();
                await _scriptService.RunScriptAsync(_scriptsFolderPath, "claim.bat");
                _currentBalance = 0;
                PendingBalance.Text = $"Pending Balance: 0\nWallet: {_walletLast4}\nNodes: {_nodeCount}";
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
                    _resultScreenScrollViewer?.ScrollToEnd();
                });
            }
        }

        // Update the pending balance (for non-sensitive log lines)
        private void UpdatePendingBalance(decimal balance)
        {
            _currentBalance = balance;
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                PendingBalance.Text = $"Pending Balance: {_currentBalance:F0}\nWallet: {_walletLast4}\nNodes: {_nodeCount}";
            });
        }

        // Update wallet and node info when a guardian log line is detected
        private void UpdateWalletInfo(string walletLast4, int nodeCount)
        {
            _walletLast4 = walletLast4;
            _nodeCount = nodeCount;
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                PendingBalance.Text = $"Pending Balance: {_currentBalance:F0}\nWallet: {_walletLast4}\nNodes: {_nodeCount}";
            });
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

        private async Task RestartHomeProcessAsync()
        {
            if (!string.IsNullOrWhiteSpace(_scriptsFolderPath))
            {
                await _scriptService.StopHomeProcessAsync();
                await _scriptService.StartHomeProcessAsync(_scriptsFolderPath, "start.bat");
                HomeButtonText.Text = "Stop Nodes";
            }
        }

        private class ScriptService
        {
            private Process? _homeProcess;
            private decimal _pendingBalance = 0M;
            private readonly Action<string?> _logAction;
            private readonly Action<decimal> _updateBalanceAction;
            private readonly Action<bool> _updateStatusAction;
            private readonly Func<Task> _restartHomeProcessAsync;
            private readonly Action<string, int> _updateWalletInfoAction;

            public bool IsHomeProcessRunning => _homeProcess != null && !_homeProcess.HasExited;

            public ScriptService(
                Action<string?> logAction,
                Action<decimal> updateBalanceAction,
                Action<bool> updateStatusAction,
                Func<Task> restartHomeProcessAsync,
                Action<string, int> updateWalletInfoAction)
            {
                _logAction = logAction;
                _updateBalanceAction = updateBalanceAction;
                _updateStatusAction = updateStatusAction;
                _restartHomeProcessAsync = restartHomeProcessAsync;
                _updateWalletInfoAction = updateWalletInfoAction;
            }

            public async Task StartHomeProcessAsync(string scriptsFolderPath, string scriptFileName)
            {
                try
                {
                    // Use the long-running version for start.bat.
                    _homeProcess = await StartLongRunningScriptAsync(scriptsFolderPath, scriptFileName);
                    _updateStatusAction(true);
                }
                catch (Exception ex)
                {
                    _logAction("\nERROR: " + ex.Message);
                    _updateStatusAction(false);
                    throw;
                }
            }

            public async Task StopHomeProcessAsync()
            {
                try
                {
                    if (_homeProcess != null)
                    {
                        await Task.Run(() =>
                        {
                            KillProcessTree(_homeProcess);
                            _homeProcess.WaitForExit();
                        });
                        _homeProcess = null;
                        _updateStatusAction(false);
                    }
                }
                catch (Exception ex)
                {
                    _logAction("\nERROR: " + ex.Message);
                    throw;
                }
            }

            public async Task RunScriptAsync(string? scriptsFolderPath, string scriptFileName)
            {
                if (scriptsFolderPath == null) throw new ArgumentNullException(nameof(scriptsFolderPath));

                try
                {
                    await Task.Run(() =>
                    {
                        var processInfo = new ProcessStartInfo
                        {
                            FileName = "cmd.exe",
                            Arguments = $"/c \"{scriptsFolderPath}\\{scriptFileName}\"",
                            WorkingDirectory = scriptsFolderPath,
                            CreateNoWindow = true,
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true
                        };

                        using var process = new Process { StartInfo = processInfo };

                        process.OutputDataReceived += (sender, args) =>
                        {
                            if (!string.IsNullOrWhiteSpace(args.Data))
                            {
                                var cleanLog = CleanLog(args.Data);
                                // For one-shot scripts, check for guardian info (delegated or owned node keys).
                                if (cleanLog.Contains("Running guardian for owner") &&
                                    (cleanLog.Contains("delegated node keys") || cleanLog.Contains("owned node keys")))
                                {
                                    var match = Regex.Match(cleanLog, @"owner \((0x[a-fA-F0-9]+)\) with (\d+) (?:delegated|owned) node keys");
                                    if (match.Success)
                                    {
                                        var wallet = match.Groups[1].Value;
                                        var last4 = wallet.Length >= 4 ? wallet.Substring(wallet.Length - 4) : wallet;
                                        int nodeCount = int.Parse(match.Groups[2].Value);
                                        _updateWalletInfoAction(last4, nodeCount);
                                        return;
                                    }
                                }
                                _logAction(cleanLog);
                                UpdatePendingBalance(cleanLog);

                                if (IsCriticalError(cleanLog))
                                {
                                    _logAction("\nERROR: Critical error detected. Attempting to restart nodes...");
                                    _restartHomeProcessAsync().Wait();
                                }
                            }
                        };

                        process.ErrorDataReceived += (sender, args) =>
                        {
                            if (!string.IsNullOrWhiteSpace(args.Data))
                            {
                                _logAction("\nERROR: " + CleanLog(args.Data));
                            }
                        };

                        process.Start();
                        process.BeginOutputReadLine();
                        process.BeginErrorReadLine();
                        process.WaitForExit();
                    });
                }
                catch (Exception ex)
                {
                    _logAction("\nERROR: " + ex.Message);
                    throw;
                }
            }

            public void ResetPendingBalance()
            {
                _pendingBalance = 0M;
            }

            /// <summary>
            /// Starts a long-running script (e.g. start.bat) with output filtering.
            /// Does not wait for the process to exit.
            /// </summary>
            private async Task<Process> StartLongRunningScriptAsync(string scriptsFolderPath, string scriptFileName)
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c \"{scriptsFolderPath}\\{scriptFileName}\"",
                    WorkingDirectory = scriptsFolderPath,
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
                        var cleanLog = CleanLog(args.Data);
                        // Filter out sensitive guardian info.
                        if (cleanLog.Contains("Running guardian for owner") &&
                           (cleanLog.Contains("delegated node keys") || cleanLog.Contains("owned node keys")))
                        {
                            var match = Regex.Match(cleanLog, @"owner \((0x[a-fA-F0-9]+)\) with (\d+) (?:delegated|owned) node keys");
                            if (match.Success)
                            {
                                var wallet = match.Groups[1].Value;
                                var last4 = wallet.Length >= 4 ? wallet.Substring(wallet.Length - 4) : wallet;
                                int nodeCount = int.Parse(match.Groups[2].Value);
                                _updateWalletInfoAction(last4, nodeCount);
                                // Skip logging this sensitive line.
                                return;
                            }
                        }
                        // When the sleep line is encountered, trigger auto-check.
                        if (cleanLog.Contains("Sleeping for") && cleanLog.Contains("before running guardian again"))
                        {
                            // Reset balance so we don't sum to the previous total.
                            ResetPendingBalance();
                            // Trigger "Check Balance" by running check.bat.
                            Task.Run(async () =>
                            {
                                await RunScriptAsync(scriptsFolderPath, "check.bat");
                            });
                        }
                        _logAction(cleanLog);
                    }
                };

                process.ErrorDataReceived += (sender, args) =>
                {
                    if (!string.IsNullOrWhiteSpace(args.Data))
                    {
                        _logAction("\nERROR: " + CleanLog(args.Data));
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                await Task.Delay(500);
                return process;
            }

            private async Task<Process> StartScriptAsync(string scriptsFolderPath, string scriptFileName)
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c \"{scriptsFolderPath}\\{scriptFileName}\"",
                    WorkingDirectory = scriptsFolderPath,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                var process = new Process { StartInfo = processInfo };

                process.OutputDataReceived += (sender, args) => _logAction(CleanLog(args.Data));
                process.ErrorDataReceived += (sender, args) => _logAction("\nERROR: " + CleanLog(args.Data));

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                await Task.Delay(500);
                return process;
            }

            // Parses balance output and updates the pending balance.
            private void UpdatePendingBalance(string output)
            {
                var match = Regex.Match(output, @"Rewards to claim for NodeKey \(\d+\): (\d+)");
                if (match.Success && decimal.TryParse(match.Groups[1].Value, out var rawBalance))
                {
                    var correctedBalance = Math.Floor(rawBalance / 1_000_000_000_000_000_000M);
                    _pendingBalance += correctedBalance;
                    _updateBalanceAction(_pendingBalance);
                }
            }

            private static bool IsCriticalError(string logMessage)
            {
                return logMessage.Contains("unexpected call exception") ||
                       logMessage.Contains("Transaction reverted without a reason string") ||
                       logMessage.Contains("node:internal/process/task_queues");
            }

            private static string CleanLog(string? logMessage)
            {
                if (string.IsNullOrWhiteSpace(logMessage))
                    return string.Empty;

                // Omit any lines that start with "C:\"
                if (Regex.IsMatch(logMessage, @"^C:\\")) 
                    return string.Empty;

                // Remove milliseconds from timestamps (e.g., convert "[22:09:02.123]" to "[22:09:02.]")
                logMessage = Regex.Replace(logMessage, @"\[\d{2}:\d{2}:\d{2}\.\d{3}\]", match =>
                {
                    return match.Value.Substring(0, match.Value.Length - 4) + "]";
                });

                // Remove ANSI color codes (e.g., \e[32mINFO\e[39m)
                return Regex.Replace(logMessage, @"\e\[[0-9;]*m", string.Empty);
            }

            private static void KillProcessTree(Process process)
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "taskkill",
                    Arguments = $"/T /F /PID {process.Id}",
                    CreateNoWindow = true,
                    UseShellExecute = false
                };

                using var killProcess = new Process { StartInfo = startInfo };
                killProcess.Start();
                killProcess.WaitForExit();
            }
        }
    }
}
