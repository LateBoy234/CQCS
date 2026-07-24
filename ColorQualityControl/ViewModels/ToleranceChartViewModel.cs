using ControlzEx.Standard;
using LiveCharts;
using LiveCharts.Configurations;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using ThreeNH.ColorAlgorithm;
using ThreeNH.ColorQualityControl.ViewModels.Charts;
using ThreeNH.ColorSpecimen;
using ThreeNH.DataModel;
using Brushes = System.Windows.Media.Brushes;

namespace ThreeNH.ColorQualityControl.ViewModels
{
    /// <summary>
    /// 容差色平图 ViewModel。
    /// 左侧为 Δa-Δb 平面（中心为标样，红色方框为 ab 容差范围），右侧为 ΔL 亮度条（上白下灰，红色横线为 ΔL 上下限）。
    /// </summary>
    class ToleranceChartViewModel : ColorDiffChartViewModel
    {
        readonly ChromaticityField _xField;
        readonly ChromaticityField _yField;
        readonly ChromaticityField _zField;

        readonly ChartValues<ScatterPoint> _standardXyPoints = new ChartValues<ScatterPoint>();
        readonly ChartValues<ScatterPoint> _standardZPoints = new ChartValues<ScatterPoint>();
        readonly ChartValues<TolerancePlotPoint> _samplePoints = new ChartValues<TolerancePlotPoint>();

        Rectangle _toleranceRect;
        MyVisualElement _toleranceRectElement;

        // Z 轴绘图区边框（仅框住坐标区域，不含刻度与轴标题）
        const double ZChartPlotXMin = -0.6;
        const double ZChartPlotXMax = 0.6;
        Rectangle _zPlotBorderRect;
        MyVisualElement _zPlotBorderElement;

        // 四象限三角形背景（上黄/右红/下蓝/左绿）
        MyVisualElement _quadrantBackgroundElement;
        Canvas _quadrantCanvas;
        readonly Polygon[] _quadrantPolygons = new Polygon[4];
        readonly TextBlock[] _quadrantLabels = new TextBlock[4];

        ScatterSeries _standardXySeries;
        ScatterSeries _standardZSeries;
        ScatterSeries _sampleXySeries;
        ScatterSeries _sampleZSeries;

        Tolerance _tolerance;
        double _standardL = double.NaN;
        double _standardA = double.NaN;
        double _standardB = double.NaN;
        string _standardName;

        /// <summary>使用简洁 Tooltip（不显示系列色块和系列名称）</summary>
        public override bool UsePlainTooltip => true;

        protected ToleranceChartViewModel(string[] titles, double[] ranges,
            ChromaticityField xField, ChromaticityField yField, ChromaticityField zField,
            bool coloredCross = false)
            : base(titles, ranges)
        {
            _xField = xField;
            _yField = yField;
            _zField = zField;

            AxisXMinSeparatorStep = 0.05;
            AxisYMinSeparatorStep = 0.05;
            AxisZMinSeparatorStep = 0.05;

            // 不使用色度图背景图片，改用手绘四象限色块
            IsXyBackgroundVisible = false;

            ConfigureSeries();
            SetupQuadrantBackground(coloredCross);
            SetupColoredCross(coloredCross);
            SetupToleranceVisual();
            SetupZPlotBorder();

            // 替换基类的白-黑渐变，改为上白下灰的亮度条背景
            UpdateZLightnessBackground();
        }

        public static ToleranceChartViewModel CreateLab()
        {
            return new ToleranceChartViewModel(
                // Δa/Δb 名称改由四象限背景标注，轴标题仅保留 ΔL
                new[] { string.Empty, string.Empty, S._("ΔL*") },
                new[] { -10.0, 10.0, -10.0, 10.0, -10.0, 10.0 },
                ChromaticityField.da, ChromaticityField.db, ChromaticityField.dL,
                coloredCross: true);
        }


        public void SetTolerance(Tolerance tolerance)
        {
            _tolerance = tolerance;
            UpdateToleranceVisual();
            UpdateToleranceZSections();

            if (IsAutoFitAxes)
            {
                AutoFitXyAxes();
                AutoFitZAxis();
            }
        }

