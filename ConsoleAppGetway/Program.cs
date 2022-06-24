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

            gateway.Start();
        }
    }
}
