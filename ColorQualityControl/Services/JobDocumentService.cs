using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using ThreeNH.ColorQualityControl.Adapter;
using ThreeNH.ColorQualityControl.Specimens.Events;
using ThreeNH.ColorSpecimen;
using ThreeNH.DataModel;
using ThreeNH.Optical;

namespace ThreeNH.ColorQualityControl.Services
{
    /// <summary>
    /// 阶段 1：Job 标试样数据库层与文件持久化；TreeView / 图表刷新仍由 MainWindow 编排。
    /// </summary>
    internal sealed class JobDocumentService
    {
        public event EventHandler<SpecimenAddedEventArgs> SpecimenAdded;

        public event EventHandler<SpecimenRemovedEventArgs> SpecimenRemoved;

        public SpecimenDatabase Database { get; }

        public JobDocumentService(SpecimenDatabase database = null)
        {
            // 数据库实例由服务持有，MainWindow.Database 仅作只读转发，避免多处 new
            Database = database ?? new SpecimenDatabase();
        }

        public void PrepareStandardName(StandardSpecimen standard)
        {
            if (standard.Name == "")
            {
                standard.Name = Database.StandardNamgeGenerator.GenerateName(standard, Database.GetNewStandardIndex());
            }
        }

        public bool HasDuplicateStandardName(StandardSpecimen standard)
        {
            return FindStandardByName(standard.Name) != null;
        }

        public StandardSpecimen FindStandardByName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            return Database.StandardSpecimens.FirstOrDefault(s => s.Name == name);
        }

        /// <summary>导入标样及其下属试样写入 Job（阶段1-T18，不含 Gloss 计算与 UI）。</summary>
        public void ImportStandardWithTests(
            StandardSpecimen standard,
            bool createNewId,
            SpecularComponent specularComponent,
            StandardObserver observer,
            StandardIlluminant illuminant)
        {
            CommitStandard(standard, createNewId, specularComponent, observer, illuminant);
            if (standard.TestSpecimens == null)
            {
                return;
            }

            foreach (var test in standard.TestSpecimens.ToArray())
            {
                CommitTest(test, standard, specularComponent, observer, illuminant);
            }
        }

        public void AddStandardToDatabase(StandardSpecimen standard, bool createNewId = true)
        {
            Database.AddStandard(standard, createNewId);
        }

        /// <summary>标样写入 Job 并返回视图 adapter（阶段1-T14，UI 刷新仍由 MainWindow 负责）。</summary>
        public StandardSpecimenAdapter CommitStandard(
            StandardSpecimen standard,
            bool createNewId,
            SpecularComponent specularComponent,
            StandardObserver observer,
            StandardIlluminant illuminant)
        {
            PrepareStandardName(standard);
            AddStandardToDatabase(standard, createNewId);
            var adapter = GetStandardAdapter(standard, specularComponent, observer, illuminant);
            SpecimenAdded?.Invoke(this, new SpecimenAddedEventArgs(standard));
            return adapter;
        }

        /// <summary>试样写入 Job 并返回视图 adapter（阶段1-T15）。</summary>
        public TestSpecimenAdapter CommitTest(
            TestSpecimen test,
            StandardSpecimen standard,
            SpecularComponent specularComponent,
            StandardObserver observer,
            StandardIlluminant illuminant)
        {
            PrepareTestName(test, standard);
            AddTestToDatabase(test, standard);
            var adapter = GetTestAdapter(test, specularComponent, observer, illuminant);
            SpecimenAdded?.Invoke(this, new SpecimenAddedEventArgs(standard, test));
            return adapter;
        }

        public void PrepareTestName(TestSpecimen test, StandardSpecimen standard)
        {
            if (test.Name == "")
            {
                test.Name = Database.TestNameGenerator.GenerateName(test, Database.GetNewTestIndex(standard));
            }
        }

        public void ValidateStandardExists(StandardSpecimen standard)
        {
            if (standard != null && !Database.IsExists(standard))
            {
                throw new ArgumentException("standard not exists");
            }
        }

        public void AddTestToDatabase(TestSpecimen test, StandardSpecimen standard)
        {
            ValidateStandardExists(standard);
            Database.AddTest(test, standard);
        }

        public StandardSpecimenAdapter GetStandardAdapter(
            StandardSpecimen standard,
            SpecularComponent specularComponent,
            StandardObserver observer,
            StandardIlluminant illuminant)
        {
            return Database.GetAdapter(standard, specularComponent, observer, illuminant);
        }

