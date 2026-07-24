using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using LiveCharts;
using LiveCharts.Wpf;
using ThreeNH.Optical;

namespace ThreeNH.ColorQualityControl.ViewModels.Charts
{
    public interface ITriaxialPoint
    {
        string Name { get; }

        double X { get; }

        double Y { get; }

        double Z { get; }
    }

    public class DefaultTriaxialPoint : ITriaxialPoint
    {
        public string Name { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public object Tag { get; set; }
    }

    public class TriaxialChartViewModel : INotifyPropertyChanged
    {
        private double _axisXMin = -100.0;
        private double _axisXMax = 100.0;
        private double _axisYMin = -100.0;
        private double _axisYMax = 100.0;
        private double _axisXSeparatorStep = 20.0;
        private double _axisYSeparatorStep = 20.0;
        private string _axisXTitle;
        private string _axisYTitle;
        private string _axisZTitle;
        private double _axisZSeparatorStep = 20.0;
        private double _axisZMin = 0.0;
        private double _axisZMax = 100.0;
        private Func<ChartPoint, string> _xyLabelPoint = point => string.Format("{0}, {1}",
            DefaultLabelFormatter(point.X), DefaultLabelFormatter(point.Y));
        private Func<ChartPoint, string> _zLabelPoint = point => DefaultLabelFormatter(point.Y);
        private Func<double, string> _xLabelFormatter;
        private Func<double, string> _yLabelFormatter;
        private Func<double, string> _zLabelFormatter;
        private bool _autoFitAxes = true;
        private LegendLocation _legendLocation = LegendLocation.Top;
        protected StandardObserver _observer = StandardObserver.CIE1964;
        protected StandardIlluminant _illuminant = StandardIlluminant.D65;
        private readonly Rectangle _background;

        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChange(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        //3.000 显示成 3，3.1415 显示成 3.1415，不固定小数位，只看实际有效数字。
        public static string DefaultLabelFormatter(double value)
        {
            int decimals = 0;
            while (Math.Abs(value - Math.Round(value, decimals)) > 0.00001)
            {
                decimals += 1;
            }

            return string.Format("{0:F" + decimals + "}", value);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="titles">坐标轴名称，分别对应X、Y、Z</param>
        /// <param name="ranges">坐标范围，分别对应X、Y、Z的范围，其中0,1为X的范围，2,3为Y的，4,5为Z的</param>
        /// <param name="pointExtractor">提点提取函数</param>
        /// <param name="background">背景图片</param>
        public TriaxialChartViewModel(string[] titles, double[] ranges,
            ImageSource background = null)
        {
            Trace.Assert(titles?.Length == 3);
            Trace.Assert(ranges?.Length == 6);

            _axisXTitle = titles[0];
            _axisYTitle = titles[1];
            _axisZTitle = titles[2];

            AxisXMinRange = ranges[0];
            AxisXMaxRange = ranges[1];
            AxisYMinRange = ranges[2];
            AxisYMaxRange = ranges[3];
            AxisZMinRange = ranges[4];
            AxisZMaxRange = ranges[5];

            _axisXMin = AxisXMinRange;
            _axisXMax = AxisXMaxRange;
            _axisYMin = AxisYMinRange;
            _axisYMax = AxisYMaxRange;
            _axisZMin = AxisZMinRange;
            _axisZMax = AxisZMaxRange;

            AxisXSeparatorStep = (AxisXMax - AxisXMin) / 10;
            AxisYSeparatorStep = (AxisYMax - AxisYMin) / 10;
            AxisZSeparatorStep = (AxisZMax - AxisZMin) / 10;

            // 添加Z轴背景
            AxisZSections.Add(new AxisSection
            {
                Value = _axisZMin,
                SectionWidth = _axisZMax - _axisZMin,
                Fill = new LinearGradientBrush(Colors.White, Colors.Black, new Point(0, 0), new Point(0, 1))
            });
            _background = new Rectangle
            {
                Fill = background != null ? (Brush)new ImageBrush(background) : Brushes.Transparent,
                Opacity = 0.75,
            };
            Panel.SetZIndex(_background, -1);

            VisualElements.Add(new MyVisualElement
            {
                X = AxisXMin,
                Y = AxisYMin,
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Right,
                UIElement = _background,
                WidthByUnits = AxisXMax - AxisXMin,
                HeightByUnits = AxisYMax - AxisYMin,
            });

            XLabelFormatter = DefaultLabelFormatter;
            YLabelFormatter = DefaultLabelFormatter;
            ZLabelFormatter = DefaultLabelFormatter;

        }

        public Brush Background
        {
            get { return _background.Fill; }
            set { _background.Fill = value; }
        }

        public Func<ChartPoint, string> XyLabelPoint
        {
            get => _xyLabelPoint;
            set
            {
                if (Equals(value, _xyLabelPoint)) return;
                _xyLabelPoint = value;
                RaisePropertyChange(nameof(XyLabelPoint));
            }
        }

        public Func<ChartPoint, string> ZLabelPoint
        {
            get => _zLabelPoint;
            set
            {
                if (Equals(value, _zLabelPoint)) return;
                _zLabelPoint = value;
                RaisePropertyChange(nameof(ZLabelPoint));
            }
        }



        /// <summary>
        /// 坐标轴网格间隔
        /// </summary>
        public double AxisXSeparatorStep
        {
            get => _axisXSeparatorStep;
            set
            {
                if (value.Equals(_axisXSeparatorStep)) return;
                _axisXSeparatorStep = value;
                RaisePropertyChange(nameof(AxisXSeparatorStep));
            }
        }

        public double AxisYSeparatorStep
        {
            get => _axisYSeparatorStep;
            set
            {
                if (value.Equals(_axisYSeparatorStep)) return;
                _axisYSeparatorStep = value;
                RaisePropertyChange(nameof(AxisYSeparatorStep));
            }
        }

        public double AxisZSeparatorStep
        {
            get => _axisZSeparatorStep;
            set
            {
                if (value.Equals(_axisZSeparatorStep)) return;
                _axisZSeparatorStep = value;
                RaisePropertyChange(nameof(AxisZSeparatorStep));
            }
        }

        public double AxisXMinRange { get; set; } = -100.0;

        public double AxisXMaxRange { get; set; } = 100.0;

        public double AxisYMinRange { get; set; } = -100.0;

        public double AxisYMaxRange { get; set; } = 100.0;

        public double AxisZMinRange { get; set; } = 0.0;

        public double AxisZMaxRange { get; set; } = 100.0;

        public double AxisXMin
        {
            get => _axisXMin;
            set
            {
                if (value.Equals(_axisXMin)) return;
                _axisXMin = value;
                RaisePropertyChange(nameof(AxisXMin));
            }
        }

        public double AxisXMax
        {
            get => _axisXMax;
            set
            {
                if (value.Equals(_axisXMax)) return;
                _axisXMax = value;
                RaisePropertyChange(nameof(AxisXMax));
            }
        }

        public double AxisYMin
        {
            get => _axisYMin;
            set
            {
                if (value.Equals(_axisYMin)) return;
                _axisYMin = value;
                RaisePropertyChange(nameof(AxisYMin));
            }
        }

        public double AxisYMax
        {
            get => _axisYMax;
            set
            {
                if (value.Equals(_axisYMax)) return;
                _axisYMax = value;
                RaisePropertyChange(nameof(AxisYMax));
            }
        }

        public double AxisZMin
        {
            get => _axisZMin;
            set
            {
                if (value.Equals(_axisZMin)) return;
                _axisZMin = value;
                RaisePropertyChange(nameof(AxisZMin));
            }
        }

        public double AxisZMax
        {
            get => _axisZMax;
            set
            {
                if (value.Equals(_axisZMax)) return;
                _axisZMax = value;
                RaisePropertyChange(nameof(AxisZMax));
            }
        }

        public string AxisXTitle
        {
            get => _axisXTitle;
            set
            {
                if (value == _axisXTitle) return;
                _axisXTitle = value;
                RaisePropertyChange(nameof(AxisXTitle));
            }
        }

        public string AxisYTitle
        {
            get => _axisYTitle;
            set
            {
                if (value == _axisYTitle) return;
                _axisYTitle = value;
                RaisePropertyChange(nameof(AxisYTitle));
            }
        }

        public string AxisZTitle
        {
            get => _axisZTitle;
            set
            {
                if (value == _axisZTitle) return;
                _axisZTitle = value;
                RaisePropertyChange(nameof(AxisZTitle));
            }
        }

        /// <summary>
        /// Z轴坐标图
        /// </summary>
        public SeriesCollection ZChartSeriesCollection { get; set; } = new SeriesCollection();

        /// <summary>
        /// XY轴坐标图
        /// </summary>
        public SeriesCollection XyChartSeriesCollection { get; set; } = new SeriesCollection();

        public SectionsCollection AxisXSections { get; set; } = new SectionsCollection();

        public SectionsCollection AxisYSections { get; set; } = new SectionsCollection();

        public SectionsCollection AxisZSections { get; set; } = new SectionsCollection();

        /// <summary>
        /// 缩小xy图
        /// </summary>
        /// <param name="centerPoint">缩放的中心点</param>
        protected virtual void ZoomOutXyChart(Point centerPoint)
        {

        }

        /// <summary>
        /// 放大xy图
        /// </summary>
        /// <param name="centerPoint">缩放的中心点</param>
        protected virtual void ZoomInXyChart(Point centerPoint)
        {
            var halfWidth = (AxisXMax - AxisXMin) / 4;
            var halfHeight = (AxisYMax - AxisYMin) / 4;

            var centerX = centerPoint.X;
            var centerY = centerPoint.Y;

            var xTuple = ChartHelper.GetAutoFitAxisRange(AxisXMinRange, AxisXMaxRange, AxisXMinSeparatorStep,
                centerX - halfWidth,
                centerX + halfWidth);

            var yTuple = ChartHelper.GetAutoFitAxisRange(AxisYMinRange, AxisYMaxRange, AxisYMinSeparatorStep,
                centerY - halfHeight,
                centerY + halfHeight);

            centerX = (xTuple.Item1 + xTuple.Item2) / 2;
            centerY = (yTuple.Item1 + yTuple.Item2) / 2;
            var half = Math.Min(xTuple.Item2 - xTuple.Item1, yTuple.Item2 - yTuple.Item1) / 2;
            var step = Math.Min(xTuple.Item3, yTuple.Item3);

            AxisXSeparatorStep = AxisYSeparatorStep = step;

            AxisXMin = centerX - half;
            AxisXMax = centerX + half;
            AxisYMin = centerY - half;
            AxisYMax = centerY + half;
        }

        /// <summary>
        /// 还原xy图的尺寸
        /// </summary>
        protected virtual void ResetXyChart()
        {
            if (IsAutoFitAxes)
            {
                AutoFitXyAxes();
            }
            else
            {
                FullScreenXyChart();
            }
        }

        // X,Y 坐标系双击处理函数
        // 左键双击放大，右键双击还原
        public void XyChart_OnMouseDoubleClick(CartesianChart chart, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Right)
            {
                // 双击右键还原
                ResetXyChart();
            }
            else if (e.ChangedButton == MouseButton.Left)
            {
                var position = e.GetPosition(chart);
                // 双击放大
                ZoomInXyChart(chart.ConvertToChartValues(position));
            }
            chart.Update();
        }



        // Z 坐标系双击处理函数
        // 左键双击放大，右键双击还原
        public void ZChart_OnMouseDoubleClick(CartesianChart chart, MouseButtonEventArgs e)
        {
            // 双击右键还原
            if (e.ChangedButton == MouseButton.Right)
            {
                if (IsAutoFitAxes)
                {
                    AutoFitZAxis();
                }
                else
                {
                    FullScreenZChart();
                }
            }
            else
            {
                // 双击左键放大

                var point = chart.ConvertToChartValues(e.GetPosition(chart));

                var half = Math.Round((AxisZMax - AxisZMin) / 4);
                var center = point.Y;
                var tuple = ChartHelper.GetAutoFitAxisRange(AxisZMinRange, AxisZMaxRange, AxisZMinSeparatorStep,
                    center - half, center + half);

                AxisZMin = tuple.Item1;
                AxisZMax = tuple.Item2;
                AxisZSeparatorStep = tuple.Item3;
            }
            chart.Update();
        }

        public bool IsXyBackgroundVisible
        {
            get { return _background.Visibility == Visibility.Visible; }
            set
            {
                _background.Visibility = value ? Visibility.Visible : Visibility.Hidden;
            }
        }

        public VisualElementsCollection VisualElements { get; set; } = new VisualElementsCollection();

        /// <summary>Z 轴绘图区叠加元素（不含刻度与轴标题区域）。</summary>
        public VisualElementsCollection ZVisualElements { get; set; } = new VisualElementsCollection();

        /// <summary>
        /// X轴的最小分隔，用于自动适应
        /// </summary>
        public double AxisXMinSeparatorStep { get; set; } = double.NaN;

        /// <summary>
        /// Y轴的最小分隔，用于自动适应
        /// </summary>
        public double AxisYMinSeparatorStep { get; set; } = double.NaN;

        /// <summary>
        /// Z轴的最小分隔，用于自动适应
        /// </summary>
        public double AxisZMinSeparatorStep { get; set; } = double.NaN;


        /// <summary>
        /// 自动调整XY轴
        /// </summary>
        public virtual void AutoFitXyAxes()
        {
            var bound = GetXyChartMinBound();

            var xAxis = ChartHelper.GetAutoFitAxisRange(AxisXMinRange, AxisXMaxRange, AxisXMinSeparatorStep, bound.Left, bound.Right);
            var yAxis = ChartHelper.GetAutoFitAxisRange(AxisYMinRange, AxisYMaxRange, AxisYMinSeparatorStep, bound.Top, bound.Bottom);

            var size = Math.Max(xAxis.Item2 - xAxis.Item1, yAxis.Item2 - yAxis.Item1);
            var separator = Math.Max(xAxis.Item3, yAxis.Item3);
            var xCenter = (xAxis.Item1 + xAxis.Item2) / 2;
            var yCenter = (yAxis.Item1 + yAxis.Item2) / 2;

            AxisXSeparatorStep = AxisYSeparatorStep = separator;
            AxisXMin = xCenter - size / 2;
            AxisYMin = yCenter - size / 2;
            AxisXMax = xCenter + size / 2;
            AxisYMax = yCenter + size / 2;
        }

        public Rect GetXyChartMinBound()
        {
            double xMax = double.NegativeInfinity;
            double yMin = double.PositiveInfinity;
            double yMax = double.NegativeInfinity;
            double xMin = double.PositiveInfinity;

            foreach (var series in XyChartSeriesCollection)
            {
                if (!series.IsSeriesVisible || series.Values.Count == 0)
                {
                    continue;
                }

                foreach (var point in series.Values.GetPoints(series))
                {
                    if (point.X < xMin)
                        xMin = point.X;
                    if (point.X > xMax)
                        xMax = point.X;
                    if (point.Y < yMin)
                        yMin = point.Y;
                    if (point.Y > yMax)
                        yMax = point.Y;
                }
            }

            return new Rect(new Point(xMin, yMin), new Point(xMax, yMax));
        }

        // 自动适应坐标轴范围
        public virtual void AutoFitZAxis()
        {
            var bound = GetZChartMinBound();

            var tuple = ChartHelper.GetAutoFitAxisRange(AxisZMinRange, AxisZMaxRange, AxisZMinSeparatorStep, bound.Top, bound.Bottom);
            AxisZMin = tuple.Item1;
            AxisZMax = tuple.Item2;
            AxisZSeparatorStep = tuple.Item3;
        }

        public Rect GetZChartMinBound()
        {
            double zMax = double.NegativeInfinity;
            double zMin = double.PositiveInfinity;
            foreach (var series in ZChartSeriesCollection)
            {
                if (!series.IsSeriesVisible || series.Values.Count == 0)
                {
                    continue;
                }

                foreach (var point in series.Values.GetPoints(series))
                {
                    if (point.Y < zMin)
                        zMin = point.Y;
                    if (point.Y > zMax)
                        zMax = point.Y;
                }
            }

            return new Rect(new Point(-1, zMin), new Point(1, zMax));
        }

        public Func<double, string> XLabelFormatter
        {
            get => _xLabelFormatter;
            set
            {
                if (Equals(value, _xLabelFormatter)) return;
                _xLabelFormatter = value;
                RaisePropertyChange(nameof(XLabelFormatter));
            }
        }

        public Func<double, string> YLabelFormatter
        {
            get => _yLabelFormatter;
            set
            {
                if (Equals(value, _yLabelFormatter)) return;
                _yLabelFormatter = value;
                RaisePropertyChange(nameof(YLabelFormatter));
            }
        }

        public Func<double, string> ZLabelFormatter
        {
            get => _zLabelFormatter;
            set
            {
                if (Equals(value, _zLabelFormatter)) return;
                _zLabelFormatter = value;
                RaisePropertyChange(nameof(ZLabelFormatter));
            }
        }

        public bool IsAutoFitAxes
        {
            get => _autoFitAxes;
            set
            {
                if (value == _autoFitAxes) return;
                _autoFitAxes = value;
                RaisePropertyChange(nameof(IsAutoFitAxes));

                if (value)
                {
                    AutoFitXyAxes();
                    AutoFitZAxis();
                }
            }
        }

        public LegendLocation LegendLocation
        {
            get => _legendLocation;
            set
            {
                if (value == _legendLocation) return;
                _legendLocation = value;
                RaisePropertyChange(nameof(LegendLocation));
            }
        }

        /// <summary>
        /// 为真时 Tooltip 不显示系列色块和系列名称
        /// </summary>
        public virtual bool UsePlainTooltip => false;

        /// <summary>
        /// 显示全屏
        /// </summary>
        public void FullScreen()
        {
            FullScreenXyChart();
            FullScreenZChart();
        }

        private void FullScreenZChart()
        {
            Tuple<double, double, double> range;

            range = ChartHelper.GetAutoFitAxisRange(AxisZMinRange, AxisZMaxRange, AxisZMinSeparatorStep,
                double.NegativeInfinity, double.PositiveInfinity);
            AxisZMin = range.Item1;
            AxisZMax = range.Item2;
            AxisZSeparatorStep = range.Item3;
        }

        private void FullScreenXyChart()
        {
            var range = ChartHelper.GetAutoFitAxisRange(AxisXMinRange, AxisXMaxRange, AxisXMinSeparatorStep,
                double.NegativeInfinity, double.PositiveInfinity);
            AxisXMin = range.Item1;
            AxisXMax = range.Item2;
            AxisXSeparatorStep = range.Item3;

            range = ChartHelper.GetAutoFitAxisRange(AxisYMinRange, AxisYMaxRange, AxisYMinSeparatorStep,
                double.NegativeInfinity, double.PositiveInfinity);
            AxisYMin = range.Item1;
            AxisYMax = range.Item2;
            AxisYSeparatorStep = range.Item3;
        }

        public virtual StandardObserver Observer
        {
            get => _observer;
            set
            {
                if (value == _observer) return;
                _observer = value;
                RaisePropertyChange(nameof(Observer));
            }
        }

        public virtual StandardIlluminant Illuminant
        {
            get => _illuminant;
            set
            {
                if (value == _illuminant) return;
                _illuminant = value;
                RaisePropertyChange(nameof(Illuminant));
            }
        }
    }
}
