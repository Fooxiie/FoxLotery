using System;
using Life;
using Life.UI;
using Life.Network;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FoxLottery.DB;
using System.Text.RegularExpressions;
using UnityEngine;
using Random = UnityEngine.Random;

namespace FoxLottery
{
    public class FoxLottery : Plugin
    {
        public FoxLottery(IGameAPI api) : base(api)
        {
        }

        public override void OnPluginInit()
        {
            base.OnPluginInit();

            InitDataBase();

            SetupCommand();
        }

        private void SetupCommand()
        {
            var commandCreateLottery = new SChatCommand("/gestionLoterie", "Create your lottery", "/manageLottery",
                (player, args) => { PanelCreateManageLottery(player); });
            commandCreateLottery.Register();

            var commandSellTicket = new SChatCommand("/vendreTicket", "", "/sellTicket",
                (player, args) => { GiveTicketClosestPlayer(player); });
            commandSellTicket.Register();

            var commandViewTicket =
                new SChatCommand("/mesTickets", "", "/mesTickets", (player, args) => { GetTickets(player); });
            commandViewTicket.Register();
        }

        private static async void GetTickets(Player player)
        {
            var allLotteryOpened = await DBManager.GetLotterys();

            var listTickets = new UIPanel("Mes tickets", UIPanel.PanelType.Tab);
            listTickets.AddButton("Fermer", player.ClosePanel);
            listTickets.AddButton("Selectionner", (ui) => { ui.SelectTab(); });

            // foreach in all the opened lottery
            foreach (var lottery in allLotteryOpened)
            {
                // i fetch all the tickets from the player and the lottery
                var ticketModels = await DBManager.GetTickets(player.character.Id, lottery.id);

                if (ticketModels.Count == 0) continue;

                // Every single line of this menu is a society with open lottery
                listTickets.AddTabLine("Lottery de " + lottery.enterpriseName, (subLottery) =>
                {
                    #region menu list ticket for society

                    player.ClosePanel(subLottery);
                    var panelForTicket = new UIPanel("Ticket de " + lottery.enterpriseName, UIPanel.PanelType.Tab);
                    panelForTicket.AddButton("Montrer", (showTicket) =>
                    {
                        showTicket.SelectTab();
                        player.ClosePanel(showTicket);
                    });
                    panelForTicket.AddButton("Fermer", player.ClosePanel);

                    foreach (var ticket in ticketModels)
                    {
                        panelForTicket.AddTabLine("Ticket numéro " + ticket.Numero,
                            ui => { ShowTicketToPlayer(player, ticket); });
                    }

                    player.ShowPanelUI(panelForTicket);

                    #endregion
                });
            }

            player.ShowPanelUI(listTickets);
        }

        private static void ShowTicketToPlayer(Player player, TicketModel ticket)
        {
            var closestPlayer = player.GetClosestPlayer();
            if (closestPlayer != null)
            {
                var ticketShowing = new UIPanel("Ticket de " + player.GetFullName(), UIPanel.PanelType.Text);
                ticketShowing.SetText("\nTicket numéro : " + ticket.Numero);
                ticketShowing.AddButton("Fermer", closestPlayer.ClosePanel);

                closestPlayer.ShowPanelUI(ticketShowing);
                player.Notify("Loterie", "Vous avez montré votre ticket à la personne à coté de vous.");
            }
            else
            {
                player.Notify("Loterie", "Vous n'avez personne à qui montrer votre ticket.");
            }
        }

        private static async void GiveTicketClosestPlayer(Player player)
        {
            var closestPlayer = player.GetClosestPlayer();
            if (closestPlayer != null)
            {
                if (!player.HasBiz()) return;
                if (player.biz.Id == 0) return;
                var lotteryOpened = await DBManager.GetLottery(player.biz.Id);

                if (lotteryOpened != null)
                {
                    if (lotteryOpened.status == 2)
                    {
                        player.Notify("Loterie", "Vous ne pouvez plus vendre de ticket, la loterie est terminé");
                    }
                    else
                    {
                        AskNumberTicket(player, closestPlayer, lotteryOpened);
                    }
                }
                else
                {
                    player.Notify("Loterie", "Aucune loterie n'est organiser par votre entreprise",
                        NotificationManager.Type.Error);
                }
            }
            else

            {
                player.Notify("Loterie", "Personne n'est prêt de vous.");
            }
        }

        private static void AskNumberTicket(Player player, Player closestPlayer, LotteryModel lottery)
        {
            var panel = new UIPanel("Entrez un numéro de ticket", UIPanel.PanelType.Input)
            {
                inputPlaceholder = "Numéro de ticket compris entre 1 et 1000"
            };
            panel.AddButton("Valider", (ui) =>
            {
                const string pattern = "^(?:[1-9]\\d{0,2}|1000)$";
                var regex = new Regex(pattern);
                if (regex.IsMatch(ui.inputText))
                {
                    var newTicket = new TicketModel
                    {
                        idLottery = lottery.id,
                        idCharacter = closestPlayer.character.Id,
                        Numero = int.Parse(ui.inputText)
                    };

                    DBManager.RegisterTicketForLottery(newTicket);

                    player.Notify("Loterie", "Vous avez bien donner un ticket à " + closestPlayer.GetFullName());
                    closestPlayer.Notify("Loterie", "Votre ticket a bien été pris en compte",
                        NotificationManager.Type.Success);

                    closestPlayer.ClosePanel(ui);
                }
                else
                {
                    closestPlayer.Notify("Loterie", "Le numéro doit être compris entre 1 et 1000");
                }
            });
            panel.AddButton("Fermer", (ui) =>
            {
                closestPlayer.ClosePanel(ui);
                player.Notify("Loterie", "La personne n'as pas validé le numéro de son ticket");
            });

            closestPlayer.ShowPanelUI(panel);
            player.Notify("Loterie", "Votre demande de numéro a été envoyé à la personne devant vous.");
        }

