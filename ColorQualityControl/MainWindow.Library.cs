using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using ThreeNH.ColorLibrary;
using ThreeNH.ColorQualityControl.Annotations;
using ThreeNH.ColorQualityControl.UI;
using ThreeNH.DataModel;
using ToastNotificationMsg;
using WinForms = System.Windows.Forms;

namespace ThreeNH.ColorQualityControl
{
    public partial class MainWindow
    {
        /// <summary>
        /// 重置存样库菜单项
        /// </summary>
        private void ResetLibrarySubmenu()
        {
            int index = LibraryManagementMenuItem.Items.IndexOf(LibrarySeparator);
            while (LibraryManagementMenuItem.Items.Count > index + 1)
            {
                LibraryManagementMenuItem.Items.RemoveAt(index + 1);
            }

            LibrarySeparator.Visibility = Visibility.Collapsed;
        }

        private MenuItem[] GetAllLibraryMenuItems()
        {
            List<MenuItem> items = new List<MenuItem>();
            int index = LibraryManagementMenuItem.Items.IndexOf(LibrarySeparator);
            for (int i = index + 1; i < LibraryManagementMenuItem.Items.Count; i++)
            {
                items.Add((MenuItem)LibraryManagementMenuItem.Items[i]);
            }

            return items.ToArray();
        }

        private MenuItem[] CreateLibraryMenuItems([NotNull] RoutedEventHandler clickEventHandler)
        {
            List<MenuItem> list = new List<MenuItem>();

            foreach (var libraryName in ColorLibraryInfo.GetLibraryNames())
            {
                MenuItem menuItem = new MenuItem();
                menuItem.HeaderTemplate = (DataTemplate)FindResource("LibraryMenuItemHeaderTemplate");
                menuItem.Header = S._(libraryName);
                menuItem.Tag = libraryName;
                menuItem.Click += clickEventHandler;

                list.Add(menuItem);
            }

            return list.ToArray();
        }

