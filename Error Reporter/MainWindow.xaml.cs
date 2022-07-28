using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Error_Reporter
{
    using System.Diagnostics;
    using System.IO;
    using System.Threading;
    using System.Windows.Threading;

    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            this.BtnHelp.Visibility = Visibility.Hidden;

            // Start restore logic.
            this.SetState("Looking for recent saves...");
            Thread thread = new Thread(this.TryRestoreSession);
            thread.IsBackground = true;
            thread.Start();
        }

        private void TryRestoreSession(object obj)
        {
            Thread.Sleep(1000);
            try
            {
                if (File.Exists("Savegame.fr"))
                {
                    this.SetState("Savegame found, validating...");
                    string content = File.ReadAllText("Savegame.fr");
                    if (content.StartsWith("<?xml version=\"1.0\"?>"))
                    {
                        if (content.EndsWith("</SavedGameData>"))
                        {
                            this.SetState("Valid savegame found, terminating old process...");
                            this.KillProcess("GTAIV");
                            this.KillProcess("EFLC");
                            this.KillProcess("WerFault");

                            // Ugly hack to check if game crashed before during restore.
                            bool skipRecovery = false;
                            if (this.DidGameCrashDuringRestoreBefore())
                            {
                                MessageBoxResult result = MessageBox.Show("It seems the game has crashed before while attempting to restore. Would you like to cancel the crash-recovery process "
                                                + "and launch the game normally instead? ", "Failed to restore", MessageBoxButton.YesNo, MessageBoxImage.Question);

                                if (result == MessageBoxResult.Yes)
                                {
                                    skipRecovery = true;
                                }
                            }

                            if (!skipRecovery)
                            {
                                Thread.Sleep(2500);
                            }

                            this.SetState("Relaunching game...");
                            if (File.Exists("oldsession.fr"))
                            {
                                File.Delete("oldsession.fr");
                            }

                            // If user decided to skip recovery, don't copy savegame.
                            if (!skipRecovery)
                            {
                                File.Copy("Savegame.fr", "oldsession.fr");
                            }

                            Process proc = null;
                            if (File.Exists("LaunchEFLCc.exe"))
                            {
                                proc = Process.Start("LaunchEFLCc.exe");
                            }
                            else if (File.Exists("LaunchEFLC.exe"))
                            {
                                proc = Process.Start("LaunchEFLC.exe");
                            }
                            else if (File.Exists("LaunchGTAIV.exe"))
                            {
                                proc = Process.Start("LaunchGTAIV.exe");
                            }

                            if (proc != null)
                            {
                                this.SetState("Game is running. This window will terminate now...");

                                this.InvokeLogic(this.progressBar.Dispatcher,
                                    delegate
                                    {
                                        this.progressBar.IsIndeterminate = false;
                                        this.progressBar.Maximum = 5;
                                        this.progressBar.Value = 0;
                                    });

                                Timer timer = new Timer(state => this.InvokeLogic(
                                        this.progressBar.Dispatcher,
                                        delegate
                                        {
                                            this.progressBar.Value++;
                                            if (this.progressBar.Value.Equals(this.progressBar.Maximum))
                                            {
                                                Application.Current.Shutdown();
                                            }
                                        }));

                                // Run timer.
                                timer.Change(0, 1000);
                            }
                            else
                            {
                                this.SetError("Failed to launch game. You may close this window now.");
                            }
                        }
                        else
                        {
                            this.SetError("Savegame corrupted. You may close this window now.");
                            this.InvokeLogic(this.progressBar.Dispatcher, () => this.BtnHelp.Visibility = Visibility.Visible);
                        }
                    }
                    else
                    {
                        this.SetError("Savegame corrupted. You may close this window now.");
                        this.InvokeLogic(this.progressBar.Dispatcher, () => this.BtnHelp.Visibility = Visibility.Visible);
                    }
                }
                else
                {
                    this.SetError("Failed to find savegame. You may close this window now.");
                    this.InvokeLogic(this.progressBar.Dispatcher, () => this.BtnHelp.Visibility = Visibility.Visible);
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show(
                    "An exception occured while trying to restore your session: " + Environment.NewLine + exception.Message
                    + exception.StackTrace, "An exception occured", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetState(string text)
        {
            this.InvokeLogic(this.LabelState.Dispatcher, () => this.LabelState.Content = text);
        }

        private void KillProcess(string name)
        {
            Process[] processes = Process.GetProcessesByName(name);
            if (processes.Length > 0)
            {
                foreach (Process process in processes)
                {
                    process.Kill();
                }
            }
        }

        private bool DidGameCrashDuringRestoreBefore()
        {
            try
            {
                if (File.Exists("LCPDFR.log"))
                {
                    StreamReader streamReader = new StreamReader("LCPDFR.log");
                    string content = streamReader.ReadToEnd();
                    streamReader.Close();

                    if (content.Contains("------------------------"))
                    {
                        string lastSession = content.Substring(content.LastIndexOf("------------------------"));
                        if (!lastSession.Contains("[Main] Main: Initializing done"))
                        {
                            return true;
                        }
                    }
                }
            }
            catch (Exception)
            {
                
                throw;
            }

            return false;
        }

        private void SetError(string text)
        {
            this.InvokeLogic(this.LabelState.Dispatcher,
                delegate
                {
                    this.LabelState.Content = text;
                    this.LabelState.Foreground = Brushes.Red;
                });

            this.InvokeLogic(this.progressBar.Dispatcher, () => this.progressBar.Foreground = Brushes.Red);
        }

        private void InvokeLogic(Dispatcher dispatcher, Action action)
        {
            if (dispatcher.CheckAccess())
            {
                action();
            }
            else
            {
                dispatcher.BeginInvoke(action);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void BtnHelp_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
    "Your game has just crashed. Unfortunately LCPDFR wasn't able to save during gameplay and hence couldn't restore your session. "
    + "This can happen if your game crashes before LCPDFR is running for at least 30 seconds. "
    + "Please note that this is in no way related to your GTA IV savegames and no reportable issue. You can turn off this"
    + " feature using the Diagnostics Tool.", "Failed to restore", MessageBoxButton.OK);
        }
    }
}