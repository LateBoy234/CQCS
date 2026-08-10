using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using ThreeNH.ColorSpecimen;
using ThreeNH.Optical;

namespace ThreeNH.ColorLibrary
{
    public class Database : IDisposable
    {
        private DatabaseHelper _helper;
        private DbConnection _connection;

        public Database(string connectionString)
        {
            _helper = new DatabaseHelper();
            _connection = _helper.CreateConnection(connectionString);
        }

        public Material[] Materials
        {
            get
            {
                _connection.Open();
                List<Material> materials = new List<Material>();
                try
                {
                    using (var command = _connection.CreateCommand())
                    {
                        command.CommandText = @"SELECT [MaterialId], [MaterialName], [DateTime]," +
                                              "[Remark] FROM [Materials]";
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                materials.Add(new Material
                                {
                                    Id = reader.GetInt32(0),
                                    Name = reader.GetString(1),
                                    DateTime = reader.GetDateTime(2),
                                    Remark = reader.IsDBNull(5) ? null : reader.GetString(5)
                                });
                            }
                        }
                    }
                }
                finally
                {
                    _connection.Close();
                }

                return materials.ToArray();
            }
        }

        public Pigment[] Pigments
        {
            get
            {
                _connection.Open();
                List<Pigment> pigments = new List<Pigment>();
                try
                {
                    using (var command = _connection.CreateCommand())
                    {
                        command.CommandText = @"SELECT [MaterialId], [MaterialName], [DateTime]," +
                                              "[Remark] FROM [Materials]";
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                pigments.Add(new Pigment
                                {
                                    Id = reader.GetInt32(0),
                                    Name = reader.GetString(1),
                                    DateTime = reader.GetDateTime(2),
                                    Remark = reader.IsDBNull(5) ? null : reader.GetString(5)
                                });
                            }
                        }
                    }
                }
                finally
                {
                    _connection.Close();
                }

                return pigments.ToArray();
            }
        }

        /// <summary>
        /// 更新给定的颜色，颜色的ID必须设置
        /// </summary>
        /// <param name="expectedColors"></param>
        public void UpdateTargetColorsById(IEnumerable<TargetColor> expectedColors)
        {
            try
            {
                
                using (var command = _connection.CreateCommand())
                {
                    _connection.Open();
                    command.CommandText = "UPDATE [Colors] SET " +
                                          "[ColorNumber]=@number, [Remark]=@name, [DateTime]=@date, " +
                                          "[MaterialName]=@material, [Instrument]=@instrument, " +
                                          "[Conditions]=@conditions, [IsTransmission]=@isTrans, " +
                                          "[ColorData]=@color, [ColorFormula]=@formula " +
                                          "WHERE [ColorId]=@id";

                    command.Parameters.AddRange(_helper.GetColorsParameters());

                    using (var trans = _connection.BeginTransaction())
                    {
                        foreach (var color in expectedColors)
                        {
                            command.Parameters["@id"].Value = color.Id;
                            command.Parameters["@number"].Value = color.Number;
                            command.Parameters["@name"].Value = color.Name ?? (object) DBNull.Value;
                            command.Parameters["@date"].Value = color.DateTime;
                            command.Parameters["@material"].Value = color.Material ?? (object) DBNull.Value;
                            command.Parameters["@instrument"].Value = color.Instrument ?? (object) DBNull.Value;
                            command.Parameters["@conditions"].Value = color.Conditions ?? (object) DBNull.Value;
                            command.Parameters["@isTrans"].Value = color.IsTransmission;
                            command.Parameters["@color"].Value = color.ColorDataBytes;
                            command.Parameters["@formula"].Value = color.ColorFormula ?? (object) DBNull.Value;

                            command.ExecuteNonQuery();
                        }
                        trans.Commit();
                    }
                }
            }
            finally
            {
                _connection.Close();
            }
        }

        /// <summary>
        /// 通过颜色的编号更新相应颜色
        /// </summary>
        /// <param name="expectedColors"></param>
        public void UpdateTargetColorsByNumber(IEnumerable<TargetColor> expectedColors)
        {
            try
            {
                
                using (var command = _connection.CreateCommand())
                {
                    _connection.Open();
                    command.CommandText = "UPDATE [Colors] SET " +
                                          "[Remark]=@name, [DateTime]=@date, " +
                                          "[MaterialName]=@material, [Instrument]=@instrument, " +
                                          "[Conditions]=@conditions, [IsTransmission]=@isTrans, " +
                                          "[ColorData]=@color, [ColorFormula]=@formula " +
                                          "WHERE [ColorNumber]=@number";

                    command.Parameters.AddRange(_helper.GetColorsParameters());

                    using (var trans = _connection.BeginTransaction())
                    {
                        foreach (var color in expectedColors)
                        {
                            command.Parameters["@number"].Value = color.Number;
                            command.Parameters["@name"].Value = color.Name ?? (object) DBNull.Value;
                            command.Parameters["@date"].Value = color.DateTime;
                            command.Parameters["@material"].Value = color.Material ?? (object) DBNull.Value;
                            command.Parameters["@instrument"].Value = color.Instrument ?? (object) DBNull.Value;
                            command.Parameters["@conditions"].Value = color.Conditions ?? (object) DBNull.Value;
                            command.Parameters["@isTrans"].Value = color.IsTransmission;
                            command.Parameters["@color"].Value = color.ColorDataBytes;
                            command.Parameters["@formula"].Value = color.ColorFormula ?? (object) DBNull.Value;

                            command.ExecuteNonQuery();
                        }
                        trans.Commit();
                    }
                }
            }
            finally
            {
                _connection.Close();
            }
        }

        /// <summary>
        /// 创建一个临时表
        /// </summary>
        /// <param name="illuminant"></param>
        /// <param name="observer"></param>
        /// <returns></returns>
        public List<TargetColor> GetTargetColors(StandardIlluminant illuminant, StandardObserver observer)
        {
            _connection.Open();
            List<TargetColor> colors = new List<TargetColor>();
            try
            {
                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = "SELECT [ColorId], [ColorNumber], [Remark], [DateTime], " +
                                          "[MaterialName], [Instrument], [Conditions], [IsTransmission], " +
                                          "[ColorData], [ColorFormula] FROM [Colors]";
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var color =new TargetColor
                            {
                                Id = reader.GetInt32(0),
                                Number = reader.GetString(1),
                                Name = reader.IsDBNull(2) ? null : reader.GetString(2),
                                DateTime = reader.GetDateTime(3),
                                Material = reader.IsDBNull(4) ? null : reader.GetString(4),
                                Instrument = reader.IsDBNull(5) ? null : reader.GetString(5),
                                Conditions = reader.IsDBNull(6) ? null : reader.GetString(6),
                                IsTransmission = reader.GetBoolean(7),
                                ColorFormula = reader.IsDBNull(9) ? null : reader.GetString(10)
                            };
  
                            color.SetColorDataBytes(reader.GetValue(8) as byte[]);
                            color.SetIlluminantObserver(illuminant, observer);
                            colors.Add(color);
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

        public void InsertTargetColors(IEnumerable<TargetColor> colors)
        {
            _connection.Open();
            try
            {
                using (var trans = _connection.BeginTransaction())
                {
                    using (var command = _connection.CreateCommand())
                    {
                        command.CommandText = @"INSERT INTO [Colors](
[ColorNumber], [Remark], [DateTime], [MaterialName], [Instrument],
[Conditions], [IsTransmission], [ColorData], [ColorFormula])
VALUES(@number, @name, @date, @material, @instrument, @conditions, @isTrans, @colorData, @formula)";
                        
                        command.Parameters.AddRange(_helper.GetColorsParameters());

                        foreach (var color in colors)
                        {
                            command.Parameters["@number"].Value = color.Number;
                            command.Parameters["@name"].Value = color.Name ?? (object)DBNull.Value;
                            command.Parameters["@date"].Value = color.DateTime;
                            command.Parameters["@material"].Value = color.Material ?? (object)DBNull.Value;
                            command.Parameters["@instrument"].Value = color.Instrument ?? (object)DBNull.Value;
                            command.Parameters["@conditions"].Value = color.Conditions ?? (object)DBNull.Value;
                            command.Parameters["@isTrans"].Value = color.IsTransmission;
                            command.Parameters["@colorData"].Value = color.ColorDataBytes;
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

        public void InsertTargetColors(IEnumerable<ColorSpecimen.Specimen> specimens)
        {
            InsertTargetColors(from s in specimens select TargetColor.FromSpecimen(s));
        }

        public void InsertMaterials(IEnumerable<Material> materials)
        {
             _connection.Open();
            try
            {
                using (var trans = _connection.BeginTransaction())
                {
                    using (var command = _connection.CreateCommand())
                    {
                        command.CommandText = @"INSERT INTO [Materials](
[MaterialName], [DateTime], [Remark]) VALUES(@name, @date,  @remark)";
                        command.Parameters.AddRange(_helper.GetMaterialsParameters());


                        foreach (var material in materials)
                        {
                            command.Parameters["@name"].Value = material.Name;
                            command.Parameters["@date"].Value = material.DateTime;
                            command.Parameters["@remark"].Value = material.Remark ?? (object)DBNull.Value;

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

        public void InsertPigments(IEnumerable<Pigment> pigments)
        {
            _connection.Open();
            try
            {
                using (var trans = _connection.BeginTransaction())
                {
                    using (var command = _connection.CreateCommand())
                    {
                        command.CommandText = @"INSERT INTO [Pigments](
[PigmentName], [DateTime], [Remark]) VALUES(@name, @date, @remark)";
                        command.Parameters.AddRange(_helper.GetPigmentsParameters());


                        foreach (var pigment in pigments)
                        {
                            command.Parameters["@name"].Value = pigment.Name;
                            command.Parameters["@date"].Value = pigment.DateTime;
                            command.Parameters["@remark"].Value = pigment.Remark ?? (object)DBNull.Value;

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


        public void Dispose()
        {
            _connection?.Dispose();
        }

        // 更新同名
        public void UpdateTargetColors(IEnumerable<ColorSpecimen.Specimen> specimens)
        {
            var colors = from _ in specimens select TargetColor.FromSpecimen(_);
            UpdateTargetColorsByNumber(colors);
        }
    }
}
