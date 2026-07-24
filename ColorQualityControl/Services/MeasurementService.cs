using System;
using System.Threading;
using System.Threading.Tasks;
using ThreeNH.ColorQualityControl.Measurement;
using ThreeNH.ColorQualityControl.Refactoring.Contracts;
using ThreeNH.ColorQualityControl.Specimens.Selection;
using ThreeNH.ColorSpecimen;
using ThreeNH.DataModel;

namespace ThreeNH.ColorQualityControl.Services
{
    /// <summary>
    /// 标样匹配与试样测量入库编排；选标样策略见 Specimens.Selection（阶段 3-T03）。
    /// 测量 UI 态收口到 <see cref="MeasureStateMachine"/>（阶段 3-T06）。
    /// </summary>
    internal sealed class MeasurementService
    {
        private IMeasurementHost _host;
        // 阶段1-T06：测量互斥锁，防止快速连点测样/测标导致并发入库
        private readonly SemaphoreSlim _measureLock = new SemaphoreSlim(1, 1);
        // 与锁同生命周期：持锁=Measuring，释放=Idle，避免命名取消等中途误恢复菜单
        private readonly MeasureStateMachine _stateMachine = new MeasureStateMachine();

        /// <summary>测量状态变更时通知（MainWindow 可订阅；默认已经 Host 推 UI）。</summary>
        public event EventHandler MeasureStateChanged;

        public MeasureState MeasureState => _stateMachine.State;

        public bool IsMeasuring => _stateMachine.IsMeasuring;

        public MeasurementService()
        {
            _stateMachine.StateChanged += (_, __) =>
            {
                MeasureStateChanged?.Invoke(this, EventArgs.Empty);
                // 状态机驱动菜单/状态栏，调用方不再各自 UpdateMeasureUi
                _host?.SetMeasureUiActive(_stateMachine.IsMeasuring);
            };
        }

        public void AttachHost(IMeasurementHost host)
        {
            _host = host;
        }

        /// <summary>尝试获取测量锁并进入 Measuring；已有测量进行中时返回 false。</summary>
        public async Task<bool> TryAcquireMeasureLockAsync(int millisecondsTimeout = 0)
        {
            if (!await _measureLock.WaitAsync(millisecondsTimeout).ConfigureAwait(true))
            {
                return false;
            }

            // 获锁即 Measuring：整批测完前菜单保持禁用（含命名对话框）
            _stateMachine.Force(MeasureState.Measuring);
            return true;
        }

        public void ReleaseMeasureLock()
        {
            if (_measureLock.CurrentCount == 0)
            {
                _measureLock.Release();
            }

            _stateMachine.Force(MeasureState.Idle);
        }

        /// <summary>未持锁时的收尾（校准失败等），保证 UI 回到 Idle。</summary>
        public void EnsureMeasureIdle()
        {
            _stateMachine.Force(MeasureState.Idle);
        }

        public StandardSpecimen GetBestStandard(Specimen test)
        {
            if (_host == null)
            {
                return null;
            }

            var context = new StandardSelectionContext(_host, test);
            var strategy = StandardSelectionStrategyResolver.ResolveForMeasure(_host.IsAutoSelectStandard);
            return strategy.Select(context);
        }

        public async Task FinishMeasureOnUiAsync(Specimen sample)
        {
            if (_host == null || sample == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(sample.Instrument))
            {
                sample.Instrument = _host.GetInstrumentModel();
            }

            // 在 UI 线程解析父标样；自动选标样的色差计算再丢后台，避免跨线程读错选中状态
            StandardSpecimen std;
            if (_host.IsAutoSelectStandard)
            {
                var fallback = ManualStandardSelectionStrategy.Instance.Select(
                    new StandardSelectionContext(_host, sample));
                std = await Task.Run(() => FindNearestStandard(sample)).ConfigureAwait(true) ?? fallback;
            }
            else
            {
                std = GetBestStandard(sample);
            }

            // 回到 UI 线程再弹命名对话框（WPF ShowDialog 要求）
            var test = new TestSpecimen(sample);

            if (!_host.IsAutoNaming && !_host.TryPromptTestName(test, std))
            {
                return;
            }

            _host.OnTestMeasurementCompleted(test, std);
        }

