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
    class WhitenessIndexItem
    {
        /// <summary>
        /// 指數的名字
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 標樣的白度指數
        /// </summary>
        public string Standard { get; set; }
        /// <summary>
        /// 試樣的白度指數
        /// </summary>
        public string Test { get; set; }
        /// <summary>
        /// 試樣與標樣之差
        /// </summary>
        public string Diff { get; set; }
    }

    /// <summary>
    /// WhitenessDialog.xaml 的交互逻辑
    /// </summary>
    public partial class WhitenessDialog : Window
    {
        List<WhitenessIndexItem> whitenessIndexItems = new List<WhitenessIndexItem>();
        public WhitenessDialog()
        {
            InitializeComponent();

            whitenessIndexItems.Add(new WhitenessIndexItem { Name="WI(E313)" });
            whitenessIndexItems.Add(new WhitenessIndexItem { Name = "WI(CIE/ISO)" });
            whitenessIndexItems.Add(new WhitenessIndexItem { Name = "WI(Hunter 1942)" });
            whitenessIndexItems.Add(new WhitenessIndexItem { Name = "WI(Hunter 1960)" });
            whitenessIndexItems.Add(new WhitenessIndexItem { Name = "WI(Tappl)" });
            whitenessIndexItems.Add(new WhitenessIndexItem { Name = "WI(Taube)" });
            dataGrid.ItemsSource = whitenessIndexItems;

        }
    }
}
