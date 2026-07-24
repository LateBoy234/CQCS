using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using LiveCharts.Wpf;
using LiveCharts;
using LiveCharts.Configurations;
using LiveCharts.Defaults;
using System.Collections.Generic;
using ThreeNH.ColorQualityControl.Adapter;
using ThreeNH.ColorSpecimen;
using ThreeNH.DataModel;
using System.Windows.Media;

namespace ThreeNH.ColorQualityControl.UI
{
    /// <summary>
    /// StatisticsPlotControl.xaml 的交互逻辑
    /// </summary>
    partial class StatisticsPlotControl : UserControl
    {
        class PointItem //: IObservableChartPoint
        {
            public int Index;
            public SpecimenWrap Test;
            public TestSpecimenAdapter Adapter {  get { return Test.Adapter as TestSpecimenAdapter; } }

            //public event Action PointChanged;
        }

        enum PointSharp
        {
            None,
            Circle,
            Cross,
            Diamond,
            Square,
            Triangle,
        }


        private ChartValues<PointItem> _chartValues = new ChartValues<PointItem>();
        // 属性名和属性提取器
        private Dictionary<string, Func<TestSpecimenAdapter, double>> _propertyExtractor =
            new Dictionary<string, Func<TestSpecimenAdapter, double>>();
        private Geometry[] _pointGeometries;
        // ΔE 容差上限虚线（不加入 _propertyExtractor）
        private LineSeries _deltaELimitSeries;
        private Tolerance _rangeTolerance;

        public StatisticsPlotControl()
        {
            _pointGeometries = new Geometry[]
            {
                //DefaultGeometries.None,
                DefaultGeometries.Circle,
                DefaultGeometries.Diamond,
                DefaultGeometries.Square,
                DefaultGeometries.Triangle,
                DefaultGeometries.Cross
            };

            InitializeComponent();

            _axisY.LabelFormatter = x => x.ToString("F1");
        }

        /// <summary>
        /// 统计图使用的容差（优先系统/标样主容差）；用于绘制 ΔE 上限虚线。
        /// </summary>
        internal Tolerance RangeTolerance
        {
            get { return _rangeTolerance; }
            set
            {
                _rangeTolerance = value;
                UpdateDeltaELimitLines();
                Chart.Update();
            }
        }

        /// <summary>
        /// 创建一个新的LineSeries
        /// </summary>
        /// <param name="title">LineSeries的名称</param>
        /// <param name="propertyExtractor">要统计的项</param>
        /// <returns>返回新创建的LineSeries</returns>
        LineSeries CreateSeries(string title, Func<TestSpecimenAdapter, double> propertyExtractor)
        {
            LineSeries series = new LineSeries();
            var mapper = Mappers.Xy<PointItem>()
                .X(pt => pt.Index)
                .Y(pt => propertyExtractor(pt.Adapter));

            // ΔE 系列：超差点改成红色填充/描边，而不是另叠一层 Scatter
            if (IsDeltaETitle(title))
            {
                mapper = mapper
                    .Fill(pt => IsDeltaEOutOfTolerance(pt, propertyExtractor, title)
                        ? Brushes.Red
                        : Brushes.Green)
                    .Stroke(pt => IsDeltaEOutOfTolerance(pt, propertyExtractor, title)
                        ? Brushes.Red
                        : Brushes.Green);
            }

            series.Configuration = mapper;
            series.Title = title;
            series.Values = _chartValues;
            series.LabelPoint = pt => string.Format("{0:F3}", pt.Y);
            series.Fill = Brushes.Transparent;

            return series;
        }

        bool IsDeltaEOutOfTolerance(
            PointItem point,
            Func<TestSpecimenAdapter, double> propertyExtractor,
            string title)
        {
            if (point?.Adapter == null || propertyExtractor == null)
            {
                return false;
            }

            double value = propertyExtractor(point.Adapter);
            if (double.IsNaN(value))
            {
                return false;
            }

            double? upper = ResolveDeltaEUpper(title);
            return upper != null && value > upper.Value;
        }

