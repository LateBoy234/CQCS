namespace ThreeNH.SQCX.Application.StandardSelection
{
    public interface ISelectStandardUseCase<TStandard, in TSample>
        where TStandard : class
    {
        TStandard SelectManual();
        TStandard SelectForMeasurement(TSample sample, bool automatic);
        TStandard FindNearest(TSample sample);
    }
}
