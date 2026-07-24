using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using ThreeNH.DataModel;
using System.Diagnostics;

namespace ThreeNH.ColorQualityControl.UI
{
    /// <summary>
    /// ToleranceControl.xaml 的交互逻辑
    /// </summary>
    public partial class ToleranceControl : UserControl
    {
        class Item
        {
            public Item()
            {
                IsDirty = false;
            }

            public string Name {get; set; }
            public bool UseForJudgement { get; set; }
            public string Lower { get; set; }
            public string Upper { get; set; }
            public bool IsLowerNull { get; set; }
            public bool IsDirty { get; set; }
            public string Precision { get; set; } = "F1"; // 精度
        };

        public static readonly DependencyProperty KL00Property =
            DependencyProperty.Register("KL00", typeof(double), typeof(ToleranceControl));
        public static readonly DependencyProperty KC00Property =
            DependencyProperty.Register("KC00", typeof(double), typeof(ToleranceControl));
        public static readonly DependencyProperty KH00Property =
            DependencyProperty.Register("KH00", typeof(double), typeof(ToleranceControl));

        public static readonly DependencyProperty KL94Property =
            DependencyProperty.Register("KL94", typeof(double), typeof(ToleranceControl));
        public static readonly DependencyProperty KC94Property =
            DependencyProperty.Register("KC94", typeof(double), typeof(ToleranceControl));
        public static readonly DependencyProperty KH94Property =
            DependencyProperty.Register("KH94", typeof(double), typeof(ToleranceControl));

        public static readonly DependencyProperty KLcmcProperty =
            DependencyProperty.Register("KLcmc", typeof(double), typeof(ToleranceControl));
        public static readonly DependencyProperty KCcmcProperty =
            DependencyProperty.Register("KCcmc", typeof(double), typeof(ToleranceControl));

        public static readonly DependencyProperty IsShowAllItemsProperty =
            DependencyProperty.Register("IsShowAllItems", typeof(bool), typeof(ToleranceControl),
                new PropertyMetadata(false));

        public event EventHandler IsModifiedChanged;

        private ObservableCollection<Item> Items = new ObservableCollection<Item>();
        private Tolerance originalTolerance;

        public ToleranceControl()
        {
            InitializeComponent();

            DataContext = this;
            ToleranceItems.ItemsSource = Items;

            ToleranceItems.BeginningEdit += ToleranceItems_BeginningEdit;
            ToleranceItems.CellEditEnding += ToleranceItems_CellEditEnding;
        }

        private void ToleranceItems_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            var item = e.Row.Item as Item;
            if (item != null)
            {
                if (e.Column is DataGridTextColumn)
                {
                    var column = e.Column as DataGridTextColumn;
                    var binding = column.Binding as Binding;
                    var txtbox = e.EditingElement as TextBox;
                    try
                    {
                        double value = double.Parse(txtbox.Text);
                        if (binding.Path.Path == "Lower")
                        {
                            if (value.ToString(item.Precision) != item.Lower)
                                item.IsDirty = true;
                        }
                        else if (binding.Path.Path == "Upper")
                        {
                            if (value.ToString(item.Precision) != item.Upper)
                                item.IsDirty = true;
                        }
                    }
                    catch
                    {
                        e.Cancel = true;
                        txtbox.Text = binding.Path.Path == "Lower" ? item.Lower : item.Upper;
                        return;
                    }
                }
                else if (e.Column is DataGridCheckBoxColumn)
                {
                    var checkbox = e.EditingElement as CheckBox;
                    if (checkbox.IsChecked != item.UseForJudgement)
                        item.IsDirty = true;
                }

                IsModified = item.IsDirty || IsModified;
            }
        }

        private void ToleranceChanged(object sender, EventArgs e)
        {
            if (originalTolerance != null)
            {
                if (KL00 != originalTolerance.KL00 || KC00 != originalTolerance.KC00 || KH00 != originalTolerance.KH00
                    || KL94 != originalTolerance.KL94 || KC94 != originalTolerance.KC94 || KH94 != originalTolerance.KH94
                    || KLcmc != originalTolerance.KLcmc || KCcmc != originalTolerance.KCcmc)
                {
                    IsModified = true || IsModified;
                }
            }
        }

