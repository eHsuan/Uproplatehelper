using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.OleDb;

namespace percentagToAmp
{
    public class OperationAccessDataBase
    {
        private static string strConn = "";
        private static OleDbConnection Conn;
        private static OleDbDataAdapter DataAdapter;
        private static OleDbCommand Command;
        private static object objTemp = new object();

        public static void OpenDataBase()
        {
            lock (OperationAccessDataBase.objTemp)
            {
                try
                {
                    string dbpath = Config.AccessDBPath;
                    OperationAccessDataBase.strConn = $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={dbpath};Jet OLEDB:Database Password = winsundms";
                    OperationAccessDataBase.Conn = new OleDbConnection(OperationAccessDataBase.strConn);
                    if (OperationAccessDataBase.Conn.State == ConnectionState.Open)
                        return;
                    OperationAccessDataBase.Conn.Open();
                    Utility.OperationAccessDataBaseFlag = true;
                }
                catch (Exception ex)
                {
                    CyntecLog.GeneralLogger.Error("OperationDataBase_OpenDataBase", ex.Message.ToString());
                    Utility.OperationAccessDataBaseFlag = false;
                }
            }
        }

        public static void CloseDataBase()
        {
            lock (OperationAccessDataBase.objTemp)
            {
                try
                {
                    if (OperationAccessDataBase.Conn.State != ConnectionState.Open)
                        return;
                    OperationAccessDataBase.Conn.Close();
                    Utility.OperationAccessDataBaseFlag = true;
                }
                catch (Exception ex)
                {
                    CyntecLog.GeneralLogger.Error("OperationDataBase_CloseDataBase", ex.Message.ToString());
                    Utility.OperationAccessDataBaseFlag = false;
                }
            }
        }

        public static DataTable ReturnDataTable(string strSqlSelect)
        {
            lock (OperationAccessDataBase.objTemp)
            {
                DataTable dataTable = (DataTable)null;
                try
                {
                    dataTable = new DataTable("TempTable");
                    new OleDbDataAdapter(strSqlSelect, OperationAccessDataBase.Conn).Fill(dataTable);
                    Utility.OperationAccessDataBaseFlag = true;
                }
                catch (Exception ex)
                {
                    CyntecLog.GeneralLogger.Error("OperationDataBase_ReturnDataTable", ex.Message.ToString());
                    Utility.OperationAccessDataBaseFlag = false;
                }
                return dataTable;
            }
        }

        public static int InsertIntoDataTable(string strSqlInsert)
        {
            lock (OperationAccessDataBase.objTemp)
            {
                int num = 0;
                try
                {
                    num = new OleDbCommand(strSqlInsert, OperationAccessDataBase.Conn).ExecuteNonQuery();
                    Utility.OperationAccessDataBaseFlag = true;
                }
                catch (Exception ex)
                {
                    CyntecLog.GeneralLogger.Error("OperationDataBase_InsertIntoDataTable", ex.Message.ToString());
                    Utility.OperationAccessDataBaseFlag = false;
                }
                return num;
            }
        }

        public static int DeleteFromDataTable(string strSqlDelete)
        {
            lock (OperationAccessDataBase.objTemp)
            {
                int num = 0;
                try
                {
                    num = new OleDbCommand(strSqlDelete, OperationAccessDataBase.Conn).ExecuteNonQuery();
                    Utility.OperationAccessDataBaseFlag = true;
                }
                catch (Exception ex)
                {
                    CyntecLog.GeneralLogger.Error("OperationDataBase_DeleteFromDataTable", ex.Message.ToString());
                    Utility.OperationAccessDataBaseFlag = false;
                }
                return num;
            }
        }

        public static int UpdateIntoDataTable(string strSqlUpdate)
        {
            lock (OperationAccessDataBase.objTemp)
            {
                int num = 0;
                try
                {
                    num = new OleDbCommand(strSqlUpdate, OperationAccessDataBase.Conn).ExecuteNonQuery();
                    Utility.OperationAccessDataBaseFlag = true;
                }
                catch (Exception ex)
                {
                    CyntecLog.GeneralLogger.Error("OperationDataBase_UpdateIntoDataTable", ex.Message.ToString());
                    Utility.OperationAccessDataBaseFlag = false;
                }
                return num;
            }
        }

