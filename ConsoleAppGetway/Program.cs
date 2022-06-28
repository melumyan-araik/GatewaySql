using System;

namespace ConsoleAppGateway
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var gateway = new Gateway(
                AppConfig.Property.ConnectionStringMsSql,
                AppConfig.Property.ConnectionStringOracle,
                AppConfig.Property.Tables
            );
            try
            {
                gateway.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                gateway.Stop();
            }
        }
    }
}