        public void SetData(IColorData standard, IEnumerable<IColorData> samples, Tolerance tolerance = null)
        {
            if (tolerance != null)
                SetTolerance(tolerance);

            _standardXyPoints.Clear();
            _standardZPoints.Clear();
            _samplePoints.Clear();

            if (standard == null || samples == null)
                return;

            // 标样位于 Δ 坐标系原点
            _standardXyPoints.Add(new ScatterPoint(0, 0));
            _standardZPoints.Add(new ScatterPoint(0, 0));

            if (standard is Specimen standardSpecimen)
                _standardName = standardSpecimen.Name;

            if (standard.Lab != null)
            {
                _standardL = standard.Lab.L;
                _standardA = standard.Lab.a;
                _standardB = standard.Lab.b;
            }

            foreach (var sample in samples)
            {
                var delta = GetDelta(standard, sample);
                if (delta == null)
                    continue;

                var name = sample is Specimen specimen ? specimen.Name : null;
                var lab = sample.Lab;
                _samplePoints.Add(new TolerancePlotPoint(
                    name,
                    delta.Item1, delta.Item2, delta.Item3,
                    GetJudgement(delta.Item1, delta.Item2, delta.Item3),
                    lab?.L ?? double.NaN,
                    lab?.a ?? double.NaN,
                    lab?.b ?? double.NaN));
            }

            if (IsAutoFitAxes)
            {
                AutoFitXyAxes();
                AutoFitZAxis();
            }
        }

        public void SetSample(IColorData standard, IColorData sample, Tolerance tolerance = null)
        {
            SetData(standard, sample != null ? new[] { sample } : null, tolerance);
        }

        public override void AutoFitXyAxes()
        {
            var bound = GetXyChartMinBound();
            IncludeToleranceInAxis(ref bound);

            if (double.IsInfinity(bound.Width) || double.IsInfinity(bound.Height))
            {
                base.AutoFitXyAxes();
                UpdateQuadrantBackground();
                return;
            }

            var hsize = Math.Max(Math.Abs(bound.Left), Math.Abs(bound.Right));
            var vsize = Math.Max(Math.Abs(bound.Top), Math.Abs(bound.Bottom));
            if (Math.Abs(AxisXMinSeparatorStep - AxisYMinSeparatorStep) < 1e-7)
                hsize = vsize = Math.Max(hsize, vsize);

            var xAxis = ChartHelper.GetAutoFitAxisRange(AxisXMinRange, AxisXMaxRange, AxisXMinSeparatorStep, -hsize, hsize);
            var yAxis = ChartHelper.GetAutoFitAxisRange(AxisYMinRange, AxisYMaxRange, AxisYMinSeparatorStep, -vsize, vsize);

            var size = Math.Max(xAxis.Item2 - xAxis.Item1, yAxis.Item2 - yAxis.Item1);
            var separator = Math.Max(xAxis.Item3, yAxis.Item3);
            var xCenter = (xAxis.Item1 + xAxis.Item2) / 2;
            var yCenter = (yAxis.Item1 + yAxis.Item2) / 2;

            AxisXSeparatorStep = AxisYSeparatorStep = separator;
            // 拓宽XY轴最大范围
            AxisXMin = CustomConvert(xCenter - size / 2);
            AxisXMax = CustomConvert(xCenter + size / 2);
            AxisYMin = CustomConvert(yCenter - size / 2);
            AxisYMax = CustomConvert(yCenter + size / 2);

            UpdateQuadrantBackground();
        }

        public override void AutoFitZAxis()
        {
            var bound = GetZChartMinBound();
            var zLower = GetToleranceLimit(_zField, true);
            var zUpper = GetToleranceLimit(_zField, false);

            var extent = Math.Max(
                Math.Max(Math.Abs(bound.Top), Math.Abs(bound.Bottom)),
                Math.Max(Math.Abs(zLower), Math.Abs(zUpper)));

            if (double.IsInfinity(extent) || extent < 1e-6)
                extent = Math.Max(Math.Abs(zUpper - zLower) / 2, 0.5);

            // 仅留约 20% 边距，避免 ΔL 轴范围过宽
            extent *= 1.2;

            var range = ChartHelper.GetAutoFitAxisRange(
                AxisZMinRange, AxisZMaxRange, AxisZMinSeparatorStep, -extent, extent);
            AxisZMin = CustomConvert(range.Item1);
            AxisZMax = CustomConvert(range.Item2);
            AxisZSeparatorStep = range.Item3;

            UpdateToleranceZSections();
        }

