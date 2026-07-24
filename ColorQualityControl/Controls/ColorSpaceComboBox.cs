using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using ThreeNH.Helper;
using ThreeNH.Optical;

namespace ThreeNH.Controls
{
    public class ColorSpaceComboBox : ComboBox
    {
        public ColorSpaceComboBox()
        {
            BindingItemsSource();
        }

        public void BindingItemsSource(System.Collections.Generic.IEnumerable<ValueDescription> items = null, bool enableWhileNull = true)
        {
            if (enableWhileNull)
            {
                if (WithNone)
                {
                    ItemsSource = items ?? EnumHelper.GetAllValuesAndDescriptions(typeof(ColorSpace));
                }
                else
                {
                    ItemsSource = items ?? EnumHelper.GetAllValuesAndDescriptions(typeof(ColorSpace),
                        e => (ColorSpace)e != ColorSpace.Unknown);
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
            binding.Path = new PropertyPath(ColorSpaceProperty);
            SetBinding(SelectedValueProperty, binding);
        }

        private static void OnDependencyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as ColorSpaceComboBox;
            if (control != null)
            {
                if (e.Property == WithNoneProperty)
                    control.BindingItemsSource();
            }
        }

        public static readonly DependencyProperty WithNoneProperty =
            DependencyProperty.Register("WithNone", typeof(bool), typeof(ColorSpaceComboBox),
                new PropertyMetadata(false, OnDependencyPropertyChanged));

        /// <summary>
        /// 是否包括 StandardColorSpace.Unknown
        /// </summary>
        public bool WithNone
        {
            get { return (bool)GetValue(WithNoneProperty); }
            set { SetValue(WithNoneProperty, value); }
        }

        public static readonly DependencyProperty ColorSpaceProperty =
            DependencyProperty.Register("ColorSpace", typeof(ColorSpace),
                typeof(ColorSpaceComboBox), new PropertyMetadata(ColorSpace.CIE_LAB));

        public ColorSpace ColorSpace
        {
            get { return (ColorSpace)GetValue(ColorSpaceProperty); }
            set { SetValue(ColorSpaceProperty, value); }
        }
    }
}
