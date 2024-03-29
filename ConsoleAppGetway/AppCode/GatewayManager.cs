﻿using ConsoleAppGateway.Logger;
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
            _startTime = DateTime.Now;
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
                Console.WriteLine($"\nОбновление: {table.To}");
                CheckSettings(table);
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
            finally
            {
                Console.WriteLine($"\nВремя работы программы: { DateTime.Now - _startTime}");
            }
        }

        public void CheckSettings(Table table)
        {
            try
            {
                var sqlMs = $"select  count(*) from {table.From};";
                var cmdMs = new SqlCommand(sqlMs, _sqlConnection);
                cmdMs.CommandTimeout = COMMAND_TIMEOUT;
                _countFrom = Convert.ToInt32(cmdMs.ExecuteScalar());
                Console.WriteLine($"Количество записей в таблице {table.From}: {_countFrom}  <== From");
            }
            catch (Exception ex)
            {
                _logger.AddLog($"Ошибка при проверки таблицы MSSql {table.From}: {ex.Message}", EventLogEntryType.Error);
                throw new Exception($"Ошибка при проверки таблицы MSSql {table.From}: {ex.Message}", ex);
            }

            try
            {
                string strCommand = $"select count(*) from {table.To}";
                OracleCommand cmd = new OracleCommand(strCommand, _oracleConnection);
                _countTo = Convert.ToInt32(cmd.ExecuteScalar());
                Console.WriteLine($"Количество записей в таблице {table.To}: {_countTo}  <== To");
            }
            catch (Exception ex)
            {
                _logger.AddLog($"Ошибка при проверки таблицы ORA {table.To}: {ex.Message}", EventLogEntryType.Error);
                throw new Exception($"Ошибка при проверки таблицы ORA {table.To}: {ex.Message}", ex);
            }
        }

        private void TakeThisFromTheMssqlAndPutItDownOracle(Table table)
        {
            if (table.IsClear)
            {
                TruncOracle(table.To);
                Console.WriteLine($"Очищение таблицы: {table.To}");
            }
            Console.WriteLine($"Уровень комитов: {_countCommit}");

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
            cmdMs.CommandTimeout = COMMAND_TIMEOUT;
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
            var dInsert = DateTime.Now;
            var iteratorCountCommit = 0;
            int totalCountCommit = _countFrom / _countCommit + 1;
            Console.Write($"Вставка в {table.To}: {iteratorCountCommit}/{totalCountCommit}");
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
                    var d = DateTime.Now;
                    InsertOracle(listSqlOra);
                    var dt = DateTime.Now - d;
                    iteratorCountCommit++;
                    Console.SetCursorPosition(0, Console.CursorTop);
                    Console.Write($"Вставка в {table.To}: {iteratorCountCommit}/{totalCountCommit} :: Время вставки: {dt}");
                    listSqlOra = new List<OracleCommand>();
                }
            }
            if (listSqlOra.Count < _countCommit)
            {
                var d = DateTime.Now;
                InsertOracle(listSqlOra);
                var dt = DateTime.Now - d;
                iteratorCountCommit++;
                Console.SetCursorPosition(0, Console.CursorTop);
                Console.WriteLine($"Вставка в {table.To}: {iteratorCountCommit}/{totalCountCommit} :: Время вставки: {dt}");
                listSqlOra = new List<OracleCommand>();
            }
            reader.Close();
            Console.WriteLine($"Время обновления {table.To}: {DateTime.Now - dInsert}");
        }

        private void TakeThisFromTheOracleAndPutItDownMssql(Table table)
        {

        }

        private void TruncOracle(string tableName)
        {
            try
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
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при удалении из таблицы {tableName}: {ex.Message}");
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
        private int _countCommit = 5000;
        private int _countTo;
        private int _countFrom;
        private DateTime _startTime;
        private static readonly ILogger _logger = new LoggerEvent("Gatway");
        private const int COMMAND_TIMEOUT = 140;
    }
}
