using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using ThreeNH.ColorAlgorithm;
using ThreeNH.ColorSpecimen;
using ThreeNH.Optical;

namespace ThreeNH.ColorLibrary
{
    public class ColorDatabase : IDisposable
    {
        private const int SPECTRAL_DATA_FLAG = 0;
        private const int XYZ_DATA_FLAG = 1;
        //        private const int SCI = (int)SpecularComponent.SCI;
        //        private const int SCE = (int)SpecularComponent.SCE;
        //        private const int OTHER = (int)SpecularComponent.Unknown;

        private bool _isDisposed = false;
        private DatabaseHelper _helper;
        private DbConnection _connection;
        private DbDataAdapter _colorsAdapter;
        private DbDataAdapter _pigmentsAdapter;
        private DbDataAdapter _materialsAdapter;

        /// <summary>
        /// 获取数据集
        /// </summary>
        public DataSet DataSet { get; private set; }

        /// <summary>
        /// 颜色表
        /// </summary>
        public DataTable Colors => DataSet.Tables["Colors"];

        /// <summary>
        /// 物料表
        /// </summary>
        public DataTable Materials => DataSet.Tables["Materials"];

        /// <summary>
        /// 颜类表
        /// </summary>
        public DataTable Pigments => DataSet.Tables["Pigments"];


        public ColorDatabase(string connectionString, bool fillDataSet = true)
        {
            _helper = new DatabaseHelper();
            _connection = _helper.CreateConnection(connectionString);
            _materialsAdapter = _helper.CreateMaterialsAdapter(_connection);
            _colorsAdapter = _helper.CreateColorsAdpater(_connection);
            _pigmentsAdapter = _helper.CreatePigmentsAdapter(_connection);

            if (fillDataSet)
            {
                FillDataSet();
            }
        }

        private void FillDataSet()
        {
            _connection.Open();
            try
            {
                var ds = new DataSet("ColorDatabase");

                _materialsAdapter.Fill(ds, "Materials");
                _pigmentsAdapter.Fill(ds, "Pigments");
                _colorsAdapter.Fill(ds, "Colors");
                ds.Tables["Colors"].Columns.AddRange(new DataColumn[]
                {
                    new DataColumn("ScMode", typeof(int)),
                    new DataColumn("Illuminant", typeof(int)),
                    new DataColumn("Observer", typeof(int)),
                    new DataColumn("L", typeof(double)),
                    new DataColumn("a", typeof(double)),
                    new DataColumn("b", typeof(double)),
                });
                DataSet = ds;
            }
            finally
            {
                _connection.Close();
            }

            UpdateAutoIncrementSeed();
        }


        public void UpdateColorsLab(SpecularComponent? scMode, StandardIlluminant illuminant, StandardObserver observer)
        {
            if (scMode == SpecularComponent.SCIAndSCE)
            {
                throw new ArgumentException("scMode can't be \"SCIAndSCE\"");
            }

            Colors.Columns["ScMode"].DefaultValue = (int?)scMode;
            Colors.Columns["Illuminant"].DefaultValue = (int)illuminant;
            Colors.Columns["Observer"].DefaultValue = (int)observer;

            var formula = new Formula(illuminant, observer);
            foreach (var dataRow in Colors.AsEnumerable())
            {
                UpdateColorRowLab(dataRow, formula, scMode);
            }
            Colors.AcceptChanges();
        }

        private static void UpdateColorRowLab(DataRow dataRow, Formula formula, SpecularComponent? scMode)
        {
            var clrs = ToColorData(dataRow["ColorData"] as byte[]);

            ColorData clr = null;
            if (scMode == null || clrs[(int)(SpecularComponent)scMode] == null)
            {
                scMode = clrs[0] != null
                    ? SpecularComponent.Unknown
                    : (clrs[1] != null ? SpecularComponent.SCI : SpecularComponent.SCE);
            }
            dataRow["ScMode"] = (int)scMode;
            clr = clrs[(int)(SpecularComponent)scMode];

            CIELAB lab = clr.Lab(formula);
            if (lab == null)
            {
                formula = clr.Formulas[0];
                lab = clr.Lab(formula);
            }

            dataRow["Illuminant"] = (int)formula.Illuminant;
            dataRow["Observer"] = (int)formula.Observer;
            dataRow["L"] = lab?.L;
            dataRow["a"] = lab?.a;
            dataRow["b"] = lab?.b;
        }