        public static void DataBaseToDataSet()
        {
            lock (OperationAccessDataBase.objTemp)
            {
                try
                {
                    OperationAccessDataBase.FillDataTable("select * from ConcussMotorTimeTable order by ConcussMotorIndex ASC", "ConcussMotorTimeTable");
                    OperationAccessDataBase.FillDataTable("select * from CurrentFaultTable order by FaultDatetime DESC", "CurrentFaultTable");
                    OperationAccessDataBase.FillDataTable("select * from DosingMonitorTable order by DosingPump ASC", "DosingMonitorTable");
                    OperationAccessDataBase.FillDataTable("select * from ElectricCurrentTable order by RectifierNumer ASC", "ElectricCurrentTable");
                    OperationAccessDataBase.FillDataTable("select * from FaultMessageTable order by FaultAddress ASC", "FaultMessageTable");
                    OperationAccessDataBase.FillDataTable("select * from FBInOutTankMonitorTable order by StationNumber ASC", "FBInOutTankMonitorTable");
                    OperationAccessDataBase.FillDataTable("select * from FBNumberTable order by TankNumber ASC", "FBNumberTable");
                    OperationAccessDataBase.FillDataTable("select * from FilterMaintainTable order by FilterIndex ASC", "FilterMaintainTable");
                    OperationAccessDataBase.FillDataTable("select * from IdenticalTankUseFlagTable order by TankNumber ASC", "IdenticalTankUseFlagTable");
                    OperationAccessDataBase.FillDataTable("select * from LineStatusTable order by StationNumber ASC", "LineStatusTable");
                    OperationAccessDataBase.FillDataTable("select * from LoadSortTable order by LoadIndex ASC", "LoadSortTable");
                    OperationAccessDataBase.FillDataTable("select * from LotTable", "LotTable");
                    OperationAccessDataBase.FillDataTable("select * from MetalConsumeTable order by RectifierIndex ASC", "MetalConsumeTable");
                    OperationAccessDataBase.FillDataTable("select * from PartTable order by AddOrEditDateTime DESC", "PartTable");
                    OperationAccessDataBase.FillDataTable("select * from TemperatureTable order by StationNumber ASC", "TemperatureTable");
                    OperationAccessDataBase.FillDataTable("select * from TransporterTable order by TransporterNumber ASC", "TransporterTable");
                    OperationAccessDataBase.FillDataTable("select * from UserTable order by UserName ASC", "UserTable");
                    OperationAccessDataBase.FillDataTable("select * from WobbleCylinderTimeTable order by WobbleCylinderIndex ASC", "WobbleCylinderTimeTable");
                    OperationAccessDataBase.FillDataTable("select * from WorkingTable order by PCode ASC", "WorkingTable");
                    OperationAccessDataBase.FillDataTable("select * from ProcessEditTable order by ProcessNumber ASC", "ProcessEditTable");
                    OperationAccessDataBase.FillDataTable("select * from WaveformTable order by WaveformNumber ASC", "WaveformTable");
                    OperationAccessDataBase.FillDataTable("select * from CuPlateDataTable order by PartNumber ASC", "CuPlateDataTable");
                    OperationAccessDataBase.FillDataTable("select * from TankNameTable order by StationNumber ASC", "TankNameTable");
                    OperationAccessDataBase.FillDataTable("select * from TankSelectTable order by TankNumber ASC", "TankSelectTable");
                    OperationAccessDataBase.FillDataTable("select * from RowPositionTable", "RowPositionTable");
                    OperationAccessDataBase.FillDataTable("select * from SystemParameterTable", "SystemParameterTable");
                    OperationAccessDataBase.FillDataTable("select * from SoftwareLicenseTable", "SoftwareLicenseTable");
                }
                catch (Exception ex)
                {
                    CyntecLog.GeneralLogger.Error("OperationDataBase_DataBaseToDataSet", ex.Message.ToString());
                }
            }
        }

