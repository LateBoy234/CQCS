using System.Windows;
using System.Windows.Controls;
using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using System.Collections.Generic;
using System.Windows.Media;
using ThreeNH.ColorAlgorithm;
using ThreeNH.Optical;
using System;
using System.Diagnostics;
using System.Linq;

namespace ThreeNH.Controls
{
    /// <summary>
    /// SpectrumPlotControl.xaml 的交互逻辑
    /// </summary>
    public partial class SpectrumPlotControl : UserControl
    {
        List<Series> _StandardLineSeries = new List<Series>();
        List<Series> _TestLineSeries = new List<Series>();

        public SpectrumPlotControl()
        {
            YAxisTitle = "R%";

            InitializeComponent();
            chart.DataContext = this;
            Formula formula = new Formula(StandardIlluminant.D65, StandardObserver.CIE1964);
            LinearGradientBrush brush = new LinearGradientBrush();
            brush.StartPoint = new Point(0.0, 0.0);
            brush.EndPoint = new Point(1.0, 0.0);
            for (int wave = 400; wave <= 700; wave += 5)
            {
                CIEYxy yxy = ChromaticityPlotColors.get_Chromaticity(wave);
                var srgb = formula.XyzToSrgb(yxy.XYZ);
                //Trace.TraceInformation("XYZ:{0},{1},{2}, rgb:{3},{4},{5}", yxy.X, yxy.Y, yxy.Z, srgb.R, srgb.G, srgb.B);
                double offset = (wave - 400.0) / (700.0 - 400.0);
                brush.GradientStops.Add(new GradientStop(Color.FromRgb(srgb.R, srgb.G, srgb.B), offset));
            }

            SpectrumRectangle.Fill = brush;
        }

        //public static readonly DependencyProperty WavelengthRangeProperty =
        //    DependencyProperty.Register("WavelengthRange", typeof(WavelengthRange),
        //        typeof(SpectrumPlotControl), new PropertyMetadata(new WavelengthRange(400, 700, 10)));


        public static readonly DependencyProperty MinWavelengthProperty =
            DependencyProperty.Register("MinWavelength", typeof(int), typeof(SpectrumPlotControl),
                new PropertyMetadata(400));
        public int MinWavelength
        {
            get { return (int)GetValue(MinWavelengthProperty); }
            set { SetValue(MinWavelengthProperty, value); }
        }

        public static readonly DependencyProperty MaxWavelengthProperty =
            DependencyProperty.Register("MaxWavelength", typeof(int), typeof(SpectrumPlotControl),
                new PropertyMetadata(700));
        public int MaxWavelength
        {
            get { return (int)GetValue(MaxWavelengthProperty); }
            set { SetValue(MaxWavelengthProperty, value); }
        }

        public static readonly DependencyProperty WavelengthIntervalProperty =
            DependencyProperty.Register("WavelengthInterval", typeof(int), typeof(SpectrumPlotControl),
                new PropertyMetadata(50));
        public int WavelengthInterval
        {
            get { return (int)GetValue(WavelengthIntervalProperty); }
            set { SetValue(WavelengthIntervalProperty, value); }
        }

        public static readonly DependencyProperty YAxisTitleProperty =
            DependencyProperty.Register("YAxisTitle", typeof(string), typeof(SpectrumPlotControl));

        public string YAxisTitle
        {
            get { return (string)GetValue(YAxisTitleProperty); }
            set { SetValue(YAxisTitleProperty, value); }
        }

        /// <summary>
        /// 波长范围
        /// </summary>
        public WavelengthRange WavelengthRange
        {
            get
            {
                return new WavelengthRange(MinWavelength, MaxWavelength);
            }

            set
            {
                MinWavelength = value.Min;
                MaxWavelength = value.Max;
            }
        }


        public static readonly DependencyProperty MinReflectanceProperty =
            DependencyProperty.Register("MinReflectance", typeof(double),
                typeof(SpectrumPlotControl), new PropertyMetadata(0.0));

        public double MinReflectance
        {
            get { return (double)GetValue(MinReflectanceProperty); }
            set { SetValue(MinReflectanceProperty, value); }
        }

        public static readonly DependencyProperty MaxReflectanceProperty =
            DependencyProperty.Register("MaxReflectance", typeof(double),
                typeof(SpectrumPlotControl), new PropertyMetadata(100.0));

