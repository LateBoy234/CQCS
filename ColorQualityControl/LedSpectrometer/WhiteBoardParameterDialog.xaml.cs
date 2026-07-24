using System;
using System.Windows;
using System.Windows.Controls;
using ThreeNH.ColorQualityControl;
using ThreeNH.Helper;

namespace ThreeNH.LedSpectrometer
{
    /// <summary>
    /// WhiteBoardParameterControl.xaml 的交互逻辑
    /// </summary>
    public partial class WhiteBoardParameterDialog : Window
    {
        public WhiteBoardParameterDialog(Spectrometer spectrometer)
        {
            Spectrometer = spectrometer;
            InitializeComponent();

            groupbox.Header = Spectrometer.SpecularComponent.Description();
        }

        public Spectrometer Spectrometer
        {
            get; set;
        }

        /// <summary>
        /// 验证用户输入是否有效
        /// </summary>
        /// <returns>如果输入有效返回真；否则返回假</returns>
        public bool Verify()
        {
            InnerNumber.Text = InnerNumber.Text.Trim();
            OuterNumber.Text = OuterNumber.Text.Trim();

            if (string.IsNullOrEmpty(InnerNumber.Text))
            {
                MessageBox.Show(S._("PleaseInputInnerNumber"));
                InnerNumber.Focus();
                return false;
            }
            if (string.IsNullOrEmpty(OuterNumber.Text))
            {
                MessageBox.Show(S._("PleaseInputExternalNumber"));
                OuterNumber.Focus();
                return false;
            }
            if (!Reflectances.Verify())
            {
                return false;
            }
            

            return true;
        }

        public WhiteBoardParameter WhiteBoardParameter
        {
            get
            {
                WhiteBoardParameter para = new WhiteBoardParameter();
                para.InnerNumber = InnerNumber.Text;
                para.OuterNumber = OuterNumber.Text;
                para.Spectrum = Reflectances.Spectrum;

                return para;
            }
            set
            {
                if (value == null)
                    return;

                InnerNumber.Text = value.InnerNumber;
                OuterNumber.Text = value.OuterNumber;
                Reflectances.Spectrum = value.Spectrum;
            }
        }

        private void OnReadClick(object sender, RoutedEventArgs e)
        {
            try
            {
                WhiteBoardParameter = Spectrometer.GetWhiteBoardParameter();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void OnWriteClick(object sender, RoutedEventArgs e)
        {
            if (Verify())
            {
                try
                {
                    Spectrometer.SetWhiteBoardParameter(WhiteBoardParameter);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }
    }
}
