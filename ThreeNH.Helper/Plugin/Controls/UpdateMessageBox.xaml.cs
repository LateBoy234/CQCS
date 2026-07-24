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

namespace ThreeNH.Helper.Plugin.Controls
{
    /// <summary>
    /// UpdateMessageBox.xaml 的交互逻辑
    /// </summary>
    public partial class UpdateMessageBox : Window
    {
        public bool Result { get; private set; }

        public UpdateMessageBox(UpdateMessageBoxViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            
            // 根据 NewVersionText 是否为空来调整窗口显示内容
            if (string.IsNullOrWhiteSpace(viewModel.NewVersionText))
            {
                tb_NewVersionText.Visibility = Visibility.Collapsed;
                tb_CurrentVersionText.Visibility = Visibility.Collapsed;
                sp_FileSizeText.Visibility = Visibility.Collapsed;
                btnNo.Visibility = Visibility.Collapsed; // 隐藏"否"按钮
            }

            // 调整窗口高度大小为自适应
            this.SizeToContent = SizeToContent.Height;
            this.Width = 480; // 设置最大宽度

          //  LoadIcon();
             // 支持 ESC 关闭
             PreviewKeyDown += (s, e) =>
            {
                if (e.Key == Key.Escape)
                {
                    Result = false;
                    Close();
                }
            };
        }

        //private void LoadIcon()
        //{
        //    // 获取当前程序集名称（ThreeNH.Helper）
        //    var assemblyName = typeof(UpdateMessageBox).Assembly.GetName().Name;

        //    // 动态构建 URI
        //    var uri = $"pack://application:,,,/{assemblyName};component/Plugin/Images/download.png";

        //    // 设置图片源
        //    imgIcon.Source = new BitmapImage(new Uri(uri));
        //    titleimgIcon.Source = new BitmapImage(new Uri(uri));

            
        //}

        private void btnYes_Click(object sender, RoutedEventArgs e)
        {
            Result = true;
            Close();
        }

        private void btnNo_Click(object sender, RoutedEventArgs e)
        {
            Result = false;
            Close();
        }

        /// <summary>
        /// 显示更新对话框（静态方法）
        /// </summary>
        public static bool ShowUpdateDialog(AppVersionResponse updateInfo, string currentVersion, UpdateMessageBoxViewModel updateMessage)
        {
            // 设置语言
          //  LocalizationHelper.SetLanguage(culture);

            var vm = new UpdateMessageBoxViewModel
            {
                Title = updateMessage.Title,
                NewVersionText = updateMessage.NewVersionText,
                CurrentVersionText = updateMessage.CurrentVersionText,
                FileSizeLabel = updateMessage.FileSizeLabel,
                FileSizeText = updateMessage.ErrorFlag?null: $"{updateInfo.FileSize / 1024.0 / 1024.0:F1} MB",
                DownloadPrompt =updateMessage.DownloadPrompt,
                YesButtonText =updateMessage.YesButtonText,
                NoButtonText =updateMessage.NoButtonText
            };

            var dialog = new UpdateMessageBox(vm);
            dialog.ShowDialog();
            return dialog.Result;
        }
    }

    public class UpdateMessageBoxViewModel
    {
        public string Title { get; set; }
        public string NewVersionText { get; set; }
        public string CurrentVersionText { get; set; }
        public string FileSizeLabel { get; set; }
        public string FileSizeText { get; set; }
        public string DownloadPrompt { get; set; }
        public string YesButtonText { get; set; }
        public string NoButtonText { get; set; }
        public bool ErrorFlag { get; set; } = false;
    }
}
