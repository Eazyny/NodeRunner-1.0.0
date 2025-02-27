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
        }

        private async void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_scriptsFolderPath))
            {
                ResultScreen.Text += "\nERROR: No folder selected.";
                return;
            }

            if (_homeProcess == null || _homeProcess.HasExited)
            {
                try
                {
                    _homeProcess = StartScript("start.bat");
                    HomeButtonText.Text = "Stop Nodes";
                }
                catch (Exception ex)
                {
                    ResultScreen.Text += "\nERROR: " + ex.Message;
                }
            }
            else
            {
                try
                {
                    await Task.Run(() =>
                    {
                        KillProcessTree(_homeProcess);
                        _homeProcess.WaitForExit();
                    });

                    _homeProcess = null;
                    HomeButtonText.Text = "Run Nodes";
                    ResultScreen.Text += "\nScript stopped.";
                }
                catch (Exception ex)
                {
                    ResultScreen.Text += "\nERROR: " + ex.Message;
                }
            }
        }

        private Process StartScript(string scriptFileName)
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

            process.OutputDataReceived += (sender, args) => Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                if (!string.IsNullOrWhiteSpace(args.Data))
                {
                    ResultScreen.Text += "\n" + args.Data;
                    this.FindControl<ScrollViewer>("ResultScreenScrollViewer").ScrollToEnd();
                }
            });

            process.ErrorDataReceived += (sender, args) => Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                if (!string.IsNullOrWhiteSpace(args.Data))
                {
                    ResultScreen.Text += "\nERROR: " + args.Data;
                    this.FindControl<ScrollViewer>("ResultScreenScrollViewer").ScrollToEnd();
                }
            });

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            return process;
        }

        private void NodesButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _pendingBalance = 0M;
                PendingBalance.Text = "Pending Balance: 0";
                RunScriptAsync("check.bat");
            }
            catch (Exception ex)
            {
                ResultScreen.Text += "\nERROR: " + ex.Message;
            }
        }

        private void BalancesButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RunScriptAsync("claim.bat");
                _pendingBalance = 0M;
                PendingBalance.Text = "Pending Balance: 0";
            }
            catch (Exception ex)
            {
                ResultScreen.Text += "\nERROR: " + ex.Message;
            }
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

                    process.OutputDataReceived += (sender, args) => Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        if (!string.IsNullOrWhiteSpace(args.Data))
                        {
                            ResultScreen.Text += "\n" + args.Data;
                            UpdatePendingBalance(args.Data);
                            this.FindControl<ScrollViewer>("ResultScreenScrollViewer").ScrollToEnd();
                        }
                    });

                    process.ErrorDataReceived += (sender, args) => Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        if (!string.IsNullOrWhiteSpace(args.Data))
                        {
                            ResultScreen.Text += "\nERROR: " + args.Data;
                            this.FindControl<ScrollViewer>("ResultScreenScrollViewer").ScrollToEnd();
                        }
                    });

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    process.WaitForExit();
                });
            }
            catch (Exception ex)
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    ResultScreen.Text += "\nERROR: " + ex.Message;
                });
            }
        }

        private void UpdatePendingBalance(string output)
        {
            try
            {
                var match = Regex.Match(output, @"Rewards to claim for NodeKey \(\d+\): (\d+)");
                if (match.Success)
                {
                    if (decimal.TryParse(match.Groups[1].Value, out decimal rawBalance))
                    {
                        // Convert from raw value (which includes 18 decimals) to whole number tokens
                        decimal correctedBalance = Math.Floor(rawBalance / 1_000_000_000_000_000_000M);

                        _pendingBalance += correctedBalance;
                        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                        {
                            PendingBalance.Text = $"Pending Balance: {_pendingBalance:F0}";
                        });
                    }
                    else
                    {
                        ResultScreen.Text += "\nERROR: Failed to parse balance.";
                    }
                }
            }
            catch (Exception ex)
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    ResultScreen.Text += $"\nERROR: {ex.Message}";
                });
            }
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

        private async void SelectScriptsFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog();
            var result = await dialog.ShowAsync(this);

            if (!string.IsNullOrWhiteSpace(result))
            {
                _scriptsFolderPath = result;
            }
        }
    }
}
