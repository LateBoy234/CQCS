using System.Windows;
using System.Windows.Input;
using ThreeNH.ColorQualityControl.UI;

namespace ThreeNH.ColorQualityControl
{
    public partial class MainWindow
    {
        private void OpenScanBarcodeDialogExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            ShowScanBarcodeDialog();
        }

        private void ShowScanBarcodeDialog()
        {
            //var ownerWidth = ActualWidth > 0 ? ActualWidth : Width;
            //if (ownerWidth <= 0)
            //    ownerWidth = MinWidth > 0 ? MinWidth : 800;

            //var dlg = new ScanBarcodeDialog
            //{
            //    Owner = this,
            //    Width = ownerWidth / 2
            //};
            //dlg.ShowDialog();
            //if (!string.IsNullOrWhiteSpace(dlg.Barcode))
            //    ScanBarcodeTextBox.Text = dlg.Barcode;
        }
    }
}
