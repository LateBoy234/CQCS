using System;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;

namespace ThreeNH.ColorLibrary
{
    class DatabaseHelper
    {
        private const string SQLITE_INIT_COMMANDS = @"
CREATE TABLE IF NOT EXISTS [Materials](
[MaterialId] INTEGER PRIMARY KEY AUTOINCREMENT,
[MaterialName] VARCHAR(64) NOT NULL UNIQUE,   /* 原料名 */
[DateTime] DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP, /* 插入时间 */
[Remark] TEXT                   /* 备注信息 */
);

CREATE TABLE IF NOT EXISTS [Pigments](
[PigmentId] INTEGER PRIMARY KEY AUTOINCREMENT,
[PigmentName] VARCHAR(64) NOT NULL UNIQUE,   /* 原料名 */
[DateTime] DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP, /* 插入时间 */
[Remark] TEXT                   /* 备注信息 */
);

CREATE TABLE IF NOT EXISTS [Colors](
[ColorId] INTEGER PRIMARY KEY AUTOINCREMENT,
[ColorNumber] VARCHAR(32) NOT NULL UNIQUE, /* 编码 */
[Remark] VARCHAR(64),                   /* 颜色名 */
[DateTime] DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP, /* 日期 */
[MaterialName] VARCHAR(64),           /* 料号 */
[Instrument] VARCHAR(64),             /* 测试仪器 */
[Conditions] VARCHAR(64),             /* 测试条件 */
[IsTransmission] BOOLEAN DEFAULT 0,   /* 是否透射 */
 /*
第一个字节为SCMode（0表示Other，1表示SCI，2表示SCE，3表示I+E），
第二个字节为颜色数据类型（0表示反射率，1表示XYZ）
如果是反射率，接着是光谱数据，否则是XYZ数据
光谱数据格式为：
    {Interval: byte, Start: short, Count: short, data: float[Count][n]}
XYZ格式：
    {Illuminant: byte, Observer: byte, XYZ[n]}
其中n = 2，当SCMode为I+E，
n = 1，当SCMode为其他莫式
 */
[ColorData] BLOB,
[ColorFormula] TEXT   /* 配方 */
);

";

        internal DbDataAdapter CreateColorsAdpater(DbConnection connection)
        {
            return CreateColorsSqliteAdapter(connection as SQLiteConnection);
        }

        internal DbDataAdapter CreatePigmentsAdapter(DbConnection connection)
        {
            return CreatePigmentsSqliteAdapter(connection as SQLiteConnection);
        }


        internal DbDataAdapter CreateMaterialsAdapter(DbConnection connection)
        {
            return CreateMaterialsSqliteAdapter(connection as SQLiteConnection);
        }

