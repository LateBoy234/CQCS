using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using ThreeNH.ColorQualityControl.Adapter;
using ThreeNH.DataModel;
using ThreeNH.Helper;

namespace ThreeNH.ColorQualityControl.UI
{
    /// <summary>
    /// SpecimenDataGrid.xaml 的交互逻辑
    /// </summary>
    public partial class SpecimenDataGrid : UserControl
    {
        ChromaticityField[] _fields = new ChromaticityField[]
        {
            ChromaticityField.PseudoColor,
            ChromaticityField.Name,
            ChromaticityField.UV,
            ChromaticityField.SpecularComponent,
            ChromaticityField.L,
            ChromaticityField.a,
            ChromaticityField.b
        };

        //ObservableCollection<SpecimenWrap> _items = new ObservableCollection<SpecimenWrap>();

        public SpecimenDataGrid()
        {
            InitializeComponent();
            ResetColumns();
        }

        public ChromaticityField[] Fields
        {
            get { return _fields; }
            set
            {
                _fields = value;
                ResetColumns();
            }
        }

        bool _isShowTestFields = false;
        public bool IsShowTestFields
        {
            get { return _isShowTestFields;  }
            set
            {
                if (_isShowTestFields != value)
                {
                    _isShowTestFields = value;
                    foreach(var field in _fields)
                    {
                        if (field.IsNeedStandard())
                        {
                            int index = Array.IndexOf(_fields, field);
                            datagrid.Columns[index].Visibility = _isShowTestFields ? Visibility.Visible : Visibility.Collapsed;
                        }
                    }
                }
            }
        }

        private void ResetColumns()
        {
            if (datagrid == null)
                return;

            datagrid.BeginInit();
            datagrid.Columns.Clear();
            if (_fields == null)
            {
                datagrid.EndInit();
                return;
            }

            //datagrid.SelectedValuePath = "Specimen";

            foreach (var field in _fields)
            {
                var converter = TestSpecimenAdapter.GetPropertyValueConverter(field);

                if (field == ChromaticityField.PseudoColor)
                {
                    DataGridTemplateColumn column = Resources["PseudoColorColumn"] as DataGridTemplateColumn;
                    column.Header = field.Name();
                    datagrid.Columns.Add(column);
                }
                else if (field.IsReflectance())
                {
                    DataGridTextColumn column = new DataGridTextColumn();
                    datagrid.Columns.Add(column);

                    int wavelength = field.ToWavelength();
                    column.MinWidth = 60;
                    column.Header = field.Description();

                    Binding binding = new Binding("Spectrum");
                    binding.Converter = converter;
                    binding.ConverterParameter = wavelength;
                    column.Binding = binding;
                    column.SortMemberPath = "Spectrum[" + wavelength.ToString() + "]";
                }
                else if (field.IsReflectanceDifference())
                {
                    DataGridTextColumn column = new DataGridTextColumn();
                    datagrid.Columns.Add(column);

                    int wavelength = field.ToWavelength();
                    column.MinWidth = 60;
                    column.Header = field.Description();

                    Binding binding = new Binding("ReflectanceDifferences");
                    binding.Converter = converter;
                    binding.ConverterParameter = wavelength;
                    column.Binding = binding;
                    column.SortMemberPath = "ReflectanceDifferences[" + wavelength.ToString() + "]";
                }
                else if (field == ChromaticityField.Judgement)
                {
                    DataGridTextColumn column = new DataGridTextColumn();
                    column.Header = S._("Label_Judgement");

                    Binding binding = new Binding("Judgement.TotalJudgement");
                    binding.Converter = converter;
                    column.Binding = binding;
                    datagrid.Columns.Add(column);
                }
                else
                {
                    DataGridTextColumn column = new DataGridTextColumn();
                    datagrid.Columns.Add(column);

                    column.MinWidth = 60;
                    column.Header = field.Name();
                    Binding binding = new Binding();
                    binding.Converter = converter;

                    // 如果是名称或料号，直接引用SpecimenWrap的成员，方便用户修改时更新
                    binding.Path = new PropertyPath(field.ToString());
                    column.Binding = binding;
                }
            }

            datagrid.EndInit();
        }

        public ItemCollection Items
        {
            get { return datagrid.Items; }
        }

        public IEnumerable<SpecimenAdapter> ItemsSource
        {
            get { return datagrid.ItemsSource as IEnumerable<SpecimenAdapter>; }
            set { datagrid.ItemsSource = value; }
        }
        public DataGrid DataGrid
        {
            get { return datagrid; }
        }

        public SpecimenAdapter SelectedItem
        {
            get { return datagrid.SelectedItem as SpecimenAdapter;  }
            set { datagrid.SelectedItem = value; }
        }

        public IList SelectedItems
        {
            get { return datagrid.SelectedItems; }
        }


    }
}
