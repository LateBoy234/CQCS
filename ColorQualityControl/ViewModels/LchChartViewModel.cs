using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using LiveCharts.Wpf;
using ThreeNH.ColorAlgorithm;

namespace ThreeNH.ColorQualityControl.ViewModels.Charts
{
    class LchChartViewModel : ColorChartViewModel
    {
        public LchChartViewModel()
            : base(new []{S._(" "), S._(" "), S._("L")},
                new []{-100.0, 100.0, -100.0, 100.0, 0.0, 100.0},
                (sample =>
                {
                    if (sample is IColorData clr && clr.Lab != null)
                        return new Tuple<double, double, double>(clr.Lab.a, clr.Lab.b, clr.Lab.L);
                    return null;
                }),
                ColorDiagrams.CieLabDiagram)
        {
            XyLabelPoint = point =>
            {
                var lab = new CIELAB(50.0, point.X, point.Y);
                return $"C*: {lab.C:F2}, " +
                       $"h°: {lab.h:F2}";
            };
            ZLabelPoint = point => $"L*: {point.Y:F2}";

            // 圆圈
            for (int c = 20; c < 100; c += 20)
            {
                var circle = new Ellipse
                {
                    Fill = Brushes.Transparent,
                    StrokeThickness = 1,
                };
                circle.SetResourceReference(Shape.StrokeProperty, "TextBrush");

                VisualElements.Add(new MyVisualElement
                {
                    UIElement = circle,
                    X = 0.0,
                    Y = 0.0,
                    WidthByUnits = c*2,
                    HeightByUnits = c*2,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                });
            }

            // 十字线
            AxisXSections.Add(new AxisSection
            {
                Value = 0.0,
                StrokeThickness = 1,
                Stroke = Brushes.Black,
            });
            AxisYSections.Add(new AxisSection
            {
                Value = 0.0,
                StrokeThickness = 1,
                Stroke = Brushes.Black,
            });
        }
    }
}
