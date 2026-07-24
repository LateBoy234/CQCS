using DocumentFormat.OpenXml.Drawing.Charts;
using System;
using System.Collections.Generic;
using System.Globalization;
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
using ThreeNH.Controls;
using ThreeNH.DataModel;
using ThreeNH.Optical;

namespace ThreeNH.ColorQualityControl.UI
{
    /// <summary>
    /// Pathlength.xaml 的交互逻辑
    /// </summary>
    public partial class Pathlength : Window
    {
        internal SystemDefaultConfig SystemDefaultConfig
        {
            get { return (Application.Current as App).SystemDefaultConfig; }
        }
        public Pathlength()
        {
            InitializeComponent();
            txt_Concentration.Text= SystemDefaultConfig.ICUMSAParameter.IUConcentration==0?"1": SystemDefaultConfig.ICUMSAParameter.IUConcentration.ToString();
            txt_Thickness.Text= SystemDefaultConfig.ICUMSAParameter.IUThickness==0?"1": SystemDefaultConfig.ICUMSAParameter.IUThickness.ToString();
            OnPathLengthEnumChanged(SystemDefaultConfig.ICUMSAParameter.PathLength);
        }

        

        private void OnPathLengthEnumChanged(PathLengthEnum newValue)
        {
            switch (newValue)
            {
                case PathLengthEnum.Pathlength_10:
                    Rb_10.IsChecked = true;
                    break;
                case PathLengthEnum.Pathlength_30:
                    Rb_30.IsChecked = true;
                    break;
                case PathLengthEnum.Pathlength_50:
                    Rb_50.IsChecked = true;
                    break;
                case PathLengthEnum.Pathlength_100:
                    Rb_100.IsChecked = true;
                    break;

                default:
                    Rb_10.IsChecked = true;
                    break;
            }
        }
        public event EventHandler<ValueEventArg<ICUMSAParameter>> ChangeAccepted;

        private void OkClick(object sender, RoutedEventArgs e)
        {
            ICUMSAParameter toleranceFromChanged = new ICUMSAParameter() {PathLength= PathLengthEnum.Pathlength_10 };// PathLengthEnum.Pathlength_10;
            if (Rb_30.IsChecked == true)
            {
                toleranceFromChanged.PathLength = PathLengthEnum.Pathlength_30;
            }
            if (Rb_50.IsChecked == true)
            {
                toleranceFromChanged.PathLength = PathLengthEnum.Pathlength_50;
            }
            else if (Rb_100.IsChecked == true)
            {
                toleranceFromChanged.PathLength = PathLengthEnum.Pathlength_100;
            }
            double t, c;
            double.TryParse(txt_Thickness.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out t);
            toleranceFromChanged.IUThickness=t;
            double.TryParse(txt_Concentration.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out c);
            toleranceFromChanged.IUConcentration=c;

            ChangeAccepted?.Invoke(this, new ValueEventArg<ICUMSAParameter> { Value = toleranceFromChanged });
            SystemDefaultConfig.ICUMSAParameter = toleranceFromChanged;
            this.Close();
        }
    }
}
