using ControlzEx.Standard;
using DocumentFormat.OpenXml.Office.CustomUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ThreeNH.ColorAlgorithm;
using ThreeNH.ColorQualityControl.Adapter;
using ThreeNH.ColorQualityControl.UI;
using ThreeNH.ColorQualityControl.UI.Charts;
using ThreeNH.ColorQualityControl.ViewModels;
using ThreeNH.ColorQualityControl.ViewModels.Charts;
using ThreeNH.Controls;
using ThreeNH.DataModel;
using ThreeNH.Helper;
using Xceed.Words.NET;

namespace ThreeNH.ColorQualityControl.Reports
{
    class WordStatisticsReportItem
    {
        public Formula Formula { get; set; }
        public Tolerance Tolerance { get; set; }
        public KeyValuePair<StandardSpecimenAdapter, TestSpecimenAdapter[]> Adapters { get; set; }
    }
    class WordStatisticsReport : WordReportBase
    {
        public override void GenerateReport()
        {
            var reportFileName = Path.GetTempFileName();
            File.Delete(reportFileName);
            reportFileName = Path.ChangeExtension(reportFileName, ".docx");

            DocX doc = null;
            try
            {
                doc = DocX.Create(reportFileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, S._("Label_PrintToWord"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }


            AddReportInfo(doc);

            {
                var p = AppendParagraph(doc, S._("ColorDiffCumulative"));
                SetAsReportTitle(p);
            }

            List<WordStatisticsReportItem> items = GetWordStatisticsReportItems();

            //插入图表
            InsertFigures(doc, items);

            // 容差
            InsertTolerance(doc, items);

            // 标样
            InsertStandards(doc, items);
            // 业务要求：色差累计报告可按设置省略试样 Lab，避免报告过长
            if (ReportSetting.IsExportTestLab)
                InsertTestData(doc, items);

            // 统计结果
            Insetstatistics(doc, items);


            // 试样数据
            InsertTestColorDiffData(doc, items);

            //下一页
            //if (i != items.Count - 1)
            //    doc.InsertSectionPageBreak();

            doc.InsertParagraph();
            AppendTail(doc);

            doc.Save();

            Process.Start(reportFileName);
        }

        private void InsertFigures(DocX doc, List<WordStatisticsReportItem> items)
        {
            //目前只插入一个图片,是否需要插入参考光源对应的图表根据客户反馈再添加
            var adapter = items.FirstOrDefault().Adapters;
            var figure = MakeFigure(adapter.Key, adapter.Value.ToArray(), (int)doc.PageWidth);
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(figure));
            MemoryStream stream = new MemoryStream();
            encoder.Save(stream);
            stream.Seek(0, SeekOrigin.Begin);
            var image = doc.AddImage(stream);

            var p = doc.InsertParagraph();
            p.InsertPicture(image.CreatePicture()).Alignment = Alignment.center;
        }

        private List<WordStatisticsReportItem> GetWordStatisticsReportItems()
        {
            List<WordStatisticsReportItem> items = new List<WordStatisticsReportItem>();
            items.Add(new WordStatisticsReportItem()
            {
                Formula = Formula,
                Tolerance = ToleranceSetting.PrimaryTolerance,
                Adapters = GetAdapters(Formula)
            });
            if (ReportSetting.IsExportReferences)
            {
                //参考光源和观察者为空时不打印
                if (ReferenceFormula1 != null)
                {
                    items.Add(new WordStatisticsReportItem()
                    {
                        Formula = ReferenceFormula1,
                        Tolerance = ToleranceSetting.SecondaryTolerance,
                        Adapters = GetAdapters(ReferenceFormula1)
                    });
                }
                if (ReferenceFormula2 != null)
                {
                    items.Add(new WordStatisticsReportItem()
                    {
                        Formula = ReferenceFormula2,
                        Tolerance = ToleranceSetting.TertiaryTolerance,
                        Adapters = GetAdapters(ReferenceFormula2)
                    });
                }
            }

            return items;
        }
        //  
        /// <summary>
        /// 试样Lab值
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="items"></param>
        private void InsertTestData(DocX doc, List<WordStatisticsReportItem> items)
        {
            var fields = ChromaticitySetting.ColorFields;
            if (fields.Length > 0)
            {

                for (int i = 0; i < Tests.Length; i++)
                {
                    int columnCount = fields.Length + 1;
                    var testAdapters = Tests[i].Name;
                    var table = AppendTable(doc, 2, columnCount, S._("Test") + testAdapters);
                    SetColumnHeader(table, 1, 0, S._("Label_Name"));
                    for (int j = 0; j < fields.Length; j++)
                    {
                        var field = fields[j];
                        int col = j + 1;
                        SetColumnHeader(table, 1, col, field.Name());
                    }
                    int r = 2;
                    foreach (var item in items)
                    {
                        
                       

                        table.InsertRow();
                        SetRowHeader(table, r, 0, AddFormulaInfo(testAdapters, item.Formula));
                        for (int k = 0; k < fields.Length; k++)
                        {
                            var field = fields[k];
                            int col = k + 1;
                            //SetColumnHeader(table, 1, col, field.Name());
                            table.Cell(r, col).SetText(item.Adapters.Key.GetFormatedPropertyValue(field));
                        }
                        r++;
                    }
                }
            }
        }


        /// <summary>
        /// 式样色差
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="items"></param>
        private void InsertTestColorDiffData(DocX doc, List<WordStatisticsReportItem> items)
        {
            List<ChromaticityField> fields = new List<ChromaticityField>(ChromaticitySetting.ColorDiffFields);
            if (fields.Count > 0)
            {
                if (ChromaticitySetting.DisplayFields.Contains(ChromaticityField.ColorOffset))
                {
                    fields.Add(ChromaticityField.ColorOffset);
                }

                int columnCount = fields.Count + 2;
                int judgementColumn = -1;
                if (ChromaticitySetting.DisplayFields.Contains(ChromaticityField.Judgement))
                {
                    columnCount += 1;
                    judgementColumn = columnCount - 1;
                }

                //int rowCount = testAdapters.Length + 2;

                var table = AppendTable(doc, 2, columnCount,
                    S._("TestColorDiff"));
                SetColumnHeader(table, 1, 0, S._("Label_Index"));
                SetColumnHeader(table, 1, 1, S._("Label_Name"));
                for (int i = 0; i < fields.Count; i++)
                {
                    int col = i + 2;
                    SetColumnHeader(table, 1, col, GetFieldName(fields[i]));
                }

                if (judgementColumn >= 0)
                {
                    SetColumnHeader(table, 1, judgementColumn, S._("Label_Judgement"));
                }
                int row = 2;
                for (int i = 0; i < Tests.Length; i++)
                {
                    foreach (var item in items)
                    {
                        var testAdapters = item.Adapters.Value;
                        table.InsertRow();
                        var test = testAdapters[i];
                        int col = 0;
                        table.Cell(row, col++).SetText((i + 1).ToString());
                        table.Cell(row, col++).SetText(AddFormulaInfo(test.Name, item.Formula));
                        foreach (var field in fields)
                        {
                            table.Cell(row, col++).SetText(test.GetFormatedPropertyValue(field));
                        }
                        if (judgementColumn >= 0)
                        {
                            table.Cell(row, col).SetText(test.Judgement.TotalJudgement.Description());
                        }
                        row++;
                    }
                    var interval = items.Count;
                    if (interval > 1)
                        table.MergeCellsInColumn(0, row - interval, row - 1);
                }
            }
        }

        private KeyValuePair<StandardSpecimenAdapter, TestSpecimenAdapter[]> GetAdapters(Formula formula)
        {
            var helper = GetAdapterHelper();
            var standard = helper.CreateAdapter(Standard, formula);
            helper.Standard = standard;
            var tests = new List<TestSpecimenAdapter>();
            foreach (var test in Tests)
            {
                var testAdapter = helper.CreateAdapter(test, formula);
                tests.Add(testAdapter);
            }
            return new KeyValuePair<StandardSpecimenAdapter, TestSpecimenAdapter[]>(standard, tests.ToArray());
        }

        private void Insetstatistics(DocX doc, List<WordStatisticsReportItem> items)
        {
            var fields = ChromaticitySetting.ColorFields;
            if (fields.Length > 0)
            {
                string[] columnHeaders = new string[]
                {
                        S._("Min"), S._("Max"), S._("Range"), S._("Mean"), S._("StdDev")
                };

                int columnCount = columnHeaders.Length + 1;

                var table = AppendTable(doc, 2, columnCount, S._("StatisticsResult"));

                for (int i = 0; i < columnHeaders.Length; i++)
                {
                    int col = i + 1;
                    SetColumnHeader(table, 1, col, columnHeaders[i]);
                }

                int row = 2;
                foreach (var item in items)
                {
                    var testAdapters = item.Adapters.Value.ToArray();
                    for (int i = 0; i < fields.Length; i++)
                    {
                        table.InsertRow();
                        var field = fields[i];
                        SetRowHeader(table, row, 0, AddFormulaInfo(GetFieldName(field), item.Formula));
                        var statistics = StatisticsHelper.GetChromaticityStatistics(testAdapters, field);
                        table.Cell(row, 1).SetText(statistics.Min.ToString("F3"));
                        table.Cell(row, 2).SetText(statistics.Max.ToString("F3"));
                        table.Cell(row, 3).SetText(statistics.Range.ToString("F3"));
                        table.Cell(row, 4).SetText(statistics.Mean.ToString("F3"));
                        table.Cell(row, 5).SetText(statistics.StdDev.ToString("F3"));
                        row++;
                    }
                    if (ChromaticitySetting.DisplayFields.Contains(ChromaticityField.Judgement))
                    {
                        table.InsertRow();
                        var totalStatistics = StatisticsHelper.GetJudgementStatistics(testAdapters);
                        SetRowHeader(table, row, 0, AddFormulaInfo(S._("Judgement"), item.Formula));
                        table.Rows[row].MergeCells(1, columnCount - 1);
                        table.Cell(row, 1).SetText(string.Format(S._("Total_0_Pass_1_PrecentOfPass_2"),
                            totalStatistics.Valid, totalStatistics.Pass, totalStatistics.PercentOfPass.ToString("F2")));
                        row++;
                    }
                    //如果选中的结论项，显示合格率统计
                    //if (ChromaticitySetting.DisplayFields.Contains(ChromaticityField.Judgement))
                    //{
                    //    table.InsertRow();
                    //    row++;
                    //    var totalStatistics = StatisticsHelper.GetJudgementStatistics(testAdapters);
                    //    SetRowHeader(table, row, 0, AddFormulaInfo(S._("Judgement"), item.Formula));
                    //    table.Rows[row].MergeCells(1, columnCount - 1);
                    //    table.Cell(row, 1).SetText(string.Format(S._("Total_0_Pass_1_PrecentOfPass_2"),
                    //        totalStatistics.Valid, totalStatistics.Pass, totalStatistics.PercentOfPass.ToString("F2")));
                    //}
                    //row++;
                }
            }
        }

        private void InsertStandards(DocX doc, List<WordStatisticsReportItem> items)
        {
            var fields = ChromaticitySetting.ColorFields;
            if (fields.Length > 0)
            {
                int columnCount = fields.Length + 1;

                var table = AppendTable(doc, 2, columnCount, S._("Label_Standard"));
                SetColumnHeader(table, 1, 0, S._("Label_Name"));

                for (int i = 0; i < fields.Length; i++)
                {
                    var field = fields[i];
                    int col = i + 1;
                    SetColumnHeader(table, 1, col, field.Name());
                }
                int row = 2;
                foreach (var item in items)
                {
                    table.InsertRow();
                    SetRowHeader(table, row, 0, AddFormulaInfo(item.Adapters.Key.Name, item.Formula));
                    for (int i = 0; i < fields.Length; i++)
                    {
                        var field = fields[i];
                        int col = i + 1;
                        //SetColumnHeader(table, 1, col, field.Name());
                        table.Cell(row, col).SetText(item.Adapters.Key.GetFormatedPropertyValue(field));
                    }
                    row++;
                }
            }
        }

        private void InsertTolerance(DocX doc, List<WordStatisticsReportItem> items)
        {
            var fields = ChromaticitySetting.ColorDiffFields;
            if (!ReportSetting.IsExportTolerance || fields.Length <= 0) return;

            int columnCount = fields.Length + 1;
            var table = AppendTable(doc, 2, columnCount, S._("Tolerance"));
            //设置表头
            for (int i = 0; i < fields.Length; i++)
            {
                var field = fields[i];
                int col = i + 1;
                string header = GetFieldName(field);
                SetColumnHeader(table, 1, col, header);
            }
            int row = 2;
            foreach (var item in items)
            {
                table.InsertRow();
                var tolerance = item.Tolerance;
                SetRowHeader(table, row, 0, GetFormulaInfo(item.Formula));
                for (int i = 0; i < fields.Length; i++)
                {
                    var field = fields[i];
                    int col = i + 1;
                    //string header = GetFieldName(field);
                    //SetColumnHeader(table, 1, col, header);
                    if (tolerance != null)
                    {
                        var data = tolerance[field];
                        string text = data.UseForJudgement ? tolerance[field].ToString() : "--";
                        if (data.UseForJudgement)
                        {
                            table.Cell(row, col).SetText(data.ToString());
                        }
                        else
                        {
                            table.Cell(row, col).SetBorder(TableCellBorderType.TopRightToBottomLeft, SingleLineBorder);
                        }
                    }
                }
                row++;
            }
        }


        public TestSpecimen[] Tests { get; set; }

        private BitmapSource MakeFigure(StandardSpecimenAdapter stdAdpater,
            TestSpecimenAdapter[] testAdapters, int width)
        {
            Grid grid = new Grid();
            grid.Height = 200;

            // Word 导出用容差色平图替代旧版 DeltaPlotControl，与界面左侧容差图保持一致
            bool exportToleranceChart = VisibleDeltaPlots != null && VisibleDeltaPlots.Length > 0;
            int columnCount = (exportToleranceChart ? 1 : 0) + 1;
            if (IsExportChromaticityChart)
                columnCount++;
            for (int i = 0; i < columnCount; i++)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition());
            }
            int gridColumn = 0;
            if (exportToleranceChart)
            {
                var toleranceChart = ToleranceChartHelper.CreateChartView(
                    stdAdpater, testAdapters, ToleranceSetting?.PrimaryTolerance);
                grid.Children.Add(toleranceChart);
                Grid.SetColumn(toleranceChart, gridColumn);
                gridColumn++;
            }


