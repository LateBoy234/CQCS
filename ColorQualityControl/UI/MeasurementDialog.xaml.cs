﻿﻿﻿using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Drawing.Charts;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ThreeNH.ColorAlgorithm;
using ThreeNH.ColorimeterInterface;
using ThreeNH.ColorQualityControl.Refactoring.Contracts;
using ThreeNH.ColorSpecimen;
using ThreeNH.DataModel;
using ThreeNH.Optical;
using Formula = ThreeNH.ColorAlgorithm.Formula;

namespace ThreeNH.ColorQualityControl.UI
{
    /// <summary>
    /// MeasurementDialog.xaml 的交互逻辑
    /// </summary>
    public partial class MeasurementDialog : Window, INotifyPropertyChanged
    {
        struct MeasurementWorkerArgument
        {
            public MeasurementWorkerArgument(int measuredCount, int times, int interval)
            {
                MeasuredCount = measuredCount;
                Times = times;
                Interval = interval;
            }

            public int MeasuredCount; // 已测次数
            public int Times; // 总共要测的次数
            public int Interval; // 测量间隔
        }

        private IMeasurement _colorimeter = null;
        private BackgroundWorker _worker = null;

        public MeasurementDialog(MainWindow mainWindow)
        {
            _colorimeter = mainWindow.Colorimeter;
            IntervalList = new int[31];
            for (int i = 0; i < 31; i++)
            {
                IntervalList[i] = i;
            }

            TimesList = new ushort[10];
            for (int i = 0; i < TimesList.Length; i++)
            {
                TimesList[i] = (ushort)(i + 1);
            }

            MainWindow = mainWindow;

            Loaded += OnLoaded;


            _worker = new BackgroundWorker();
            _worker.WorkerReportsProgress = true;
            _worker.WorkerSupportsCancellation = true;
            _worker.DoWork += DoMeasurementWork;
            _worker.ProgressChanged += MeasurementWorkerOnProgressChanged;
            _worker.RunWorkerCompleted += MeasurementWorkerOnRunWorkerCompleted;

            Database = mainWindow.Database;
            InitializeComponent();
            DataContext = this;

            Times = (ushort)MainWindow.SystemDefaultConfig.AveragementConfig.Times;
            Interval = MainWindow.SystemDefaultConfig.AveragementConfig.Interval;
            AutoSave = MainWindow.SystemDefaultConfig.AveragementConfig.IsAutoSave;
            AutoMeasure = MainWindow.SystemDefaultConfig.AveragementConfig.IsAutoMeasure;

            (_colorimeter as ISystemSetting)?.RefreshCaches();
            SpecularComponent = (_colorimeter as ISystemSetting)?.SpecularComponent ?? SpecularComponent.SCI;

            UpdateColorLabels();
            ShowColorColumns();
            InitializeMeasureDic();
        }

