using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using ThreeNH.ColorimeterInterface;
using ThreeNH.Helper;

namespace ThreeNH.Controls
{
    class SpectrumMeasurementModeComboBox : ComboBox
    {
        public SpectrumMeasurementModeComboBox()
        {
            BindingItemsSource();
        }

        public void BindingItemsSource(IEnumerable<ValueDescription> items = null, bool enableWhileNull = true)
        {
            if (enableWhileNull)
                ItemsSource = items ?? EnumHelper.GetAllValuesAndDescriptions(typeof(SpectrumMeasurementMode));
            else 
            {
                ItemsSource = items;
                IsEnabled = items != null;
            }
            DisplayMemberPath = "Description";
            SelectedValuePath = "Value";

            Binding binding = new Binding();
            binding.Source = this;
            binding.Path = new PropertyPath(SpectrumMeasurementModeProperty);
            SetBinding(SelectedValueProperty, binding);
        }


        public static readonly DependencyProperty SpectrumMeasurementModeProperty =
            DependencyProperty.Register("SpectrumMeasurementMode", typeof(SpectrumMeasurementMode),
                typeof(SpectrumMeasurementModeComboBox), new PropertyMetadata(SpectrumMeasurementMode.Reflective));

        public SpectrumMeasurementMode SpectrumMeasurementMode
        {
            get { return (SpectrumMeasurementMode)GetValue(SpectrumMeasurementModeProperty); }
            set { SetValue(SpectrumMeasurementModeProperty, value); }
        }
    }
}
