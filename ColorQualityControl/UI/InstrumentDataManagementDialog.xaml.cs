using ClosedXML.Excel;
using ControlzEx.Standard;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using ThreeNH.ColorAlgorithm;
using ThreeNH.ColorimeterInterface;
using ThreeNH.ColorQualityControl.Adapter;
using ThreeNH.ColorQualityControl.Specimens.Import;
using ThreeNH.ColorSpecimen;
using ThreeNH.CommDevice;
using ThreeNH.CommunicationBase;
using ThreeNH.Controls;
using ThreeNH.DataModel;
using ThreeNH.Optical;
using ThreeNH.SQCX.Infrastructure.Instruments.YHComm;
using ThreeNH.SQCX.Infrastructure.Instruments.Spectrophotometer;

namespace ThreeNH.ColorQualityControl.UI
{
    class ObjectVisibleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return Visibility.Collapsed;
            TypeConverter converter = TypeDescriptor.GetConverter(value);
            if (converter.CanConvertTo(typeof(bool)))
            {
                return (bool)converter.ConvertTo(value, typeof(bool)) ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    internal class SampleDataItem
    {
        public Specimen Specimen { get; set; }
        public SpecimenAdapter SCI { get; set; }
        public SpecimenAdapter SCE { get; set; }
        public SpecimenAdapter Clr { get; set; }
        public string Name { get { return Specimen.Name; } }
        public int ID { get { return Specimen.ID; } }
        public object Tag { get; set; }
        public bool IsChecked { get; set; }


    }

    internal class StandardDataItem : SampleDataItem, INotifyPropertyChanged
    {
        ObservableCollection<SampleDataItem> _tests = new ObservableCollection<SampleDataItem>();
        public ObservableCollection<SampleDataItem> Tests
        {
            get { return _tests; }
            set
            {
                _tests = value;
                OnPropertyChanged(nameof(Tests));
            }
        }

        public StandardSpecimen StandardSpecimen
        {
            get { return Specimen as StandardSpecimen; }
            set { Specimen = value; }
        }

        public void ClearTests()
        {
            Tests.Clear();
            StandardSpecimen.TestSpecimens?.Clear();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string prop = "")
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));

        // 可选：VM 懒得去 CollectionChanged 再定位哪一项变了，就订这个事件
        public event EventHandler CheckedChanged;

        private bool _isChecked;
        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                if (_isChecked == value) return;
                _isChecked = value;
                OnPropertyChanged(nameof(IsChecked));

