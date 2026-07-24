using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Data;
using ThreeNH.DataModel;
using System.Windows.Controls;
using System.ComponentModel;
using System.Windows.Input;
using ThreeNH.Optical;
using ThreeNH.ColorAlgorithm;

namespace ThreeNH.ColorQualityControl.UI
{
    /// <summary>
    /// ChromaticitySettingDialog.xaml 的交互逻辑
    /// </summary>
    public partial class ChromaticitySettingDialog : Window, INotifyPropertyChanged
    {
        ChromaticitySetting originalChromaticitySetting =
            new DataModel.ChromaticitySetting();

        // 左边未选中的字段
        ObservableCollection<ChromaticityField> leftChromaticityFields =
            new ObservableCollection<ChromaticityField>();

        // 右边选中的字段
        ObservableCollection<ChromaticityField> rightChromaticityFields =
            new ObservableCollection<ChromaticityField>();

        public event PropertyChangedEventHandler PropertyChanged;

        public ChromaticitySettingDialog(WavelengthRange wavelengthRange)
        {
            InitializeComponent();

            LeftListBox = LeftAttributeList;

            LeftAttributeList.ItemsSource = LeftChromaticityFields;

            Binding binding = new Binding("LeftColorFields");
            binding.Source = this;
            LeftColorList.SetBinding(ListBox.ItemsSourceProperty, binding);

            binding = new Binding("LeftDiffFields");
            binding.Source = this;
            LeftDiffList.SetBinding(ListBox.ItemsSourceProperty, binding);

            binding = new Binding("LeftIndexFields");
            binding.Source = this;
            LeftIndexList.SetBinding(ListBox.ItemsSourceProperty, binding);

            //指数差
            binding = new Binding("LeftIndexDiffFields");
            binding.Source = this;
            LeftIndexDiffList.SetBinding(ListBox.ItemsSourceProperty, binding);

            binding = new Binding("LeftSpecimentAttributesFields");
            binding.Source = this;
            LeftSpecimenAttributesList.SetBinding(ListBox.ItemsSourceProperty, binding);

            binding = new Binding("LeftReflectanceFields");
            binding.Source = this;
            LeftReflectanceList.SetBinding(ListBox.ItemsSourceProperty, binding);

            //K/S
            binding = new Binding("LeftAbsoluteStrengthFields");
            binding.Source = this;
            LeftAbsoluteStrengthList.SetBinding(ListBox.ItemsSourceProperty, binding);

            binding = new Binding("LeftChPListFields");
            binding.Source = this;
            LeftChPList.SetBinding(ListBox.ItemsSourceProperty, binding);

            binding = new Binding("LeftEUPListFields");
            binding.Source = this;
            LeftEUPList.SetBinding(ListBox.ItemsSourceProperty, binding);

            binding = new Binding("LeftUSAPListFields");
            binding.Source = this;
            LeftUSAPList.SetBinding(ListBox.ItemsSourceProperty, binding);

            binding = new Binding("LeftAbsorbanceListFields");
            binding.Source = this;
            LeftAbsorbanceList.SetBinding(ListBox.ItemsSourceProperty, binding);

            RightAttributeList.ItemsSource = rightChromaticityFields;

            foreach (ChromaticityField field in Enum.GetValues(typeof(ChromaticityField)))
            {
                if (field.IsReflectance() || field.IsReflectanceDifference())
                {
                    if (!wavelengthRange.IsValid || !wavelengthRange.Contains(field.ToWavelength()))
                        continue;
                }
                else if (field == ChromaticityField.MaxCount)
                {
                    continue;
                }
                leftChromaticityFields.Add(field);
            }

            DataContext = this;
        }

        void UpdateListBox(ListBox listBox)
        {
            var expression = listBox.GetBindingExpression(ListBox.ItemsSourceProperty);
            if (expression != null)
                expression.UpdateTarget();
        }

        public ObservableCollection<ChromaticityField> LeftChromaticityFields
        {
            get { return leftChromaticityFields; }
        }

