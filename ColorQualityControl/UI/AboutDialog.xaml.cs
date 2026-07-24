using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Xml;

namespace ThreeNH.ColorQualityControl.UI
{
    /// <summary>
    /// AboutDialog.xaml 的交互逻辑
    /// </summary>
    public partial class AboutDialog : Window
    {
        public AboutDialog()
        {
            Type clsType = typeof(App);
            Assembly assy = clsType.Assembly;

            SoftwareVersion = string.Format("v{0}", App.AppVersion.ToString());


            InitializeComponent();
            Icon = Application.Current.MainWindow.Icon;
            DataContext = this;


            var infoElem = App.GetAboutInfoElement();
            if (infoElem != null)
            {
                defaultAboutInfo.Visibility = Visibility.Collapsed;
                grid.Children.Add(infoElem);
                Grid.SetColumn(infoElem, 1);
                Grid.SetRow(infoElem, 0);
            }
        }

        public string SoftwareVersion
        {
            get;set;
        }
    }
}
