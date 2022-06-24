using ConsoleAppGateway.Models;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Data.SqlClient;
using System.Linq;

namespace ConsoleAppGateway
{
    internal class Gateway : IGateway
    {
        public string DbFrom { get => _dbFrom; set => _dbFrom = value; }
        public string DbTo { get => _dbTo; set => _dbTo = value; }
        public List<Table> Tablesl { get => _tablesl; set => _tablesl = value; }

        public Gateway(string dbfrom, string dbto, List<Table> tables)
        {
            _dbFrom = dbfrom;
            DbTo = dbto;
            Tablesl = tables;

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
                throw new Exception($"Ошибка при старте: {ex.Message}");
            }

            foreach(var table in _tablesl)
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
                throw new Exception($"Ошибка при закрытии соеденения: {ex.Message}");
            }
        }

        private void TakeThisFromTheMssqlAndPutItDownOracle (Table table)
        {
            string listColNameFrom = "";
            table.ListColName.ForEach(x => listColNameFrom += $"{x.NameFrom},");
            listColNameFrom = listColNameFrom.Remove(listColNameFrom.Length - 1, 1);

            string listColNameTo = "";
            table.ListColName.ForEach(x => listColNameTo += $"{x.NameTo},");
            listColNameTo = listColNameTo.Remove(listColNameTo.Length - 1, 1);

            var sqlMs = $"select {listColNameFrom} from {table.From};";
            var cmdMs = new SqlCommand(sqlMs, _sqlConnection);
            cmdMs.ExecuteNonQuery();

            //var sqlOra = $"insert into :tabName (:listColNameTo) values (:values)";
            //var cmdOra = new OracleCommand(sqlOra, _oracleConnection);
            //cmdOra.Parameters.Add("tabName", table.To);
            //cmdOra.Parameters.Add("listColName", listColNameTo);
            //cmdOra.Parameters.Add("values", listColName);
        }

        private void TakeThisFromTheOracleAndPutItDownMssql(Table table)
        {
          
        }

        private OracleConnection _oracleConnection;
        private SqlConnection _sqlConnection;
        private string _dbFrom;
        private string _dbTo;
        private List<Table> _tablesl;
    }
}
