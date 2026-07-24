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
using ThreeNH.ColorQualityControl.Adapter;
using ThreeNH.ColorQualityControl.UI;
using ThreeNH.ColorQualityControl.UI.Charts;
using ThreeNH.ColorQualityControl.ViewModels;
using ThreeNH.ColorQualityControl.ViewModels.Charts;
using ThreeNH.Controls;
using ThreeNH.DataModel;
using ThreeNH.Helper;
using Xceed.Words.NET;
using ThreeNH.ColorAlgorithm;
using ThreeNH.ColorSpecimen;
using System.Windows.Forms;
using MessageBox = System.Windows.MessageBox;

namespace ThreeNH.ColorQualityControl.Reports
{
    class WordSampleReport : WordReportBase
    {
        private Specimen _specimen;
        public WordSampleReport(Specimen specimen)
        {
            _specimen = specimen;
        }
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

            var p = AppendParagraph(doc, S._("SampleReport"));
            SetAsReportTitle(p);

            InsertInfoTable(doc);

            //插入图表
            InsertFigures(doc);

            //// 容差
            //InsertTolerance(doc, items);

            //标样
            InsertDatas(doc);

            //// 统计结果
            //Insetstatistics(doc, items);


            //// 试样数据
            //InsertTestData(doc, items);

            ////下一页
            ////if (i != items.Count - 1)
            ////    doc.InsertSectionPageBreak();

            doc.InsertParagraph();
            AppendTail(doc);

            doc.Save();

            Process.Start(reportFileName);
        }

        private void InsertInfoTable(DocX doc)
        {
            Table table = doc.AddTable(4, 2);
            table.SetColumnWidth(0, 3000);
            table.SetColumnWidth(1, 6000);
            table.Cell(0, 0).SetText(S._("Label_Name"));
            table.Cell(1, 0).SetText(S._("Material"));
            table.Cell(2, 0).SetText(S._("MeasurementCondition"));
            table.Cell(3, 0).SetText(S._("MeasurementDateTime"));
            table.Cell(0, 1).SetText(_specimen.Name);
            table.Cell(1, 1).SetText(_specimen.Material);
            table.Cell(2, 1).SetText(GetMeasurementCondition(_specimen));
            table.Cell(3, 1).SetText(_specimen.DateTime.ToString());

            doc.InsertTable(table);
            doc.InsertParagraph(); // 插入空行
        }

        private void InsertFigures(DocX doc)
        {
            var figure = MakeFigure(new SpecimenAdapter(_specimen, Formula, ChromaticitySetting, SpecularComponent), (int)doc.PageWidth);
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(figure));
            MemoryStream stream = new MemoryStream();
            encoder.Save(stream);
            stream.Seek(0, SeekOrigin.Begin);
            var image = doc.AddImage(stream);

            var p = doc.InsertParagraph();
            p.InsertPicture(image.CreatePicture()).Alignment = Alignment.center;
        }




        private void InsertDatas(DocX doc)
        {
            var items = new Dictionary<string, SpecimenAdapter>();
            var Testitems = new Dictionary<string, TestSpecimenAdapter>();
            items.Add($"{Formula.Illuminant.Description()}/{Formula.Observer.Description()}", new SpecimenAdapter(_specimen, Formula, ChromaticitySetting, SpecularComponent));
            if (ReportSetting.IsExportReferences)
            {
                if (ReferenceFormula1 != null)
                    items.Add($"{ReferenceFormula1.Illuminant.Description()}/{ReferenceFormula1.Observer.Description()}", new SpecimenAdapter(_specimen, ReferenceFormula1, ChromaticitySetting, SpecularComponent));
                if (ReferenceFormula2 != null)
                    items.Add($"{ReferenceFormula2.Illuminant.Description()}/{ReferenceFormula2.Observer.Description()}", new SpecimenAdapter(_specimen, ReferenceFormula2, ChromaticitySetting, SpecularComponent));
            }
            var fields = ChromaticitySetting.DisplayFields;
            if (fields.Length > 0)
            {
                int columnCount = items.Count + 1;

                var table = AppendTable(doc, 2, columnCount, S._("SampleData"));
                SetColumnHeader(table, 1, 0, S._("Chromaticity"));

                int col = 1;
                foreach (var item in items)
                {
                    SetColumnHeader(table, 1, col++, item.Key);
                }
                int row = 2;
                foreach (var field in fields)
                {
                    if (!field.IsSampleReportField())
                        continue;
                    table.InsertRow();
                    SetRowHeader(table, row, 0, field.Description());
                    int column = 1;
                    foreach (var item in items)
                    {
                        var tmp= item.Value.GetFormatedPropertyValue(field);
                        table.Cell(row, column++).SetText(item.Value.GetFormatedPropertyValue(field));
                    }
                    row++;
                }
            }
        }




        private BitmapSource MakeFigure(SpecimenAdapter stdAdpater, int width)
        {
            Grid grid = new Grid();
            grid.Height = 150;

            int columnCount = 1;
            if (IsExportChromaticityChart)
                columnCount += 1;
            for (int i = 0; i < columnCount; i++)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition());
            }
            int gridColumn = 0;

            SpectrumPlotControl spectrumPlot = new SpectrumPlotControl();
            spectrumPlot.Margin = new Thickness(5);
            grid.Children.Add(spectrumPlot);
            Grid.SetColumn(spectrumPlot, gridColumn);

            if (stdAdpater.Spectrum != null)
                spectrumPlot.AddStandard(stdAdpater.Spectrum, stdAdpater.Name);

            //插入CIE Yxy色品图

            if (IsExportChromaticityChart)
            {
                gridColumn++;
                TriaxialChartView tcv = new TriaxialChartView();
                tcv.Margin = new Thickness(5);
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