        /// <summary>
        /// 试样 Command 编排：循环测样 → 8°光泽 → 选标/命名/入库（UI 采集由 Host 完成）。
        /// </summary>
        public async Task ExecuteTrialMeasurementsAsync()
        {
            if (_host == null)
            {
                return;
            }

            var schedule = _host.GetTrialMeasurementSchedule();
            for (int i = 0; i < schedule.Times; i++)
            {
                string msg = _host.FormatMeasureProgressMessage(schedule.ModeLabel, i + 1, schedule.Times);
                var specimen = await _host.CaptureTrialSampleAsync(msg, schedule.DelaySeconds).ConfigureAwait(true);
                if (specimen == null)
                {
                    break;
                }

                specimen.Gloss8 = _host.CalculateEightDegreesGloss(specimen.SCI, specimen.SCE);
                await FinishMeasureOnUiAsync(specimen).ConfigureAwait(true);
            }
        }

        /// <summary>
        /// 标样 Command 编排：循环测标 → 清晰度/光泽/命名 → 入库（UI 采集由 Host 完成）。
        /// </summary>
        public async Task ExecuteStandardMeasurementsAsync()
        {
            if (_host == null)
            {
                return;
            }

            var schedule = _host.GetStandardMeasurementSchedule();
            for (int i = 0; i < schedule.Times; i++)
            {
                string msg = _host.FormatMeasureProgressMessage(schedule.ModeLabel, i + 1, schedule.Times);
                var specimen = await _host.CaptureStandardSampleAsync(msg, schedule.DelaySeconds).ConfigureAwait(true);
                if (specimen == null)
                {
                    break;
                }

                await ProcessMeasuredStandardAsync(specimen).ConfigureAwait(true);
            }
        }

        /// <summary>单次标样测量后的数据补全与入库（原 ProcessSingleStandard 业务部分）。</summary>
        public Task ProcessMeasuredStandardAsync(Specimen sample)
        {
            if (_host == null || sample == null)
            {
                return Task.CompletedTask;
            }

            if (string.IsNullOrWhiteSpace(sample.Instrument))
            {
                sample.Instrument = _host.GetInstrumentModel();
            }

            if (_host.IsInstrumentClarityValid())
            {
                sample.Clarity = _host.PromptAndMeasureClarity();
            }

            if (_host.ShouldMarkSampleAsTransmission())
            {
                sample.IsTransmission = true;
            }

            sample.Gloss8 = _host.CalculateEightDegreesGloss(sample.SCI, sample.SCE);

            var standard = new StandardSpecimen(sample);
            if (!_host.IsAutoNaming && !_host.TryPromptStandardName(standard))
            {
                return Task.CompletedTask;
            }

            _host.OnStandardMeasurementCompleted(standard);
            return Task.CompletedTask;
        }

        /// <summary>
        /// 按色差在 Job 标样中查找最近标样（编辑菜单「自动匹配标样」等，不受 CurrentStandard / 自动开关影响）。
        /// </summary>
        public StandardSpecimen FindNearestStandard(Specimen test)
        {
            if (_host == null)
            {
                return null;
            }

            var context = new StandardSelectionContext(_host, test);
            return StandardSelectionStrategyResolver.ResolveNearestOnly().Select(context);
        }

        /// <summary>雾度测样：对话框采集后走 FinishMeasureOnUiAsync 统一入库（阶段1-T10）。</summary>
        public async Task ExecuteHazeTrialMeasurementAsync()
        {
            if (_host == null)
            {
                return;
            }

            // 雾度路径原先仅 SetMeasureUiActive，未持锁；与普通测样统一走锁+状态机
            if (!await TryAcquireMeasureLockAsync(0).ConfigureAwait(true))
            {
                return;
            }

            try
            {
                var sample = _host.CaptureHazeSpecimen();
                if (sample == null)
                {
                    return;
                }

                await FinishMeasureOnUiAsync(sample).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                if (!_host.TryHandleMeasurementAbnormal(ex))
                {
                    _host.ShowMeasureError(ex.Message);
                }
            }
            finally
            {
                ReleaseMeasureLock();
            }
        }

        /// <summary>雾度测标：透射准备 + 对话框 + 命名 + 入库（不做 Gloss8/清晰度后处理）。</summary>
        public async Task ExecuteHazeStandardMeasurementAsync()
        {
            if (_host == null)
            {
                return;
            }

            if (!_host.EnsureTransmissiveModeForHaze())
            {
                return;
            }

            if (!_host.EnsureTransmissionCalibratedForHaze())
            {
                return;
            }

            if (!await TryAcquireMeasureLockAsync(0).ConfigureAwait(true))
            {
                return;
            }

            try
            {
                var sample = _host.CaptureHazeSpecimen();
                if (sample == null)
                {
                    return;
                }

                var standard = new StandardSpecimen(sample);
                if (!_host.IsAutoNaming && !_host.TryPromptStandardName(standard))
                {
                    return;
                }

                _host.OnStandardMeasurementCompleted(standard);
            }
            finally
            {
                ReleaseMeasureLock();
            }
        }
    }
}