        public double MaxReflectance
        {
            get { return (double)GetValue(MaxReflectanceProperty); }
            set { SetValue(MaxReflectanceProperty, value); }
        }

        bool _isFillSeries = false;
        public bool IsFillSeries
        {
            get { return _isFillSeries; }
            set
            {
                if (value != _isFillSeries)
                {
                    _isFillSeries = true;
                    foreach (LineSeries series in chart.Series)
                    {
                        series.Fill = value ? null : Brushes.Transparent;
                    }
                }
            }
        }

        private LineSeries MakeSeries(Spectrum spectrum, string name, bool isStandard = false)
        {
            LineSeries series = new LineSeries();

            var values = new ChartValues<ObservablePoint>();
            for (int wave = spectrum.Range.Min; wave <= spectrum.Range.Max;
                wave += spectrum.Range.Step)
            {
                values.Add(new ObservablePoint(wave, spectrum[wave] * 100.0));
            }

            series.Values = values;
            series.Title = name;
            series.Tag = spectrum;
            series.StrokeThickness = isStandard ? 2 : 1;
            series.LabelPoint = pt => string.Format("{0:F2}", pt.Y);
            series.PointGeometry = DefaultGeometries.None;
            if (!IsFillSeries)
                series.Fill = Brushes.Transparent;
            return series;
        }


        void AdjustRange(Spectrum spectrum)
        {
            double max = spectrum.Data.Max() * 100;
            double min = spectrum.Data.Min() * 100;

            min = Math.Floor(min / 10) * 10;
            max = Math.Ceiling(max / 10) * 10;

            if (chart.Series.Count > 1)
            {
                MinReflectance = Math.Min(min, MinReflectance);
                MaxReflectance = Math.Max(max, MaxReflectance);
            }
            else
            {
                MinReflectance = min;
                MaxReflectance = max;
            }

            if (Math.Abs(MinReflectance - MaxReflectance) < 10)
            {
                MinReflectance = Math.Max(MinReflectance - 10, 0);
                MaxReflectance += 10;
            }

            min = 400;
            max = 700;
            foreach (var series in chart.Series)
            {
                var range = ((Spectrum)((Series)series).Tag).Range;
                if (range.Min < min)
                {
                    min = range.Min;
                }

                if (range.Max > max)
                {
                    max = range.Max;
                }
            }

            MinWavelength = (int)min;
            MaxWavelength = (int)max;
            if ((MaxWavelength - MinWavelength) % 60 == 0)
                WavelengthInterval = 60;
            else
                WavelengthInterval = 50;

        }

        private Brush _selectedStandardStroke = SystemColors.HighlightBrush;
        public Brush SelectedStandardStroke
        {
            get { return _selectedStandardStroke; }
            set
            {
                if (value != null)
                    _selectedStandardStroke = value;
                else
                    _selectedStandardStroke = SystemColors.HighlightBrush;
            }
        }
        private Brush _selectedTestStroke = Brushes.Green;
        public Brush SelectedTestStroke
        {
            get { return _selectedTestStroke; }
            set { _selectedTestStroke = value != null ? value : Brushes.Green; }
        }

        private Brush _defaultStandardStroke = Brushes.Blue;
        public Brush DefaultStandardStroke
        {
            get { return _defaultStandardStroke; }
            set { _defaultStandardStroke = value != null ? value : Brushes.Blue; }
        }
        private Brush _defaultTestStroke = Brushes.DarkGray;
        public Brush DefaultTestStroke
        {
            get { return _defaultTestStroke; }
            set { _defaultTestStroke = value != null ? value : Brushes.Black; }
        }

        public void AddStandard(Spectrum spectrum, string name, Brush stroke = null)
        {
            if (spectrum == null || spectrum.Data.ToList().Exists(p => double.IsInfinity(p) || double.IsNaN(p)))
                return;

            var series = MakeSeries(spectrum, name, true);
            series.Stroke = stroke == null ? DefaultStandardStroke : stroke;
            _StandardLineSeries.Add(series);
            chart.Series.Add(series);

            if (_StandardLineSeries.Count > 20)
            {
                var s = _StandardLineSeries[0];
                _StandardLineSeries.RemoveAt(0);
                chart.Series.Remove(s);
                _selectedIndex -= 1;
            }

            AdjustRange(spectrum);
        }

