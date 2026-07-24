namespace ThreeNH.ColorQualityControl.ViewModels.Charts
{
    class DeltaYxyChartViewModel : ColorDiffChartViewModel
    {
        public DeltaYxyChartViewModel()
            : base(new []{S._("dx_"), S._("dy_"), S._("dY")},
                new []{-0.1, 0.1, -0.1, 0.1, -10.0, 10.0})
        {
            AxisXMinSeparatorStep = 0.0005;
            AxisYMinSeparatorStep = 0.0005;
            AxisZMinSeparatorStep = 0.05;

            AxisXMin = -0.01;
            AxisXMax = 0.01;
            AxisXSeparatorStep = 0.05;

            AxisYMin = -0.01;
            AxisYMax = 0.01;
            AxisYSeparatorStep = 0.05;

            AxisZMin = -2.0;
            AxisZMax = 2.0;
            AxisZSeparatorStep = 0.5;
        }
    }
}
