using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Text.RegularExpressions;
using ThreeNH.ColorAlgorithm;
using ThreeNH.ColorQualityControl;

namespace ThreeNH.Controls
{
    /// <summary>
    /// ReflectanceInputControl.xaml 的交互逻辑
    /// </summary>
    public partial class SpectrumInputControl : UserControl
    {
        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register("Minimum", typeof(int), typeof(SpectrumInputControl),
                new PropertyMetadata(400, new PropertyChangedCallback(OnWavelengthRangeChanged),
                    new CoerceValueCallback(CoerceMinimum)));

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register("Maximum", typeof(int),
                typeof(SpectrumInputControl), 
                new PropertyMetadata(700, new PropertyChangedCallback(OnWavelengthRangeChanged),
                    new CoerceValueCallback(CoerceMaximum)));

        public static readonly DependencyProperty IntervalProperty =
            DependencyProperty.Register("Interval", typeof(int),
                typeof(SpectrumInputControl), 
                new PropertyMetadata(10, new PropertyChangedCallback(OnWavelengthRangeChanged),
                    new CoerceValueCallback(CoerceInterval)));

        public static readonly DependencyProperty IsPercentProperty =
            DependencyProperty.Register("IsPercent", typeof(bool),
                typeof(SpectrumInputControl), new PropertyMetadata(true));

        

        private List<TextBox> reflectanceTextBoxList = new List<TextBox>();

        public SpectrumInputControl()
        {
            InitializeComponent();
            Initialize();
        }

        /// <summary>
        /// 验证数据是否有效
        /// </summary>
        /// <returns>如果有效返回真，否则返回假</returns>
        public bool Verify()
        {
            double max = IsPercent ? 300.0 : 3.0;

            foreach(TextBox txtbox in reflectanceTextBoxList)
            {
                txtbox.Text = txtbox.Text.Trim();
                if (string.IsNullOrWhiteSpace(txtbox.Text))
                {
                    MessageBox.Show(S._("PleaseInputReflectance"));
                    txtbox.Focus();
                    return false;
                }

                double result;
                if (!Double.TryParse(txtbox.Text, out result))
                {
                    MessageBox.Show(S._("TheReflectanceMustBeNumerics"));
                    txtbox.Focus();
                    txtbox.SelectAll();
                    return false;
                }

                if (result < 0.0 || result > max)
                {
                    MessageBox.Show(string.Format(S._("ReflectanceMusetBeBetween0And_0"), max));
                    txtbox.Focus();
                    txtbox.SelectAll();
                    return false;
                }
            }
            return true;
        }

        private static object CoerceMinimum(DependencyObject d, object value)
        {
            SpectrumInputControl control = (SpectrumInputControl)d;
            if ((int)value > control.Maximum)
                return control.Maximum;
            return value;
        }
        private static object CoerceMaximum(DependencyObject d, object value)
        {
            SpectrumInputControl control = (SpectrumInputControl)d;
            if ((int)value < control.Minimum)
                return control.Minimum;
            return value;
        }
        private static object CoerceInterval(DependencyObject d, object value)
        {
            SpectrumInputControl control = (SpectrumInputControl)d;
            if ((int)value > (control.Maximum - control.Minimum))
            {
                return control.Maximum - control.Minimum;
            }
            return value;
        }
        private static void OnWavelengthRangeChanged(DependencyObject d, 
            DependencyPropertyChangedEventArgs e)
        {
            SpectrumInputControl control = (SpectrumInputControl)d;
            control.Initialize();
        }