        /// <summary>
        /// 拓宽范围
        /// 正数：去掉小数 +1
        /// 负数：去掉小数 -1
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private int CustomConvert(double value)
        {
            if (value >= 0)
                return (int)Math.Truncate(value) + 1;
            else
                return (int)Math.Truncate(value) - 1;
        }

        void ConfigureSeries()
        {
            // 标样：蓝色菱形，位于原点
            _standardXySeries = new ScatterSeries
            {
                PointGeometry = DefaultGeometries.Diamond,
                Fill = Brushes.Blue,
                Values = _standardXyPoints,
            };
            _standardXySeries.SetResourceReference(Series.TitleProperty, "Standard");
            _standardXySeries.LabelPoint = FormatStandardXyLabel;

            _standardZSeries = new ScatterSeries
            {
                PointGeometry = DefaultGeometries.Diamond,
                Fill = Brushes.Blue,
                Values = _standardZPoints,
                MinPointShapeDiameter = 5,
                MaxPointShapeDiameter = 8,
            };
            _standardZSeries.SetResourceReference(Series.TitleProperty, "Standard");
            _standardZSeries.LabelPoint = FormatStandardZLabel;

            // 试样：绿色圆点（不合格为红色），按 Δa/Δb 坐标绘制
            var sampleMapper = Mappers.Xy<TolerancePlotPoint>()
                .X(p => p.X)
                .Y(p => p.Y)
                .Fill(p => p.IsPass ? Brushes.LawnGreen : Brushes.Red);

            _sampleXySeries = new ScatterSeries
            {
                PointGeometry = DefaultGeometries.Circle,
                Fill = Brushes.LawnGreen,
                Values = _samplePoints,
                Configuration = sampleMapper,
            };
            _sampleXySeries.SetResourceReference(Series.TitleProperty, "Test");
            _sampleXySeries.LabelPoint = FormatSampleXyLabel;
            _sampleXySeries.MinPointShapeDiameter = 6;
            _sampleXySeries.MaxPointShapeDiameter = 10;

            // 试样在亮度条上的投影点
            _sampleZSeries = new ScatterSeries
            {
                PointGeometry = DefaultGeometries.Circle,
                Fill = Brushes.LawnGreen,
                Values = _samplePoints,
                Configuration = Mappers.Xy<TolerancePlotPoint>()
                    .X(_ => 0.0)
                    .Y(p => p.Z)
                    .Fill(p => p.IsPass ? Brushes.LawnGreen : Brushes.Red),
            };
            _sampleZSeries.SetResourceReference(Series.TitleProperty, "Test");
            _sampleZSeries.LabelPoint = FormatSampleZLabel;
            _sampleZSeries.MinPointShapeDiameter = 5;
            _sampleZSeries.MaxPointShapeDiameter = 8;

            XyChartSeriesCollection.Add(_standardXySeries);
            XyChartSeriesCollection.Add(_sampleXySeries);
            ZChartSeriesCollection.Add(_standardZSeries);
            ZChartSeriesCollection.Add(_sampleZSeries);
        }

        /// <summary>
        /// 创建四象限三角形背景：上黄(+b)、右红(+a)、下蓝(-b)、左绿(-a)，在中心交汇。
        /// </summary>
        void SetupQuadrantBackground(bool enabled)
        {
            if (!enabled)
                return;

            // 0=上黄  1=右红  2=下蓝  3=左绿
            var fills = new[]
            {
                Color.FromArgb(110, 255, 255, 120),
                Color.FromArgb(110, 255, 160, 160),
                Color.FromArgb(110, 160, 200, 255),
                Color.FromArgb(110, 160, 240, 160),
            };

            //创建一个画布，超出边界的部分会被裁掉。
            _quadrantCanvas = new Canvas { ClipToBounds = true };
            //把背景放到最底层（在网格、容差框、数据点下面）。
            Panel.SetZIndex(_quadrantCanvas, -2);
            //画布大小变化时（坐标轴缩放后），重新计算四个三角形的顶点。
            _quadrantCanvas.SizeChanged += (s, e) => UpdateQuadrantPolygons(e.NewSize.Width, e.NewSize.Height);

            for (int i = 0; i < 4; i++)
            {
                _quadrantPolygons[i] = new Polygon
                {
                    Fill = new SolidColorBrush(fills[i]),
                    Stroke = Brushes.Transparent,
                };
                _quadrantCanvas.Children.Add(_quadrantPolygons[i]);
            }

            // 0=上Δb  1=右Δa  2=下-Δb  3=左-Δa，与四象限色块方向一致
            var labelTexts = new[] { "Δb*", "Δa*", "-Δb*", "-Δa*" };
            for (int i = 0; i < 4; i++)
            {
                _quadrantLabels[i] = new TextBlock
                {
                    Text = labelTexts[i],
                    FontSize = 11,
                    Foreground = Brushes.Gray,
                };
                Panel.SetZIndex(_quadrantLabels[i], 1);
                _quadrantCanvas.Children.Add(_quadrantLabels[i]);
            }

            // 锚点与色品图背景一致：X 锚左缘，Y 锚底缘，才能铺满整个 ab 平面
            _quadrantBackgroundElement = new MyVisualElement
            {
                UIElement = _quadrantCanvas,
                X = AxisXMin,
                Y = AxisYMin,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
            };
            VisualElements.Insert(0, _quadrantBackgroundElement);

            UpdateQuadrantBackground();
        }

