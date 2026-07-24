using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;
using ThreeNH.ColorLibrary;
using ThreeNH.DataModel;
using Path = System.IO.Path;

namespace ThreeNH.ColorQualityControl.UI
{
    /// <summary>
    /// ImportLibraryDialog.xaml 的交互逻辑
    /// </summary>
    public partial class ImportLibraryDialog : Window
    {
        private readonly string NullMaterial = "<没有料号>";
        private SpecimenDatabase _database;
        private ColorDatabase _db;

        public ImportLibraryDialog(ColorDatabase db, string libraryName, SpecimenDatabase database)
        {
            _db = db;
            LibraryName = libraryName;
            _database = database;

            Materials.Add(S._("All"));
            try
            {
                Materials.AddRange((from s in database.StandardSpecimens select s.Material).Distinct());
                int index = Materials.IndexOf(null);
                if (index >= 0)
                {
                    Materials[index] = NullMaterial;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载文件失败：{ex.Message}", "导入存样库", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            InitializeComponent();
            DataContext = this;
        }

        public ImportLibraryDialog(SpecimenDatabase database, string name)
        {
            _database = database;
            LibraryName = name;

            Materials.Add(S._("All"));
            try
            {
                Materials.AddRange((from s in database.StandardSpecimens select s.Material).Distinct());
                int index = Materials.IndexOf(null);
                if (index >= 0)
                {
                    Materials[index] = NullMaterial;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载文件失败：{ex.Message}", "导入存样库", MessageBoxButton.OK, MessageBoxImage.Error);
            }

//            LibraryNames.AddRange(
//                from fileName in Directory.EnumerateFiles(App.LibraryPath, "*.db", SearchOption.TopDirectoryOnly)
//                select Path.GetFileNameWithoutExtension(fileName));

            SelectedMaterial = 0;
            InitializeComponent();
            DataContext = this;
        }

        public bool CanAddLibrary { get; set; } = true;

        public string LibraryName { get; set; }

        public int SelectedMaterial { get; set; }

        public List<string> Materials { get; } = new List<string>();

//        public List<string> LibraryNames { get; } = new List<string>();


        private void OnClickImport(object sender, RoutedEventArgs e)
        {
            IEnumerable<StandardSpecimen> standards = null;
            if (SelectedMaterial == 0)
            {
                standards = _database.StandardSpecimens;
            }
            else
            {
                string material = Materials[SelectedMaterial];
                if (material == NullMaterial)
                {
                    material = null;
                }
                standards = from std in _database.StandardSpecimens where std.Material == material select std;
            }

            var names = (from std in _database.StandardSpecimens select std.Name).Distinct();
            if (names.Count() < _database.StandardSpecimens.Length)
            {
                MessageBox.Show($"导入的样品不能存在重名项！");
                return;
            }

            var dict = new Dictionary<string, DataRow>();
            foreach (DataRow dataRow in _db.Colors.Rows)
            {
                dict[dataRow.Field<string>("Number")] = dataRow;
            }

            var newSpecimens = new List<StandardSpecimen>();
            var duplications = new List<StandardSpecimen>();
            var standardSpecimens = standards.ToList();
            foreach (var specimen in standardSpecimens)
            {
                if (dict.ContainsKey(specimen.Name))
                {
                    duplications.Add(specimen);
                }
                else
                {
                    newSpecimens.Add(specimen);
                }
            }

            int totalCount = standardSpecimens.Count - duplications.Count;
            string message;

            string action = null;

            if (duplications.Count > 0)
            {
                DuplicationOperationsDialog dialog = new DuplicationOperationsDialog
                {
                    TotalCount = standardSpecimens.Count,
                    DuplicationCount = duplications.Count,
                    SourceName = "",
                    DestinationName = LibraryName,
                    Owner = this,
                };
                if (dialog.ShowDialog().GetValueOrDefault() == false)
                {
                    return;
                }

                action = dialog.HowToDealDuplication;
            }

            foreach (var specimen in newSpecimens)
            {
                _db.Colors.Rows.Add(_db.ToColorRow(specimen));
            }

            if (action == "R")
            {
                foreach (var specimen in duplications)
                {
                    ColorDatabase.UpdateColorRow(dict[specimen.Name], specimen);
                }

                totalCount += duplications.Count;

                message = $"{S._("Label_Import")}{totalCount}{S._("Label_StoredSample")}，{S._("Label_Cover")}{duplications.Count}";
            }
            else if (action == "S")
            {
                // 跳过
                message = $"{S._("Label_Import")}{totalCount}{S._("Label_StoredSample")}，{S._("Label_Skip")}{duplications.Count}个";
            }
            else if (action == "L")
            {
                // 用户选择
                message = "";
            }
            else
            {
                message = $"{S._("Label_Import")}{totalCount}{S._("Label_StoredSample")}";
            }

            try
            {
                _db.Update(_db.Colors);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Title, MessageBoxButton.OK, MessageBoxImage.Error);
                DialogResult = false;
                _db.Colors.RejectChanges();
            }

            MessageBox.Show(message);
            DialogResult = true;
        }
    }
}