        private SQLiteDataAdapter CreateColorsSqliteAdapter(SQLiteConnection connection)
        {
            var idParameter = new SQLiteParameter("@id", DbType.Int32, "ColorId");
            var numberParameter = new SQLiteParameter("@number", DbType.String, "ColorNumber");
            var nameParameter = new SQLiteParameter("@remark", DbType.String, "Remark");
            var dateParameter = new SQLiteParameter("@date", DbType.DateTime, "DateTime");
            var materialParameter = new SQLiteParameter("@material", DbType.String, "MaterialName");
            var instrumentParameter = new SQLiteParameter("@instrument", DbType.String, "Instrument");
            var conditionsParameter = new SQLiteParameter("@conditions", DbType.String, "Conditions");
            var isTransParameter = new SQLiteParameter("@isTrans", DbType.Boolean, "IsTransmission");
            var dataParameter = new SQLiteParameter("@colorData", DbType.Binary, "ColorData");
            var formulaParameter = new SQLiteParameter("@formula", DbType.String, "ColorFormula");


            var selectCommand = connection.CreateCommand();
            selectCommand.CommandText = GetSelectFromColorsSql();

            var insertCommand = connection.CreateCommand();
            insertCommand.CommandText = GetInsertIntoColorsSql();
            insertCommand.Parameters.Add(numberParameter);
            insertCommand.Parameters.Add(nameParameter);
            insertCommand.Parameters.Add(dateParameter);
            insertCommand.Parameters.Add(materialParameter);
            insertCommand.Parameters.Add(instrumentParameter);
            insertCommand.Parameters.Add(conditionsParameter);
            insertCommand.Parameters.Add(isTransParameter);
            insertCommand.Parameters.Add(dataParameter);
            insertCommand.Parameters.Add(formulaParameter);

            var updateCommand = connection.CreateCommand();
            updateCommand.CommandText = GetUpdateColorsSql();
            updateCommand.Parameters.Add(idParameter);
            updateCommand.Parameters.Add(numberParameter);
            updateCommand.Parameters.Add(nameParameter);
            updateCommand.Parameters.Add(dateParameter);
            updateCommand.Parameters.Add(materialParameter);
            updateCommand.Parameters.Add(instrumentParameter);
            updateCommand.Parameters.Add(conditionsParameter);
            updateCommand.Parameters.Add(isTransParameter);
            updateCommand.Parameters.Add(dataParameter);
            updateCommand.Parameters.Add(formulaParameter);

            var deleteCommand = connection.CreateCommand();
            deleteCommand.CommandText = GetDeleteFromColorsSql();
            deleteCommand.Parameters.Add(idParameter);



            var mapping = new DataTableMapping("Colors", "Colors");
            mapping.ColumnMappings.AddRange(new DataColumnMapping[]
            {
                new DataColumnMapping("ColorId", "Id"),
                new DataColumnMapping("ColorNumber", "Number"),
                new DataColumnMapping("Remark", "Remark"),
                new DataColumnMapping("MaterialName", "Material"),
                new DataColumnMapping("DateTime", "DateTime"),
                new DataColumnMapping("Instrument", "Instrument"),
                new DataColumnMapping("Conditions", "Conditions"),
                new DataColumnMapping("IsTransmission", "IsTransmission"),
                new DataColumnMapping("ColorData", "ColorData"),
                new DataColumnMapping("ColorFormula", "ColorFormula"),
            });

            var adpter = new SQLiteDataAdapter
            {
                SelectCommand = selectCommand,
                InsertCommand = insertCommand,
                UpdateCommand = updateCommand,
                DeleteCommand = deleteCommand
            };
            adpter.TableMappings.Add(mapping);

            return adpter;
        }

        private SQLiteDataAdapter CreatePigmentsSqliteAdapter(SQLiteConnection connection)
        {
            var nameParameter = new SQLiteParameter("@name", DbType.String, "PigmentName");
            var idParameter = new SQLiteParameter("@id", DbType.Int32, "PigmentId");
            var dateParameter = new SQLiteParameter("@date", DbType.DateTime, "DateTime");
            var remarkParameter = new SQLiteParameter("@remark", DbType.String, "Remark");


            var selectCommand = connection.CreateCommand();
            selectCommand.CommandText = "SELECT * FROM [Pigments]";


            var insertCommand = connection.CreateCommand();
            insertCommand.CommandText = "INSERT INTO [Pigments](" +
                                        "[PigmentName],[DateTime],[Remark]) " +
                                        "VALUES(@name, @date,  @remark)";
            insertCommand.Parameters.Add(nameParameter);
            insertCommand.Parameters.Add(dateParameter);
            insertCommand.Parameters.Add(remarkParameter);

            var updateCommand = connection.CreateCommand();
            updateCommand.CommandText = "UPDATE [Pigments] SET [PigmentName]=@name, " +
                                        "[DateTime]=@date,[Remark]=@remark " +
                                        "WHERE [PigmentId]=@id";
            updateCommand.Parameters.Add(idParameter);
            updateCommand.Parameters.Add(nameParameter);
            updateCommand.Parameters.Add(dateParameter);
            updateCommand.Parameters.Add(remarkParameter);

            var deleteCommand = connection.CreateCommand();
            deleteCommand.CommandText = "DELETE FROM [Pigments] WHERE [PigmentId]=@id";
            deleteCommand.Parameters.Add(idParameter);

            var mapping = new DataTableMapping("Pigments", "Pigments",
                new DataColumnMapping[]
                {
                    new DataColumnMapping("PigmentId", "Id"),
                    new DataColumnMapping("PigmentName", "Name"),
                    new DataColumnMapping("DateTime", "DateTime"),
                    new DataColumnMapping("Remark", "Remark"),
                });

            SQLiteDataAdapter adapter = new SQLiteDataAdapter();
            adapter.SelectCommand = selectCommand;
            adapter.InsertCommand = insertCommand;
            adapter.UpdateCommand = updateCommand;
            adapter.DeleteCommand = deleteCommand;

            adapter.TableMappings.Add(mapping);
            return adapter;
        }