        /// <summary>
        /// 反射率的最小波长
        /// </summary>
        public int Minimum
        {
            get { return (int)GetValue(MinimumProperty); }
            set { SetValue(MinimumProperty, value); }
        }
        /// <summary>
        /// 反射率的最大波长
        /// </summary>
        public int Maximum
        {
            get { return (int)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }
        /// <summary>
        /// 反射率的波长间隔
        /// </summary>
        public int Interval
        {
            get { return (int)GetValue(IntervalProperty); }
            set { SetValue(IntervalProperty, value); }
        }

        /// <summary>
        /// 反射率数据是否是百分比
        /// </summary>
        public bool IsPercent
        {
            get { return (bool)GetValue(IsPercentProperty); }
            set { SetValue(IsPercentProperty, value); }
        }

        /// <summary>
        /// 反射率数据
        /// </summary>
        public Spectrum Spectrum
        {
            get
            {
                Spectrum spectrum = new Spectrum(new WavelengthRange(Minimum, Maximum, Interval));

                double result;
                int i = 0;
                foreach(TextBox txtbox in reflectanceTextBoxList)
                {
                    if (Double.TryParse(txtbox.Text, out result))
                    {
                        spectrum.Data[i] = IsPercent ? result / 100.0 : result;
                    }

                    i += 1;
                }

                return spectrum;
            }
            set
            {
                if (value != null)
                {
                    if (value.Range.Min != Minimum)
                    {
                        Minimum = value.Range.Min;
                    }
                    if (value.Range.Max != Maximum)
                    {
                        Maximum = value.Range.Max;
                    }
                    if (value.Range.Step != Interval)
                    {
                        Interval = value.Range.Step;
                    }

                    for (int i = 0; i < value.Range.Count; i++)
                    {
                        if (IsPercent)
                            reflectanceTextBoxList[i].Text = (value.Data[i] * 100).ToString("F2");
                        else
                            reflectanceTextBoxList[i].Text = value.Data[i].ToString("F2");
                    }
                }
            }
        }


        private void Initialize()
        {
            grid.RowDefinitions.Clear();
            grid.ColumnDefinitions.Clear();
            grid.Children.Clear();
            reflectanceTextBoxList.Clear();

            int rowCount = Maximum / 100 - Minimum / 100 + 2;
            int columnCount = 100 / Interval + 1;

            for (int i = 0; i < rowCount; i++)
            {
                RowDefinition rowdef = new RowDefinition();
                if (i == 0)
                {
                    rowdef.Height = GridLength.Auto;
                }
                grid.RowDefinitions.Add(rowdef);
            }
            for (int i = 0; i < columnCount; i++)
            {
                ColumnDefinition coldef = new ColumnDefinition();
                if (i == 0)
                {
                    coldef.Width = GridLength.Auto;
                }
                grid.ColumnDefinitions.Add(coldef);
            }

            // 行标签
            for (int row = 1; row < rowCount; row++)
            {
                TextBlock txtblk = new TextBlock();
                txtblk.VerticalAlignment = VerticalAlignment.Center;
                txtblk.Text = (((Minimum / 100) + row - 1)*100).ToString();
                grid.Children.Add(txtblk);
                Grid.SetRow(txtblk, row);
                Grid.SetColumn(txtblk, 0);
            }
            // 列标签
            for (int col = 1; col < columnCount; col++)
            {
                TextBlock txtblk = new TextBlock();
                txtblk.HorizontalAlignment = HorizontalAlignment.Center;
                txtblk.Text = ((col - 1) * Interval).ToString("D2");
                txtblk.Margin = new Thickness(3, 3, 3, 3);
                grid.Children.Add(txtblk);
                Grid.SetColumn(txtblk, col);
                Grid.SetRow(txtblk, 0);
            }

            for (int wavelength = Minimum; wavelength <= Maximum; wavelength += Interval)
            {
                int row = wavelength / 100 - Minimum / 100 + 1;
                int column = (wavelength % 100) / Interval + 1;

                TextBox txtbox = new TextBox();
                reflectanceTextBoxList.Add(txtbox);

                txtbox.MinWidth = 40;
                txtbox.Margin = new Thickness(2, 1, 2, 1);
                txtbox.Tag = wavelength;

                CommandBinding commandBinding = new CommandBinding(
                    ApplicationCommands.Paste, OnPasteExecuted, PasteCanExecute);
                txtbox.CommandBindings.Add(commandBinding);

                grid.Children.Add(txtbox);
                Grid.SetColumn(txtbox, column);
                Grid.SetRow(txtbox, row);
            }
        }


        private void OnPasteExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            TextBox txtbox = sender as TextBox;
            if (txtbox != null && txtbox.Tag is int)
            {
                int wavelength = (int)txtbox.Tag;
                int index = (wavelength - Minimum) / Interval;

                Regex regex = new Regex(@"[\s,]+");
                foreach(string s in regex.Split(Clipboard.GetText()))
                {
                    if (index < reflectanceTextBoxList.Count)
                    {
                        reflectanceTextBoxList[index++].Text = s;
                    }
                }
            }
        }

        private void PasteCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = Clipboard.ContainsText();
        }
    }
}
