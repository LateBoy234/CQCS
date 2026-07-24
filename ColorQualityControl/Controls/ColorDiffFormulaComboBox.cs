using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using ThreeNH.Helper;
using ThreeNH.Optical;

namespace ThreeNH.Controls
{
    public class ColorDiffFormulaComboBox : ComboBox
    {
        public ColorDiffFormulaComboBox()
        {
            BindingItemsSource();
        }

        public void BindingItemsSource(System.Collections.Generic.IEnumerable<ValueDescription> items = null, bool enableWhileNull = true)
        {
            if (enableWhileNull)
            {
                if (WithNone)
                {
                    ItemsSource = items ?? EnumHelper.GetAllValuesAndDescriptions(typeof(ColorDiffFormula));
                }
                else
                {
                    ItemsSource = items ?? EnumHelper.GetAllValuesAndDescriptions(typeof(ColorDiffFormula),
                        e => (ColorDiffFormula)e != ColorDiffFormula.Unknown);
                }
            }
            else
            {
                ItemsSource = items;
                IsEnabled = items != null;
            }

            DisplayMemberPath = "Description";
            SelectedValuePath = "Value";

            Binding binding = new Binding();
            binding.Source = this;
            binding.Path = new PropertyPath(ColorDiffFormulaProperty);
            SetBinding(SelectedValueProperty, binding);
        }

        private static void OnDependencyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as ColorDiffFormulaComboBox;
            if (control != null)
            {
                if (e.Property == WithNoneProperty)
                    control.BindingItemsSource();
            }
        }

        public static readonly DependencyProperty WithNoneProperty =
            DependencyProperty.Register("WithNone", typeof(bool), typeof(ColorDiffFormulaComboBox),
                new PropertyMetadata(false, OnDependencyPropertyChanged));

        /// <summary>
        /// 是否包括 StandardColorDiffFormula.Unknown
        /// </summary>
        public bool WithNone
        {
            get { return (bool)GetValue(WithNoneProperty); }
            set { SetValue(WithNoneProperty, value); }
        }

        public static readonly DependencyProperty ColorDiffFormulaProperty =
            DependencyProperty.Register("ColorDiffFormula", typeof(ColorDiffFormula),
                typeof(ColorDiffFormulaComboBox), new PropertyMetadata(ColorDiffFormula.CIE1976));

        public ColorDiffFormula ColorDiffFormula
        {
            get { return (ColorDiffFormula)GetValue(ColorDiffFormulaProperty); }
            set { SetValue(ColorDiffFormulaProperty, value); }
        }
    }
}
