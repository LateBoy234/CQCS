using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ThreeNH.ColorQualityControl.UI.ColourAtla
{
    public static class TableAdapter
    {
        private const string TableName = "MatSamples";
        private const string Name = "Common_Name";
        private const string IllumObserStr = "Color_IlluminantObserver";
        public static DataTable CreateTableFromKnownTable( DataTable knownTable)
        {
            DataTable newTable = new DataTable(TableName);
            bool _bl = false;
            if (knownTable == null) throw new Exception();
            try
            {
                foreach (DataColumn col in knownTable.Columns)
                {
                    if (col.ToString() == Name)
                    {
                        _bl = true;
                        break ;
                    }
                }
                foreach (DataColumn col in knownTable.Columns)
                {
                    if (!_bl)
                    {
                        newTable.Columns.Add(S._(Name), Name.GetType());
                        _bl = true;
                    }
                    if (col.ToString() == IllumObserStr)
                    {
                        newTable.Columns.Add(S._(col.ToString() + "Lilin"), col.DataType);
                        continue ;
                    }
                    newTable.Columns.Add(col.ToString(), col.DataType);
                    
                }
                return newTable;
            }
            catch 
            {
                return null;
            }
        }
        private static void FindGivenNameColumn(DataTable knownTable, string name,ref int pos)
        {
            int c = 0;
            foreach (DataColumn col in knownTable.Columns)
            {
                if (col.ToString() == name)
                {
                    pos = c;
                    break;
                }
                c++;
            }
        }
        public static void  RowAdaptFromKnownTable(List<string> listName, ref DataTable newTable, DataTable knownTable)
        {
            int namePos = -1, illumObserPos = -1;
            FindGivenNameColumn(knownTable, Name, ref namePos);
            FindGivenNameColumn(knownTable, IllumObserStr, ref illumObserPos);
            int count = 0;
            foreach (DataRow row in knownTable.Rows)
            {

                DataRow dataRow = newTable.NewRow();
                int i = 0;
                if (namePos == -1)
                {
                    dataRow[i] = listName.ElementAt(count);
                    count++;
                    i++;
                }
                
                foreach (var item in row.ItemArray)
                {
                    if (item is double)
                    {
                        double ff = (double)item;
                        ff = Math.Round(ff, 2);
                        dataRow[i] = ff;
                    }
                    else if (item is float)
                    {
                        float ff = (float)item;
                        ff = (float)Math.Round((double)ff, 2);
                        dataRow[i] = ff;
                    }
                    else if (item is string)
                    {
                        dataRow[i] = S._(item.ToString());
                    }
                    else
                    {
                        dataRow[i] = item;
                    }
                    if (i == namePos)
                    {
                        dataRow[i] = listName.ElementAt(count);
                        count++;
                    }
                    i++;
                }
                newTable.Rows.Add( dataRow );
            }            
        }
    }
}
