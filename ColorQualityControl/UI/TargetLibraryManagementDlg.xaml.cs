using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ClosedXML.Excel;
using ThreeNH.ColorAlgorithm;
using ThreeNH.ColorQualityControl.Adapter;
using ThreeNH.DataModel;
using ThreeNH.Optical;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.FileIO;
using Microsoft.Win32;
using ThreeNH.ColorQualityControl.Annotations;
using ThreeNH.ColorSpecimen;
using SearchOption = System.IO.SearchOption;

namespace ThreeNH.ColorQualityControl.UI
{
    [ValueConversion(typeof(int), typeof(Visibility))]
    public class FilterMethodToVisibleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
            {
                return null;
            }

            int tmp;
            if (int.TryParse((string) parameter, out tmp))
            {
                if (tmp == (int) value)
                {
                    return Visibility.Visible;
                }
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class StandardAdapterFilter
    {
        public SpecularComponent SpecularComponent { get; set; } = SpecularComponent.SCIAndSCE;

        public DateTime DateBegin { get; set; } = new DateTime(2000, 1, 1);

        public DateTime DateEnd { get; set; } = DateTime.Now;

        public string Name { get; set; }

        public double L { get; set; }

        public double a { get; set; }

        public double b { get; set; }

        public double DE { get; set; } = 1.0;

        private bool IsSpecularComponentOk(SpecimenAdapter adapter)
        {
            return (SpecularComponent == SpecularComponent.SCIAndSCE ||
                    adapter.SpecularComponent == SpecularComponent);
        }
        public bool FilterByName(Object item)
        {
            
            SpecimenAdapter adapter = item as SpecimenAdapter;
            if (string.IsNullOrWhiteSpace(Name))
            {
                return IsSpecularComponentOk(adapter);
            }

            if (adapter != null)
            {
                return IsSpecularComponentOk(adapter) &&
                       string.Compare(Name, adapter.Name, StringComparison.OrdinalIgnoreCase) == 0;
            }

            return true;
        }

        public bool FilterByDate(Object item)
        {
            SpecimenAdapter adapter = item as SpecimenAdapter;
            if (adapter != null)
            {
                return IsSpecularComponentOk(adapter) && 
                       adapter.DateTime >= DateBegin && adapter.DateTime < DateEnd;
            }
            return true;
        }

        public bool FilterByLab(Object item)
        {
            SpecimenAdapter adapter = item as SpecimenAdapter;
            if (adapter.Lab == null)
            {
                return IsSpecularComponentOk(adapter);
            }


            var delta = new CIEDE1976(adapter.Lab, new CIELAB(L, a, b));
            return IsSpecularComponentOk(adapter) && delta.dE <= DE;
        }

        public bool FilterByNone(Object item)
        {
            SpecimenAdapter adapter = item as SpecimenAdapter;
            if (adapter != null)
            {
                return IsSpecularComponentOk(adapter);
            }

            return true;
        }
    }


    /// <summary>
    /// TargetLibraryManagementDlg.xaml 的交互逻辑
    /// </summary>
    public partial class TargetLibraryManagementDlg : Window, INotifyPropertyChanged
    {
        public static readonly DependencyProperty SelectedLibraryProperty =
            DependencyProperty.Register("SelectedLibrary", typeof(Pair), typeof(TargetLibraryManagementDlg));

        private ChromaticitySetting _displayFields = new ChromaticitySetting();
        private Formula _formula = new Formula();
        private StandardAdapterFilter _filter = new StandardAdapterFilter();

        public class Pair : INotifyPropertyChanged
        {

            public Pair(string name, TargetLibrary library=null)
            {
                Name = name;
                Library = library;
            }

            private string _name;
            public string Name
            {
                get { return _name;}
                set
                {
                    if (value != _name)
                    {
                        _name = value;
                        OnPropertyChanged(nameof(Name));
                    }
                }

            }
            public TargetLibrary Library { get; set; }
            public ObservableList<SpecimenAdapter> Adapters { get; set; }
            public event PropertyChangedEventHandler PropertyChanged;

            protected virtual void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public TargetLibraryManagementDlg()
        {
            foreach (var fileName in Directory.EnumerateFiles(App.LibraryPath, "*.db", SearchOption.TopDirectoryOnly))
            {
                Libraries.Add(new Pair(Path.GetFileNameWithoutExtension(fileName)));
            }
            //_displayFields.Copy(((App)Application.Current).SystemDefaultConfig.InstrumentDataDisplaySetting);
            _displayFields.SetDisplayFields(new ChromaticityField[]
            {
                ChromaticityField.Name,
                ChromaticityField.SpecularComponent,
                ChromaticityField.PseudoColor,
                ChromaticityField.DateTime,
                ChromaticityField.UV,
                ChromaticityField.L,
                ChromaticityField.a,
                ChromaticityField.b,
                ChromaticityField.Y,
                ChromaticityField.x,
                ChromaticityField.y,
                ChromaticityField.R400,
                ChromaticityField.R410,
                ChromaticityField.R420,
                ChromaticityField.R430,
                ChromaticityField.R440,
                ChromaticityField.R450,
                ChromaticityField.R460,
                ChromaticityField.R470,
                ChromaticityField.R480,
                ChromaticityField.R490,
                ChromaticityField.R500,
                ChromaticityField.R510,
                ChromaticityField.R520,
                ChromaticityField.R530,
                ChromaticityField.R540,
                ChromaticityField.R550,
                ChromaticityField.R560,
                ChromaticityField.R570,
                ChromaticityField.R580,
                ChromaticityField.R590,
                ChromaticityField.R600,
                ChromaticityField.R610,
                ChromaticityField.R620,
                ChromaticityField.R630,
                ChromaticityField.R640,
                ChromaticityField.R650,
                ChromaticityField.R660,
                ChromaticityField.R670,
                ChromaticityField.R680,
                ChromaticityField.R690,
                ChromaticityField.R700,
            });
            

            InitializeComponent();
            DataContext = this;

            SpecimenDataGrid.Fields = _displayFields.DisplayFields;
        }

        private Pair SelectedLibrary
        {
            get { return (Pair) GetValue(SelectedLibraryProperty); }
            set { SetValue(SelectedLibraryProperty, value);}
        }

        public StandardAdapterFilter Filter
        {
            get { return _filter; }
        }

        public ObservableCollection<Pair> Libraries { get; } = new ObservableCollection<Pair>();


        private void RefreshLibraryAdapters(Pair pair)
        {
            if (pair.Adapters == null)
            {
                pair.Adapters = new ObservableList<SpecimenAdapter>();
            }
            else
            {
                pair.Adapters.Clear();
            }

            foreach (var target in pair.Library.Targets.Value)
            {
                if (target.Value.SCI != null)
                {
                    SpecimenAdapter adapter =
                        new SpecimenAdapter(target.Value, _formula, _displayFields, SpecularComponent.SCI);
                    pair.Adapters.Add(adapter);
                }

                if (target.Value.SCE != null)
                {
                    SpecimenAdapter adapter =
                        new SpecimenAdapter(target.Value, _formula, _displayFields, SpecularComponent.SCE);
                    pair.Adapters.Add(adapter);
                }

                if (target.Value.Clr != null)
                {
                    SpecimenAdapter adapter =
                        new SpecimenAdapter(target.Value, _formula, _displayFields, SpecularComponent.Unknown);
                    pair.Adapters.Add(adapter);
                }
            }
        }

        private void OnSelectedLibraryChanged(object sender, SelectionChangedEventArgs e)
        {
            var listBox = sender as ListBox;
            if (listBox?.SelectedItem != null)
            {
                var pair = (Pair) listBox.SelectedItem;
                Mouse.OverrideCursor = Cursors.Wait;
                string libraryName = pair.Name;
                if (pair.Library == null)
                {
                    pair.Library = new TargetLibrary(Path.Combine(App.LibraryPath, $"{libraryName}.db"));
                    pair.Adapters = new ObservableList<SpecimenAdapter>();
                    RefreshLibraryAdapters(pair);
                }

                SpecimenDataGrid.ItemsSource = pair.Adapters;
                var view = CollectionViewSource.GetDefaultView(SpecimenDataGrid.DataGrid.ItemsSource);
                if (view != null)
                {
                    view.Filter = GetFilter();
                }

                Mouse.OverrideCursor = Cursors.Arrow;

                CommandManager.InvalidateRequerySuggested();
            }
            else
            {
                SpecimenDataGrid.ItemsSource = null;
            }
        }

        private void AddLibrary_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            GetInputDialog dialog = new GetInputDialog();
            dialog.Owner = this;
            dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            dialog.Prompt = S._("Tip_PleaseEnterName");
            if (dialog.ShowDialog().GetValueOrDefault())
            {
                if (!TargetLibrary.IsNameValid(dialog.Input))
                {
                    MessageBox.Show(S._("LibNameContInvalChars"), S._("Title_NewLibrary"), MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string path = Path.Combine(App.LibraryPath, $"{dialog.Input}.db");
                if (File.Exists(path))
                {
                    MessageBox.Show(S._("LibAlreadExist"), S._("Title_NewLibrary"), MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                TargetLibrary library = new TargetLibrary(path);

                var pair = new Pair(library.Name);
                Libraries.Add(pair);
                SelectedLibrary = pair;
                MessageBox.Show(S._("CreateLibSuccess"));
            }
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
            switch (Path.GetExtension(dialog.FileName).ToLower())
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

            ImportLibraryDialog importLibraryDialog =
                new ImportLibraryDialog(specimenDatabase, SelectedLibrary.Name);
            importLibraryDialog.Owner = this;
            importLibraryDialog.CanAddLibrary = false;
            if (importLibraryDialog.ShowDialog().GetValueOrDefault())
            {
                Pair pair = Libraries.First((x) => x.Name == importLibraryDialog.LibraryName);
                if (pair != null)
                {
                    RefreshLibraryAdapters(pair);
                }

                if (SelectedLibrary != pair)
                {
                    SelectedLibrary = pair;
                }
            }
        }

        private void DelLibrary_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (SelectedLibrary != null)
            {
                var ret = MessageBox.Show(
                    string.Format(S._("Format_ConfirmDeleteLibrary_0"), SelectedLibrary.Name),
                    //$"存样库删除后不能恢复，确定要删除\"{SelectedLibrary.Name}\"吗？",
                    //$"删除存样库[{SelectedLibrary.Name}]", 
                    string.Format(S._("Format_DeleteLibrary_0"), SelectedLibrary.Name),
                    MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                if (ret == MessageBoxResult.OK)
                {
                    Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(SelectedLibrary.Library.FileName, 
                        Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                    Libraries.Remove(SelectedLibrary);
                }
            }
        }

        private void RenameLibrary_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (SelectedLibrary == null)
            {
                return;
            }

            GetInputDialog dialog = new GetInputDialog
            {
                Owner = this,
                Title = S._("Title_Rename"),
                Prompt = S._("Tip_PleaseEnterANewName"),
                Input = SelectedLibrary.Name
            };
            if (dialog.ShowDialog().GetValueOrDefault())
            {
                if (!TargetLibrary.IsNameValid(dialog.Input))
                {
                    MessageBox.Show(S._("LibNameContInvalChars"), S._("Title_Rename"), MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (File.Exists(TargetLibrary.GetPathByName(dialog.Input)))
                {
                    MessageBox.Show(S._("LibraryNameConflicts"), S._("Title_Rename"), MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                SelectedLibrary.Library.Rename(dialog.Input);
                SelectedLibrary.Name = dialog.Input;
            }
        }

        private void CanOperateOnLibrary(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = SelectedLibrary != null;
        }

        private void OnSpecularComponentChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SpecimenDataGrid == null)
                return;

            var view = CollectionViewSource.GetDefaultView(SpecimenDataGrid.DataGrid.ItemsSource);
            if (view != null)
            {
                view.Filter = GetFilter();
            }
        }

        private int _filterMethod = 0;
        public int FilterMethod
        {
            get { return _filterMethod;}
            set
            {
                if (value != _filterMethod)
                {
                    _filterMethod = value;
                    OnPropertyChanged(nameof(FilterMethod));
                }
            }
        }

        private Predicate<object> GetFilter()
        {
            switch (FilterMethod)
            {
                case 0: return Filter.FilterByName;
                case 1: return Filter.FilterByDate;
                case 2: return Filter.FilterByLab;
                default: return Filter.FilterByNone;
            }
        }

        private void OnFilterMethodChanged(object sender, SelectionChangedEventArgs e)
        {
            var view = CollectionViewSource.GetDefaultView(SpecimenDataGrid.DataGrid.ItemsSource);
            if (view != null)
            {
                view.Filter = GetFilter();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OnSearch(object sender, RoutedEventArgs e)
        {
            RefreshDataGrid();
        }

        private void RefreshDataGrid()
        {
            var view = CollectionViewSource.GetDefaultView(SpecimenDataGrid.DataGrid.ItemsSource);
            if (view != null)
            {
                view.Filter = GetFilter();
            }
        }


        private void OnDeleteSelectedStandards(object sender, ExecutedRoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show(
                S._("PromptConfirmDeleteSampleInLibrary"), //"存样删除后不能恢复，确定要删除吗？",
                S._("Title_DeleteSampleInLibrary"), // 删除存样
                MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                HashSet<string> names = new HashSet<string>();
                foreach (SpecimenAdapter adapter in SpecimenDataGrid.SelectedItems)
                {
                    names.Add(adapter.Name);
                }

                SelectedLibrary.Adapters.RemoveAll((x) => names.Contains(x.Name));
                SelectedLibrary.Library.RemoveAll((x) => names.Contains(x.Name));
//                RefreshDataGrid();
            }
        }

        private void CanDeleteStandardsExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = SpecimenDataGrid.DataGrid.SelectedItems.Count > 0;
        }

        private void OnRenameStandard(object sender, ExecutedRoutedEventArgs e)
        {
            if (SpecimenDataGrid.SelectedItem == null)
            {
                return;
            }
            GetInputDialog dialog = new GetInputDialog();
            dialog.Owner = this;
            dialog.Title = S._("Title_Rename");
            dialog.Prompt = S._("Tip_PleaseEnterANewName");
            dialog.Input = SpecimenDataGrid.SelectedItem.Name;
            if (dialog.ShowDialog().GetValueOrDefault())
            {
                string oldName = SpecimenDataGrid.SelectedItem.Name;
                string newName = dialog.Input.Trim();
                if (newName == oldName)
                {
                    return;
                }

                if (string.IsNullOrWhiteSpace(newName))
                {
                    // 名称不能为空
                    MessageBox.Show(S._("PromptNameCantEmpty"), dialog.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (SelectedLibrary.Library.Targets.Value.ContainsKey(newName))
                {
                    // 名称和现有名字冲突
                    MessageBox.Show(S._("LibraryNameConflicts"), dialog.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                

                Specimen tmp = SelectedLibrary.Library.Targets.Value[oldName];
                tmp.Name = newName;
                SelectedLibrary.Library.Targets.Value.Remove(oldName);
                SelectedLibrary.Library.Targets.Value.Add(newName, tmp);

                //SpecimenDataGrid.DataGrid.Ce
                SpecimenDataGrid.SelectedItem.OnPropertyChanged("Name");

            }
        }

        private void CanRenameStandardExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = SpecimenDataGrid.DataGrid.SelectedItems.Count == 1;
        }

        private void OnModifyStandard(object sender, ExecutedRoutedEventArgs e)
        {
            if (SpecimenDataGrid.SelectedItem == null)
            {
                return;
            }

            MainWindow mainWindow = (MainWindow) App.Current.MainWindow;
            StandardInputDialog dialog = new StandardInputDialog(mainWindow.Colorimeter);
            dialog.Owner = this;

            var oldSpecimen = SpecimenDataGrid.SelectedItem.Specimen;
            dialog.Specimen = oldSpecimen;
            dialog.Title = S._("Title_ModifyLibrarySample");
            if (dialog.ShowDialog().GetValueOrDefault())
            {
                StandardSpecimen tmp = new StandardSpecimen(dialog.Specimen);
                if (tmp.Name != oldSpecimen.Name &&
                    SelectedLibrary.Library.Targets.Value.ContainsKey(tmp.Name))
                {
                    // "名称和现有存样冲突"
                    MessageBox.Show(S._("LibraryNameConflicts"), S._("Title_ModifyLibrarySample"), MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                SelectedLibrary.Library.Targets.Value.Remove(oldSpecimen.Name);
                SelectedLibrary.Library.Targets.Value.Add(tmp.Name, new StandardSpecimen(tmp));

                int sciIndex = SelectedLibrary.Adapters.FindIndex(0, (x) => (x.Name == oldSpecimen.Name && x.SpecularComponent == SpecularComponent.SCI));
                int sceIndex = SelectedLibrary.Adapters.FindIndex(0, (x) => (x.Name == oldSpecimen.Name && x.SpecularComponent == SpecularComponent.SCE));
                if (tmp.SCI != null)
                {
                    StandardSpecimenAdapter adapter =
                        new StandardSpecimenAdapter(tmp, _formula, _displayFields, SpecularComponent.SCI);
                    if (sciIndex >= 0)
                    {
                        SelectedLibrary.Adapters[sciIndex] = adapter;
                    }
                    else
                    {
                        if (sceIndex >= 0)
                        {
                            
                            SelectedLibrary.Adapters.Insert(sceIndex, adapter);
                            sceIndex += 1;
                        }
                        else
                        {
                            SelectedLibrary.Adapters.Add(adapter);
                            sciIndex = SelectedLibrary.Adapters.Count - 1;
                        }
                    }
                }
                else if (sciIndex >= 0)
                {
                    if (oldSpecimen.SCE != null)
                    {
                        SelectedLibrary.Adapters.RemoveAt(sciIndex);
                    }
                    sceIndex = sciIndex;
                    sciIndex = -1;
                }

                if (tmp.SCE != null)
                {
                    StandardSpecimenAdapter adapter =
                        new StandardSpecimenAdapter(tmp, _formula, _displayFields, SpecularComponent.SCE);
                    if (sceIndex >= 0)
                    {
                        SelectedLibrary.Adapters[sceIndex] = adapter;
                    }
                    else
                    {
                        if (sciIndex >= 0)
                        {
                            SelectedLibrary.Adapters.Insert(sciIndex + 1, adapter);
                        }
                        else
                        {
                            SelectedLibrary.Adapters.Add(adapter);
                        }
                    }
                }
                else if (oldSpecimen.SCE != null && sceIndex >= 0)
                {
                    SelectedLibrary.Adapters.RemoveAt(sceIndex);
                }
            }
        }
    }
}
