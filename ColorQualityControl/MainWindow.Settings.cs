using System;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using ColorQualityControl.Utils;
using ThreeNH.ColorQualityControl.UI;
using ThreeNH.Controls;
using ThreeNH.Helper;
using ThreeNH.Helper.Plugin.Controls;

namespace ThreeNH.ColorQualityControl
{
    /// <summary>
    /// 语言/关于/更新/手册等应用级 UI 编排（阶段4-T02 自 MainWindow.cs 迁出）。
    /// </summary>
    public partial class MainWindow
    {
        /// <summary>初始化程序信息（标题与图标）。</summary>
        private void InitializeApplicationInformation()
        {
            RefreshTitle();
            Uri logo_uri = new Uri(App.GetAppPath() + "\\logo.ico", UriKind.Absolute);
            try
            {
                Icon = new BitmapImage(logo_uri);
            }
            catch (IOException)
            {
                Icon = new BitmapImage(new Uri("pack://Application:,,,/Images/logo.ico"));
            }
        }

        /// <summary>初始化语言菜单。</summary>
        private void InitLanguageMenuItem()
        {
            var uris = LocalUtil.FindLangUrisFromAssembly(Assembly.GetExecutingAssembly())?.OrderBy(x => x.Key.Name);
            foreach (var uri in uris)
            {
                var cl = uri.Key;
                MenuItem item = new MenuItem();
                switch (cl.Name)
                {
                    case "zh-CN":
                        item.Header = "中文简体";
                        break;
                    case "zh-TW":
                        item.Header = "中文繁體";
                        break;
                    case "ru":
                        item.Header = "Русский";
                        break;
                    case "ko-KR":
                        item.Header = "한국어";
                        break;
                    case "zh-Hans":
                        continue;
                    default:
                        item.Header = cl.NativeName;
                        break;
                }
                item.CommandParameter = cl;
                item.Click += OnLanguageClick;
                item.IsChecked = cl.Equals(CultureInfo.CurrentCulture);
                LanguagesMenuItem.Items.Add(item);
            }
        }

        private void OnLanguageClick(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            if (menuItem != null && menuItem.CommandParameter is CultureInfo)
            {
                CultureInfo ci = menuItem.CommandParameter as CultureInfo;

                Configuration config =
                 ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

                AppSettingsSection appSettings = config.AppSettings;
                if (appSettings.Settings.AllKeys.Contains("Culture"))
                {
                    appSettings.Settings["Culture"].Value = ci.Name;
                }
                else
                {
                    appSettings.Settings.Add("Culture", ci.Name);
                }
                config.Save(ConfigurationSaveMode.Modified);

                var result = MessageBox.Show(_("LanugageChangedYouNeedRestart"),
                    App.Title, MessageBoxButton.OKCancel, MessageBoxImage.Question);
                if (result == MessageBoxResult.OK)
                {
                    if (!ConfirmSave())
                        return;
                    Database.IsModified = false;
                    App.Restart();
                }
            }
        }

        private void AboutExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            AboutDialog dlg = new AboutDialog();
            dlg.Owner = this;
            dlg.ShowDialog();
        }

        private async void UpdateExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            string url = SystemDefaultConfig.ReadTestUrl() ?? "https://cuali.3n88.com:7000";
            AppUpdaterManager _updater = new AppUpdaterManager(url, SystemDefaultConfig.AppInfoId);

            var currentVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();

            var result = await _updater.CheckUpdateAsync(currentVersion);
            UpdateMessageBoxViewModel viewModel = new UpdateMessageBoxViewModel();
            // 检查更新返回内容为空失败
            if (result.UpdateInfo == null)
            {
                viewModel.Title = S._("SystemException");
                viewModel.YesButtonText = S._("Label_OK");
                viewModel.DownloadPrompt = S._("ParsedBadCommand");
                viewModel.ErrorFlag = true;
                UpdateMessageBox.ShowUpdateDialog(result.UpdateInfo, currentVersion, viewModel);
                return;
            }
            if (!result.HasUpdate)
            {
                viewModel.Title = S._("Label_Update");
                viewModel.YesButtonText = S._("Label_OK");
                viewModel.DownloadPrompt = S._("NoUpdates");
                UpdateMessageBox.ShowUpdateDialog(result.UpdateInfo, currentVersion, viewModel);
                return;
            }

            viewModel.Title = S._("Label_Update");
            viewModel.NoButtonText = S._("Label_Cancel");
            viewModel.YesButtonText = S._("Label_OK");
            viewModel.CurrentVersionText = (S._("CurrentVersionText") + currentVersion);
            viewModel.FileSizeLabel = S._("FileSizeLabel");
            viewModel.DownloadPrompt = S._("UpdateConfirmMessage");
            viewModel.NewVersionText = (S._("NewVersionAvailable") + result.UpdateInfo.Version);

            bool shouldDownload = UpdateMessageBox.ShowUpdateDialog(result.UpdateInfo, currentVersion, viewModel);

            if (shouldDownload)
            {
                try
                {
                    LoadingManageParams loadingParams = new LoadingManageParams
                    {
                        DownloadCaption = S._("Label_Update"),
                        DownloadMessage = S._("DownloadingUpdatePleaseWait"),
                        DownloadCancelText = S._("Label_Cancel")
                    };
                    await _updater.DownloadAndInstallAsync(result.UpdateInfo, loadingParams);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"更新失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void OpenUserManualExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (File.Exists(App.GetUserManualPath()))
            {
                Process.Start(App.GetUserManualPath());
            }
        }

        private void OpenUserManualCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = File.Exists(App.GetUserManualPath());
        }
    }
}