        private void ToleranceItems_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            // 如果列绑定的数据是"Lower"且其为IsLowerNull为真，禁用编辑
            if (e.Column is DataGridTextColumn)
            {
                Binding binding = (e.Column as DataGridTextColumn).Binding as Binding;
                if (binding != null && binding.Path.Path == "Lower")
                {
                    if (e.Row.DataContext is Item)
                    {
                        e.Cancel = ((Item)e.Row.DataContext).IsLowerNull;
                        return;
                    }
                }
            }
            e.Cancel = false;
        }

        public void Restore()
        {
            Tolerance = originalTolerance;
        }

        public bool VerifyData()
        {
            try
            {
                ToleranceItems.CommitEdit();
                foreach (Item item in Items)
                {
                    var fied = item.Name.ToChromaticityField();
                    if (item.UseForJudgement)
                    {
                        double? upper;
                        double? lower;
                        try
                        {
                            upper = double.Parse(item.Upper);
                            lower = !item.IsLowerNull ? double.Parse(item.Lower) : (double?)null;
                        }
                        catch
                        {
                            MessageBox.Show(string.Format(S._("PleaseInputNumber")));
                            return false;
                        }

                        if ((item.IsLowerNull && upper >= 0.0) || (!item.IsLowerNull && lower <= upper))
                            continue;

                        if (item.IsLowerNull && upper < 0)
                        {
                            MessageBox.Show(string.Format(S._("TheUpperOf_0_MustGreaterThanZero"), item.Name));
                        }
                        else if (!item.IsLowerNull && upper < lower)
                        {
                            MessageBox.Show(S._("TheLowerMustBeLessThanTheUpper"));
                        }

                        ToleranceItems.SelectedItem = item;
                        ToleranceItems.CurrentCell = new DataGridCellInfo(item, ToleranceItems.Columns[3]);
                        ToleranceItems.BeginEdit();
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("验证容差数据时发生异常，原因："+ex);
                return false;
            }
           
            return true;
        }

        public void AcceptChange()
        {
            if (originalTolerance != null)
            {
                originalTolerance.KL00 = KL00;
                originalTolerance.KC00 = KC00;
                originalTolerance.KH00 = KH00;

                originalTolerance.KL94 = KL94;
                originalTolerance.KC94 = KC94;
                originalTolerance.KH94 = KH94;

                originalTolerance.KLcmc = KLcmc;
                originalTolerance.KCcmc = KCcmc;

                foreach (Item item in Items)
                {
                    if (!item.IsDirty)
                        continue;

                    var field = item.Name.ToChromaticityField();
                    double? lower = item.IsLowerNull ? (double?) null : double.Parse(item.Lower);
                    double? upper = double.Parse(item.Upper);
                    originalTolerance[field] =
                        new ToleranceItem
                        {
                            Lower = field.IsReflectanceDifference() ? lower / 100.0 : lower,
                            Upper = field.IsReflectanceDifference() ? upper / 100.0 : upper,
                            UseForJudgement = item.UseForJudgement
                        };
                }
            }
            IsModified = false;
        }

        /// <summary>
        /// 可用的容差项
        /// </summary>
        public ChromaticityField[] ValidItems { get; set; }

        public Tolerance Tolerance
        {
            get
            {
                Tolerance tolerance = originalTolerance != null ? new Tolerance(originalTolerance) : new Tolerance(ValidItems);

                tolerance.KL00 = KL00;
                tolerance.KC00 = KC00;
                tolerance.KH00 = KH00;

                tolerance.KL94 = KL94;
                tolerance.KC94 = KC94;
                tolerance.KH94 = KH94;

                tolerance.KLcmc = KLcmc;
                tolerance.KCcmc = KCcmc;

                foreach (Item item in Items)
                {
                    if (!item.IsDirty)
                    {
                        continue;
                    }

                    var field = item.Name.ToChromaticityField();

                    if (field.IsReflectanceDifference())
                    {
                        tolerance[field] = new ToleranceItem
                        {
                            Lower = double.Parse(item.Lower) / 100.0,
                            Upper = double.Parse(item.Upper) / 100.0,
                            UseForJudgement = item.UseForJudgement
                        };
                    }
                    else
                    {
                        tolerance[field] = new ToleranceItem
                        {
                            Lower = item.Lower != null ? double.Parse(item.Lower) : (double ?)null,
                            Upper = item.Upper != null ? double.Parse(item.Upper) : (double ?)null,
                            UseForJudgement = item.UseForJudgement
                        };
                    }

                }

                return tolerance;
            }
            set
            {
                Items.Clear();

                // 可用的容差项显示在前面
                if (ValidItems != null)
                {
                    foreach (var key in ValidItems)
                    {
                        string precision = "F1";
                        if (key == ChromaticityField.dx || key == ChromaticityField.dy ||
                            key == ChromaticityField.dLuminanceFactor)
                        {
                            precision = "F3";
                        }

                        Items.Add(new Item
                        {
                            Name = key.Name(),
                            UseForJudgement = value[key].UseForJudgement,
                            Lower = (key.IsReflectanceDifference() ? value[key].Lower * 100.0 : value[key].Lower)?.ToString(precision),
                            IsLowerNull = value[key].Lower == null,
                            Upper = (key.IsReflectanceDifference() ? value[key].Upper * 100.0 : value[key].Upper)?.ToString(precision),
                            Precision = precision,
                        });
                    }
                }

                // 如果显示所有容差项，接著显示其他项
                if (IsShowAllItems)
                {
                    foreach (var key in Tolerance.AllToleranceItemKeys)
                    {
                        if (!ValidItems.Contains(key))
                        {
                            string precision = "F1";
                            if (key == ChromaticityField.dx || key == ChromaticityField.dy ||
                                key == ChromaticityField.dLuminanceFactor)
                            {
                                precision = "F3";
                            }

                            var lower = key.IsReflectanceDifference() ? value[key].Lower * 100.0 : value[key].Lower;
                            var upper = key.IsReflectanceDifference() ? value[key].Upper * 100.0 : value[key].Upper;
                            var item = new Item
                            {
                                Name = key.Name(),
                                UseForJudgement = value[key].UseForJudgement,
                                Lower = lower?.ToString(precision),
                                IsLowerNull = value[key].Lower == null,
                                Upper = upper?.ToString(precision),
                                Precision = precision,
                            };
                            
                            Items.Add(item);
                        }
                    }

                }

                KL94 = value.KL94;
                KC94= value.KC94;
                KH94= value.KH94;

                KL00 = value.KL00;
                KC00 = value.KC00;
                KH00 = value.KH00;

                KLcmc = value.KLcmc;
                KCcmc = value.KCcmc;

                originalTolerance = value;
                IsModified = false;
            }
        }

        bool isModified = false;
        public bool IsModified
        {
            get { return isModified; }
            set
            {
                if (value != isModified)
                {
                    isModified = value;
                    OnIsModifiedChanged();
                }
            }
        }

        public double KL00
        {
            get { return (double)GetValue(KL00Property); }
            set { SetValue(KL00Property, value); }
        }
        public double KC00
        {
            get { return (double)GetValue(KC00Property); }
            set { SetValue(KC00Property, value); }
        }
        public double KH00
        {
            get { return (double)GetValue(KH00Property); }
            set { SetValue(KH00Property, value); }
        }

        public double KL94
        {
            get { return (double)GetValue(KL94Property); }
            set { SetValue(KL94Property, value); }
        }
        public double KC94
        {
            get { return (double)GetValue(KC94Property); }
            set { SetValue(KC94Property, value); }
        }
        public double KH94
        {
            get { return (double)GetValue(KH94Property); }
            set { SetValue(KH94Property, value); }
        }

        public double KLcmc
        {
            get { return (double)GetValue(KLcmcProperty); }
            set { SetValue(KLcmcProperty, value); }
        }
        public double KCcmc
        {
            get { return (double)GetValue(KCcmcProperty); }
            set { SetValue(KCcmcProperty, value); }
        }

        public bool IsShowAllItems
        {
            get { return (bool)GetValue(IsShowAllItemsProperty); }
            set { SetValue(IsShowAllItemsProperty, value); }
        }

        void OnIsModifiedChanged()
        {
            IsModifiedChanged?.Invoke(this, EventArgs.Empty);
        }

        private void ShowAllItems_Click(object sender, RoutedEventArgs e)
        {
            if (IsShowAllItems)
            {

            }
            else
            {

            }
        }
    }
}
