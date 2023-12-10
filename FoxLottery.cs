using Life;
using Life.UI;
using Life.Network;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FoxLottery.DB;
using System.Text.RegularExpressions;

namespace FoxLottery
{
    public class FoxLottery : Plugin
    {
        public FoxLottery(IGameAPI api) : base(api)
        { }

        public override void OnPluginInit()
        {
            base.OnPluginInit();

            InitDataBase();

            SetupCommand();
        }

        private void SetupCommand()
        {
            SChatCommand commandCreateLottery = new SChatCommand("/manageLottery", "Create your lottery", "/manageLottery", (player, args) =>
            {
                PanelCreateManageLottery(player);
            });
            commandCreateLottery.Register();

            SChatCommand commandSellticket = new SChatCommand("/sellTicket", "", "/sellTicket", (player, args) =>
            {
                GiveTicketClosestPlayer(player);
            });
            commandSellticket.Register();

            SChatCommand commandViewTicket = new SChatCommand("/mesTickets", "", "/mesTickets", (player, args) =>
            {
                GetTickets(player);
            });
            commandViewTicket.Register();
        }

        private static async Task GetTickets(Player player)
        {
            List<TicketModel> ticketModels = await DBManager.GetTickets(player.character.Id);
            UIPanel listTickets = new UIPanel("Mes tickets", UIPanel.PanelType.Tab);
            listTickets.AddButton("Fermer", (ui) => { player.ClosePanel(ui); });
            foreach(var ticket in ticketModels)
            {
                listTickets.AddTabLine("Numero : " + ticket.Numero, null);
            }

            player.ShowPanelUI(listTickets);
        }

        private async void GiveTicketClosestPlayer(Player player)
        {
            var closestPlayer = player.GetClosestPlayer();

            if (player.HasBiz())
            {
                if (player.biz.Id != 0)
                {
                    var LotteryOpened = await DBManager.GetLottery(player.biz.Id);

                    if (LotteryOpened != null)
                    {
                        AskNumberTicket(player, closestPlayer, LotteryOpened);
                    } else
                    {
                        player.Notify("Lotterie", "Aucune lotterie n'est organiser par votre entreprise", NotificationManager.Type.Error);
                    }
                }
            }
        }

        private void AskNumberTicket(Player player, Player closestPlayer, LotteryModel lottery)
        {
            UIPanel panel = new UIPanel("Entrez un numéro de ticket", UIPanel.PanelType.Input);
            panel.inputPlaceholder = "Entrez un numéro de ticket compris entre 1 et 1000";
            panel.AddButton("Valider", (ui) =>
            {
                string pattern = "^(?:[1-9]\\d{0,2}|1000)$";
                Regex regex = new Regex(pattern);
                if (regex.IsMatch(ui.inputText))
                {
                    TicketModel newTicket = new TicketModel();
                    newTicket.idLottery = lottery.id;
                    newTicket.idCharacter = closestPlayer.character.Id;
                    newTicket.Numero = int.Parse(ui.inputText);

                    DBManager.RegisterTicketForLottery(newTicket);

                    player.Notify("Lotterie", "Vous avez bien donner un ticket à " + closestPlayer.GetFullName());
                    closestPlayer.Notify("Lotterie", "Votre ticket a bien été pris en compte", NotificationManager.Type.Success);

                    closestPlayer.ClosePanel(ui);
                } else
                {
                    closestPlayer.Notify("Lotterie", "Le numéro doit être compris entre 1 et 1000");
                }
            });
            panel.AddButton("Fermer", (ui) =>
            {
                closestPlayer.ClosePanel(ui);
                player.Notify("Lotterie", "La personne n'as pas validé le numéro de son ticket");
            });

            closestPlayer.ShowPanelUI(panel);
        }

        private async void PanelCreateManageLottery(Player player)
        {
            UIPanel lotteryPanel = new UIPanel("Lotterie System", UIPanel.PanelType.Tab)
                .AddButton("Fermer", (ui) =>
                {
                    player.ClosePanel(ui);
                })
                .AddButton("Selectionner", (ui) =>
                {
                    ui.SelectTab();
                });

            if (player.HasBiz())
            {
                if (player.biz.Id != 0)
                {
                    var LotteryOpened = await DBManager.GetLottery(player.biz.Id);
                    if (LotteryOpened != null)
                    {
                        ManageLottery(player, LotteryOpened, lotteryPanel);
                    }
                    else
                    {
                        CreateLottery(player, lotteryPanel);
                    }
                }
            }
            else
            {
                player.Notify("Erreur de commande", "Vous n'avez pas d'enterprise afin de créer une lotterie", NotificationManager.Type.Error);
            }
        }

        private void CreateLottery(Player player, UIPanel panel)
        {
            panel.AddTabLine("Créer une lotterie", (ui) =>
            {
                player.ClosePanel(ui);

                int montantAGagner = 0;

                UIPanel panelMontantLotterie = new UIPanel("Montant à gagner", UIPanel.PanelType.Input)
                .AddButton("Valider", (subUI) =>
                {
                    montantAGagner = int.Parse(subUI.inputText);
                    if (CheckFound(player, montantAGagner))
                    {
                        player.ClosePanel(subUI);
                        UIPanel prixBillet = new UIPanel("Montant du ticket", UIPanel.PanelType.Input);
                        prixBillet.inputPlaceholder = "100€, 200€";
                        prixBillet.AddButton("Valider", (btnValidatePrix) =>
                        {
                            DBManager.RegisterLottery((uint)player.biz.Id, montantAGagner, float.Parse(btnValidatePrix.inputText));
                            player.Notify("Lotterie ajouté", "Votre lotterie a bien été créer.", NotificationManager.Type.Success);
                            player.ClosePanel(btnValidatePrix);
                        });
                        prixBillet.AddButton("Fermer", (btnClosePrix) =>
                        {
                            player.ClosePanel(btnClosePrix);
                        });

                        player.ShowPanelUI(prixBillet);
                    }
                })
                .AddButton("Fermer", (subUI) =>
                {
                    player.ClosePanel(subUI);
                });
                panelMontantLotterie.inputPlaceholder = "Entrer le montant à gagner";

                player.ShowPanelUI(panelMontantLotterie);
            });

            player.ShowPanelUI(panel);
        }

        private bool CheckFound(Player player, int montantAGagner)
        {
            if (player.HasBiz()) { return player.biz.Bank >= montantAGagner; }
            return false;
        }

        private async void ManageLottery(Player player, LotteryModel lotteryOpened, UIPanel panel)
        {
            panel.AddTabLine("Montant à gagner : " + lotteryOpened.montant + "€", (ui) =>
            {

            });


            panel.AddTabLine("Prix du ticket : " + lotteryOpened.price + "€", (ui) =>
            {

            });

            var nbParticipant = await DBManager.GetCountTicketsForLottery(lotteryOpened.id);
            panel.AddTabLine("Nombre de participant : " + nbParticipant, (ui) =>
            {

            });

            panel.AddTabLine("Tirer un nombre au hasard", (ui) => {

            });

            if (lotteryOpened.status != 2)
            {
                panel.AddTabLine("Annuler la lotterie", (ui) =>
                {
                    lotteryOpened.status = 1;
                    DBManager.UpdateAlarm(lotteryOpened);
                    player.ClosePanel(ui);
                });
            }

            player.ShowPanelUI(panel);
        }

        private async void InitDataBase()
        {
            await DBManager.Init(Path.Combine(pluginsPath, "FoxLottery/data.db"));
        }
    }
}
