using System;
using System.Windows;
using System.Windows.Controls;
using ThreeNH.Optical;

namespace ThreeNH.Controls
{
    /// <summary>
    /// SpecularComponentControl.xaml 的交互逻辑
    /// </summary>
    public partial class SpecularComponentControl : UserControl
    {
        public event EventHandler<ValueChangedEventArgs<SpecularComponent>> SpecularComponentChanged;

        public SpecularComponentControl()
        {
            InitializeComponent();
            //DataContext = this;

            UpdateChecked(SpecularComponent);
        }

        private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as SpecularComponentControl;
            if (e.NewValue != e.OldValue)
            {
                control.UpdateChecked((SpecularComponent)e.NewValue);

                if (control.SpecularComponentChanged != null)
                {
                    control.SpecularComponentChanged.Invoke(control,
                        new ValueChangedEventArgs<SpecularComponent>(
                            (SpecularComponent)e.NewValue,
                            (SpecularComponent)e.OldValue));
                }
            }
        }

        private void UpdateChecked(SpecularComponent newValue)
        {
            switch(newValue)
            {
                case SpecularComponent.SCI:
                    if (SCI.IsChecked != true)
                        SCI.IsChecked = true;
                    break;
                case SpecularComponent.SCE:
                    if (SCE.IsChecked != true)
                        SCE.IsChecked = true;
                    break;
                case SpecularComponent.SCIAndSCE:
                    if (SCIAndSCE.IsChecked != true)
                        SCIAndSCE.IsChecked = true;
                    break;
                case SpecularComponent.Unknown:
                    if (Unknown.IsChecked != true)
                        Unknown.IsChecked = true;
                    break;
                default:
                    break;
            }
        }

        public static readonly DependencyProperty SpecularComponentProperty =
            DependencyProperty.Register("SpecularComponent", typeof(SpecularComponent),
                typeof(SpecularComponentControl), 
                new PropertyMetadata(SpecularComponent.SCIAndSCE, OnPropertyChanged));

        

        public SpecularComponent SpecularComponent
        {
            get { return (SpecularComponent)GetValue(SpecularComponentProperty); }
            set { SetValue(SpecularComponentProperty, value); }
        }

        private void OnRadioButtonClick(object sender, RoutedEventArgs e)
        {
            RadioButton button = sender as RadioButton;
            if (SCI.IsChecked == true)
                SpecularComponent = SpecularComponent.SCI;
            else if (SCE.IsChecked == true)
                SpecularComponent = SpecularComponent.SCE;
            else if (SCIAndSCE.IsChecked == true)
                SpecularComponent = SpecularComponent.SCIAndSCE;
            else if (Unknown.IsChecked == true)
                SpecularComponent = SpecularComponent.Unknown;
        }
    }
}
