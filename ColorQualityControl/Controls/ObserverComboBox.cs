using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using ThreeNH.Helper;
using ThreeNH.Optical;

namespace ThreeNH.Controls
{
    public class ObserverComboBox : ComboBox
    {
        public ObserverComboBox()
        {
            BindingItemsSource();
        }

        public void BindingItemsSource(System.Collections.Generic.IEnumerable<ValueDescription> items = null, bool enableWhileNull = true)
        {
            if (enableWhileNull)
            {
                if (WithNone)
                {
                    ItemsSource = items ?? EnumHelper.GetAllValuesAndDescriptions(typeof(StandardObserver));
                }
                else
                {
                    ItemsSource = items ?? EnumHelper.GetAllValuesAndDescriptions(typeof(StandardObserver),
                        e => (StandardObserver)e != StandardObserver.Unknown);
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
            binding.Path = new PropertyPath(ObserverProperty);
            SetBinding(SelectedValueProperty, binding);
        }

        private static void OnDependencyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as ObserverComboBox;
            if (control != null)
            {
                if (e.Property == WithNoneProperty)
                    control.BindingItemsSource();
            }
        }

        public static readonly DependencyProperty WithNoneProperty =
            DependencyProperty.Register("WithNone", typeof(bool), typeof(ObserverComboBox),
                new PropertyMetadata(false, OnDependencyPropertyChanged));

        /// <summary>
        /// 是否包括 StandardObserver.Unknown
        /// </summary>
        public bool WithNone
        {
            get { return (bool)GetValue(WithNoneProperty); }
            set { SetValue(WithNoneProperty, value); }
        }

        public static readonly DependencyProperty ObserverProperty =
            DependencyProperty.Register("Observer", typeof(StandardObserver),
                typeof(ObserverComboBox), new PropertyMetadata(StandardObserver.CIE1964));

        public StandardObserver Observer
        {
            get { return (StandardObserver)GetValue(ObserverProperty); }
            set { SetValue(ObserverProperty, value); }
        }
    }
}
