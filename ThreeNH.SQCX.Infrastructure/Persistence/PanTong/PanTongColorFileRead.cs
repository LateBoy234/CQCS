using ClosedXML.Excel;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ThreeNH.ColorAlgorithm;
using ThreeNH.ColorSpecimen;

namespace PanTongColorMatchSearch
{
    public class PanTongColor
    {
        private string _sampleNames;
        private string _sampleRemarks;

        public string SampleRemarks;

        public string SampleNames;

        public Specimen SpecimenData { get; set; }

        public CIELAB SciLab;

        public CIELAB SceLab;

        public string DateTimeStr { get; set; }
    }

    public class ColourAtlaCollection
    {
        public string Name { get; set; }

        public List<PanTongColor> ColourAtlas { get; set; }

        public static ColourAtlaCollection Load(string fileName)
        {
            if (!System.IO.File.Exists(fileName)) return null;
            var buffer = File.ReadAllText(fileName);
            return JsonConvert.DeserializeObject<ColourAtlaCollection>(buffer);
        }

        public void Save(string fileName)
        {
            var content = JsonConvert.SerializeObject(this);
            File.WriteAllText(fileName, content);
        }
    }

    public class PanTongColorFileRead
    {
        private PanTongColorFileConfig _cfg;
        public PanTongColorFileRead(PanTongColorFileConfig cfg)
        {
            _cfg = cfg;
        }
        public List<PanTongColor> ReadPanTongColorFromSheet()
        {
            try
            {
                FileStream fileStream = File.Open(_cfg.FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                var workbook = new XLWorkbook(fileStream);
                fileStream.Close();
                _cfg.SheetName = workbook.Worksheets.First().Name;
                return ReadPanTongColorFromSheet(workbook.Worksheet(_cfg.SheetName), _cfg);
            }
            catch (Exception ex)
            {
                // 适配层不弹 UI；调用方根据 null 决定是否提示
                Trace.TraceError(ex.Message);
                return null;
            }
        }
        private int ColumnLetterToNumber(string letter)
        {
            int number = 0;
            foreach (var ch in letter)
            {
                number = number * 26 + ch - 'A';
            }
            return number + 1;
        }
        private List<PanTongColor> ReadPanTongColorFromSheet(IXLWorksheet sheet, PanTongColorFileConfig config)
        {
            List<ColorData> swatches = new List<ColorData>();
            List<string> sampleRemarksList = new List<string>();
            List<string> sampleNamesList = new List<string>();
            List<CIELAB> labList = new List<CIELAB>();

            int rowNumberMax = config.EndRow < config.StartRow ? sheet.RowCount() : config.EndRow;
            var sampleNamesColumn = config.SampleNameColumn;
            WavelengthRange waveRange = config.WavelengthRange;


            var columns = sheet.Columns().Select(x => x.ColumnLetter());
            if (sampleNamesColumn != null && !columns.Contains(config.SampleNameColumn))
            {
                sampleNamesColumn = null;
            }

            for (int rowNumber = config.StartRow; rowNumber < rowNumberMax; rowNumber++)
            {
                IXLRow row = sheet.Row(rowNumber);
                // 遇到第一个空行跳出
                if (row.IsEmpty(false))
                {
                    break;
                }
                if (config.SampleRemarksColumn != null)
                {
                    sampleRemarksList.Add(row.Cell(config.SampleRemarksColumn).GetString());
                }


                if (sampleNamesColumn != null)
                {
                    sampleNamesList.Add(row.Cell(sampleNamesColumn).GetString());
                }

                ColorData clr = new ColorData();

                if (config.SpectraStartColumn != null)
                {
                    Spectrum s = new Spectrum(waveRange);
                    int startColumnNumber = ColumnLetterToNumber(config.SpectraStartColumn);
                    for (int i = 0; i < waveRange.Count; i++)
                    {
                        s.Data[i] = row.Cell(i + startColumnNumber).GetDouble() / 100.0;
                    }

                    clr.Spectrum = s;
                }
                else if (config.YxyStartColumn != null)
                {
                    var cell = row.Cell(config.YxyStartColumn);
                    double Y = cell.GetDouble();
                    cell = cell.CellRight();
                    double x = cell.GetDouble();
                    cell = cell.CellRight();
                    double y = cell.GetDouble();

                    //clr.Set(StandardObserver.CIE1964, StandardIlluminant.D65, formula.YxyToXyz(new CIEYxy(Y, x, y)));
                }
                if (config.LabStartColumn != null)
                {
                    var cell = row.Cell(config.LabStartColumn);
                    double L = cell.GetDouble();
                    cell = cell.CellRight();
                    double a = cell.GetDouble();
                    cell = cell.CellRight();
                    double b = cell.GetDouble();

                    labList.Add(new CIELAB(L, a, b));
                }

                swatches.Add(clr);
            }


            List<PanTongColor> list = new List<PanTongColor>();
            for (int i = 0; i < swatches.Count;)
            {
                PanTongColor standard = new PanTongColor();
                Specimen specimen = new Specimen();
                standard.SampleRemarks = config.SampleRemarksColumn != null ? sampleRemarksList[i] : null;
                standard.SampleNames = sampleNamesColumn != null ? sampleNamesList[i] : null;
                if (config.IfNot45_0)
                {
                    if (config.ScModeSetting == ScModeSetting.None || config.ScModeSetting == ScModeSetting.Sci) // Unknown or SCI
                    {
                        standard.SciLab = labList[i];
                        specimen.SCI = swatches[i++];
                        standard.SpecimenData = specimen;

                    }
                    else if (config.ScModeSetting == ScModeSetting.Sce) // SCE
                    {
                        standard.SceLab = labList[i];
                        specimen.SCE = swatches[i++];
                        standard.SpecimenData = specimen;
                    }
                    else if (config.ScModeSetting == ScModeSetting.SciFollowWithSce) // SCI followed by SCE
                    {
                        standard.SciLab = labList[i];
                        specimen.SCI = swatches[i++];
                        if (i < swatches.Count)
                        {
                            standard.SceLab = labList[i];
                            specimen.SCE = swatches[i++];
                        }
                        standard.SpecimenData = specimen;
                    }
                    else if (config.ScModeSetting == ScModeSetting.SceFollowWithSci) // SCE followed by SCI
                    {
                        standard.SceLab = labList[i];
                        specimen.SCE = swatches[i++];
                        if (i < swatches.Count)
                        {
                            standard.SciLab = labList[i];
                            specimen.SCI = swatches[i++];
                        }
                        standard.SpecimenData = specimen;
                    }
                }
                else
                {
                    standard.SciLab = labList[i];
                    specimen.Clr = swatches[i++];
                    standard.SpecimenData = specimen;
                }

                list.Add(standard);
            }

            return list;
        }
    }

}