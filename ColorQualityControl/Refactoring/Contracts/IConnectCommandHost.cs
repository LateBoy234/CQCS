using ThreeNH.ColorimeterInterface;

namespace ThreeNH.ColorQualityControl.Refactoring.Contracts
{
    /// <summary>
    /// Connect/Disconnect Command 编排所需的 UI 能力（阶段4-T01，便于 Handler 单测注入）。
    /// </summary>
    internal interface IConnectCommandHost
    {
        IInstrument Colorimeter { get; }

        bool IsWindowLoaded { get; }

        void UpdateConnectionStatus();
    }
}
