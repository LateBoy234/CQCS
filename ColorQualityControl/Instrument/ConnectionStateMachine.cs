using System;

namespace ThreeNH.ColorQualityControl.Instrument
{
    /// <summary>
    /// 连接状态机：集中合法迁移，避免 MainWindow / Service 多处 bool 互相打架（3-T02）。
    /// </summary>
    internal sealed class ConnectionStateMachine
    {
        private InstrumentConnectionState _state = InstrumentConnectionState.Disconnected;

        public event EventHandler StateChanged;

        public InstrumentConnectionState State => _state;

        /// <summary>连接流程进行中（含等选设备、扫描），对应原 IsConnecting。</summary>
        public bool IsConnecting =>
            _state == InstrumentConnectionState.Connecting
            || _state == InstrumentConnectionState.WaitingForDeviceSelection
            || _state == InstrumentConnectionState.Searching;

        public bool IsSearching => _state == InstrumentConnectionState.Searching;

        public bool IsConnected => _state == InstrumentConnectionState.Connected;

        public bool IsDisconnected => _state == InstrumentConnectionState.Disconnected;

        /// <summary>尝试迁移；非法路径返回 false 且不改状态。</summary>
        public bool TryTransition(InstrumentConnectionState target)
        {
            if (_state == target)
            {
                return true;
            }

            if (!IsAllowed(_state, target))
            {
                return false;
            }

            _state = target;
            StateChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }

        /// <summary>
        /// 强制进入目标状态（Worker 完成、断开等收尾路径），不校验来源。
        /// 收尾必须落到终态，避免非法迁移导致 IsConnecting 卡住。
        /// </summary>
        public void Force(InstrumentConnectionState target)
        {
            if (_state == target)
            {
                return;
            }

            _state = target;
            StateChanged?.Invoke(this, EventArgs.Empty);
        }

        private static bool IsAllowed(InstrumentConnectionState from, InstrumentConnectionState to)
        {
            switch (from)
            {
                case InstrumentConnectionState.Disconnected:
                    return to == InstrumentConnectionState.Connecting
                        || to == InstrumentConnectionState.Searching;

                case InstrumentConnectionState.Connecting:
                    return to == InstrumentConnectionState.WaitingForDeviceSelection
                        || to == InstrumentConnectionState.Searching
                        || to == InstrumentConnectionState.Connected
                        || to == InstrumentConnectionState.Disconnected;

                case InstrumentConnectionState.WaitingForDeviceSelection:
                    return to == InstrumentConnectionState.Connecting
                        || to == InstrumentConnectionState.Searching
                        || to == InstrumentConnectionState.Disconnected;

                case InstrumentConnectionState.Searching:
                    return to == InstrumentConnectionState.Connecting
                        || to == InstrumentConnectionState.WaitingForDeviceSelection
                        || to == InstrumentConnectionState.Disconnected;

                case InstrumentConnectionState.Connected:
                    return to == InstrumentConnectionState.Disconnected
                        || to == InstrumentConnectionState.Connecting;

                default:
                    return false;
            }
        }
    }
}
