using ConsoleAppGateway.Models;
using System.Collections.Generic;

namespace ConsoleAppGateway
{
    public class AppProperties
    {
        public string ConnectionStringOracle { get; set; }
        public string ConnectionStringMsSql { get; set; }
        public List<Table> Tables { get; set; }
    }

}
