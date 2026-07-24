using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using ThreeNH.Optical;
using ThreeNH.Helper;

namespace ThreeNH.Controls
{


    /// <summary>
    /// IlluminantControl.xaml 的交互逻辑
    /// </summary>
    public partial class IlluminantControl : UserControl
    {
        public event EventHandler<ValueChangedEventArgs<StandardIlluminant>> IlluminantChanged;

        public IlluminantControl()
        {
            InitializeComponent();
            //comboBox.ItemsSource = IlluminantExtension.IlluminantNames;
            //comboBox.ItemsSource = Enum.GetValues(typeof(StandardIlluminant));
            DataContext = this;
        }

        public static DependencyProperty IlluminantProperty =
            DependencyProperty.Register("Illuminant", typeof(StandardIlluminant),
                typeof(IlluminantControl), 
                new PropertyMetadata(StandardIlluminant.D65, OnPropertyChanged));

        private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as IlluminantControl;
            if (e.Property == IlluminantProperty)
            {
                control.OnIlluminantChanged((StandardIlluminant)e.NewValue, (StandardIlluminant)e.OldValue);
            }
        }

        private void OnIlluminantChanged(StandardIlluminant newValue, StandardIlluminant oldValue)
        {
            IlluminantChanged?.Invoke(this,
                new ValueChangedEventArgs<StandardIlluminant>(newValue, oldValue));
        }

        public StandardIlluminant Illuminant
        {
            get { return (StandardIlluminant)GetValue(IlluminantProperty); }
            set { SetValue(IlluminantProperty, value); }
        }
    }
}
