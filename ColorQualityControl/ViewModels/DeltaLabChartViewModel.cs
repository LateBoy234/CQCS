using System.Windows.Media;

namespace ThreeNH.ColorQualityControl.ViewModels.Charts
{
    sealed class DeltaLabChartViewModel : ColorDiffChartViewModel
    {
        public DeltaLabChartViewModel()
            : base(new[] {S._("da"), S._("db"), S._("dL")},
                new[] {-10.0, 10.0, -10.0, 10.0, -10.0, 10.0})
        {
            AxisXMinSeparatorStep = 0.05;
            AxisYMinSeparatorStep = 0.05;
            AxisZMinSeparatorStep = 0.05;

            AxisXMin = -2.0;
            AxisXMax = 2.0;
            AxisXSeparatorStep = 0.5;

            AxisYMin = -2.0;
            AxisYMax = 2.0;
            AxisYSeparatorStep = 0.5;

            AxisZMin = -2.0;
            AxisZMax = 2.0;
            AxisZSeparatorStep = 0.5;

            HorizontalLineOfTheCross.Stroke = new LinearGradientBrush(
                new GradientStopCollection
                {
                    new GradientStop(Colors.Green, 0.0),
                    new GradientStop(Colors.Green, 0.5),
                    new GradientStop(Colors.Red, 0.5),
                    new GradientStop(Colors.Red, 1.0)
                }, 0.0);
            VerticalLineOfTheCross.Stroke = new LinearGradientBrush(
                new GradientStopCollection
                {
                    new GradientStop(Colors.Yellow, 0.0),
                    new GradientStop(Colors.Yellow, 0.5),
                    new GradientStop(Colors.Blue, 0.5),
                    new GradientStop(Colors.Blue, 1.0),
                }, 90.0);
        }
    }
}