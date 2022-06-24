using ConsoleAppGateway.Models;
using System.Collections.Generic;

namespace ConsoleAppGateway
{
    internal class Gateway : IGateway
    {
        public string DbFrom { get => _dbFrom; set => _dbFrom = value; }
        public string DbTo { get => _dbTo; set => _dbTo = value; }
        public List<Table> Tablesl { get => _tablesl; set => _tablesl = value; }

        public Gateway() { }

        public Gateway(string dbfrom, string dbto, List<Table> tables)
        {
            _dbFrom = dbfrom;
            DbTo = dbto;
            Tablesl = tables;
        }

        public void Start()
        {

        }

        public void Stop()
        {

        }

        private string _dbFrom;
        private string _dbTo;
        private List<Table> _tablesl;
    }
}
