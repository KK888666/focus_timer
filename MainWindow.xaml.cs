using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;
using Forms = System.Windows.Forms;
using WpfMessageBox = System.Windows.MessageBox;
using WpfApplication = System.Windows.Application;

namespace GlassTodo
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer timer;
        private DateTime endTime;
        private bool isFocusPhase = true; 
        private Forms.NotifyIcon trayIcon;
        private readonly string settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FocusTimer", "settings.json");
        private AppSettings settings = new AppSettings();
        private bool isInitialized = false;

        public MainWindow()
        {
            InitializeComponent();
            InitializeTray();
            LoadSettings();
            DetermineStartupPanel();

            this.StateChanged += MainWindow_StateChanged;
            this.Closing += MainWindow_Closing;
        }

        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                this.Hide();
                UpdateTrayText();
            }
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            trayIcon?.Dispose();
        }

        private void InitializeTray()
        {
            trayIcon = new Forms.NotifyIcon
            {
                Icon = new System.Drawing.Icon("icon.ico"),
                Text = "专注锁屏 - 就绪",
                Visible = true
            };

            var menu = new Forms.ContextMenuStrip();
            menu.Items.Add("显示窗口", null, (s, e) => ShowWindow());
            menu.Items.Add(new Forms.ToolStripSeparator());
            menu.Items.Add("退出程序", null, (s, e) => WpfApplication.Current.Shutdown());
            trayIcon.ContextMenuStrip = menu;
            trayIcon.MouseDoubleClick += (s, e) => { if (e.Button == Forms.MouseButtons.Left) ShowWindow(); };
        }

        private void ShowWindow()
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
        }

        private void LoadSettings()
        {
            try
            {
                if (File.Exists(settingsPath))
                {
                    var json = File.ReadAllText(settingsPath);
                    var loaded = JsonSerializer.Deserialize<AppSettings>(json);
                    if (loaded != null) 
                    {
                        settings = loaded;
                        isInitialized = true;
                    }
                }
            }
            catch { settings = new AppSettings(); }
        }

        private void DetermineStartupPanel()
        {
            if (!isInitialized)
            {
                SwitchPanel("SetupPanel");
            }
            else
            {
                UpdateSummary();
                SwitchPanel("MainPanel");
            }
        }

        private void SwitchPanel(string panelName)
        {
            SetupPanel.Visibility = Visibility.Collapsed;
            MainPanel.Visibility = Visibility.Collapsed;
            SettingsPanel.Visibility = Visibility.Collapsed;
            StatusPanel.Visibility = Visibility.Collapsed;

            switch (panelName)
            {
                case "SetupPanel": 
                    SetupPanel.Visibility = Visibility.Visible; 
                    break;
                case "MainPanel": 
                    MainPanel.Visibility = Visibility.Visible; 
                    break;
                case "SettingsPanel":
                    txtEditFocus.Text = settings.FocusMinutes.ToString();
                    txtEditRest.Text = settings.RestMinutes.ToString();
                    SettingsPanel.Visibility = Visibility.Visible;
                    break;
                case "StatusPanel": 
                    StatusPanel.Visibility = Visibility.Visible; 
                    break;
            }
        }

        private void UpdateSummary()
        {
            txtConfigSummary.Text = $"专注 {settings.FocusMinutes} 分钟 | 休息 {settings.RestMinutes} 分钟";
        }

        private bool SaveSettings()
        {
            int newFocus = settings.FocusMinutes;
            int newRest = settings.RestMinutes;

            if (SetupPanel.Visibility == Visibility.Visible)
            {
                if (int.TryParse(txtSetupFocus.Text, out int f) && f > 0) newFocus = f;
                if (int.TryParse(txtSetupRest.Text, out int r) && r > 0) newRest = r;
            }
            else if (SettingsPanel.Visibility == Visibility.Visible)
            {
                if (int.TryParse(txtEditFocus.Text, out int f) && f > 0) newFocus = f;
                if (int.TryParse(txtEditRest.Text, out int r) && r > 0) newRest = r;
            }

            settings.FocusMinutes = newFocus;
            settings.RestMinutes = newRest;

            Directory.CreateDirectory(Path.GetDirectoryName(settingsPath));
            File.WriteAllText(settingsPath, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
            
            isInitialized = true;
            UpdateSummary();
            return true;
        }

        private void BtnFinishSetup_Click(object sender, RoutedEventArgs e)
        {
            if (SaveSettings()) SwitchPanel("MainPanel");
        }

        private void BtnQuickStart_Click(object sender, RoutedEventArgs e)
        {
            isFocusPhase = true;
            StartPhase(settings.FocusMinutes, "专注中...");
        }

        private void BtnOpenSettings_Click(object sender, RoutedEventArgs e) => SwitchPanel("SettingsPanel");

        private void BtnSaveAndBack_Click(object sender, RoutedEventArgs e)
        {
            if (SaveSettings())
            {
                txtSaveHint.Visibility = Visibility.Visible;
                _ = Task.Delay(1500).ContinueWith(_ => Dispatcher.Invoke(() => txtSaveHint.Visibility = Visibility.Collapsed));
                _ = Task.Delay(2000).ContinueWith(_ => Dispatcher.Invoke(() => SwitchPanel("MainPanel")));
            }
        }

        private void BtnCancelSettings_Click(object sender, RoutedEventArgs e) => SwitchPanel("MainPanel");

        private void BtnAbout_Click(object sender, RoutedEventArgs e)
        {
            var aboutWin = new AboutWindow { Owner = this };
            aboutWin.ShowDialog();
        }

        private void StartPhase(int minutes, string title)
        {
            SwitchPanel("StatusPanel");
            txtStatusTitle.Text = title;
            endTime = DateTime.Now.AddMinutes(minutes);
            
            timer?.Stop();
            timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            timer.Tick += Timer_Tick;
            timer.Start();
            
            UpdateTrayText();
            this.Title = $"⏱ {title}";
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            var remaining = endTime - DateTime.Now;
            if (remaining.TotalSeconds <= 0)
            {
                timer.Stop();
                if (isFocusPhase) 
                {
                    TriggerLock(); 
                }
                else 
                {
                    FinishRest();   
                }
            }
            else
            {
                txtStatusTime.Text = $"{remaining.Minutes:D2}:{remaining.Seconds:D2}";
                UpdateTrayText();
            }
        }

        private void UpdateTrayText()
        {
            if (trayIcon == null) return;
            var remaining = endTime - DateTime.Now;
            string phase = isFocusPhase ? "专注" : "休息";
            trayIcon.Text = remaining.TotalSeconds > 0 
                ? $"{phase}: {remaining.Minutes:D2}:{remaining.Seconds:D2} - 专注锁屏" 
                : "专注锁屏 - 就绪";
        }

        private void TriggerLock()
        {
            this.Hide();
            // 🔧 传入 settings.RestMinutes 作为休息倒计时时长
            var lockWin = new LockWindow(settings.RestMinutes, () =>
            {
                // ✅ 解锁后：直接结束当前周期，回到主界面
                // 不再进入所谓的"休息阶段"
                SwitchPanel("MainPanel");
                this.Show();
                this.WindowState = WindowState.Normal;
            });
            lockWin.ShowDialog();
        }

        private void FinishRest()
        {
            trayIcon?.ShowBalloonTip(2000, "🎉 周期完成", "专注与休息均已完成！", Forms.ToolTipIcon.Info);
            SwitchPanel("MainPanel");
            this.Show();
            this.WindowState = WindowState.Normal;
        }

        private void BtnForceStop_Click(object sender, RoutedEventArgs e)
        {
            var result = WpfMessageBox.Show("确定要强制结束当前周期吗？", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                timer?.Stop();
                SwitchPanel("MainPanel");
                this.Show();
                this.WindowState = WindowState.Normal;
            }
        }
    }

    public class AppSettings
    {
        public int FocusMinutes { get; set; } = 25;
        public int RestMinutes { get; set; } = 5;
    }
}