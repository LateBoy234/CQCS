using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using ThreeNH.Helper;
using ThreeNH.Optical;

namespace ThreeNH.Controls
{
    public class IlluminantComboBox : ComboBox
    {
        public IlluminantComboBox()
        {
            BindingItemsSource();
        }

        public void BindingItemsSource(IEnumerable<ValueDescription> items = null, bool enableWhileNull = true)
        {
            if (enableWhileNull)
            {
                if (WithNone)
                {
                    ItemsSource = items ?? EnumHelper.GetAllValuesAndDescriptions(typeof(StandardIlluminant));
                }
                else
                {
                    ItemsSource = items ?? EnumHelper.GetAllValuesAndDescriptions(typeof(StandardIlluminant),
                        e => (StandardIlluminant)e != StandardIlluminant.Unknown);
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
            binding.Path = new PropertyPath(IlluminantProperty);
            SetBinding(SelectedValueProperty, binding);
        }

        private static void OnDependencyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as IlluminantComboBox;
            if (control != null)
            {
                if (e.Property == WithNoneProperty)
                    control.BindingItemsSource();
            }
        }

        public static readonly DependencyProperty WithNoneProperty =
            DependencyProperty.Register("WithNone", typeof(bool), typeof(IlluminantComboBox),
                new PropertyMetadata(false, OnDependencyPropertyChanged));

        /// <summary>
        /// 是否包括 StandardIlluminant.Unknown
        /// </summary>
        public bool WithNone
        {
            get { return (bool)GetValue(WithNoneProperty); }
            set { SetValue(WithNoneProperty, value); }
        }

        public static readonly DependencyProperty IlluminantProperty =
            DependencyProperty.Register("Illuminant", typeof(StandardIlluminant),
                typeof(IlluminantComboBox), new PropertyMetadata(StandardIlluminant.D65));

        public StandardIlluminant Illuminant
        {
            get { return (StandardIlluminant)GetValue(IlluminantProperty); }
            set { SetValue(IlluminantProperty, value); }
        }
    }
}