        private SQLiteDataAdapter CreateMaterialsSqliteAdapter(SQLiteConnection connection)
        {

            var selectCommand = connection.CreateCommand();
            selectCommand.CommandText = "SELECT [MaterialId], [MaterialName],[DateTime],[Remark] FROM [Materials]";


            var nameParameter = new SQLiteParameter("@name", DbType.String, "MaterialName");
            var idParameter = new SQLiteParameter("@id", DbType.Int32, "MaterialId");
            var dateParameter = new SQLiteParameter("@date", DbType.DateTime, "DateTime");
            var remarkParameter = new SQLiteParameter("@remark", DbType.String, "Remark");


            var insertCommand = connection.CreateCommand();
            insertCommand.CommandText = "INSERT INTO [Materials](" +
                                        "[MaterialName],[DateTime],[Remark]) " +
                                        "VALUES(@name, @date,  @remark)";
            insertCommand.Parameters.Add(nameParameter);
            insertCommand.Parameters.Add(dateParameter);
            insertCommand.Parameters.Add(remarkParameter);

            var updateCommand = connection.CreateCommand();
            updateCommand.CommandText = "UPDATE [Materials] SET [MaterialName]=@name, " +
                                        "[DateTime]=@date,[Remark]=@remark " +
                                        "WHERE [MaterialId]=@id";
            updateCommand.Parameters.Add(idParameter);
            updateCommand.Parameters.Add(nameParameter);
            updateCommand.Parameters.Add(dateParameter);
            updateCommand.Parameters.Add(remarkParameter);

            var deleteCommand = connection.CreateCommand();
            deleteCommand.CommandText = "DELETE FROM [Materials] WHERE [MaterialId]=@id";
            deleteCommand.Parameters.Add(idParameter);

            var mapping = new DataTableMapping("Materials", "Materials",
                new DataColumnMapping[]
                {
                    new DataColumnMapping("MaterialId", "Id"),
                    new DataColumnMapping("MaterialName", "Name"),
                    new DataColumnMapping("DateTime", "DateTime"),
                    new DataColumnMapping("Remark", "Remark"),
                });

            SQLiteDataAdapter adapter = new SQLiteDataAdapter
            {
                SelectCommand = selectCommand,
                InsertCommand = insertCommand,
                UpdateCommand = updateCommand,
                DeleteCommand = deleteCommand
            };
            adapter.TableMappings.Add(mapping);
            return adapter;
        }


        internal DbConnection CreateConnection(string connectionString)
        {
            var conn = new SQLiteConnection(connectionString);

            try
            {
                using (var command = conn.CreateCommand())
                {
                    conn.Open();
                    command.CommandText = SQLITE_INIT_COMMANDS;
                    command.ExecuteNonQuery();
                }
            }
            finally
            {
                conn.Close();
            }

            return conn;
        }

