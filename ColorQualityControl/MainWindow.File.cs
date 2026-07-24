using DocumentFormat.OpenXml.Bibliography;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Xps.Packaging;
using System.Xml;
using ThreeNH.ColorAlgorithm;
using ThreeNH.ColorimeterInterface;
using ThreeNH.ColorQualityControl.Adapter;
using ThreeNH.ColorQualityControl.Reports;
using ThreeNH.ColorQualityControl.UI;
using ThreeNH.ColorSpecimen;
using ThreeNH.Controls;
using ThreeNH.DataModel;
using ThreeNH.Helper;
using ThreeNH.Optical;
using ThreeNH.ColorQualityControl.Refactoring;
using ThreeNH.ColorQualityControl.Services;
using ThreeNH.ColorQualityControl.ViewModels.Charts;

namespace ThreeNH.ColorQualityControl
{
    public partial class MainWindow
    {
        /// <summary>报表导出时使用的默认色差图类型。</summary>
        internal static DeltaPlotType[] DefaultVisibleDeltaPlots { get; } = { DeltaPlotType.DeltaLab };

        private void NewFile()
        {
            ResetJobDocument();
            RefreshTitle();
        }

        /// <summary>启动时若有未保存备份，询问是否恢复（阶段4-T02 自 OnLoaded 抽出）。</summary>
        private void TryRestoreLatestBackup()
        {
            var backupFileName = _session.JobDocument.GetLatestBackup();
            if (backupFileName?.BackupPath == null)
            {
                return;
            }

            var result = MessageBox.Show(S._("ConfirmLoadUnsavedFile"), Title, MessageBoxButton.YesNo);
            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            LoadXml(backupFileName.BackupPath);
            Database.IsModified = true;
            try
            {
                var path = SystemDefaultConfig.RecentFiles.First(_ =>
                    Path.GetFileName(_) == backupFileName.FileName);
                Database.FilePath = path;
            }
            catch
            {
                Database.FilePath = null;
            }
            RefreshDisplay();
        }

        private void SaveAsExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = _("FileFilter"); // "Xml 文件|*.xml";
            if (dlg.ShowDialog().GetValueOrDefault())
            {
                Save(dlg.FileName);
                SystemDefaultConfig.AddRecentFile(dlg.FileName);
                RefreshTitle();
                _session.JobDocument.RemoveBackupFiles();
            }
        }

        /// <summary>阶段1-T19：从 UI 状态采集 ViewSetting，由 JobDocumentService 写入 XML。</summary>
        private JobViewSettings CaptureJobViewSettings()
        {
            return new JobViewSettings
            {
                SpecularComponent = SpecularComponent,
                Illuminant = Illuminant,
                Observer = Observer,
                IsCanUseHaze = IsCanUseHaze
            };
        }

        /// <summary>阶段1-T19：加载后应用 ViewSetting；数据库侧观察者/光源由 ApplyJobDocumentLoadResult 先行设置。</summary>
        private void ApplyJobViewSettings(JobViewSettings settings)
        {
            if (settings == null)
            {
                return;
            }

            if (settings.HasSpecularComponent)
            {
                SpecularComponent = settings.SpecularComponent;
            }

            if (settings.HasObserverIlluminant)
            {
                ObserverIlluminant = new ObserverIlluminantPair(settings.Observer, settings.Illuminant);
            }

            if (settings.HasIsCanUseHaze)
            {
                IsCanUseHaze = settings.IsCanUseHaze;
            }
        }

        /// <summary>阶段1-T19：Service 加载完成后，MainWindow 负责光源下拉与 ViewSetting 刷新。</summary>
        private void ApplyJobDocumentLoadResult(JobDocumentLoadResult result)
        {
            ResetIlluminantComboBox(result.ObserverAndIlluminantSetting);
            ObserverIlluminant = result.ObserverAndIlluminantSetting.Primary;
            SpecularComponent = result.SpecularComponent;
            ApplyJobViewSettings(result.ViewSettings);
        }

