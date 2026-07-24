using System;
using System.Drawing;
//using DocumentFormat.OpenXml.Spreadsheet;
//using DocumentFormat.OpenXml.Spreadsheet;
//using DocumentFormat.OpenXml.Wordprocessing;
using ThreeNH.DataModel;
using Xceed.Words.NET;
using Alignment = Xceed.Words.NET.Alignment;
using Border = Xceed.Words.NET.Border;
using Paragraph = Xceed.Words.NET.Paragraph;
using Table = Xceed.Words.NET.Table;

namespace ThreeNH.ColorQualityControl.Reports
{

    public static class DocXExtensions
    {
        public static Cell Cell(this Table table, int row, int col)
        {
            return table.Rows[row].Cells[col];
        }

        public static Cell SetText(this Cell cell, string text)
        {
            if (string.IsNullOrEmpty(cell.Paragraphs[0].Text))
            {
                cell.Paragraphs[0].Append(text);
            }
            else
            {
                while (cell.Paragraphs.Count > 1)
                {
                    cell.RemoveParagraphAt(1);
                }

                cell.ReplaceText(cell.Paragraphs[0].Text, text);
            }

            return cell;
        }

        public static Cell Align(this Cell cell, Alignment align)
        {
            cell.Paragraphs[0].Alignment = align;
            return cell;
        }
    }

    abstract class WordReportBase : ReportBase
    {
        public TestSpecimen Test { get; set; }

        public abstract void GenerateReport();

        protected void AddReportInfo(DocX doc)
        {
            {
                doc.InsertParagraph(DateTime.Now.ToString())
                    .FontSize(8)
                    .Bold()
                    .Alignment = Alignment.right;
                
            }
            if (ReportSetting == null)
                return;

            if (!string.IsNullOrEmpty(ReportSetting.Title))
            {
                var p = AppendParagraph(doc, ReportSetting.Title);
                SetAsTitle(p);
            }
            if (!string.IsNullOrEmpty(ReportSetting.Subtitle))
            {
                var p = AppendParagraph(doc, ReportSetting.Subtitle);
                SetAsSubtitle(p);
            }
            if (!string.IsNullOrEmpty(ReportSetting.CompanyAddress))
            {
                var p = AppendParagraph(doc, ReportSetting.CompanyAddress);
                SetAsContent(p, Alignment.right);
            }
            if (!string.IsNullOrEmpty(ReportSetting.CompanyEnglishAddress))
            {
                var p = AppendParagraph(doc, ReportSetting.CompanyEnglishAddress);
                SetAsContent(p, Alignment.right);
            }
        }

        protected void SetAsReportTitle(Paragraph p)
        {
            p.UnderlineStyle(UnderlineStyle.singleLine).FontSize(18).Bold().Alignment = Alignment.center;
            SetAsContent(p.InsertParagraphAfterSelf(""));
        }

        protected void SetAsTitle(Paragraph p)
        {
            p.FontSize(20).Bold().Alignment = Alignment.center;
        }
        protected void SetAsSubtitle(Paragraph p)
        {
            p.FontSize(14).Bold().Alignment = Alignment.center;
        }
        protected void SetAsContent(Paragraph p, Alignment align = Alignment.left)
        {
            p.FontSize(10).UnderlineStyle(UnderlineStyle.none).Alignment = align;
        }
        protected Paragraph AppendParagraph(DocX doc, string text)
        {
            var p = doc.InsertParagraph(text);
            SetAsContent(p);
            return p;
        }


        protected static Border DoubleLineBorder { get; } = new Border(BorderStyle.Tcbs_single, BorderSize.one, 0,
            Color.DarkGray);

        protected static Border SingleLineBorder { get; } = new Border(BorderStyle.Tcbs_single, BorderSize.one, 0,
            Color.DarkGray);
        public bool IsExportChromaticityChart { get; internal set; }

        protected Table AppendTable(DocX doc, int numRows, int numColumns, string title)
        {
            var table = doc.InsertParagraph().InsertTableAfterSelf(numRows, numColumns);
//            var table = doc.InsertTable(numRows, numColumns);

//            table.AutoFit = AutoFit.Window;
            table.SetBorder(TableBorderType.Left, DoubleLineBorder);
            table.SetBorder(TableBorderType.Right, DoubleLineBorder);
            table.SetBorder(TableBorderType.Top, DoubleLineBorder);
            table.SetBorder(TableBorderType.Bottom, DoubleLineBorder);

            table.SetBorder(TableBorderType.InsideH, SingleLineBorder);
            table.SetBorder(TableBorderType.InsideV, SingleLineBorder);

            table.Rows[0].MergeCells(0, numColumns -1);
            table.Rows[0].Cells[0].Paragraphs[0].Append(title).Bold().Alignment = Alignment.center;

            table.Cell(0, 0).FillColor = Color.DarkGray;
            table.Cell(0, 0).Paragraphs[0].Color(Color.White);

//            for (int rowIndex = 1; rowIndex < numRows; rowIndex++)
//            {
//                var row = table.Rows[rowIndex];
//                for (var cellIndex = 0; cellIndex < row.ColumnCount; cellIndex++)
//                {
//                    var cell = row.Cells[cellIndex];
//                    cell.Paragraphs[0].Color(Color.Black);
//                    cell.FillColor = rowIndex % 2 == 1 ? Color.LightGray : Color.White;
//                }
//            }


            return table;
        }


        protected void SetColumnHeader(Table table, int row,int column, string text)
        {
            table.Rows[row].Cells[column].Paragraphs[0].Append(text)
                .Bold().Alignment = Alignment.center;
        }
        protected void SetRowHeader(Table table, int row, int column, string text)
        {
            table.Rows[row].Cells[column].Paragraphs[0].Append(text).Bold();
        }

        protected void SetAsColumnHeader(Cell cell)
        {
            cell.Paragraphs[0].Bold().Alignment = Alignment.center;
        }
        protected void SetAsRowHeader(Cell cell)
        {
            cell.Paragraphs[0].Bold();

        }
        protected void SetAsTableTitle(Cell cell)
        {
            cell.Paragraphs[0].Bold().Alignment = Alignment.center;
        }

        protected void AppendTail(DocX doc)
        {
            AppendParagraph(doc, S._("Test__") + "  " + S._("Audit__")).Alignment = Alignment.right;
            AppendParagraph(doc, string.Format(S._("InstrumentModel_0"), string.IsNullOrWhiteSpace(InstrumentModel) ? S._("Underline__") : InstrumentModel))
                .Alignment = Alignment.right;
        }

    }
}