        internal DbParameter[] GetColorsParameters()
        {
            return new DbParameter[]
            {
                new SQLiteParameter("@id", DbType.Int32, "ColorId"),
                new SQLiteParameter("@number", DbType.String, "ColorNumber"),
                new SQLiteParameter("@remark", DbType.String, "Remark"),
                new SQLiteParameter("@date", DbType.DateTime, "DateTime"),
                new SQLiteParameter("@material", DbType.String, "MaterialName"),
                new SQLiteParameter("@instrument", DbType.String, "Instrument"),
                new SQLiteParameter("@conditions", DbType.String, "Conditions"),
                new SQLiteParameter("@isTrans", DbType.Boolean, "IsTransmission"),
                new SQLiteParameter("@colorData", DbType.Binary, "ColorData"),
                new SQLiteParameter("@formula", DbType.String, "ColorFormula"),
            };
        }

        internal DbParameter[] GetMaterialsParameters()
        {
            return new DbParameter[]
            {
                new SQLiteParameter("@id", DbType.Int32, "MaterialId"),
                new SQLiteParameter("@name", DbType.String, "MaterialName"),
                new SQLiteParameter("@date", DbType.DateTime, "DateTime"),
                new SQLiteParameter("@remark", DbType.String, "Remark"),
            };
        }

        internal DbParameter[] GetPigmentsParameters()
        {
            return new DbParameter[]
            {
                new SQLiteParameter("@name", DbType.String, "PigmentName"),
                new SQLiteParameter("@id", DbType.Int32, "PigmentId"),
                new SQLiteParameter("@date", DbType.DateTime, "DateTime"),
                new SQLiteParameter("@remark", DbType.String, "Remark"),
            };
        }

        internal string GetUpdateColorsSql()
        {
            //return "UPDATE [Colors] SET [ColorNumber]=@number, [Remark]=@remark, " +
            //    "[DateTime]=@date, [MaterialName]=@material, [Instrument] = @instrument, " +
            //    "[Conditions]=@conditions, [IsTransmission]=@isTrans, [ColorData]=@colorData, " +
            //    "[ColorFormula]=@formula " +
            //    "WHERE [ColorId]=@id";

          return  "UPDATE Colors SET ColorNumber=@number, Remark=@remark, [DateTime]=@date, MaterialName=@material, Instrument=@instrument, Conditions=@conditions, IsTransmission=@isTrans, ColorData=@colorData, ColorFormula=@formula WHERE ColorId=@id;";



        }

        internal string GetInsertIntoColorsSql()
        {

            return "INSERT INTO [Colors]([ColorNumber], [Remark], [DateTime], [MaterialName], " +
                   "[Instrument], [Conditions], [IsTransmission], [ColorData], [ColorFormula]) " +
                   "VALUES(@number, @remark, @date, @material, @instrument, @conditions, @isTrans, " +
                   "@colorData, @formula)";
        }

        internal string GetDeleteFromColorsSql()
        {
            return "DELETE FROM [Colors] WHERE [ColorId]=@id";
        }

        internal string GetSelectFromColorsSql()
        {
            return "SELECT [ColorId], [ColorNumber], [Remark], [DateTime], " +
                "[MaterialName], [Instrument], [Conditions], [IsTransmission], " +
                "[ColorData], [ColorFormula] FROM [Colors]";
        }

        internal string GetFindFromColorsSql(string name, DateTime? dtBegin, DateTime? dtEnd)
        {
            var sql = "SELECT [ColorId], [ColorNumber], [Remark], [DateTime], " +
                "[MaterialName], [Instrument], [Conditions], [IsTransmission], " +
                "[ColorData], [ColorFormula] FROM [Colors] Where 1 = 1";
            if (!string.IsNullOrEmpty(name))
                sql += $"{Environment.NewLine}And [ColorNumber] Like'%{name}%'";
            if (dtBegin != null)
                sql += $"{Environment.NewLine}And [DateTime] >='{((DateTime)dtBegin).ToString("yyyy-MM-dd")}'";
            if (dtEnd != null)
                sql += $"{Environment.NewLine} And [DateTime] <='{((DateTime)dtEnd).AddDays(1).ToString("yyyy-MM-dd")}'";
            return sql;
        }
    }
}
