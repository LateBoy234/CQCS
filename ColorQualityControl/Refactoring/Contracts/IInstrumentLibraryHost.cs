using System;
using ThreeNH.ColorimeterInterface;
using ThreeNH.ColorSpecimen;
using ThreeNH.DataModel;

namespace ThreeNH.ColorQualityControl.Refactoring.Contracts
{
    /// <summary>
    /// 存样库 / 仪器颜色库流程中需由 MainWindow 提供的 UI（阶段1-T24）。
    /// </summary>
    internal interface IInstrumentLibraryHost
    {
        IInstrument Colorimeter { get; }

        StandardSpecimen[] GetJobStandardSpecimens();

        void InvokeOnUi(Action action);

        void ShowColorLibraryAddSuccess();

        void ShowColorLibraryAddSuccessToast();

        void ShowColorLibraryDuplicateError();

        void ShowColorLibraryDuplicateErrorToast(string detail);

        void ShowInstrumentLibraryNotSupported();

        void ShowPleaseSelectAStandard();

        void SetInstrumentLibraryBusy(bool busy, string statusResourceKey);

        void ShowInstrumentLibraryWriteCompleted();

        void ShowInstrumentLibraryOperationCompleted();

        void ShowInstrumentLibraryError(string message);

        bool ConfirmClearInstrumentLibrary();

        bool ConfirmUpdateInstrumentLibrary();

        void ImportInstrumentLibraryTargets(Specimen[] targets);
    }
}