        private void UpdateAutoIncrementSeed()
        {
            var keyColumn = Materials.Columns["Id"];
            keyColumn.AutoIncrement = true;
            keyColumn.AutoIncrementStep = 1;
            keyColumn.AutoIncrementSeed = Materials.Rows.Count > 0
                ? Materials.AsEnumerable().Max(row => row.Field<Int64>("Id")) + 1
                : 1;
            Materials.PrimaryKey = new[] { keyColumn };

            keyColumn = Pigments.Columns["Id"];
            keyColumn.AutoIncrement = true;
            keyColumn.AutoIncrementStep = 1;
            keyColumn.AutoIncrementSeed = Pigments.Rows.Count > 0
                ? Pigments.AsEnumerable().Max(row => row.Field<Int64>("Id")) + 1
                : 1;
            Pigments.PrimaryKey = new[] { keyColumn };

            keyColumn = Colors.Columns["Id"];
            keyColumn.AutoIncrement = true;
            keyColumn.AutoIncrementStep = 1;
            keyColumn.AutoIncrementSeed = Colors.Rows.Count > 0
                ? Colors.AsEnumerable().Max(row => row.Field<Int64>("id")) + 1
                : 1;
            Colors.PrimaryKey = new[] { keyColumn };
        }

        /// <summary>
        /// 将数据集更改写表数据库
        /// </summary>
        public void Update()
        {
            _connection.Open();
            try
            {
                using (var trans = _connection.BeginTransaction())
                {
                    _materialsAdapter.UpdateCommand.Transaction = trans;
                    _materialsAdapter.InsertCommand.Transaction = trans;
                    _materialsAdapter.DeleteCommand.Transaction = trans;
                    _materialsAdapter.Update(DataSet);
                    _pigmentsAdapter.UpdateCommand.Transaction = trans;
                    _pigmentsAdapter.InsertCommand.Transaction = trans;
                    _pigmentsAdapter.DeleteCommand.Transaction = trans;
                    _pigmentsAdapter.Update(DataSet);
                    _colorsAdapter.UpdateCommand.Transaction = trans;
                    _colorsAdapter.InsertCommand.Transaction = trans;
                    _colorsAdapter.DeleteCommand.Transaction = trans;
                    _colorsAdapter.Update(DataSet);
                    trans.Commit();
                }
            }
            finally
            {
                _connection.Close();
            }
            UpdateAutoIncrementSeed();
        }

        /// <summary>
        /// 将数据集更改写表数据库
        /// </summary>
        public void Update(DataTable table)
        {
            _connection.Open();
            try
            {
                using (var trans = _connection.BeginTransaction())
                {
                    DbDataAdapter adapter;
                    switch (table.TableName)
                    {
                        case nameof(Colors):
                            adapter = _colorsAdapter;
                            break;
                        case nameof(Materials):
                            adapter = _materialsAdapter;
                            break;
                        case nameof(Pigments):
                            adapter = _pigmentsAdapter;
                            break;
                        default: throw new ArgumentException($"Unknown table {table.TableName}");
                    }

                    adapter.UpdateCommand.Transaction = trans;
                    adapter.InsertCommand.Transaction = trans;
                    adapter.DeleteCommand.Transaction = trans;

                    adapter.Update(table);

                    trans.Commit();
                }
            }
            finally
            {
                _connection.Close();
            }

        }

