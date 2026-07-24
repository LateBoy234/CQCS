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
using ThreeNH.DataModel;
using ThreeNH.Helper;

namespace ThreeNH.ColorQualityControl.UI
{
    /// <summary>
    /// AutoSelectStandardSettingDlg.xaml 的交互逻辑
    /// </summary>
    public partial class AutoSelectStandardSettingDlg : Window, IDataErrorInfo
    {
        public static readonly DependencyProperty dLProperty =
            DependencyProperty.Register("dL", typeof(string), typeof(AutoSelectStandardSettingDlg),
                new PropertyMetadata(null));
        public static readonly DependencyProperty daProperty =
            DependencyProperty.Register("da", typeof(string), typeof(AutoSelectStandardSettingDlg),
                new PropertyMetadata(null));
        public static readonly DependencyProperty dbProperty =
            DependencyProperty.Register("db", typeof(string), typeof(AutoSelectStandardSettingDlg),
                new PropertyMetadata(null));
        public static readonly DependencyProperty dEabProperty =
            DependencyProperty.Register("dEab", typeof(string), typeof(AutoSelectStandardSettingDlg),
                new PropertyMetadata(null));

        public static readonly DependencyProperty dLEnabledProperty =
            DependencyProperty.Register("dLEnabled", typeof(bool), typeof(AutoSelectStandardSettingDlg),
                new PropertyMetadata(false));
        public static readonly DependencyProperty daEnabledProperty =
            DependencyProperty.Register("daEnabled", typeof(bool), typeof(AutoSelectStandardSettingDlg),
                new PropertyMetadata(false));
        public static readonly DependencyProperty dbEnabledProperty =
            DependencyProperty.Register("dbEnabled", typeof(bool), typeof(AutoSelectStandardSettingDlg),
                new PropertyMetadata(false));
        public static readonly DependencyProperty dEabEnabledProperty =
            DependencyProperty.Register("dEabEnabled", typeof(bool), typeof(AutoSelectStandardSettingDlg),
                new PropertyMetadata(true));


        public AutoSelectStandardSettingDlg()
        {
            InitializeComponent();

            var limits = (Application.Current as App).SystemDefaultConfig.AutoSelectStandardLimits;
            dLEnabled = limits[ChromaticityField.dL].UseForJudgement;
            dL = limits[ChromaticityField.dL].Upper.ToString();
            daEnabled = limits[ChromaticityField.da].UseForJudgement;
            da = limits[ChromaticityField.da].Upper.ToString();
            dbEnabled = limits[ChromaticityField.db].UseForJudgement;
            db = limits[ChromaticityField.db].Upper.ToString();
            dEabEnabled = limits[ChromaticityField.dEab].UseForJudgement;
            dEab = limits[ChromaticityField.dEab].Upper.ToString();

            DataContext = this;
        }

        private void SetItem(Tolerance limits, ChromaticityField field, string value)
        {
            if (value != null)
            {
                limits[field] = new ToleranceItem
                {
                    Lower = null,
                    Upper = double.Parse(value),
                    UseForJudgement = true
                };
            }
            else
            {
                var saved = limits[field].Upper;
                limits[field] = new ToleranceItem
                {
                    Lower = null,
                    Upper = saved,
                    UseForJudgement = false
                };
            }

        }

        private void OnOkClick(object sender, RoutedEventArgs e)
        {
            if (Error != null)
            {
                MessageBox.Show(Error);
                return;
            }

            var limits = (Application.Current as App).SystemDefaultConfig.AutoSelectStandardLimits;
            SetItem(limits, ChromaticityField.dL, dLEnabled ? dL : null);
            SetItem(limits, ChromaticityField.da, daEnabled ? da : null);
            SetItem(limits, ChromaticityField.db, dbEnabled ? db : null);
            SetItem(limits, ChromaticityField.dEab, dEabEnabled ? dEab : null);

            DialogResult = true;
        }

        internal string dL
        {
            get { return (string)GetValue(dLProperty); }
            set { SetValue(dLProperty, value); }
        }
        internal string da
        {
            get { return (string)GetValue(daProperty); }
            set { SetValue(daProperty, value); }
        }
        internal string db
        {
            get { return (string)GetValue(dbProperty); }
            set { SetValue(dbProperty, value); }
        }
        internal string dEab
        {
            get { return (string)GetValue(dEabProperty); }
            set { SetValue(dEabProperty, value); }
        }

        internal bool dLEnabled
        {
            get { return (bool)GetValue(dLEnabledProperty); }
            set { SetValue(dLEnabledProperty, value); }
        }
        internal bool daEnabled
        {
            get { return (bool)GetValue(daEnabledProperty); }
            set { SetValue(daEnabledProperty, value); }
        }
        internal bool dbEnabled
        {
            get { return (bool)GetValue(dbEnabledProperty); }
            set { SetValue(dbEnabledProperty, value); }
        }
        internal bool dEabEnabled
        {
            get { return (bool)GetValue(dEabEnabledProperty); }
            set { SetValue(dEabEnabledProperty, value); }
        }

        Dictionary<ChromaticityField, string> _errors = new Dictionary<ChromaticityField, string>();
        public string Error
        {
            get
            {
                string strErr = null;
                foreach(var field in _errors.Keys)
                {
                    if (_errors[field] != null)
                    {
                        if (strErr == null)
                            strErr = _errors[field];
                        else
                            strErr += "\n" + _errors[field];
                    }
                }
                return strErr;
            }
        }

        public string this[string columnName]
        {
            get
            {
                string value = null;
                ChromaticityField field = ChromaticityField.dEab;
                switch (columnName)
                {
                    case "dL":
                        field = ChromaticityField.dL;
                        if (dLEnabled)
                            value = dL;
                        break;
                    case "da":
                        field = ChromaticityField.da;
                        if (daEnabled)
                            value = da;
                        break;
                    case "db":
                        field = ChromaticityField.db;
                        if (dbEnabled)
                            value = db;
                        break;
                    case "dEab":
                        field = ChromaticityField.dEab;
                        if (dEabEnabled)
                            value = dEab;
                        break;

                }
                if (value == null)
                {
                    _errors[field] = null;
                    return null;
                }

                double dVal;
                if (!double.TryParse(value, out dVal))
                { 
                    _errors[field] = string.Format(S._("X_MustBeNumeric"), field.Description());
                }
                else if (dVal < 0)
                {
                    _errors[field] = string.Format(S._("X_MustBeGreaterOrEqual_Y"), field.Description(), 0);
                }
                else
                {
                    _errors[field] = null;
                }
                return Error;
            }
        }
    }
}
