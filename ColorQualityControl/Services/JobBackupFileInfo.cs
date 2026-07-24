using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace ThreeNH.ColorQualityControl.Services
{
    /// <summary>空闲备份文件元数据（阶段1-T21，文件名格式：{JobFileName}@{yyyy-MM-dd_HHmmss}）。</summary>
    internal sealed class JobBackupFileInfo
    {
        private JobBackupFileInfo(string backupPath, string fileName, DateTime dateTime)
        {
            BackupPath = backupPath;
            FileName = fileName;
            DateTime = dateTime;
        }

        public string BackupPath { get; }

        public string FileName { get; }

        public DateTime DateTime { get; }

        public static string GetFilePattern(string fileName = null)
        {
            if (fileName != null)
            {
                return "(" + Regex.Escape(Path.GetFileName(fileName)) + @")@(\d{4}-\d{2}-\d{2}_\d{6})$";
            }

            return @"(.+)@(\d{4}-\d{2}-\d{2}_\d{6})$";
        }

        public static JobBackupFileInfo TryParse(string backupFilePath)
        {
            try
            {
                var pattern = GetFilePattern();
                var m = Regex.Match(Path.GetFileName(backupFilePath), pattern);
                if (!m.Success)
                {
                    return null;
                }

                return new JobBackupFileInfo(
                    backupFilePath,
                    m.Groups[1].Value,
                    DateTime.ParseExact(m.Groups[2].Value, "yyyy-MM-dd_HHmmss", CultureInfo.CurrentCulture));
            }
            catch
            {
                return null;
            }
        }
    }
}
