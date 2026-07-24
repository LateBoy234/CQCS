using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using ThreeNH.Helper;
using ThreeNH.Optical;

namespace ThreeNH.Controls
{
    class ColorIndexComboBox : ComboBox
    {
        public ColorIndexComboBox()
        {
            BindingItemsSource();
        }

        public void BindingItemsSource(System.Collections.Generic.IEnumerable<ValueDescription> items = null, bool enableWhileNull = true)
        {
            if (enableWhileNull)
            {
                if (WithNone)
                {
                    ItemsSource = items ?? EnumHelper.GetAllValuesAndDescriptions(typeof(ColorIndex));
                }
                else
                {
                    ItemsSource = items ?? EnumHelper.GetAllValuesAndDescriptions(typeof(ColorIndex),
                        e => (ColorIndex)e != ColorIndex.None);
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
            binding.Path = new PropertyPath(ColorIndexProperty);
            SetBinding(SelectedValueProperty, binding);
        }

        private static void OnDependencyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as ColorIndexComboBox;
            if (control != null)
            {
                if (e.Property == WithNoneProperty)
                    control.BindingItemsSource();
            }
        }

        public static readonly DependencyProperty WithNoneProperty =
            DependencyProperty.Register("WithNone", typeof(bool), typeof(ColorIndexComboBox),
                new PropertyMetadata(true, OnDependencyPropertyChanged));

        /// <summary>
        /// 是否包括 ColorIndex.Unknown
        /// </summary>
        public bool WithNone
        {
            get { return (bool)GetValue(WithNoneProperty); }
            set { SetValue(WithNoneProperty, value); }
        }

        public static readonly DependencyProperty ColorIndexProperty =
            DependencyProperty.Register("ColorIndex", typeof(ColorIndex),
                typeof(ColorIndexComboBox), new PropertyMetadata(ColorIndex.None));

        public ColorIndex ColorIndex
        {
            get { return (ColorIndex)GetValue(ColorIndexProperty); }
            set { SetValue(ColorIndexProperty, value); }
        }
    }
}