        /// <summary>
        /// 根据当前坐标轴范围，将四象限三角形铺满 ab 平面。
        /// </summary>
        void UpdateQuadrantBackground()
        {
            if (_quadrantBackgroundElement == null)
                return;

            _quadrantBackgroundElement.X = AxisXMin;
            _quadrantBackgroundElement.Y = AxisYMin;
            _quadrantBackgroundElement.WidthByUnits = Math.Max(AxisXMax - AxisXMin, 0.001);
            _quadrantBackgroundElement.HeightByUnits = Math.Max(AxisYMax - AxisYMin, 0.001);

            UpdateQuadrantPolygons(_quadrantCanvas.ActualWidth, _quadrantCanvas.ActualHeight);
        }

        /// <summary>
        /// 更新四个三角形顶点（Canvas 坐标：左上为 xMin/yMax，右下为 xMax/yMin）。
        /// </summary>
        void UpdateQuadrantPolygons(double width, double height)
        {
            if (width <= 0 || height <= 0 || _quadrantPolygons[0] == null)
                return;

            var cx = width / 2;
            var cy = height / 2;

            // 上三角 (+b)
            _quadrantPolygons[0].Points = new PointCollection
            {
                new Point(cx, cy), new Point(0, 0), new Point(width, 0)
            };
            // 右三角 (+a)
            _quadrantPolygons[1].Points = new PointCollection
            {
                new Point(cx, cy), new Point(width, 0), new Point(width, height)
            };
            // 下三角 (-b)
            _quadrantPolygons[2].Points = new PointCollection
            {
                new Point(cx, cy), new Point(width, height), new Point(0, height)
            };
            // 左三角 (-a)
            _quadrantPolygons[3].Points = new PointCollection
            {
                new Point(cx, cy), new Point(0, height), new Point(0, 0)
            };

            UpdateQuadrantLabels(width, height, cx, cy);
        }

        /// <summary>
        /// 将 Δa/Δb 方向标注贴在四象限背景边缘，替代轴标题。
        /// </summary>
        void UpdateQuadrantLabels(double width, double height, double cx, double cy)
        {
            if (_quadrantLabels[0] == null)
                return;

            const double margin = 4;

            PlaceQuadrantLabel(_quadrantLabels[0], cx - MeasureLabelWidth(_quadrantLabels[0]) / 2, margin);
            PlaceQuadrantLabel(_quadrantLabels[1], width - margin - MeasureLabelWidth(_quadrantLabels[1]), cy - MeasureLabelHeight(_quadrantLabels[1]) / 2);
            PlaceQuadrantLabel(_quadrantLabels[2], cx - MeasureLabelWidth(_quadrantLabels[2]) / 2, height - margin - MeasureLabelHeight(_quadrantLabels[2]));
            PlaceQuadrantLabel(_quadrantLabels[3], margin, cy - MeasureLabelHeight(_quadrantLabels[3]) / 2);
        }

        static void PlaceQuadrantLabel(TextBlock label, double left, double top)
        {
            Canvas.SetLeft(label, left);
            Canvas.SetTop(label, top);
        }

        static double MeasureLabelWidth(TextBlock label)
        {
            label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            return label.DesiredSize.Width;
        }

        static double MeasureLabelHeight(TextBlock label)
        {
            label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            return label.DesiredSize.Height;
        }