        public List<ChromaticityField> LeftColorFields
        {
            get
            {
                List<ChromaticityField> list = new List<ChromaticityField>();
                foreach (var field in leftChromaticityFields)
                {
                    if (field.IsColorField())
                        list.Add(field);
                }
                return list;
            }
        }

        public List<ChromaticityField> LeftDiffFields
        {
            get
            {
                List<ChromaticityField> list = new List<ChromaticityField>();
                foreach (var field in leftChromaticityFields)
                {
                    if (field.IsColorDiffField() && !field.IsReflectanceDifference())
                        list.Add(field);
                }
                return list;
            }
        }

        public List<ChromaticityField> LeftIndexFields
        {
            get
            {
                List<ChromaticityField> list = new List<ChromaticityField>();
                foreach (var field in leftChromaticityFields)
                {
                    if (field.IsChromaticityIndex())
                        list.Add(field);
                }
                return list;
            }
        }

        public List<ChromaticityField> LeftIndexDiffFields
        {
            get
            {
                List<ChromaticityField> list = new List<ChromaticityField>();
                foreach (var field in leftChromaticityFields)
                {
                    if (field.IsChromaticityIndexDiff())
                        list.Add(field);
                }
                return list;
            }
        }

        public List<ChromaticityField> LeftSpecimentAttributesFields
        {
            get
            {
                List<ChromaticityField> list = new List<ChromaticityField>();
                foreach (var field in leftChromaticityFields)
                {
                    if (field.IsSpecimenAttribute())
                        list.Add(field);
                }
                return list;
            }
        }

        public List<ChromaticityField> LeftReflectanceFields
        {
            get
            {
                List<ChromaticityField> list = new List<ChromaticityField>();
                foreach (var field in leftChromaticityFields)
                {
                    if (field.IsReflectance() || field.IsReflectanceDifference())
                        list.Add(field);
                }
                return list;
            }
        }

        public List<ChromaticityField> LeftAbsoluteStrengthFields
        {
            get
            {
                List<ChromaticityField> list = new List<ChromaticityField>();
                foreach (var field in leftChromaticityFields)
                {
                    if (field.IsAbsoluteStrengthField())
                        list.Add(field);
                }
                return list;
            }
        }
        //LeftAChPList
        /// <summary>
        /// 中国药典项
        /// </summary>
        public List<ChromaticityField> LeftChPListFields
        {
            get
            {
                List<ChromaticityField> list = new List<ChromaticityField>();
                foreach (var field in leftChromaticityFields)
                {
                    if (field.IsChPField())
                        list.Add(field);
                }
                return list;
            }
        }

        /// <summary>
        /// 欧洲药典项
        /// </summary>
        public List<ChromaticityField> LeftEUPListFields
        {
            get
            {
                List<ChromaticityField> list = new List<ChromaticityField>();
                foreach (var field in leftChromaticityFields)
                {
                    if (field.IsEUPField())
                        list.Add(field);
                }
                return list;
            }
        }

        /// <summary>
        /// 美国药典项
        /// </summary>
        public List<ChromaticityField> LeftUSAPListFields
        {
            get
            {
                List<ChromaticityField> list = new List<ChromaticityField>();
                foreach (var field in leftChromaticityFields)
                {
                    if (field.IsUSAPField())
                        list.Add(field);
                }
                return list;
            }
        }

        public List<ChromaticityField> LeftAbsorbanceListFields
        {
            get
            {
                List<ChromaticityField> list = new List<ChromaticityField>();
                foreach (var field in leftChromaticityFields)
                {
                    if (field.IsAbsorbanceField())
                        list.Add(field);
                }
                return list;
            }
        }

        public ChromaticitySetting ChromaticitySetting
        {
            get
            {
                ChromaticitySetting newSetting = new ChromaticitySetting();
                newSetting.SetDisplayFields(rightChromaticityFields);
                return newSetting;
            }
            set
            {
                originalChromaticitySetting = new ChromaticitySetting(value);
                MoveToRight(originalChromaticitySetting.DisplayFields, false);

                UpdateColorSpaceAndDiffFormula();
            }
        }

        public bool CanMoveToRight
        {
            get
            {
                if (LeftListBox != null)
                {
                    return LeftListBox.SelectedItems.Count > 0;
                }
                return false;
            }
        }

