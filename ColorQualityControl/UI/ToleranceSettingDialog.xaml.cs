using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ThreeNH.DataModel;

namespace ThreeNH.ColorQualityControl.UI
{

    /// <summary>
    /// ToleranceSettingDialog.xaml 的交互逻辑
    /// </summary>
    public partial class ToleranceSettingDialog : Window
    {
        ToleranceSetting originalToleranceSetting;
        //ToleranceSetting

        public event EventHandler<ValueEventArg<bool>> ChangeAccepted;

        public ToleranceSettingDialog(ChromaticitySetting setting)
        {
            InitializeComponent();
            PrimaryToleranceControl.ValidItems = setting.ColorDiffFields;
            SecondaryToleranceControl.ValidItems = setting.ColorDiffFields;
            TertiaryToleranceControl.ValidItems = setting.ColorDiffFields;
        }

        private void ToleranceIsModifiedChanged(object sender, EventArgs e)
        {
            if (AcceptButton != null)
                RestoreButton.IsEnabled = AcceptButton.IsEnabled = IsModified;
        }

        public ToleranceSetting ToleranceSetting
        {
            set
            {
                originalToleranceSetting = value;

                if (value != null)
                {
                    PrimaryToleranceControl.Tolerance = value.PrimaryTolerance ?? new Tolerance();
                    SecondaryToleranceControl.Tolerance = value.SecondaryTolerance ?? new Tolerance();
                    TertiaryToleranceControl.Tolerance = value.TertiaryTolerance ?? new Tolerance();
                    if (value is SystemToleranceSetting)
                    {
                        IsFromSystemDefault = (value as SystemToleranceSetting).IsFromSystemDefault;
                    }
                    else
                    {
                        ToleranceFromGroupBox.Visibility = Visibility.Collapsed;
                    }
                }
            }
            get
            {
                return originalToleranceSetting;
            }
        }

        public bool IsFromSystemDefault
        {
            get
            {
                if (FromSystemDefault != null)
                    return FromSystemDefault.IsChecked.GetValueOrDefault();
                return false;
            }
            set
            {
                if (FromSystemDefault == null || FromStandard == null)
                    return;
                if (value)
                {
                    FromSystemDefault.IsChecked = true;
                }
                else
                {
                    FromStandard.IsChecked = true;
                }
            }
        }

        public bool IsModified
        {
            get
            {
                if (originalToleranceSetting == null)
                    return false;
                bool isModified = PrimaryToleranceControl.IsModified ||
                    SecondaryToleranceControl.IsModified ||
                    TertiaryToleranceControl.IsModified;
                if (originalToleranceSetting is SystemToleranceSetting)
                {
                    isModified = isModified ||  IsFromSystemDefault != 
                        (originalToleranceSetting as SystemToleranceSetting).IsFromSystemDefault;
                }
                return isModified;
            }
        }

        private bool VerifyData()
        {
            var curTolCtrl = (tabControl.SelectedItem as TabItem).Content as ToleranceControl;
            return curTolCtrl.VerifyData();
        }

        private void AcceptButton_Click(object sender, RoutedEventArgs e)
        {
            if (!VerifyData())
                return;
            bool isToleranceFromChanged = false;
            if (originalToleranceSetting != null)
            {
                PrimaryToleranceControl.AcceptChange();
                SecondaryToleranceControl.AcceptChange();
                TertiaryToleranceControl.AcceptChange();
                if (originalToleranceSetting is SystemToleranceSetting)
                {
                    var sysSetting = originalToleranceSetting as SystemToleranceSetting;
                    if (sysSetting.IsFromSystemDefault != IsFromSystemDefault)
                    {
                        sysSetting.IsFromSystemDefault = IsFromSystemDefault;
                        isToleranceFromChanged = true;
                    }
                }
            }

            AcceptButton.IsEnabled = RestoreButton.IsEnabled = false;
            OnChangeAccepted(isToleranceFromChanged);
        }

        private void OnChangeAccepted(bool isToleranceFromChanged)
        {
            ChangeAccepted?.Invoke(this, new ValueEventArg<bool> { Value = isToleranceFromChanged });
        }

        private void RestoreButton_Click(object sender, RoutedEventArgs e)
        {
            if (originalToleranceSetting != null)
            {
                if (PrimaryToleranceControl.IsModified)
                    PrimaryToleranceControl.Restore();
                if (SecondaryToleranceControl.IsModified)
                    SecondaryToleranceControl.Restore();
                if (TertiaryToleranceControl.IsModified)
                    TertiaryToleranceControl.Restore();

                if (originalToleranceSetting is SystemToleranceSetting)
                {
                    IsFromSystemDefault = (originalToleranceSetting as SystemToleranceSetting)
                        .IsFromSystemDefault;
                }
            }

            AcceptButton.IsEnabled = RestoreButton.IsEnabled = false;
        }

        private void OkClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (IsModified)
                {
                    MessageBoxResult result = MessageBox.Show(
                        S._("PromatSaveToleranceChanges"), Title, MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes)
                    {
                        if (!VerifyData())
                            return;

                        AcceptButton_Click(this, new RoutedEventArgs());
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.TraceInformation("保存容差值设置异常，原因："+ ex);
            }
           

            DialogResult = true;
        }

        private void TabItem_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Left || e.Key == Key.Right)
            {
                var toleranceControl = (tabControl.SelectedItem as TabItem).Content as ToleranceControl;
                if (!toleranceControl.VerifyData())
                    e.Handled = true;
            }
        }

        private void TabItem_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            TabItem item = sender as TabItem;
            if (item != tabControl.SelectedItem)
            {
                var toleranceControl = (tabControl.SelectedItem as TabItem).Content as ToleranceControl;
                if (!toleranceControl.VerifyData())
                    e.Handled = true;
            }
        }
    }
}