        private static byte[] ToBytes(SpecularComponent sc, ColorData clr)
        {
            Trace.Assert(sc != SpecularComponent.SCIAndSCE);
            List<byte> bytes;
            if (clr.Spectrum != null)
            {
                bytes = new List<byte>(8 + clr.Spectrum.Data.Length * sizeof(float));
                bytes.Add((byte)sc);
                bytes.Add(SPECTRAL_DATA_FLAG);
                var spectrum = clr.Spectrum;
                bytes.AddRange(BitConverter.GetBytes((short)spectrum.Range.Min));
                bytes.AddRange(BitConverter.GetBytes((short)spectrum.Range.Step));
                bytes.AddRange(BitConverter.GetBytes((short)spectrum.Data.Length));
                foreach (var v in spectrum.Data)
                {
                    bytes.AddRange(BitConverter.GetBytes((float)v));
                }
            }
            else
            {
                bytes = new List<byte>(4 + 3 * sizeof(float));
                bytes.Add((byte)sc);
                var formula = clr.Formulas[0];
                bytes.Add(XYZ_DATA_FLAG);
                bytes.Add((byte)formula.Illuminant);
                bytes.Add((byte)formula.Observer);
                var xyz = clr.XYZ(formula);
                bytes.AddRange(BitConverter.GetBytes((float)xyz.X));
                bytes.AddRange(BitConverter.GetBytes((float)xyz.Y));
                bytes.AddRange(BitConverter.GetBytes((float)xyz.Z));
            }

            return bytes.ToArray();
        }

        /// <summary>
        /// 将颜数据转换为字节数组，该数组存储在Colors的ColorData中，其中SpecularComponent设为Unknown
        /// </summary>
        /// <param name="clr"></param>
        /// <returns></returns>
        public static byte[] ToBytes(ColorData clr)
        {
            return ToBytes(SpecularComponent.Unknown, clr);
        }

        /// <summary>
        /// 将颜色数据转换为字节数组，该数组用于存储到Colors表的ColorData字段
        /// 其中SpecularComponent视sci或sce是否为null设为SCI、SCE或I+E
        /// </summary>
        /// <param name="sci">SCI数据</param>
        /// <param name="sce">SCE数据</param>
        /// <returns></returns>
        public static byte[] ToBytes(ColorData sci, ColorData sce)
        {
            Trace.Assert(sci != null || sce != null);


            if (sci != null && sce != null)
            {
                Trace.Assert((sci.Spectrum != null && sce.Spectrum != null) ||
                             (sci.Spectrum == null && sce.Spectrum == null));

                List<byte> bytes;
                if (sci.Spectrum != null && sce.Spectrum != null)
                {
                    Trace.Assert(sci.Spectrum.Range == sce.Spectrum.Range);
                    var range = sci.Spectrum.Range;

                    bytes = new List<byte>(8 + sizeof(float) * range.Count * 2);
                    bytes.Add((byte)SpecularComponent.SCIAndSCE);
                    bytes.Add(SPECTRAL_DATA_FLAG);
                    bytes.AddRange(BitConverter.GetBytes((short)range.Min));
                    bytes.AddRange(BitConverter.GetBytes((short)range.Step));
                    bytes.AddRange(BitConverter.GetBytes((short)range.Count));
                    foreach (var v in sci.Spectrum.Data)
                    {
                        bytes.AddRange(BitConverter.GetBytes((float)v));
                    }

                    foreach (var v in sce.Spectrum.Data)
                    {
                        bytes.AddRange(BitConverter.GetBytes((float)v));
                    }
                }
                else if (sci.Spectrum == null && sce.Spectrum == null)
                {
                    Trace.Assert(sci.Formulas[0].Illuminant == sce.Formulas[0].Illuminant &&
                                 sci.Formulas[0].Observer == sce.Formulas[0].Observer);
                    var formula = sci.Formulas[0];
                    bytes = new List<byte>(4 + sizeof(float) * 6);
                    bytes.Add((byte)SpecularComponent.SCIAndSCE);
                    bytes.Add(XYZ_DATA_FLAG);
                    bytes.Add((byte)formula.Illuminant);
                    bytes.Add((byte)formula.Observer);
                    var xyz = sci.XYZ(formula);
                    bytes.AddRange(BitConverter.GetBytes((float)xyz.X));
                    bytes.AddRange(BitConverter.GetBytes((float)xyz.Y));
                    bytes.AddRange(BitConverter.GetBytes((float)xyz.Z));
                    xyz = sce.XYZ(formula);
                    bytes.AddRange(BitConverter.GetBytes((float)xyz.X));
                    bytes.AddRange(BitConverter.GetBytes((float)xyz.Y));
                    bytes.AddRange(BitConverter.GetBytes((float)xyz.Z));
                }
                else
                {
                    throw new ArgumentException("sci and sce must be have the save color type.");
                }

                return bytes.ToArray();
            }
            else
            {
                SpecularComponent sc = sci != null ? SpecularComponent.SCI : SpecularComponent.SCE;
                ColorData clr = sci ?? sce;

                return ToBytes(sc, clr);
            }

            throw new ArgumentException();
        }