        public TestSpecimenAdapter GetTestAdapter(
            TestSpecimen test,
            SpecularComponent specularComponent,
            StandardObserver observer,
            StandardIlluminant illuminant)
        {
            return Database.GetAdapter(test, specularComponent, observer, illuminant);
        }

        public void RemoveStandardFromDatabase(StandardSpecimen standard)
        {
            Database.RemoveStandard(standard);
            SpecimenRemoved?.Invoke(this, new SpecimenRemovedEventArgs(standard));
        }

        public void RemoveTestFromDatabase(TestSpecimen test)
        {
            var parent = test?.StandardSpecimen;
            Database.RemoveTest(test);
            SpecimenRemoved?.Invoke(this, new SpecimenRemovedEventArgs(parent, test));
        }

        public void ClearDatabase()
        {
            Database.Clear();
        }

        /// <summary>将 Database 恢复为系统默认 Job 设置（不含标试样，阶段1-T22）。</summary>
        public void ApplySystemDefaults(SystemDefaultConfig defaults)
        {
            Database.DefaultMaterial = defaults.DefaultMaterial;
            Database.StandardNamgeGenerator.Rule = defaults.StandardAutoNamingRule;
            Database.TestNameGenerator.Rule = defaults.TestAudotNamingRule;
            Database.SystemChromaticitySetting.Copy(defaults.DisplaySetting);
            Database.ObserverAndIlluminantSetting = defaults.ObserverAndIlluminantSetting;
            Database.SystemToleranceSetting.Copy(defaults.ToleranceSetting);
            Database.ReportSetting.Copy(defaults.ReportSetting);
            Database.IsModified = false;
            Database.FilePath = null;
        }

        /// <summary>新建 Job：清空标试样并套用系统默认（阶段1-T22）。</summary>
        public void ResetNewJob(SystemDefaultConfig defaults)
        {
            Database.Clear();
            ApplySystemDefaults(defaults);
        }

        /// <summary>按当前观察者/光源/测光方式取得标试样 adapter 快照，供 MainWindow 重建 TreeView（阶段1-T17）。</summary>
        public JobSpecimensForView GetAllStandardsForView(
            SpecularComponent specularComponent,
            StandardObserver observer,
            StandardIlluminant illuminant)
        {
            var standards = new List<JobStandardForView>();
            foreach (StandardSpecimenAdapter standard in Database.GetStandards(specularComponent, observer, illuminant))
            {
                IReadOnlyList<TestSpecimenAdapter> tests = Array.Empty<TestSpecimenAdapter>();
                if (standard.StandardSpecimen.TestSpecimens != null)
                {
                    tests = Database.GetTests(standard.StandardSpecimen, specularComponent, observer, illuminant);
                }

                standards.Add(new JobStandardForView
                {
                    Standard = standard,
                    Tests = tests
                });
            }

            return new JobSpecimensForView
            {
                Standards = standards,
                OrphanTests = Database.GetTests(null, specularComponent, observer, illuminant)
            };
        }

        /// <summary>按扩展名保存 Job（XML 含 ViewSetting / QTX）。</summary>
        public void SaveToFile(string fileName, JobViewSettings viewSettings, SpecularComponent qtxSpecularComponent)
        {
            switch (Path.GetExtension(fileName).ToUpperInvariant())
            {
                case ".QTX":
                    SaveAsQtx(fileName, qtxSpecularComponent);
                    break;
                default:
                    SaveAsXml(fileName, viewSettings);
                    break;
            }
        }

        public void SaveAsXml(string fileName, JobViewSettings viewSettings)
        {
            var doc = new XmlDocument();
            doc.AppendChild(doc.CreateXmlDeclaration("1.0", "utf-8", null));
            var root = Database.ToXmlElement(doc, "ThreeNHJob");
            if (viewSettings != null)
            {
                root.AppendChild(viewSettings.ToXmlElement(doc));
            }

            doc.AppendChild(root);
            doc.Save(fileName);
            Database.FilePath = fileName;
            Database.IsModified = false;
        }

        public void SaveAsQtx(string fileName, SpecularComponent specularComponent)
        {
            QTXHelper.Save(fileName, Database, specularComponent);
            // 与原 SaveAsQtx 一致：QTX 保存不更新 FilePath，仅清除修改标记
            Database.IsModified = false;
        }

        /// <summary>空闲备份：仅写标试样数据，不含 ViewSetting（与原 Database.Save 一致）。</summary>
        public void SaveBackup(string backupFilePath)
        {
            Database.Save(backupFilePath);
        }

