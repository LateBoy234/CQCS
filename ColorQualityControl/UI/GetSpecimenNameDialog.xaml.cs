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

namespace ThreeNH.ColorQualityControl.UI
{
    /// <summary>
    /// GetSpecimenNameDialog.xaml 的交互逻辑
    /// </summary>
    public partial class GetSpecimenNameDialog : Window
    {
        public GetSpecimenNameDialog()
        {
            InitializeComponent();
        }


        public string SpecimenName
        {
            get
            {
                return txtboxName.Text;
            }
            set
            {
                txtboxName.Text = value;
            }
        }

        public string Material
        {
            get { return txtboxMaterial.Text; }
            set { txtboxMaterial.Text = value; }
        }

        private void OnOkClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtboxName.Text))
            {
                MessageBox.Show(S._("PleaseInputName"));
                txtboxName.Focus();
                return;
            }

            DialogResult = true;
        }
    }
}
