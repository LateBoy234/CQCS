using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using ThreeNH.Helper;
using ThreeNH.Optical;

namespace ThreeNH.Controls
{
    class MeasurementCaliberComboBox : ComboBox
    {
        public MeasurementCaliberComboBox()
        {
            BindingItemsSource();
        }

        public void BindingItemsSource(IEnumerable<ValueDescription> items = null, bool enableWhileNull = true)
        {
            if (enableWhileNull)
                ItemsSource = items ?? EnumHelper.GetAllValuesAndDescriptions(typeof(MeasurementCaliber),
                    e => (MeasurementCaliber)e < MeasurementCaliber.Phi2);
            else
            {
                ItemsSource = items;
                IsEnabled = items != null;
            }

            DisplayMemberPath = "Description";
            SelectedValuePath = "Value";
            Binding binding = new Binding();
            binding.Source = this;
            binding.Path = new PropertyPath(MeasurementCaliberProperty);
            SetBinding(SelectedValueProperty, binding);
        }


        public static readonly DependencyProperty MeasurementCaliberProperty =
            DependencyProperty.Register("MeasurementCaliber", typeof(MeasurementCaliber),
                typeof(MeasurementCaliberComboBox), new PropertyMetadata(MeasurementCaliber.Phi8));

        public MeasurementCaliber MeasurementCaliber
        {
            get { return (MeasurementCaliber)GetValue(MeasurementCaliberProperty); }
            set { SetValue(MeasurementCaliberProperty, value); }
        }
    }
}
