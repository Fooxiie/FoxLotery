using JetBrains.Annotations;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoxLottery.DB
{
    public class TicketModel
    {
        [AutoIncrement]
        [PrimaryKey]
        public int Id { get; set; }

        public int idCharacter { get; set; }

        public int idLottery { get; set; }

        public int Numero { get; set; }
    }
}
