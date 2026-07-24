using System;
using System.Windows;
using ThreeNH.ColorQualityControl.Models;
using ThreeNH.ColorQualityControl.Utils;

namespace ThreeNH.ColorQualityControl.UI
{
    public partial class CloudSettingDialog : Window
    {
        public CloudSettingDialog()
        {
            InitializeComponent();
            LoadConfig(CloudConfigStore.Load());
        }

        public CloudConfig Config { get; private set; }

        private void LoadConfig(CloudConfig config)
        {
            Config = config ?? new CloudConfig();
            EnabledCheckBox.IsChecked = Config.Enabled;
            ApiBaseUrlTextBox.Text = Config.ApiBaseUrl ?? string.Empty;
            UploadPathTextBox.Text = string.IsNullOrWhiteSpace(Config.UploadPath)
                ? "/api/measure/upload"
                : Config.UploadPath;
            ApiTokenBox.Password = Config.ApiToken ?? string.Empty;
            TimeoutTextBox.Text = Config.TimeoutSeconds.ToString();
        }

        private void OnOkClick(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(TimeoutTextBox.Text.Trim(), out var timeout) || timeout < 5)
            {
                MessageBox.Show(S._("CloudTimeoutInvalid"), Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Config = new CloudConfig
            {
                Enabled = EnabledCheckBox.IsChecked == true,
                ApiBaseUrl = ApiBaseUrlTextBox.Text.Trim(),
                UploadPath = string.IsNullOrWhiteSpace(UploadPathTextBox.Text)
                    ? "/api/measure/upload"
                    : UploadPathTextBox.Text.Trim(),
                ApiToken = ApiTokenBox.Password,
                TimeoutSeconds = timeout
            };

            if (Config.Enabled && string.IsNullOrWhiteSpace(Config.ApiBaseUrl))
            {
                MessageBox.Show(S._("CloudUploadUrlMissing"), Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            CloudConfigStore.Save(Config);
            DialogResult = true;
        }
    }
}