        /// <summary>
        /// 设置中心十字线的四色渐变（绿-红 水平，黄-蓝 垂直），标识 a*b* 方向。
        /// </summary>
        void SetupColoredCross(bool coloredCross)
        {
            if (!coloredCross)
                return;

            HorizontalLineOfTheCross.Stroke = new LinearGradientBrush(
                new GradientStopCollection
                {
                    new GradientStop(Colors.Gray, 0.0),
                    new GradientStop(Colors.Gray, 0.5),
                    new GradientStop(Colors.Gray, 0.5),
                    new GradientStop(Colors.Gray, 1.0)
                }, 0.0);

            VerticalLineOfTheCross.Stroke = new LinearGradientBrush(
                new GradientStopCollection
                {
                    new GradientStop(Colors.Gray, 0.0),
                    new GradientStop(Colors.Gray, 0.5),
                    new GradientStop(Colors.Gray, 0.5),
                    new GradientStop(Colors.Gray, 1.0),
                }, 90.0);
        }

        /// <summary>
        /// 创建红色实线方框，表示 Δa 和 Δb 的容差范围。
        /// </summary>
        void SetupToleranceVisual()
        {
            _toleranceRect = new Rectangle
            {
                Fill = Brushes.Transparent,
                Stroke = Brushes.Red,
                StrokeThickness = 2,
            };
            Panel.SetZIndex(_toleranceRect, 1);

            _toleranceRectElement = new MyVisualElement
            {
                UIElement = _toleranceRect,
                X = 0.0,
                Y = 0.0,
                WidthByUnits = 1.0,
                HeightByUnits = 1.0,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            VisualElements.Add(_toleranceRectElement);
        }

        /// <summary>
        /// 根据容差设置更新红色方框的位置和大小。
        /// </summary>
        void UpdateToleranceVisual()
        {
            if (_toleranceRectElement == null)
                return;

            var xLower = GetToleranceLimit(_xField, true);
            var xUpper = GetToleranceLimit(_xField, false);
            var yLower = GetToleranceLimit(_yField, true);
            var yUpper = GetToleranceLimit(_yField, false);

            _toleranceRectElement.X = (xLower + xUpper) / 2;
            _toleranceRectElement.Y = (yLower + yUpper) / 2;
            _toleranceRectElement.WidthByUnits = Math.Max(xUpper - xLower, 0.001);
            _toleranceRectElement.HeightByUnits = Math.Max(yUpper - yLower, 0.001);
        }

        /// <summary>
        /// 更新亮度条背景（上白下灰）及 ΔL 容差的红色上下限标线。
        /// </summary>
        void UpdateToleranceZSections()
        {
            SetAxisZSections(BuildZSections(includeToleranceLines: true));
            UpdateZPlotBorder();
        }

        /// <summary>
        /// 亮度条背景：上半（ΔL&gt;0）白色，下半（ΔL&lt;0）灰色。
        /// </summary>
        void UpdateZLightnessBackground()
        {
            SetAxisZSections(BuildZSections(includeToleranceLines: false));
            UpdateZPlotBorder();
        }

        /// <summary>
        /// 在 Z 轴绘图区叠加边框，避免上白区与界面背景融为一体。
        /// </summary>
        void SetupZPlotBorder()
        {
            _zPlotBorderRect = new Rectangle
            {
                Fill = Brushes.Transparent,
                Stroke = Brushes.Black,
                StrokeThickness = 1,
            };
            Panel.SetZIndex(_zPlotBorderRect, 2);

            _zPlotBorderElement = new MyVisualElement
            {
                UIElement = _zPlotBorderRect,
                X = ZChartPlotXMin,
                Y = AxisZMin,
                WidthByUnits = ZChartPlotXMax - ZChartPlotXMin,
                HeightByUnits = Math.Max(AxisZMax - AxisZMin, 0.001),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
            };
            ZVisualElements.Add(_zPlotBorderElement);
        }

        void UpdateZPlotBorder()
        {
            if (_zPlotBorderElement == null)
                return;

            _zPlotBorderElement.X = ZChartPlotXMin;
            _zPlotBorderElement.Y = AxisZMin;
            _zPlotBorderElement.WidthByUnits = ZChartPlotXMax - ZChartPlotXMin;
            _zPlotBorderElement.HeightByUnits = Math.Max(AxisZMax - AxisZMin, 0.001);
        }

        /// <summary>
        /// 构建 Z 轴分区集合。图表绑定后不可 Clear/RemoveAt 原集合，必须整体替换。
        /// </summary>
        SectionsCollection BuildZSections(bool includeToleranceLines)
        {
            var sections = new SectionsCollection();

            // 下半灰色区域：从 zMin 到 0
            sections.Add(new AxisSection
            {
                Value = AxisZMin,
                SectionWidth = 0 - AxisZMin,
                Fill = new SolidColorBrush(Color.FromRgb(192, 192, 192)),
            });

            // 上半白色区域：从 0 到 zMax
            sections.Add(new AxisSection
            {
                Value = 0,
                SectionWidth = AxisZMax,
                Fill = Brushes.White,
            });

            if (includeToleranceLines)
            {
                sections.Add(CreateToleranceLine(GetToleranceLimit(_zField, true)));
                sections.Add(CreateToleranceLine(GetToleranceLimit(_zField, false)));
            }

            return sections;
        }

        /// <summary>
        /// 替换 Z 轴分区并通知 UI 重新绑定。
        /// </summary>
        void SetAxisZSections(SectionsCollection sections)
        {
            AxisZSections = sections;
            RaisePropertyChange(nameof(AxisZSections));
        }

        static AxisSection CreateToleranceLine(double value)
        {
            return new AxisSection
            {
                Value = value,
                Stroke = Brushes.Red,
                StrokeThickness = 1.5,
            };
        }

        void IncludeToleranceInAxis(ref Rect bound)
        {
            if (_tolerance == null)
                return;

            var xLower = GetToleranceLimit(_xField, true);
            var xUpper = GetToleranceLimit(_xField, false);
            var yLower = GetToleranceLimit(_yField, true);
            var yUpper = GetToleranceLimit(_yField, false);

            bound = Union(bound, xLower, yLower);
            bound = Union(bound, xUpper, yUpper);
        }

        void IncludeToleranceInZAxis(ref Rect bound)
        {
            if (_tolerance == null)
                return;

            var zLower = GetToleranceLimit(_zField, true);
            var zUpper = GetToleranceLimit(_zField, false);
            bound = Union(bound, -1, zLower);
            bound = Union(bound, 1, zUpper);
        }

        static Rect Union(Rect bound, double x, double y)
        {
            if (double.IsInfinity(bound.Width) || double.IsInfinity(bound.Height))
                return new Rect(new Point(x, y), new Point(x, y));

            var left = Math.Min(bound.Left, x);
            var right = Math.Max(bound.Right, x);
            var top = Math.Min(bound.Top, y);
            var bottom = Math.Max(bound.Bottom, y);
            return new Rect(new Point(left, top), new Point(right, bottom));
        }

        Tuple<double, double, double> GetDelta(IColorData standard, IColorData sample)
        {
            if (standard == null || sample == null)
                return null;

            CIEDE1976 deltaLab = null;
            DeltaXYZ deltaXyz = null;

            if (standard.Lab != null && sample.Lab != null)
                deltaLab = new CIEDE1976(sample.Lab, standard.Lab);

            if (standard.Xyz != null && sample.Xyz != null)
                deltaXyz = new DeltaXYZ(sample.Xyz, standard.Xyz);

            return new Tuple<double, double, double>(
                GetFieldValue(_xField, deltaLab, deltaXyz),
                GetFieldValue(_yField, deltaLab, deltaXyz),
                GetFieldValue(_zField, deltaLab, deltaXyz));
        }

        static double GetFieldValue(ChromaticityField field, CIEDE1976 deltaLab, DeltaXYZ deltaXyz)
        {
            if (deltaLab != null)
            {
                switch (field)
                {
                    case ChromaticityField.dL: return deltaLab.dL;
                    case ChromaticityField.da: return deltaLab.da;
                    case ChromaticityField.db: return deltaLab.db;
                    case ChromaticityField.dC: return deltaLab.dC;
                    case ChromaticityField.dH: return deltaLab.dH;
                }
            }

            if (deltaXyz != null)
            {
                switch (field)
                {
                    case ChromaticityField.dX: return deltaXyz.dX;
                    case ChromaticityField.dY: return deltaXyz.dY;
                    case ChromaticityField.dZ: return deltaXyz.dZ;
                    case ChromaticityField.dx: return deltaXyz.dx;
                    case ChromaticityField.dy: return deltaXyz.dy;
                }
            }

            return double.NaN;
        }

        double GetToleranceLimit(ChromaticityField field, bool lower)
        {
            if (_tolerance != null)
            {
                var item = _tolerance[field];
                var value = lower ? item.Lower : item.Upper;
                if (value != null)
                    return value.Value;
            }

            var defaultItem = Tolerance.DefaultToleranceItem(field);
            var defaultValue = lower ? defaultItem.Lower : defaultItem.Upper;
            return defaultValue ?? 0.0;
        }

        bool GetJudgement(double x, double y, double z)
        {
            return IsInTolerance(x, _xField)
                && IsInTolerance(y, _yField)
                && IsInTolerance(z, _zField);
        }

        bool IsInTolerance(double value, ChromaticityField field)
        {
            if (double.IsNaN(value))
                return false;

            var item = _tolerance != null ? _tolerance[field] : Tolerance.DefaultToleranceItem(field);
            if (item.Lower != null && value < item.Lower)
                return false;
            if (item.Upper != null && value > item.Upper)
                return false;
            return true;
        }

        string FormatStandardXyLabel(ChartPoint point)
        {
            return string.Format("L*={0:F2} a*={1:F2} b*={2:F2}",
                _standardL, _standardA, _standardB);
        }

        string FormatStandardZLabel(ChartPoint point)
        {
            return string.Format("L*={0:F2}", _standardL);
        }

        /// <summary>
        /// 根据 ChartPoint 查找对应试样数据（Key 失效时按坐标匹配）。
        /// </summary>
        TolerancePlotPoint FindSamplePoint(ChartPoint point)
        {
            if (point.Key >= 0 && point.Key < _samplePoints.Count)
                return _samplePoints[point.Key];

            foreach (var sample in _samplePoints)
            {
                if (Math.Abs(sample.X - point.X) < 1e-6 && Math.Abs(sample.Y - point.Y) < 1e-6)
                    return sample;
            }

            return null;
        }

        /// <summary>
        /// ab 平面 Tooltip：样品名称、合格状态、绝对 a*b* 值（非 Δa/Δb）。
        /// </summary>
        string FormatSampleXyLabel(ChartPoint point)
        {
            var plotPoint = FindSamplePoint(point);
            if (plotPoint == null)
                return " ";

            return string.Format("{0}\n{1}\na*={2:F2} b*={3:F2}",
                plotPoint.Name ?? string.Empty,
                plotPoint.IsPass ? S._("JudgementLevel_Pass") : S._("JudgementLevel_NG"),
                plotPoint.A, plotPoint.B);
        }

        /// <summary>
        /// 亮度条 Tooltip：仅显示样品 L* 绝对值。
        /// </summary>
        string FormatSampleZLabel(ChartPoint point)
        {
            TolerancePlotPoint plotPoint = null;
            if (point.Key >= 0 && point.Key < _samplePoints.Count)
                plotPoint = _samplePoints[point.Key];
            else
            {
                foreach (var sample in _samplePoints)
                {
                    if (Math.Abs(sample.Z - point.Y) < 1e-6)
                    {
                        plotPoint = sample;
                        break;
                    }
                }
            }

            if (plotPoint == null)
                return " ";

            return string.Format("L*={0:F2}", plotPoint.L);
        }

        /// <summary>
        /// 容差图数据点：存储 Δ 坐标、合格状态及绝对 Lab 值（用于 Tooltip）。
        /// </summary>
        sealed class TolerancePlotPoint
        {
            public TolerancePlotPoint(string name, double x, double y, double z, bool isPass,
                double l, double a, double b)
            {
                Name = name;
                X = x;
                Y = y;
                Z = z;
                IsPass = isPass;
                L = l;
                A = a;
                B = b;
            }

            public string Name { get; }
            /// <summary>Δ 坐标（如 Δa）</summary>
            public double X { get; }
            /// <summary>Δ 坐标（如 Δb）</summary>
            public double Y { get; }
            /// <summary>Δ 坐标（如 ΔL）</summary>
            public double Z { get; }
            public bool IsPass { get; }
            /// <summary>绝对 L* 值</summary>
            public double L { get; }
            /// <summary>绝对 a* 值</summary>
            public double A { get; }
            /// <summary>绝对 b* 值</summary>
            public double B { get; }
        }
    }
}
