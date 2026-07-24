using System;
using System.Windows.Media;
using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;

namespace ThreeNH.ColorQualityControl.ViewModels.Charts
{
    class ColorDiffChartViewModel : TriaxialChartViewModel
    {
        public ColorDiffChartViewModel(string[] titles, double[] ranges,
            ImageSource background = null)
            : base(titles, ranges, background)
        {
            //XySeries = new ScatterSeries
            //{
            //    Values = XyPoints,
            //    PointGeometry = DefaultGeometries.Circle,
            //    Fill = Brushes.YellowGreen,
            //};
            //XySeries.SetBinding(Series.LabelPointProperty, nameof(XyLabelPoint));

            //ZSeries = new ScatterSeries
            //{
            //    Values = ZPoints,
            //    PointGeometry = DefaultGeometries.Circle,
            //    Fill = Brushes.YellowGreen,
            //};
            //ZSeries.SetBinding(Series.LabelPointProperty, nameof(ZLabelPoint));

            //XyChartSeriesCollection.Add(XySeries);
            //ZChartSeriesCollection.Add(ZSeries);

            VerticalLineOfTheCross = new AxisSection { Value = 0, StrokeThickness = 1 };
            VerticalLineOfTheCross.SetResourceReference(AxisSection.FillProperty, "TextBrush");

            AxisXSections.Add(VerticalLineOfTheCross);


            HorizontalLineOfTheCross = new AxisSection { Value = 0, StrokeThickness = 1 };
            HorizontalLineOfTheCross.SetResourceReference(AxisSection.FillProperty, "TextBrush");
            AxisYSections.Add(HorizontalLineOfTheCross);
        }

        //public Func<object, Tuple<double, double, double>> PointMapper;

        /// <summary>
        /// 中间十字线的水平线
        /// </summary>
        protected AxisSection HorizontalLineOfTheCross { get; private set; }
        /// <summary>
        /// 中间十字线的垂直线
        /// </summary>
        protected AxisSection VerticalLineOfTheCross { get; private set; }

        protected ScatterSeries XySeries { get; }

        protected ScatterSeries ZSeries { get; }

        protected ChartValues<ScatterPoint> XyPoints { get; } = new ChartValues<ScatterPoint>();

        protected ChartValues<ScatterPoint> ZPoints { get; } = new ChartValues<ScatterPoint>();

        public void SetDelta(ITriaxialPoint delta)
        {
            XyPoints.Clear();
            ZPoints.Clear();

            if (delta != null)
            {
                //var point = PointMapper(delta);
                XyPoints.Add(new ScatterPoint(delta.X, delta.Y));
                ZPoints.Add(new ScatterPoint(0, delta.Z));
            }

            if (IsAutoFitAxes)
            {
                AutoFitXyAxes();
                AutoFitZAxis();
            }
        }

        public override void AutoFitZAxis()
        {
            var bound = GetZChartMinBound();
            if (double.IsInfinity(bound.Height))
            {
                base.AutoFitZAxis();
            }
            else
            {
                var size = Math.Max(Math.Abs(bound.Bottom), Math.Abs(bound.Top));
                var range = ChartHelper.GetAutoFitAxisRange(AxisZMinRange, AxisZMaxRange, AxisZMinSeparatorStep, -size, size);
                AxisZMin = range.Item1;
                AxisZMax = range.Item2;
                AxisZSeparatorStep = range.Item3;
            }
        }

        public override void AutoFitXyAxes()
        {
            var bound = GetXyChartMinBound();
            if (double.IsInfinity(bound.Width) || double.IsInfinity(bound.Height))
            {
                base.AutoFitXyAxes();
            }
            else
            {
                var hsize = Math.Max(Math.Abs(bound.Left), Math.Abs(bound.Right));
                var vsize = Math.Max(Math.Abs(bound.Bottom), Math.Abs(bound.Top));
                if (Math.Abs(AxisXMinSeparatorStep - AxisYMinSeparatorStep) < 0.1E-6)
                {
                    hsize = vsize = Math.Max(hsize, vsize);
                }

                var xAxis = ChartHelper.GetAutoFitAxisRange(AxisXMinRange, AxisXMaxRange, AxisXMinSeparatorStep, -hsize, hsize);
                var yAxis = ChartHelper.GetAutoFitAxisRange(AxisYMinRange, AxisYMaxRange, AxisXMinSeparatorStep, -vsize, vsize);

                AxisXMin = xAxis.Item1;
                AxisXMax = xAxis.Item2;
                AxisXSeparatorStep = xAxis.Item3;

                AxisYMin = yAxis.Item1;
                AxisYMax = yAxis.Item2;
                AxisYSeparatorStep = yAxis.Item3;
            }
        }
    }
}