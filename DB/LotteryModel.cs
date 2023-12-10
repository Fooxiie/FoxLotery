using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoxLottery.DB
{
    public class LotteryModel
    {
        [AutoIncrement]
        [PrimaryKey]
        public int id
        { get; set; }

        public int montant
        { get; set; }

        public int bizID
        { get; set; }

        public float price
        { get; set; }

        public int status
        { get; set; }
    }
}
