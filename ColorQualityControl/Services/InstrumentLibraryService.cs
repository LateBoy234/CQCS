using System;
using System.Threading.Tasks;
using ThreeNH.ColorLibrary;
using ThreeNH.ColorimeterInterface;
using ThreeNH.ColorQualityControl.Refactoring.Contracts;
using ThreeNH.ColorSpecimen;
using ThreeNH.DataModel;

namespace ThreeNH.ColorQualityControl.Services
{
    /// <summary>
    /// 阶段 1：PC 存样库写入与仪器颜色库读写；MessageBox / Toast / 导入 UI 由 Host 提供。
    /// </summary>
    internal sealed class InstrumentLibraryService
    {
        private IInstrumentLibraryHost _host;

        public void AttachHost(IInstrumentLibraryHost host)
        {
            _host = host;
        }

        public bool TryAddToColorLibrary(string libraryName, StandardSpecimen[] standards)
        {
            if (_host == null || standards == null || standards.Length == 0)
            {
                return false;
            }

            var db = new ColorDatabase(ColorLibraryInfo.GetDbConnectionString(libraryName), false);
            try
            {
                db.InsertIntoColors(standards);
                _host.ShowColorLibraryAddSuccess();
                return true;
            }
            catch (Exception)
            {
                _host.ShowColorLibraryDuplicateError();
                return false;
            }
        }

        /// <summary>后台写入存样库，测量完成后自动存库走此路径（MS-01）。</summary>
        public void AddToColorLibraryAsync(string libraryName, StandardSpecimen[] standards)
        {
            if (_host == null || standards == null || standards.Length == 0)
            {
                return;
            }

            Task.Factory.StartNew(() =>
            {
                var db = new ColorDatabase(ColorLibraryInfo.GetDbConnectionString(libraryName), false);
                try
                {
                    db.InsertIntoColors(standards);
                    _host.InvokeOnUi(_host.ShowColorLibraryAddSuccessToast);
                }
                catch (Exception ex)
                {
                    _host.InvokeOnUi(() => _host.ShowColorLibraryDuplicateErrorToast(ex.Message));
                }
            });
        }

        public void AutoSaveStandardToColorLibrary(bool autoSaveEnabled, string libraryName, StandardSpecimen standard)
        {
            if (!autoSaveEnabled || standard == null)
            {
                return;
            }

            AddToColorLibraryAsync(libraryName, new[] { standard });
        }

        public bool SupportsInstrumentTargetLibrary()
            => _host?.Colorimeter is ITargetLibraryManagement;

        public void WriteToInstrumentLibraryAsync(StandardSpecimen[] standards)
        {
            if (_host == null)
            {
                return;
            }

            if (!SupportsInstrumentTargetLibrary())
            {
                _host.ShowInstrumentLibraryNotSupported();
                return;
            }

            if (standards == null || standards.Length == 0)
            {
                _host.ShowPleaseSelectAStandard();
                return;
            }

            _host.SetInstrumentLibraryBusy(true, "InWriting");

            Task.Factory.StartNew(() =>
            {
                Exception exception = null;
                try
                {
                    (_host.Colorimeter as ITargetLibraryManagement)?.WriteToLibrary(standards);
                }
                catch (Exception ex)
                {
                    exception = ex;
                }

                var captured = exception;
                _host.InvokeOnUi(() =>
                {
                    _host.SetInstrumentLibraryBusy(false, "Ready");
                    if (captured != null)
                    {
                        _host.ShowInstrumentLibraryError(captured.Message);
                    }
                    else
                    {
                        _host.ShowInstrumentLibraryWriteCompleted();
                    }
                });
            });
        }

        public void ClearInstrumentLibrary()
        {
            if (_host == null)
            {
                return;
            }

            if (!_host.ConfirmClearInstrumentLibrary())
            {
                return;
            }

            try
            {
                (_host.Colorimeter as ITargetLibraryManagement)?.ClearLibrary();
                _host.ShowInstrumentLibraryOperationCompleted();
            }
            catch (Exception ex)
            {
                _host.ShowInstrumentLibraryError(ex.Message);
            }
        }

        public void UpdateInstrumentLibrary()
        {
            if (_host == null)
            {
                return;
            }

            if (!_host.ConfirmUpdateInstrumentLibrary())
            {
                return;
            }

            var management = _host.Colorimeter as ITargetLibraryManagement;
            if (management == null)
            {
                return;
            }

            try
            {
                management.ClearLibrary();
                var standards = _host.GetJobStandardSpecimens();
                if (standards != null && standards.Length > 0)
                {
                    management.WriteToLibrary(standards);
                }

                _host.ShowInstrumentLibraryOperationCompleted();
            }
            catch (Exception ex)
            {
                _host.ShowInstrumentLibraryError(ex.Message);
            }
        }

        public void ReadFromInstrumentLibrary()
        {
            if (_host == null)
            {
                return;
            }

            if (!SupportsInstrumentTargetLibrary())
            {
                _host.ShowInstrumentLibraryNotSupported();
                return;
            }

            _host.SetInstrumentLibraryBusy(true, "InReading");
            try
            {
                var targets = (_host.Colorimeter as ITargetLibraryManagement)?.ReadTargets();
                if (targets != null)
                {
                    _host.ImportInstrumentLibraryTargets(targets);
                }
            }
            catch (Exception ex)
            {
                _host.ShowInstrumentLibraryError(ex.Message);
            }
            finally
            {
                _host.SetInstrumentLibraryBusy(false, "Ready");
            }
        }
    }
}