        /// <summary>查找指定 Job 文件（或全部）的最新备份。</summary>
        public JobBackupFileInfo GetLatestBackup(string fileName = null)
        {
            var pattern = JobBackupFileInfo.GetFilePattern(fileName);
            var list = new List<JobBackupFileInfo>();
            foreach (var path in Directory.EnumerateFiles(App.BackupPath))
            {
                if (Regex.IsMatch(Path.GetFileName(path), pattern))
                {
                    var info = JobBackupFileInfo.TryParse(path);
                    if (info != null)
                    {
                        list.Add(info);
                    }
                }
            }

            return list.Count > 0 ? list.OrderByDescending(_ => _.DateTime).First() : null;
        }

        /// <summary>删除匹配的旧备份，可保留 exceptFileName 对应文件。</summary>
        public void RemoveBackupFiles(string fileName = null, string exceptFileName = null)
        {
            var pattern = JobBackupFileInfo.GetFilePattern(fileName);
            foreach (var path in Directory.EnumerateFiles(App.BackupPath))
            {
                var name = Path.GetFileName(path);
                if (Regex.IsMatch(name, pattern)
                    && (exceptFileName == null || name != Path.GetFileName(exceptFileName)))
                {
                    try
                    {
                        File.Delete(path);
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError($"Delete backup file '{path}' failed: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// 线程空闲时自动备份：有未保存修改且自上次备份以来又有新改动时才写入。
        /// </summary>
        public void BackupIfModified()
        {
            if (!Database.IsModified)
            {
                return;
            }

            string fileName = string.IsNullOrWhiteSpace(Database.FilePath)
                ? "new"
                : Path.GetFileName(Database.FilePath);

            var latestBackup = GetLatestBackup(fileName);
            // 若磁盘备份已新于内存修改时间，说明当前改动已被备份过，避免重复写盘
            if (latestBackup?.BackupPath != null
                && Database.LatestModificationTimestamp < latestBackup.DateTime)
            {
                return;
            }

            string backupFileName = Path.Combine(App.BackupPath, $"{fileName}@{DateTime.Now:yyyy-MM-dd_HHmmss}");
            try
            {
                SaveBackup(backupFileName);
            }
            catch (Exception ex)
            {
                Trace.TraceError($"Backup file failed: {ex.Message}");
                return;
            }

            RemoveBackupFiles(fileName, backupFileName);
        }

        public JobDocumentLoadResult LoadFromFile(string fileName)
        {
            var result = new JobDocumentLoadResult { FilePath = fileName };
            try
            {
                if (Path.GetExtension(fileName).ToUpperInvariant() == ".QTX")
                {
                    LoadQtx(fileName, result);
                }
                else
                {
                    LoadXml(fileName, result);
                }

                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        /// <summary>从已打开的 XmlDocument 加载（OpenFileEx 后台线程用）。</summary>
        public JobDocumentLoadResult LoadFromXmlDocument(XmlDocument doc, string fileName)
        {
            var result = new JobDocumentLoadResult { FilePath = fileName };
            try
            {
                ApplyXmlDocument(doc, result);
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        public JobDocumentLoadResult LoadQtxFromReader( string fileName)
        {
            var result = new JobDocumentLoadResult { FilePath = fileName };
            try
            {
                var reader = File.OpenText(fileName);
                QTXHelper.Load(reader, Database);
                FillLoadResultFromDatabase(result);
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        private void LoadXml(string fileName, JobDocumentLoadResult result)
        {
            var doc = new XmlDocument();
            doc.Load(fileName);
            ApplyXmlDocument(doc, result);
        }

        private void ApplyXmlDocument(XmlDocument doc, JobDocumentLoadResult result)
        {
            Database.LoadXmlElement(doc.DocumentElement);
            Database.IsModified = false;
            var viewSettingNode = doc.DocumentElement.SelectSingleNode("ViewSetting") as XmlElement;
            result.ViewSettings = JobViewSettings.TryFromXml(viewSettingNode);
            FillLoadResultFromDatabase(result);
        }

        private void LoadQtx(string fileName, JobDocumentLoadResult result)
        {
            using (var reader = File.OpenText(fileName))
            {
                QTXHelper.Load(reader, Database);
            }

            FillLoadResultFromDatabase(result);
        }

        private void FillLoadResultFromDatabase(JobDocumentLoadResult result)
        {
            var sc = Database.GetSpecularComponent();
            result.SpecularComponent = sc == SpecularComponent.SCIAndSCE ? SpecularComponent.SCI : sc;
            result.ObserverAndIlluminantSetting = Database.ObserverAndIlluminantSetting;
        }
    }
}
