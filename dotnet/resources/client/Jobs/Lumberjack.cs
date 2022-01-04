using GTANetworkAPI;
using System.Collections.Generic;
using System;
using NeptuneEvo.GUI;
using NeptuneEvo.Core;
using Redage.SDK;

namespace NeptuneEvo.Jobs
{
    class Lumberjack : Script
    {
        private static int checkpointPayment = 25; //Зарплата

        private static int JobsMinLVL = 0;
        private static int JobWorkId = 15; // Номер работы
        private static nLog Log = new nLog("Lumberjack");

        private static int lumberjack1 = 25; // 1 уровень

        private static int lumberjack2 = 35; // 2 уровень
        private static int sawdust1 = 30;


        private static Vector3 ReceptionP = new Vector3(-1587.4827, 4513.3853, 18.6483); //Пункт сдачи деревьев

        [ServerEvent(Event.ResourceStart)]
        public void Event_ResourceStart()
        {
            try
            {
                #region лесоруб
                NAPI.Blip.CreateBlip(77, new Vector3(-1582.1957, 4527.828, 17.784897), 1, 25, Main.StringToU16("Лесоруб"), 255, 0, true, 0, 0); // Блип на карте
                NAPI.TextLabel.CreateTextLabel("~w~Лесоруб", new Vector3(-1582.1957, 4527.828, 19), 30f, 0.3f, 0, new Color(255, 255, 255), true, NAPI.GlobalDimension); // Над головой у бота
                NAPI.Marker.CreateMarker(1, new Vector3(-1580.9956, 4527.482, 16.997709) - new Vector3(0, 0, 0.7), new Vector3(), new Vector3(), 1, new Color(255, 255, 255, 220)); //Начать рабочий день маркер
                var col = NAPI.ColShape.CreateCylinderColShape(new Vector3(-1580.9956, 4527.482, 17.557709), 4, 2, 0); // Меню которое открывается на 'E'
                var lic = NAPI.ColShape.CreateCylinderColShape(new Vector3(-567.93066, 5332.393, 70.214455), 4, 2, 0); // License
                NAPI.TextLabel.CreateTextLabel("~w~Получить лицензию на рубку леса", new Vector3(-567.93066, 5332.393, 71.214455), 30f, 0.3f, 0, new Color(255, 255, 255), true, NAPI.GlobalDimension); // Над головой у бота
                NAPI.Marker.CreateMarker(1, new Vector3(-569.0281, 5332.691, 69.714455) - new Vector3(0, 0, 0.7), new Vector3(), new Vector3(), 1, new Color(255, 255, 255, 220));

                col.OnEntityEnterColShape += (shape, player) => {
                    try
                    {
                        player.SetData("INTERACTIONCHECK", 514);
                    }
                    catch (Exception ex) { Log.Write("col.OnEntityEnterColShape: " + ex.Message, nLog.Type.Error); }
                };
                col.OnEntityExitColShape += (shape, player) => {
                    try
                    {
                        player.SetData("INTERACTIONCHECK", 0);
                    }
                    catch (Exception ex) { Log.Write("col.OnEntityExitColShape: " + ex.Message, nLog.Type.Error); }
                };
                lic.OnEntityEnterColShape += (shape, player) => {
                    try
                    {
                        player.SetData("INTERACTIONCHECK", 515);
                    }
                    catch (Exception ex) { Log.Write("col.OnEntityEnterColShape: " + ex.Message, nLog.Type.Error); }
                };
                lic.OnEntityExitColShape += (shape, player) => {
                    try
                    {
                        player.SetData("INTERACTIONCHECK", 0);
                    }
                    catch (Exception ex) { Log.Write("col.OnEntityExitColShape: " + ex.Message, nLog.Type.Error); }
                };
                #endregion

                int i = 0;
                foreach (var Check in Checkpoints)
                {
                    col = NAPI.ColShape.CreateCylinderColShape(Check.Position, 1, 2, 0);
                    col.SetData("NUMBER2", i);
                    col.OnEntityEnterColShape += PlayerEnterCheckpoint;
                    i++;
                };
                #region Пункты приема деревьев
                var reception_point = NAPI.ColShape.CreateCylinderColShape(((ReceptionP)), 5, 2, 0); // Пункт сдачи деревьев
                reception_point.OnEntityEnterColShape += (shape, player) => {
                    try
                    {
                        ReceptionPoint(player);
                    }
                    catch (Exception ex) { Log.Write("col.OnEntityEnterColShape: " + ex.Message, nLog.Type.Error); }
                };
                #endregion
            }
            catch (Exception e) { Log.Write("ResourceStart: " + e.Message, nLog.Type.Error); }
        }