                // 如果 VM 想直接监听，这里抛一个弱事件（可选）
                CheckedChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }


    /// <summary>
    /// InstrumentDataManagementDialog.xaml 的交互逻辑
    /// </summary>
    public partial class InstrumentDataManagementDialog : Window, INotifyPropertyChanged
    {
        private static NameGenerator standardRenameRule = null;
        private static NameGenerator testRenameRule = null;
        private static string material = null;



        public EventWaitHandle stopUploadEvent = new EventWaitHandle(false, EventResetMode.ManualReset);

        private List<KeyValuePair<string, string>> RenameRuleVariables;
        private StandardDataItem currentStandard = null;  // 当前显示的试样所属的标样
        private ObservableCollection<StandardDataItem> standardList =
            new ObservableCollection<StandardDataItem>();
        private ObservableCollection<SpecimenAdapter> datagridItems =
            new ObservableCollection<SpecimenAdapter>();

        private ChromaticitySetting displayFields = new ChromaticitySetting();

        public event PropertyChangedEventHandler PropertyChanged;

        public InstrumentDataManagementDialog()
        {
            standardRenameRule = new NameGenerator()
            {
                Rule = "{OldName}",
                VariableEvaluator = VariableEvaluator
            };
            testRenameRule = new NameGenerator()
            {
                Rule = "{OldName}",
                VariableEvaluator = VariableEvaluator
            };

            displayFields.Copy((Application.Current as App).SystemDefaultConfig.InstrumentDataDisplaySetting);


            Formula = new Formula();

            RenameRuleVariables = new List<KeyValuePair<string, string>>();
            RenameRuleVariables.Add(new KeyValuePair<string, string>("Index", S._("MenuItem_Index")));
            RenameRuleVariables.Add(new KeyValuePair<string, string>("InstrumentModel", S._("MenuItem_InstrumentModel")));
            RenameRuleVariables.Add(new KeyValuePair<string, string>("Material", S._("MenuItem_Material")));
            RenameRuleVariables.Add(new KeyValuePair<string, string>("SpecularComponent", S._("MenuItem_SpecularComponent")));
            RenameRuleVariables.Add(new KeyValuePair<string, string>("UV", S._("MenuItem_UV")));
            RenameRuleVariables.Add(new KeyValuePair<string, string>("DateTime", S._("MenuItem_DateTime")));
            RenameRuleVariables.Add(new KeyValuePair<string, string>("OldName", S._("MenuItem_OldName")));


            InitializeComponent();
            DataContext = this;
            SpecimenDataGrid.Fields = displayFields.DisplayFields;
            SpecimenDataGrid.ItemsSource = datagridItems;

            RootTreeItem.ItemsSource = standardList;

        }


        private void HookChecked(StandardDataItem item)
        {
            item.CheckedChanged += (s, _) =>
            {
                var i = (StandardDataItem)s;
                if (i.IsChecked)
                {
                    currentStandard = i;
                }
            };
        }

        static string VariableEvaluator(string variable, string format, object parameter)
        {
            Specimen specimen = parameter as Specimen;
            switch (variable.ToUpper())
            {
                case "INDEX": return specimen.ID.ToString();
                case "DATETIME":
                    if (string.IsNullOrWhiteSpace(format))
                        return specimen.DateTime.ToString("yyMMddhhmmss");
                    else
                        return specimen.DateTime.ToString(format);
                case "MATERIAL": return specimen.Material;
                case "INSTRUMENT": return specimen.Instrument;
                case "SPECULARCOMPONENT": return specimen.SpecularComponent.ToString();
                case "UV": return specimen.UV.ToString();
                case "OLDNAME": return specimen.Name;
                default: return null;
            }
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        internal ObservableCollection<StandardDataItem> StandardList
        {
            get { return standardList; }
            set
            {
                standardList = value;
                OnPropertyChanged(nameof(StandardList));
            }
        }

        private int standardCount = 0;
        public int StandardCount
        {
            get { return standardCount; }
            set
            {
                standardCount = value;
                OnPropertyChanged("StandardCount");
            }
        }
        private int testCount = 0;
        public int TestCount
        {
            get { return testCount; }
            set
            {
                testCount = value;
                OnPropertyChanged("TestCount");
            }
        }
        private int testTotalCount = 0;
        public int TestTotalCount
        {
            get { return testTotalCount; }
            set
            {
                testTotalCount = value;
                OnPropertyChanged("TestTotalCount");
            }
        }
        private string uploadingStatus = null;
        public string UploadingStatus
        {
            get { return uploadingStatus; }
            set
            {
                uploadingStatus = value;
                OnPropertyChanged("UploadingStatus");
            }
        }

        private void OnStopUpload(object sender, EventArgs e)
        {
            stopUploadEvent.Set();
        }

        /// <summary>
        /// 保留兼容的同步封装（若代码其他处仍依赖同步返回），但注意同步调用仍会阻塞调用线程。
        /// 推荐新调用方使用 LoadStandardsAsync。
        /// </summary>
        private bool LoadStandards(int from = 0)
        {
            // 阻塞等待异步操作完成，以兼容原有同步调用语义
            return LoadStandardsAsync(from).GetAwaiter().GetResult();
        }
        async void OnLoaded(object sender, EventArgs e)
        {
            try
            {
                // 读取计数不要阻塞 UI 线程
                StandardCount = Colorimeter.TotalStandards;
                await Task.Delay(1000).ConfigureAwait(true); // 保留原来的延迟但不阻塞 UI
                TestTotalCount = Colorimeter.TotalSamples;
            }
            catch (Exception ex)
            {
                MessageBox.Show(S._(ex.Message));
            }

            // 异步加载标样，避免在 UI 线程进行长时间阻塞
            // 不用 BeginInvoke 去 UI 线程执行（原来反而在 UI 线程阻塞），改为异步任务
            _ = LoadStandardsAsync();
        }

        private int _isLoadingStandardsFlag = 0;
        /// <summary>
        /// 异步版本：在后台线程循环上传标样并在 UI 线程安全地添加到集合中。
        /// 返回是否成功（若被取消或通信异常则为 false）。
        /// </summary>
        private async Task<bool> ValidateConnectionAsync()
        {
            try
            {
                // 检查仪器是否响应
                if (Colorimeter is IDeviceInfo deviceInfo)
                {
                    //尝获取简单的设备信息来验证连接
                    var info = await Task.Run(() => deviceInfo.SN).ConfigureAwait(false);
                    return info != null;
                }
                else
                {
                    // 对于不支持 IDeviceInfo 的仪器，尝试简单的命令
                    await Task.Run(() => Colorimeter.TotalStandards).ConfigureAwait(false);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Connection validation failed: " + ex.Message);
                return false;
            }
        }

        private async Task<bool> LoadStandardsAsync(int from = 0)
        {
            // 防重入：如果已经在加载中，直接返回
            if (Interlocked.CompareExchange(ref _isLoadingStandardsFlag, 1, 0) == 1)
            {
                Trace.TraceWarning("LoadStandardsAsync 被重入，忽略本次调用。");
                return false;
            }

            var sw = Stopwatch.StartNew();
            try
            {
                if (Colorimeter == null)
                {
                    await Dispatcher.InvokeAsync(new Action(() =>
                        MessageBox.Show(S._("ColorimeterNotInitialized"))));
                    return false;
                }

                // 添加连接状态检查
                if (!await ValidateConnectionAsync())
                {
                    await Dispatcher.InvokeAsync(() =>
                        MessageBox.Show(S._("InstrumentConnectionInvalid"),
                            S._("Title_UploadingStandards"), MessageBoxButton.OK, MessageBoxImage.Warning));
                    return false;
                }

                // 创建进度对话框
                Loading loading = new Loading(S._("LoadingStandards"), S._("ProgressDialogTitle"));
                loading.ShowingProgress = true;
                loading.CanBeCanceled = true;

                // Use TaskCompletionSource to wait for the operation to complete
                var tcs = new TaskCompletionSource<bool>();
                Exception exceptionOccurred = null;
                bool operationCompleted = false;

                loading.BackgroundWork = (reportProgress, setResult, args) =>
                {
                    try
                    {
                        int step = IsBleDevice() ? 1 : 50;

                        for (int i = from; i < StandardCount; i += step)
                        {
                            if (args.IsCancelled || stopUploadEvent.WaitOne(0))
                            {
                                // 用户取消
                                operationCompleted = true;
                                tcs.TrySetResult(false);
                                return;
                            }

                            int count = Math.Min(step, StandardCount - i);

                            // 更新进度
                            int progressPercentage = (int)((double)i / StandardCount * 100);
                            string progressMessage = $"{S._("LoadingStandardsProgress")} ({i + 1}-{Math.Min(i + step, StandardCount)} of {StandardCount})";

                            Dispatcher.Invoke(() =>
                            {
                                reportProgress(progressMessage);
                                reportProgress(progressPercentage);
                            });

                            StandardSpecimen[] standards;
                            try
                            {
                                // 在线程池里执行设备 IO
                                standards = Colorimeter.UploadStandards(i, count);
                            }
                            catch (TimeoutException timeoutEx)
                            {
                                // 处理超时异常
                                Trace.TraceError("TimeoutException occurred: " + timeoutEx.ToString());
                                // 根据超时类型选择合适的错误码
                                var errorCode = timeoutEx.Message.Contains("receive") || timeoutEx.Message.Contains("读取") ?
                                    CommunicationError.ReceivingTimeout : CommunicationError.WritingTimeout;
                                exceptionOccurred = new CommunicationException(

                                    errorCode,
                                     S._("InstrumentCommunicationTimeout") + ": " + timeoutEx.Message,
                                    timeoutEx);
                                operationCompleted = true;
                                tcs.TrySetResult(false);
                                return;
                            }
                            catch (CommunicationException comEx)
                            {
                                // 记录完整信息，UI 提示
                                Trace.TraceError(comEx.ToString());
                                exceptionOccurred = comEx;
                                operationCompleted = true;
                                tcs.TrySetResult(false);
                                return;
                            }
                            catch (Exception ex)
                            {
                                // 捕获其他异常，记录并提示
                                Trace.TraceError(ex.ToString());
                                exceptionOccurred = ex;
                                operationCompleted = true;
                                tcs.TrySetResult(false);
                                return;
                            }

                            // 批量回写到 UI 线程的集合（线程安全）
                            Dispatcher.Invoke(() =>
                            {
                                foreach (var std in standards)
                                {
                                    try
                                    {
                                        StandardDataItem stdWrap = new StandardDataItem { StandardSpecimen = std };
                                        StandardList.Add(stdWrap);
                                        HookChecked(stdWrap);
                                    }
                                    catch (Exception ex)
                                    {
                                        // 单条对象构建出错时记录，但继续处理后续项
                                        Trace.TraceError("构建 StandardDataItem 时出错: " + ex.ToString());
                                    }
                                }
                            });
                        }

                        // Operation completed successfully
                        operationCompleted = true;
                        tcs.TrySetResult(true);
                    }
                    catch (Exception ex)
                    {
                        exceptionOccurred = ex;
                        operationCompleted = true;
                        tcs.TrySetResult(false);
                    }
                };

                // Also handle the case where the loading dialog is closed externally
                loading.BackgroundWorkerCompleted += (sender, e) =>
                {
                    // Ensure the task is completed if it hasn't been already
                    if (!operationCompleted)
                    {
                        operationCompleted = true;
                        tcs.TrySetResult(false); // Return false if operation didn't complete properly
                    }
                };

                // Handle the background task completion
                loading.BackgroundWorkerCompleted += (sender, e) =>
                {
                    // We handle exceptions in the BackgroundWork method directly
                };

                // Show the progress dialog and start loading
                loading.Start();

                // Wait for the operation to complete
                bool result = await tcs.Task;

                // If an exception occurred during processing, show it now
                if (exceptionOccurred != null)
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        // 检查是否是超时异常，如果是则建议重新连接
                        if (exceptionOccurred is CommunicationException commEx &&
                            (commEx.ErrorCode == CommunicationError.ReceivingTimeout ||
                             commEx.ErrorCode == CommunicationError.WritingTimeout))
                        {
                            var result1 = MessageBox.Show(
                                exceptionOccurred.Message + Environment.NewLine +
                                S._("InstrumentTimeoutSuggestion") + Environment.NewLine +
                                S._("RetryConnectionQuestion"),
                                S._("Title_UploadingStandards"),
                                MessageBoxButton.YesNo,
                                MessageBoxImage.Warning);

                            if (result1 == MessageBoxResult.Yes)
                            {
                                // 用户选择重新连接，这里可以触发重新连接逻辑
                                // 由于我们无法直接访问主窗口的连接方法，建议用户手动重连
                                MessageBox.Show(S._("PleaseReconnectManually"),
                                    S._("Title_UploadingStandards"),
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Information);
                            }
                        }
                        else
                        {
                            MessageBox.Show(exceptionOccurred.Message + Environment.NewLine +
                                (exceptionOccurred is CommunicationException ? exceptionOccurred.StackTrace : ""),
                                S._("Title_UploadingStandards"), MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    });
                    return false;
                }

                return result;
            }
            catch (Exception ex)
            {
                // Handle any unhandled exceptions that might occur outside the background worker
                Trace.TraceError("Unhandled exception in LoadStandardsAsync: " + ex.ToString());
                await Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show(ex.Message,
                        S._("Title_UploadingStandards"), MessageBoxButton.OK, MessageBoxImage.Warning);
                });
                return false;
            }
            finally
            {
                // 恢复状态 - 确保无论什么情况都会重置标志
                Interlocked.Exchange(ref _isLoadingStandardsFlag, 0);
                await Dispatcher.InvokeAsync(new Action(() => Cursor = Cursors.Arrow));
                sw.Stop();
                Trace.TraceInformation("加载数据管理界面耗时:{0}", sw.ElapsedMilliseconds);
            }
        }

        private void UploadAllSpecimensExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var sw = Stopwatch.StartNew();
            LoadAllTests();
            sw.Stop();
            MessageBox.Show(sw.Elapsed.TotalMilliseconds.ToString("F2"));
        }



        public Formula Formula { get; set; }

        private Task<StandardSpecimen[]> UploadStandardAsync(int index, int count)
        {
            return Task.Factory.StartNew(() => Colorimeter.UploadStandards(index, count));
        }

        private bool IsBleDevice()
        {
            return Colorimeter is SQCX.Infrastructure.Instruments.BTS.BleSpectrophotometer || Colorimeter is PTS.BleSpectrophotometer ||
                    Colorimeter is SQCX.Infrastructure.Instruments.GratingSpectrometer.Spectrometer ys && ys.CommunicationDevice is BluetoothDevice;
        }

        //private bool LoadStandards(int from = 0)
        //{
        //    var sw = Stopwatch.StartNew();
        //    try
        //    {
        //        int step = IsBleDevice() ? 1 : 50;
        //        for (int i = from; i < StandardCount; i += step)
        //        {
        //            int count = Math.Min(step, standardCount - i);
        //            var standards = Colorimeter.UploadStandards(i, count);
        //            foreach (var std in standards)
        //            {
        //                StandardDataItem stdWrap = new StandardDataItem { StandardSpecimen = std };


        //                StandardList.Add(stdWrap);
        //                    HookChecked(stdWrap);
        //            }
        //        }

        //    }
        //    catch (CommunicationException ex)
        //    {
        //        //Dispatcher.BeginInvoke(new Action(() =>
        //        //{
        //        MessageBox.Show(ex.Message+ex.StackTrace, S._("Tilte_UploadingStandards"),
        //            MessageBoxButton.OK, MessageBoxImage.Warning);
        //        //}));
        //        return false;
        //    }
        //    finally
        //    {
        //        Cursor = Cursors.Arrow;
        //    }
        //    sw.Stop();
        //    Trace.TraceInformation("加载数据管理界面耗时:{0}",sw.ElapsedMilliseconds);
        //    return true;
        //}

        bool IsTestsLoaded(StandardDataItem std)
        {
            return std.Tag != null && (bool)std.Tag;
        }
        void SetTestsLoadedFlag(StandardDataItem std, bool loaded = true)
        {
            std.Tag = loaded;
        }

        //        Task<Specimen[]> UploadTestAsync(int stdId, int index, int count)
        //        {
        //            return Task.Factory.StartNew(() => Colorimeter.UploadSamplesById(stdId, index, count));
        //        }

        SampleDataItem ToSampleDataItem(StandardDataItem standard, Specimen specimen)
        {
            var test = new TestSpecimen(specimen);
            test.StandardSpecimen = standard.StandardSpecimen;

            SampleDataItem testItem = new SampleDataItem { Specimen = test };

            if (test.SCI != null)
            {
                testItem.SCI = new TestSpecimenAdapter(
                    test, Formula, displayFields,
                    SpecularComponent.SCI, standard.SCI as StandardSpecimenAdapter);
            }
            if (test.SCE != null)
            {
                testItem.SCE = new TestSpecimenAdapter(
                    test, Formula, displayFields,
                    SpecularComponent.SCE, standard.SCE as StandardSpecimenAdapter);
            }
            if (test.Clr != null)
            {
                testItem.Clr = new TestSpecimenAdapter(
                    test, Formula, displayFields,
                    SpecularComponent.Unknown, standard.Clr as StandardSpecimenAdapter);
            }
            return testItem;
        }

        private bool LoadTests(StandardDataItem std)
        {
            try
            {
                int stdId = std.ID;
                TestCount = Colorimeter.GetSamplesCountById(stdId);
                UploadingStatus = string.Format(S._("UploadingTestXN"), 0, TestCount);
                std.ClearTests();
                int step = 20;
                for (int i = 0; i < TestCount; i += step)
                {
                    if (stopUploadEvent.WaitOne(0))
                    {
                        SetTestsLoadedFlag(std, false);
                        UploadingStatus = S._("UploadAborted");
                        return false;
                    }

                    int count = Math.Min(step, TestCount - i);
                    var tests = Colorimeter.UploadSamplesById(stdId, i, count);
                    foreach (var test in tests)
                    {
                        SampleDataItem testItem = ToSampleDataItem(std, test);
                        std.Tests.Add(testItem);
                        if (std == currentStandard)
                        {
                            if (testItem.SCI != null && IsShowSci)
                                datagridItems.Add(testItem.SCI);
                            if (testItem.SCE != null && IsShowSce)
                                datagridItems.Add(testItem.SCE);
                            if (testItem.Clr != null)
                                datagridItems.Add(testItem.Clr);
                        }
                    }
                    UploadingStatus = string.Format(S._("UploadingTestXN"),
                        std.StandardSpecimen.TestSpecimens.Count, TestCount);
                }

                SetTestsLoadedFlag(std);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(S._(ex.Message), S._("Title_UploadingTests"),
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
        }

        private void LoadAllTests()
        {
            // 如果未能成功上传所有数据，上传后面的标样
            if (standardList.Count < StandardCount)
            {
                if (!LoadStandards(standardList.Count))
                {
                    return;
                }
            }

            if (TestTotalCount == 0)
                return;

            stopUploadEvent.Reset();
            foreach (var stdWrap in standardList)
            {
                // 如果试样已经上传过，直接跳过
                if (IsTestsLoaded(stdWrap))
                    continue;

                UploadingStatus = string.Format(S._("UploadingTheSamplesOfX"), stdWrap.Name);
                try
                {
                    LoadTests(stdWrap);
                }
                catch (CommunicationException ex)
                {
                    UploadingStatus = ex.Message;
                    return;
                }

                if (stopUploadEvent.WaitOne(0))
                {
                    UploadingStatus = S._("UploadAborted");
                    break;
                }
            }

            UploadingStatus = S._("Ready");

        }

        public IRecordManagement Colorimeter { get; internal set; }

        bool IsShowSci
        {
            get { return _sc == SpecularComponent.SCI || _sc == SpecularComponent.SCIAndSCE; }
        }
        bool IsShowSce
        {
            get { return _sc == SpecularComponent.SCE || _sc == SpecularComponent.SCIAndSCE; }
        }

        private bool IsShowClr
        {
            get { return _sc == SpecularComponent.Unknown || _sc == SpecularComponent.SCIAndSCE; }
        }

        List<SpecimenAdapter> GetStandardAdapters()
        {
            List<SpecimenAdapter> list = new List<SpecimenAdapter>();
            foreach (var std in StandardList)
            {
                if (std.SCI != null && IsShowSci)
                {
                    list.Add(std.SCI);
                }
                if (std.SCE != null && IsShowSce)
                {
                    list.Add(std.SCE);
                }
                if (std.Clr != null && IsShowClr)
                {
                    list.Add(std.Clr);
                }
            }

            return list;
        }
        List<SpecimenAdapter> GetTestAdapters(StandardDataItem stdItem)
        {
            List<SpecimenAdapter> list = new List<SpecimenAdapter>();
            foreach (var item in stdItem.Tests)
            {
                if (item.SCI != null && IsShowSci)
                {
                    list.Add(item.SCI);
                }
                if (item.SCE != null && IsShowSce)
                {
                    list.Add(item.SCE);
                }
                if (item.Clr != null && IsShowClr)
                {
                    list.Add(item.Clr);
                }
            }
            return list;
        }

        void ShowAllStandards()
        {
            datagridItems.Clear();
            foreach (var item in GetStandardAdapters())
            {
                datagridItems.Add(item);
            }
            currentStandard = null;
            SpecimenDataGrid.ContextMenu = (ContextMenu)Resources["StandardDataContextMenu"];
        }

        void ShowTestsOfCurrentStandard()
        {
            datagridItems.Clear();
            if (currentStandard == null)
                return;

            stopUploadEvent.Reset();
            if (!IsTestsLoaded(currentStandard))
            {
                LoadTests(currentStandard);
            }
            else
            {
                foreach (var item in GetTestAdapters(currentStandard))
                {
                    datagridItems.Add(item);
                }
            }
            SpecimenDataGrid.ContextMenu = (ContextMenu)Resources["TestDataContextMenu"];
        }

        private void SpecimenTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            Mouse.OverrideCursor = Cursors.Wait;
            if (e.NewValue == RootTreeItem)
            {
                ShowAllStandards();

            }
            else if (e.NewValue is StandardDataItem)
            {
                datagridItems.Clear();

                currentStandard = e.NewValue as StandardDataItem;
                ShowTestsOfCurrentStandard();

            }
            else if (e.NewValue is SampleDataItem)
            {
                SampleDataItem testWrap = e.NewValue as SampleDataItem;
                StandardDataItem stdWrap = GetStandardDataItem(testWrap);
                if (stdWrap != currentStandard)
                {
                    currentStandard = stdWrap;
                    ShowTestsOfCurrentStandard();
                }
            }

            Mouse.OverrideCursor = Cursors.Arrow;
        }

        private StandardDataItem GetStandardDataItem(SampleDataItem test)
        {
            TestSpecimen testSpecimen = test.Specimen as TestSpecimen;
            foreach (var stdWrap in StandardList)
            {
                if (testSpecimen.StandardSpecimen == stdWrap.StandardSpecimen)
                    return stdWrap;
            }
            return null;
        }


        /// <summary>
        /// 添加式样数据行
        /// </summary>
        /// <param name="item"></param>
        /// <param name="sheet"></param>
        /// <param name="row"></param>
        void writeSamColorDataToRow(SampleDataItem item, IXLWorksheet sheet, ref int row)
        {

            if (item.SCI != null && IsShowSci)
            {
                row += 1;
                int col = 1;
                foreach (var field in displayFields.DisplayFields)
                {
                    var cell = sheet.Cell(row, col++);
                    cell.Value = item.SCI.GetFormatedPropertyValue(field);
                    if (field == ChromaticityField.PseudoColor)
                    {
                        cell.Style.Fill.BackgroundColor = XLColor.FromHtml(cell.Value as string);
                    }
                }
            }
            if (item.SCE != null && IsShowSce)
            {
                row += 1;
                int col = 1;
                foreach (var field in displayFields.DisplayFields)
                {
                    var cell = sheet.Cell(row, col++);
                    cell.Value = item.SCE.GetFormatedPropertyValue(field);
                    if (field == ChromaticityField.PseudoColor)
                    {
                        cell.Style.Fill.BackgroundColor = XLColor.FromHtml(cell.Value as string);
                    }
                }
            }
            if (item.Clr != null)
            {
                row += 1;
                int col = 1;
                foreach (var field in displayFields.DisplayFields)
                {
                    var cell = sheet.Cell(row, col++);
                    cell.Value = item.Clr.GetFormatedPropertyValue(field);
                    if (field == ChromaticityField.PseudoColor)
                    {
                        cell.Style.Fill.BackgroundColor = XLColor.FromHtml(cell.Value as string);
                    }
                }
            }
        }
        SpecimenAdapterHelper helper => MainWindow.GetSpecimenAdapterHelper(MainWindow.ChromaticitySetting);
        /// <summary>
        /// 添加标样数据行
        /// </summary>
        /// <param name="item"></param>
        /// <param name="sheet"></param>
        /// <param name="row"></param>
        void writeStdColorDataToRow(StandardDataItem item, IXLWorksheet sheet, ref int row)
        {
            if (item.StandardSpecimen != null)
            {
                row += 1;
                int col = 1;
                foreach (var field in displayFields.DisplayFields)
                {
                    var cell = sheet.Cell(row, col++);
                    cell.Value = helper.CreateAdapter(item.StandardSpecimen).GetFormatedPropertyValue(field);
                    if (field == ChromaticityField.PseudoColor)
                    {
                        cell.Style.Fill.BackgroundColor = XLColor.FromHtml(cell.Value as string);
                    }
                }
            }
        }
        /// <summary>
        /// 根据条件匹配数据
        /// </summary>
        /// <param name="stdWraps">数据列表</param>
        /// <param name="withTests">是否带试样数据</param>
        /// <param name="allFlag">是否全部导出</param>
        /// <returns></returns>
        StandardSpecimen[] PrepareStandards(IEnumerable<StandardDataItem> stdWraps, bool withTests, bool allFlag)
        {
            stopUploadEvent.Reset();
            List<StandardSpecimen> list = new List<StandardSpecimen>();
            foreach (var stdWrap in stdWraps)
            {
                list.Add(stdWrap.StandardSpecimen);
                Rename(stdWrap);
                if (TestTotalCount == 0)
                {
                    continue;
                }

                if (!IsTestsLoaded(stdWrap))
                {
                    LoadTests(stdWrap);
                }
                if (!withTests)
                {
                    stdWrap.ClearTests();
                }
                if (!allFlag && withTests)
                {
                    //假如一个都没有选，默认选中所有
                    if (stdWrap.Tests.Where(item => item.IsChecked == true).Count() == 0)
                    {
                        LoadTests(stdWrap);
                    }
                    else
                    {
                        // 1. 找出所有未勾选的 SampleDataItem
                        var uncheckedItems = stdWrap.Tests.Where(item => item.IsChecked == false).ToList();
                        // 2. 拿到它们对应的 TestSpecimen
                        var specimensToRemove = uncheckedItems.Select(item => item.Specimen).ToList();
                        // 3. 从 ICollection<TestSpecimen> 中删除
                        foreach (var sp in specimensToRemove)
                            stdWrap.StandardSpecimen.TestSpecimens.Remove((TestSpecimen)sp);
                    }
                }
                // 对试样重命名
                foreach (var testWrap in stdWrap.Tests)
                {
                    Rename(testWrap);
                }
            }

            UploadingStatus = S._("Ready");

            return list.ToArray();
        }

        /// <summary>
        /// 根据条件匹配数据
        /// </summary>
        /// <param name="stdWraps">数据列表</param>
        /// <param name="withTests">是否带试样数据</param>
        /// <param name="allFlag">是否全部导出</param>
        /// <returns></returns>
        IEnumerable<StandardDataItem> PrepareStandardDataItems(IEnumerable<StandardDataItem> stdWraps, bool withTests, bool allFlag)
        {
            stopUploadEvent.Reset();
            List<StandardDataItem> list = new List<StandardDataItem>();
            foreach (var stdWrap in stdWraps)
            {
                // list.Add(stdWrap.StandardSpecimen);
                Rename(stdWrap);
                if (TestTotalCount == 0)
                {
                    continue;
                }

                if (!IsTestsLoaded(stdWrap))
                {
                    LoadTests(stdWrap);
                }
                if (!withTests)
                {
                    stdWrap.ClearTests();
                }
                var stdWrapClone = stdWrap;
                if (!allFlag && withTests)
                {
                    //假如一个都没有选，默认选中所有
                    if (stdWrap.Tests.Where(item => item.IsChecked == true).Count() == 0)
                    {
                        LoadTests(stdWrap);
                    }
                    else
                    {

                        // 1. 找出所有未勾选的 SampleDataItem
                        var uncheckedItems = stdWrapClone.Tests.Where(item => item.IsChecked == false).ToList();
                        // 2. 拿到它们对应的 TestSpecimen
                        var specimensToRemove = uncheckedItems.Select(item => item.Specimen).ToList();
                        // 3. 从 ICollection<TestSpecimen> 中删除
                        foreach (var sp in uncheckedItems)
                            stdWrapClone.Tests.Remove(sp);
                    }
                }
                // 对试样重命名
                foreach (var testWrap in stdWrapClone.Tests)
                {
                    Rename(testWrap);
                }
                list.Add(stdWrapClone);
            }

            UploadingStatus = S._("Ready");

            return list.ToArray();
        }

        private string AskFilePathToExport(string defaultPath = null)
        {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "Excel Workbook(*.xlsx)|*.xlsx";
            if (defaultPath != null)
            {
                if (Path.IsPathRooted(defaultPath))
                    dlg.FileName = defaultPath;
                else
                    dlg.FileName = Path.Combine(dlg.InitialDirectory, defaultPath);
            }
            dlg.AddExtension = true;
            if (dlg.ShowDialog() != true)
                return null;

            return dlg.FileName;
        }
        private bool IfNot45_0Lilin(IEnumerable<StandardDataItem> stdWraps)
        {
            if (stdWraps == null || stdWraps.Count() == 0)
            {
                return true;
            }
            var tmp = stdWraps.ElementAt(0);
            if (tmp.Clr == null) return true;
            else return false;
        }
        private void ExportStandards(IEnumerable<StandardDataItem> stdWraps, bool withTests, bool allFlag = true)
        {
            string fileName = AskFilePathToExport(stdWraps.Count() == 1 ? (stdWraps.ElementAt(0).Name + ".xlsx") : null);
            if (fileName == null)
                return;
            bool ifNot45_0 = IfNot45_0Lilin(stdWraps);
            Mouse.OverrideCursor = Cursors.Wait;

            stdWraps = PrepareStandardDataItems(stdWraps, withTests, allFlag);

            try
            {
                XLWorkbook workbook = new XLWorkbook();
                IXLWorksheet sheet = workbook.AddWorksheet("Sheet1");
                //列名
                List<string> labels = new List<string>();
                foreach (var field in displayFields.DisplayFields)
                {
                    if (field == ChromaticityField.SpecularComponent)
                    {
                        if (ifNot45_0)
                            labels.Add("SCI/SCE");
                        else
                            labels.Add("45/0");
                    }
                    else
                    {
                        labels.Add(field.Name());
                    }
                }

                int row = 1;
                for (int col = 1; col <= labels.Count; col++)
                {
                    sheet.Cell(row, col).Value = labels[col - 1];
                }

                foreach (var std in stdWraps)
                {
                    writeStdColorDataToRow(std, sheet, ref row);

                    if (withTests && std.Tests != null)
                    {
                        foreach (var test in std.Tests)
                        {
                            writeSamColorDataToRow(test, sheet, ref row);
                        }
                    }
                }
                workbook.SaveAs(fileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(S._("SomethingWrongOnExcel") + ex.Message);
            }
            finally
            {
                Mouse.OverrideCursor = Cursors.Arrow;
            }
        }

        private void ExportTests(IEnumerable<SampleDataItem> tests)
        {
            string fileName = AskFilePathToExport();
            if (fileName == null)
                return;

            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                XLWorkbook workbook = new XLWorkbook();
                IXLWorksheet sheet = workbook.AddWorksheet("Sheet1");

                List<string> labels = new List<string>();
                foreach (var field in displayFields.DisplayFields)
                {
                    if (field == ChromaticityField.SpecularComponent)
                    {
                        labels.Add("SCI/SCE");
                    }
                    else
                    {
                        labels.Add(field.Name());
                    }
                }

                int row = 1;
                for (int col = 1; col <= labels.Count; col++)
                {
                    sheet.Cell(row, col).Value = labels[col - 1];
                }

                foreach (var test in tests)
                {
                    Rename(test);

                    if (test.SCI != null)
                    {
                        writeSamColorDataToRow(test, sheet, ref row);
                    }
                    if (test.SCE != null)
                    {
                        writeSamColorDataToRow(test, sheet, ref row);
                    }
                }

                workbook.SaveAs(fileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(S._("SomethingWrongOnExcel") + ex.Message);
            }
            finally
            {
                Mouse.OverrideCursor = Cursors.Arrow;
            }
        }

        MainWindow MainWindow
        {
            get { return Application.Current.MainWindow as MainWindow; }
        }

        private void ImportStandards(IEnumerable<StandardDataItem> stdWraps, bool withTests, bool allFlag)
        {
            Mouse.OverrideCursor = Cursors.Wait;

            var standards = PrepareStandards(stdWraps, withTests, allFlag);

            MainWindow.ImportStandardsViaStrategy(
                SpecimenImportSourceKind.InstrumentLibrary,
                standards,
                createNewId: true,
                isRemoveSame: false);

            Mouse.OverrideCursor = Cursors.Arrow;
            Close();
        }

        private void ImportTests(IEnumerable<SampleDataItem> tests)
        {
            Mouse.OverrideCursor = Cursors.Wait;

            List<TestSpecimen> list = new List<TestSpecimen>();
            foreach (var test in tests)
            {
                Rename(test);
                list.Add(test.Specimen as TestSpecimen);
                list.Last().StandardSpecimen = null;
            }

            MainWindow.ImportTests(list.ToArray(), null);
            Mouse.OverrideCursor = Cursors.Arrow;
            Close();
        }

        /// <summary>
        /// 重命名标样并设置料号
        /// </summary>
        /// <param name="standard">要重命名的标样</param>
        private void Rename(StandardDataItem standard)
        {
            standard.Specimen.Name = standardRenameRule.GenerateName(standard.Specimen);
            standard.Specimen.Material = material;
        }

        /// <summary>
        /// 重命名试样并设置料号
        /// </summary>
        /// <param name="test">要重命名的标样</param>
        private void Rename(SampleDataItem test)
        {
            test.Specimen.Name = testRenameRule.GenerateName(test.Specimen);
            test.Specimen.Material = material;
        }

        private bool ShowRenameRuleDlg(bool showStandard, bool showTest)
        {
            RenameRuleDialog dlg = new RenameRuleDialog()
            {
                Owner = this,
                Material = material,
                StandardNameRule = standardRenameRule.Rule,
                StandardNameRuleVariables = RenameRuleVariables.ToArray(),
                TestNameRule = testRenameRule.Rule,
                TestNameRuleVariables = RenameRuleVariables.ToArray(),
                IsShowStandardRenameRule = showStandard,
                IsShowTestRenameRule = showTest
            };

            if (dlg.ShowDialog() == true)
            {
                standardRenameRule.Rule = dlg.StandardNameRule;
                testRenameRule.Rule = dlg.TestNameRule;
                material = dlg.Material;

                return true;
            }
            return false;
        }


        StandardDataItem[] GetSelectedStandardItems(object source)
        {
            return StandardList.Where(p => p.IsChecked).ToArray();
            if (source == SpecimenDataGrid)
            {
                if (currentStandard == null)
                {
                    HashSet<StandardDataItem> selectedItems = new HashSet<StandardDataItem>();
                    foreach (var stdAdapter in SpecimenDataGrid.SelectedItems.Cast<SpecimenAdapter>())
                    {
                        selectedItems.Add(StandardList.First(x => x.Specimen == stdAdapter.Specimen));
                    }
                    return selectedItems.ToArray();
                }
            }
            else if (currentStandard != null)
            {
                return new StandardDataItem[] { currentStandard };
            }
            return new StandardDataItem[0];
        }


        private void OnImportSelectedStandardExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (ShowRenameRuleDlg(true, false))
            {
                ImportStandards(GetSelectedStandardItems(e.Source), false, false);
            }
        }
        private void OnImportSelectedStandardWithTestsExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (ShowRenameRuleDlg(true, true))
            {
                ImportStandards(GetSelectedStandardItems(e.Source), true, false);
            }
        }
        private void OnImportAllStandardsExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (ShowRenameRuleDlg(true, false))
            {
                ImportStandards(StandardList, false, true);
            }
        }
        private void OnImportAllStandardsWidthTestsExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (ShowRenameRuleDlg(true, true))
            {
                ImportStandards(StandardList, true, true);
            }
        }


        private void OnExportSelectedStandardExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (ShowRenameRuleDlg(true, false))
            {
                ExportStandards(GetSelectedStandardItems(e.Source), false, false);
            }
        }
        private void OnExportSelectedStandardWithTestsExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (ShowRenameRuleDlg(true, true))
            {
                ExportStandards(GetSelectedStandardItems(e.Source), true, false);
            }
        }

        private void OnExportAllStandardsExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (ShowRenameRuleDlg(true, false))
            {
                ExportStandards(StandardList, false);
            }
        }
        private void OnExportAllStandardsWidthTestsExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (ShowRenameRuleDlg(true, true))
            {
                ExportStandards(StandardList, true);
            }
        }

        private void OnDeleteAllStandardsExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (MessageBox.Show(S._("ConfirmDelAllSpecimensFromInstrument"),
                S._("DeleteAllSpecimens"), MessageBoxButton.YesNo,
                MessageBoxImage.Exclamation) == MessageBoxResult.Yes)
            {
                Mouse.OverrideCursor = Cursors.Wait;
                UploadingStatus = S._("Deleting");
                try
                {
                    Colorimeter.RemoveAllRecords();
                    StandardList.Clear();
                    StandardCount = 0;
                    datagridItems.Clear();
                    TestCount = 0;
                    TestTotalCount = 0;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, S._("DeleteAllSpecimens"),
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }

                UploadingStatus = S._("Ready");
                Mouse.OverrideCursor = Cursors.Arrow;
            }
        }
        private void OnDeleteAllTestsExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (MessageBox.Show(S._("ConfirmDelAllTestFromInstrument"),
                S._("DeleteTests"), MessageBoxButton.YesNo,
                MessageBoxImage.Exclamation) == MessageBoxResult.Yes)
            {
                Mouse.OverrideCursor = Cursors.Wait;
                UploadingStatus = S._("Deleting");
                try
                {
                    Colorimeter.RemoveAllSamples();
                    if (currentStandard != null)
                        datagridItems.Clear();

                    foreach (var standard in StandardList)
                    {
                        standard.Tests.Clear();
                        if (standard.StandardSpecimen.TestSpecimens != null)
                            standard.StandardSpecimen.TestSpecimens.Clear();
                    }
                    TestTotalCount = 0;
                    TestCount = 0;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, S._("DeleteTests"),
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }

                UploadingStatus = S._("Ready");
                Mouse.OverrideCursor = Cursors.Arrow;
            }
        }
        private void DeleteAllTestsCanExecuted(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = TestTotalCount > 0;
        }

        SampleDataItem[] GetSelectedTests()
        {
            HashSet<SampleDataItem> list = new HashSet<SampleDataItem>();
            foreach (var adapter in SpecimenDataGrid.SelectedItems.Cast<SpecimenAdapter>())
            {
                foreach (var test in currentStandard.Tests)
                {
                    if (test.SCI == adapter || test.SCE == adapter)
                    {
                        list.Add(test);
                        break;
                    }
                }
            }
            return list.ToArray();
        }


        private void OnImportSelectedTestExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (ShowRenameRuleDlg(false, true))
            {
                ImportTests(GetSelectedTests());
            }
        }
        private void OnImportAllTestsExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (ShowRenameRuleDlg(false, true) && currentStandard != null)
            {
                ImportTests(currentStandard.Tests);
            }
        }
        private void OnExportSelectedTestExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (ShowRenameRuleDlg(false, true))
            {
                ExportTests(GetSelectedTests());
            }
        }
        private void OnExportAllTestsExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (ShowRenameRuleDlg(false, true) && currentStandard != null)
            {
                ExportTests(currentStandard.Tests);
            }
        }

        private void OnDeleteTestsInListExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (MessageBox.Show(S._("ConfirmDelTestInListFromInstrument"),
                S._("DeleteTests"), MessageBoxButton.YesNo,
                MessageBoxImage.Exclamation) == MessageBoxResult.Yes)
            {
                try
                {
                    Colorimeter.RemoveAllSamplesById(currentStandard.ID);
                    datagridItems.Clear();
                    currentStandard.StandardSpecimen.TestSpecimens.Clear();
                    TestTotalCount -= currentStandard.Tests.Count;
                    TestCount = 0;
                    currentStandard.Tests.Clear();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, S._("DeleteTests"),
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ImportSelectedStandardCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (e.Source == SpecimenDataGrid)
            {
                e.CanExecute = currentStandard == null && SpecimenDataGrid.SelectedItems.Count > 0;
            }
            else
            {
                e.CanExecute = currentStandard != null;
            }
        }

        private void ImportAllStandardsCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = StandardList.Count > 0;
        }

        private void ImportSelectedTestCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (e.Source == SpecimenDataGrid)
            {
                e.CanExecute = SpecimenDataGrid.SelectedItems.Count > 0;
            }
            else
            {
                var wrap = SpecimenTreeView.SelectedItem as SpecimenWrap;
                e.CanExecute = wrap != null && wrap.Specimen is TestSpecimen;
            }
        }

        private void ImportAllTestsCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = e.Source == SpecimenDataGrid && SpecimenDataGrid.Items.Count > 0;
        }

        private void ObserverComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Formula.Observer = (sender as ObserverComboBox).Observer;

            RefreshDisplayData();
        }

        private void IlluminantComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Formula.Illuminant = (sender as IlluminantComboBox).Illuminant;

            RefreshDisplayData();
        }

        private void RefreshDisplayData()
        {
            Cursor = Cursors.Wait;

            foreach (var stdItem in StandardList)
            {
                if (stdItem.Specimen.SCI != null)
                {
                    stdItem.SCI = new StandardSpecimenAdapter(stdItem.StandardSpecimen,
                        Formula, displayFields, SpecularComponent.SCI);
                }
                if (stdItem.Specimen.SCE != null)
                {
                    stdItem.SCE = new StandardSpecimenAdapter(stdItem.StandardSpecimen,
                        Formula, displayFields, SpecularComponent.SCE);
                }
                if (stdItem.Specimen.Clr != null)
                {
                    stdItem.Clr = new StandardSpecimenAdapter(
                        stdItem.StandardSpecimen, Formula, displayFields, SpecularComponent.Unknown);
                }

                if (stdItem.Tests.Count > 0)
                {
                    foreach (var testItem in stdItem.Tests)
                    {
                        if (testItem.Specimen.SCI != null)
                        {
                            testItem.SCI = new TestSpecimenAdapter(
                                testItem.Specimen as TestSpecimen, Formula, displayFields,
                                SpecularComponent.SCI, stdItem.SCI as StandardSpecimenAdapter);
                        }
                        if (testItem.Specimen.SCE != null)
                        {
                            testItem.SCE = new TestSpecimenAdapter(
                                testItem.Specimen as TestSpecimen, Formula, displayFields,
                                SpecularComponent.SCE, stdItem.SCE as StandardSpecimenAdapter);
                        }

                        if (testItem.Specimen.Clr != null)
                        {
                            testItem.Clr = new TestSpecimenAdapter(
                                testItem.Specimen as TestSpecimen, Formula, displayFields,
                                SpecularComponent.Unknown, stdItem.Clr as StandardSpecimenAdapter);
                        }
                    }
                }
            }

            RefreshView();

            Cursor = Cursors.Arrow;
        }

        private void RefreshView()
        {
            if (SpecimenTreeView == null)
            {
                return;
            }
            if (SpecimenTreeView.SelectedItem == RootTreeItem)
            {
                ShowAllStandards();
            }
            else
            {
                ShowTestsOfCurrentStandard();
            }
        }

        private void OnDisplaySettingClick(object sender, RoutedEventArgs e)
        {
            WavelengthRange wavelengthRange = (Colorimeter as IDeviceInfo)?.WavelengthRange ?? new WavelengthRange();

            ChromaticitySettingDialog dlg = new ChromaticitySettingDialog(wavelengthRange);
            dlg.Owner = this;
            dlg.ChromaticitySetting = displayFields;
            if (dlg.ShowDialog().GetValueOrDefault())
            {
                if (dlg.IsModified)
                {
                    displayFields = dlg.ChromaticitySetting;
                    (Application.Current as App).SystemDefaultConfig.InstrumentDataDisplaySetting.Copy(displayFields);

                    SpecimenDataGrid.Fields = displayFields.DisplayFields;
                    RefreshDisplayData();
                }
            }
        }

        private SpecularComponent _sc = SpecularComponent.SCIAndSCE;

        private void SpecularComponentComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _sc = (sender as SpecularComponentComboBox).SpecularComponent;
            RefreshDisplayData();
        }


        private void OnDeleteSelectedStandards(object sender, ExecutedRoutedEventArgs e)
        {
            if (Colorimeter is ThreeNH.SQCX.Infrastructure.Instruments.BTS.Spectrometer ||
                Colorimeter is PTS.Spectrometer ||
                Colorimeter is ThreeNH.SQCX.Infrastructure.Instruments.BTS.BleSpectrophotometer ||
                Colorimeter is PTS.BleSpectrophotometer ||
                Colorimeter is SQCX.Infrastructure.Instruments.YHComm.BleComm ||
                Colorimeter is USBComm ||
                Colorimeter is SQCX.Infrastructure.Instruments.Spectrophotometer.TS8450)
            {
                Loading loading = new Loading("Deleting,Please wait...");
                loading.BackgroundWork = (o, p, a) =>
                {
                    var items = GetSelectedStandardItems(e.Source);
                    try
                    {
                        foreach (var item in items)
                        {
                            Colorimeter.DeleteStandardById(item.ID);
                            Thread.Sleep(200);
                            Dispatcher.Invoke(new Action(() =>
                            {
                                StandardList.Remove(item);
                            }));
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, S._("MenuItem_DeleteSelectedStandards"),
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                };
                loading.BackgroundWorkerCompleted += (m, n) => { };
                loading.Start();
            }
        }

        private void CanDeleteSelectedStandards(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = Colorimeter is ThreeNH.SQCX.Infrastructure.Instruments.BTS.Spectrometer ||
                           Colorimeter is PTS.Spectrometer ||
                Colorimeter is ThreeNH.SQCX.Infrastructure.Instruments.BTS.BleSpectrophotometer ||
                Colorimeter is PTS.BleSpectrophotometer ||
                Colorimeter is USBComm ||
                Colorimeter is BleComm ||
                Colorimeter is TS8450;
        }

        private void OnDeleteSelectedTestsExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (MessageBox.Show(S._("ConfirmDelTestInListFromInstrument"), S._("DeleteTests"), MessageBoxButton.YesNo, MessageBoxImage.Exclamation) == MessageBoxResult.Yes)
            {
                Loading loading = new Loading("Deleting,Please wait...");
                loading.BackgroundWork = (o, p, a) =>
                {
                    try
                    {
                        foreach (var standard in StandardList)
                        {
                            var tests = standard.Tests.OrderByDescending(t => t.ID);
                            foreach (var sample in tests)
                            {
                                if (sample.IsChecked)
                                {
                                    Colorimeter.DeleteSampleById(standard.ID, sample.ID - 1);
                                    Dispatcher.Invoke(new Action(() =>
                                    {
                                        standard.Tests.Remove(sample);
                                    }));
                                    Thread.Sleep(200);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, S._("DeleteTests"),
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                };
                loading.BackgroundWorkerCompleted += (m, n) =>
                {
                    this.Dispatcher.Invoke(new Action(() =>
                    {
                        OnPropertyChanged(nameof(StandardList));
                    }));

                };
                loading.Start();
            }
        }

        private void DeleteSelectedTestsCanExecuted(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = Colorimeter is ThreeNH.SQCX.Infrastructure.Instruments.BTS.Spectrometer ||
               Colorimeter is PTS.Spectrometer ||
                Colorimeter is ThreeNH.SQCX.Infrastructure.Instruments.BTS.BleSpectrophotometer ||
                Colorimeter is PTS.BleSpectrophotometer;
        }
    }
}
