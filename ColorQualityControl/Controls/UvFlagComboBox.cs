using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using ThreeNH.Helper;
using ThreeNH.Optical;

namespace ThreeNH.Controls
{
    public class UvFlagComboBox : ComboBox
    {
        public UvFlagComboBox()
        {
            BindingItemsSource();
        }

        public void BindingItemsSource(IEnumerable<ValueDescription> items = null, bool enableWhileNull = true)
        {
            if (enableWhileNull)
                ItemsSource = items ?? EnumHelper.GetAllValuesAndDescriptions(typeof(UVFlag));
            else
            {
                ItemsSource = items;
                IsEnabled = items != null;
            }
            DisplayMemberPath = "Description";
            SelectedValuePath = "Value";

            Binding binding = new Binding { Source = this, Path = new PropertyPath(UvFlagProperty) };
            SetBinding(SelectedValueProperty, binding);
        }


        public static readonly DependencyProperty UvFlagProperty =
            DependencyProperty.Register("UvFlag", typeof(UVFlag),
                typeof(UvFlagComboBox), new PropertyMetadata(UVFlag.None));

        public UVFlag UvFlag
        {
            get { return (UVFlag)GetValue(UvFlagProperty); }
            set { SetValue(UvFlagProperty, value); }
        }
    }
}
