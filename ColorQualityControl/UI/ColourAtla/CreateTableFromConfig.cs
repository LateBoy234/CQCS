using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Shapes;
using ThreeNH.ColorQualityControl.Adapter;
using ThreeNH.ColorSpecimen;
using ThreeNH.DataModel;
using ThreeNH.Helper;
using ThreeNH.ColorAlgorithm;
using ThreeNH.Optical;

namespace ThreeNH.ColorQualityControl.UI.ColourAtla
{
    class CreateTableFromConfig
    {
        private CurrentSetting _curSet;
        private PanTongMatch _parent;
        private Specimen sample;
        private DataTable _dataTable;
        private StandardObserver _observer;
        private const string _noData = "--";
        public DataTable DataTable
        {
            get => _dataTable;
            private set
            {
                _dataTable = value;
            }
        }
        /// <summary>
        /// 显示的列
        /// </summary>
        public ObservableCollection<DataGridColumn> Columns { get; } = new ObservableCollection<DataGridColumn>();
        public List<Specimen> SampleList { get; set; }
        private Type GetItemDataType(ChromaticityField field)
        {
            string str = "";
            double d = 0;
            int i = 0;
            DateTime dateTime = DateTime.Now;
            bool bl;
            bl = ChromaticityFieldExtension.IsDoubleValue(field);
            if (bl) return d.GetType();
            bl = ChromaticityFieldExtension.IsDateTime(field);
            if (bl) return dateTime.GetType();
            bl = ChromaticityFieldExtension.IsIntValue(field);
            if (bl) return i.GetType();
            return str.GetType();
        }
        public DataRow CreateDataRow(DataTable dataTable, Specimen specimen, StandardSpecimen standard)
        {
            DataRow row = dataTable.NewRow();
            TestSpecimen test = new TestSpecimen(specimen);
            test.StandardSpecimen = standard;
            SpecularComponent specularComponent = _parent.CurrentSpecCom == "SCE" ? SpecularComponent.SCE : 
                (_parent.IfNot45_0 ? SpecularComponent.SCI : SpecularComponent.Unknown);

            var database = _curSet.MainWindowObj.Database;
            var formula = new Formula(_parent.CurrentIllum, _observer);
            Formula ref1, ref2;
            database.GetReferenceFormula(database.PrimaryFormula, out ref1, out ref2);//参考光源一 参考光源二
            var standardAdapter = database.GetAdapter(standard, specularComponent, _observer, _parent.CurrentIllum);
            var adapter = new TestSpecimenAdapter(
                test, formula, database.GetChromaticitySetting(standard), specularComponent,
                standardAdapter, /*GetTolerance(standard, key.Illuminant)*/
                ref1, ref2);
          
            adapter.Judge(standardAdapter, database.JudgementSetting);
            int count = 0;
            foreach (var item in row.ItemArray)
            {
                try
                {
                    if (adapter.GetType().GetProperty(
                                       _curSet.DispSet.DisplayFields[count].ToString()) != null)
                    {
                        row[count] = adapter.GetType().GetProperty(
                                       _curSet.DispSet.DisplayFields[count].ToString()).GetValue(adapter, null);
                    }
                   
                }
                catch (Exception ex)
                {
                    count++;
                    continue;
                }
               
                if (_curSet.DispSet.DisplayFields[count].ToString() == "Judgement") row[count] = _noData;
                if (_curSet.DispSet.DisplayFields[count].ToString() == "PseudoColor") row[count] = _noData;
                if (_curSet.DispSet.DisplayFields[count].ToString() == "Name") row[count] = _noData;
                if (_curSet.DispSet.DisplayFields[count].ToString() == "IlluminantAndObserver") row[count] = _noData;
                count++;
            }
            return row;
        }
        private static int _count = 0;
        private DataGridTemplateColumn CreatePseudoColorColumn(object columnHeader)
        {
            DataGridTemplateColumn column = new DataGridTemplateColumn();
            column.Header = columnHeader;

            FrameworkElementFactory factoryRectangle =
                new FrameworkElementFactory(typeof(Rectangle));
            factoryRectangle.Name = "pseudoColorRect";
            factoryRectangle.SetValue(Rectangle.MinWidthProperty, 16.0);
            factoryRectangle.SetValue(Rectangle.MinHeightProperty, 16.0);
            factoryRectangle.SetValue(Rectangle.FillProperty, new Binding("PseudoColor"));

            column.CellTemplate = new DataTemplate();
            column.CellTemplate.VisualTree = factoryRectangle;
            column.SortMemberPath = "Adapter.ColorValue";
            return column;
        }