        private void Save(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToUpperInvariant();
            if (extension == ".QTX")
            {
                try
                {
                    _session.JobDocument.SaveToFile(fileName, CaptureJobViewSettings(), SpecularComponent);
                }
                catch (IOException ex)
                {
                    MessageBox.Show(ex.Message);
                }

                return;
            }

            Cursor = Cursors.Wait;
            try
            {
                _session.JobDocument.SaveToFile(fileName, CaptureJobViewSettings(), SpecularComponent);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                Cursor = Cursors.Arrow;
            }
        }

        private void SaveExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (Database.FilePath == null)
            {
                ApplicationCommands.SaveAs.Execute(null, this);
            }
            else
            {
                Save(Database.FilePath);
                _session.JobDocument.RemoveBackupFiles();
            }
        }

        /// <summary>关闭/打开/新建前确认是否保存；MessageBox 留在 MainWindow（阶段1-T22）。</summary>
        public bool ConfirmSave()
        {
            if (!Database.IsModified)
            {
                return true;
            }

            var result = MessageBox.Show(_("ToConfirmSaveFile"), Title, MessageBoxButton.YesNoCancel);
            if (result == MessageBoxResult.Cancel)
            {
                Database.IsModified = true;
                return false;
            }

            if (result == MessageBoxResult.Yes)
            {
                ApplicationCommands.Save.Execute(null, this);
            }

            return true;
        }

        private void LoadXml(string fileName)
        {
            var result = _session.JobDocument.LoadFromFile(fileName);
            if (!result.Success)
            {
                MessageBox.Show(result.ErrorMessage);
                return;
            }

            ApplyJobDocumentLoadResult(result);
        }

        private void LoadQTX(string fileName)
        {
            var result = _session.JobDocument.LoadFromFile(fileName);
            if (!result.Success)
            {
                MessageBox.Show(result.ErrorMessage);
                return;
            }

            ApplyJobDocumentLoadResult(result);
        }

        private void OpenExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (!ConfirmSave())
                return;

            NewFile();

            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = _("FileFilter"); //"Xml 文件|*.xml";
            if (dlg.ShowDialog().GetValueOrDefault())
            {
                //OpenFile(dlg.FileName);
                OpenFileEx(dlg.FileName);
            }
        }

        private bool OpenFile(string fileName)
        {
            Mouse.OverrideCursor = Cursors.Wait;

            FileInfo fileinfo = new FileInfo(fileName);
            try
            {
                if (fileinfo.Extension.ToUpper() == ".QTX")
                {
                    LoadQTX(fileName);
                }
                else
                {
                    LoadXml(fileName);
                }
            }
            catch (Exception ex)
            {
                Mouse.OverrideCursor = Cursors.Arrow;
                MessageBox.Show(ex.Message);
                return false;
            }

            Database.FilePath = fileName;
            RefreshDisplay();

            SystemDefaultConfig.AddRecentFile(fileName);
            Mouse.OverrideCursor = Cursors.Arrow;
            return true;
        }

        private void OpenFileEx(string fileName)
        {
            Loading loading = new Loading("Loading,Please wait...");
            loading.BackgroundWork = (m, n, p) =>
            {
                JobDocumentLoadResult result;
                var isQtx = Path.GetExtension(fileName).ToUpperInvariant() == ".QTX";
                try
                {
                    if (isQtx)
                    {
                        // QTX 解析可在后台线程完成（与原 OpenFileEx 一致）
                        using (var reader = File.OpenText(fileName))
                        {
                            result = _session.JobDocument.LoadQtxFromReader(fileName);
                        }
                    }
                    else
                    {
                        var doc = new XmlDocument();
                        doc.Load(fileName);
                        // XML 入库仍在 UI 线程，避免与界面绑定并发访问 Database
                        result = null;
                        Dispatcher.Invoke(new Action(() =>
                        {
                            result = _session.JobDocument.LoadFromXmlDocument(doc, fileName);
                        }));
                    }
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(new Action(() => MessageBox.Show(ex.Message)));
                    return;
                }

                if (result == null || !result.Success)
                {
                    if (result != null)
                    {
                        Dispatcher.Invoke(new Action(() => MessageBox.Show(result.ErrorMessage)));
                    }

                    return;
                }

                var loadResult = result;
                Dispatcher.Invoke(new Action(() =>
                {
                    ApplyJobDocumentLoadResult(loadResult);
                    Database.FilePath = fileName;
                    RefreshDisplay();
                    SystemDefaultConfig.AddRecentFile(fileName);
                }));
            };
            loading.BackgroundWorkerCompleted += (o, p) => { };
            loading.Start();
        }

        private void RefreshTitle()
        {
#if false
            if (App.Title == "SQCX")
                Title = string.Format("{0} - {1}", Database.FilePath ?? "new", App.Title);//20191023lilin zhong only this line
            else
                Title = App.Title;
# else
            Title = App.Title;
#endif
        }

        private void RefreshDisplay()
        {
            ResetDisplayedFields();
            LoadAllSpecimens();
            if (StandardGroupTreeItem.Items.Count != 0)
            {
                StandardGroupTreeItem.IsSelected = true;
                StandardGroupTreeItem.IsExpanded = true;
            }
            else
            {
                AllTestsTreeItem.IsSelected = true;
                AllTestsTreeItem.IsExpanded = true;
            }
            RefreshTitle();
            RefreshCharts();
        }

        private void NewExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (!ConfirmSave())
                return;

            NewFile();
        }

        private SpecimenExportService CreateExportService() => new SpecimenExportService(Database);

        /// <summary>
        /// 询问用户保存导出数据的文件；如果用户取消，或打开文件出错，返回null
        /// </summary>
        /// <returns></returns>
        private IMyWriter AskFileNameToExport()
        {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = _("ExportFileFilter"); // "Excel Workbook(*.xlsx)|*.xlsx|Text(*.txt)|*.txt";
            if (Database.FilePath != null)
            {
                dlg.InitialDirectory = Path.GetDirectoryName(Database.FilePath);
                dlg.FileName = Path.GetFileNameWithoutExtension(Database.FilePath) + ".xlsx";
            }
            else
            {
                dlg.FileName = DateTime.Now.ToString("yyyy-MM-dd") + ".xlsx";
            }

            if (dlg.ShowDialog() != true)
                return null;

            FileInfo fileInfo = new FileInfo(dlg.FileName);

            IMyWriter myWriter = fileInfo.Extension.ToLower() == ".txt"
                ? new MyTextWriter()
                : myWriter = new MyExcelWriter();

            try
            {
                myWriter.Create(dlg.FileName);
                return myWriter;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return null;
            }
        }

        private void ExportAllExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            IMyWriter myWriter = AskFileNameToExport();
            if (myWriter == null)
                return;

            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                // 导出行/列构建已下沉 SpecimenExportService（阶段4-T03）
                var export = CreateExportService();
                var helper = GetSpecimenAdapterHelper(Database.SystemChromaticitySetting);
                export.ExportAll(Database.SystemChromaticitySetting, myWriter, helper);
                myWriter.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                Mouse.OverrideCursor = Cursors.Arrow;
            }
        }

        private void ExportSelectedExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            IMyWriter myWriter = AskFileNameToExport();
            if (myWriter == null)
                return;

            var wraps = GetSelectedSpecimens(e.Source);

            try
            {
                var export = CreateExportService();
                var helper = GetSpecimenAdapterHelper(ChromaticitySetting);
                myWriter.WriteRow(export.MakeColumnLabels(helper, false));
                foreach (var wrap in wraps)
                {
                    export.WriteSpecimenRows(myWriter, helper, wrap.Specimen, false, false);
                }

                myWriter.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public SpecimenAdapterHelper GetSpecimenAdapterHelper(ChromaticitySetting setting)
        {
            Formula formula = Database.GetFormula(Observer, Illuminant);
            Formula ref1, ref2;
            Database.GetReferenceFormula(formula, out ref1, out ref2);
            if (ref1 != null && ref2 != null)
            {
                return new SpecimenAdapterHelper(formula, setting, SpecularComponent, referenceFormula1: ref1, referenceFormula2: ref2);
            }
            else
            {
                return new SpecimenAdapterHelper(formula, setting, SpecularComponent);
            }

        }

        private void ExportSelectedWithTestsExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            IMyWriter myWriter = AskFileNameToExport();
            if (myWriter == null)
                return;

            var export = CreateExportService();
            var helper = GetSpecimenAdapterHelper(ChromaticitySetting);
            var standards = GetSelectedStandards(e.Source);
            foreach (var std in standards)
            {
                helper.ChromaticitySetting = Database.GetChromaticitySetting(std);
                export.ExportStandardWithTests(myWriter, std, helper, true);
                myWriter.WriteRow(null);
            }

            myWriter.Close();
        }

        private PrintTicket _printTicket;

        private void PrinterSetupExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            PrintDialog dlg = new PrintDialog();
            if (_printTicket != null)
                dlg.PrintTicket = _printTicket;

            if (dlg.ShowDialog().GetValueOrDefault())
            {
                _printTicket = dlg.PrintTicket;
            }
        }

        private FlowDocument GetReportDocument(object source)
        {
            if (SingleStandardInShowing == null)
            {
                MessageBox.Show(_("PleaseSelectedAStandard"));
                return null;
            }

            FlowDocumentReportBase report = null;

            var tests = GetSelectedTests(source);
            if (tests.Length == 1)
            {
                ColorDiffReport diffReport = new ColorDiffReport();
                diffReport.Test = tests[0];
                diffReport.IsExportChromaticityChart = Database.ReportSetting.IsExportChromaticityChart;
                report = diffReport;
            }
            else
            {
                if (tests.Length == 0 && SingleStandardInShowing.TestSpecimens == null ||
                    SingleStandardInShowing.TestSpecimens.Count == 0)
                {
                    MessageBox.Show(_("TheStandardHasNoTests"));
                    return null;
                }

                StatisticsReport statisticsReport = new StatisticsReport();
                statisticsReport.Tests = tests.Length != 0 ? tests : SingleStandardInShowing.TestSpecimens.ToArray();
                statisticsReport.IsExportChromaticityChart = Database.ReportSetting.IsExportChromaticityChart;
                report = statisticsReport;
            }

            report.ReportSetting = Database.ReportSetting;
            report.Standard = SingleStandardInShowing;
            report.ChromaticitySetting = ChromaticitySetting;
            report.SpecularComponent = SpecularComponent;
            report.Formula = Formula;
            Formula ref1, ref2;
            Database.GetReferenceFormula(Formula, out ref1, out ref2);
            report.Tolerance = Database.GetTolerance(report.Standard, new ObserverIlluminantPair(Observer, Illuminant));
            report.InstrumentModel = (Colorimeter as IDeviceInfo)?.Model ?? "";
            report.VisibleDeltaPlots = DefaultVisibleDeltaPlots;
            report.ToleranceSetting = ToleranceSetting;
            report.ObserverAndIlluminantSetting = Database.ObserverAndIlluminantSetting;
            report.ReferenceFormula1 = ref1;
            report.ReferenceFormula2 = ref2;

            return report.GetFlowDocument();
        }

        private void PrintExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            PrintDialog dlg = new PrintDialog();
            if (_printTicket != null)
                dlg.PrintTicket = _printTicket;

            if (dlg.ShowDialog().GetValueOrDefault())
            {
                _printTicket = dlg.PrintTicket;

                var doc = GetReportDocument(e.Source);
                if (doc != null)
                {
                    doc.PageWidth = dlg.PrintableAreaWidth;
                    doc.PageHeight = dlg.PrintableAreaHeight;
                    doc.ColumnWidth = dlg.PrintableAreaWidth;
                    doc.FontFamily = FontFamily;

                    dlg.PrintDocument(((IDocumentPaginatorSource)doc).DocumentPaginator, _("ColorDiffReport"));
                }
            }
        }

        private void PrintPreviewExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var doc = GetReportDocument(e.Source);
            if (doc == null) // 出错
                return;

            doc.FontFamily = FontFamily;

            Window window = new Window();
            Page page = new Page();
            page.Content = doc;
            Frame frame = new Frame();
            frame.Content = page;
            window.Content = frame;
            window.Width = doc.PageWidth / 2;
            window.Height = doc.PageHeight / 2;
            window.ShowDialog();
        }

        private void PrintToPDFExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var flowDocument = GetReportDocument(e.Source);
            if (flowDocument == null) // 出错
                return;
            SaveFileDialog dlg = new SaveFileDialog
            {
                DefaultExt = ".pdf",
                Filter = "PDF Documents (.pdf)|*.pdf"
            };

            if (dlg.ShowDialog().GetValueOrDefault())
            {
                try
                {
                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        System.IO.Packaging.Package package = System.IO.Packaging.Package.Open(memoryStream, FileMode.Create);
                        XpsDocument xpsDocument = new XpsDocument(package);
                        System.Windows.Xps.XpsDocumentWriter xpsDocumentWriter = XpsDocument.CreateXpsDocumentWriter(xpsDocument);

                        xpsDocumentWriter.Write(((IDocumentPaginatorSource)flowDocument).DocumentPaginator);
                        xpsDocument.Close();
                        package.Close();

                        var pdfXpsDoc = PdfSharp.Xps.XpsModel.XpsDocument.Open(memoryStream);
                        PdfSharp.Xps.XpsConverter.Convert(pdfXpsDoc, dlg.FileName, 0);
                        System.Diagnostics.Process.Start(dlg.FileName);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Print to PDF failed:{ex.Message}{ex.StackTrace}");
                }
            }
        }

        private void PrintToWordExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (SingleStandardInShowing == null)
            {
                MessageBox.Show(_("PleaseSelectedAStandard"));
                return;
            }

            WordReportBase report = null;

            var tests = GetSelectedTests(e.Source).ToList().OrderBy(m => m.Name).ToArray();

            if (tests.Length == 1)
            {
                //                WordColorDiffReport diffReport = new WordColorDiffReport();
                ColorDiffWordReport diffReport = new ColorDiffWordReport();
                diffReport.Test = tests[0];
                diffReport.IsExportChromaticityChart = Database.ReportSetting.IsExportChromaticityChart;
                report = diffReport;
            }
            else
            {
                if (tests.Length == 0 && SingleStandardInShowing.TestSpecimens == null ||
                    SingleStandardInShowing.TestSpecimens.Count == 0)
                {
                    MessageBox.Show(_("TheStandardHasNoTests"));
                    return;
                }

                WordStatisticsReport statisticsReport = new WordStatisticsReport();
                statisticsReport.Tests = tests.Length != 0 ? tests : SingleStandardInShowing.TestSpecimens.ToArray();
                statisticsReport.IsExportChromaticityChart = Database.ReportSetting.IsExportChromaticityChart;
                report = statisticsReport;
            }

            report.ReportSetting = Database.ReportSetting;
            report.Standard = SingleStandardInShowing;
            report.ChromaticitySetting = ChromaticitySetting;
            report.SpecularComponent = SpecularComponent;
            report.Formula = Formula;
            Formula ref1, ref2;
            Database.GetReferenceFormula(Formula, out ref1, out ref2);
            report.Tolerance = Database.GetTolerance(report.Standard, new ObserverIlluminantPair(Observer, Illuminant));
            report.InstrumentModel = (Colorimeter as IDeviceInfo)?.Model ?? "";//190923lilin 打印文件最后的仪器型号
            report.VisibleDeltaPlots = DefaultVisibleDeltaPlots;
            report.ToleranceSetting = Database.GetToleranceSetting(SingleStandardInShowing);
            report.ObserverAndIlluminantSetting = Database.ObserverAndIlluminantSetting;
            report.ReferenceFormula1 = ref1;
            report.ReferenceFormula2 = ref2;

            report.GenerateReport();
        }

        private void SetReportInfoExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            ReportSettingDialog dlg = new ReportSettingDialog();
            dlg.Owner = this;
            dlg.ReportSetting = Database.ReportSetting;

            if (dlg.ShowDialog().GetValueOrDefault())
            {
                Database.ReportSetting = dlg.ReportSetting;
                Database.IsModified = true;

                SystemDefaultConfig.ReportSetting.Copy(dlg.ReportSetting);
            }
        }

        private void OnClickRecentFile(object sender, RoutedEventArgs e)
        {
            var item = sender as MenuItem;
            if (!(item?.Tag is string)) return;

            var path = (string)item.Tag;
            // 如果是当前文件返回
            if (path == Database.FilePath)
            {
                return;
            }

            if (!File.Exists(path))
            {
                MessageBox.Show(S._("FileHasBeenRemoved"));
                SystemDefaultConfig.RemoveFromRecentFileList(path);
                return;
            }

            if (ConfirmSave())
            {
                NewFile();
                OpenFile(path);
            }
        }

        private void FileMenuItem_OnSubmenuOpened(object sender, RoutedEventArgs e)
        {
            var recentFiles = SystemDefaultConfig.RecentFiles;
            if (recentFiles.Length == 0)
            {
                RecentFilesMenuItem.IsEnabled = false;
                return;
            }

            RecentFilesMenuItem.IsEnabled = true;
            RecentFilesMenuItem.Items.Clear();
            for (int i = 0; i < recentFiles.Length; i++)
            {
                var item = new MenuItem
                {
                    Header = Path.GetFileName(recentFiles[i]),
                    ToolTip = recentFiles[i],
                    Tag = recentFiles[i]
                };
                item.Click += OnClickRecentFile;
                RecentFilesMenuItem.Items.Add(item);
            }
        }


        private void ExportOneNMSpectrumCanExexuted(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = Database.StandardSpecimens.Length > 0;
        }

        private void ExportOneNMSpectrumExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                SaveFileDialog dlg = new SaveFileDialog();
                dlg.Filter = "Excel Workbook(*.xlsx)|*.xlsx"; ; // "Excel Workbook(*.xlsx)|*.xlsx|Text(*.txt)|*.txt";
                if (Database.FilePath != null)
                {
                    dlg.InitialDirectory = Path.GetDirectoryName(Database.FilePath);
                    dlg.FileName = Path.GetFileNameWithoutExtension(Database.FilePath) + ".xlsx";
                }
                else
                {
                    dlg.FileName = DateTime.Now.ToString("yyyy-MM-dd") + ".xlsx";
                }

                if (dlg.ShowDialog() != true) return;

                FileInfo fileInfo = new FileInfo(dlg.FileName);
                var begin = 360;
                var end = 780;
                var interval = 1;
                IMyWriter myWriter = myWriter = new MyExcelWriter();
                myWriter.Create(dlg.FileName);
                var waveLengths = new List<int>();
                for (var waveLength = begin; waveLength <= end; waveLength++)
                {
                    waveLengths.Add(waveLength);
                }
                var labels = new List<string>() { "Name", "DateTime", "Channel" };
                var waveLengthLabels = Array.ConvertAll(waveLengths.ToArray(), x => x.ToString());
                labels = labels.Union(waveLengthLabels).ToList();
                myWriter.WriteRow(labels.ToArray());
                foreach (var std in Database.StandardSpecimens)
                {
                    var columns = GetSpecimenData(std, begin, end, interval);
                    foreach (var column in columns)
                    {
                        myWriter.WriteRow(column);
                    }
                    if (std.TestSpecimens != null)
                    {
                        foreach (var test in std.TestSpecimens)
                        {
                            var testColumns = GetSpecimenData(test, begin, end, interval);
                            foreach (var testColumn in testColumns)
                            {
                                myWriter.WriteRow(testColumn);
                            }
                        }
                    }

                    //row += 1; // 空一行
                    myWriter.WriteRow(null);
                }
                myWriter.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        private void ExportFiveNMSpecimensCanExexuted(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = Database.StandardSpecimens.Length > 0;
        }

        private void ExportFiveNMSpectrumExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                SaveFileDialog dlg = new SaveFileDialog();
                dlg.Filter = "Excel Workbook(*.xlsx)|*.xlsx"; ; // "Excel Workbook(*.xlsx)|*.xlsx|Text(*.txt)|*.txt";
                if (Database.FilePath != null)
                {
                    dlg.InitialDirectory = Path.GetDirectoryName(Database.FilePath);
                    dlg.FileName = Path.GetFileNameWithoutExtension(Database.FilePath) + ".xlsx";
                }
                else
                {
                    dlg.FileName = DateTime.Now.ToString("yyyy-MM-dd") + ".xlsx";
                }

                if (dlg.ShowDialog() != true) return;

                FileInfo fileInfo = new FileInfo(dlg.FileName);
                var begin = 360;
                var end = 780;
                var interval = 5;
                IMyWriter myWriter = myWriter = new MyExcelWriter();
                myWriter.Create(dlg.FileName);
                var waveLengths = new List<int>();
                for (var waveLength = begin; waveLength <= end; waveLength += interval)
                {
                    waveLengths.Add(waveLength);
                }
                var labels = new List<string>() { "Name", "DateTime", "Channel" };
                var waveLengthLabels = Array.ConvertAll(waveLengths.ToArray(), x => x.ToString());
                labels = labels.Union(waveLengthLabels).ToList();
                myWriter.WriteRow(labels.ToArray());
                foreach (var std in Database.StandardSpecimens)
                {
                    var columns = GetSpecimenData(std, begin, end, interval);
                    foreach (var column in columns)
                    {
                        myWriter.WriteRow(column);
                    }
                    if (std.TestSpecimens != null)
                    {
                        foreach (var test in std.TestSpecimens)
                        {
                            var testColumns = GetSpecimenData(test, begin, end, interval);
                            foreach (var testColumn in testColumns)
                            {
                                myWriter.WriteRow(testColumn);
                            }
                        }
                    }

                    //row += 1; // 空一行
                    myWriter.WriteRow(null);
                }
                myWriter.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        List<string[]> GetSpecimenData(Specimen std, int begin, int end, int interval)
        {
            var list = new List<string[]>();
            if (std.SCI != null)
            {
                var stdLabels = new List<string>
                        {
                            std.Name,
                            std.DateTime.ToString("yyyy-MM-dd HH:mm:ss"),
                            "SCI"
                        };
                var sci = std.SCI.Spectrum;
                var spectrum = Formula.Interpolate(sci, begin, end, interval);
                stdLabels.AddRange(Array.ConvertAll(spectrum.Data, o => (o * 100).ToString("F2")));
                list.Add(stdLabels.ToArray());
            }
            if (std.SCE != null)
            {
                var stdLabels = new List<string>
                        {
                            std.Name,
                            std.DateTime.ToString("yyyy-MM-dd HH:mm:ss"),
                            "SCE"
                        };
                var sce = std.SCE.Spectrum;
                var spectrum = Formula.Interpolate(sce, begin, end, interval);
                stdLabels.AddRange(Array.ConvertAll(spectrum.Data, o => (o * 100).ToString("F2")));
                list.Add(stdLabels.ToArray());
            }
            if (std.Clr != null)
            {
                var stdLabels = new List<string>
                        {
                            std.Name,
                            std.DateTime.ToString("yyyy-MM-dd HH:mm:ss"),
                            "Clr"
                        };
                var clr = std.Clr.Spectrum;
                var spectrum = Formula.Interpolate(clr, begin, end, interval);
                stdLabels.AddRange(Array.ConvertAll(spectrum.Data, o => (o * 100).ToString("F2")));
                list.Add(stdLabels.ToArray());
            }
            return list;
        }
    }
}