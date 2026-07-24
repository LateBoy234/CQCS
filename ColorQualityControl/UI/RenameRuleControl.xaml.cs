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
using System.Windows.Navigation;
using System.Windows.Shapes;
using ThreeNH.ColorSpecimen;

namespace ThreeNH.ColorQualityControl.UI
{
    /// <summary>
    /// RenameRuleControl.xaml 的交互逻辑
    /// </summary>
    public partial class RenameRuleControl : UserControl
    {
        public static readonly DependencyProperty NameRuleProperty =
            DependencyProperty.Register("NameRule", typeof(string), typeof(RenameRuleControl));

        public RenameRuleControl()
        {
            InitializeComponent();
            DataContext = this;
        }

        public string NameRule
        {
            get { return (string)GetValue(NameRuleProperty); }
            set { SetValue(NameRuleProperty, value); }
        }

        public bool IsRuleValid
        {
            get
            {
                return NameGenerator.VerifyRule(NameRule);
            }
        }

        private void OnVariableMenuItemClick(object sender, RoutedEventArgs e)
        {
            MenuItem item = e.OriginalSource as MenuItem;
            if (item != null)
            {
                if (!txtbox.IsFocused)
                {
                    txtbox.Focus();
                    txtbox.Select(txtbox.Text.Length, 0);
                }

                if (item.Tag != null)
                    txtbox.SelectedText = "{" + item.Tag.ToString() + "}";
                else
                    txtbox.SelectedText = "{" + item.Header.ToString() + "}";
            }
        }

        List<MenuItem> variableMenuItems = new List<MenuItem>();
        public List<MenuItem> VariableMenuItems
        {
            get { return variableMenuItems; }
        }

        /// <summary>
        /// 设置可用的命名变量，其中Key为变量，Value为变量的名称
        /// </summary>
        public KeyValuePair<string, string>[] Variables
        {
            get
            {
                List<KeyValuePair<string, string>> items = new List<KeyValuePair<string, string>>();
                foreach(MenuItem item in VariableMenuItems)
                {
                    items.Add(new KeyValuePair<string, string>(item.Header as string, item.Tag as string));
                }
                return items.ToArray();
            }
            set
            {
                VariableMenuItems.Clear();
                foreach (var item in value)
                {
                    MenuItem menuItem = new MenuItem()
                    {
                        Header = item.Value,
                        Tag = item.Key
                    };
                    VariableMenuItems.Add(menuItem);
                }
            }
        }
        protected override void OnGotFocus(RoutedEventArgs e)
        {
            base.OnGotFocus(e);
            txtbox.Focus();
            e.Handled = true;
        }
    }
}
