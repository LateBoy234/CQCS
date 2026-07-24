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
using Border = Xceed.Words.NET.Border;
using VerticalAlignment = Xceed.Words.NET.VerticalAlignment;

namespace ThreeNH.ColorQualityControl.Reports
{
    class ColorDiffWordReport : WordReportBase
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
                var p = AppendParagraph(doc, S._("ColorDiffReport"));
                SetAsReportTitle(p);
            }


            // 样品信息
            InsertInfoTable(doc);

            // 插入图片
            InsertFigure(doc);


            var items = GetReportItems(Test);

            if ((ChromaticitySetting.ColorFields.Length + ChromaticitySetting.ColorIndexFields.Length) > 5 ||
                ChromaticitySetting.ColorDiffFields.Length > 5)
            {
                InsertVerticalDataTables(doc, items);
            }
            else
            {
                InsertHorizontalDataTables(doc, items);
            }

            AppendTail(doc);


            doc.Save();

            Process.Start(reportFileName);
        }

        private void InsertHorizontalDataTables(DocX doc, List<Item> items)
        {
            // 容差
            if (ReportSetting.IsExportTolerance && ChromaticitySetting.DisplayFields.Any(_ => _.IsColorDiffField()))
            {
                // 插入标题
                var p = doc.InsertParagraph();
                p.InsertText(S._("Tolerance"));

                var keys = (from key in ToleranceSetting.ItemKeys
                            where items.Any(_ => _.Tolerance[key].UseForJudgement)
                            select key).ToArray();

                var table = doc.AddTable(items.Count + 1, keys.Length + 1);
                int rowIndex = 1;
                foreach (var tuple in items)
                {
                    var formula = tuple.Formula;
                    table.Cell(rowIndex, 0).SetText(string.Format("{0}/{1}",
                            formula.Illuminant, formula.Observer.Description()))
                        .Align(Alignment.center);
                    int colIndex = 1;
                    foreach (var field in keys)
                    {
                        if (rowIndex == 1)
                        {
                            table.Cell(0, colIndex).SetText(field.Description());
                        }

                        if (tuple.Tolerance[field].UseForJudgement)
                        {
                            table.Cell(rowIndex, colIndex).SetText(tuple.Tolerance[field].ToString()).Align(Alignment.center);
                        }
                        else
                        {
                            table.Cell(rowIndex, colIndex).SetText("--").Align(Alignment.center);
                        }

                        colIndex += 1;
                    }

                    rowIndex += 1;
                }

                doc.InsertTable(table);
                doc.InsertParagraph();
            }

            // 颜色数据和指数

            var colorFields = ChromaticitySetting.ColorFields;
            var indexFields = ChromaticitySetting.ColorIndexFields;
            ChromaticityField[] fields;
            if (colorFields != null && indexFields != null)
                fields = colorFields.Concat(indexFields).ToArray();
            else if (colorFields != null)
                fields = colorFields;
            else
                fields = indexFields;
            if (fields != null && fields.Length > 0)
            {
                var table = doc.AddTable(items.Count * 2 + 1, fields.Length + 2);
                for (int i = 0; i < items.Count; i++)
                {
                    var formula = items[i].Formula;
                    int rowIndex = i * 2 + 1;
                    table.MergeCellsInColumn(0, rowIndex, rowIndex + 1);
                    table.Cell(rowIndex, 0).SetText(string.Format("{0}/{1}",
                            formula.Illuminant, formula.Observer.Description()))
                        .Align(Alignment.center).VerticalAlignment = VerticalAlignment.Center;

                    table.Cell(rowIndex, 1).SetText(S._("Label_Standard"));
                    table.Cell(rowIndex + 1, 1).SetText(S._("Label_TestSample"));

                    int colIndex = 2;


                    foreach (var field in fields)
                    {
                        if (rowIndex == 1)
                        {
                            if (field == ChromaticityField.MI1 && ReferenceFormula1 != null)
                            {
                                table.Cell(0, colIndex).SetText(SpecimenAdapter.MakeMIName(ReferenceFormula1));
                            }
                            else if (field == ChromaticityField.MI2 && ReferenceFormula2 != null)
                            {
                                table.Cell(0, colIndex).SetText(SpecimenAdapter.MakeMIName(ReferenceFormula2));
                            }
                            else
                            {
                                table.Cell(0, colIndex).SetText(field.Description());
                            }
                        }

                        // 标样数据
                        table.Cell(rowIndex, colIndex).SetText(items[i].StdAdapter.GetFormatedPropertyValue(field));
                        table.Cell(rowIndex + 1, colIndex).SetText(items[i].TestAdapter.GetFormatedPropertyValue(field));

                        colIndex += 1;
                    }
                }

                var p = doc.InsertParagraph();
                p.InsertText(S._("ChromaticityIndexes"));
                doc.InsertTable(table);
                doc.InsertParagraph();
            }

            // 色差数据

            fields = ChromaticitySetting.DiffFields;
            if (fields != null && fields.Length > 0)
            {
                int columnCount = fields.Length + 1;
                columnCount += 1; // 结论
                if (ChromaticitySetting.DisplayFields.Contains(ChromaticityField.ColorOffset))
                    columnCount += 1;
                var table = doc.AddTable(items.Count + 1, columnCount);
                for (int i = 0; i < items.Count; i++)
                {
                    var formula = items[i].Formula;
                    int rowIndex = i + 1;
                    table.Cell(rowIndex, 0).SetText(string.Format("{0}/{1}",
                            formula.Illuminant, formula.Observer.Description()))
                        .Align(Alignment.center);

                    int colIndex = 1;


                    foreach (var field in fields)
                    {
                        if (rowIndex == 1)
                        {
                            table.Cell(0, colIndex).SetText(field.Description());
                        }

                        // 标样数据
                        table.Cell(rowIndex, colIndex).SetText(items[i].TestAdapter.GetFormatedPropertyValue(field));

                        colIndex += 1;
                    }

                    if (ChromaticitySetting.DisplayFields.Contains(ChromaticityField.ColorOffset))
                    {
                        table.Cell(0, colIndex).SetText(ChromaticityField.ColorOffset.Description());
                        table.Cell(rowIndex, colIndex)
                            .SetText(items[i].TestAdapter.GetFormatedPropertyValue(ChromaticityField.ColorOffset));
                        colIndex += 1;
                    }

                    //  显示结论
                    table.Cell(0, colIndex).SetText(ChromaticityField.Judgement.Description());
                    table.Cell(rowIndex, colIndex).SetText(items[i].TestAdapter.Judgement.TotalJudgement.Description());
                }

                var p = doc.InsertParagraph();
                p.InsertText(S._("ColorDiff"));
                doc.InsertTable(table);
                doc.InsertParagraph();
            }
        }

        private void InsertVerticalDataTables(DocX doc, List<Item> items)
        {
            // 容差
            if (ReportSetting.IsExportTolerance && ChromaticitySetting.DisplayFields.Any(_ => _.IsColorDiffField()))
            {
                // 插入标题
                var p = doc.InsertParagraph();
                p.InsertText(S._("Tolerance"));

                var keys = (from key in ToleranceSetting.ItemKeys
                            where items.Any(_ => _.Tolerance[key].UseForJudgement)
                            select key).ToArray();

                var table = doc.AddTable(keys.Length + 1, items.Count + 1);
                int col = 1;
                foreach (var tuple in items)
                {
                    var formula = tuple.Formula;
                    table.Cell(0, col).SetText(string.Format("{0}/{1}",
                            formula.Illuminant, formula.Observer.Description()))
                        .Align(Alignment.center);
                    int row = 1;
                    foreach (var field in keys)
                    {
                        if (col == 1)
                        {
                            table.Cell(row, 0).SetText(field.Description());
                        }

                        if (tuple.Tolerance[field].UseForJudgement)
                        {
                            table.Cell(row, col).SetText(tuple.Tolerance[field].ToString()).Align(Alignment.center);
                        }
                        else
                        {
                            table.Cell(row, col).SetText("--").Align(Alignment.center);
                        }

                        row += 1;
                    }

                    col += 1;
                }

                doc.InsertTable(table);
                doc.InsertParagraph();
            }

            // 颜色数据和指数

            var colorFields = ChromaticitySetting.ColorFields;
            var indexFields = ChromaticitySetting.ColorIndexFields;
            ChromaticityField[] fields;
            if (colorFields != null && indexFields != null)
                fields = colorFields.Concat(indexFields).ToArray();
            else if (colorFields != null)
                fields = colorFields;
            else
                fields = indexFields;
            if (fields != null && fields.Length > 0)
            {
                var table = doc.AddTable(fields.Length + 2, items.Count * 2 + 1);
                for (int i = 0; i < items.Count; i++)
                {
                    var formula = items[i].Formula;
                    int col = i * 2 + 1;
                    table.Rows[0].MergeCells(i + 1, i + 2);
                    table.Cell(0, i + 1).SetText(string.Format("{0}/{1}",
                            formula.Illuminant, formula.Observer.Description()))
                        .Align(Alignment.center);

                    table.Cell(1, col).SetText(S._("Label_Standard"));
                    table.Cell(1, col + 1).SetText(S._("Label_TestSample"));

                    int row = 2;


                    foreach (var field in fields)
                    {
                        if (col == 1)
                        {
                            if (field == ChromaticityField.MI1 && ReferenceFormula1 != null)
                            {
                                table.Cell(row, 0).SetText(SpecimenAdapter.MakeMIName(ReferenceFormula1));
                            }
                            else if (field == ChromaticityField.MI2 && ReferenceFormula2 != null)
                            {
                                table.Cell(row, 0).SetText(SpecimenAdapter.MakeMIName(ReferenceFormula2));
                            }
                            else
                            {
                                table.Cell(row, 0).SetText(field.Description());
                            }
                        }

                        // 标样数据
                        table.Cell(row, col).SetText(items[i].StdAdapter.GetFormatedPropertyValue(field));
                        table.Cell(row, col + 1).SetText(items[i].TestAdapter.GetFormatedPropertyValue(field));

                        row += 1;
                    }
                }

                var p = doc.InsertParagraph();
                p.InsertText(S._("ChromaticityIndexes"));
                doc.InsertTable(table);
                doc.InsertParagraph();
            }

            // 色差数据
            fields = ChromaticitySetting.ColorDiffFields;
            if (fields != null && fields.Length > 0)
            {
                int rowCount = fields.Length + 1;
                rowCount += 1; // 结论
                if (ChromaticitySetting.DisplayFields.Contains(ChromaticityField.ColorOffset))
                    rowCount += 1;
                var table = doc.AddTable(rowCount, items.Count + 1);
                for (int i = 0; i < items.Count; i++)
                {
                    var formula = items[i].Formula;
                    int col = i + 1;
                    table.Cell(0, col).SetText(string.Format("{0}/{1}",
                            formula.Illuminant, formula.Observer.Description()))
                        .Align(Alignment.center);

                    int row = 1;


                    foreach (var field in fields)
                    {
                        if (col == 1)
                        {
                            table.Cell(row, 0).SetText(field.Description());
                        }

                        // 标样数据
                        table.Cell(row, col).SetText(items[i].TestAdapter.GetFormatedPropertyValue(field));

                        row += 1;
                    }

                    if (ChromaticitySetting.DisplayFields.Contains(ChromaticityField.ColorOffset))
                    {
                        table.Cell(row, 0).SetText(ChromaticityField.ColorOffset.Description());
                        table.Cell(row, col)
                            .SetText(items[i].TestAdapter.GetFormatedPropertyValue(ChromaticityField.ColorOffset));
                        row += 1;
                    }

                    //  显示结论
                    table.Cell(row, 0).SetText(ChromaticityField.Judgement.Description());
                    table.Cell(row, col).SetText(items[i].TestAdapter.Judgement.TotalJudgement.Description());
                }

                var p = doc.InsertParagraph();
                p.InsertText(S._("ColorDiff"));
                doc.InsertTable(table);
                doc.InsertParagraph();
            }
        }

        private void InsertFigure(DocX doc)
        {
            var stdAdapter = new StandardSpecimenAdapter(
                Standard, Formula, ChromaticitySetting, SpecularComponent, Tolerance);
            var testAdapter = new TestSpecimenAdapter(
                Test, Formula, ChromaticitySetting, SpecularComponent,
                stdAdapter, ReferenceFormula1, ReferenceFormula2);
            if (Tolerance != null)
                testAdapter.Judge(stdAdapter, new JudgementSetting());

            var figure = MakeFigure(stdAdapter, testAdapter, (int)doc.PageWidth);
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(figure));
            MemoryStream stream = new MemoryStream();
            encoder.Save(stream);
            stream.Seek(0, SeekOrigin.Begin);
            var image = doc.AddImage(stream);

            var p = doc.InsertParagraph();
            p.InsertPicture(image.CreatePicture()).Alignment = Alignment.center;
        }

        private void InsertInfoTable(DocX doc)
        {
            Table table = doc.AddTable(5, 3);
            table.AutoFit = AutoFit.Contents;

            // 标签
            table.Cell(0, 1).SetText(S._("Label_Standard"));
            table.Cell(0, 2).SetText(S._("Label_TestSample"));
            table.Cell(1, 0).SetText(S._("Label_Name"));
            table.Cell(2, 0).SetText(S._("Material"));
            table.Cell(3, 0).SetText(S._("MeasurementCondition"));
            table.Cell(4, 0).SetText(S._("MeasurementDateTime"));


            table.Cell(1, 1).SetText(Standard.Name);
            table.Cell(2, 1).SetText(Standard.Material);
            table.Cell(3, 1).SetText(GetMeasurementCondition(Standard));
            table.Cell(4, 1).SetText(Standard.DateTime.ToString());
            table.Cell(1, 2).SetText(Test.Name);
            table.Cell(2, 2).SetText(Test.Material);
            table.Cell(3, 2).SetText(GetMeasurementCondition(Test));
            table.Cell(4, 2).SetText(Test.DateTime.ToString());

            doc.InsertTable(table);

            doc.InsertParagraph(); // 插入空行
        }

        BitmapSource MakeFigure(StandardSpecimenAdapter stdAdapter, TestSpecimenAdapter testAdapter, int width)
        {
            Grid grid = new Grid();
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
                    stdAdapter, new[] { testAdapter }, Tolerance);
                grid.Children.Add(toleranceChart);
                Grid.SetColumn(toleranceChart, gridColumn);
                gridColumn++;
            }

            SpectrumPlotControl spectrumPlot = new SpectrumPlotControl();
            if (stdAdapter.Spectrum != null)
            {
                spectrumPlot.WavelengthRange = stdAdapter.Spectrum.Range;
                spectrumPlot.AddStandard(stdAdapter.Spectrum, stdAdapter.Name, Brushes.DarkRed);
            }

            if (testAdapter.Spectrum != null)
            {
                if (stdAdapter.Spectrum == null)
                {
                    spectrumPlot.WavelengthRange = testAdapter.Spectrum.Range;
                }

                spectrumPlot.AddTest(testAdapter.Spectrum, testAdapter.Name, Brushes.Blue);
            }

            if (stdAdapter.TransmissionFlag == testAdapter.TransmissionFlag)
            {
                spectrumPlot.YAxisTitle = stdAdapter.TransmissionFlag;
            }
            else
            {
                spectrumPlot.YAxisTitle = "R%/T%";
            }

            grid.Children.Add(spectrumPlot);
            Grid.SetColumn(spectrumPlot, gridColumn);
            gridColumn += 1;

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
                    var standardData = new ChartColorData(stdAdapter.Formula.Illuminant, stdAdapter.Formula.Observer, stdAdapter.SpecularComponent, stdAdapter.Specimen);
                    var sampleDatas = new List<IColorData>();
                    var sample = new ChartColorData(testAdapter.Formula.Illuminant, testAdapter.Formula.Observer, testAdapter.SpecularComponent, testAdapter.Specimen);
                    sampleDatas.Add(sample);
                    model.SetData(standardData, sampleDatas);
                    tcv.DataContext = model;
                }
            }

            grid.MinHeight = 200;

            ContentToImageWindow dlg = new ContentToImageWindow(grid, width);
            dlg.ShowDialog();
            dlg.Content = null;
            return dlg.Image;
        }
    }
}