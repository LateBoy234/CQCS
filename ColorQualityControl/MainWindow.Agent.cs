using System.Windows;
using ThreeNH.ColorQualityControl.Agent;

namespace ThreeNH.ColorQualityControl
{
    public partial class MainWindow
    {
        private AgentChatView _agentChatView;

        /// <summary>菜单“AI 助手”入口：单例打开聊天窗口。</summary>
        private void OpenAgentAssistant_Click(object sender, RoutedEventArgs e)
        {
            if (_agentChatView != null)
            {
                if (!_agentChatView.IsVisible)
                {
                    _agentChatView.Show();
                }
                _agentChatView.Activate();
                return;
            }

            _agentChatView = new AgentChatView
            {
                Owner = this
            };
            _agentChatView.Closed += (s, args) => _agentChatView = null;
            _agentChatView.Show();
        }
    }
}
