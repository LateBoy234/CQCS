using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace ThreeNH.ColorQualityControl
{
    class ColorLibraryInfo
    {

        public static List<string> GetLibraryNames()
        {
            List<string> list = new List<string>();
            foreach (var fileName in Directory.EnumerateFiles(App.LibraryPath, "*.db", SearchOption.TopDirectoryOnly))
            {
                list.Add(System.IO.Path.GetFileNameWithoutExtension(fileName));
            }
            if (list.Count == 0)
            {
                list.Add("Default");
            }
            return list;
        }

        public static string GetLibraryFilePath(string libraryName)
        {
            return System.IO.Path.Combine(App.LibraryPath, $"{libraryName}.db");
        }

        public static string GetDbConnectionString(string libraryName)
        {
            return $"Data Source={GetLibraryFilePath(libraryName)}";
        }

        /// <summary>
        /// 最近使用的存样库名
        /// </summary>
        public static string LastUsedLibrary
        {
            get { return AppSettings.LastUsedLibrary; }
            set => AppSettings.LastUsedLibrary = value;
        }


        public static bool IsNameValid(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }
            return name.Intersect(Path.GetInvalidFileNameChars()).Count() == 0;
        }

        public static bool AutoClearAfterStored
        {
            get => AppSettings.LibraryAutoClear;
            set => AppSettings.LibraryAutoClear = value;
        }
    }
}