        public static void FillDataTable(string Sql, string TableName)
        {
            lock (OperationAccessDataBase.objTemp)
            {
                try
                {
                    //OperationAccessDataBase.Command = new OleDbCommand(Sql, OperationAccessDataBase.Conn);
                    //OperationAccessDataBase.DataAdapter = new OleDbDataAdapter();
                    //OperationAccessDataBase.DataAdapter.MissingSchemaAction = MissingSchemaAction.AddWithKey;
                    //OperationAccessDataBase.DataAdapter.SelectCommand = OperationAccessDataBase.Command;
                    //if (Variable.DmsAccessDataSet.Tables.Contains(TableName))
                    //    Variable.DmsAccessDataSet.Tables[TableName].Clear();
                    //OperationAccessDataBase.DataAdapter.FillSchema(Variable.DmsAccessDataSet, SchemaType.Source, TableName);
                    //OperationAccessDataBase.DataAdapter.Fill(Variable.DmsAccessDataSet, TableName);
                }
                catch (Exception ex)
                {
                    CyntecLog.GeneralLogger.Error("OperationDataBase_FillDataTable", ex.Message.ToString());
                }
            }
        }

        public static void DataSetUpdateToDataBase()
        {
            lock (OperationAccessDataBase.objTemp)
            {
                try
                {
                    OperationAccessDataBase.DataSetTableUpdateToDataBaseTable("select * from IdenticalTankUseFlagTable order by TankNumber ASC", "IdenticalTankUseFlagTable");
                    OperationAccessDataBase.DataSetTableUpdateToDataBaseTable("select * from LoadSortTable order by LoadIndex ASC", "LoadSortTable");
                    OperationAccessDataBase.DataSetTableUpdateToDataBaseTable("select * from LotTable", "LotTable");
                    OperationAccessDataBase.DataSetTableUpdateToDataBaseTable("select * from PartTable order by AddOrEditDateTime DESC", "PartTable");
                    OperationAccessDataBase.DataSetTableUpdateToDataBaseTable("select * from WorkingTable order by PCode ASC", "WorkingTable");
                    OperationAccessDataBase.DataSetTableUpdateToDataBaseTable("select * from ProcessEditTable order by ProcessNumber ASC", "ProcessEditTable");
                    OperationAccessDataBase.DataSetTableUpdateToDataBaseTable("select * from RowPositionTable", "RowPositionTable");
                }
                catch (Exception ex)
                {
                    CyntecLog.GeneralLogger.Error("OperationDataBase_DataSetUpdateToDataBase", ex.Message.ToString());
                }
            }
        }

        public static void DataSetUpdateToDataBase(string Sql, string TableName)
        {
            lock (OperationAccessDataBase.objTemp)
            {
                try
                {
                    OperationAccessDataBase.DataSetTableUpdateToDataBaseTable(Sql, TableName);
                }
                catch (Exception ex)
                {
                    CyntecLog.GeneralLogger.Error("OperationDataBase_DataSetUpdateToDataBase_Sql_TableName", ex.Message.ToString());
                }
            }
        }