        internal void AddRange(IEnumerable<SpecimenWrap> tests)
        {
            int nextIndex = _chartValues.Count + 1;
            _chartValues.AddRange(from x in tests select new PointItem {Index = nextIndex++, Test = x});
            UpdateAxesRange();
        }

        internal void Add(SpecimenWrap test)
        {
            _chartValues.Add(new PointItem { Index = _chartValues.Count + 1, Test = test });
            foreach (var func in _propertyExtractor.Values)
            {
                var value = func(test.Adapter as TestSpecimenAdapter);
                if (value > _axisY.MaxValue)
                {
                    _axisY.MaxValue = Math.Ceiling(value);
                }
                else if (value < _axisY.MinValue)
                {
                    // 向下取整，把Y轴最小值往下拓宽，图表底部自动拉低
                    _axisY.MinValue = Math.Floor(value);
                }
            }
           // X 轴长度不够就自动拓宽
            if (_chartValues.Count > _axisX.MaxValue)
            {
                _axisX.MaxValue += 5;
            }

            UpdateDeltaELimitLines();
            Chart.Update();
        }

        internal void Remove(SpecimenWrap test)
        {

            int index = -1;
            for (int i = 0; i < _chartValues.Count; i++)
            {
                if (_chartValues[i].Test == test)
                {
                    index = i;
                    break;
                }
            }
            if (index != -1)
            {
                _chartValues.RemoveAt(index);
                // 将Index后的所有点的Index减一
                for (int i = index; i < _chartValues.Count; i++)
                {
                    _chartValues[i].Index = i + 1;
                }
            }

            UpdateAxesRange();
        }

        internal void Show(string title, Func<TestSpecimenAdapter, double> propertyExtractor)
        {
            if (IsShow(title))
            {
                //Hide(title);
                return;
            }
            _propertyExtractor[title] = propertyExtractor;
            var series = CreateSeries(title, propertyExtractor);
            // 用系列数字而非 Chart.Series.Count：后者可能含 ΔE 容差虚线
            series.PointGeometry = _pointGeometries[(_propertyExtractor.Count - 1) % _pointGeometries.Length];
            Chart.Series.Add(series);

            UpdateAxesRange();
        }

        internal void Hide(string title)
        {
            int index = -1;
            for (int i = 0; i < Chart.Series.Count; i++)
            {
                if (Chart.Series[i].Title == title)
                {
                    index = i;
                    break;
                }
            }

            if (index != -1)
            {
                Chart.Series.RemoveAt(index);
                _propertyExtractor.Remove(title);
            }

            UpdateAxesRange();
        }

        internal void HideAll()
        {
            RemoveDeltaELimitSeries();
            Chart.Series.Clear();
            _propertyExtractor.Clear();
        }

        internal void Clear()
        {
            _chartValues.Clear();
            UpdateAxesRange();
        }

        internal bool IsShow(string title)
        {
            return _propertyExtractor.ContainsKey(title);
        }

        internal string[] SeriesTitles
        {
            get { return _propertyExtractor.Keys.ToArray(); }
        }

        internal void Refresh()
        {
            UpdateDeltaELimitLines();
            Chart.Update();
        }