            SpectrumPlotControl spectrumPlot = new SpectrumPlotControl();
            grid.Children.Add(spectrumPlot);
            Grid.SetColumn(spectrumPlot, gridColumn);

            if (stdAdpater.Spectrum != null)
                spectrumPlot.AddStandard(stdAdpater.Spectrum, stdAdpater.Name);

            foreach (var a in testAdapters)
            {
                if (a.Spectrum != null)
                    spectrumPlot.AddTest(a.Spectrum, a.Name, Brushes.Gray);
            }
            //插入CIE Yxy色品图

            if (IsExportChromaticityChart)
            {
                gridColumn++;
                TriaxialChartView tcv = new TriaxialChartView();
                grid.Children.Add(tcv);
                Grid.SetColumn(tcv, gridColumn);
                //默认打印Lab
                ColorChartViewModel model = new LabChartViewModel();
                //if (ChromaticitySetting.ColorSpace == Optical.ColorSpace.CIE_LAB
                //     || ChromaticitySetting.ColorSpace == Optical.ColorSpace.DIN99_LAB
                //     || ChromaticitySetting.ColorSpace == Optical.ColorSpace.HUNTER_LAB)
                //    model = new LabChartViewModel();
                if (ChromaticitySetting.ColorSpace == Optical.ColorSpace.CIE_XYZ
                    || ChromaticitySetting.ColorSpace == Optical.ColorSpace.CIE_Yxy)
                    model = new XyzChartViewModel();
                else if (ChromaticitySetting.ColorSpace == Optical.ColorSpace.CIE_LCH
                    || ChromaticitySetting.ColorSpace == Optical.ColorSpace.DIN99_LCh)
                    model = new LchChartViewModel();
                if (model != null)
                {
                    model.IsAutoFitAxes = false;
                    model.IsXyBackgroundVisible = true;
                    var standardData = new ChartColorData(stdAdpater.Formula.Illuminant, stdAdpater.Formula.Observer, stdAdpater.SpecularComponent, stdAdpater.Specimen);
                    var sampleDatas = new List<IColorData>();
                    foreach (var test in testAdapters)
                    {
                        var sample = new ChartColorData(test.Formula.Illuminant, test.Formula.Observer, test.SpecularComponent, test.Specimen);
                        sampleDatas.Add(sample);
                    }
                    model.SetData(standardData, sampleDatas);
                    tcv.DataContext = model;
                }
            }

            ContentToImageWindow dlg = new ContentToImageWindow(grid, width);
            dlg.ShowDialog();
            dlg.Content = null;

            return dlg.Image;
        }
    }
}