        /// <summary>
        /// 如果显示的字段被更改了，返回真
        /// </summary>
        public bool IsModified
        {
            get
            {
                var originalFields = originalChromaticitySetting.DisplayFields;
                if (originalFields.Length != rightChromaticityFields.Count)
                    return true;

                for (int i = 0; i < originalFields.Length; i++)
                {
                    if (originalFields[i] != rightChromaticityFields.ElementAt(i))
                        return true;
                }

                return false;
            }
        }


        private int GetAttributeOrder(ChromaticityField field)
        {
            for (int i = 0; i < leftChromaticityFields.Count; i++)
            {
                if (field < leftChromaticityFields.ElementAt(i))
                    return i;
            }
            return leftChromaticityFields.Count;
        }

        private void MoveToLeftClick(object sender, RoutedEventArgs e)
        {
            MoveToLeft(GetSelectedFields(RightAttributeList).Values);

            OnPropertyChanged("IsModified");
        }

        private void MoveToRightClick(object sender, RoutedEventArgs e)
        {
            MoveToRight(GetSelectedFields(LeftListBox).Values);

            OnPropertyChanged("IsModified");
        }

        private void MoveToLeft(IEnumerable<ChromaticityField> fields)
        {
            foreach (ChromaticityField field in fields)
            {
                //if (field.IsFixedDisplayField())
                //    continue;

                leftChromaticityFields.Insert(GetAttributeOrder(field), field);
                rightChromaticityFields.Remove(field);
            }
            UpdateListBox(LeftListBox);
        }

        static SortedList<int, ChromaticityField> GetSelectedFields(ListBox lstbox)
        {
            SortedList<int, ChromaticityField> fields =
                new SortedList<int, ChromaticityField>();
            foreach (ChromaticityField field in lstbox.SelectedItems)
            {
                fields.Add(lstbox.Items.IndexOf(field), field);
            }

            return fields;
        }

        private void MoveToRight(IEnumerable<ChromaticityField> fields, bool selectNewFields = true)
        {
            int insertIndex;
            if (RightAttributeList.SelectedItems.Count >= 1)
            {
                insertIndex = rightChromaticityFields.IndexOf(
                    (ChromaticityField)RightAttributeList.SelectedItems[RightAttributeList.SelectedItems.Count - 1]) + 1;
            }
            else
            {
                insertIndex = rightChromaticityFields.Count;
            }
            RightAttributeList.SelectedItems.Clear();
            foreach (ChromaticityField field in fields)
            {
                rightChromaticityFields.Insert(insertIndex++, field);
                if (selectNewFields)
                {
                    RightAttributeList.SelectedItems.Add(field);
                }

                leftChromaticityFields.Remove(field);
            }
            UpdateListBox(LeftListBox);
            RightAttributeList.ScrollIntoView(fields.First());
        }

        private void RemoveAllClick(object sender, RoutedEventArgs e)
        {
            RemoveAll();
            OnPropertyChanged("IsModified");
        }

        private void RemoveAll()
        {
            foreach (ChromaticityField field in RightAttributeList.Items)
            {
                //if (field.IsFixedDisplayField())
                //    continue;

                leftChromaticityFields.Insert(GetAttributeOrder(field), field);
            }

           // var fixedFields = rightChromaticityFields.Where(field => field.IsFixedDisplayField()).ToList();
            rightChromaticityFields.Clear();
            //foreach (var field in fixedFields)
            //{
            //    rightChromaticityFields.Add(field);
            //}

            UpdateListBox(LeftListBox);
        }

