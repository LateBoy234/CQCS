using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ThreeNH.ColorQualityControl
{
    class MyTextWriter : IMyWriter
    {
        StreamWriter stream;
        public void Create(string fileName)
        {
            stream = File.CreateText(fileName);
        }

        public void Close()
        {
            stream.Close();
        }

        public void WriteRow(string[] items)
        {
            if (items != null)
            {
                stream.WriteLine(string.Join("\t", items));
            }
            else
            {
                stream.WriteLine();
            }
        }

        public int NextRow
        {
            get; set;
        }
    }
}
