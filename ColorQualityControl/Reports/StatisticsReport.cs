using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using ThreeNH.ColorQualityControl.Adapter;
using ThreeNH.ColorQualityControl.UI;
using ThreeNH.ColorQualityControl.UI.Charts;
using ThreeNH.ColorQualityControl.ViewModels;
using ThreeNH.ColorQualityControl.ViewModels.Charts;
using ThreeNH.Controls;
using ThreeNH.DataModel;
using ThreeNH.Helper;

namespace ThreeNH.ColorQualityControl.Reports
{
    class StatisticsReport : FlowDocumentReportBase
    {
        public TestSpecimen[] Tests { get; set; }

        public bool IsExportChromaticityChart { get; set; }

        public override FlowDocument GetFlowDocument()
        {
            FlowDocument doc = new FlowDocument();
            doc.FontSize = 9;
            doc.PageWidth = PageWidth;
            doc.PageHeight = PageHeight;
            doc.ColumnWidth = PageWidth;
            doc.PagePadding = new Thickness(60, 72, 60, 60);
            doc.LineHeight = 1;

            AddReportInfo(doc);

            doc.Blocks.Add(MakeReportTitle(S._("ColorDiffCumulative")));
            doc.Blocks.Add(new Paragraph());


            var adapterHelper = GetAdapterHelper();
            var stdAdapter = adapterHelper.CreateAdapter(Standard);
            adapterHelper.Standard = stdAdapter;
            TestSpecimenAdapter[] testAdapters = new TestSpecimenAdapter[Tests.Length];
            for (int i = 0; i < Tests.Length; i++)
            {
                testAdapters[i] = adapterHelper.CreateAdapter(Tests[i]);
            }

            doc.Blocks.Add(MakeFigure(stdAdapter, testAdapters));
            doc.Blocks.Add(new Paragraph());
            doc.Blocks.Add(MakeToleranceBlock());
            doc.Blocks.Add(new Paragraph());
            doc.Blocks.Add(MakeStandardBlock(stdAdapter));
            doc.Blocks.Add(new Paragraph());
            doc.Blocks.Add(MakeStatisticsBlock(stdAdapter, testAdapters));
            doc.Blocks.Add(new Paragraph());
            doc.Blocks.Add(MakeTestDataBlock(testAdapters));
            doc.Blocks.Add(new Paragraph());
            doc.Blocks.Add(MakeTail());

            return doc;
        }

        private Block MakeToleranceBlock()
        {
            var fields = ChromaticitySetting.ColorDiffFields;
            int columnCount = fields.Length + 1;

            var table = NewTable(S._("Tolerance"), columnCount);
            var rowGroup = table.RowGroups[0];

            // 列表签
            var headerRow = NewRow(rowGroup);
            var contentRow = NewRow(rowGroup);

            headerRow.Cells.Add(NewCell());
            contentRow.Cells.Add(NewCell(GetFormulaInfo(Formula)));

            foreach (var field in fields)
            {
                string header = GetFieldName(field);
                var item = Tolerance[field];
                string text = item.UseForJudgement ? item.ToString() : "--";

                headerRow.Cells.Add(NewCell(header));
                contentRow.Cells.Add(NewCell(text));
            }

            return table;
        }

        private Block MakeStandardBlock(StandardSpecimenAdapter stdAdapter)
        {

            var fields = ChromaticitySetting.ColorFields;
            int columnCount = fields.Length + 1;

            var table = NewTable(S._("Label_Standard"), columnCount);
            var rowGroup = table.RowGroups[0];

            // 列表签
            var headerRow = NewRow(rowGroup);
            var contentRow = NewRow(rowGroup);

            headerRow.Cells.Add(NewCell(S._("Label_Name")));
            contentRow.Cells.Add(NewCell(AddFormulaInfo(stdAdapter.Name, Formula)));

            foreach (var field in fields)
            {
                headerRow.Cells.Add(NewCell(GetFieldName(field)));
                contentRow.Cells.Add(NewCell(stdAdapter.GetFormatedPropertyValue(field)));
            }

            return table;
        }

