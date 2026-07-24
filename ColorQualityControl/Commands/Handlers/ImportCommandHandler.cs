using System;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using ThreeNH.ColorQualityControl.Refactoring.Contracts;
using ThreeNH.ColorQualityControl.Specimens.Import;
using ThreeNH.ColorQualityControl.UI;

namespace ThreeNH.ColorQualityControl.CommandHandlers
{
    /// <summary>
    /// Excel/文件导入 RoutedCommand 编排；解析在 SpecimenImportStrategy（阶段4-T01）。
    /// </summary>
    internal sealed class ImportCommandHandler
    {
        private readonly IImportCommandHost _host;

        public ImportCommandHandler(IImportCommandHost host)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
        }

        public void ExecuteImportFromExcel(object sender, ExecutedRoutedEventArgs e)
        {
            var fileDlg = new OpenFileDialog
            {
                Title = S._("MenuItem_ImportFromExcel"),
                Filter = S._("ExcelFileFilter"),
                CheckFileExists = true
            };
            if (!fileDlg.ShowDialog().GetValueOrDefault())
            {
                return;
            }

            var dlg = new ImportStandardsDialog(fileDlg.FileName)
            {
                Owner = _host.DialogOwner
            };
            if (!dlg.ShowDialog().GetValueOrDefault())
            {
                return;
            }

            // Excel 解析在对话框内完成；策略只统一打包 createNewId/removeSame 默认值
            var result = SpecimenImportStrategyRegistry
                .Resolve(SpecimenImportSourceKind.ExcelWorkbook)
                .Load(new SpecimenImportRequest
                {
                    PreParsedStandards = dlg.StandardSpecimens,
                    RemoveSame = false
                });
            _host.ApplySpecimenImportResult(result);
        }

        public void ExecuteImportFromFile(object sender, ExecutedRoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = S._("MenuItem_ImportFromFile"),
                Filter = S._("ImportStandardFileFilter"),
                Multiselect = true
            };
            if (!dialog.ShowDialog().GetValueOrDefault())
            {
                return;
            }

            Mouse.OverrideCursor = Cursors.Wait;
            foreach (var fileName in dialog.FileNames)
            {
                var strategy = SpecimenImportStrategyRegistry.ResolveForFile(fileName);
                if (strategy == null)
                {
                    MessageBox.Show(S._("FileTypeNotSupported"), dialog.Title, MessageBoxButton.OK);
                    Mouse.OverrideCursor = Cursors.Arrow;
                    return;
                }

                try
                {
                    var result = strategy.Load(new SpecimenImportRequest { FilePath = fileName });
                    _host.ApplySpecimenImportResult(result);
                }
                catch (Exception exception)
                {
                    MessageBox.Show($"{S._("LoadFileFailed")}: {exception.Message}", dialog.Title, MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    Mouse.OverrideCursor = Cursors.Arrow;
                    return;
                }
            }

            Mouse.OverrideCursor = Cursors.Arrow;
        }
    }
}
