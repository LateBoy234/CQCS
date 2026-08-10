using System.Collections.Generic;
using System.Data;
using System.Windows.Controls;
using System.Windows.Media;
using System.Linq;//Ilist  AsEnumerable()
using System.Text.RegularExpressions;
using System;
using System.Windows.Input;
using PanTongColorMatchSearch;
using RelayCommand = ThreeNH.Helper.Plugin.RelayCommand;
using ThreeNH.ColorSpecimen;
using ThreeNH.ColorAlgorithm;
using ThreeNH.Optical;
using MahApps.Metro.Controls;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ThreeNH.ColorQualityControl.UI.ColourAtla
{
    public partial class KeyWordSearch : MetroWindow, INotifyPropertyChanged
    {
        private IList<PanTongColor> _pantong;
        private PanTongMatch _parent;
        private const string _resName = "ResultTable";
        private DataTable _searchResultDataTable;
        public DataTable SearchResultDataTable
        {
            get => _searchResultDataTable;
            private set
            {
                _searchResultDataTable = value;
                NotifyOfPropertyChange(nameof(SearchResultDataTable));
            }
        }
        private int _selectIndex;
        public int SelectIndex
        {
            get => _selectIndex;
            set
            {
                _selectIndex = value;
                NotifyOfPropertyChange(nameof(SelectIndex));
            }
        }
        private const string _orgName = "OrginalTable";
        private DataTable _allDataTable;
        private List<MatchedColorItem> _listSaved;

        private const string _specName = "SpectraTable";
        private DataTable _spectraTable;

        private Brush _searchPseudoColor;
        public Brush SearchPseudoColor
        {
            get => _searchPseudoColor;
            private set
            {
                _searchPseudoColor = value;
                NotifyOfPropertyChange(nameof(SearchPseudoColor));
            }
        }

        private int _begin, _interval, _count, _end;
        private ToastInfo _toastInfo;
        public KeyWordSearch(IList<PanTongColor> pantong, PanTongMatch obj)
        {
            
            InitializeComponent();
            _toastInfo = new ToastInfo(this);
            if (pantong == null)
            {
                _toastInfo.ShowToastInfo(S._("NoPanTongColorDataLilin"));
                return;
            }
            _pantong = pantong;
            _parent = obj;
            InitSpectraInfo();
            _allDataTable = new DataTable(_orgName);
            _searchResultDataTable = new DataTable(_resName);
            _spectraTable = new DataTable(_specName);
            InitialSearchResultList();
            SelectIndex = 0;
            InitCmd();
            InitContextMenu();
            DataContext = this;
        }
        private string _name = S._("Name");
        private string _remark = S._("Remark");
        private string _L = S._("L*");
        private string _a = S._("a*");
        private string _b = S._("b*");

        private void InitSpectraInfo()
        {
            PanTongColor pantong_ = _pantong.ElementAt(0);
            var colorData = pantong_.SpecimenData.SCI != null ? pantong_.SpecimenData.SCI : 
                (pantong_.SpecimenData.SCE != null ? pantong_.SpecimenData.SCE : 
                (pantong_.SpecimenData.Clr != null ? pantong_.SpecimenData.Clr : null));
            if (colorData == null) return;
            _begin = colorData.Spectrum.Range.Min;
            _interval = colorData.Spectrum.Range.Step;
            _end = colorData.Spectrum.Range.Max;
            _count = (_end - _begin) / _interval + 1;
        }
        private void SpectraTableCreateColumns(ref DataTable table)
        {
            double varDouble = 0.0;
            for (int i = _begin; i <= _end;)
            {
                table.Columns.Add(i.ToString(), varDouble.GetType());
                i += _interval;
            }
        }
        private void TableCreateColumns(ref DataTable table)
        {
            double varDouble = 0.0;
            string varString = "";
            table.Columns.Add(_name, varString.GetType());
            table.Columns.Add(_remark, varString.GetType());
            table.Columns.Add(_L, varDouble.GetType());
            table.Columns.Add(_a, varDouble.GetType());
            table.Columns.Add(_b, varDouble.GetType());
        }
        private void SpectraTableCreateRow(ref DataTable dataTable, PanTongColor panTongColor)
        {
            var backup = _parent.CurrentSpecCom;

            if (panTongColor.SpecimenData.SCI != null)
            {
                var row = dataTable.NewRow();
                int j = 0;
                for (int i = _begin; i <= _end;)
                {
                    row[i.ToString()] = panTongColor.SpecimenData.SCI.Spectrum.Data[j];
                    j++;
                    i += _interval;
                }
                dataTable.Rows.Add(row);
            }

            if (panTongColor.SpecimenData.SCE != null)
            {
                var row = dataTable.NewRow();
                int j = 0;
                for (int i = _begin; i <= _end;)
                {
                    row[i.ToString()] = panTongColor.SpecimenData.SCE.Spectrum.Data[j];
                    j++;
                    i += _interval;
                }
                dataTable.Rows.Add(row);
            }
            if (panTongColor.SpecimenData.Clr != null)
            {
                var row = dataTable.NewRow();
                int j = 0;
                for (int i = _begin; i <= _end;)
                {
                    row[i.ToString()] = panTongColor.SpecimenData.Clr.Spectrum.Data[j];
                    j++;
                    i += _interval;
                }
                dataTable.Rows.Add(row);
            }
            _parent.CurrentSpecCom = backup;
        }
        private void TableCreateRow(ref DataTable dataTable, PanTongColor panTongColor)
        {
            var backup = _parent.CurrentSpecCom;

            if (panTongColor.SpecimenData.SCI != null)
            {
                var row = dataTable.NewRow();
                row[_name] = panTongColor.SampleNames;
                row[_remark] = panTongColor.SampleRemarks;
                _parent.CurrentSpecCom = "SCI";
                var lab = _parent.PanTongColorDataObtainLab(panTongColor);
                row[_L] = Math.Round(lab.L, 2);
                row[_a] = Math.Round(lab.a, 2);
                row[_b] = Math.Round(lab.b, 2);
                dataTable.Rows.Add(row);
            }

            if (panTongColor.SpecimenData.SCE != null)
            {
                var row = dataTable.NewRow();
                var re = new Regex(@"^*\.SCE");
                var mat = re.Match(panTongColor.SampleNames);
                row[_name] = panTongColor.SampleNames + (mat.Success ? "" : ".SCE");
                mat = re.Match(panTongColor.SampleRemarks);
                row[_remark] = (mat.Success ? "--" : "SCE");
                _parent.CurrentSpecCom = "SCE";
                var lab = _parent.PanTongColorDataObtainLab(panTongColor);
                row[_L] = Math.Round(lab.L, 2);
                row[_a] = Math.Round(lab.a, 2);
                row[_b] = Math.Round(lab.b, 2);
                dataTable.Rows.Add(row);
            }
            if (panTongColor.SpecimenData.Clr != null)
            {
                var row = dataTable.NewRow();
                row[_name] = panTongColor.SampleNames;
                row[_remark] = panTongColor.SampleRemarks;
                var lab = _parent.PanTongColorDataObtainLab(panTongColor);
                row[_L] = Math.Round(lab.L, 2);
                row[_a] = Math.Round(lab.a, 2);
                row[_b] = Math.Round(lab.b, 2);
                dataTable.Rows.Add(row);
            }
            _parent.CurrentSpecCom = backup;
        }
        private void InitialSearchResultList()
        {
            TableCreateColumns(ref _allDataTable);
            TableCreateColumns(ref _searchResultDataTable);
            SpectraTableCreateColumns(ref _spectraTable);
            IEnumerable<MatchedColorItem> query;
            try
            {
                query = from pantong_ in _pantong.AsEnumerable()
                        let lab = _parent.PanTongColorDataObtainLab(pantong_)
                        let sample = _parent.GenerateSampleFromSpectra(pantong_)
                        select new MatchedColorItem
                        {
                            Name = pantong_.SampleNames + (_parent.CurrentSpecCom != "SCE" ? "" : ".SCE"),
                            Lab = lab,
                            Sample = sample
                        };
                _listSaved = query.ToList();
                foreach (PanTongColor panTongColor in _pantong)
                {
                    TableCreateRow(ref _allDataTable, panTongColor);
                    TableCreateRow(ref _searchResultDataTable, panTongColor);
                    SpectraTableCreateRow(ref _spectraTable, panTongColor);
                }
                SearchPseudoColor = _parent.GetPseudoColorFromLab(_listSaved.First().Lab);
            }
            catch (Exception ex)
            {
                _toastInfo.ShowToastInfo(ex.Message);
            }
        }
        #region UI Operation Deal Begin
        private void SetPseudoColor(int num, DataTable table)
        {
            if (table.Rows.Count > 0)
            {
                int idx = table.Columns.IndexOf(_L);
                double l = (double)table.Rows[num].ItemArray[idx];
                double a = (double)table.Rows[num].ItemArray[idx + 1];
                double b = (double)table.Rows[num].ItemArray[idx + 2];
                SearchPseudoColor = _parent.GetPseudoColorFromLab(l, a, b);
            }
        }
        public void OnSelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            if (args.AddedItems == null || args.AddedItems.Count == 0)
            {
                return;
            }
            var data = (DataRowView)args.AddedItems[0];

            int num = _parent.SelectedItemInDataTablePos(data, _searchResultDataTable);
            if (num != -1)
            {
                SetPseudoColor(num, _searchResultDataTable);
            }
        }
        private string _curfilter, _newfilter;
        private static readonly object Lock = new object();
        private void CopyRow(DataRow srcRow, DataRow dstRow)
        {
            try
            {
                int i = 0;
                foreach (var tmp in srcRow.ItemArray)
                {
                    dstRow[i] = tmp;
                    i++;
                }
            }
            catch
            { }

        }
        private DataTable UpdateSearchResultTable(string filter, string nameCol, DataTable allDataTable)
        {
            DataTable dstTable = new DataTable("result");
            foreach (DataColumn dataColumn in allDataTable.Columns)
            {
                dstTable.Columns.Add(dataColumn.Caption, dataColumn.DataType);
            }
            if (dstTable.Rows != null) dstTable.Rows.Clear();
            Regex regex = new Regex(@"^.*" + filter + ".*");
            int idx_name = allDataTable.Columns.IndexOf(nameCol);
            foreach (DataRow row in allDataTable.Rows)
            {
                var mat_name = regex.Match(row[idx_name].ToString());
                var mat_remark = regex.Match(row[idx_name + 1].ToString());
                if (mat_name.Success || mat_remark.Success)
                {
                    var newRow = dstTable.NewRow();
                    CopyRow(row, newRow);
                    dstTable.Rows.Add(newRow);
                }
            }
            return dstTable;
        }
        public void OnFilterTextChanged(object sender, TextChangedEventArgs args)
        {
            string str= ((TextBox)args.Source).Text;
            _newfilter = str;
            System.Threading.Monitor.Enter(Lock);
            if (_curfilter == _newfilter) return;
            _curfilter = _newfilter;
            Func<string, string, DataTable, DataTable> action = UpdateSearchResultTable;
            var tmp = action.BeginInvoke(_curfilter, _name, _allDataTable, null, null);
            DataTable t = action.EndInvoke(tmp)?.Copy();
            SearchResultDataTable.Clear();
            SearchResultDataTable = t;
            System.Threading.Monitor.Exit(Lock);
        }
        public void OnFilterTextSetUpdated()
        {

        }
        #endregion  UI Operation Deal End
        #region ContextMenu Area begin
        private List<MenuItem> _contextMenuItems;
        public List<MenuItem> ContextMenuItemsKWSLilin
        {
            get => _contextMenuItems;
            private set
            {
                _contextMenuItems = value;
            }
        }
        public ICommand SetSelectToCurrentStandard { get; set; }
        private void InitCmd()
        {
            SetSelectToCurrentStandard = new RelayCommand(ExeSetSelectToCurrentStandard);
        }
        private void InitContextMenu()
        {
            ContextMenuItemsKWSLilin = new List<MenuItem>
            {
                new MenuItem()
            };
            ContextMenuItemsKWSLilin.ElementAt(0).Header = S._("SetMatchToBeStdLilin");
            ContextMenuItemsKWSLilin.ElementAt(0).Command = SetSelectToCurrentStandard;
            ContextMenuItemsKWSLilin.ElementAt(0).InputGestureText = "Ctrl+M";
        }

        public Specimen CreateSampleFromRow(DataTable dataTable, int rowNum)
        {
            Specimen sample = new Specimen();
            ColorData colorData = new ColorData();
            WavelengthRange waveRange = new WavelengthRange(_begin, _end, _interval);
            Spectrum s = new Spectrum(waveRange);
            int col = dataTable.Columns.IndexOf(_b) + 1;
            for (int i = 0; i < waveRange.Count; i++)
            {
                s.Data[i] = (double)_spectraTable.Rows[rowNum].ItemArray.ElementAt(i);
            }

            colorData.Spectrum = s;
            if (_parent.CurrentSpecCom == "SCE") sample.SCE = colorData;
            else if (_parent.CurrentSpecCom == "SCI") sample.SCI = colorData;
            else sample.Clr = colorData;
            sample.Name = dataTable.Rows[rowNum].ItemArray.ElementAt(0).ToString();
            return sample;
        }
        private void ExeSetSelectToCurrentStandard(object obj)
        {
            DataGrid dataGrid = obj as DataGrid;
            var selectedItems = dataGrid?.SelectedItems.OfType<DataRowView>().ToArray();
            if (selectedItems == null || !selectedItems.Any())
            {
                return;
            }
            int num = _parent.SelectedItemInDataTablePos(selectedItems[0], _allDataTable);
            if (num != -1)
            {
                bool bl = MessageBoxShow.DialogSetToBeStandardEnsure(_allDataTable.Rows[num].ItemArray[0].ToString());
                if (!bl)
                {
                    return;
                }
                var sample = CreateSampleFromRow(_allDataTable, num);
                _parent.SetToBeGlobalStandardSpecimen(sample);
                _toastInfo.ShowToastInfo(S._("PanTongStdSuccessLilin") + ":" +
                    _allDataTable.Rows[num].ItemArray[0].ToString());
            }
            else
            {
                _toastInfo.ShowToastInfo(S._("PanTongStdFailLilin"));
            }
        }
        #endregion ContextMenu Area End
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void NotifyOfPropertyChange([CallerMemberName] string propertyName = null)//System.Runtime.CompilerServices
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