        void OnLoaded(object sender, EventArgs e)
        {
            if (AutoSave && StoreDeleteButton != null)
            {
                StoreDeleteButton.Content = AutoSave ? S._("Delete") : S._("Store");
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if ((string)MeasurementButton.Content != S._("Label_Measure"))
            {
                var dr = MessageBox.Show("正在测量中，是否终止当前测试？",
                               "询问",
                               MessageBoxButton.YesNo,
                               MessageBoxImage.Question);
                if (dr == MessageBoxResult.Yes)
                {
                    _worker.CancelAsync();                  // 发取消信号
                }
                else
                {
                    return;
                }
            }

            MainWindow.SystemDefaultConfig.AveragementConfig.Times = Times;
            MainWindow.SystemDefaultConfig.AveragementConfig.Interval = Interval;
            MainWindow.SystemDefaultConfig.AveragementConfig.IsAutoMeasure = AutoMeasure;
            MainWindow.SystemDefaultConfig.AveragementConfig.IsAutoSave = AutoSave;
            base.OnClosing(e);
        }


        private string _datetime;

        public string DateTime
        {
            get { return _datetime; }
            set
            {
                if (_datetime != value)
                {
                    _datetime = value;
                    NotifyPropertyChanged("DateTime");
                }
            }
        }

        private bool _isStandardMeasurement = true;

        public bool IsStandardMeasurement
        {
            get { return _isStandardMeasurement; }
            set { _isStandardMeasurement = value; }
        }

        public MainWindow MainWindow { get; set; }
        public SpecimenDatabase Database { get; set; }

        private Specimen _specimen;

        public Specimen Specimen
        {
            get { return _specimen; }
            set
            {
                if (_specimen != value)
                {
                    _specimen = value;
                    NotifyPropertyChanged("Specimen");

                    if (_specimen != null)
                    {
                        DateTime = _specimen.DateTime.ToString();
                    }
                    else
                    {
                        DateTime = null;
                    }

                    UpdateColorValues();
                    AddSpectrumToPlot(value, !IsAverageMeasurementRuning);
                }
            }
        }

        public bool IsShowSCI
        {
            get
            {
                return SpecularComponent == SpecularComponent.SCI ||
                       SpecularComponent == SpecularComponent.SCIAndSCE;
            }
        }

        public bool IsShowSCE
        {
            get
            {
                return SpecularComponent == SpecularComponent.SCE ||
                       SpecularComponent == SpecularComponent.SCIAndSCE;
            }
        }

        public bool IsShowClr
        {
            get { return SpecularComponent == SpecularComponent.Unknown; }
        }

        private void AddSpectrumToPlot(Specimen specimen, bool clearFirst = false)
        {
            if (clearFirst)
            {
                SpectrumPlotControl.Clear();
            }

            if (specimen != null)
            {
                string prefix = clearFirst ? "" : string.Format("{0}:", MeasuredCount);
                if (IsShowSCI && specimen.GetSpectrum(SpecularComponent.SCI) != null)
                {
                    SpectrumPlotControl.AddStandard(specimen.GetSpectrum(SpecularComponent.SCI),
                        prefix + "SCI", Brushes.Blue);
                }

                if (IsShowSCE && specimen.GetSpectrum(SpecularComponent.SCE) != null)
                {
                    SpectrumPlotControl.AddStandard(specimen.GetSpectrum(SpecularComponent.SCE),
                        prefix + "SCE", Brushes.Green);
                }

                if (IsShowClr && specimen.GetSpectrum(SpecularComponent.Unknown) != null)
                {
                    SpectrumPlotControl.AddStandard(specimen.GetSpectrum(SpecularComponent.Unknown),
                        prefix + "---", Brushes.Magenta);
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        private string _measurementTimesText;

        public string MeasurementTimesText
        {
            get { return _measurementTimesText; }
            private set
            {
                if (_measurementTimesText != value)
                {
                    _measurementTimesText = value;
                    NotifyPropertyChanged("MeasurementTimesText");
                }
            }
        }

        #region 颜色值的标签1，不同的表色系有不同的标签

        #region SCI
        private string _colorLabel1 = "L*";
        public string ColorLabel1
        {
            get { return _colorLabel1; }
            set
            {
                if (_colorLabel1 != value)
                {
                    _colorLabel1 = value;
                    NotifyPropertyChanged("ColorLabel1");
                }
            }
        }

        private string _colorLabel2 = "a*";
        public string ColorLabel2
        {
            get { return _colorLabel2; }
            set
            {
                if (_colorLabel2 != value)
                {
                    _colorLabel2 = value;
                    NotifyPropertyChanged("ColorLabel2");
                }
            }
        }

        private string _colorLabel3 = "b*";
        public string ColorLabel3
        {
            get { return _colorLabel3; }
            set
            {
                if (_colorLabel3 != value)
                {
                    _colorLabel3 = value;
                    NotifyPropertyChanged("ColorLabel3");
                }
            }
        }
        #endregion

        #region SCE
        private string _colorSce1 = "L*";
        public string ColorSCE1
        {
            get { return _colorSce1; }
            set
            {
                if (_colorSce1 != value)
                {
                    _colorSce1 = value;
                    NotifyPropertyChanged("ColorSCE1");
                }
            }
        }

        private string _colorSCE2 = "a*";
        public string ColorSCE2
        {
            get { return _colorSCE2; }
            set
            {
                if (_colorSCE2 != value)
                {
                    _colorSCE2 = value;
                    NotifyPropertyChanged("ColorSCE2");
                }
            }
        }

        private string _colorSCE3 = "b*";
        public string ColorSCE3
        {
            get { return _colorSCE3; }
            set
            {
                if (_colorSCE3 != value)
                {
                    _colorSCE3 = value;
                    NotifyPropertyChanged("ColorSCE3");
                }
            }
        }
        #endregion

        #region 其他Clr
        private string _colorClr1 = "L*";
        public string ColorClr1
        {
            get { return _colorClr1; }
            set
            {
                if (_colorClr1 != value)
                {
                    _colorClr1 = value;
                    NotifyPropertyChanged("_colorClr1");
                }
            }
        }

        private string _colorClr2 = "a*";
        public string ColorClr2
        {
            get { return _colorClr2; }
            set
            {
                if (_colorClr2 != value)
                {
                    _colorClr2 = value;
                    NotifyPropertyChanged("ColorClr2");
                }
            }
        }

        private string _colorClr3 = "b*";
        public string ColorClr3
        {
            get { return _colorClr3; }
            set
            {
                if (_colorClr3 != value)
                {
                    _colorClr3 = value;
                    NotifyPropertyChanged("ColorClr3");
                }
            }
        }
        #endregion


        #endregion


        #region SCIColor
        private string _SCIColorValue1 = "";

        public string SCIColorValue1
        {
            get { return _SCIColorValue1; }
            set
            {
                if (_SCIColorValue1 != value)
                {
                    _SCIColorValue1 = value;
                    NotifyPropertyChanged("SCIColorValue1");
                }
            }
        }

        private string _SCIColorValue2 = "";

        public string SCIColorValue2
        {
            get { return _SCIColorValue2; }
            set
            {
                if (_SCIColorValue2 != value)
                {
                    _SCIColorValue2 = value;
                    NotifyPropertyChanged("SCIColorValue2");
                }
            }
        }

        private string _SCIColorValue3 = "";

        public string SCIColorValue3
        {
            get { return _SCIColorValue3; }
            set
            {
                if (_SCIColorValue3 != value)
                {
                    _SCIColorValue3 = value;
                    NotifyPropertyChanged("SCIColorValue3");
                }
            }
        }
        #endregion

        #region SCEColor
        private string _SCEColorValue1 = "";

        public string SCEColorValue1
        {
            get { return _SCEColorValue1; }
            set
            {
                if (_SCEColorValue1 != value)
                {
                    _SCEColorValue1 = value;
                    NotifyPropertyChanged("SCEColorValue1");
                }
            }
        }

        private string _SCEColorValue2 = "";

        public string SCEColorValue2
        {
            get { return _SCEColorValue2; }
            set
            {
                if (_SCEColorValue2 != value)
                {
                    _SCEColorValue2 = value;
                    NotifyPropertyChanged("SCEColorValue2");
                }
            }
        }

        private string _SCEColorValue3 = "";

        public string SCEColorValue3
        {
            get { return _SCEColorValue3; }
            set
            {
                if (_SCEColorValue3 != value)
                {
                    _SCEColorValue3 = value;
                    NotifyPropertyChanged("SCEColorValue3");
                }
            }
        }
        #endregion

        #region ColorValue
        private string _ColorValue1 = "";

        public string ColorValue1
        {
            get { return _ColorValue1; }
            set
            {
                if (_ColorValue1 != value)
                {
                    _ColorValue1 = value;
                    NotifyPropertyChanged("ColorValue1");
                }
            }
        }

        private string _ColorValue2 = "";

        public string ColorValue2
        {
            get { return _ColorValue2; }
            set
            {
                if (_ColorValue2 != value)
                {
                    _ColorValue2 = value;
                    NotifyPropertyChanged("ColorValue2");
                }
            }
        }

        private string _ColorValue3 = "";

        public string ColorValue3
        {
            get { return _ColorValue3; }
            set
            {
                if (_ColorValue3 != value)
                {
                    _ColorValue3 = value;
                    NotifyPropertyChanged("ColorValue3");
                }
            }
        }
        #endregion


        private ColorSpace _colorSpace = ColorSpace.CIE_LAB;

        public ColorSpace ColorSpace
        {
            get { return _colorSpace; }
            set
            {
                if (_colorSpace != value)
                {
                    _colorSpace = value;
                    NotifyPropertyChanged("ColorSpace");
                    Times = (ushort)(MeasureDic.Count == 0 ? 0 : MeasureDic.Values.Min(v => v.Count));

                    UpdateColorLabels();
                    foreach (var item in MeasureDic)
                    {
                        item.Value.Clear();
                    }
                    for (int i = 0; i < Times; i++)
                    {
                        Specimen = specimens[i];
                       // UpdateColorValues();
                    }
                }
            }
        }

        private StandardIlluminant _illuminant = StandardIlluminant.D65;

        public StandardIlluminant Illuminant
        {
            get { return _illuminant; }
            set
            {
                if (_illuminant != value)
                {
                    _illuminant = value;
                    NotifyPropertyChanged("Illuminant");
                    Times = (ushort)(MeasureDic.Count == 0 ? 0 : MeasureDic.Values.Min(v => v.Count));
                    foreach (var item in MeasureDic)
                    {
                        item.Value.Clear();
                    }
                    for (int i = 0; i < Times; i++)
                    {
                        Specimen = specimens[i];
                       // UpdateColorValues();
                    }
                }
            }
        }

        private StandardObserver _observer = StandardObserver.CIE1964;

        public StandardObserver Observer
        {
            get { return _observer; }
            set
            {
                if (_observer != value)
                {
                    _observer = value;
                    NotifyPropertyChanged("Observer");
                    Times = (ushort)(MeasureDic.Count == 0 ? 0 : MeasureDic.Values.Min(v => v.Count));
                    foreach (var item in MeasureDic)
                    {
                        item.Value.Clear();
                    }
                    for (int i = 0; i < Times; i++)
                    {
                        Specimen = specimens[i];
                        // 触发更新
                       // UpdateColorValues();
                    }
                }
            }
        }

        private SpecularComponent _sc = SpecularComponent.SCIAndSCE;

        public SpecularComponent SpecularComponent
        {
            get { return _sc; }
            set
            {
                if (_sc != value)
                {
                    _sc = value;
                    NotifyPropertyChanged("SpecularComponent");
                }
            }
        }

        private GridLength _columnWidthOfSCI = new GridLength(100, GridUnitType.Star);

        public GridLength ColumnWidthOfSCI
        {
            get { return _columnWidthOfSCI; }
            set
            {
                if (_columnWidthOfSCI != value)
                {
                    _columnWidthOfSCI = value;
                    NotifyPropertyChanged("ColumnWidthOfSCI");
                }
            }
        }

        private GridLength _columnWidthOfSCE = new GridLength(100, GridUnitType.Star);

        public GridLength ColumnWidthOfSCE
        {
            get { return _columnWidthOfSCE; }
            set
            {
                if (_columnWidthOfSCE != value)
                {
                    _columnWidthOfSCE = value;
                    NotifyPropertyChanged("ColumnWidthOfSCE");
                }
            }
        }

        private GridLength _columnWidthOfClr = new GridLength(0, GridUnitType.Pixel);
        public GridLength ColumnWidthOfClr
        {
            get { return _columnWidthOfClr; }
            set
            {
                if (_columnWidthOfClr != value)
                {
                    _columnWidthOfClr = value;
                    NotifyPropertyChanged(nameof(ColumnWidthOfClr));
                }
            }
        }

        private void UpdateColorValues()
        {
            if (_sc == SpecularComponent.SCI)
            {
                UpdateSCIColorValues();
            }
            else if (_sc == SpecularComponent.SCE)
            {
                UpdateSCEColorValues();
            }
            else if (_sc == SpecularComponent.SCIAndSCE)
            {
                UpdateSCIColorValues();
                UpdateSCEColorValues();
            }
            else
            {
                UpdateClrColorValues();
            }
        }
        public Dictionary<ColorSpace, ObservableCollection<MeasureRow>> MeasureDic = new Dictionary<ColorSpace, ObservableCollection<MeasureRow>>();

        // 初始化字典
        public void InitializeMeasureDic()
        {
            foreach (ColorSpace colorSpace in Enum.GetValues(typeof(ColorSpace)))
            {
                if (colorSpace == ColorSpace.Unknown)
                {
                    continue;
                }
                MeasureDic[colorSpace] = new ObservableCollection<MeasureRow>();
            }
        }
        private void UpdateClrColorValues()
        {
            if (Specimen == null)
            {
                return;
            }

            var formula = Database.GetFormula(Observer, Illuminant);

            CIEXYZ xyz = null;
            if (Specimen.Clr != null && (xyz = Specimen.Clr.XYZ(formula)) != null)
            {
                if (MeasureDic.Any(kv => kv.Value.Count >= Times))
                {

                }
                else
                {
                    UpdateColorValue(SpecularComponent.Unknown, formula, xyz);
                }
                Rows = MeasureDic.TryGetValue(ColorSpace, out var list) ? list : new ObservableCollection<MeasureRow>();
                if (MeasureDic.Any(kv => kv.Value.Count >= Times))
                {
                    if (Rows.Any(r => r.Index == S._("Mean")))
                    {
                        Rows.Remove(Rows.Where(r => r.Index == S._("Mean")).FirstOrDefault());
                    }
                    //添加平均值
                    Rows.Add(
                  new MeasureRow
                  {
                      Index = S._("Mean"),
                      Value1 = Rows.Average(x => x.Value1),
                      Value2 = Rows.Average(x => x.Value2),
                      Value3 = Rows.Average(x => x.Value3),
                  }
                  );
                }
            }
        }

        private void AddMeasureRow(ColorSpace space, double v1, double v2, double v3,
                           SpecularComponent specular)
        {
            var row = new MeasureRow
            {
                Index = (MeasureDic[space].Count + 1).ToString(),
                Value1 = v1,
                Value2 = v2,
                Value3 = v3
            };

            // 只有当成分是 SCE 时才填充 SCE_* 字段
            if (specular == SpecularComponent.SCE)
            {
                row.SCE_Value1 = v1;
                row.SCE_Value2 = v2;
                row.SCE_Value3 = v3;
            }

            MeasureDic[space].Add(row);
        }
        private void UpdateColorValue(SpecularComponent specular, Formula formula, CIEXYZ xyz)
        {
            if (xyz != null)
            {
                CIELAB lab = formula.XyzToLab(xyz);
                CIELUV luv = formula.XyzToLuv(xyz);
                HunterLab hlab = formula.XyzToHunterLab(xyz);
                CIERGB rgb = Formula.XyzToCieRgb(xyz);
                MunsellHVC munsell = Formula.XyzToMunsell(xyz);
                sRGB sRGB = formula.XyzToSrgb(xyz);
                DIN99Lab dIN99 = formula.LabToDin99(lab);
                if (_sc == SpecularComponent.SCIAndSCE && specular == SpecularComponent.SCE)
                {
                    var labRow = MeasureDic[ColorSpace.CIE_LAB].FirstOrDefault(r => r.Index == (MeasureDic[ColorSpace.CIE_LAB].Count).ToString() && r.SCE_Value1 == 0);
                    if (labRow != null)
                    {
                        labRow.SCE_Value1 = lab.L;
                        labRow.SCE_Value2 = lab.a;
                        labRow.SCE_Value3 = lab.b;
                        Trace.TraceInformation("第{0}次测量，L值为{1}，a值为{2}，b值为{3}", MeasureDic[ColorSpace.CIE_LAB].Count, lab.L, lab.a, lab.b);
                    }

                    // ------------ CIE-LCH ------------
                    var lchRow = MeasureDic[ColorSpace.CIE_LCH]
                                .FirstOrDefault(r => r.Index == (MeasureDic[ColorSpace.CIE_LCH].Count).ToString() && r.SCE_Value1 == 0);
                    if (lchRow != null)
                    {
                        lchRow.SCE_Value1 = lab.L;
                        lchRow.SCE_Value2 = lab.C;
                        lchRow.SCE_Value3 = lab.h;
                    }

                    // ------------ CIE-LUV ------------
                    var luvRow = MeasureDic[ColorSpace.CIE_LUV]
                                .FirstOrDefault(r => r.Index == (MeasureDic[ColorSpace.CIE_LUV].Count).ToString() && r.SCE_Value1 == 0);
                    if (luvRow != null)
                    {
                        luvRow.SCE_Value1 = luv.L;
                        luvRow.SCE_Value2 = luv.u;
                        luvRow.SCE_Value3 = luv.v;
                    }

                    // ------------ CIE-XYZ ------------
                    var xyzRow = MeasureDic[ColorSpace.CIE_XYZ]
                                .FirstOrDefault(r => r.Index == (MeasureDic[ColorSpace.CIE_XYZ].Count).ToString() && r.SCE_Value1 == 0);
                    if (xyzRow != null)
                    {
                        xyzRow.SCE_Value1 = xyz.X;
                        xyzRow.SCE_Value2 = xyz.Y;
                        xyzRow.SCE_Value3 = xyz.Z;
                    }

                    // ------------ CIE-Yxy ------------
                    var yxyRow = MeasureDic[ColorSpace.CIE_Yxy]
                                .FirstOrDefault(r => r.Index == (MeasureDic[ColorSpace.CIE_Yxy].Count).ToString() && r.SCE_Value1 == 0);
                    if (yxyRow != null)
                    {
                        yxyRow.SCE_Value1 = xyz.Y;
                        yxyRow.SCE_Value2 = xyz.x;
                        yxyRow.SCE_Value3 = xyz.y;
                    }

                    // ------------ HUNTER-LAB ------------
                    var hlabRow = MeasureDic[ColorSpace.HUNTER_LAB]
                                .FirstOrDefault(r => r.Index == (MeasureDic[ColorSpace.HUNTER_LAB].Count).ToString() && r.SCE_Value1 == 0);
                    if (hlabRow != null)
                    {
                        hlabRow.SCE_Value1 = hlab.L;
                        hlabRow.SCE_Value2 = hlab.a;
                        hlabRow.SCE_Value3 = hlab.b;
                    }

                    // ------------ CIE-RGB ------------
                    var rgbRow = MeasureDic[ColorSpace.CIE_RGB]
                                .FirstOrDefault(r => r.Index == (MeasureDic[ColorSpace.CIE_RGB].Count).ToString() && r.SCE_Value1 == 0);
                    if (rgbRow != null)
                    {
                        rgbRow.SCE_Value1 = rgb.R;
                        rgbRow.SCE_Value2 = rgb.G;
                        rgbRow.SCE_Value3 = rgb.B;
                    }

                    // ------------ MUNSELL ------------
                    var munsellRow = MeasureDic[ColorSpace.MUNSELL]
                                .FirstOrDefault(r => r.Index == (MeasureDic[ColorSpace.MUNSELL].Count).ToString() && r.SCE_Value1 == 0);
                    if (munsellRow != null)
                    {
                        munsellRow.SCE_Value1 = munsell.H;
                        munsellRow.SCE_Value2 = munsell.V;
                        munsellRow.SCE_Value3 = munsell.C;
                    }

                    var bxyRow = MeasureDic[ColorSpace.BETA_xy]
                              .FirstOrDefault(r => r.Index == (MeasureDic[ColorSpace.BETA_xy].Count).ToString() && r.SCE_Value1 == 0);
                    if (bxyRow != null)
                    {
                        bxyRow.SCE_Value1 = xyz.Y / 100.0;
                        bxyRow.SCE_Value2 = xyz.x;
                        bxyRow.SCE_Value3 = xyz.y;
                    }

                    // ------------ sRGB ------------
                    var sRGBRow = MeasureDic[ColorSpace.sRGB]
                                .FirstOrDefault(r => r.Index == (MeasureDic[ColorSpace.sRGB].Count).ToString() && r.SCE_Value1 == 0);
                    if (sRGBRow != null)
                    {
                        sRGBRow.SCE_Value1 = sRGB.R;
                        sRGBRow.SCE_Value2 = sRGB.G;
                        sRGBRow.SCE_Value3 = sRGB.B;
                    }

                    // ------------ DIN99_LAB ------------
                    var din99_labRow = MeasureDic[ColorSpace.DIN99_LAB]
                                .FirstOrDefault(r => r.Index == (MeasureDic[ColorSpace.DIN99_LAB].Count).ToString() && r.SCE_Value1 == 0);
                    if (din99_labRow != null)
                    {
                        din99_labRow.SCE_Value1 = dIN99.L;
                        din99_labRow.SCE_Value2 = dIN99.a;
                        din99_labRow.SCE_Value3 = dIN99.b;
                    }

                    // ------------ DIN99_LCh ------------
                    var din99_lchRow = MeasureDic[ColorSpace.DIN99_LCh]
                                .FirstOrDefault(r => r.Index == (MeasureDic[ColorSpace.DIN99_LCh].Count).ToString() && r.SCE_Value1 == 0);
                    if (din99_lchRow != null)
                    {
                        din99_lchRow.SCE_Value1 = dIN99.L;
                        din99_lchRow.SCE_Value2 = dIN99.C;
                        din99_lchRow.SCE_Value3 = dIN99.h;
                    }
                }
                else
                {
                    AddMeasureRow(ColorSpace.CIE_LAB, lab.L, lab.a, lab.b, specular);
                    AddMeasureRow(ColorSpace.CIE_LCH, lab.L, lab.C, lab.h, specular);
                    AddMeasureRow(ColorSpace.CIE_LUV, luv.L, luv.u, luv.v, specular);
                    AddMeasureRow(ColorSpace.CIE_XYZ, xyz.X, xyz.Y, xyz.Z, specular);
                    AddMeasureRow(ColorSpace.CIE_Yxy, xyz.Y, xyz.x, xyz.y, specular);
                    AddMeasureRow(ColorSpace.HUNTER_LAB, hlab.L, hlab.a, hlab.b, specular);
                    AddMeasureRow(ColorSpace.CIE_RGB, rgb.R, rgb.G, rgb.B, specular);
                    AddMeasureRow(ColorSpace.MUNSELL, munsell.H, munsell.V, munsell.C, specular);
                    AddMeasureRow(ColorSpace.DIN99_LCh, dIN99.L, dIN99.C, dIN99.h, specular);
                    AddMeasureRow(ColorSpace.DIN99_LAB, dIN99.L, dIN99.a, dIN99.b, specular);
                    AddMeasureRow(ColorSpace.sRGB, sRGB.R, sRGB.G, sRGB.B, specular);
                    AddMeasureRow(ColorSpace.BETA_xy, xyz.Y / 100.0, xyz.x, xyz.y, specular);
                }

                Rows = MeasureDic.TryGetValue(ColorSpace, out var list) ? list : new ObservableCollection<MeasureRow>();
                if (MeasureDic.Any(kv => kv.Value.Count >= Times))
                {
                    if (Rows.Any(r => r.Index == S._("Mean")))
                    {
                        Rows.Remove(Rows.Where(r => r.Index == S._("Mean")).FirstOrDefault());
                    }
                    //添加平均值,如果I+E，算完SCE再算SCI的平均值
                    if (_sc == SpecularComponent.SCIAndSCE && specular == SpecularComponent.SCI)
                    {
                        return;
                    }
                    Rows.Add(
                  new MeasureRow
                  {
                      Index = S._("Mean"),
                      Value1 = Rows.Average(x => x.Value1),
                      Value2 = Rows.Average(x => x.Value2),
                      Value3 = Rows.Average(x => x.Value3),
                  }
                  );
                }
            }
        }

        private void UpdateSCIColorValues()
        {
            if (Specimen == null)
            {
                SCIColorValue1 = SCIColorValue2 = SCIColorValue3 = null;
                PseudoColorOfSCI = null;
                return;
            }

            var formula = Database.GetFormula(Observer, Illuminant);

            CIEXYZ xyz = null;
            if (Specimen.SCI != null && (xyz = Specimen.SCI.XYZ(formula)) != null)
            {
                if (MeasureDic.Any(kv => kv.Value.Count >= Times))
                {
                }
                else
                {
                    UpdateColorValue(SpecularComponent.SCI, formula, xyz);
                }
                Rows = MeasureDic.TryGetValue(ColorSpace, out var list) ? list : new ObservableCollection<MeasureRow>();
                if (MeasureDic.Any(kv => kv.Value.Count >= Times))
                {
                    if (Rows.Any(r => r.Index == S._("Mean")))
                    {
                        Rows.Remove(Rows.Where(r => r.Index == S._("Mean")).FirstOrDefault());
                    }
                    //添加平均值
                    if (_sc == SpecularComponent.SCIAndSCE)
                    {
                        return;
                    }
                    Rows.Add(
                  new MeasureRow
                  {
                      Index = S._("Mean"),
                      Value1 = Rows.Average(x => x.Value1),
                      Value2 = Rows.Average(x => x.Value2),
                      Value3 = Rows.Average(x => x.Value3),
                  }
                  );
                }
            }
        }

        private void UpdateSCEColorValues()
        {
            if (Specimen == null)
            {
                return;
            }
            var formula = Database.GetFormula(Observer, Illuminant);
            CIEXYZ xyz = null;
            if (Specimen.SCE != null && (xyz = Specimen.SCE.XYZ(formula)) != null)
            {
                if (MeasureDic.Any(kv => kv.Value.Count > (_sc == SpecularComponent.SCIAndSCE ? Times : Times - 1)))
                {

                }
                else
                {
                    UpdateColorValue(SpecularComponent.SCE, formula, xyz);
                }

                Rows = MeasureDic.TryGetValue(ColorSpace, out var list) ? list : new ObservableCollection<MeasureRow>();
                if (MeasureDic.Any(kv => kv.Value.Count >= Times))
                {
                    if (Rows.Any(r => r.Index == S._("Mean")))
                    {
                        Rows.Remove(Rows.Where(r => r.Index == S._("Mean")).FirstOrDefault());
                    }
                    //添加平均值
                    Rows.Add(
                  new MeasureRow
                  {
                      Index = S._("Mean"),
                      SCE_Value1 = Rows.Average(x => x.SCE_Value1),
                      SCE_Value2 = Rows.Average(x => x.SCE_Value2),
                      SCE_Value3 = Rows.Average(x => x.SCE_Value3),
                      Value1 = Rows.Average(x => x.Value1),
                      Value2 = Rows.Average(x => x.Value2),
                      Value3 = Rows.Average(x => x.Value3),
                  }
                  );
                }

            }
        }
        /// <summary>
        /// 显示颜色列
        /// </summary>
        private void ShowColorColumns()
        {
            bool showSci = _sc == SpecularComponent.SCI || _sc == SpecularComponent.SCIAndSCE;
            bool showSce = _sc == SpecularComponent.SCE || _sc == SpecularComponent.SCIAndSCE;
            bool showClr = _sc == SpecularComponent.Unknown;

            dgc_sci1.Visibility = showSci ? Visibility.Visible : Visibility.Collapsed;
            dgc_sci2.Visibility = showSci ? Visibility.Visible : Visibility.Collapsed;
            dgc_sci3.Visibility = showSci ? Visibility.Visible : Visibility.Collapsed;
            dgc_sce1.Visibility = showSce ? Visibility.Visible : Visibility.Collapsed;
            dgc_sce2.Visibility = showSce ? Visibility.Visible : Visibility.Collapsed;
            dgc_sce3.Visibility = showSce ? Visibility.Visible : Visibility.Collapsed;
            dgc_Clr1.Visibility = showClr ? Visibility.Visible : Visibility.Collapsed;
            dgc_Clr2.Visibility = showClr ? Visibility.Visible : Visibility.Collapsed;
            dgc_Clr3.Visibility = showClr ? Visibility.Visible : Visibility.Collapsed;

            /* ---------- 1. 先把所有列恢复成固定 70 ---------- */
            foreach (var col in new[] { dgc_sci1, dgc_sci2, dgc_sci3,
                                dgc_sce1, dgc_sce2, dgc_sce3,dgc_Clr1,dgc_Clr2,dgc_Clr3 })
                col.Width = new DataGridLength(70);

            /* ---------- 2. 让最后一根可见列吃剩余 ---------- */
            var lastVisible = dg_date.Columns
                                     .Where(c => c.Visibility == Visibility.Visible)
                                     .LastOrDefault();
            if (lastVisible != null)
                lastVisible.Width = new DataGridLength(1, DataGridLengthUnitType.Star);

            /* ---------- 3. 计算总宽，锁死 DataGrid 宽度 ---------- */
            double fixedWidth = dg_date.Columns.Where(c => c.Visibility == Visibility.Visible)
                                              .Sum(c => c is DataGridTextColumn ? 70 : 0);   // 列宽
            const int rowHeaderWidth = 20;      // 行选择列
            const int borderMargin = 2;    // 左右边框 1+1
            dg_date.Width = fixedWidth + rowHeaderWidth + borderMargin;
        }

        /// <summary>
        /// 将标签转换为当前表色系的标签
        /// </summary>
        private void UpdateColorLabels()
        {
            string specularSCIName = "SCI_";
            switch (ColorSpace)
            {
                case ColorSpace.CIE_LAB:
                    ColorLabel1 = specularSCIName + "L*";
                    ColorLabel2 = specularSCIName + "a*";
                    ColorLabel3 = specularSCIName + "b*";
                    ColorSCE1 = "SCE_L*";
                    ColorSCE2 = "SCE_a*";
                    ColorSCE3 = "SCE_b*";

                    ColorClr1 = "L*";
                    ColorClr2 = "a*";
                    ColorClr3 = "b*";
                    break;
                case ColorSpace.CIE_LCH:
                    ColorLabel1 = specularSCIName + "L*";
                    ColorLabel2 = specularSCIName + "C*";
                    ColorLabel3 = specularSCIName + "h°";
                    ColorSCE1 = "SCE_L*";
                    ColorSCE2 = "SCE_C*";
                    ColorSCE3 = "SCE_h°";
                    ColorClr1 = "L*";
                    ColorClr2 = "C*";
                    ColorClr3 = "h°";
                    break;

                case ColorSpace.CIE_XYZ:
                    ColorLabel1 = specularSCIName + "X";
                    ColorLabel2 = specularSCIName + "Y";
                    ColorLabel3 = specularSCIName + "Z";
                    ColorSCE1 = "SCE_X";
                    ColorSCE2 = "SCE_Y";
                    ColorSCE3 = "SCE_Z";
                    ColorClr1 = "X";
                    ColorClr2 = "Y";
                    ColorClr3 = "Z";
                    break;

                case ColorSpace.CIE_Yxy:
                    ColorLabel1 = specularSCIName + "Y";
                    ColorLabel2 = specularSCIName + "x";
                    ColorLabel3 = specularSCIName + "y";
                    ColorSCE1 = "SCE_Y";
                    ColorSCE2 = "SCE_x";
                    ColorSCE3 = "SCE_y";
                    ColorClr1 = "Y";
                    ColorClr2 = "x";
                    ColorClr3 = "y";
                    break;

                case ColorSpace.CIE_LUV:
                    ColorLabel1 = specularSCIName + "L*";
                    ColorLabel2 = specularSCIName + "u*";
                    ColorLabel3 = specularSCIName + "v*";
                    ColorSCE1 = "SCE_L*";
                    ColorSCE2 = "SCE_u*";
                    ColorSCE3 = "SCE_v*";
                    ColorClr1 = "L*";
                    ColorClr2 = "u*";
                    ColorClr3 = "v*";
                    break;

                case ColorSpace.CIE_RGB:
                    ColorLabel1 = specularSCIName + "R";
                    ColorLabel2 = specularSCIName + "G";
                    ColorLabel3 = specularSCIName + "B";
                    ColorSCE1 = "SCE_R";
                    ColorSCE2 = "SCE_G";
                    ColorSCE3 = "SCE_B";
                    ColorClr1 = "R";
                    ColorClr2 = "G";
                    ColorClr3 = "B";
                    break;

                case ColorSpace.HUNTER_LAB:
                    ColorLabel1 = specularSCIName + "L";
                    ColorLabel2 = specularSCIName + "a";
                    ColorLabel3 = specularSCIName + "b";
                    ColorSCE1 = "SCE_L";
                    ColorSCE2 = "SCE_a";
                    ColorSCE3 = "SCE_b";
                    ColorClr1 = "L";
                    ColorClr2 = "a";
                    ColorClr3 = "b";
                    break;

                case ColorSpace.MUNSELL:
                    ColorLabel1 = specularSCIName + "H";
                    ColorLabel2 = specularSCIName + "V";
                    ColorLabel3 = specularSCIName + "C";
                    ColorSCE1 = "SCE_H";
                    ColorSCE2 = "SCE_V";
                    ColorSCE3 = "SCE_C";
                    ColorClr1 = "H";
                    ColorClr2 = "V";
                    ColorClr3 = "C";
                    break;

                case ColorSpace.DIN99_LAB:
                    ColorLabel1 = specularSCIName + "L";
                    ColorLabel2 = specularSCIName + "a";
                    ColorLabel3 = specularSCIName + "b";
                    ColorSCE1 = "SCE_L";
                    ColorSCE2 = "SCE_a";
                    ColorSCE3 = "SCE_b";
                    ColorClr1 = "L";
                    ColorClr2 = "a";
                    ColorClr3 = "b";
                    break;

                case ColorSpace.DIN99_LCh:
                    ColorLabel1 = specularSCIName + "L";
                    ColorLabel2 = specularSCIName + "C";
                    ColorLabel3 = specularSCIName + "h";
                    ColorSCE1 = "SCE_L";
                    ColorSCE2 = "SCE_C";
                    ColorSCE3 = "SCE_h";
                    ColorClr1 = "L";
                    ColorClr2 = "C";
                    ColorClr3 = "h";
                    break;

                case ColorSpace.sRGB:
                    ColorLabel1 = specularSCIName + "sR";
                    ColorLabel2 = specularSCIName + "sG";
                    ColorLabel3 = specularSCIName + "sB";
                    ColorSCE1 = "SCE_sR";
                    ColorSCE2 = "SCE_sG";
                    ColorSCE3 = "SCE_sB";
                    ColorClr1 = "sR";
                    ColorClr2 = "sG";
                    ColorClr3 = "sB";
                    break;

                case ColorSpace.BETA_xy:
                    ColorLabel1 = specularSCIName + "β";
                    ColorLabel2 = specularSCIName + "x";
                    ColorLabel3 = specularSCIName + "y";
                    ColorSCE1 = "SCE_β";
                    ColorSCE2 = "SCE_x";
                    ColorSCE3 = "SCE_y";
                    ColorClr1 = "β";
                    ColorClr2 = "x";
                    ColorClr3 = "y";
                    break;
            }
        }

        Brush _pseudoColorOfSCI = null;

        public Brush PseudoColorOfSCI
        {
            get { return _pseudoColorOfSCI; }
            set
            {
                _pseudoColorOfSCI = value;
                NotifyPropertyChanged("PseudoColorOfSCI");
            }
        }

        Brush _pseudoColorOfSCE = null;

        public Brush PseudoColorOfSCE
        {
            get { return _pseudoColorOfSCE; }
            set
            {
                _pseudoColorOfSCE = value;
                NotifyPropertyChanged("PseudoColorOfSCE");
            }
        }

        Brush _pseudoColorOfClr = null;

        public Brush PseudoColorOfClr
        {
            get { return _pseudoColorOfClr; }
            set
            {
                _pseudoColorOfClr = value;
                NotifyPropertyChanged(nameof(PseudoColorOfClr));
            }
        }

        // 1. 递归找子级（可视化树）
        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null) yield break;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = VisualTreeHelper.GetChild(depObj, i);
                if (child is T t) yield return t;
                foreach (var item in FindVisualChildren<T>(child)) yield return item;
            }
        }

        // 2. 统一设置 IsEnabled
        private void SetMeasureControlsEnabled(bool enabled)
        {
            // 找所有 ComboBox / CheckBox
            var boxes = FindVisualChildren<ComboBox>(this)
                       .Concat<Control>(FindVisualChildren<CheckBox>(this));

            foreach (var box in boxes)
                box.IsEnabled = enabled;
        }

        private void OnMeasureClick(object sender, RoutedEventArgs e)
        {
            // 如果正在测量，则暂停测量
            // 否则如果没有开始平均测量，则开始平均测量
            // 如果已开始平均测量但已暂停，则开启线程继续测量
            SetMeasureControlsEnabled(false);   // 禁用
            if (_worker.IsBusy)
            {
                MeasurementButton.IsEnabled = false;
                _worker.CancelAsync();
            }
            else
            {
                if (!IsAverageMeasurementRuning)
                {
                    StartAverageMeasurement();
                }

                MeasurementTimesText = string.Format(S._("Measuring_m_n"), MeasuredCount, Times);

                _worker.RunWorkerAsync(new MeasurementWorkerArgument(MeasuredCount,
                    AutoMeasure ? Times : MeasuredCount + 1, Interval));
                MeasurementButton.Content = S._("Label_PauseMeasuring");
                EndAvgButton.IsEnabled = false;
                AutoMeasureCheckBox.IsEnabled = false;
            }
        }

        private void StoreSpecimen(Specimen tmp)
        {
            if (IsStandardMeasurement)
            {
                StandardSpecimen std = new StandardSpecimen(tmp);
                MainWindow.AddStandard(std);
                // 仅设 CurrentStandard 不会切到单标样视图；与仪器测标同样走 ApplyMeasuredStandardResult
                MainWindow.ApplyMeasuredStandardResult(std);
                Specimen = std;
            }
            else
            {
                TestSpecimen test = new TestSpecimen(tmp);
                // IsAutoSelectStandard 开启时找最近标样；入库+切视图+选中一并交给仪器测样同一路径
                var bestStandard = ((IMeasurementService)MainWindow).GetBestStandard(tmp);
                MainWindow.ApplyMeasuredTestResult(test, bestStandard);
                Specimen = test;
            }
        }

        private bool _autoSave = true;

        public bool AutoSave
        {
            get { return _autoSave; }
            set
            {
                if (value != _autoSave)
                {
                    _autoSave = value;
                    NotifyPropertyChanged("AutoSave");

                    if (StoreDeleteButton != null)
                        StoreDeleteButton.Content = value ? S._("Delete") : S._("Store");
                }
            }
        }

        private bool _autoMeasure = true;

        public bool AutoMeasure
        {
            get { return _autoMeasure; }
            set
            {
                if (value != _autoMeasure)
                {
                    _autoMeasure = value;
                    NotifyPropertyChanged("AutoMeasure");
                }
            }
        }

        public int[] IntervalList { get; }

        public ushort[] TimesList { get; }


        private int _interval = 1;

        /// <summary>
        /// 测量间隔
        /// </summary>
        public int Interval
        {
            get { return _interval; }
            set
            {
                if (_interval != value)
                {
                    _interval = value;
                    NotifyPropertyChanged(nameof(Interval));
                }
            }
        }

        private ushort _times = 3;

        public ushort Times
        {
            get { return _times; }
            set
            {
                if (_times != value && value > 0)
                {
                    _times = value;
                    NotifyPropertyChanged(nameof(Times));
                }
            }
        }

        private void OnStoreDeleteButtonClick(object sender, RoutedEventArgs e)
        {
            if (Specimen == null)
                return;

            if (AutoSave && Specimen.ID > 0) // 为删除操作
            {
                if (IsStandardMeasurement)
                    MainWindow.RemoveStandard(Specimen as StandardSpecimen);
                else
                    MainWindow.RemoveTest(Specimen as TestSpecimen);
                Specimen = null;
            }

            if (!AutoSave && Specimen.ID == 0)
            {
                StoreSpecimen(_specimen);
            }
            Rows.Clear();
            StoreDeleteButton.IsEnabled = false;
        }

        /// <summary>
        /// 是否在进行平均测量
        /// </summary>
        bool IsAverageMeasurementRuning { get; set; }


        List<Specimen> _measuredSpecimens = new List<Specimen>();
        // 单次结果
        private ObservableCollection<MeasureRow> _rows = new ObservableCollection<MeasureRow>();
        public ObservableCollection<MeasureRow> Rows
        {
            get => _rows;
            private set               // 去掉 SetProperty，自己通知
            {
                if (ReferenceEquals(_rows, value)) return; // 可省
                _rows = value;
                NotifyPropertyChanged(nameof(Rows));      // 手动触发通知
            }
        }

        // 平均值一行
        private AverageRow _avgRow = new AverageRow();
        public AverageRow AvgRow
        {
            get => _avgRow;
            private set { _avgRow = value; NotifyPropertyChanged(nameof(AvgRow)); }
        }
        public int MeasuredCount
        {
            get { return Rows.Count; }
        }

        /// <summary>
        /// 开始平均测量
        /// </summary>
        void StartAverageMeasurement()
        {
            IsAverageMeasurementRuning = true;
            _measuredSpecimens.Clear();
            SpectrumPlotControl.Clear();
            Rows.Clear();
            foreach (var col in MeasureDic.Values)
            {
                col.Clear();
            }
            Specimen = null;
            StoreDeleteButton.IsEnabled = false;
        }

        /// <summary>
        /// 结束平均测量
        /// </summary>
        void StopAverageMeasurement()
        {
            IsAverageMeasurementRuning = false;
            if (_measuredSpecimens.Count != 0)
            {
                Specimen result = new Specimen(Specimen);
                result.SCI = result.SCE = null;

                if (IsShowSCI && _measuredSpecimens.All(_ => _.SCI != null))
                {
                    var __wr = ((ThreeNH.Instruments.DeviceInfo.IDeviceInfo)_colorimeter).WavelengthRange;
                    Spectrum spectrum = new Spectrum(new ThreeNH.ColorAlgorithm.WavelengthRange(__wr.Min, __wr.Max, __wr.Step));
                    foreach (var specimen in _measuredSpecimens)
                    {
                        for (int wavelength = spectrum.Range.Min;
                            wavelength <= spectrum.Range.Max;
                            wavelength += spectrum.Range.Step)
                        {
                            spectrum[wavelength] += specimen.SCI.Spectrum[wavelength];
                        }
                    }

                    for (int wavelength = spectrum.Range.Min;
                        wavelength <= spectrum.Range.Max;
                        wavelength += spectrum.Range.Step)
                    {
                        spectrum[wavelength] /= _measuredSpecimens.Count;
                    }

                    result.SCI = new ColorData(spectrum);
                }

                if (IsShowSCE && _measuredSpecimens.All(_ => _.SCE != null))
                {
                    var __wr = ((Instruments.DeviceInfo.IDeviceInfo)_colorimeter).WavelengthRange;
                    Spectrum spectrum = new Spectrum(new WavelengthRange(__wr.Min, __wr.Max, __wr.Step));
                    foreach (var specimen in _measuredSpecimens)
                    {
                        for (int wavelength = spectrum.Range.Min;
                            wavelength <= spectrum.Range.Max;
                            wavelength += spectrum.Range.Step)
                        {
                            spectrum[wavelength] += specimen.SCE.Spectrum[wavelength];
                        }
                    }

                    for (int wavelength = spectrum.Range.Min;
                        wavelength <= spectrum.Range.Max;
                        wavelength += spectrum.Range.Step)
                    {
                        spectrum[wavelength] /= _measuredSpecimens.Count;
                    }

                    result.SCE = new ColorData(spectrum);
                }

                result.Name = null;
                result.DateTime = System.DateTime.Now;

                if (AutoSave)
                {
                    StoreSpecimen(result);
                }
                else
                {
                    Specimen = result;
                }
            }

            MeasurementTimesText = null;
            StoreDeleteButton.IsEnabled = true;
            SetMeasureControlsEnabled(true);    // 恢复
        }

        private void OnAverageMeasurementButtonClick(object sender, RoutedEventArgs e)
        {
            StopAverageMeasurement();
            Times = (ushort)(MeasureDic.Values.First().Count-1);
            foreach (var item in MeasureDic)
            {
                item.Value.Clear();
            }
            foreach (var measuredspecimen in _measuredSpecimens)
            {
                Specimen = measuredspecimen;
               // UpdateColorValues();
            }
            MeasurementButton.Content = S._("Label_Measure");
            EndAvgButton.IsEnabled = false;
        }

        public List<Specimen> specimens = new List<Specimen>();

        private void DoMeasurementWork(object sender, DoWorkEventArgs args)
        {
            specimens.Clear();
            var worker = (BackgroundWorker)sender;
            var argument = (MeasurementWorkerArgument)args.Argument;
            try
            {
                for (int i = argument.MeasuredCount; i < argument.Times; i++)
                {
                    worker.ReportProgress((int)((float)(i + 1) / argument.Times * 100), null);
                    Specimen specimen = null;
                    if (_colorimeter is GratingSpectrometer.Spectrometer && (_colorimeter as IInstrument).OpticalStructure != "45/0")
                    {
                        specimen = (( param($mt) $map[$mt.Value] .Spectrometer)_colorimeter).Measure(((ISystemSetting)_colorimeter).SpecularComponent,
                            ((ISystemSetting)_colorimeter).UvMode);
                    }
                    else
                        specimen = _colorimeter.Measure();
                    worker.ReportProgress((int)((float)(i + 1) / argument.Times * 100), specimen);

                    specimens.Add(specimen);
                    if (worker.CancellationPending)
                    {
                        args.Cancel = true;
                        break;
                    }

                    if (i != argument.Times - 1 && Interval > 0)
                    {
                        Thread.Sleep(argument.Interval * 1000);
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }

        }


        private void MeasurementWorkerOnRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                if (!MainWindow.IsMeasurementAbnormalException(e.Error))
                    MessageBox.Show(e.Error.Message);
            }

            if (MeasuredCount < Times)
            {
                MeasurementButton.Content = S._("Label_ContinueMeasure");
                EndAvgButton.IsEnabled = MeasuredCount > 0;
            }
            else
            {
                StopAverageMeasurement();
                MeasurementButton.Content = S._("Label_Measure");
            }

            MeasurementButton.IsEnabled = true;
            AutoMeasureCheckBox.IsEnabled = true;
        }

        private void MeasurementWorkerOnProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            var tmp = e.UserState as Specimen;

            // 为空表示将测，非空表示已测
            if (tmp != null)
            {
                Specimen = tmp;
                _measuredSpecimens.Add(Specimen);
                MeasurementTimesText = string.Format(S._("Measured_m_n"), MeasuredCount, Times);
            }
            else if (MeasuredCount < Times)
            {
                string str = string.Format(S._("Measuring_m_n"), MeasuredCount + 1, Times);
                MeasurementTimesText = str;
            }
        }
    }

    public class AverageRow
    {
        public string ColorAvgValue1 { get; set; }
        public string ColorAvgValue2 { get; set; }
        public string ColorAvgValue3 { get; set; }
    }

    public class MeasureRow
    {
        public string Index { get; set; }
        public double Value1 { get; set; }
        public double Value2 { get; set; }
        public double Value3 { get; set; }
        public double SCE_Value1 { get; set; }
        public double SCE_Value2 { get; set; }
        public double SCE_Value3 { get; set; }
    }
}