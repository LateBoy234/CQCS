using System;
using System.Windows.Media;
using ThreeNH.Optical;

namespace ThreeNH.ColorQualityControl.ViewModels.Charts
{
    sealed class XyzChartViewModel : ColorChartViewModel
    {
        public XyzChartViewModel()
            : base(new[] {"x", "y", "Y"},
                new[] {-0.1, 0.8, 0.0, 0.9, 0.0, 100.0},
                (sample =>
                {
                    if (sample is IColorData clr && clr.Xyz != null) 
                        return new Tuple<double, double, double>(clr.Xyz.x, clr.Xyz.y, clr.Xyz.Y);
                    return null;
                }),
                ColorDiagrams.Cie1931DiagramObserver10)
        {
            AxisXSeparatorStep = 0.1;
            AxisYSeparatorStep = 0.1;

            XyLabelPoint = point => $"x: {point.X:F4}, y: {point.Y:F4}";
            ZLabelPoint = point => $"Y: {point.Y:F2}";

            AxisXMinSeparatorStep = 0.001;
            AxisYMinSeparatorStep = 0.001;
            AxisZMinSeparatorStep = 0.1;

            AutoFitXyAxes();
            AutoFitZAxis();
        }


        public override StandardObserver Observer
        {
            get => base.Observer;
            set
            {
                if (value != base.Observer)
                {
                    if (value == StandardObserver.CIE1931)
                    {
                        base.Background = new ImageBrush(ColorDiagrams.Cie1931DiagramObserver2);
                    }
                    else
                    {
                        base.Background = new ImageBrush(ColorDiagrams.Cie1931DiagramObserver10);
                    }

                    base.Observer = value;
                }
            }
        }
    }
}