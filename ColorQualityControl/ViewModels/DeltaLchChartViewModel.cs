namespace ThreeNH.ColorQualityControl.ViewModels.Charts
{
    sealed class DeltaLchChartViewModel : ColorDiffChartViewModel
    {
        public DeltaLchChartViewModel()
        : base(new []{S._("dC"), S._("dH"), S._("dL")},
            new []{-10.0, 10.0, -10.0, 10.0, -10.0, 10.0})
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
        }
    }
}