        private void ColorSpaceSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ColorSpaceComboBox.ColorSpace != ColorSpace.Unknown)
            {
                ChromaticitySetting setting = new ChromaticitySetting();
                setting.SetDisplayFields(rightChromaticityFields);

                if (setting.ColorSpace != ColorSpaceComboBox.ColorSpace)
                {
                    setting.DefaultChromaticity(ColorSpaceComboBox.ColorSpace);

                    RemoveAll();
                    MoveToRight(setting.DisplayFields);
                    OnPropertyChanged("IsModified");
                }
            }
        }

        private void DiffFormulaSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DiffFormulaComboBox.ColorDiffFormula != ColorDiffFormula.Unknown)
            {
                ChromaticitySetting setting = new ChromaticitySetting();
                setting.SetDisplayFields(rightChromaticityFields);

                if (setting.ColorDiffFormula != DiffFormulaComboBox.ColorDiffFormula)
                {
                    setting.DefaultEquation(DiffFormulaComboBox.ColorDiffFormula);

                    RemoveAll();
                    MoveToRight(setting.DisplayFields);
                    OnPropertyChanged("IsModified");
                }
            }
        }

        private void TopClick(object sender, RoutedEventArgs e)
        {
            var fields = GetSelectedFields(RightAttributeList);
            if (fields.Count == 0)
                return;

            for (int i = 0; i < fields.Values.Count; i++)
            {
                rightChromaticityFields.Move(
                    rightChromaticityFields.IndexOf(fields.Values[i]), i);
            }

            OnPropertyChanged("IsModified");
        }

        private void UpClick(object sender, RoutedEventArgs e)
        {
            var fields = GetSelectedFields(RightAttributeList);
            if (fields.Count == 0)
                return;

            int newPos = Math.Max(fields.Keys[0] - 1, 0);
            for (int i = 0; i < fields.Values.Count; i++)
            {
                rightChromaticityFields.Move(
                    rightChromaticityFields.IndexOf(fields.Values[i]),
                    newPos + i);
            }
            OnPropertyChanged("IsModified");
        }

        private void DownClick(object sender, RoutedEventArgs e)
        {
            var fields = GetSelectedFields(RightAttributeList);
            if (fields.Count == 0)
                return;

            int newPos = Math.Min(fields.Keys.Last() + 1, rightChromaticityFields.Count - 1);
            for (int i = 0; i < fields.Values.Count; i++)
            {
                rightChromaticityFields.Move(
                    rightChromaticityFields.IndexOf(fields.Values[i]),
                    newPos);
            }

            OnPropertyChanged("IsModified");
        }

        private void BottomClick(object sender, RoutedEventArgs e)
        {
            var fields = GetSelectedFields(RightAttributeList);
            if (fields.Count == 0)
                return;

            for (int i = 0; i < fields.Values.Count; i++)
            {
                rightChromaticityFields.Move(
                    rightChromaticityFields.IndexOf(fields.Values[i]),
                    rightChromaticityFields.Count - 1);
            }

            OnPropertyChanged("IsModified");
        }

        void OkClick(object sender, RoutedEventArgs e)
        {
            if (rightChromaticityFields.Count == 0)
            {
                MessageBox.Show(S._("PleaseSelectDisplayedItems"), Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            DialogResult = true;
        }

        private void RestoreButtonClick(object sender, RoutedEventArgs e)
        {
            RemoveAll();
            MoveToRight(originalChromaticitySetting.DisplayFields);

            OnPropertyChanged("IsModified");
        }

        void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));

                if (propertyName == "IsModified")
                {
                    UpdateColorSpaceAndDiffFormula();
                }
            }
        }

        void UpdateColorSpaceAndDiffFormula()
        {
            ChromaticitySetting setting = new ChromaticitySetting();
            setting.SetDisplayFields(rightChromaticityFields);

            ColorSpaceComboBox.ColorSpace = setting.ColorSpace;
            DiffFormulaComboBox.ColorDiffFormula = setting.ColorDiffFormula;
        }


        // 左侧当前显示的列表框
        ListBox LeftListBox { get; set; }

        private void OnLeftTabControlSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                TabItem tabItem = e.AddedItems[0] as TabItem;
                if (tabItem != null)
                {
                    LeftListBox = tabItem.Content as ListBox;
                    if (LeftListBox != null)
                    {
                        UpdateListBox(LeftListBox);
                    }
                }
            }
            OnPropertyChanged("CanMoveToRight");
        }

        private void OnLeftListBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            OnPropertyChanged("CanMoveToRight");
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                RightAttributeList.SelectedItems.Clear();
                e.Handled = true;
            }
            else
            {
                base.OnPreviewKeyDown(e);
            }
        }
    }
}