        // 将由ToBytes转换为字节数组转换回ColorData
        // 视其为SpecularComponent，反回的数组中索引为Unknown处存储Clr数据，SCI处存储SCI数据，SCE处存储SCE数据
        public static ColorData[] ToColorData(byte[] bytes)
        {
            ColorData[] clrs = new ColorData[3];

            SpecularComponent scMode = (SpecularComponent)bytes[0];
            bool isSpectralData = bytes[1] == SPECTRAL_DATA_FLAG;
            ColorData clr1 = null;
            ColorData clr2 = null;

            if (isSpectralData)
            {
                int start = BitConverter.ToInt16(bytes, 2);
                int interval = BitConverter.ToInt16(bytes, 4);
                int count = BitConverter.ToInt16(bytes, 6);
                var range = new WavelengthRange(start, start + (count - 1) * interval, interval);

                double[] data = new double[count];
                for (int i = 0; i < count; i++)
                {
                    data[i] = BitConverter.ToSingle(bytes, 8 + i * sizeof(float));
                }

                clr1 = new ColorData(new Spectrum(range, data));

                if (scMode == SpecularComponent.SCIAndSCE)
                {
                    data = new double[count];
                    for (int i = 0; i < count; i++)
                    {
                        data[i] = BitConverter.ToSingle(bytes, 8 + (count + i) * sizeof(float));
                    }
                    clr2 = new ColorData(new Spectrum(range, data));
                }
            }
            else
            {
                var illuminant = (StandardIlluminant)bytes[2];
                var observer = (StandardObserver)bytes[3];
                clr1 = new ColorData();
                clr1.Set(observer, illuminant, new CIEXYZ(
                    BitConverter.ToSingle(bytes, 4),
                    BitConverter.ToSingle(bytes, 8),
                    BitConverter.ToSingle(bytes, 12)));
                if (scMode == SpecularComponent.SCIAndSCE)
                {
                    clr2 = new ColorData();
                    clr2.Set(observer, illuminant,
                        new CIEXYZ(
                            BitConverter.ToSingle(bytes, 16),
                            BitConverter.ToSingle(bytes, 20),
                            BitConverter.ToSingle(bytes, 24)));
                }
            }

            if (scMode == SpecularComponent.Unknown)
            {
                clrs[(int)SpecularComponent.Unknown] = clr1;
            }
            else if (scMode == SpecularComponent.SCI)
            {
                clrs[(int)SpecularComponent.SCI] = clr1;
            }
            else if (scMode == SpecularComponent.SCE)
            {
                clrs[(int)SpecularComponent.SCE] = clr1;
            }
            else if (scMode == SpecularComponent.SCIAndSCE)
            {
                clrs[(int)SpecularComponent.SCI] = clr1;
                clrs[(int)SpecularComponent.SCE] = clr2;
            }

            return clrs;
        }

        /// <summary>
        /// 将Colors表的一行转换为Specimen对象
        /// </summary>
        /// <param name="colorRow">Colors表的一行</param>
        public static Specimen ToSpecimen(DataRow colorRow)
        {
            var clrs = ToColorData(colorRow["ColorData"] as byte[]);
            return new Specimen
            {
                ID = (int)colorRow.Field<long>("Id"),
                Name = colorRow.Field<string>("Number"),
                DateTime = colorRow.Field<DateTime>("DateTime"),
                Material = colorRow.Field<string>("Material"),
                Instrument = colorRow.Field<string>("Instrument"),
                IsTransmission = colorRow.Field<bool>("IsTransmission"),
                Clr = clrs[(int)SpecularComponent.Unknown],
                SCI = clrs[(int)SpecularComponent.SCI],
                SCE = clrs[(int)SpecularComponent.SCE],
                ColorFormula = colorRow.Field<string>("ColorFormula"),
                Remark = colorRow.Field<string>("Remark")
            };
        }

