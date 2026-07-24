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
    public class LensSwitchModeComboBox : ComboBox
    {
        public LensSwitchModeComboBox()
        {
            BindingItemsSource();
        }

        public void BindingItemsSource(IEnumerable<ValueDescription> items = null, bool enableWhileNull = true)
        {
            if (enableWhileNull)
                ItemsSource = items ?? EnumHelper.GetAllValuesAndDescriptions(typeof(LensSwitchMode));
            else
            {
                ItemsSource = items;
                IsEnabled = items != null;
            }
            DisplayMemberPath = "Description";
            SelectedValuePath = "Value";

            Binding binding = new Binding();
            binding.Source = this;
            binding.Path = new PropertyPath(LensSwitchModeProperty);
            SetBinding(SelectedValueProperty, binding);
        }


        public static readonly DependencyProperty LensSwitchModeProperty =
            DependencyProperty.Register("LensSwitchMode", typeof(LensSwitchMode),
                typeof(LensSwitchModeComboBox), new PropertyMetadata(LensSwitchMode.Manual));

        public LensSwitchMode LensSwitchMode
        {
            get { return (LensSwitchMode)GetValue(LensSwitchModeProperty); }
            set { SetValue(LensSwitchModeProperty, value); }
        }
    }
}
