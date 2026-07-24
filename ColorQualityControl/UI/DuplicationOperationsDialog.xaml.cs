using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    /// DuplicationOperationsDialog.xaml 的交互逻辑
    /// </summary>
    public partial class DuplicationOperationsDialog : Window
    {
        public static readonly DependencyProperty TotalCountProperty =
            DependencyProperty.Register("TotalCount", typeof(int), typeof(DuplicationOperationsDialog));
        public static readonly DependencyProperty DuplicationCountProperty =
            DependencyProperty.Register("DuplicationCount", typeof(int), typeof(DuplicationOperationsDialog));
        public static readonly DependencyProperty SourceNameProperty =
            DependencyProperty.Register("SourceName", typeof(string), typeof(DuplicationOperationsDialog));
        public static readonly DependencyProperty DestinationNameProperty =
            DependencyProperty.Register("DestinationName", typeof(string), typeof(DuplicationOperationsDialog));

        public DuplicationOperationsDialog()
        {
            InitializeComponent();
            DataContext = this;
        }

        /// <summary>
        /// 复制的总个数
        /// </summary>
        public int TotalCount
        {
            get { return (int)GetValue(TotalCountProperty); }
            set { SetValue(TotalCountProperty, value);}
        }

        /// <summary>
        /// 重复个数
        /// </summary>
        public int DuplicationCount
        {
            get { return (int) GetValue(DuplicationCountProperty);}
            set { SetValue(DuplicationCountProperty, value);}
        }

        /// <summary>
        /// 导入源名称
        /// </summary>
        public string SourceName
        {
            get { return (string) GetValue(SourceNameProperty); }
            set { SetValue(SourceNameProperty, value);}
        }

        /// <summary>
        /// 导入目的名称
        /// </summary>
        public string DestinationName
        {
            get { return (string)GetValue(DestinationNameProperty); }
            set { SetValue(DestinationNameProperty, value); }
        }

        /// <summary>
        /// 处理重复项的方法
        /// "R"表示替换
        /// "S"表示跳过
        /// "L"表示用户用动选择
        /// </summary>
        public string HowToDealDuplication { get; set; }

        private void OnChoiceHowTo(object sender, ExecutedRoutedEventArgs e)
        {
            HowToDealDuplication = e.Parameter as string;
            DialogResult = true;
        }
    }
}
