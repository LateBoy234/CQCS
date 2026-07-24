using System;
using System.Windows.Media;
using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using System.Collections.Generic;
using ControlzEx.Standard;
using DocumentFormat.OpenXml.Drawing.Diagrams;

namespace ThreeNH.ColorQualityControl.ViewModels.Charts
{
    public class ColorChartViewModel : TriaxialChartViewModel
    {
        public ColorChartViewModel(string[] titles, double[] ranges,
            Func<IColorData, Tuple<double, double, double>> pointMapper,
            ImageSource background = null)
            : base(titles, ranges, background)
        {
            PointMapper = pointMapper;


            StandardXySeries = new ScatterSeries
            {
                PointGeometry = DefaultGeometries.Diamond,
                Fill = Brushes.Blue,
                Values = StandardXyPoints,
                //Title = S._("Standard"),
            };

            StandardXySeries.SetBinding(Series.LabelPointProperty, nameof(XyLabelPoint));
            StandardXySeries.SetResourceReference(Series.TitleProperty, "Standard");

            StandardZSeries = new ScatterSeries
            {
                PointGeometry = DefaultGeometries.Diamond,
                Fill = Brushes.Blue,
                Values = StandardZPoints,
                //                Title = S._("Standard"),
            };

            StandardZSeries.SetBinding(Series.LabelPointProperty, nameof(ZLabelPoint));
            StandardZSeries.SetResourceReference(Series.TitleProperty, "Standard");

            SampleXySeries = new ScatterSeries
            {
                PointGeometry = DefaultGeometries.Circle,
                Fill = Brushes.LawnGreen,
                Values = SampleXyPoints,
                //Title = S._("Sample"),
            };

            SampleXySeries.SetBinding(Series.LabelPointProperty, nameof(XyLabelPoint));
            SampleXySeries.SetResourceReference(Series.TitleProperty, "Test");

            SampleZSeries = new ScatterSeries
            {
                PointGeometry = DefaultGeometries.Circle,
                Fill = Brushes.LawnGreen,
                Values = SampleZPoints,
                Title = S._("Test"),
            };

            SampleZSeries.SetBinding(Series.LabelPointProperty, nameof(ZLabelPoint));
            SampleZSeries.SetResourceReference(Series.TitleProperty, "Test");

            XyChartSeriesCollection.Add(StandardXySeries);
            XyChartSeriesCollection.Add(SampleXySeries);
            ZChartSeriesCollection.Add(StandardZSeries);
            ZChartSeriesCollection.Add(SampleZSeries);
        }


        /// <summary>
        ///由Sample获取相应的坐标点 x,y,z
        /// </summary>
        /// <param name="sample">样品</param>
        /// <returns></returns>
        public Func<IColorData, Tuple<double, double, double>> PointMapper { get; set; }


        public virtual void SetStandard(IColorData standard)
        {
            StandardXyPoints.Clear();
            StandardZPoints.Clear();

            if (standard != null && PointMapper != null)
            {
                var point = PointMapper(standard);
                if (point != null)
                {
                    StandardXyPoints.Add(new ScatterPoint(point.Item1, point.Item2));
                    StandardZPoints.Add(new ScatterPoint(0, point.Item3));
                }
            }

            if (IsAutoFitAxes)
            {
                AutoFitXyAxes();
                AutoFitZAxis();
            }
        }

        public virtual void SetSample(IColorData sample)
        {
            SampleXyPoints.Clear();
            SampleZPoints.Clear();

            if (sample != null && PointMapper != null)
            {
                var point = PointMapper(sample);
                if (point != null)
                {
                    SampleXyPoints.Add(new ScatterPoint(point.Item1, point.Item2));
                    SampleZPoints.Add(new ScatterPoint(0, point.Item3));
                }
            }

            if (IsAutoFitAxes)
            {
                AutoFitXyAxes();
                AutoFitZAxis();
            }
        }

        public void SetData(IColorData standard, IEnumerable<IColorData> samples)
        {
            StandardXyPoints.Clear();
            StandardZPoints.Clear();

            if (standard != null && PointMapper != null)
            {
                var point = PointMapper(standard);
                if (point != null)
                {
                    StandardXyPoints.Add(new ScatterPoint(point.Item1, point.Item2, 1));
                    StandardZPoints.Add(new ScatterPoint(0, point.Item3, 1));
                }
            }

            SampleXyPoints.Clear();
            SampleZPoints.Clear();

            foreach (var sample in samples)
            {
                if (sample != null && PointMapper != null)
                {
                    var point = PointMapper(sample);
                    if (point != null)
                    {
                        SampleXyPoints.Add(new ScatterPoint(point.Item1, point.Item2));
                        SampleZPoints.Add(new ScatterPoint(0, point.Item3));
                    }
                }
            }
            if (IsAutoFitAxes)
            {
                AutoFitXyAxes();
                AutoFitZAxis();
            }
        }



        protected ScatterSeries StandardXySeries { get; set; }

        protected ScatterSeries StandardZSeries { get; set; }

        protected ScatterSeries SampleXySeries { get; set; }

        protected ScatterSeries SampleZSeries { get; set; }

        protected ChartValues<ScatterPoint> StandardXyPoints { get; } = new ChartValues<ScatterPoint>();

        protected ChartValues<ScatterPoint> StandardZPoints { get; } = new ChartValues<ScatterPoint>();

        public ChartValues<ScatterPoint> SampleZPoints { get; } = new ChartValues<ScatterPoint>();

        public ChartValues<ScatterPoint> SampleXyPoints { get; } = new ChartValues<ScatterPoint>();
    }
}
