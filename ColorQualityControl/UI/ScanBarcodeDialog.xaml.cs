using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;

namespace ThreeNH.ColorQualityControl.UI
{
    public partial class ScanBarcodeDialog : Window
    {
        public ScanBarcodeDialog()
        {
            InitializeComponent();
        }

        public string Barcode { get; private set; }

        private void OnBarcodeRichTextBoxLoaded(object sender, RoutedEventArgs e)
        {
            FocusBarcodeInput(selectAll: true);
        }

        private void OnBarcodePreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
                return;

            // Shift+Enter 手动换行；单独 Enter 留给扫码枪结束符
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                return;

            e.Handled = true;
            var text = GetPlainText().Trim();
            if (string.IsNullOrWhiteSpace(text))
                return;

            Barcode = text;
            var preview = text.Replace("\r\n", " ").Replace('\n', ' ');
            if (preview.Length > 80)
                preview = preview.Substring(0, 80) + "...";
            HintTextBlock.Text = string.Format(S._("ScanBarcodeCapturedHint"), preview);
            FocusBarcodeInput(selectAll: true);
        }

        private string GetPlainText()
        {
            return new TextRange(
                BarcodeRichTextBox.Document.ContentStart,
                BarcodeRichTextBox.Document.ContentEnd).Text;
        }

        private void FocusBarcodeInput(bool selectAll)
        {
            BarcodeRichTextBox.Dispatcher.InvokeAsync(() =>
            {
                BarcodeRichTextBox.Focus();
                if (selectAll)
                    BarcodeRichTextBox.SelectAll();
            }, System.Windows.Threading.DispatcherPriority.Input);
        }
    }
}