        #region Чекпоинты
        private static List<Checkpoint> Checkpoints = new List<Checkpoint>()
        {
            new Checkpoint(new Vector3(-1584.06, 4516.0483, 18.511911), 144.9358), // Дерево 1
            new Checkpoint(new Vector3(-1578.1637, 4511.701, 19.503168), 66.76145), // Дерево 2
            new Checkpoint(new Vector3(-1573.6885, 4503.4634, 20.529808), -107.136955), // Дерево 3
            new Checkpoint(new Vector3(-1581.0087, 4494.4873, 20.772), 16.060425), // Дерево 4
            new Checkpoint(new Vector3(-1580.6091, 4490.755, 21.442795), -168.59813), // Дерево 5

            new Checkpoint(new Vector3(-1589.62, 4488.376, 18.238361), 149.69136), // Дерево 6
            new Checkpoint(new Vector3(-1597.6288, 4487.983, 18.24954), 108.92541), // Дерево 7
            new Checkpoint(new Vector3(-1603.3838, 4483.296, 16.43074), 45.074997), // Дерево 8
            new Checkpoint(new Vector3(-1600.4958, 4508.4404, 17.863478), 243.5095), // Дерево 9

            new Checkpoint(new Vector3(-1597.5543, 4497.1206, 19.56659), 148.73453), // Дерево 10
            new Checkpoint(new Vector3(-1603.4116, 4477.5034, 15.117557), -172.06453), // Дерево 11
            new Checkpoint(new Vector3(-1583.8843, 4473.9775, 13.033747), 149.932), // Дерево 12
            new Checkpoint(new Vector3(-1554.9749, 4471.8877, 18.824911), -132.99454), // Дерево 13
            new Checkpoint(new Vector3(-1534.9291, 4456.009, 14.33447), -134.81082), // Дерево 14

            new Checkpoint(new Vector3(-1542.4873, 4452.173, 13.947832), 123.86243), // Дерево 15
            new Checkpoint(new Vector3(-1544.7604, 4444.395, 11.872186), 159.9072), // Дерево 16
            new Checkpoint(new Vector3(-1561.3413, 4435.3706, 8.354037), 145.99005), // Дерево 17
            new Checkpoint(new Vector3(-1571.5996, 4427.2305, 6.627333), 142.96483), // Дерево 18
            new Checkpoint(new Vector3(-1569.7524, 4479.6255, 20.785915), 71.0128), // Дерево 19
        };
        private static List<Checkpoint> Checkpoints2 = new List<Checkpoint>()
        {
            new Checkpoint((ReceptionP), 145.99005), // Пункт сдачи дерева

        };
        #endregion

        #region Меню которое нажимается на E
        public static void StartWorkDayLumberjack(Player player)
        {
            if (player.IsInVehicle) return;
            if (Main.Players[player].LVL < JobsMinLVL)
            {
                Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"Необходим как минимум {JobsMinLVL} уровень", 3000);
                return;
            }

