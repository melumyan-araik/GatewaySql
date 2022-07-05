using ConsoleAppGateway.Logger;
using ConsoleAppGateway.Models;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;

namespace ConsoleAppGateway
{
    internal class GatewayManager : IGatewayManager
    {
        public string DbFrom { get => _dbFrom; set => _dbFrom = value; }
        public string DbTo { get => _dbTo; set => _dbTo = value; }
        public List<Table> Tablesl { get => _tablesl; set => _tablesl = value; }
        public int CountCommit { get => _countCommit; set => _countCommit = value; }
        public GatewayManager(string dbfrom, string dbto, List<Table> tables, int countCommit = 5000)
        {
            DbFrom = dbfrom;
            DbTo = dbto;
            Tablesl = tables;
            CountCommit = countCommit;
        }

        public GatewayManager()
        {
        }

        public void Start()
        {
            _sqlConnection = new SqlConnection(_dbFrom);
            _oracleConnection = new OracleConnection(_dbTo);
            try
            {
                _oracleConnection.Open();
                _sqlConnection.Open();
            }
            catch (Exception ex)
            {
                Stop();
                _logger.AddLog($"Ошибка при старте: {ex.Message}", EventLogEntryType.Error);
                throw new Exception($"Ошибка при старте: {ex.Message}");
            }

            foreach (var table in _tablesl)
            {
                TakeThisFromTheMssqlAndPutItDownOracle(table);
            }
        }

        public void Stop()
        {
            try
            {
                _oracleConnection.Close();
                _sqlConnection.Close();
            }
            catch (Exception ex)
            {
                _logger.AddLog($"Ошибка при закрытии соеденения: {ex.Message}", EventLogEntryType.Error);
                throw new Exception($"Ошибка при закрытии соеденения: {ex.Message}");
            }
        }

        private void TakeThisFromTheMssqlAndPutItDownOracle(Table table)
        {
            if (table.IsClear)
                TruncOracle(table.From);

            string listColNameFrom = "";
            table.ListColName.ForEach(x => listColNameFrom += $"{x.NameFrom},");
            listColNameFrom = listColNameFrom.TrimEnd(',');

            string listColNameTo = "";
            string listParamValTo = "";
            table.ListColName.ForEach(x =>
            {
                listColNameTo += $"{x.NameTo},";
                listParamValTo += $" :{x.NameTo},";
            });
            listColNameTo = listColNameTo.TrimEnd(',');
            listParamValTo = listParamValTo.TrimEnd(',');

            string strCommand = $"insert into {table.To} ({listColNameTo}) values ({listParamValTo})";

            var sqlMs = $"select {listColNameFrom} from {table.From};";
            var cmdMs = new SqlCommand(sqlMs, _sqlConnection);
            SqlDataReader reader;
            try
            {
                reader = cmdMs.ExecuteReader();
            }
            catch (Exception ex)
            {
                _logger.AddLog($"Ошибка при получении данных: {ex.Message}", EventLogEntryType.Error);
                throw new Exception($"Ошибка при получении данных: {ex.Message}");
                
            }
            
            List<OracleCommand> listSqlOra = new List<OracleCommand>();
            while (reader.Read())
            {
                OracleCommand cmd = new OracleCommand(strCommand, _oracleConnection);
                var colName = listColNameTo.Split(',');
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    cmd.Parameters.Add(colName[i], GetOracleDbType(reader.GetValue(i)), reader.GetValue(i), ParameterDirection.Input);
                }

                listSqlOra.Add(cmd);

                if (listSqlOra.Count == _countCommit)
                {
                    InsertOracle(listSqlOra);
                }
            }
            if (listSqlOra.Count < _countCommit)
            {
                InsertOracle(listSqlOra);
            }
        }

        private void TakeThisFromTheOracleAndPutItDownMssql(Table table)
        {

        }

        private void TruncOracle(string tableName)
        {
            var schema = _dbTo.Split('l')[1];
            var FKlist = new DataTable();
            //запрос списка внешних ключей
            using (var oda = new OracleDataAdapter($@"select constraint_name, table_name
                                                    from user_constraints 
                                                    where r_constraint_name in
                                                    (
                                                    select constraint_name
                                                    from user_constraints
                                                    where constraint_type in ('P','U')
                                                    and table_name = upper('{tableName}') and owner = upper('{schema}'))", _oracleConnection))
            {
                oda.Fill(FKlist);
            }
            //Отключаем все ключи
            foreach (DataRow row in FKlist.Rows)
            {
                using (var cmddisabled = new OracleCommand($"alter table {row["table_name"]} disable CONSTRAINT  {row["constraint_name"]}", _oracleConnection))
                {
                   cmddisabled.ExecuteNonQuery();
                }
            }

            using (var cmd = new OracleCommand($"truncate table {tableName}", _oracleConnection))
            {
                cmd.ExecuteNonQuery();
            }

            //Включаем все ключи
            foreach (DataRow row in FKlist.Rows)
            {
                using (var cmddisabled = new OracleCommand($"alter table {row["table_name"]} enable CONSTRAINT  {row["constraint_name"]}", _oracleConnection))
                {
                    cmddisabled.ExecuteNonQuery();
                }
            }
        }

        private static OracleDbType GetOracleDbType(object o)
       {
            if (o is string) return OracleDbType.Varchar2;
            if (o is DateTime) return OracleDbType.Date;
            if (o is Int64) return OracleDbType.Int64;
            if (o is Int32) return OracleDbType.Int32;
            if (o is Int16) return OracleDbType.Int16;
            if (o is sbyte) return OracleDbType.Byte;
            // if (o is byte) return OracleDbType.Int16; -- <== unverified
            if (o is decimal) return OracleDbType.Decimal;
            if (o is float) return OracleDbType.Single;
            if (o is double) return OracleDbType.Double;
            if (o is byte[]) return OracleDbType.Blob;

            return OracleDbType.Varchar2;
        }
        private void InsertOracle(List<OracleCommand> listSqlOra)
        {
            OracleTransaction Tran = _oracleConnection.BeginTransaction(IsolationLevel.ReadCommitted);
            try
            {
                foreach (var cmdOra in listSqlOra)
                {
                    cmdOra.ExecuteNonQuery();
                }
                Tran.Commit();
            }
            catch (Exception ex)
            {
                Tran.Rollback();
                _logger.AddLog($"Ошибка при вставки данных: {ex.Message}", EventLogEntryType.Error);
                throw new Exception($"Ошибка при вставки данных: {ex.Message}");
            }
        }

        private OracleConnection _oracleConnection;
        private SqlConnection _sqlConnection;
        private string _dbFrom;
        private string _dbTo;
        private List<Table> _tablesl;
        private int _countCommit;
        private static readonly ILogger _logger = new LoggerEvent("Gatway");
    }
}