        public DataRow ToColorRow(Specimen specimen,bool isQuery=false)
        {
            var colorRow = Colors.NewRow();
            UpdateColorRow(colorRow, specimen, isQuery);
            return colorRow;
        }

        public static void UpdateColorRow(DataRow colorRow, Specimen specimen,bool isModify=false)
        {
            if (isModify)
            {
                colorRow["ID"] = specimen.ID;
            }
         
            if (!string.IsNullOrEmpty(specimen.Name))
            {
                colorRow["Number"] = specimen.Name;
            }
            else if (string.IsNullOrEmpty(colorRow["Number"] as string))
            {
                colorRow["Number"] = "N" + colorRow.Field<long>("Id").ToString("D8");
            }

            colorRow["Remark"] = specimen.Remark;
            colorRow["DateTime"] = specimen.DateTime;
            colorRow["Material"] = specimen.Material;
            colorRow["Instrument"] = specimen.Instrument;
            colorRow["Conditions"] = GetConditions(specimen);
            colorRow["IsTransmission"] = specimen.IsTransmission;
            colorRow["ColorData"] = specimen.SpecularComponent == SpecularComponent.Unknown
                ? ToBytes(specimen.Clr)
                : ToBytes(specimen.SCI, specimen.SCE);
            colorRow["ColorFormula"] = specimen.ColorFormula;
            var formula = new Formula((StandardIlluminant)colorRow.Field<int>("Illuminant"),
                (StandardObserver)colorRow.Field<int>("Observer"));
            UpdateColorRowLab(colorRow, formula, (SpecularComponent?)colorRow.Field<int?>("ScMode"));
        }

        static string GetConditions(Specimen specimen)
        {
            return string.Join(", ", new string[]
            {
                specimen.OpticalStruct, specimen.UV.ToString(), specimen.MeasurementCaliber.ToString()
            });
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _connection?.Dispose();
                _colorsAdapter?.Dispose();
                _pigmentsAdapter?.Dispose();
                _materialsAdapter?.Dispose();
                DataSet?.Dispose();
                DataSet = null;
                _isDisposed = true;
            }
        }

        public void InsertIntoColors(IEnumerable<Specimen> colors)
        {
            _connection.Open();
            try
            {
                using (var trans = _connection.BeginTransaction())
                {
                    using (var command = _connection.CreateCommand())
                    {
                        command.CommandText = _helper.GetInsertIntoColorsSql();
                        command.Parameters.AddRange(_helper.GetColorsParameters());

                        foreach (var color in colors)
                        {
                            command.Parameters["@number"].Value = color.Name;
                            command.Parameters["@remark"].Value = color.Remark;
                            command.Parameters["@date"].Value = color.DateTime;
                            command.Parameters["@material"].Value = color.Material ?? (object)DBNull.Value;
                            command.Parameters["@instrument"].Value = color.Instrument ?? (object)DBNull.Value;
                            command.Parameters["@conditions"].Value = GetConditions(color);
                            command.Parameters["@isTrans"].Value = color.IsTransmission;
                            command.Parameters["@colorData"].Value =
                                color.SpecularComponent == SpecularComponent.Unknown
                                    ? ToBytes(color.Clr)
                                    : ToBytes(color.SCI, color.SCE);
                            command.Parameters["@formula"].Value = color.ColorFormula ?? (object)DBNull.Value;

                            command.ExecuteNonQuery();
                        }
                    }

                    trans.Commit();
                }
            }
            finally
            {
                _connection.Close();
            }
        }

        public void DeleteFromColors(IEnumerable<int> ids)
        {
            _connection.Open();
            try
            {
                using (var trans = _connection.BeginTransaction())
                {
                    using (var command = _connection.CreateCommand())
                    {
                        command.CommandText = _helper.GetDeleteFromColorsSql();
                        command.Parameters.AddRange(_helper.GetColorsParameters());

                        foreach (var id in ids)
                        {
                            command.Parameters["@id"].Value = id;
                            command.ExecuteNonQuery();
                        }
                    }

                    trans.Commit();
                }
            }
            finally
            {
                _connection.Close();
            }
        }

