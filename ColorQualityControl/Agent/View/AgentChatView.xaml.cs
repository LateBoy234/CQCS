using MdXaml;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ThreeNH.ColorQualityControl;

namespace ThreeNH.ColorQualityControl.Agent
{
    /// <summary>
    /// SQCX AI 助手聊天窗口（k-netagent 风格外壳）。复用 AgentSession 与
    /// SqcxAgentQueryService，工具执行发生在后台线程，不会阻塞 UI。
    /// </summary>
    public partial class AgentChatView : Window
    {
        private AgentSession _session;
        private readonly List<ChatMessage> _history = new List<ChatMessage>();
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly DispatcherTimer _dotsTimer;

        private bool _isBusy;
        private int _dotsPhase;
        private Border _loadingBubble;
        private TextBlock _loadingText;

        public AgentChatView()
        {
            InitializeComponent();

            _dotsTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
            _dotsTimer.Tick += DotsTimer_Tick;

            // 输入框：回车/Shift+Enter/IME 逻辑封装在 ChatInputBox 内，这里只接收发送/中断请求。
            ChatInput.SendRequested += (s, e) => _ = SendAsync();
            ChatInput.CancelRequested += (s, e) => CancelCurrent();

            // 预填已保存的设置（API Key 出于安全不回填）。
            EndpointBox.Text = AppSettings.Agent_LlmEndpoint ?? string.Empty;
            ModelBox.Text = AppSettings.Agent_LlmModel ?? string.Empty;

            try
            {
                _session = AgentSession.CreateDefault();
                AppendMessage("assistant",
                    "你好，我是 SQCX 色彩质量管理助手。可以问我当前 Job、标样、试样色差与判定等问题，例如：\n" +
                    "· 当前连接的仪器和测量条件是什么？\n" +
                    "· 列出当前 Job 的所有标样。\n" +
                    "· 解释标样 3 下试样 2 的色差与判定。");
            }
            catch (InvalidOperationException ex)
            {
                AppendMessage("assistant", "Agent 尚未配置：" + ex.Message);
                ChatInput.IsEnabled = false;
                _devInfoExpander.IsExpanded = true;
            }
        }

        private void DotsTimer_Tick(object sender, EventArgs e)
        {
            if (_loadingText == null) return;
            _dotsPhase = (_dotsPhase + 1) % 3;
            _loadingText.Text = "正在思考" + new string('.', _dotsPhase + 1);
        }

        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            var ep = (EndpointBox.Text ?? string.Empty).Trim();
            var model = (ModelBox.Text ?? string.Empty).Trim();
            var key = ApiKeyBox.Password ?? string.Empty;

            if (string.IsNullOrWhiteSpace(ep) || string.IsNullOrWhiteSpace(model))
            {
                SettingsStatus.Foreground = Brushes.Red;
                SettingsStatus.Text = "端点和模型不能为空。";
                return;
            }

            AppSettings.Agent_LlmEndpoint = ep;
            AppSettings.Agent_LlmModel = model;
            if (!string.IsNullOrWhiteSpace(key))
            {
                AppSettings.Agent_LlmApiKey = key;
            }

            try
            {
                _session = AgentSession.CreateDefault();
                ChatInput.IsEnabled = true;
                SettingsStatus.Foreground = Brushes.Green;
                SettingsStatus.Text = "已保存并应用新配置。";
            }
            catch (InvalidOperationException ex)
            {
                SettingsStatus.Foreground = Brushes.Red;
                SettingsStatus.Text = "已保存，但会话创建失败：" + ex.Message;
            }
        }

        private async Task SendAsync()
        {
            if (_isBusy) return;
            string text = ChatInput.Text.Trim();
            if (string.IsNullOrEmpty(text) || _session == null)
            {
                return;
            }

            _isBusy = true;
            _cts = new CancellationTokenSource();
            ChatInput.InputTextBox.Clear();
            AppendMessage("user", text);
            ShowLoading();
            ChatInput.SetSendingState(true);
            try
            {
                string answer = await _session.ChatAsync(text, _history, _cts.Token);
                HideLoading();
                AppendMessage("assistant", answer);
            }
            catch (OperationCanceledException)
            {
                HideLoading();
                AppendMessage("assistant", "（已取消）");
            }
            catch (Exception ex)
            {
                HideLoading();
                AppendMessage("assistant", "出错了：" + ex.Message);
            }
            finally
            {
                ChatInput.SetSendingState(false);
                ChatInput.Focus();
                _isBusy = false;
            }
        }

        /// <summary>
        /// 中断当前正在进行的对话：取消底层 LLM 请求所需的 CancellationToken。
        /// SendAsync 捕获 OperationCanceledException 后以“（已取消）”收尾。
        /// </summary>
        private void CancelCurrent()
        {
            _cts?.Cancel();
        }

        private void AppendMessage(string role, string text)
        {
            bool isUser = role == "user";
            var bubble = new Border
            {
                Background = isUser
                    ? new SolidColorBrush(Color.FromRgb(0x1E, 0x90, 0xFF))
                    : new SolidColorBrush(Color.FromRgb(0xEE, 0xEE, 0xEE)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(8),
                Margin = new Thickness(10, 4, isUser ? 48 : 10, 4),
                HorizontalAlignment = isUser ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                MaxWidth = 360
            };

            if (isUser)
            {
                bubble.Child = new TextBlock
                {
                    Text = text,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = Brushes.White
                };
            }
            else
            {
                // 助手回复按 Markdown 渲染（标题/列表/加粗/代码块/表格等）
                var markdown = new MarkdownScrollViewer
                {
                    Markdown = text,
                    Background = Brushes.Transparent,
                    Foreground = Brushes.Black
                };
                // 禁用内部滚动，统一由外部 ScrollViewer 滚动，避免嵌套滚动条
               // ScrollViewer.SetVerticalScrollBarVisibility(markdown, ScrollBarVisibility.Disabled);
                bubble.Child = markdown;
            }

            MessagesPanel.Children.Add(bubble);
            MessageScroller.ScrollToEnd();
        }

        private void HideLoading()
        {
            _dotsTimer.Stop();
            if (_loadingBubble != null)
            {
                MessagesPanel.Children.Remove(_loadingBubble);
                _loadingBubble = null;
                _loadingText = null;
            }
        }

        private void ShowLoading()
        {
            _dotsPhase = 0;
            _loadingText = new TextBlock
            {
                Text = "正在思考",
                Foreground = Brushes.Gray,
                TextWrapping = TextWrapping.Wrap
            };
            _loadingBubble = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0xEE, 0xEE, 0xEE)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(8),
                Margin = new Thickness(4, 4, 48, 4),
                HorizontalAlignment = HorizontalAlignment.Left,
                MaxWidth = 360,
                Child = _loadingText
            };
            MessagesPanel.Children.Add(_loadingBubble);
            MessageScroller.ScrollToEnd();
            _dotsTimer.Start();
        }
    }
}
