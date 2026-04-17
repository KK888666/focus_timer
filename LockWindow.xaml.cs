using System;
using System.Windows;
using System.Windows.Threading;

namespace GlassTodo
{
    public partial class LockWindow : Window
    {
        private int correctAnswer;
        private Action onUnlock;
        private DispatcherTimer restTimer;
        private DateTime restEndTime;
        private bool isUnlocked = false;

        public LockWindow(int restMinutes, Action unlockCallback)
        {
            InitializeComponent();
            
            onUnlock = unlockCallback;
            GenerateQuestion();
            StartRestTimer(restMinutes);
            
            // ✅ 双重保险：窗口完全加载后自动聚焦，无需鼠标点击
            this.Loaded += (s, e) => txtAnswer.Focus();
        }

        private void GenerateQuestion()
        {
            Random random = new Random();
            int num1 = random.Next(10, 50);
            int num2 = random.Next(5, 30);
            correctAnswer = num1 + num2;
            txtQuestion.Text = $"{num1} + {num2} = ?";
        }

        private void StartRestTimer(int minutes)
        {
            restEndTime = DateTime.Now.AddMinutes(minutes);
            restTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            restTimer.Tick += RestTimer_Tick;
            restTimer.Start();
            UpdateRestDisplay();
        }

        private void RestTimer_Tick(object sender, EventArgs e)
        {
            UpdateRestDisplay();
            
            var remaining = restEndTime - DateTime.Now;
            if (remaining.TotalSeconds <= 0)
            {
                restTimer.Stop();
                txtRestCountdown.Text = "00:00";
            }
        }

        private void UpdateRestDisplay()
        {
            var remaining = restEndTime - DateTime.Now;
            if (remaining.TotalSeconds > 0)
            {
                txtRestCountdown.Text = $"{remaining.Minutes:D2}:{remaining.Seconds:D2}";
            }
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftCtrl) || 
                System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.RightCtrl))
            {
                if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftShift) || 
                    System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.RightShift))
                {
                    if (e.Key == System.Windows.Input.Key.M)
                    {
                        ForceUnlock();
                        e.Handled = true;
                        return;
                    }
                }
            }
            
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                e.Handled = true;
            }
            
            if (e.Key == System.Windows.Input.Key.Enter && !isUnlocked)
            {
                UnlockButton_Click(null, null);
                e.Handled = true;
            }
        }

        private void UnlockButton_Click(object sender, RoutedEventArgs e)
        {
            if (isUnlocked) return;
            
            string inputText = txtAnswer.Text.Trim();
            
            if (int.TryParse(inputText, out int userAnswer))
            {
                if (userAnswer == correctAnswer)
                {
                    isUnlocked = true;
                    restTimer?.Stop();
                    onUnlock?.Invoke();
                    this.Close();
                }
                else
                {
                    txtError.Text = "❌ 答案错误！请重新计算";
                    txtError.Visibility = Visibility.Visible;
                    txtAnswer.Text = "";
                    txtAnswer.Focus();
                }
            }
            else
            {
                txtError.Text = "❌ 请输入数字";
                txtError.Visibility = Visibility.Visible;
                txtAnswer.Focus();
            }
        }

        private void ForceUnlock()
        {
            isUnlocked = true;
            restTimer?.Stop();
            onUnlock?.Invoke();
            this.Close();
        }
    }
}