        private Block MakeStatisticsBlock(StandardSpecimenAdapter stdAdapter, TestSpecimenAdapter[] testAdapters)
        {
            var fields = ChromaticitySetting.ColorFields;
            int rowCount = fields.Length + 3;

            string[] columnHeaders = new string[]
            {
                    S._("Min"), S._("Max"), S._("Range"), S._("Mean"), S._("StdDev")
            };

            int columnCount = columnHeaders.Length + 1;

            var table = NewTable(S._("StatisticsResult"), columnCount);
            var rowGroup = table.RowGroups[0];

            var headerRow = NewRow(rowGroup);
            headerRow.Cells.Add(NewCell());
            foreach (var header in columnHeaders)
            {
                headerRow.Cells.Add(NewCell(header));
            }

            foreach (var field in fields)
            {
                var row = NewRow(rowGroup);
                row.Cells.Add(NewCell(GetFieldName(field)));

                var statistics = StatisticsHelper.GetChromaticityStatistics(testAdapters, field);
                row.Cells.Add(NewCell(statistics.Min.ToString("F3")));
                row.Cells.Add(NewCell(statistics.Max.ToString("F3")));
                row.Cells.Add(NewCell(statistics.Range.ToString("F3")));
                row.Cells.Add(NewCell(statistics.Mean.ToString("F3")));
                row.Cells.Add(NewCell(statistics.StdDev.ToString("F3")));
            }

            var totalStatistics = StatisticsHelper.GetJudgementStatistics(testAdapters);

            var totalRow = NewRow(rowGroup);
            totalRow.Cells.Add(NewCell(S._("Judgement")));
            totalRow.Cells.Add(NewCell(string.Format(S._("Total_0_Pass_1_PrecentOfPass_2"),
                totalStatistics.Valid, totalStatistics.Pass, totalStatistics.PercentOfPass)));
            totalRow.Cells[1].ColumnSpan = columnCount - 1;

            return table;
        }

        private Block MakeTestDataBlock(TestSpecimenAdapter[] testAdapters)
        {
            List<ChromaticityField> fields = new List<ChromaticityField>(ChromaticitySetting.ColorDiffFields);
            if (ChromaticitySetting.DisplayFields.Contains(ChromaticityField.ColorOffset))
            {
                fields.Add(ChromaticityField.ColorOffset);
            }
            int columnCount = fields.Count + 3;

            var table = NewTable(S._("TestColorDiff"), columnCount);
            var rowGroup = table.RowGroups[0];
            var headerRow = NewRow(rowGroup);
            headerRow.Cells.Add(NewCell(S._("Label_Index")));
            headerRow.Cells.Add(NewCell(S._("Label_Name")));
            foreach (var field in fields)
            {
                headerRow.Cells.Add(NewCell(GetFieldName(field)));
            }
            headerRow.Cells.Add(NewCell(S._("Label_Judgement")));

            int index = 1;
            foreach (var test in testAdapters)
            {
                var dataRow = NewRow(rowGroup);
                dataRow.Cells.Add(NewCell(index.ToString()));
                dataRow.Cells.Add(NewCell(test.Name));
                foreach (var field in fields)
                {
                    dataRow.Cells.Add(NewCell(test.GetFormatedPropertyValue(field)));
                }
                dataRow.Cells.Add(NewCell(test.Judgement.TotalJudgement.Description()));

                index += 1;
            }

            return table;
        }

        private Block MakeFigure(StandardSpecimenAdapter stdAdpater, TestSpecimenAdapter[] testAdapters)
        {
            Grid grid = new Grid();
            grid.Height = 200;
            int columnCount = VisibleDeltaPlots.Length + 1;
            //显示CIE Yxy色品图要添加一列
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
                    foreach (var testAdapter in testAdapters)
                    {
                        deltaPlot.Add(DeltaPlotAgentControl.GetPointConverter(type)((testAdapter)));
                    }

                    grid.Children.Add(deltaPlot);
                    Grid.SetColumn(deltaPlot, gridColumn);
                    gridColumn += 1;
                }
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

            ContentToImageWindow dlg = new ContentToImageWindow(grid, (int)PageWidth);
            dlg.ShowDialog();
            dlg.Content = null;

            return new BlockUIContainer(new Image { Source = dlg.Image });
        }
    }
}
