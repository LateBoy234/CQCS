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
    public class SpecularComponentComboBox : ComboBox
    {
        public SpecularComponentComboBox()
        {
            BindingItemsSource();
        }

        public void BindingItemsSource(IEnumerable<ValueDescription> items = null, bool enableWhileNull = true)
        {
            if (enableWhileNull)
            {
                List<SpecularComponent> list = new List<SpecularComponent>();
                if (!WithNone)
                {
                    list.Add(SpecularComponent.Unknown);
                }
                if (!WithSciAndSce)
                {
                    list.Add(SpecularComponent.SCIAndSCE);
                }

                ItemsSource = items ?? (EnumHelper.GetAllValuesAndDescriptions(typeof(SpecularComponent),
                        e => !list.Contains((SpecularComponent)e))).ToArray();
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
            binding.Path = new PropertyPath(SpecularComponentProperty);
            SetBinding(SelectedValueProperty, binding);
        }

        private static void OnDependencyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as SpecularComponentComboBox;
            if (control != null)
            {
                if (e.Property == WithNoneProperty || e.Property == WithSciAndSceProperty)
                    control.BindingItemsSource();
            }
        }

        public static readonly DependencyProperty WithNoneProperty =
            DependencyProperty.Register("WithNone", typeof(bool), typeof(SpecularComponentComboBox),
                new PropertyMetadata(false, OnDependencyPropertyChanged));

        public static readonly DependencyProperty WithSciAndSceProperty =
            DependencyProperty.Register("WithSciAndSce", typeof(bool), typeof(SpecularComponentComboBox),
                new PropertyMetadata(true, OnDependencyPropertyChanged));

        /// <summary>
        /// 是否包括 SpecularComponent.Unknown
        /// </summary>
        public bool WithNone
        {
            get { return (bool)GetValue(WithNoneProperty); }
            set { SetValue(WithNoneProperty, value); }
        }

        /// <summary>
        /// 是否包含I+E
        /// </summary>
        public bool WithSciAndSce
        {
            get { return (bool)GetValue(WithSciAndSceProperty); }
            set { SetValue(WithSciAndSceProperty, value); }
        }

        public static readonly DependencyProperty SpecularComponentProperty =
            DependencyProperty.Register("SpecularComponent", typeof(SpecularComponent),
                typeof(SpecularComponentComboBox), new PropertyMetadata(SpecularComponent.SCIAndSCE));

        public SpecularComponent SpecularComponent
        {
            get { return (SpecularComponent)GetValue(SpecularComponentProperty); }
            set { SetValue(SpecularComponentProperty, value); }
        }
    }
}
