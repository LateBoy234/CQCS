using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using ThreeNH.ColorAlgorithm;
using ThreeNH.ColorQualityControl.Adapter;
using ThreeNH.ColorQualityControl.UI;
using ThreeNH.ColorQualityControl.UI.Charts;
using ThreeNH.ColorQualityControl.ViewModels;
using ThreeNH.ColorQualityControl.ViewModels.Charts;
using ThreeNH.ColorSpecimen;
using ThreeNH.Controls;
using ThreeNH.DataModel;
using ThreeNH.Helper;
using ThreeNH.Optical;

namespace ThreeNH.ColorQualityControl.Reports
{
    class ColorDiffReport : FlowDocumentReportBase
    {
        public TestSpecimen Test { get; set; }
        public bool IsExportChromaticityChart { get; set; }

        public override FlowDocument GetFlowDocument()
        {
            FlowDocument doc = new FlowDocument();
            doc.PageWidth = PageWidth;
            doc.PageHeight = PageHeight;
            doc.ColumnWidth = PageWidth;
            doc.PagePadding = new Thickness(32, 48, 32, 32);
            doc.LineHeight = 1;


            AddReportInfo(doc);

            // 标题
            Paragraph title = MakeReportTitle(S._("ColorDiffReport"));
            doc.Blocks.Add(title);

            doc.Blocks.Add(new Paragraph());
            doc.Blocks.Add(MakeInfoTable());
            doc.Blocks.Add(new Paragraph());

            // 插入图片
            InsertFigure(doc);

            var items = GetReportItems(Test);

            if ((ChromaticitySetting.ColorFields.Length + ChromaticitySetting.ColorIndexFields.Length) > 5 ||
                (ChromaticitySetting.ColorDiffFields.Length+ChromaticitySetting.ColorIndexDiffFields.Length) > 5)
            {
                InsertVerticalDataTables(doc, items);
            }
            else
            {
                InsertHorizontalDataTables(doc, items);
            }


            // 添加测试，审核以及仪器型号
            doc.Blocks.Add(new Paragraph());
            doc.Blocks.Add(MakeTail());

            return doc;
        }

        private void InsertFigure(FlowDocument doc)
        {
            SpecimenAdapterHelper adapterHelper = new SpecimenAdapterHelper();
            adapterHelper.ChromaticitySetting = ChromaticitySetting;
            adapterHelper.Formula = Formula;
            adapterHelper.Tolerance = Tolerance;
            adapterHelper.ReferenceFormula1 = ReferenceFormula1;
            adapterHelper.ReferenceFormula2 = ReferenceFormula2;

            adapterHelper.SpecularComponent = SpecularComponent;
            var stdAdapter = adapterHelper.CreateAdapter(Standard);
            adapterHelper.Standard = stdAdapter;
            var testAdapter = adapterHelper.CreateAdapter(Test);

            doc.Blocks.Add(MakeFigureBlock(stdAdapter, testAdapter));
            doc.Blocks.Add(new Paragraph());
        }