            Trigger.ClientEvent(player, "OpenLumberjack", checkpointPayment, Main.Players[player].LVL, NAPI.Data.GetEntityData(player, "ON_WORK2"));

        }
        #endregion
        #region Начать рабочий день
        [RemoteEvent("enterJobLumberjack")]
        public static void JobJoin2(Player player, int job)
        {
            if (!Main.Players[player].Licenses[8])
            {
                Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"У вас нет лицензии на рубку дерева, приобретите ее на лесопилке.", 3000);
                return;
            }
            var aItem = nInventory.Find(Main.Players[player].UUID, ItemType.Axe);
            if (aItem == null)
            {
                Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"У вас нет топора, купите его в магазине 24/7.", 3000);
                return;
            }
                if (NAPI.Data.GetEntityData(player, "ON_WORK") == false)
            {

                Customization.ClearClothes(player, Main.Players[player].Gender);
                if (Main.Players[player].Gender)
                {
                    player.SetClothes(11, 43, 0);
                    player.SetClothes(4, 75, 0);
                    player.SetAccessories(0, 13, 0);
                    player.SetAccessories(1, 15, 0);
                    player.SetClothes(6, 24, 0);
                    player.SetClothes(3, 41, 0);
                }
                else
                {
                    player.SetClothes(11, 171, 0);
                    player.SetClothes(4, 1, 0);
                    player.SetAccessories(0, 20, 0);
                    player.SetAccessories(1, 9, 0);
                    player.SetClothes(6, 26, 0);
                    player.SetClothes(3, 51, 0);
                }

                var check = WorkManager.rnd.Next(0, Checkpoints.Count - 1);
                Trigger.ClientEvent(player, "createCheckpoint", 15, 1, Checkpoints[check].Position - new Vector3(0, 0, 0.7), 2, 0, 255, 0, 0);
                Trigger.ClientEvent(player, "createWorkBlip", Checkpoints[check].Position);
                player.SetData("WOOD", 0);
                player.SetData("WORKCHECK", check);
                player.SetData("ON_WORK", true);
                player.SetData("ON_WORK2", job);
                player.SetData("PLAYER_WORKING", JobWorkId);
                BasicSync.AttachObjectToPlayer(player, NAPI.Util.GetHashKey("prop_tool_fireaxe"), 18905, new Vector3(0.1, 0, 0), new Vector3(-90, 180, 0));
                Notify.Send(player, NotifyType.Success, NotifyPosition.BottomCenter, $"Вы начали рабочий день", 3000);
                  Trigger.ClientEvent(player, "JobStatsInfoW", player.GetData<int>("PAYMENT"));
            }
            else
            {
                if (player.GetData<int>("WOOD") == 1)
                {
                    Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"Сдайте дерево прежде чем закончить рабочий день", 3000);
                    return;
                }
                if (NAPI.Data.GetEntityData(player, "ON_WORK") != false)
                {
                    Customization.ApplyCharacter(player);
                    //Main.Players[player].WorkID = 0;
                    player.SetData("ON_WORK", false);
                    player.SetData("ON_WORK2", 0);
                    player.SetData("PLAYER_WORKING", 0);
                    Trigger.ClientEvent(player, "deleteCheckpoint", 15);
                    Trigger.ClientEvent(player, "deleteWorkBlip");
                    player.SetData("WOOD", 0);
                    player.SetData("LUMBER_AXE", false);

                    player.StopAnimation();
                    Main.OffAntiAnim(player);
                    BasicSync.DetachObject(player);
                    MoneySystem.Wallet.Change(player, player.GetData<int>("PAYMENT"));

                    Trigger.ClientEvent(player, "CloseJobStatsInfoW", player.GetData<int>("PAYMENT"));
                    Notify.Send(player, NotifyType.Success, NotifyPosition.BottomCenter, $"+ {player.GetData<int>("PAYMENT")}$", 3000);
                    player.SetData("PAYMENT", 0);
                    Notify.Send(player, NotifyType.Success, NotifyPosition.BottomCenter, $"Вы закончили рабочий день.", 3000);
                }
                else
                {
                    Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"Вы уже не работаете", 3000);
                }
            }

        }
        #endregion

        #region Before work
        public static void GiveLicense(Player player)
        {
            if (Main.Players[player].Licenses[8])
            {
                Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"У вас уже есть лицензия на рубку леса.", 3000);
            }
            else if (Main.Players[player].Money >= 200)
            {
                Main.Players[player].Licenses[8] = true;
                MoneySystem.Wallet.Change(player, -200);
                Notify.Send(player, NotifyType.Success, NotifyPosition.BottomCenter, $"Вам была выданна лицензия на рубку леса.", 3000);
            }
            else
            {
                Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"У Вас не хватает " + (200 - Main.Players[player].Money) + "$ на покупку лицензии на рубку леса", 3000);
            }
        }
        #endregion 

        #region Когда заходишь в чекпоинт рубки дерева
        private static void PlayerEnterCheckpoint(ColShape shape, Player player)
        {
            try
            {
                if (player.IsInVehicle) return;
                if (!Main.Players.ContainsKey(player)) return;
                if (!player.GetData<bool>("ON_WORK")) return;

                if (player.GetData<int>("WOOD") == 0)
                {
                    if (shape.GetData<int>("NUMBER2") == player.GetData<int>("WORKCHECK"))
                    {
                        player.SetData("WORKCHECK", -1);
                        NAPI.Entity.SetEntityPosition(player, Checkpoints[shape.GetData<int>("NUMBER2")].Position + new Vector3(0, 0, 0));
                        NAPI.Entity.SetEntityRotation(player, new Vector3(0, 0, Checkpoints[shape.GetData<int>("NUMBER2")].Heading));
                        Main.OnAntiAnim(player);
                        Trigger.ClientEvent(player, "OpenMiniGameLumber"); //Mini game


                    }
                }

            }
            catch (Exception e) { Log.Write("PlayerEnterCheckpoint: " + e.Message, nLog.Type.Error); }
        }

        [RemoteEvent("EndGameLumber")]
        private static void Client_EndGameLumber(Player player)
        {
            if (!Main.Players.ContainsKey(player)) return;
            if (!player.GetData<bool>("ON_WORK")) return;

            if (player.GetData<int>("WOOD") == 0)
            {
                    Main.OnAntiAnim(player);
                    player.SetData("WOOD", player.GetData<int>("WOOD") + 1);
                    player.PlayAnimation("melee@large_wpn@streamed_core", "ground_attack_on_spot", 47);

                    NAPI.Task.Run(() =>
                    {
                        try
                        {
                            BasicSync.AttachObjectToPlayer(player, NAPI.Util.GetHashKey("prop_fncwood_14a"), 18905, new Vector3(0.15, 0.1, -0.2), new Vector3(10, 10, 0));
                            player.PlayAnimation("anim@heists@box_carry@", "idle", 49);
                            Notify.Send(player, NotifyType.Success, NotifyPosition.BottomCenter, $"Вы срубили дерево.", 3000);

                            var payment = Convert.ToInt32(checkpointPayment * Group.GroupPayAdd[Main.Accounts[player].VipLvl] * Main.oldconfig.PaydayMultiplier);
                            player.SetData("PAYMENT", player.GetData<int>("PAYMENT") + payment);

                            var check = WorkManager.rnd.Next(0, Checkpoints2.Count - 1);
                            player.SetData("WORKCHECK", check);
                            Trigger.ClientEvent(player, "createCheckpoint", 15, 1, Checkpoints2[check].Position - new Vector3(0, 0, 0.7), 4, 0, 255, 0, 0);
                            Trigger.ClientEvent(player, "createWorkBlip", Checkpoints2[check].Position);
                        }
                        catch { }
                    }, 5000);
                }
        }
        #endregion
        #region Когда игрок заходит в чекпоинт сдачи деревьев
        public void ReceptionPoint(Player player)
        {
            if (player.IsInVehicle) return;
            if (!player.GetData<bool>("ON_WORK")) return;
            if (player.GetData<int>("WOOD") == 1)
            {
                player.SetData("WOOD", player.GetData<int>("WOOD") - 1);

                Trigger.ClientEvent(player, "JobStatsInfoW", player.GetData<int>("PAYMENT"));

                Main.OnAntiAnim(player);
                player.PlayAnimation("anim@mp_snowball", "pickup_snowball", 0);

                player.SetData("WORKCHECK", -1);
                Main.Players[player].Lumberjack = Main.Players[player].Lumberjack+1;

                if (Main.Players[player].Lumberjack >= lumberjack1 && Main.Players[player].Woodlevel == 0)
                {
                    Notify.Send(player, NotifyType.Success, NotifyPosition.BottomCenter, $"Вы достигли 1 уровня рубки леса.", 1500);
                    Main.Players[player].Woodlevel = 1;
                }

                if (Main.Players[player].Lumberjack >= lumberjack2 && Main.Players[player].Sawdust >= sawdust1 && Main.Players[player].Woodlevel == 1)
                {
                    Notify.Send(player, NotifyType.Success, NotifyPosition.BottomCenter, $"Вы достигли 2 уровня рубки леса.", 1500);
                    Main.Players[player].Woodlevel = 2;
                }

                NAPI.Task.Run(() =>
                {
                    try
                    {
                        if (player != null && Main.Players.ContainsKey(player))
                        {
                            BasicSync.DetachObject(player);
                            player.StopAnimation();
                            Main.OffAntiAnim(player);

                            var check = WorkManager.rnd.Next(0, Checkpoints.Count - 1);
                            player.SetData("WORKCHECK", check);
                            BasicSync.AttachObjectToPlayer(player, NAPI.Util.GetHashKey("prop_tool_fireaxe"), 18905, new Vector3(0.1, 0, 0), new Vector3(-90, 180, 0));
                            Notify.Send(player, NotifyType.Success, NotifyPosition.BottomCenter, $"Вы сдали дерево.", 3000);
                            Trigger.ClientEvent(player, "createCheckpoint", 15, 1, Checkpoints[check].Position - new Vector3(0, 0, 0.7), 2, 0, 255, 0, 0);
                            Trigger.ClientEvent(player, "createWorkBlip", Checkpoints[check].Position);
                        }
                    }
                    catch { }
                }, 3000);
            }
        }
    #endregion 



        #region Если игрок умер
        public static void Event_PlayerDeath(Player player, Player entityKiller, uint weapon)
        {
            try
            {
                if (player.GetData<int>("PLAYER_WORKING") != JobWorkId) return;
                if (!Main.Players.ContainsKey(player)) return;
                if (player.GetData<bool>("ON_WORK"))
                {
                    Customization.ApplyCharacter(player);
                    Main.Players[player].WorkID = 0;
                    player.SetData("ON_WORK", false);
                    player.SetData("ON_WORK2", 0);
                    player.SetData("PLAYER_WORKING", 0);
                    Trigger.ClientEvent(player, "deleteCheckpoint", 15);
                    Trigger.ClientEvent(player, "deleteWorkBlip");
                    player.SetData("WOOD", 0);

                    player.StopAnimation();
                    Main.OffAntiAnim(player);
                    BasicSync.DetachObject(player);
                    //MoneySystem.Wallet.Change(player, player.GetData<int>("PAYMENT"));

                    Trigger.ClientEvent(player, "CloseJobStatsInfoW", player.GetData<int>("PAYMENT"));
                    Notify.Send(player, NotifyType.Success, NotifyPosition.BottomCenter, $"+ {player.GetData<int>("PAYMENT")}$", 3000);
                    player.SetData("PAYMENT", 0);
                }
            }
            catch (Exception e) { Log.Write("PlayerDeath: " + e.Message, nLog.Type.Error); }
        }
        #endregion
        #region Если игрок вышел из игры или его кикнуло
        public static void Event_PlayerDisconnected(Player player, DisconnectionType type, string reason)
        {
            try
            {
                if (player.GetData<int>("PLAYER_WORKING") != JobWorkId) return;
                if (player.GetData<bool>("ON_WORK"))
                {
                    Customization.ApplyCharacter(player);
                    Main.Players[player].WorkID = 0;
                    player.SetData("ON_WORK", false);
                    player.SetData("ON_WORK2", 0);
                    player.SetData("PLAYER_WORKING", 0);
                    Trigger.ClientEvent(player, "deleteCheckpoint", 15);
                    Trigger.ClientEvent(player, "deleteWorkBlip");
                    player.SetData("WOOD", 0);

                    player.StopAnimation();
                    Main.OffAntiAnim(player);
                    BasicSync.DetachObject(player);
                    //MoneySystem.Wallet.Change(player, player.GetData<int>("PAYMENT"));
                    player.SetData("PAYMENT", 0);
                }
            }
            catch (Exception e) { Log.Write("PlayerDisconnected: " + e.Message, nLog.Type.Error); }
        }
        #endregion
        internal class Checkpoint
        {
            public Vector3 Position { get; }
            public double Heading { get; }

            public Checkpoint(Vector3 pos, double rot)
            {
                Position = pos;
                Heading = rot;
            }
        }
    }
}
