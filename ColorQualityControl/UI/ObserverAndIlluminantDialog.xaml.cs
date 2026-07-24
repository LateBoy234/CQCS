using System;
using System.Windows;
using System.Windows.Controls;
using ThreeNH.Optical;
using ThreeNH.Helper;

namespace ThreeNH.ColorQualityControl.UI
{
    /// <summary>
    /// ObserverAndIlluminantWindow.xaml 的交互逻辑
    /// </summary>
    partial class ObserverAndIlluminantDialog : Window
    {
        public ObserverAndIlluminantDialog()
        {
            InitializeComponent();

            InitIlluminantComboBox(PrimaryIlluminant, true);
            InitIlluminantComboBox(SecondaryIlluminant, false);
            InitIlluminantComboBox(TertiaryIlluminant, false);

            InitObserverComboBox(PrimaryObserver, true);
            InitObserverComboBox(SecondaryObserver, false);
            InitObserverComboBox(TertiaryObserver, false);
        }

        void InitIlluminantComboBox(ComboBox combobox, bool isPrimary)
        {
            var values = Enum.GetValues(typeof(StandardIlluminant));
            foreach (StandardIlluminant illuminant in values)
            {
                if (!isPrimary || illuminant != StandardIlluminant.Unknown)
                {
                    ComboBoxItem item = new ComboBoxItem();
                    item.Content = illuminant.Description();
                    item.Tag = illuminant;
                    combobox.Items.Add(item);
                }
            }
        }

        void InitObserverComboBox(ComboBox comboBox, bool isPrimary)
        {
            var values = Enum.GetValues(typeof(StandardObserver));
            foreach (StandardObserver observer in values)
            {
                if (!isPrimary || observer != StandardObserver.Unknown)
                {
                    ComboBoxItem item = new ComboBoxItem();
                    item.Content = observer.Description();
                    item.Tag = observer;
                    comboBox.Items.Add(item);
                }
            }
        }

        internal ObserverAndIlluminantSetting Setting
        {
            get
            {
                ObserverAndIlluminantSetting setting = new ObserverAndIlluminantSetting();

                setting.PrimaryIlluminant = SelectedIlluminant(PrimaryIlluminant);
                setting.SecondaryIlluminant = SelectedIlluminant(SecondaryIlluminant);
                setting.TertiaryIlluminant = SelectedIlluminant(TertiaryIlluminant);

                setting.PrimaryObserver = SelectedObserver(PrimaryObserver);
                setting.SecondaryObserver = SelectedObserver(SecondaryObserver);
                setting.TertiaryObserver = SelectedObserver(TertiaryObserver);

                return setting;
            }
            set
            { 
                SelectIlluminant(PrimaryIlluminant, value.PrimaryIlluminant);
                SelectIlluminant(SecondaryIlluminant, value.SecondaryIlluminant);
                SelectIlluminant(TertiaryIlluminant, value.TertiaryIlluminant);

                SelectObserver(PrimaryObserver, value.PrimaryObserver);
                SelectObserver(SecondaryObserver, value.SecondaryObserver);
                SelectObserver(TertiaryObserver, value.TertiaryObserver);
            }
        }

        void SelectIlluminant(ComboBox comboBox, StandardIlluminant illuminant)
        {
            foreach(ComboBoxItem item in comboBox.Items)
            {
                if ((StandardIlluminant)item.Tag == illuminant)
                {
                    comboBox.SelectedItem = item;
                    break;
                }
            }
        }
        void SelectObserver(ComboBox comboBox, StandardObserver observer)
        {
            foreach(ComboBoxItem item in comboBox.Items)
            {
                if ((StandardObserver)item.Tag == observer)
                {
                    comboBox.SelectedItem = item;
                    break;
                }
            }
        }

        StandardIlluminant SelectedIlluminant(ComboBox comboBox)
        {
            if (comboBox.SelectedItem != null)
            {
                var item = (comboBox.SelectedItem as ComboBoxItem);
                return (StandardIlluminant)item.Tag;
            }
            return StandardIlluminant.Unknown;
        }

        StandardObserver SelectedObserver(ComboBox comboBox)
        {
            if (comboBox.SelectedItem != null)
            {
                var item = (comboBox.SelectedItem as ComboBoxItem);
                return (StandardObserver)item.Tag;
            }
            return StandardObserver.Unknown;
        }

        private void OkClick(object sender, RoutedEventArgs e)
        {
            var setting = Setting;
            if (setting.Primary == setting.Secondary || setting.Primary == setting.Tertiary ||
                (setting.Secondary.IsValid && setting.Secondary == setting.Tertiary))
            {
                MessageBox.Show(S._("ObserverAndIlluminantSettingMustBeDeference"),
                    Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }


            DialogResult = true;
        }

        private void SecondaryIlluminant_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender == SecondaryObserver && SelectedObserver(SecondaryObserver) == StandardObserver.Unknown)
            {
                if (SelectedIlluminant(SecondaryIlluminant) != StandardIlluminant.Unknown)
                    SelectIlluminant(SecondaryIlluminant, StandardIlluminant.Unknown);
                
            }
            else if (sender == SecondaryIlluminant && SelectedIlluminant(SecondaryIlluminant) == StandardIlluminant.Unknown)
            {
                if (SelectedObserver(SecondaryObserver) != StandardObserver.Unknown)
                    SelectObserver(SecondaryObserver, StandardObserver.Unknown);
            }

            if (SelectedObserver(SecondaryObserver) == StandardObserver.Unknown ||
                SelectedIlluminant(SecondaryIlluminant) == StandardIlluminant.Unknown)
            {
                TertiaryObserver.IsEnabled = false;
                TertiaryIlluminant.IsEnabled = false;
                SelectObserver(TertiaryObserver, StandardObserver.Unknown);
                SelectIlluminant(TertiaryIlluminant, StandardIlluminant.Unknown);
            }
            else
            {
                TertiaryObserver.IsEnabled = true;
                TertiaryIlluminant.IsEnabled = true;
            }
        }

        private void TertiaryIlluminant_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender == TertiaryObserver && SelectedObserver(TertiaryObserver) == StandardObserver.Unknown)
            {
                if (SelectedIlluminant(TertiaryIlluminant) != StandardIlluminant.Unknown)
                    SelectIlluminant(TertiaryIlluminant, StandardIlluminant.Unknown);
                
            }
            else if (sender == TertiaryObserver && SelectedIlluminant(TertiaryIlluminant) == StandardIlluminant.Unknown)
            {
                if (SelectedObserver(TertiaryObserver) != StandardObserver.Unknown)
                    SelectObserver(TertiaryObserver, StandardObserver.Unknown);
            }
        }
    }
}
