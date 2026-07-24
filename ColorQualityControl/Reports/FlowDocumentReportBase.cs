using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace ThreeNH.ColorQualityControl.Reports
{
    static class FlowDocumentExtensions
    {
        public static Table AddTable(this FlowDocument doc, int rowCount, int columnCount)
        {
            Table table = new Table();
            table.Style = FlowDocumentReportBase.TableStyle;
            table.RowGroups.Add(new TableRowGroup());
            for (int col = 0; col < columnCount; col++)
            {
                table.Columns.Add(new TableColumn());
            }

            for (int row = 0; row < rowCount; row++)
            {
                TableRow tableRow = new TableRow();
                table.RowGroups[0].Rows.Add(tableRow);
                for (int col = 0; col < columnCount; col++)
                {
                    var cell = new TableCell {Style = FlowDocumentReportBase.CellStyle};
                    cell.Tag = new Tuple<int, int>(row, col);
                    tableRow.Cells.Add(cell);
                }
            }

            return table;
        }

        public static TableCell Cell(this Table table, int row, int col)
        {
            foreach (var tableCell in table.RowGroups[0].Rows[row].Cells)
            {
                var tuple = ((Tuple<int, int>)tableCell.Tag);
                if (tuple.Item1 == row && tuple.Item2 == col)
                {
                    return tableCell;
                }
            }

            return null;
        }

        public static TableRow Row(this Table table, int row)
        {
            return table.RowGroups[0].Rows[row];
        }

        public static TableCell MergeCells(this TableRow tableRow, int startColumn, int endColumn)
        {
            var span = endColumn - startColumn + 1;
            tableRow.Cells[startColumn].ColumnSpan = span;
            tableRow.Cells.RemoveRange(startColumn + 1, span - 1);
            return tableRow.Cells[startColumn];
        }

        private static Table RemoveCell(this Table table, int row, int col)
        {
            var tableRow = table.RowGroups[0].Rows[row];
            var cell = table.Cell(row, col);
            if (cell != null)
            {
                tableRow.Cells.Remove(cell);
            }

            return table;
        }

        public static Table MergeCellsInColumn(this Table table, int column, int startRow, int endRow)
        {
            table.Cell(startRow, column).RowSpan = (endRow - startRow) + 1;
            for (int row = startRow + 1; row <= endRow; row++)
            {
                table.RemoveCell(row, column);
            }
            return table;
        }

        public static TableCell SetText(this TableCell cell, string text)
        {
            if (cell.Blocks.Count != 0)
            {
                cell.Blocks.Clear();
            }
            cell.Blocks.Add(new Paragraph(new Run(text)));
            return cell;
        }

        public static TableCell Align(this TableCell cell, TextAlignment alignment)
        {
            cell.TextAlignment = alignment;
            return cell;
        }

        public static FlowDocument InsertParagraph(this FlowDocument doc, string text = null)
        {
            var p = new Paragraph(new Run(text));
            doc.Blocks.Add(p);
            return doc;
        }

        public static FlowDocument InsertTable(this FlowDocument doc, Table table)
        {
            doc.Blocks.Add(table);
            return doc;
        }
    }

    abstract class FlowDocumentReportBase : ReportBase
    {
        public const double PageWidth = 210 / 25.4 * 96;
        public const double PageHeight = 297 / 25.4 * 96;
        public static Style CellStyle;
        public static Style TableStyle;

        static FlowDocumentReportBase()
        {
            CellStyle = new Style(typeof(TableCell));
            CellStyle.Setters.Add(new Setter(TableCell.BorderBrushProperty, Brushes.Black));
            CellStyle.Setters.Add(new Setter(TableCell.BorderThicknessProperty, new Thickness(0.5)));
            CellStyle.Setters.Add(new Setter(TableCell.TextAlignmentProperty, TextAlignment.Center));

            TableStyle = new Style(typeof(Table));
            TableStyle.Setters.Add(new Setter(Table.BorderBrushProperty, Brushes.Black));
            TableStyle.Setters.Add(new Setter(Table.BorderThicknessProperty, new Thickness(1)));
            TableStyle.Setters.Add(new Setter(Table.CellSpacingProperty, 0.0));
        }

        protected void AddReportInfo(FlowDocument doc)
        {
            {
                var p = new Paragraph(new Run(DateTime.Now.ToString()));
                p.FontSize = 10;
                p.TextAlignment = TextAlignment.Right;
                doc.Blocks.Add(p);
            }

            if (ReportSetting == null)
                return;

            if (!string.IsNullOrEmpty(ReportSetting.Title))
            {
                var p = new Paragraph(new Run(ReportSetting.Title));
                p.FontSize = 24;
                p.TextAlignment = TextAlignment.Center;
                p.FontWeight = FontWeights.Bold;
                doc.Blocks.Add(p);
            }
            if (!string.IsNullOrEmpty(ReportSetting.Subtitle))
            {
                var p = new Paragraph(new Run(ReportSetting.Subtitle));
                p.FontSize = 16;
                p.FontWeight = FontWeights.Bold;
                p.TextAlignment = TextAlignment.Center;
                doc.Blocks.Add(p);
            }
            if (!string.IsNullOrEmpty(ReportSetting.CompanyAddress ))
            {
                var p = new Paragraph(new Run(ReportSetting.CompanyAddress));
                p.FontSize = 12;
                p.TextAlignment = TextAlignment.Right;
                doc.Blocks.Add(p);
            }
            if (!string.IsNullOrEmpty(ReportSetting.CompanyEnglishAddress))
            {
                var p = new Paragraph(new Run(ReportSetting.CompanyEnglishAddress));
                p.FontSize = 12;
                p.TextAlignment = TextAlignment.Right;
                doc.Blocks.Add(p);
            }
        }

        protected TableCell NewCell(string text = "")
        {
            TableCell cell = new TableCell();
            if (!string.IsNullOrWhiteSpace(text))
            {
                var p = new Paragraph(new Run(text));
                p.Margin = new Thickness(3);
                cell.Blocks.Add(p);
            }
            cell.Style = CellStyle;
            return cell;
        }

        protected TableCell NewTableHeader(string text, int columnSpan)
        {
            TableCell cell = new TableCell();
            cell.Blocks.Add(new Paragraph(new Run(text)));
            cell.Style = CellStyle;
            cell.ColumnSpan = columnSpan;
            return cell;
        }

        protected TableRow NewRow(TableRowGroup rowGroup)
        {
            TableRow row = new TableRow();
            rowGroup.Rows.Add(row);
            return row;
        }

        protected Table NewTable(string header = null, int? columnCount = null)
        {
            Table table = new Table();
            table.Style = TableStyle;
            table.RowGroups.Add(new TableRowGroup());
            if (header != null && columnCount != null)
            {
                for (int i = 0; i < (int)columnCount; i++)
                {
                    table.Columns.Add(new TableColumn());
                }
                var row = NewRow(table.RowGroups[0]);
                row.Cells.Add(NewTableHeader(header, (int)columnCount));
            }
            return table;
        }


        public abstract FlowDocument GetFlowDocument();


        protected Paragraph MakeReportTitle(string reportTitle)
        {
            Paragraph title = new Paragraph(new Underline(new Run(reportTitle)));
            title.FontSize = 20;
            title.FontWeight = FontWeights.Bold;
            title.TextAlignment = TextAlignment.Center;
            return title;
        }

        protected Table MakeTail()
        {
            Table table = new Table();
            var rowGroup = new TableRowGroup();
            table.RowGroups.Add(rowGroup);
            var row = NewRow(rowGroup);

            var p = new Paragraph(new Run(S._("Test__") + "  " + S._("Audit__")));
            var cell = new TableCell(p);
            cell.TextAlignment = TextAlignment.Right;
            row.Cells.Add(cell);


            row = NewRow(rowGroup);
            p = new Paragraph(new Run(string.Format(S._("InstrumentModel_0"), string.IsNullOrWhiteSpace(InstrumentModel) ? S._("Underline__") : InstrumentModel)));
            p.Margin = new Thickness(0, 10, 0, 0);
            cell = new TableCell(p);
            cell.TextAlignment = TextAlignment.Right;
            row.Cells.Add(cell);
            return table;
        }

        protected Table MakeNumberDateBlock()
        {
            Table table = new Table();
            var rowGroup = new TableRowGroup();
            table.RowGroups.Add(rowGroup);
            var row = NewRow(rowGroup);
            row.Cells.Add(NewCell("No.:"));
            row.Cells.Add(NewCell(DateTime.Now.ToString()));
            row.Cells[0].TextAlignment = TextAlignment.Left;
            row.Cells[0].BorderThickness = new Thickness(0);
            row.Cells[1].TextAlignment = TextAlignment.Right;
            row.Cells[1].BorderThickness = new Thickness(0);
            return table;
        }

    }
}
