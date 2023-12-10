using FoxLottery.DB;
using Life;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FoxLottery
{
    public class DBManager
    {
        public static SQLiteAsyncConnection db;
        public static string PathDb;

        public async static Task<bool> Init(string pluginPath)
        {
            PathDb = pluginPath;
            DBManager.db = new SQLiteAsyncConnection(pluginPath);
            await  DBManager.db.CreateTableAsync<LotteryModel>(CreateFlags.None);
            await DBManager.db.CreateTableAsync<TicketModel>(CreateFlags.None);
            Debug.Log("[FoxLottery] Database init");
            return true;
        }

        public async static void RegisterLottery(uint bizID, int montant, float price)
        {
            LotteryModel lotteryModel = new LotteryModel()
            {
                bizID = (int)bizID,
                montant = montant,
                price = price,
                status = 0
            };

            await DBManager.db.InsertAsync(lotteryModel);
        }

        public async static Task<LotteryModel> GetLottery(int bizId)
        {
            AsyncTableQuery<LotteryModel> asyncTableQuery = DBManager.db.Table<LotteryModel>();
            LotteryModel lottery = await asyncTableQuery.Where((LotteryModel l) => 
                    l.bizID == bizId && l.status != 1).FirstOrDefaultAsync();

            return lottery;
        }

        public async static void UpdateAlarm(LotteryModel lottery)
        {
            await DBManager.db.UpdateAsync(lottery, typeof(LotteryModel));
        }

        public async static void DeleteAlarm(LotteryModel lottery)
        {
            await DBManager.db.DeleteAsync(lottery);
        }

        public async static void RegisterTicketForLottery(TicketModel ticket)
        {
            await DBManager.db.InsertAsync(ticket);
        }

        public static async Task<int> GetCountTicketsForLottery(int idLottery)
        {
            AsyncTableQuery<TicketModel> asyncTableQuery = DBManager.db.Table<TicketModel>();
            return await asyncTableQuery.CountAsync((TicketModel t) => t.idLottery == idLottery);
        }

        public static async Task<List<TicketModel>> GetTickets(int idChar)
        {
            AsyncTableQuery<TicketModel> asyncTableQuery = DBManager.db.Table<TicketModel>();
            List<TicketModel> tickets =  await asyncTableQuery.Where((TicketModel t) => t.idCharacter == idChar).ToListAsync();
            return tickets;
        }
    }
}