        public DataTable CreateTableFromDispSet( Specimen specimen )
        {
            DataTable dataTable = new DataTable("No." +( _count ++ ).ToString());
                    
            Tolerance tolerance = _curSet.MainWindowObj.Database.GetTolerance(_curSet.MainWindowObj.SingleStandardInShowing, 
                new ObserverIlluminantPair(_observer, _parent.CurrentIllum));

            foreach (var field in _curSet.DispSet.DisplayFields)
            {
                var converter = TestSpecimenAdapter.GetPropertyValueConverter(field);
                DataColumn column1 = new DataColumn();
                dataTable.Columns.Add(column1);
                column1.DataType = GetItemDataType(field);
                column1.Caption = field.Description();

                if (field.IsReflectance())
                {
                    DataGridTextColumn column = new DataGridTextColumn();
                    int wavelength = field.ToWavelength();
                    

                    Binding binding = new Binding("Adapter.Spectrum");
                    binding.Converter = converter;
                    binding.ConverterParameter = wavelength;
                    column.Binding = binding;
                    column.SortMemberPath = "Adapter.Spectrum[" + wavelength.ToString() + "]";
                    Columns.Add(column);
                }
                else if (field == ChromaticityField.PseudoColor)
                {
                    DataGridTemplateColumn column = CreatePseudoColorColumn(field.Name());
                    Columns.Add(column);
                }
                else if (field.IsReflectanceDifference())
                {
                    DataGridTextColumn column = new DataGridTextColumn();
                    Columns.Add(column);

                    int wavelength = field.ToWavelength();
                    column.MinWidth = 60;
                    column.Header = field.Description();

                    Binding binding = new Binding("Adapter.ReflectanceDifferences");
                    binding.Converter = converter;
                    binding.ConverterParameter = wavelength;
                    column.Binding = binding;
                    column.SortMemberPath = "Adapter.ReflectanceDifferences[" + wavelength.ToString() + "]";
                }
                else if (field == ChromaticityField.Judgement)
                {
                    DataGridTextColumn column = new DataGridTextColumn();
                    column.Header = S._("Label_Judgement");

                    Binding binding = new Binding("Judgement.TotalJudgement");
                    binding.Converter = converter;
                    column.Binding = binding;
                    Columns.Add(column);
                }
                else
                {
                    DataGridTextColumn column = new DataGridTextColumn();
                    Columns.Add(column);

                    Formula ref1, ref2;
                    _curSet.MainWindowObj.Database.GetReferenceFormula(_curSet.MainWindowObj.Formula, out ref1, out ref2);

                    column.MinWidth = 60;
                    switch (field)
                    {
                        case ChromaticityField.dEcmc:
                           // column.Header = string.Format("{0}", field.Name());
                            column.Header = string.Format("{0}", field.Name(), tolerance.KLcmc, tolerance.KCcmc);
                            break;
                        case ChromaticityField.dE94:
                            column.Header = string.Format("{0}({1}:{2}:{3})", field.Name(), tolerance.KL94, tolerance.KC94, tolerance.KH94);
                            break;
                        case ChromaticityField.dE00:
                            column.Header = string.Format("{0}({1}:{2}:{3})", field.Name(), tolerance.KL00, tolerance.KC00, tolerance.KH00);
                            break;
                        case ChromaticityField.MI1:
                            column.Header = ref1 != null ? SpecimenAdapter.MakeMIName(ref1) : "MI1";
                            break;
                        case ChromaticityField.MI2:
                            column.Header = ref2 != null ? SpecimenAdapter.MakeMIName(ref2) : "MI2";
                            break;
                        default:
                            column.Header = field.Name();
                            break;
                    }
                    Binding binding = new Binding();
                    binding.Converter = converter;

                    // 如果是名称或料号，直接引用SpecimenWrap的成员，方便用户修改时更新
                    if (field == ChromaticityField.Name || field == ChromaticityField.Material)
                        binding.Path = new PropertyPath(field.ToString());
                    else
                        binding.Path = new PropertyPath("Adapter." + field.ToString());
                    column.Binding = binding;
                }
            }
            return dataTable;
        }
        private void InitDataColumns()
        {
            DataTable = new DataTable("Samples");
            try
            {
                foreach(var item in _curSet.DispSet.DisplayFields)
                { 
                    DataTable.Columns.Add(
                        S._(item.ToString()),
                        GetItemDataType(item)
                        ); 
                }                
            }
            catch
            {

            }
        }

        public CreateTableFromConfig(CurrentSetting curSet, PanTongMatch tongMatch)
        {
            _curSet = curSet;
            _parent = tongMatch;
            _observer = _parent.CurrentObser == "2" ? StandardObserver.CIE1931 : StandardObserver.CIE1964;
            SampleList = new List<Specimen>();
            InitDataColumns();
        }
    }
}
