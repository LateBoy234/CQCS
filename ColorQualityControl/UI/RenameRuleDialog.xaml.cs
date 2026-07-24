using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using ThreeNH.ColorSpecimen;

namespace ThreeNH.ColorQualityControl.UI
{
    /// <summary>
    /// RenameRuleDialog.xaml 的交互逻辑
    /// </summary>
    public partial class RenameRuleDialog : Window, INotifyPropertyChanged
    {
        

        public RenameRuleDialog()
        {
            InitializeComponent();
            DataContext = this;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        
        private void OnOkClick(object sender, RoutedEventArgs e)
        {
            if (IsShowStandardRenameRule && !standardRenameRule.IsRuleValid)
            {
                MessageBox.Show(S._("StanardRulesInvalid"));
                standardRenameRule.Focus();
                return;
            }
            if (IsShowTestRenameRule && !testRenameRule.IsRuleValid)
            {
                MessageBox.Show(S._("TestRulesInvalid"));
                testRenameRule.Focus();
                return;
            }

            DialogResult = true;
        }

        /// <summary>
        /// 标样的命名规则
        /// </summary>
        public string StandardNameRule
        {
            get { return standardRenameRule.NameRule; }
            set { standardRenameRule.NameRule = value; }
        }
        /// <summary>
        /// 设置标样可用的命名变量，其中Key为变量，Value为变量的名称
        /// </summary>
        public KeyValuePair<string, string>[] StandardNameRuleVariables
        {
            get { return standardRenameRule.Variables; }
            set { standardRenameRule.Variables = value; }
        }
        /// <summary>
        /// 试样的命名规则
        /// </summary>
        public string TestNameRule
        {
            get { return testRenameRule.NameRule; }
            set { testRenameRule.NameRule = value; }
        }
        /// <summary>
        /// 设置试样可用的命名变量，其中Key为变量，Value为变量的名称
        /// </summary>
        public KeyValuePair<string, string>[] TestNameRuleVariables
        {
            get { return testRenameRule.Variables; }
            set { testRenameRule.Variables = value; }
        }
        /// <summary>
        /// 是否显示标样重命名规则
        /// </summary>
        public bool IsShowStandardRenameRule
        {
            get { return standardRenameRule.IsVisible; }
            set
            {
                standardRenameRule.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
            }
        }
        public bool IsShowTestRenameRule
        {
            get { return testRenameRule.IsVisible; }
            set
            {
                testRenameRule.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        string material = null;
        public string Material
        {
            get { return material; }
            set
            {
                if (material != value)
                {
                    material = value;
                    OnPropertyChanged("Material");
                }
            }
        }

        
    }
}
