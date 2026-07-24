using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Input;
using YRGetSamplingDataDlg = ThreeNH.ColorQualityControl.YRColorimeter.YRGetSamplingDataDlg;
using YRUploadDataDlg = ThreeNH.ColorQualityControl.YRColorimeter.YRUploadDataDlg;
using YRWhiteBoardSettingDlg = ThreeNH.ColorQualityControl.YRColorimeter.YRWhiteBoardSettingDlg;

namespace ThreeNH.ColorQualityControl
{
    public partial class MainWindow
    {
        private void YR_WhiteBoardSetting_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            YRWhiteBoardSettingDlg dlg = new YRWhiteBoardSettingDlg(Colorimeter as ThreeNH.YRColorimeter.Colorimeter);
            dlg.Owner = this;
            dlg.ShowDialog();
        }

        private void YR_GetSamplingData_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            YRGetSamplingDataDlg dlg = new YRGetSamplingDataDlg(Colorimeter as ThreeNH.YRColorimeter.Colorimeter);
            dlg.Owner = this;
            dlg.ShowDialog();
        }

        private void YR_UploadData_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            YRUploadDataDlg dlg = new YRUploadDataDlg(Colorimeter as ThreeNH.YRColorimeter.Colorimeter);
            dlg.Owner = this;
            dlg.ShowDialog();
        }
    }
}