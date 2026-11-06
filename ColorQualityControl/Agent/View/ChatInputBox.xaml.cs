using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ThreeNH.ColorQualityControl.Agent
{
    /// <summary>
    /// 聊天输入框：封装多行输入、发送按钮、回车发送 / Shift+Enter 换行，
    /// 以及对中文输入法组字（上屏候选）的回车保护。
    /// </summary>
    public partial class ChatInputBox : UserControl
    {
        /// <summary>用户触发发送（点击按钮或回车）时引发。</summary>
        public event EventHandler SendRequested;

        /// <summary>发送进行中用户点击“中断”按钮时引发，用于取消当前对话。</summary>
        public event EventHandler CancelRequested;

        private bool _sending;

        public string Text
        {
            get => InputTextBox.Text;
            set => InputTextBox.Text = value ?? string.Empty;
        }

        public ChatInputBox()
        {
            InitializeComponent();

            BtnSend.Click += BtnSend_Click;
            InputTextBox.KeyDown += InputTextBox_KeyDown;
        }

        private void InputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;

            
          
            if ((Keyboard.Modifiers & ModifierKeys.Shift) == 0)
            {
                // Enter 发送：阻止 TextBox 原生换行（AcceptsReturn=False）。
                e.Handled = true;
                RaiseSend();
            }
            else
            {
                // Shift+Enter：在光标处插入换行。
                e.Handled = true;
                int caret = InputTextBox.CaretIndex;
                InputTextBox.Text = InputTextBox.Text.Insert(caret, Environment.NewLine);
                InputTextBox.CaretIndex = caret + Environment.NewLine.Length;
            }
        }

        private void RaiseSend()
        {
            if (!string.IsNullOrWhiteSpace(InputTextBox.Text))
            {
                SendRequested?.Invoke(this, EventArgs.Empty);
            }
        }

        private void BtnSend_Click(object sender, RoutedEventArgs e)
        {
            if (_sending)
            {
                CancelRequested?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                RaiseSend();
            }
        }

        /// <summary>
        /// 由宿主切换按钮状态：sending=true 显示方块（中断），false 显示发送箭头。
        /// </summary>
        public void SetSendingState(bool sending)
        {
            _sending = sending;
            if (SendIcon != null)
                SendIcon.Visibility = sending ? Visibility.Collapsed : Visibility.Visible;
            if (StopIcon != null)
                StopIcon.Visibility = sending ? Visibility.Visible : Visibility.Collapsed;
            BtnSend.ToolTip = sending ? "点击取消" : "发送";
        }
    }
}
