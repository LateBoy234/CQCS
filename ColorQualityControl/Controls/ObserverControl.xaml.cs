using System;
using System.Windows;
using System.Windows.Controls;
using ThreeNH.Optical;

namespace ThreeNH.Controls
{
    /// <summary>
    /// ObserverControl.xaml 的交互逻辑
    /// </summary>
    public partial class ObserverControl : UserControl
    {
        public event EventHandler<ValueChangedEventArgs<StandardObserver>> ObserverChanged;

        public ObserverControl()
        {
            InitializeComponent();
            DataContext = this;
        }

        private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as ObserverControl;
            control.UpdateChecked((StandardObserver)e.NewValue);

            if (e.Property == ObserverProperty)
            {
                control.OnObserverChanged((StandardObserver)e.NewValue, (StandardObserver)e.OldValue);
            }
            //else if (e.Property == IsShowInComboBoxProperty)
            //{
            //    control.OnIsShowInComboBoxChanged((bool)e.NewValue, (bool)e.OldValue);
            //}
        }

        private void OnObserverChanged(StandardObserver newValue, StandardObserver oldValue)
        {
            ObserverChanged?.Invoke(this, new ValueChangedEventArgs<StandardObserver>(newValue, oldValue));
        }

        private void UpdateChecked(StandardObserver newValue)
        {
            switch (newValue)
            {
                case StandardObserver.CIE1931:
                    CIE1931.IsChecked = true;
                    break;
                case StandardObserver.CIE1964:
                    CIE1964.IsChecked = true;
                    break;
                default:
                    break;
            }
        }

        public static readonly DependencyProperty ObserverProperty =
            DependencyProperty.Register("Observer", typeof(StandardObserver),
                typeof(ObserverControl),
                new PropertyMetadata(StandardObserver.CIE1964, OnPropertyChanged));



        public StandardObserver Observer
        {
            get { return (StandardObserver)GetValue(ObserverProperty); }
            set { SetValue(ObserverProperty, value); }
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (CIE1931.IsChecked == true)
                Observer = StandardObserver.CIE1931;
            else
                Observer = StandardObserver.CIE1964;
        }


        //public static readonly DependencyProperty IsShowInComboBoxProperty =
        //    DependencyProperty.Register("IsShowInComboBox", typeof(bool),
        //        typeof(ObserverControl), new PropertyMetadata(false, OnPropertyChanged));

        ///// <summary>
        ///// 是否已ComboBox显示
        ///// </summary>
        //public bool IsShowInComboBox
        //{
        //    get { return (bool)GetValue(IsShowInComboBoxProperty); }
        //    set { SetValue(IsShowInComboBoxProperty, value); }
        //}

        //private void OnIsShowInComboBoxChanged(bool newValue, bool oldValue)
        //{
        //    if (newValue != oldValue)
        //    {

        //    }
        //}
    }
}