        /// <summary>
        /// 将所有存样库的名称显示在存样库管理菜单下
        /// </summary>
        private void InitLibrarySubmenu()
        {
            // 获取存样库目录下的所有存样库
            if (!Directory.Exists(App.LibraryPath))
            {
                try
                {
                    Directory.CreateDirectory(App.LibraryPath);
                }
                catch (IOException ex)
                {
                    MessageBox.Show(ex.Message, Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                catch (UnauthorizedAccessException ex)
                {
                    MessageBox.Show(ex.Message, Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }

            var items = CreateLibraryMenuItems(OnSetAsCurrentLibrary);
            if (items != null && items.Length > 0)
            {
                int selectedIndex = Array.FindIndex(items, item => (string)item.Tag == ColorLibraryInfo.LastUsedLibrary);
                if (selectedIndex < 0)
                    selectedIndex = 0;

                for (int i = 0; i < items.Length; i++)
                {
                    var item = items[i];
                    LibraryManagementMenuItem.Items.Add(item);
                    item.IsChecked = i == selectedIndex;
                    if(item.IsChecked)
                    {
                        ColorLibraryInfo.LastUsedLibrary = (string)item.Tag;
                    }
                }
                LibrarySeparator.Visibility = Visibility.Visible;
            }
            else
            {
                LibrarySeparator.Visibility = Visibility.Collapsed;
            }
        }

        private void OnSetAsCurrentLibrary(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            if (menuItem == null)
                return;

            foreach (var item in GetAllLibraryMenuItems())
            {
                item.IsChecked = item == menuItem;
                ColorLibraryInfo.LastUsedLibrary = (string)menuItem.Tag;
            }
        }

        private void RefreshLibrarySubmenu()
        {
            ResetLibrarySubmenu();
            InitLibrarySubmenu();
        }

        private void AddToLibrary(string libraryName, StandardSpecimen[] standards)
            => _library.TryAddToColorLibrary(libraryName, standards);

        private void AddToLibraryAsync(string libraryName, StandardSpecimen[] standards)
            => _library.AddToColorLibraryAsync(libraryName, standards);

        private void OnManageLibrary(object sender, ExecutedRoutedEventArgs e)
        {
            ColorLibraryManagementWindow dlg = new ColorLibraryManagementWindow();
            dlg.Owner = this;
            dlg.ShowDialog();
            RefreshLibrarySubmenu();
        }

        private void OnAddStandardToLibrary(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            if (menuItem != null)
            {
                string libraryPath = menuItem.Tag as string;

                AddToLibrary(libraryPath, GetSelectedStandards(e.Source));
            }
        }

        private void StandardContextMenu_OnOpened(object sender, RoutedEventArgs e)
        {
            ContextMenu contextMenu = sender as ContextMenu;
            if (contextMenu != null)
            {
                MenuItem menuItem = null;
                foreach (var item in contextMenu.Items)
                {
                    if (item is MenuItem && ((MenuItem)item).Name == "AddToLibraryMenuItem")
                    {
                        menuItem = item as MenuItem;
                        break;
                    }
                }

                if (menuItem != null)
                {
                    //menuItem.Items.Clear();

                    //var items = CreateLibraryMenuItems(OnAddStandardToLibrary);
                    //if (items != null && items.Length > 0)
                    //{
                    //    foreach (var item in items)
                    //    {
                    //        menuItem.Items.Add(item);
                    //    }
                    //    menuItem.Visibility = Visibility.Visible;
                    //}
                    //else
                    //{
                    //    menuItem.Visibility = Visibility.Collapsed;
                    //}
                    menuItem.Header = S._("MenuItem_AddToLibrary") + $"({ColorLibraryInfo.LastUsedLibrary})";
                }
            }
        }

        private void OnSpecifyLibraryPath(object sender, RoutedEventArgs e)
        {
            var dialog = new WinForms.FolderBrowserDialog();
            dialog.SelectedPath = App.LibraryPath;
            if (dialog.ShowDialog() == WinForms.DialogResult.OK)
            {
                App.LibraryPath = dialog.SelectedPath;
                RefreshLibrarySubmenu();
            }
        }

        private void LibrarySearch_OnClick(object sender, RoutedEventArgs e)
        {
            ColorMatchWindow dlg = new ColorMatchWindow(Database);
            dlg.Owner = this;
            dlg.ShowDialog();
        }

        private void LibraryEdit_OnClick(object sender, RoutedEventArgs e)
        {
            LibraryColorInputWindow dlg = new LibraryColorInputWindow(Colorimeter);
            dlg.Owner = this;
            dlg.ShowDialog();
        }

        public bool AutoSaveStandardToLibrary
        {
            get => SystemDefaultConfig.AutoSaveStandardToLibrary;
            set
            {
                SystemDefaultConfig.AutoSaveStandardToLibrary = value;
                NotifyPropertyChanged(nameof(AutoSaveStandardToLibrary));
            }
        }

        private void OnAddToLibrary(object sender, ExecutedRoutedEventArgs e)
        {
            AddToLibrary(ColorLibraryInfo.LastUsedLibrary, GetSelectedStandards(e.Source));
        }

        private void CanAddToLibrary(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !string.IsNullOrWhiteSpace(ColorLibraryInfo.LastUsedLibrary);
        }

        private void OnFindClosedColorsFromLibrary(object sender, ExecutedRoutedEventArgs e)
        {
            var standards = GetSelectedStandards(e.Source);
            if (standards.Length != 1)
                return;

            var targetLab = standards[0].GetColorData(SpecularComponent)?.Lab(Formula);
            if (targetLab == null)
                return;

            var limits = SystemDefaultConfig.AutoSelectStandardLimits;
            var sc = SpecularComponent;
            var formula = Formula;

            Func<ChromaticityField, double, bool> isPassed = (field, d) =>
                !limits[field].UseForJudgement || (d <= (double)limits[field].Upper);

            try
            {
                using (var db = new ColorDatabase(
                    ColorLibraryInfo.GetDbConnectionString(ColorLibraryInfo.LastUsedLibrary),
                    false))
                {
                    var matchedColors = db.SelectAllColors(x =>
                    {
                        var clr = x.GetColorData(SpecularComponent);
                        var lab = clr?.Lab(formula);
                        if (lab != null)
                        {
                            double dL = Math.Abs(lab.L - targetLab.L);
                            double dA = Math.Abs(lab.a - targetLab.a);
                            double dB = Math.Abs(lab.b - targetLab.b);
                            double dE = Math.Sqrt(dL * dL + dA * dA + dB * dB);

                            return (isPassed(ChromaticityField.dL, dL) &&
                                    isPassed(ChromaticityField.da, dA) &&
                                    isPassed(ChromaticityField.db, dB) &&
                                    isPassed(ChromaticityField.dEab, dE));
                        }

                        return false;
                    });

                    if (matchedColors != null && matchedColors.Count > 0)
                    {
                        if (IsShowAllStandards)
                        {
                            ShowSingleStandard(standards[0]);
                        }

                        ImportTests(matchedColors.Select(x => new TestSpecimen(x)).ToArray(), standards[0], true);
                    }
                    else
                    {
                        MessageBox.Show(S._("CannotFindMatchedColor"), S._("MenuItem_FindClosedColorsFromLibrary"));
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, S._("MenuItem_FindClosedColorsFromLibrary"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CanFindClosedColorsFromLibrary(object sender, CanExecuteRoutedEventArgs e)
        {
            var standards = GetSelectedStandards(e.Source);

            e.CanExecute = !string.IsNullOrWhiteSpace(ColorLibraryInfo.LastUsedLibrary) && standards.Length == 1 &&
                standards[0].GetColorData(SpecularComponent) != null;
        }
    }
}