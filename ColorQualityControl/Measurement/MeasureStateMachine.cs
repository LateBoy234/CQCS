using System;

namespace ThreeNH.ColorQualityControl.Measurement
{
    /// <summary>
    /// 测量状态机：Idle↔Measuring，与互斥锁同生命周期，避免中途命名取消误恢复菜单（3-T06）。
    /// </summary>
    internal sealed class MeasureStateMachine
    {
        private MeasureState _state = MeasureState.Idle;

        public event EventHandler StateChanged;

        public MeasureState State => _state;

        public bool IsMeasuring => _state == MeasureState.Measuring;

        public bool IsIdle => _state == MeasureState.Idle;

        /// <summary>尝试迁移；非法路径返回 false 且不改状态。</summary>
        public bool TryTransition(MeasureState target)
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
        /// 强制进入目标状态（锁释放、异常收尾），不校验来源。
        /// 收尾必须落到 Idle，避免菜单一直禁用。
        /// </summary>
        public void Force(MeasureState target)
        {
            if (_state == target)
            {
                return;
            }

            _state = target;
            StateChanged?.Invoke(this, EventArgs.Empty);
        }

        private static bool IsAllowed(MeasureState from, MeasureState to)
        {
            switch (from)
            {
                case MeasureState.Idle:
                    return to == MeasureState.Measuring;

                case MeasureState.Measuring:
                    return to == MeasureState.Idle;

                default:
                    return false;
            }
        }
    }
}