        public void UpdateColors(IEnumerable<Specimen> colors)
        {
            _connection.Open();
            try
            {
                using (var trans = _connection.BeginTransaction())
                {
                    using (var command = _connection.CreateCommand())
                    {
                        command.CommandText = _helper.GetUpdateColorsSql();
                        command.Parameters.AddRange(_helper.GetColorsParameters());

                        foreach (var color in colors)
                        {
                            command.Parameters["@id"].Value = color.ID;
                            command.Parameters["@number"].Value = color.Name;
                            command.Parameters["@remark"].Value = color.Remark;
                            command.Parameters["@date"].Value = color.DateTime;
                            command.Parameters["@material"].Value = color.Material ?? (object)DBNull.Value;
                            command.Parameters["@instrument"].Value = color.Instrument ?? (object)DBNull.Value;
                            command.Parameters["@conditions"].Value = GetConditions(color);
                            command.Parameters["@isTrans"].Value = color.IsTransmission;
                            command.Parameters["@colorData"].Value =
                                color.SpecularComponent == SpecularComponent.Unknown
                                    ? ToBytes(color.Clr)
                                    : ToBytes(color.SCI, color.SCE);
                            command.Parameters["@formula"].Value = color.ColorFormula ?? (object)DBNull.Value;
                            command.ExecuteNonQuery();
                        }
                    }
                    trans.Commit();
                }
            }
            catch(Exception ex)
            {
                var info = ex.Message;
            
            }
            finally
            {
                _connection.Close();
            }
        }

        public List<Specimen> SelectAllColors(Predicate<Specimen> predicate = null)
        {
            _connection.Open();
            List<Specimen> colors = new List<Specimen>();
            try
            {
                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = _helper.GetSelectFromColorsSql();
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var clrs = ToColorData(reader.GetValue(8) as byte[]);
                            var color = new Specimen
                            {
                                ID = reader.GetInt32(0),
                                Name = reader.GetString(1),
                                Remark = reader.IsDBNull(2) ? null : reader.GetString(2),
                                DateTime = reader.GetDateTime(3),
                                Material = reader.IsDBNull(4) ? null : reader.GetString(4),
                                Instrument = reader.IsDBNull(5) ? null : reader.GetString(5),
                                //Conditions = reader.IsDBNull(6) ? null : reader.GetString(6),
                                Clr = clrs[0],
                                SCI = clrs[1],
                                SCE = clrs[2],
                                IsTransmission = reader.GetBoolean(7),
                                ColorFormula = reader.IsDBNull(9) ? null : reader.GetString(9)
                            };

                            if (predicate == null || predicate(color))
                            {
                                colors.Add(color);
                            }
                        }
                    }
                }
            }
            finally
            {
                _connection.Close();
            }

            return colors;
        }

        public DataTable Find(string name, DateTime? dtBegin, DateTime? dtEnd,bool isQuery=false)
        {
            _connection.Open();
            List<DataRow> rows = new List<DataRow>();
            try
            {
                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = _helper.GetFindFromColorsSql(name, dtBegin, dtEnd);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var clrs = ToColorData(reader.GetValue(8) as byte[]);
                            var color = new Specimen
                            {
                                ID = reader.GetInt32(0),
                                Name = reader.GetString(1),
                                Remark = reader.IsDBNull(2) ? null : reader.GetString(2),
                                DateTime = reader.GetDateTime(3),
                                Material = reader.IsDBNull(4) ? null : reader.GetString(4),
                                Instrument = reader.IsDBNull(5) ? null : reader.GetString(5),
                                //Conditions = reader.IsDBNull(6) ? null : reader.GetString(6),
                                Clr = clrs[0],
                                SCI = clrs[1],
                                SCE = clrs[2],
                                IsTransmission = reader.GetBoolean(7),
                                ColorFormula = reader.IsDBNull(9) ? null : reader.GetString(9)
                            };
                            rows.Add(ToColorRow(color,isQuery));
                        }
                    }
                }
            }

            finally
            {
                _connection.Close();
            }
            if (rows.Count == 0) return null;
            return rows.CopyToDataTable();
        }
    }
}
