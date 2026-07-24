using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using ThreeNH.Controls;
using ThreeNH.Helper;
using ThreeNH.Optical;

namespace ThreeNH.Controls
{
    public class LensPositionComboBox : ComboBox
    {
        public LensPositionComboBox()
        {
            BindingItemsSource();
        }

        public void BindingItemsSource(IEnumerable<ValueDescription> items = null, bool enableWhileNull = true)
        {
            if (enableWhileNull)
            {
                if (WithNone)
                {
                    ItemsSource = items ?? EnumHelper.GetAllValuesAndDescriptions(typeof(LensPosition));
                }
                else
                {
                    ItemsSource = items ?? EnumHelper.GetAllValuesAndDescriptions(typeof(LensPosition),
                        e => (LensPosition)e != LensPosition.Unknown);
                }
            }
            else
            {
                ItemsSource = items;
                this.IsEnabled = items != null;
            }
            DisplayMemberPath = "Description";
            SelectedValuePath = "Value";

            Binding binding = new Binding();
            binding.Source = this;
            binding.Path = new PropertyPath(LensPositionProperty);
            SetBinding(SelectedValueProperty, binding);
        }

        private static void OnDependencyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as LensPositionComboBox;
            if (control != null)
            {
                if (e.Property == WithNoneProperty)
                    control.BindingItemsSource();
            }
        }

        public static readonly DependencyProperty WithNoneProperty =
            DependencyProperty.Register("WithNone", typeof(bool), typeof(LensPositionComboBox),
                new PropertyMetadata(true, OnDependencyPropertyChanged));

        /// <summary>
        /// 是否包括 LensPosition.Unknown
        /// </summary>
        public bool WithNone
        {
            get { return (bool)GetValue(WithNoneProperty); }
            set { SetValue(WithNoneProperty, value); }
        }

        public static readonly DependencyProperty LensPositionProperty =
            DependencyProperty.Register("LensPosition", typeof(LensPosition),
                typeof(LensPositionComboBox), new PropertyMetadata(LensPosition.Pos8));

        public LensPosition LensPosition
        {
            get { return (LensPosition)GetValue(LensPositionProperty); }
            set { SetValue(LensPositionProperty, value); }
        }
    }
}