        internal void UpdateAxesRange()
        {
        // 先去掉容差线，避免参与 Y 轴极值计算
            RemoveDeltaELimitSeries();

            // TODO: 調整座標系的范圍
            if (_chartValues.Count > _axisX.MaxValue || _axisX.MaxValue - _chartValues.Count > 10)
            {
                int maxValue = (_chartValues.Count + 4) / 5 * 5;
                if (maxValue == 0)
                {
                    maxValue = 5;
                }

                if (maxValue > 20)
                {
                    if (maxValue <= 100)
                    {
                        _axisX.Separator.Step = (maxValue + 9) / 10;
                    }
                    else
                    {
                        _axisX.Separator.Step = maxValue / 100 * 10;
                    }
                }

                _axisX.MaxValue = maxValue;
            }

            if (_chartValues.Count > 0 && Chart.Series.Count > 0)
            {
                var maxValue = (from series in Chart.Series select series.Values.GetPoints(series).Max(p => p.Y)).Max();
                var minValue = (from series in Chart.Series select series.Values.GetPoints(series).Min(p => p.Y)).Min();

                maxValue = Math.Ceiling(maxValue);
                minValue = Math.Floor(minValue);
                if (maxValue == minValue)
                {
                    maxValue = maxValue + 1.0;
                    minValue = minValue - 1.0;
                }

                _axisY.MaxValue = maxValue;
                _axisY.MinValue = minValue;

                
                double step = Math.Ceiling((maxValue - minValue) / 6.0 * 10.0);
                _axisY.Separator.Step = (((int) step + 4) / 5 * 5) / 10.0;
            }

            UpdateDeltaELimitLines();
            Chart.Update();
        }

        static bool IsDeltaETitle(string title)
        {
            return !string.IsNullOrEmpty(title) && title == ("\u0394E*ab");
        }

        void RemoveDeltaELimitSeries()
        {
            if (_deltaELimitSeries == null)
            {
                return;
            }

            Chart.Series.Remove(_deltaELimitSeries);
            _deltaELimitSeries = null;
        }

        /// <summary>
        /// ΔE 显示时叠加灰色容差上限虚线；超差点颜色由 CreateSeries 的 Mapper 处理。
        /// </summary>
        void UpdateDeltaELimitLines()
        {
            RemoveDeltaELimitSeries();

            var deltaETitles = _propertyExtractor.Keys.Where(IsDeltaETitle).ToList();
            if (deltaETitles.Count == 0)
            {
                return;
            }

            double? upper = null;
            foreach (var title in deltaETitles)
            {
                var limit = ResolveDeltaEUpper(title);
                if (limit == null)
                {
                    continue;
                }

                if (upper == null || limit.Value > upper.Value)
                {
                    upper = limit;
                }
            }

            if (upper == null)
            {
                return;
            }

            double y = upper.Value;
            double xMin = _axisX.MinValue;
            double xMax = _axisX.MaxValue;
            if (xMax <= xMin)
            {
                xMax = xMin + 5;
            }

            _deltaELimitSeries = new LineSeries
            {
                Title = string.Format("ΔE={0:F1}", y),
                Values = new ChartValues<ObservablePoint>
                {
                    new ObservablePoint(xMin, y),
                    new ObservablePoint(xMax, y)
                },
                Fill = Brushes.Transparent,
                Stroke = Brushes.Gray,
                StrokeThickness = 0.8,
                StrokeDashArray = new DoubleCollection { 6, 3 },
                PointGeometry = DefaultGeometries.None,
                DataLabels = false,
                LineSmoothness = 0
            };
            Chart.Series.Add(_deltaELimitSeries);

            if (y > _axisY.MaxValue)
            {
                _axisY.MaxValue = Math.Ceiling(y);
            }
            if (y < _axisY.MinValue)
            {
                _axisY.MinValue = Math.Floor(y);
            }
        }

        double? ResolveDeltaEUpper(string title)
        {
            ChromaticityField field;
            try
            {
                field = title.ToChromaticityField();
            }
            catch (ArgumentException)
            {
                field = ChromaticityField.dEab;
            }

            if (_rangeTolerance != null)
            {
                var item = _rangeTolerance[field];
                if (item.Upper != null)
                {
                    return item.Upper;
                }
            }

            foreach (var point in _chartValues)
            {
                var test = point.Test?.Specimen as TestSpecimen;
                var primary = test?.StandardSpecimen?.ToleranceSetting?.PrimaryTolerance;
                if (primary == null)
                {
                    continue;
                }

                var item = primary[field];
                if (item.Upper != null)
                {
                    return item.Upper;
                }
            }

            try
            {
                return Tolerance.DefaultToleranceItem(field).Upper;
            }
            catch (ArgumentException)
            {
                return 1.0;
            }
        }
    }
}
