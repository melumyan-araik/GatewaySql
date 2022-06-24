using System.Data.SqlClient;
using System;
using System.Data;
using System.Threading.Tasks;
using ConsoleAppGateway.Models;

namespace ConsoleAppGateway
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var gateway = new Gateway();
            try
            {
                gateway.DbFrom = "a";
            }
            catch (Exception)
            {

                throw;
            }
            

            var connectionString = AppConfig.Property.ConnectionStringMsSql;
            DataTable dt = new DataTable();

            using (var conn = new SqlConnection(connectionString))
            {
                var queryString = "select * from TESTGATEWEY";
                using (var command = new SqlDataAdapter(queryString, conn))
                {
                    command.Fill(dt);
                }
            }
        }

        private static void InitConfig()
        {
            AppConfig.Property.ConnectionStringOracle = $"Data Source=ORCL_NSI;User ID=NSI;Password=nsi;";
            AppConfig.Property.ConnectionStringMsSql = $"Data Source=mssqlo;Initial Catalog=tfomssite;Integrated Security=true;TrustServerCertificate=true;";
            AppConfig.Property.Tables.Add(new Table
            {
                From = "RegisterDen",
                To = "dasda"
            });

            AppConfig.Save();
        }


        //var connStr = AppConfig.Property.ConnectionString;
        //DataTable dt = new DataTable();

        //using var conn = new OracleConnection(connStr);
        //var command = $"selectom nsi.f002 t";
        //using var oda = new OracleDataAdapter(command, conn);
        //oda.Fill(dt);

    }

}
