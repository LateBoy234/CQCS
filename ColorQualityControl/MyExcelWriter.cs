using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;


namespace ThreeNH.ColorQualityControl
{
    class MyExcelWriter : IMyWriter
    {
        XLWorkbook workbook;
        IXLWorksheet activeSheet;
        string fileName;
        Regex colorPattern = new Regex("^#[0-9A-F]{8}$");

        public void Create(string fileName)
        {
            this.fileName = fileName;

            workbook = new XLWorkbook();
            activeSheet = workbook.Worksheets.Add("Sheet1");

            NextRow = 1;
        }

        public void Close()
        {
            workbook.SaveAs(fileName);
        }

        public void WriteRow(string[] items)
        {
            if (items != null)
            {
                int col = 1;
                foreach (var item in items)
                {
                    var cell = activeSheet.Cell(NextRow, col++);

                    if (item != null && colorPattern.IsMatch(item))
                    {
                        cell.Value = item;
                        cell.Style.Fill.BackgroundColor = XLColor.FromHtml(item);
                    }
                    else
                    {
                        if (TryParseExportNumber(item, out double numValue))
                        {
                            cell.Value = numValue;
                            cell.Style.NumberFormat.Format = "0.00";
                        }
                        else
                        {
                            cell.Style.NumberFormat.Format = "@";
                            cell.Value = item;
                        }
                    }
                }
            }
            NextRow += 1;
        }

        private static bool TryParseExportNumber(string item, out double value)
        {
            value = 0;

            if (string.IsNullOrWhiteSpace(item))
                return false;

            string text = item.Trim();
            if (text == "--" || text.Contains("/") || text.Contains("-") || text.Contains(":") || text.Contains("%"))
                return false;

            if (!Regex.IsMatch(text, @"^[+-]?\d+([,.]\d+)?$"))
                return false;

            if (double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value))
                return true;

            string invariantText = text.Replace(',', '.');
            return double.TryParse(invariantText, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        public int NextRow
        {
            get; set;
        }
    }
}