        private async void PanelCreateManageLottery(Player player)
        {
            var lotteryPanel = new UIPanel("Loterie System", UIPanel.PanelType.Tab)
                .AddButton("Fermer", player.ClosePanel)
                .AddButton("Selectionner", (ui) =>
                {
                    ui.SelectTab();
                    player.ClosePanel(ui);
                });

            if (player.HasBiz())
            {
                if (player.biz.Id == 0) return;
                var lotteryOpened = await DBManager.GetLottery(player.biz.Id);
                if (lotteryOpened != null)
                {
                    lotteryOpened.NombreParticipant = await DBManager.GetCountTicketsForLottery(lotteryOpened.id);
                    ManageLottery(player, lotteryOpened, lotteryPanel);
                }
                else
                {
                    CreateLottery(player, lotteryPanel);
                }

                player.ShowPanelUI(lotteryPanel);
            }
            else
            {
                player.Notify("Erreur de commande", "Vous n'avez pas d'enterprise afin de créer une loterie",
                    NotificationManager.Type.Error);
            }
        }

        private static void CreateLottery(Player player, UIPanel panel)
        {
            panel.AddTabLine("Créer une loterie", (ui) =>
            {
                var montantAGagner = 0;

                var panelMontantLotterie = new UIPanel("Montant à gagner", UIPanel.PanelType.Input)
                    .AddButton("Valider", (subUI) =>
                    {
                        montantAGagner = int.Parse(subUI.inputText);
                        if (CheckFound(player, montantAGagner))
                        {
                            player.ClosePanel(subUI);
                            var prixBillet = new UIPanel("Montant du ticket", UIPanel.PanelType.Input)
                            {
                                inputPlaceholder = "100€, 200€"
                            };
                            prixBillet.AddButton("Valider", (btnValidatePrix) =>
                            {
                                DBManager.RegisterLottery((uint)player.biz.Id, player.biz.BizName, montantAGagner,
                                    float.Parse(btnValidatePrix.inputText));
                                player.Notify("Loterie ajouté", "Votre loterie a bien été créer.",
                                    NotificationManager.Type.Success);
                                player.ClosePanel(btnValidatePrix);
                            });
                            prixBillet.AddButton("Fermer", player.ClosePanel);

                            player.ShowPanelUI(prixBillet);
                        }
                        else
                        {
                            player.Notify("Loterie", "Vous n'avez pas les fonds au seins de votre entreprise");
                        }
                    })
                    .AddButton("Fermer", player.ClosePanel);
                panelMontantLotterie.inputPlaceholder = "Entrer le montant à gagner";

                player.ShowPanelUI(panelMontantLotterie);
            });
        }

        private static bool CheckFound(Player player, int montantAGagner)
        {
            if (player.HasBiz())
            {
                return player.biz.Bank >= montantAGagner;
            }

            return false;
        }

        private static async void ManageLottery(Player player, LotteryModel lotteryOpened, UIPanel panel)
        {
            panel.AddTabLine("Montant à gagner : " + lotteryOpened.montant + "€", (ui) => { });

            panel.AddTabLine("Prix du ticket : " + lotteryOpened.price + "€", (ui) => { });

            panel.AddTabLine("Nombre de participant : " + lotteryOpened.NombreParticipant, (ui) => { });

            if (lotteryOpened.status != 2)
            {
                panel.AddTabLine("Tirer un nombre au hasard", (ui) =>
                {
                    var numeroTire = Random.Range(1, 1001);
                    lotteryOpened.numSortie = numeroTire;
                    lotteryOpened.status = 2;
                    DBManager.UpdateAlarm(lotteryOpened);
                    player.ClosePanel(ui);
                    var fenResult = new UIPanel("Resultat du tirage", UIPanel.PanelType.Text);
                    fenResult.AddButton("Fermer", player.ClosePanel);
                    fenResult.SetText("\nNuméro gagnant : " + numeroTire);
                    player.ShowPanelUI(fenResult);
                });

                panel.AddTabLine("Annuler la loterie", (ui) =>
                {
                    lotteryOpened.status = 1;
                    DBManager.UpdateAlarm(lotteryOpened);
                    player.ClosePanel(ui);
                });
            }

            if (lotteryOpened.status == 2)
            {
                panel.AddTabLine("Fermer la loterie", ui =>
                {
                    lotteryOpened.status = 1;
                    DBManager.UpdateAlarm(lotteryOpened);
                    player.ClosePanel(ui);
                });
            }
        }

        private async void InitDataBase()
        {
            await DBManager.Init(Path.Combine(pluginsPath, "FoxLottery/data.db"));
        }
    }
}
