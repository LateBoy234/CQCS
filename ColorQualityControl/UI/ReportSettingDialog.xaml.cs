using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ThreeNH.ColorQualityControl.Reports;

namespace ThreeNH.ColorQualityControl.UI
{
    /// <summary>
    /// ReportSettingDialog.xaml 的交互逻辑
    /// </summary>
    public partial class ReportSettingDialog : Window
    {
        public ReportSettingDialog()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void OnOkClick(object sender, RoutedEventArgs e)
        {
            if (ReportSetting == null)
                ReportSetting = new ReportSetting();

            ReportSetting.Title = txtTitle.Text.Trim();
            ReportSetting.Subtitle = txtSubtitle.Text.Trim();
            ReportSetting.CompanyAddress = txtAddress.Text.Trim();
            ReportSetting.CompanyEnglishAddress = txtEnglishAddress.Text.Trim();
            DialogResult = true;
        }

        public ReportSetting ReportSetting
        {
            get;
            set;
        }

    }
}