        private static void DataSetTableUpdateToDataBaseTable(string Sql, string TableName)
        {
            lock (OperationAccessDataBase.objTemp)
            {
                try
                {
                  //  if (!Variable.DmsAccessDataSet.Tables.Contains(TableName) || Variable.DmsAccessDataSet.Tables[TableName].GetChanges() == null)
                      //  return;
                    OperationAccessDataBase.DataAdapter = new OleDbDataAdapter(Sql, OperationAccessDataBase.Conn);
                    DataSet dataSet = new DataSet();
                    OperationAccessDataBase.DataAdapter.MissingSchemaAction = MissingSchemaAction.AddWithKey;
                    if (dataSet.Tables.Contains(TableName))
                        dataSet.Tables[TableName].Clear();
                    OperationAccessDataBase.DataAdapter.FillSchema(dataSet, SchemaType.Source, TableName);
                    OleDbCommandBuilder dbCommandBuilder = new OleDbCommandBuilder(OperationAccessDataBase.DataAdapter);
                    OperationAccessDataBase.DataAdapter.UpdateCommand = dbCommandBuilder.GetUpdateCommand();
                    OperationAccessDataBase.DataAdapter.InsertCommand = dbCommandBuilder.GetInsertCommand();
                    OperationAccessDataBase.DataAdapter.DeleteCommand = dbCommandBuilder.GetDeleteCommand();
                    for (int index = 0; index < dataSet.Tables[TableName].Rows.Count; ++index)
                    {
                        if (dataSet.Tables[TableName].Rows[index].RowState != DataRowState.Deleted)
                            dataSet.Tables[TableName].Rows[index].Delete();
                    }
                    dataSet.Tables[TableName].AcceptChanges();
                    DataTable dataTable = new DataTable();
                    dataTable.Columns.Add("IndexID", typeof(long));
                    dataTable.Columns["IndexID"].AutoIncrement = true;
                    dataTable.Columns["IndexID"].AutoIncrementSeed = -1L;
                    dataTable.Columns["IndexID"].AutoIncrementStep = -1L;
                    dataTable.Columns["IndexID"].ReadOnly = true;
                    //if (Variable.DmsAccessDataSet.Tables[TableName].Columns.Count > 1)
                    //{
                    //    for (int index = 1; index < Variable.DmsAccessDataSet.Tables[TableName].Columns.Count; ++index)
                    //        dataTable.Columns.Add(Variable.DmsAccessDataSet.Tables[TableName].Columns[index].Caption, Variable.DmsAccessDataSet.Tables[TableName].Columns[index].DataType);
                    //}
                    //DataRow[] dataRowArray = Variable.DmsAccessDataSet.Tables[TableName].Select();
                    //if (dataRowArray.Length != 0)
                    //{
                    //    foreach (DataRow dataRow in dataRowArray)
                    //    {
                    //        if (dataRow.RowState != DataRowState.Deleted && Variable.DmsAccessDataSet.Tables[TableName].Columns.Count > 1)
                    //        {
                    //            DataRow row = dataTable.NewRow();
                    //            for (int columnIndex = 1; columnIndex < Variable.DmsAccessDataSet.Tables[TableName].Columns.Count; ++columnIndex)
                    //                row[columnIndex] = dataRow[columnIndex];
                    //            dataTable.Rows.Add(row);
                    //        }
                    //    }
                    //}
                    //int num = 0;
                    //foreach (DataRow row in (InternalDataCollectionBase)dataTable.Rows)
                    //{
                    //    dataSet.Tables[TableName].ImportRow(row);
                    //    ++num;
                    //}
                    //OperationAccessDataBase.DeleteFromDataTable("Delete From " + TableName);
                    //if (num > 0)
                    //    OperationAccessDataBase.DataAdapter.Update(dataSet, TableName);
                    //dataSet.Tables[TableName].AcceptChanges();
                    //Variable.DmsAccessDataSet.Tables[TableName].AcceptChanges();
                }
                catch (Exception ex)
                {
                    CyntecLog.GeneralLogger.Error($"OperationDataBase_DataSetTableUpdateToDataBaseTable({TableName})", ex.Message.ToString());
                }
            }
        }

        // 取得查詢結果（同步）
        public static DataTable QueryToDataTable(string sql, params OleDbParameter[] parameters)
        {
            var dt = new DataTable();

            // 建議：每次查詢都「開-關」連線，Access 容易被長連線鎖住
            using (var conn = new OleDbConnection(OperationAccessDataBase.strConn))
            using (var cmd = new OleDbCommand(sql, conn))
            using (var da = new OleDbDataAdapter(cmd))
            {
                if (parameters != null && parameters.Length > 0)
                    cmd.Parameters.AddRange(parameters);

                conn.Open();
                da.Fill(dt);
            }

            return dt;
        }

        // 非同步版本（放到背景執行，避免 UI 卡住）
        public static Task<DataTable> QueryToDataTableAsync(string sql, params OleDbParameter[] parameters)
            => Task.Run(() => QueryToDataTable(sql, parameters));
    }
}
