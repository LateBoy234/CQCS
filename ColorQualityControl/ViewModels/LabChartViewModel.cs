using System;
using LiveCharts.Wpf;
using Brushes = System.Windows.Media.Brushes;

namespace ThreeNH.ColorQualityControl.ViewModels.Charts
{
    sealed class LabChartViewModel : ColorChartViewModel
    {
        public LabChartViewModel()
            : base(new []{" ", " ", "L*"},
                new []{-100.0, 100.0, -100.0, 100.0, 0.0, 100.0},
                (sample =>
                {
                    if (sample is IColorData clr && clr.Lab != null) 
                        return new Tuple<double, double, double>(clr.Lab.a, clr.Lab.b, clr.Lab.L);
                    return null;
                }),
                ColorDiagrams.CieLabDiagram)
        {
            AxisXSection = new AxisSection
            {
                Value = 0.0,
                Stroke = Brushes.Black,
                StrokeThickness = 1
            };
            AxisYSection = new AxisSection
            {
                Value = 0.0,
                Stroke = Brushes.Black,
                StrokeThickness = 1.0
            };
            //XY轴线
            AxisXSections.Add(AxisXSection);
            AxisYSections.Add(AxisYSection);

            XyLabelPoint = point => $"a*: {point.X:F2}, b*: {point.Y:F2}";
            ZLabelPoint = point => $"L*: {point.Y:F2}";

            AxisXMinSeparatorStep = 1.0;
            AxisYMinSeparatorStep = 1.0;
            AxisZMinSeparatorStep = 1.0;

            AutoFitXyAxes();
            AutoFitZAxis();
        }

        public AxisSection AxisYSection { get; set; }

        public AxisSection AxisXSection { get; set; }

        static Tuple<double, double, double> GetPoint(IColorData sample)
        {
            if (sample is IColorData color)
            {
                return new Tuple<double, double, double>(color.Lab.a, color.Lab.b, color.Lab.L);
            }

            return null;
        }
    }
}