        public void AddTest(Spectrum spectrum, string name, Brush stroke = null)
        {
            if (spectrum == null)
                return;

            var series = MakeSeries(spectrum, name);
            series.Stroke = stroke == null ? DefaultTestStroke : stroke;
            _TestLineSeries.Add(series);
            chart.Series.Add(series);

            if (_TestLineSeries.Count > 20)
            {
                var s = _TestLineSeries[0];
                _TestLineSeries.RemoveAt(0);
                chart.Series.Remove(s);
                _selectedIndex -= 1;
            }

            AdjustRange(spectrum);
        }

        public void Remove(Spectrum spectrum)
        {

            int index = IndexOf(spectrum);
            if (index != -1)
            {
                chart.Series.RemoveAt(index);
            }
        }

        public void Clear()
        {
            chart.Series.Clear();
            _StandardLineSeries.Clear();
            _TestLineSeries.Clear();
        }

        public void RemoveAllStandards()
        {
            foreach (var series in _StandardLineSeries)
            {
                chart.Series.Remove(series);
            }
            _StandardLineSeries.Clear();
        }

        public void RemoveAllTests()
        {
            foreach (var series in _TestLineSeries)
            {
                chart.Series.Remove(series);
            }
            _TestLineSeries.Clear();
        }

        public int IndexOf(Spectrum spectrum)
        {
            for (int i = 0; i < chart.Series.Count; i++)
            {
                if ((chart.Series[i] as Series).Tag == spectrum)
                {
                    return i;
                }
            }
            return -1;
        }

        int _selectedIndex = -1;
        public int SelectedIndex
        {
            get { return _selectedIndex; }
            set
            {
                if (_selectedIndex == value)
                    return;

                if (_selectedIndex >= 0 && _selectedIndex < chart.Series.Count)
                {
                    LineSeries series = chart.Series[_selectedIndex] as LineSeries;
                    if (_StandardLineSeries.Contains(series))
                    {
                        series.Stroke = DefaultStandardStroke;
                        series.StrokeThickness = 2;
                    }
                    else
                    {
                        series.Stroke = DefaultTestStroke;
                        series.StrokeThickness = 1;
                    }
                }

                _selectedIndex = value;
                if (_selectedIndex >= 0 && _selectedIndex < chart.Series.Count)
                {
                    LineSeries series = chart.Series[_selectedIndex] as LineSeries;
                    if (_StandardLineSeries.Contains(series))
                    {
                        series.Stroke = SelectedStandardStroke;
                    }
                    else
                    {
                        series.Stroke = SelectedTestStroke;
                    }
                    series.StrokeThickness = 3;
                }
            }
        }

        public void Select(Spectrum spectrum)
        {
            if (spectrum != null)
                SelectedIndex = IndexOf(spectrum);
            else
                SelectedIndex = -1;
        }

        private void chart_LayoutUpdated(object sender, System.EventArgs e)
        {
            if (chart.IsInitialized && chart.Model.AxisX != null && chart.Model.AxisY != null)
            {
                double width1 = chart.Model.ControlSize.Width;
                double left = ChartFunctions.ToPlotArea(MinWavelength, AxisOrientation.X, chart.Model);
                double right = ChartFunctions.ToPlotArea(MaxWavelength, AxisOrientation.X, chart.Model);

                SpectrumRectangle.Margin = new Thickness(left, 0, width1 - right, 0);
            }
        }

        /// <summary>
        /// 自动调整反射率范围
        /// </summary>
        public void AdjustRange()
        {
            if (chart.Series.Count == 0)
                return;

            List<double> minRanges = new List<double>();
            List<double> maxRanges = new List<double>();

            foreach (LineSeries series in chart.Series)
            {
                Spectrum spectrum = series.Tag as Spectrum;
                maxRanges.Add(spectrum.Data.Max());
                minRanges.Add(spectrum.Data.Min());
            }

            double max = maxRanges.Max() * 100.0;
            double min = minRanges.Min() * 100.0;

            min = Math.Floor(min / 25) * 25;
            max = Math.Ceiling(max / 25) * 25;

            MinReflectance = min;
            MaxReflectance = max;
        }


    }
}
