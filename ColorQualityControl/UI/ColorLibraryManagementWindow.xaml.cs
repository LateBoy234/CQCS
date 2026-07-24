using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.VisualBasic.FileIO;
using Microsoft.Win32;
using PanTongColorMatchSearch;
using ThreeNH.ColorLibrary;
using ThreeNH.ColorQualityControl.Annotations;
using ThreeNH.ColorSpecimen;
using ThreeNH.DataModel;
using ThreeNH.Optical;
using Path = System.Windows.Shapes.Path;

namespace ThreeNH.ColorQualityControl.UI
{
    /// <summary>
    /// ColorLibraryManagementWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ColorLibraryManagementWindow : Window, INotifyPropertyChanged
    {
        private ColorDatabase _db;
        private string _currentLibraryName;
        private DataView _colorsView;

        public ColorLibraryManagementWindow()
        {
            LoadLibraryNames();
            InitializeComponent();
            DataContext = this;
        }

        public DataView ColorsView
        {
            get => _colorsView;
            private set
            {
                if (Equals(value, _colorsView)) return;
                _colorsView = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<string> LibraryNames { get; } = new ObservableCollection<string>();
        public string CurrentLibraryName
        {
            get => _currentLibraryName;
            set
            {
                if (value == _currentLibraryName) return;
                _currentLibraryName = value;
                OnPropertyChanged();
            }
        }
        private void OnSelectedLibraryChanged(object sender, SelectionChangedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Wait;
            if (e.AddedItems.Count > 0)
            {
                var libraryName = (string)e.AddedItems[0];
                _db?.Dispose();
                _db = new ColorDatabase(ColorLibraryInfo.GetDbConnectionString(libraryName));
                _db.UpdateColorsLab(null, StandardIlluminant.D65, StandardObserver.CIE1964);
                ColorsView = new DataView(_db.Colors);
            }
            Mouse.OverrideCursor = Cursors.Arrow;
        }

        public void LoadLibraryNames()
        {
            LibraryNames.Clear();
            foreach (var libraryName in ColorLibraryInfo.GetLibraryNames())
            {
                LibraryNames.Add(libraryName);
            }
        }

        private RelayCommand _QueryCommand;

        public RelayCommand QueryCommand
        {
            get
            {
                if (_QueryCommand == null)
                    _QueryCommand = new RelayCommand(o => QueryLibrary()); ;
                return _QueryCommand;
            }
            set
            {
                _QueryCommand = value;
                OnPropertyChanged(nameof(QueryCommand));
            }
        }

        private DateTime? _BeginDate;
        public DateTime? BeginDate
        {
            get => _BeginDate;
            set
            {
                _BeginDate = value;
                OnPropertyChanged(nameof(BeginDate));
            }
        }

        private DateTime? _EndDate;
        public DateTime? EndDate
        {
            get => _EndDate;
            set
            {
                _EndDate = value;
                OnPropertyChanged(nameof(EndDate));
            }
        }

        private string _QueryName;
        public string QueryName
        {
            get => _QueryName;
            set
            {
                _QueryName = value;
                OnPropertyChanged(nameof(QueryName));
            }
        }
        private void QueryLibrary()
        {
            Mouse.OverrideCursor = Cursors.Wait;
            if (!string.IsNullOrEmpty(CurrentLibraryName))
            {
                try
                {
                    var libraryName = CurrentLibraryName;
                    _db?.Dispose();
                    _db = new ColorDatabase(ColorLibraryInfo.GetDbConnectionString(libraryName));
                    _db.UpdateColorsLab(null, StandardIlluminant.D65, StandardObserver.CIE1964);
                    var table = _db.Find(QueryName, BeginDate, EndDate,true);
                    ColorsView = new DataView(table);
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            Mouse.OverrideCursor = Cursors.Arrow;
        }

        private void AddLibrary_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            GetInputDialog dialog = new GetInputDialog();
            dialog.Owner = this;
            dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            dialog.Prompt = S._("Tip_PleaseEnterName");
            if (dialog.ShowDialog().GetValueOrDefault())
            {
                if (!ColorLibraryInfo.IsNameValid(dialog.Input))
                {
                    MessageBox.Show(S._("LibNameContInvalChars"), S._("Title_NewLibrary"), MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                var libraryName = dialog.Input;
                string path = ColorLibraryInfo.GetLibraryFilePath(libraryName);
                if (File.Exists(path))
                {
                    MessageBox.Show(S._("LibAlreadExist"), S._("Title_NewLibrary"), MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                LibraryNames.Add(libraryName);
                CurrentLibraryName = libraryName;
                MessageBox.Show(S._("CreateLibSuccess"));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void ImportLibrary_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Title = S._("Title_ImportLibrary");
            dialog.Filter = S._("LibraryFileFilter");
            if (!dialog.ShowDialog().GetValueOrDefault())
            {
                return;
            }

            Mouse.OverrideCursor = Cursors.Wait;
            SpecimenDatabase specimenDatabase = new SpecimenDatabase();
            switch (System.IO.Path.GetExtension(dialog.FileName).ToLower())
            {
                case ".xml":
                    try
                    {
                        specimenDatabase.Load(dialog.FileName);
                    }
                    catch (Exception exception)
                    {
                        MessageBox.Show($"{S._("LoadFileFailed")}: {exception.Message}", S._("Title_ImportLibrary"), MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        return;
                    }

                    break;
                case ".qtx":
                    try
                    {
                        QTXHelper.Load(dialog.FileName, specimenDatabase);
                    }
                    catch (Exception exception)
                    {
                        MessageBox.Show($"{S._("LoadFileFailed")}: {exception.Message}", S._("Title_ImportLibrary"), MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        return;
                    }
                    break;

                case ".xlsx":
                    {
                        Mouse.OverrideCursor = Cursors.Arrow;
                        ImportStandardsDialog importStandardsDialog = new ImportStandardsDialog(dialog.FileName);
                        importStandardsDialog.Owner = this;
                        if (!importStandardsDialog.ShowDialog().GetValueOrDefault())
                        {
                            return;
                        }

                        foreach (var specimen in importStandardsDialog.StandardSpecimens)
                        {
                            specimenDatabase.AddStandard(specimen);
                        }
                    }
                    break;

                default:
                    MessageBox.Show(S._("FileTypeNotSupported"), S._("Title_ImportLibrary"), MessageBoxButton.OK);
                    return;
            }

            Mouse.OverrideCursor = Cursors.Arrow;

            ImportLibraryDialog importLibraryDialog = new ImportLibraryDialog(_db, CurrentLibraryName, specimenDatabase);
            importLibraryDialog.Owner = this;
            importLibraryDialog.CanAddLibrary = false;
            if (importLibraryDialog.ShowDialog().GetValueOrDefault())
            {

            }
        }

        private void DelLibrary_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (CurrentLibraryName != null)
            {
                var ret = MessageBox.Show(
                    string.Format(S._("Format_ConfirmDeleteLibrary_0"), CurrentLibraryName),
                    //$"存样库删除后不能恢复，确定要删除\"{SelectedLibrary.Name}\"吗？",
                    //$"删除存样库[{SelectedLibrary.Name}]", 
                    string.Format(S._("Format_DeleteLibrary_0"), CurrentLibraryName),
                    MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                if (ret == MessageBoxResult.OK)
                {
                    var path = ColorLibraryInfo.GetLibraryFilePath(CurrentLibraryName);
                    ColorsView = null;
                    _db.Dispose();
                    _db = null;
                    LibraryNames.Remove(CurrentLibraryName);
                    ColorLibraryInfo.LastUsedLibrary = LibraryNames.FirstOrDefault();

                    Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(
                        path,
                        Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                }
            }
        }

        private void CanOperateOnLibrary(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = CurrentLibraryName != null;
        }

        private void RenameLibrary_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (CurrentLibraryName == null)
            {
                return;
            }

            GetInputDialog dialog = new GetInputDialog
            {
                Owner = this,
                Title = S._("Title_Rename"),
                Prompt = S._("Tip_PleaseEnterANewName"),
                Input = CurrentLibraryName
            };
            if (dialog.ShowDialog().GetValueOrDefault())
            {
                if (!ColorLibraryInfo.IsNameValid(dialog.Input))
                {
                    MessageBox.Show(S._("LibNameContInvalChars"), S._("Title_Rename"), MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (File.Exists(ColorLibraryInfo.GetLibraryFilePath(dialog.Input)))
                {
                    MessageBox.Show(S._("LibraryNameConflicts"), S._("Title_Rename"), MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var oldPath = ColorLibraryInfo.GetLibraryFilePath(CurrentLibraryName);

                ColorsView = null;
                _db.Dispose();
                _db = null;
                LibraryNames.Remove(CurrentLibraryName);
                File.Move(oldPath, ColorLibraryInfo.GetLibraryFilePath(dialog.Input));
                LibraryNames.Add(dialog.Input);
                CurrentLibraryName = dialog.Input;
            }
        }

        private void OnDeleteSelectedStandards(object sender, ExecutedRoutedEventArgs e)
        {
            if (MessageBox.Show(S._("PromptConfirmDeleteSampleInLibrary"),
                    Title, MessageBoxButton.OKCancel, MessageBoxImage.Warning) == MessageBoxResult.OK)
            {
                foreach (DataRowView rowView in DataGrid.SelectedItems.OfType<DataRowView>().ToArray())
                {
                    rowView.Row.Delete();
                }
                _db.Update(_db.Colors);
            }
        }


        private void OnModifyStandard(object sender, ExecutedRoutedEventArgs e)
        {

            if (DataGrid.SelectedItem == null)
            {
                return;
            }
            MainWindow mainWindow = (MainWindow)App.Current.MainWindow;
            StandardInputDialog dialog = new StandardInputDialog(mainWindow.Colorimeter);
            dialog.Owner = this;
            var rowView = (DataRowView)DataGrid.SelectedItem;
            dialog.Specimen = ColorDatabase.ToSpecimen(rowView.Row);
            dialog.Title = S._("Title_ModifyLibrarySample");
            if (dialog.ShowDialog().GetValueOrDefault())
            {
                try
                {
                    ColorDatabase.UpdateColorRow(rowView.Row, dialog.Specimen,true);
                    List<Specimen> specimens = new List<Specimen>();
                    specimens.Add(dialog.Specimen);
                    _db.UpdateColors(specimens);
                     // ColorsView = new DataView(_db.Colors);
                    //_db.Update(_db.Colors);
                }
                catch (Exception ex)
                {
                    rowView.Row.RejectChanges();
                    MessageBox.Show(ex.Message, S._("Title_ModifyLibrarySample"), MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void OnInputStandard(object sender, ExecutedRoutedEventArgs e)
        {
            MainWindow mainWindow = (MainWindow)App.Current.MainWindow;
            StandardInputDialog dialog = new StandardInputDialog(mainWindow.Colorimeter);
            dialog.Owner = this;
            dialog.Title = S._("Title_StandardInput");
            if (dialog.ShowDialog().GetValueOrDefault())
            {
                try
                {
                    var row = _db.ToColorRow(dialog.Specimen);
                    _db.Colors.Rows.Add(row);
                    _db.Update(_db.Colors);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, S._("Title_StandardInput"), MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void CanRenameStandardExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = DataGrid.SelectedItems.Count == 1;
        }

        private void CanDeleteStandardsExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = DataGrid.SelectedItems.Count > 0;
        }

        protected override void OnClosed(EventArgs e)
        {
            _db?.Dispose();
            base.OnClosed(e);
        }

        private void OnImportIntoStandardList(object sender, ExecutedRoutedEventArgs e)
        {
            if (DataGrid.SelectedItems.Count == 0)
                return;

            var standards = from row in DataGrid.SelectedItems.OfType<DataRowView>()
                            select new StandardSpecimen(ColorDatabase.ToSpecimen(row.Row));
            ((MainWindow)App.Current.MainWindow).ImportStandards(standards,isRemoveSame:false);
        }

        private void CanImportIntoStandardList(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = DataGrid.SelectedItems.Count > 0;
        }

  
    }
}
