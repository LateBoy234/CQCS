using System;
using System.Windows.Documents;
using ThreeNH.ColorQualityControl.Adapter;
using ThreeNH.Optical;
using ThreeNH.DataModel;
using System.Windows;
using System.Windows.Media;
using ThreeNH.ColorAlgorithm;
using ThreeNH.Helper;

namespace ThreeNH.ColorQualityControl.Reports
{
    class ChromaticityAnalyzeReport
    {
        public StandardSpecimenAdapter Standard { get; set; }
        public TestSpecimenAdapter Test { get; set; }

        public ChromaticitySetting ChromaticitySetting
        {
            get
            {
                return MainWindow.ChromaticitySetting;
            }
        }

        public SpecimenDatabase Database
        {
            get { return MainWindow.Database; }
        }
        
        StandardIlluminant Illuminant
        {
            get
            {
                return MainWindow.Illuminant;
            }
        }

        StandardObserver Observer
        {
            get
            {
                return MainWindow.Observer;
            }
        }

        SpecularComponent SpecularComponent
        {
            get { return MainWindow.SpecularComponent; }
        }

        MainWindow MainWindow
        {
            get { return (Application.Current.MainWindow as MainWindow); }
        }

        public string Title
        {
            get
            {
                return string.Format("{0}, {1}/{2}",
                    ChromaticitySetting.ColorSpace.Name(),
                    Illuminant.ToString(), Observer == StandardObserver.CIE1931 ?
                    "2\u00B0" : "10\u00B0");
            }
        }

        private FlowDocument GetStandardReport()
        {
            FlowDocument doc = new FlowDocument();

            Paragraph p = new Paragraph();
            doc.Blocks.Add(p);
            p.Inlines.Add(new Run(Title));
            p.FontWeight = FontWeights.Bold;
            p.FontSize = 12;
            p.TextAlignment = TextAlignment.Center;

            Table table = new Table();
            doc.Blocks.Add(table);
            table.Columns.Add(NewTableColumn(60));
            table.Columns.Add(NewTableColumn());

            TableRowGroup rowGroup = new TableRowGroup();
            table.RowGroups.Add(rowGroup);

            TableRow row = new TableRow();
            row.Cells.Add(NewCell(S._("Standard"), true));
            if (Standard != null)
            {
                var cell = NewCell(Standard.Name);
                cell.ColumnSpan = 2;
                row.Cells.Add(cell);
            }

            rowGroup.Rows.Add(row);

            // 顏色值
            table = new Table();
            doc.Blocks.Add(table);

            table.Columns.Add(NewTableColumn(60));
            table.Columns.Add(NewTableColumn(60));
            table.Columns.Add(NewTableColumn(60));
            table.Columns.Add(NewTableColumn(60));
            table.Columns.Add(NewTableColumn(60));
            table.Columns.Add(NewTableColumn(60));
            table.Columns.Add(NewTableColumn(60));

            rowGroup = new TableRowGroup();
            table.RowGroups.Add(rowGroup);

            row = new TableRow();
            row.Background = Brushes.DarkCyan;
            rowGroup.Rows.Add(row);
            row.Cells.Add(new TableCell());
            row.Cells.Add(NewCell(S._("Standard"), underline: true));
            row.Cells.Add(NewCell(S._("Min"), underline: true));
            row.Cells.Add(NewCell(S._("Max"), underline: true));
            row.Cells.Add(NewCell(S._("Range"), underline: true));
            row.Cells.Add(NewCell(S._("Mean"), underline: true));
            row.Cells.Add(NewCell(S._("StdDev"), underline: true));

            var statistics = Database.GetStatistics(Standard.StandardSpecimen,
                SpecularComponent, Observer, Illuminant);

            foreach (var field in ChromaticitySetting.ColorFields)
            {
                row = new TableRow();
                row.Background = Brushes.LightBlue;
                rowGroup.Rows.Add(row);
                row.Cells.Add(NewCell(field.Name(), true));


                row.Cells.Add(NewCell(Standard.GetFormatedPropertyValue(field)));
                if (statistics != null && statistics.ContainsKey(field))
                {
                    row.Cells.Add(NewCell(statistics[field].Min.ToString("F3")));
                    row.Cells.Add(NewCell(statistics[field].Max.ToString("F3")));
                    row.Cells.Add(NewCell(statistics[field].Range.ToString("F3")));
                    row.Cells.Add(NewCell(statistics[field].Mean.ToString("F3")));
                    row.Cells.Add(NewCell(statistics[field].StdDev.ToString("F3")));
                }
            }

            var colorDiffFields = ChromaticitySetting.ColorDiffFields;
            if (colorDiffFields.Length > 0)
            {
                // 色差统计
                table = new Table();
                doc.Blocks.Add(table);

                table.Columns.Add(NewTableColumn(60));
                table.Columns.Add(NewTableColumn(60));
                table.Columns.Add(NewTableColumn(60));
                table.Columns.Add(NewTableColumn(60));
                table.Columns.Add(NewTableColumn(60));
                table.Columns.Add(NewTableColumn(60));
                table.Columns.Add(NewTableColumn(60));

                rowGroup = new TableRowGroup();
                table.RowGroups.Add(rowGroup);

                row = new TableRow();
                row.Background = Brushes.DarkCyan;
                rowGroup.Rows.Add(row);
                row.Cells.Add(new TableCell());
                row.Cells.Add(NewCell(S._("Min"), underline: true));
                row.Cells.Add(NewCell(S._("Max"), underline: true));
                row.Cells.Add(NewCell(S._("Range"), underline: true));
                row.Cells.Add(NewCell(S._("Mean"), underline: true));
                row.Cells.Add(NewCell(S._("StdDev"), underline: true));
                row.Cells.Add(NewCell(S._("PrecentOfPass"), underline: true));

                Tolerance tolerance = Database.GetTolerance(Standard.StandardSpecimen,
                    new ObserverIlluminantPair(Observer, Illuminant));
                // 色差统计
                foreach (var field in colorDiffFields)
                {
                    row = new TableRow();
                    row.Background = Brushes.LightBlue;
                    rowGroup.Rows.Add(row);


                    row.Cells.Add(NewCell(field.Name(), true));

                    if (statistics != null && statistics.ContainsKey(field))
                    {
                        row.Cells.Add(NewCell(statistics[field].Min.ToString("F3")));
                        row.Cells.Add(NewCell(statistics[field].Max.ToString("F3")));
                        row.Cells.Add(NewCell(statistics[field].Range.ToString("F3")));
                        row.Cells.Add(NewCell(statistics[field].Mean.ToString("F3")));
                        row.Cells.Add(NewCell(statistics[field].StdDev.ToString("F3")));
                        row.Cells.Add(NewCell(statistics[field].PercentOfPass.ToString("F2")));
                    }
                }
            }

            var colorIndies = ChromaticitySetting.ColorIndexFields;
            if (colorIndies.Length > 0)
            {
                // 顔色指數
                table = new Table();
                doc.Blocks.Add(table);
                rowGroup = new TableRowGroup();
                table.RowGroups.Add(rowGroup);

                row = new TableRow();
                row.Cells.Add(NewCell(S._("ColorIndexes"), underline: true));
                row.Cells.Add(NewCell(S._("Standard"), underline: true));

                foreach (var field in ChromaticitySetting.ColorIndexFields)
                {
                    if (field.IsNeedStandard())
                        continue;

                    row = new TableRow();
                    rowGroup.Rows.Add(row);
                    row.Cells.Add(NewCell(field.Name(), true));
                    row.Cells.Add(NewCell(Standard.GetFormatedPropertyValue(field)));
                }
            }


            return doc;
        }

