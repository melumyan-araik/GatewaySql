using ConsoleAppGateway.Logger;
using ConsoleAppGateway.Models;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Linq;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;

namespace ConsoleAppGateway
{
    internal class Gateway : IGateway
    {
        public string DbFrom { get => _dbFrom; set => _dbFrom = value; }
        public string DbTo { get => _dbTo; set => _dbTo = value; }
        public List<Table> Tablesl { get => _tablesl; set => _tablesl = value; }
        public int CountCommit { get => _countCommit; set => _countCommit = value; }
        public Gateway(string dbfrom, string dbto, List<Table> tables, int countCommit = 5000)
        {
            DbFrom = dbfrom;
            DbTo = dbto;
            Tablesl = tables;
            CountCommit = countCommit;
        }

        public Gateway()
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
                _logger .AddLog($"Ошибка при старте: {ex.Message}", EventLogEntryType.Error);
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
                _logger .AddLog($"Ошибка при закрытии соеденения: {ex.Message}", EventLogEntryType.Error);
                throw new Exception($"Ошибка при закрытии соеденения: {ex.Message}");
            }
        }

        private void TakeThisFromTheMssqlAndPutItDownOracle(Table table)
        {
            string listColNameFrom = "";
            table.ListColName.ForEach(x => listColNameFrom += $"{x.NameFrom},");
            listColNameFrom = listColNameFrom.TrimEnd(',');

            string listColNameTo = "";
            table.ListColName.ForEach(x => listColNameTo += $"{x.NameTo},");
            listColNameTo = listColNameTo.TrimEnd(',');

            var sqlMs = $"select {listColNameFrom} from {table.From};";
            var cmdMs = new SqlCommand(sqlMs, _sqlConnection);
            try
            {
                var reader = cmdMs.ExecuteReader();

                List<OracleCommand> listSqlOra = new List<OracleCommand>();

                while (reader.Read())
                {
                    string values = "";
                    for (var i = 0; i < reader.FieldCount; i++)
                    {
                        values += $"'{reader.GetValue(i)}',";
                    }
                    values = values.TrimEnd(',');
                    listSqlOra.Add(new OracleCommand($"insert into {table.To} ({listColNameTo}) values ({values})", _oracleConnection));

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
            catch (Exception ex)
            {
                _logger.AddLog($"Ошибка при получении данных: {ex.Message}", EventLogEntryType.Error);
                throw new Exception($"Ошибка при получении данных: {ex.Message}");
            }

        }

        private void TakeThisFromTheOracleAndPutItDownMssql(Table table)
        {

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
                _logger .AddLog($"Ошибка при вставки данных: {ex.Message}", EventLogEntryType.Error);
                throw new Exception($"Ошибка при вставки данных: {ex.Message}");
            }
        }

        private OracleConnection _oracleConnection;
        private SqlConnection _sqlConnection;
        private string _dbFrom;
        private string _dbTo;
        private List<Table> _tablesl;
        private int _countCommit;
        private static readonly ILogger _logger  = new LoggerEvent("Gatway");
    }
}
