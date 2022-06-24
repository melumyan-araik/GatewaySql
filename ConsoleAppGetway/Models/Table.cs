using System.Collections.Generic;

namespace ConsoleAppGateway.Models
{
    public class Table
    {
        public string From { get; set; }
        public string To { get; set; }

        public List<ColName> ListColName { get; set; }
    }

}