        private FlowDocument GetTestReport()
        {
            FlowDocument doc = new FlowDocument();

            Paragraph p = new Paragraph();
            doc.Blocks.Add(p);
            p.Inlines.Add(new Run(Title));
            p.FontWeight = FontWeights.Bold;
            p.FontSize = 12;
            p.TextAlignment = TextAlignment.Center;

            Table table = new Table();
            doc.Blocks.Add(table);
            table.Columns.Add(NewTableColumn(60));
            table.Columns.Add(NewTableColumn(60));

            TableRowGroup rowGroup = new TableRowGroup();
            table.RowGroups.Add(rowGroup);

            TableRow row = new TableRow();
            row.Cells.Add(NewCell(S._("Standard"), true));
            if (Standard != null)
            {
                var stdNameCell = NewCell(Standard.Name);
                stdNameCell.ColumnSpan = 2;
                row.Cells.Add(stdNameCell);
            }

            rowGroup.Rows.Add(row);

            row = new TableRow();
            row.Cells.Add(NewCell(S._("Test"), true));
            if (Test != null)
            {
                var testNameCell = NewCell(Test.Name);
                testNameCell.ColumnSpan = 2;
                row.Cells.Add(testNameCell);
            }

            rowGroup.Rows.Add(row);


            table = new Table();
            doc.Blocks.Add(table);

            table.Columns.Add(new TableColumn());
            table.Columns.Add(new TableColumn());
            table.Columns.Add(new TableColumn());

            // 顏色值
            rowGroup = new TableRowGroup();
            table.RowGroups.Add(rowGroup);

            row = new TableRow();
            row.Background = Brushes.DarkCyan;
            rowGroup.Rows.Add(row);
            row.Cells.Add(new TableCell());
            row.Cells.Add(NewCell(S._("Standard"), underline: true));
            row.Cells.Add(NewCell(S._("Test"), underline: true));

            foreach (var field in ChromaticitySetting.ColorFields)
            {
                row = new TableRow();
                row.Background = Brushes.LightBlue;
                rowGroup.Rows.Add(row);
                row.Cells.Add(NewCell(field.Name(), true));

                if (Standard != null)
                    row.Cells.Add(NewCell(Standard.GetFormatedPropertyValue(field)));
                else
                    row.Cells.Add(new TableCell());

                if (Test != null)
                    row.Cells.Add(NewCell(Test.GetFormatedPropertyValue(field)));
                else
                    row.Cells.Add(new TableCell());
            }


            // 色差值
            table = new Table();
            table.CellSpacing = 5;
            doc.Blocks.Add(table);
            rowGroup = new TableRowGroup();
            table.RowGroups.Add(rowGroup);

            row = new TableRow();
            row.Background = Brushes.DarkCyan;
            rowGroup.Rows.Add(row);
            row.Cells.Add(new TableCell());
            row.Cells.Add(NewCell(S._("ColorDiff"), underline: true));
            row.Cells.Add(NewCell(S._("Judgement"), underline: true));

            TableCell cell = null;

            var judgement = Test.Judgement;
            foreach (var field in ChromaticitySetting.ColorDiffFields)
            {
                row = new TableRow();
                row.Background = Brushes.LightBlue;
                rowGroup.Rows.Add(row);
                row.Cells.Add(NewCell(field.Name(), true));
                if (Test != null)
                {
                    row.Cells.Add(NewCell(Test.GetFormatedPropertyValue(field)));
                    if (judgement != null)
                    {
                        cell = NewCell(judgement[field].Description());
                        cell.Foreground = Database.JudgementSetting.Colors[judgement[field]];
                        row.Cells.Add(cell);
                    }
                }
            }
            row = new TableRow();
            row.Background = Brushes.LightBlue;
            rowGroup.Rows.Add(row);
            row.Cells.Add(NewCell(S._("TotalJudgement"), bold: true));
            if (judgement != null)
            {
                cell = NewCell(judgement.TotalJudgement.Description());
                cell.ColumnSpan = 2;
                cell.Foreground = Database.JudgementSetting.Colors[judgement.TotalJudgement];
                cell.TextAlignment = TextAlignment.Center;
                cell.FontSize = 14;
                row.Cells.Add(cell);
            }

            // 顔色指數
            table = new Table();
            table.CellSpacing = 5;
            doc.Blocks.Add(table);
            rowGroup = new TableRowGroup();
            table.RowGroups.Add(rowGroup);

            row = new TableRow();
            row.Cells.Add(NewCell(S._("ColorIndexes"), underline: true));
            row.Cells.Add(NewCell(S._("Standard"), underline: true));
            row.Cells.Add(NewCell(S._("Test"), underline: true));

            foreach (var field in ChromaticitySetting.ColorIndexFields)
            {
                
                if (field == ChromaticityField.MI1)
                {
                    Formula ref1, ref2;
                    Database.GetReferenceFormula(MainWindow.Formula, out ref1, out ref2);
                    if (ref1 != null)
                    {
                        row = new TableRow();
                        rowGroup.Rows.Add(row);
                        row.Cells.Add(NewCell(SpecimenAdapter.MakeMIName(ref1), true));

                        if (Standard != null)
                        {
                            row.Cells.Add(NewCell("--"));
                        }
                        else
                        {
                            row.Cells.Add(new TableCell());
                        }
                        if (Test != null)
                        {
                            row.Cells.Add(NewCell(SpecimenAdapter.FormatMI(Test.MI1)));
                        }
                        else
                        {
                            row.Cells.Add(new TableCell());
                        }
                    }
                }
                else if (field == ChromaticityField.MI2)
                {
                    Formula ref1, ref2;
                    Database.GetReferenceFormula(MainWindow.Formula, out ref1, out ref2);
                    if (ref2 != null)
                    {
                        row = new TableRow();
                        rowGroup.Rows.Add(row);
                        row.Cells.Add(NewCell(SpecimenAdapter.MakeMIName(ref2), true));

                        if (Standard != null)
                        {
                            row.Cells.Add(NewCell("--"));
                        }
                        else
                        {
                            row.Cells.Add(new TableCell());
                        }
                        if (Test != null)
                        {
                            row.Cells.Add(NewCell(SpecimenAdapter.FormatMI(Test.MI2)));
                        }
                        else
                        {
                            row.Cells.Add(new TableCell());
                        }
                    }
                }
                else
                {
                    row = new TableRow();
                    rowGroup.Rows.Add(row);
                    row.Cells.Add(NewCell(field.Name(), true));

                    if (Standard != null)
                    {
                        row.Cells.Add(NewCell(Standard.GetFormatedPropertyValue(field)));
                    }
                    else
                    {
                        row.Cells.Add(new TableCell());
                    }
                    if (Test != null)
                    {
                        row.Cells.Add(NewCell(Test.GetFormatedPropertyValue(field)));
                    }
                    else
                    {
                        row.Cells.Add(new TableCell());
                    }
                }
            }

            return doc;
        }

        public FlowDocument GetReport()
        {
            if (Standard == null && Test == null)
                return new FlowDocument();

            if (Test == null)
                return GetStandardReport();

            return GetTestReport();
        }


        TableCell NewCell(string text, bool bold = false, bool underline = false)
        {
            Inline inline = new Run(text);
            if (bold)
                inline = new Bold(inline);
            if (underline)
                inline = new Underline(inline);
            var cell = new TableCell(new Paragraph(inline));
            return cell;
        }

        TableColumn NewTableColumn(double? width = null)
        {
            TableColumn column = new TableColumn();
            column.Width = width != null ? new GridLength((double)width) : GridLength.Auto;
            return column;
        }
    }
}