        private void InsertHorizontalDataTables(FlowDocument doc, List<Item> items)
        {
            // 容差
            if (ReportSetting.IsExportTolerance && ChromaticitySetting.DisplayFields.Any(_ => _.IsColorDiffField()))
            {
                // 插入标题
                var p = doc.InsertParagraph(S._("Tolerance"));

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
                        .Align(TextAlignment.Center);
                    int colIndex = 1;
                    foreach (var field in keys)
                    {
                        if (rowIndex == 1)
                        {
                            table.Cell(0, colIndex).SetText(field.Description());
                        }

                        if (tuple.Tolerance[field].UseForJudgement)
                        {
                            table.Cell(rowIndex, colIndex).SetText(tuple.Tolerance[field].ToString()).Align(TextAlignment.Center);
                        }
                        else
                        {
                            table.Cell(rowIndex, colIndex).SetText("--").Align(TextAlignment.Center);
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
                        .Align(TextAlignment.Center);

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

                var p = doc.InsertParagraph(S._("ChromaticityIndexes"));
                doc.InsertTable(table);
                doc.InsertParagraph();
            }

            // 色差数据和指数差数据
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
                        .Align(TextAlignment.Center);

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

                var p = doc.InsertParagraph(S._("ColorDiff"));
                doc.InsertTable(table);
                doc.InsertParagraph();
            }
        }


        private void InsertVerticalDataTables(FlowDocument doc, List<Item> items)
        {

            // 容差
            if (ReportSetting.IsExportTolerance && ChromaticitySetting.DisplayFields.Any(_ => _.IsColorDiffField()))
            {
                // 插入标题
                doc.Blocks.Add(new Paragraph(new Run(S._("Tolerance"))));

                var keys = (from key in ToleranceSetting.ItemKeys
                            where items.Any(_ => _.Tolerance[key].UseForJudgement)
                            select key).ToArray();


                var table = doc.AddTable(keys.Length + 1, items.Count + 1); //doc.AddTable(keys.Length + 1, items.Count + 1);
                int col = 1;
                foreach (var tuple in items)
                {
                    var formula = tuple.Formula;
                    table.Cell(0, col).SetText(string.Format("{0}/{1}",
                            formula.Illuminant, formula.Observer.Description()))
                        .Align(TextAlignment.Center);
                    int row = 1;
                    foreach (var field in keys)
                    {
                        if (col == 1)
                        {
                            table.Cell(row, 0).SetText(field.Description());
                        }

                        if (tuple.Tolerance[field].UseForJudgement)
                        {
                            table.Cell(row, col).SetText(tuple.Tolerance[field].ToString()).Align(TextAlignment.Center);
                        }
                        else
                        {
                            table.Cell(row, col).SetText("--").Align(TextAlignment.Center);
                        }

                        row += 1;
                    }

                    col += 1;
                }

                doc.Blocks.Add(table);
                doc.Blocks.Add(new Paragraph());
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
                    table.Row(0).MergeCells(i + 1, i + 2);
                    table.Cell(0, col).SetText(string.Format("{0}/{1}",
                            formula.Illuminant, formula.Observer.Description()))
                        .Align(TextAlignment.Center);

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

                doc.InsertParagraph(S._("ChromaticityIndexes"))
                    .InsertTable(table)
                    .InsertParagraph();
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
                        .Align(TextAlignment.Center);

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

                doc.InsertParagraph(S._("ColorDiff"));
                doc.InsertTable(table);
                doc.InsertParagraph();
            }
        }


        private Table MakeInfoTable()
        {

            // 样品信息表
            Table table = new Table();
            table.Style = TableStyle;

            table.Columns.Add(new TableColumn());
            table.Columns.Add(new TableColumn());
            table.Columns.Add(new TableColumn());

            TableRowGroup rowGroup = new TableRowGroup();
            table.RowGroups.Add(rowGroup);

            TableRow row = NewRow(rowGroup);
            row.Cells.Add(NewTableHeader(S._("SampleInfo"), 3));

            row = NewRow(rowGroup);
            row.Cells.Add(NewCell());
            row.Cells.Add(NewCell(S._("Label_Standard")));
            row.Cells.Add(NewCell(S._("Label_TestSample")));

            row = NewRow(rowGroup);
            row.Cells.Add(NewCell(S._("Label_Name")));
            row.Cells.Add(NewCell(Standard.Name));
            row.Cells.Add(NewCell(Test.Name));

            row = NewRow(rowGroup);
            row.Cells.Add(NewCell(S._("Material")));
            row.Cells.Add(NewCell(Standard.Material));
            row.Cells.Add(NewCell(Test.Material));

            row = NewRow(rowGroup);
            row.Cells.Add(NewCell(S._("MeasurementCondition")));
            row.Cells.Add(NewCell(GetMeasurementCondition(Standard)));
            row.Cells.Add(NewCell(GetMeasurementCondition(Test)));

            row = NewRow(rowGroup);
            row.Cells.Add(NewCell(S._("MeasurementDateTime")));
            row.Cells.Add(NewCell(Standard.DateTime.ToString("G")));
            row.Cells.Add(NewCell(Test.DateTime.ToString("G")));

            return table;
        }

        private BlockUIContainer MakeFigureBlock(StandardSpecimenAdapter stdAdapter, TestSpecimenAdapter testAdapter)
        {
            Grid grid = new Grid();

            int columnCount = ((VisibleDeltaPlots != null) ? VisibleDeltaPlots.Length : 0) + 1;
            if (IsExportChromaticityChart)
                columnCount++;
            for (int i = 0; i < columnCount; i++)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition());
            }
            int gridColumn = 0;
            if (VisibleDeltaPlots != null)
            {
                foreach (DeltaPlotType type in VisibleDeltaPlots)
                {
                    DeltaPlotControl deltaPlot = DeltaPlotAgentControl.CreateDeltaPlotControl(type);
                    deltaPlot.Add(DeltaPlotAgentControl.GetPointConverter(type)((testAdapter)));
                    grid.Children.Add(deltaPlot);
                    Grid.SetColumn(deltaPlot, gridColumn);
                    gridColumn += 1;
                }
            }

            SpectrumPlotControl spectrumPlot = new SpectrumPlotControl();
            if (stdAdapter.Spectrum != null)
            {
                spectrumPlot.AddStandard(stdAdapter.Spectrum, stdAdapter.Name, Brushes.DarkRed);
            }
            if (testAdapter.Spectrum != null)
            {
                spectrumPlot.AddTest(testAdapter.Spectrum, testAdapter.Name, Brushes.Blue);
            }
            grid.Children.Add(spectrumPlot);
            Grid.SetColumn(spectrumPlot, gridColumn);

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
            ContentToImageWindow dlg = new ContentToImageWindow(grid, (int)PageWidth);
            dlg.ShowDialog();
            dlg.Content = null;

            return new BlockUIContainer(new Image { Source = dlg.Image });
        }
    }
}
