using GTANetworkAPI;
using System.Collections.Generic;
using System;
using NeptuneEvo.GUI;
using NeptuneEvo.Core;
using Redage.SDK;
using System.Timers;

namespace NeptuneEvo.Jobs
{
    class Sawmill : Script
    {
        //Зарплата (payment)
        private static int Sawdust_checkpointPayment = 25; //Тоскает опилки 
        private static int Woodworker_checkpointPayment = 15; //Обрабатывает дерево
        private static int Loader_checkpointPayment = 50; //Перевозит доски погрузчиком
        private static int checkpointPayment = 15 - 50;

        private static int LoaderRentCost = 70; //Цена аренда погрузчика

        private static int JobWorkId = 14;
        private static int JobsMinLVL = 2;

        private static int lumberjack1 = 25; // 1 уровень

        private static int lumberjack2 = 35; // 2 уровень
        private static int sawdust1 = 30;

        private static int sawdust2 = 40; // 3 уровень
        private static int woods1 = 20;

        private static int woods2 = 40; // 4 уровень
        private static int loader = 20;

        private static nLog Log = new nLog("Sawmill");

        [ServerEvent(Event.ResourceStart)]
        public void Event_ResourceStart()
        {
            try
            {
                NAPI.Blip.CreateBlip(479, new Vector3(-605.5319, 5304.9424, 70.3977), 1, 65, Main.StringToU16("Лесообработка"), 255, 0, true, 0, 0); // Блип на карте
                NAPI.TextLabel.CreateTextLabel("~w~Лесообработка", new Vector3(-605.5319, 5304.9424, 71.3977), 30f, 0.3f, 0, new Color(255, 255, 255), true, NAPI.GlobalDimension); // Над головой у бота
                NAPI.Marker.CreateMarker(1, new Vector3(-604.20294, 5305.234, 69.856735) - new Vector3(0, 0, 0.7), new Vector3(), new Vector3(), 1, new Color(255, 255, 255, 220)); //Начать рабочий день маркер
                var col = NAPI.ColShape.CreateCylinderColShape(new Vector3(-604.57837, 5305.449, 70.387), 4, 2, 0); // Меню которое открывается на 'E'

                NAPI.TextLabel.CreateTextLabel("~w~Склад", new Vector3(-576.2773, 5367.021, 71.24318), 30f, 0.3f, 0, new Color(255, 255, 255), true, NAPI.GlobalDimension); // Над головой у бота
                NAPI.Marker.CreateMarker(1, new Vector3(-575.8721, 5367.2817, 69.74155) - new Vector3(0, 0, 0.7), new Vector3(), new Vector3(), 1, new Color(255, 255, 255, 220));
                var warehouse = NAPI.ColShape.CreateCylinderColShape(new Vector3(-575.8721, 5367.2817, 70.24155), 4, 2, 0); // Меню которое открывается на 'E'

                col.OnEntityEnterColShape += (shape, player) => {
                    try
                    {
                        player.SetData("INTERACTIONCHECK", 511);
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
                warehouse.OnEntityEnterColShape += (shape, player) => {
                    try
                    {
                        player.SetData("INTERACTIONCHECK", 513);
                    }
                    catch (Exception ex) { Log.Write("col.OnEntityEnterColShape: " + ex.Message, nLog.Type.Error); }
                };
                warehouse.OnEntityExitColShape += (shape, player) => {
                    try
                    {
                        player.SetData("INTERACTIONCHECK", 0);
                    }
                    catch (Exception ex) { Log.Write("col.OnEntityExitColShape: " + ex.Message, nLog.Type.Error); }
                };

                //Знаю можно было сделать проще, но тогда не работает.
                int i = 0;
                foreach (var Check in Checkpoint1[0])
                {
                    col = NAPI.ColShape.CreateCylinderColShape(Check.Position, 1, 2, 0);
                    col.SetData("NUMBER2", i);
                    col.SetData("NUMBER3", -1);
                    col.OnEntityEnterColShape += PlayerEnterCheckpoint;
                    i++;
                };

                int ii = 0;
                foreach (var Check in Checkpoint2[0])
                {
                    col = NAPI.ColShape.CreateCylinderColShape(Check.Position, 1, 2, 0);
                    col.SetData("NUMBER3", ii);
                    col.SetData("NUMBER2", -1);
                    col.OnEntityEnterColShape += PlayerEnterCheckpoint;
                    ii++;
                };

                int iii = 0;
                foreach (var Check in Checkpoint1[1])
                {
                    col = NAPI.ColShape.CreateCylinderColShape(Check.Position, 1, 2, 0);
                    col.SetData("NUMBER2", iii);
                    col.SetData("NUMBER3", -1);
                    col.OnEntityEnterColShape += PlayerEnterCheckpoint;
                    iii++;
                };

                int iv = 0;
                foreach (var Check in Checkpoint2[1]) 
                {
                    col = NAPI.ColShape.CreateCylinderColShape(Check.Position, 3, 2, 0);
                    col.SetData("NUMBER3", iv);
                    col.SetData("NUMBER2", -1);
                    col.OnEntityEnterColShape += PlayerEnterCheckpoint;
                    col.OnEntityExitColShape += PlayerExitLoader;
                    iv++;
                };

                int vi = 0;
                foreach (var Check in Checkpoint1[2]) //Wood worker
                {
                    col = NAPI.ColShape.CreateCylinderColShape(Check.Position, 1, 2, 0);
                    col.SetData("NUMBER2", vi);
                    col.SetData("NUMBER3", -1);
                    col.OnEntityEnterColShape += PlayerEnterCheckpoint;
                    vi++;
                };
                for(int v = 0; v < Checkpoint1[1].Count; v++)
                {
                    Occupation.Add(false);
                }
                for (int x = 0; x < Checkpoint1[2].Count; x++)
                {
                    OccupationWW.Add(false);
                }
            }
            catch (Exception e) { Log.Write("ResourceStart: " + e.Message, nLog.Type.Error); }
        }

        #region Checkpoints
        private static List<List<Checkpoints>> Checkpoint1 = new List<List<Checkpoints>>() //Start
        {
            new List<Checkpoints>() //Sawdust
            {
                new Checkpoints(new Vector3(-538.8923, 5282.282, 73.92603), -168.55803), // 1 
                new Checkpoints(new Vector3(-556.1717, 5349.9756, 70.62163), -30.274675), // 2
                new Checkpoints(new Vector3(-556.37506, 5316.6978, 73.5239), 162.3881), // 3
                new Checkpoints(new Vector3(-558.86127, 5315.8613, 73.3715), -125.51743), // 4
            },

            new List<Checkpoints>() //Loader
            {
                new Checkpoints(new Vector3(-510.61246, 5245.4976, 79.80407), -92.23432), // 1
                new Checkpoints(new Vector3(-511.4358, 5263.879, 80.11027), -131.58507), // 2
                new Checkpoints(new Vector3(-494.45697, 5289.1055, 80.11013), 70.05595), // 3
                //new Checkpoints(new Vector3(-496.35532, 5264.3984, 80.12016), 151.7311), // 4

                new Checkpoints(new Vector3(-570.7643, 5243.111, 70.469124), 136.47896), // 5
                new Checkpoints(new Vector3(-569.3551, 5270.731, 70.26076), -26.275423), // 6
                new Checkpoints(new Vector3(-548.0669, 5274.7095, 74.11943), -25.650896), // 7
                new Checkpoints(new Vector3(-526.88275, 5287.43, 74.20102), -100.112495), // 8

                new Checkpoints(new Vector3(-529.2893, 5291.1997, 74.174324),-27.071836), // 9
                new Checkpoints(new Vector3(-589.35736, 5288.4365, 70.21441), -118.52163), // 10
                new Checkpoints(new Vector3(-576.82733, 5320.6987, 70.2145), -107.63543), // 11
                new Checkpoints(new Vector3(-494.95764, 5289.4663, 80.61011), -107.24582), // 12

                new Checkpoints(new Vector3(-535.72156, 5267.368, 74.13427), -111.84667), // 13
                new Checkpoints(new Vector3(-580.5296, 5248.818, 70.46849), 66.56665), // 14
            },

            new List<Checkpoints>() //Wood worker
            {
                new Checkpoints(new Vector3(-474.9436, 5318.283, 80.110054), -25.75977), // 1 
                new Checkpoints(new Vector3(-491.04858, 5302.2886, 80.110115), 152.67648), // 2
                new Checkpoints(new Vector3(-494.51242, 5295.8057, 80.11011), 76.5361), // 3
                new Checkpoints(new Vector3(-485.11667, 5295.6953, 80.11004), 62.876793), // 4
                new Checkpoints(new Vector3(-554.1201, 5371.5083, 69.8053), -103.42992), // 5
                new Checkpoints(new Vector3(-531.6383, 5372.681, 69.9111),172.75864), // 6
                new Checkpoints(new Vector3(-539.208, 5379.636, 69.94828), -21.668842), // 7
                new Checkpoints(new Vector3(-533.4566, 5394.6626, 70.17484), 157.60962), // 8
                new Checkpoints(new Vector3(-547.4342, 5373.6206, 69.98297), 106.47315), // 9
            },

        };
        private static List<List<Checkpoints>> Checkpoint2 = new List<List<Checkpoints>>() //End
        {
            new List<Checkpoints>() //Sawdust
            {
                new Checkpoints(new Vector3(-576.7787, 5289.48, 70.158801), -104.38103), // 1 
            },

            new List<Checkpoints>() //Loader
            {
                new Checkpoints(new Vector3(-601.4283, 5334.9536, 70.02276), -2.267969), // 1 
                new Checkpoints(new Vector3(-590.47424, 5366.4116, 70.07868), -13.005732), // 2
                new Checkpoints(new Vector3(-596.6956, 5322.8555, 69.8954), -36.113106), // 3
            },

        };
        private static List<List<Vector3>> Object = new List<List<Vector3>>() //Object carrier spawn (for loader)
        {
            new List<Vector3>() //Position
            {
                new Vector3(-507.0206, 5244.713, 79.304085), // 1
                new Vector3(-508.58917, 5263.133, 79.61027), // 2
                new Vector3(-492.52414, 5288.4165, 79.61013), // 3
                //new Vector3(-496.35532, 5264.3984, 79.62016), // 4
//5, 6, 7
                new Vector3(-572.4024, 5241.0703, 69.4875), // 5
                new Vector3(-568.1199, 5273.478, 69.24435), // 6
                new Vector3(-546.757, 5277.9307, 73.12673), // 7
                new Vector3(-523.3343, 5287.175, 73.17436), // 8

                new Vector3(-527.87415, 5293.577, 73.21896), // 9
                new Vector3(-586.7523, 5286.9756, 69.2715), // 10
                new Vector3(-574.1164, 5319.8774, 69.2145), // 11
                new Vector3(-491.9043, 5288.5874, 79.61011), // 12

                new Vector3(-532.49927, 5267.716, 73.18902), // 13
                new Vector3(-583.4242, 5250.1934, 69.47158), // 14
            },
            new List<Vector3>() //Rotation
            {
                new Vector3(0, 0, -110.16218), // 1
                new Vector3(0, 0, -97.70034), // 2
                new Vector3(0, 0, -119.31231), // 3
                //new Vector3(0, 0, -25.433178), // 4

                new Vector3(0, 0, 103.20547), // 5
                new Vector3(0, 0, -55.538201), // 6
                new Vector3(0, 0, -56.251716), // 7
                new Vector3(0, 0, 61.129276), // 8

                new Vector3(0, 0, -25.908737), // 9
                new Vector3(0, 0, -118.52163), // 10
                new Vector3(0, 0, -107.63543), // 11
                new Vector3(0, 0, -106.191574), // 12

                new Vector3(0, 0, 67.88444), // 13
                new Vector3(0, 0, 65.174355), // 14
            },

        };
        public static List<bool> Occupation = new List<bool>();
        public static List<bool> OccupationWW = new List<bool>();
        #endregion

        #region Меню которое нажимается на E
        public static void StartWorkDaySawmill(Player player)
        {
            if (player.IsInVehicle) return;
            if (Main.Players[player].LVL < JobsMinLVL)
            {
                Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"Необходим как минимум {JobsMinLVL} уровень", 3000);
                return;
            }

            Trigger.ClientEvent(player, "OpenSawmill", checkpointPayment, Main.Players[player].LVL, Main.Players[player].WorkID, NAPI.Data.GetEntityData(player, "ON_WORK2"));

        }
        #endregion
        #region Warehouse
        public static void WarehouseSawmill(Player player)
        {
            if (player.IsInVehicle) return;
            if (Main.Players[player].WorkID != JobWorkId)
            {
                Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"Вы не работаете на этой работе.", 3000);
                return;
            }
            if (NAPI.Data.GetEntityData(player, "ON_WORK") == false)
            {
                Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"Вы должны сначала начать рабочий день", 3000);
                return;
            }

            Trigger.ClientEvent(player, "OpenWarehouseSawmill", Main.Players[player].WorkID, NAPI.Data.GetEntityData(player, "ON_WORK2"), NAPI.Data.GetEntityData(player, "INSTRUMENT"));

        }
        [RemoteEvent("takeObjectWarehouse_Sawmill")]
        public static void ClientEvent_takeObjectWarehouse_Sawmill(Player client, int act)
        {
            try
            {
                switch (act)
                {
                    case -1:
                        Takeoff(client);
                        return;
                    case 1:
                        Shovel(client);
                        return;
                    case 2:
                        Saw(client);
                        return;
                    case 3:
                        Loaderkeys(client);
                        return;
                }
            }
            catch (Exception e) { Log.Write("jobjoin: " + e.Message, nLog.Type.Error); }
        }

        public static void Takeoff(Player player) //забрать предмет
        {
            if (NAPI.Data.GetEntityData(player, "INSTRUMENT") == 0)
            {
                Notify.Send(player, NotifyType.Info, NotifyPosition.BottomCenter, $"У вас нет ничего, что можно было бы положить на склад.", 3000);
            }
            else
            {
                Notify.Send(player, NotifyType.Info, NotifyPosition.BottomCenter, $"Вы положили предмет на склад.", 3000);
                player.StopAnimation();
                Main.OffAntiAnim(player);
                BasicSync.DetachObject(player);
                player.SetData("INSTRUMENT", 0);
                Trigger.ClientEvent(player, "CloseSawmill");
                Trigger.ClientEvent(player, "deleteCheckpoint", 15);
                Trigger.ClientEvent(player, "deleteWorkBlip");
                NAPI.Data.SetEntityData(player, "WaitSawmill", false);
                Occupation[player.GetData<int>("WORKCHECK")] = false;
                OccupationWW[player.GetData<int>("WORKCHECK")] = false;
            }
        }

        public static void Shovel(Player player)
        {
            if (NAPI.Data.GetEntityData(player, "ON_WORK2") == 1)
            { 
                if (NAPI.Data.GetEntityData(player, "INSTRUMENT") == 1 || NAPI.Data.GetEntityData(player, "INSTRUMENT") == 2 || NAPI.Data.GetEntityData(player, "INSTRUMENT") == 3)
                {
                    Trigger.ClientEvent(player, "CloseSawmill");
                    Notify.Send(player, NotifyType.Info, NotifyPosition.BottomCenter, $"У вас уже есть предмет, сдайте его прежде чем брать новый.", 3000);
                }
                else
                {
                    player.SetData("INSTRUMENT", 1);
                    Main.OnAntiAnim(player);
                    BasicSync.AttachObjectToPlayer(player, NAPI.Util.GetHashKey("prop_cs_trowel"), 4138, new Vector3(0, 0, 0), new Vector3(0, 0, 0));

                    Notify.Send(player, NotifyType.Info, NotifyPosition.BottomCenter, $"Вы взяли лопату со склада. Идите на первою точку.", 3000);
                    Trigger.ClientEvent(player, "CloseSawmill");
                    var check = WorkManager.rnd.Next(0, Checkpoint1[0].Count - 1);
                    player.SetData("WORKCHECK", check);
                    Trigger.ClientEvent(player, "createCheckpoint", 15, 1, Checkpoint1[0][check].Position - new Vector3(0, 0, 1.3), 2, 0, 255, 0, 0);
                    Trigger.ClientEvent(player, "createWorkBlip", Checkpoint1[0][check].Position);
                }
            }
            else
            {
                Notify.Send(player, NotifyType.Info, NotifyPosition.BottomCenter, $"Для вашей работы не требуется этот предмет.", 3000);
                Trigger.ClientEvent(player, "CloseSawmill");
            }
        }

        public static void Saw(Player player) 
        {
            if (NAPI.Data.GetEntityData(player, "ON_WORK2") == 2)
            {
                if (NAPI.Data.GetEntityData(player, "INSTRUMENT") == 1 || NAPI.Data.GetEntityData(player, "INSTRUMENT") == 2 || NAPI.Data.GetEntityData(player, "INSTRUMENT") == 3)
                {
                    Trigger.ClientEvent(player, "CloseSawmill");
                    Notify.Send(player, NotifyType.Info, NotifyPosition.BottomCenter, $"У вас уже есть предмет, сдайте его прежде чем брать новый.", 3000);
                }
                else
                {
                   
                    player.SetData("INSTRUMENT", 2);
                    Main.OnAntiAnim(player);
                    BasicSync.AttachObjectToPlayer(player, NAPI.Util.GetHashKey("prop_tool_consaw"), 26610, new Vector3(0.1, 0, 0), new Vector3(165, 0, 180)); //135
                    Notify.Send(player, NotifyType.Info, NotifyPosition.BottomCenter, $"Вы взяли пилу со склада. Идите на первую точку.", 3000);
                    Trigger.ClientEvent(player, "CloseSawmill");
                    var check = WorkManager.rnd.Next(0, Checkpoint1[2].Count - 1);
                    player.SetData("WORKCHECK", check);
                    Trigger.ClientEvent(player, "createCheckpoint", 15, 1, Checkpoint1[2][check].Position - new Vector3(0, 0, 1.3), 2, 0, 255, 0, 0);
                    Trigger.ClientEvent(player, "createWorkBlip", Checkpoint1[2][check].Position);
                }
            }
            else
            {
                Notify.Send(player, NotifyType.Info, NotifyPosition.BottomCenter, $"Для вашей работы не требуется этот предмет.", 3000);
                Trigger.ClientEvent(player, "CloseSawmill");
            }
}

        public static void Loaderkeys(Player player) 
        {
            if (NAPI.Data.GetEntityData(player, "ON_WORK2") == 3)
            {
                if (NAPI.Data.GetEntityData(player, "INSTRUMENT") == 1 || NAPI.Data.GetEntityData(player, "INSTRUMENT") == 2 || NAPI.Data.GetEntityData(player, "INSTRUMENT") == 3)
                {
                    Trigger.ClientEvent(player, "CloseSawmill");
                    Notify.Send(player, NotifyType.Info, NotifyPosition.BottomCenter, $"У вас уже есть предмет, сдайте его прежде чем брать новый.", 3000);
                }
                else
                {
                    player.SetData("INSTRUMENT", 3);
                    Trigger.ClientEvent(player, "CloseSawmill");
                    Notify.Send(player, NotifyType.Info, NotifyPosition.BottomCenter, $"Вы взяли ключи от погрузчика со склада. Идите на парковку.", 3000);
                    BasicSync.AttachObjectToPlayer(player, NAPI.Util.GetHashKey("p_car_keys_01"), 4169, new Vector3(0.05, 0, 0), new Vector3(0, 0, 0));
                    Trigger.ClientEvent(player, "JobStatsInfoW", player.GetData<int>("PAYMENT"));
                    Trigger.ClientEvent(player, "createWorkBlip", new Vector3(-603.42236, 5295.038, 70.269485)); //Парковка погрузчиков
                    Trigger.ClientEvent(player, "CloseSawmill");
                }
            }
            else
            {
                Notify.Send(player, NotifyType.Info, NotifyPosition.BottomCenter, $"Для вашей работы не требуется этот предмет.", 3000);
                Trigger.ClientEvent(player, "CloseSawmill");
            }
        }
        #endregion
        #region Устроться на работу
        [RemoteEvent("jobJoinSawmill")]
        public static void callback_jobsSelecting(Player client, int act)
        {
            try
            {
                switch (act)
                {
                    case -1:
                        Layoff(client);
                        return;
                    default:
                        JobJoin(client);
                        return;
                }
            }
            catch (Exception e) { Log.Write("jobjoin: " + e.Message, nLog.Type.Error); }
        }
        public static void Layoff(Player player)
        {
            if (NAPI.Data.GetEntityData(player, "ON_WORK") == true)
            {
                Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"Вы должны сначала закончить рабочий день", 3000);
                return;
            }
            if (Main.Players[player].WorkID != 0)
            {
                Main.Players[player].WorkID = 0;
                //Dashboard.sendStats(player);
                Notify.Send(player, NotifyType.Info, NotifyPosition.BottomCenter, $"Вы уволились с работы", 3000);
                var jobsid = Main.Players[player].WorkID;
                Trigger.ClientEvent(player, "secusejobSawmill", jobsid);
                return;
            }
            else
                Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"Вы никем не работаете", 3000);
        }
        public static void JobJoin(Player player)
        {
            if (NAPI.Data.GetEntityData(player, "ON_WORK") == true)
            {
                Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"Вы должны сначала закончить рабочий день", 3000);
                return;
            }
            if (Main.Players[player].WorkID != 0)
            {
                Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"Вы уже работаете: {Jobs.WorkManager.JobStats[Main.Players[player].WorkID - 1]}", 3000);
                return;
            }
            Main.Players[player].WorkID = JobWorkId;
            //Dashboard.sendStats(player);
            NAPI.Data.SetEntityData(player, "INSTRUMENT", 0);
            Notify.Send(player, NotifyType.Success, NotifyPosition.BottomCenter, $"Вы устроились на работу", 3000);
            var jobsid = Main.Players[player].WorkID;
            Trigger.ClientEvent(player, "secusejobSawmill", jobsid);
            return;
        }
        #endregion
        #region Начать рабочий день
        [RemoteEvent("enterJobSawmill")]
        public static void ClientEvent_Sawmill(Player client, int act)
        {
            try
            {
                switch (act)
                {
                    case -1:
                        Layoff2(client);
                        return;
                    case 1:
                        Sawdust(client, act);
                        return;
                    case 2:
                        Woodworker(client, act);
                        return;
                    case 3:
                        Loader(client, act);
                        return;
                }
            }
            catch (Exception e) { Log.Write("jobjoin: " + e.Message, nLog.Type.Error); }
        }
        public static void Layoff2(Player player)
        {
            if (player.GetData<int>("INSTRUMENT") == 0)
            {
                if (NAPI.Data.GetEntityData(player, "ON_WORK") != false)
                {
                    Customization.ApplyCharacter(player);
                    player.SetData("ON_WORK", false);
                    player.SetData("ON_WORK2", 0);
                    Trigger.ClientEvent(player, "deleteCheckpoint", 15);
                    Trigger.ClientEvent(player, "deleteWorkBlip");
                    Trigger.ClientEventInRange(player.Position, 550, "DestroyObjectLoader", player);
                    NAPI.Data.SetEntityData(player, "WaitSawmill", false);
                    Occupation[player.GetData<int>("WORKCHECK")] = false;
                    OccupationWW[player.GetData<int>("WORKCHECK")] = false;
                    player.SetData("WORKCHECK", -1);
                    player.SetData("PACKAGES", 0);

                    MoneySystem.Wallet.Change(player, player.GetData<int>("PAYMENT"));
                    Notify.Send(player, NotifyType.Info, NotifyPosition.BottomCenter, $"Вы закончили рабочий день.", 3000);
                    Trigger.ClientEvent(player, "CloseJobStatsInfoW", player.GetData<int>("PAYMENT"));
                    Notify.Send(player, NotifyType.Success, NotifyPosition.BottomCenter, $"+ {player.GetData<int>("PAYMENT")}$", 3000);
                    player.SetData("PAYMENT", 0);

                    var vehicle = NAPI.Data.GetEntityData(player, "WORK");
                    if (NAPI.Data.GetEntityData(vehicle, "TYPE") == "SAWMILL")
                    {
                        Timers.Stop(NAPI.Data.GetEntityData(player, "WORK_CAR_EXIT_TIMER"));
                        NAPI.Data.ResetEntityData(player, "WORK_CAR_EXIT_TIMER");
                        respawnCar(vehicle);
                    }
                    NAPI.Data.SetEntityData(player, "WORK", null);
                }
                else
                {
                    Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"Вы уже не работаете", 3000);
                }
            }
            else
            {
                Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"Сдайте инструмент или ключи обратно на склад прежде чем закончить рабочий день", 3000);
            }
        }
        #endregion
        #region Sawdust
        public static void Sawdust(Player player, int job)
        {
            if (Main.Players[player].WorkID != JobWorkId)
            {
                Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"Вы не работаете на этой работе.", 3000);
                return;
            }
            if (NAPI.Data.GetEntityData(player, "ON_WORK") == true)
            {
                Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"Вы должны сначала закончить рабочий день", 3000);
                return;
            }
            if (Main.Players[player].Woodlevel < 1)
            {
                Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"Вы должны достигнуть 1 уровня в рубке дерева. Нужно еще срубить {lumberjack1 - Main.Players[player].Lumberjack} деревьев.", 3000);
                return;
            }
            Customization.ClearClothes(player, Main.Players[player].Gender); 
                        if (Main.Players[player].Gender)
                        {
                            player.SetClothes(4, 98, 0);
                            player.SetClothes(6, 60, 0);
                            player.SetClothes(11, 253, 0);
                            player.SetClothes(3, 30, 5);
                        }
                        else
                        {
                            player.SetAccessories(0, 82, 0);
                            player.SetClothes(11, 252, 0);
                            player.SetClothes(6, 24, 0);
                            player.SetClothes(4, 47, 0);
                            player.SetClothes(3, 21, 5);
                        }

                Trigger.ClientEvent(player, "createWorkBlip", new Vector3(-576.2773, 5367.021, 70.24318)); //Склад
                player.SetData("WORKCHECK", -1);
                player.SetData("PACKAGES", 0);

                player.SetData("ON_WORK", true);
                player.SetData("ON_WORK2", job);
                Notify.Send(player, NotifyType.Success, NotifyPosition.BottomCenter, $"Вы начали рабочий день. Возьмите лопату на складе.", 3000);
                Trigger.ClientEvent(player, "JobStatsInfoW", player.GetData<int>("PAYMENT"));
                return;
        }
        #endregion
        #region Woodworker
        public static void Woodworker(Player player, int job)
        {
            if (Main.Players[player].WorkID != JobWorkId)
            {
                Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"Вы не работаете на этой работе.", 3000);
                return;
            }
            if (NAPI.Data.GetEntityData(player, "ON_WORK") == true)
            {
                Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"Вы должны сначала закончить рабочий день", 3000);
                return;
            }
            if (Main.Players[player].Woodlevel < 2)
            {
                Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"Вы должны достигнуть 2 уровня в рубке дерева. Нужно срубить еще {lumberjack2 - Main.Players[player].Lumberjack} деревьев и перенести еще {sawdust1 - Main.Players[player].Sawdust} опилок.", 3000);
                return;
            }
            Customization.ClearClothes(player, Main.Players[player].Gender);
            if (Main.Players[player].Gender)
            {
                player.SetAccessories(0, 145, 0);
                player.SetAccessories(1, 15, 0);
                player.SetClothes(6, 50, 0);
                player.SetClothes(4, 133, 0);
                player.SetClothes(11, 251, 0);
                player.SetClothes(3, 38, 5);
            }
            else
            {
                player.SetAccessories(0, 144, 0);
                player.SetClothes(3, 0, 0);
                player.SetClothes(8, 36, 0);
                player.SetClothes(11, 0, 0);
                player.SetClothes(4, 1, 5);
                player.SetClothes(6, 49, 0);
            }

            Trigger.ClientEvent(player, "createWorkBlip", new Vector3(-576.2773, 5367.021, 70.24318)); //Склад
            player.SetData("PACKAGES", 0);

            player.SetData("ON_WORK", true);
            player.SetData("ON_WORK2", job);
            Notify.Send(player, NotifyType.Success, NotifyPosition.BottomCenter, $"Вы начали рабочий день. Возьмите пилу на складе.", 3000);
            Trigger.ClientEvent(player, "JobStatsInfoW", player.GetData<int>("PAYMENT"));
            return;
        }
        #endregion
        #region Loader
        public static void Loader(Player player, int job)
        {
            if (Main.Players[player].WorkID != JobWorkId)
            {
                Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"Вы не работаете на этой работе.", 3000);
                return;
            }
            if (NAPI.Data.GetEntityData(player, "ON_WORK") == true)
            {
                Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"Вы должны сначала закончить рабочий день", 3000);
                return;
            }
            if (Main.Players[player].Woodlevel < 3)
            {
                Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"Вы должны достигнуть 3 уровня в рубке дерева. Нужно перенести еще {sawdust2 - Main.Players[player].Sawdust} опилок и обработать еще {woods1 - Main.Players[player].Woods} деревьев.", 3000);
                return;
            }
            Customization.ClearClothes(player, Main.Players[player].Gender);
            if (Main.Players[player].Gender)
            {
                player.SetAccessories(0, 21, 0);
                player.SetClothes(6, 61, 0);
                player.SetClothes(4, 34, 0);
                player.SetClothes(11, 171, 0);
                player.SetClothes(3, 44, 5);
            }
            else
            {
                player.SetAccessories(1, 19, 0);
                player.SetClothes(11, 172, 0);
                player.SetClothes(3, 7, 0);
                player.SetClothes(6, 63, 0);
                player.SetClothes(4, 141, 5);
            }

            Trigger.ClientEvent(player, "createWorkBlip", new Vector3(-576.2773, 5367.021, 70.24318)); //Склад
            player.SetData("PACKAGES", 0);

            player.SetData("ON_WORK", true);
            player.SetData("ON_WORK2", job);
            Notify.Send(player, NotifyType.Success, NotifyPosition.BottomCenter, $"Вы начали рабочий день. Возьмите ключи от погрузчика на складе.", 3000);
            return;
        }
        #endregion

        #region Когда заходишь в чекпоинт
        private static void PlayerEnterCheckpoint(ColShape shape, Player player)
        {
            try
            {
                if (!Main.Players.ContainsKey(player)) return;
                if (Main.Players[player].WorkID != JobWorkId || !player.GetData<bool>("ON_WORK")) return;

                if (NAPI.Data.GetEntityData(player, "ON_WORK2") == 1) //Sawdust
                {
                    if (player.IsInVehicle) return;
                    if (NAPI.Data.GetEntityData(player, "INSTRUMENT") == 1) //Проверка лопаты
                    {
                        if (player.GetData<int>("PACKAGES") == 0)
                        {
                            if (shape.GetData<int>("NUMBER2") == player.GetData<int>("WORKCHECK"))
                            {
                                player.SetData("WORKCHECK", -1);
                                NAPI.Entity.SetEntityPosition(player, Checkpoint1[0][shape.GetData<int>("NUMBER2")].Position + new Vector3(0, 0, 0));
                                NAPI.Entity.SetEntityRotation(player, new Vector3(0, 0, Checkpoint1[0][shape.GetData<int>("NUMBER2")].Heading));

                                Main.OnAntiAnim(player);
                                player.PlayAnimation("anim@mp_snowball", "pickup_snowball", 47);

                                NAPI.Task.Run(() =>
                                {
                                    try
                                    {
                                        player.StopAnimation();
                                        player.PlayAnimation("amb@world_human_janitor@male@base", "base", 49);
                                        BasicSync.AttachObjectToPlayer(player, NAPI.Util.GetHashKey("prop_cs_rub_binbag_01"), 57005, new Vector3(0.1, 0.0, 0.0), new Vector3(-90, 0, 0));
                                        Notify.Send(player, NotifyType.Success, NotifyPosition.BottomCenter, $"Вы взяли опилки.", 1500);
                                        player.SetData("PACKAGES", player.GetData<int>("PACKAGES") + 1);

                                        var check = WorkManager.rnd.Next(0, Checkpoint2[0].Count - 1);
                                        player.SetData("WORKCHECK", check);
                                        Trigger.ClientEvent(player, "createCheckpoint", 15, 1, Checkpoint2[0][check].Position - new Vector3(0, 0, 1.3), 2, 0, 255, 0, 0);
                                        Trigger.ClientEvent(player, "createWorkBlip", Checkpoint2[0][check].Position);
                                    }
                                    catch { }
                                }, 1300);
                            }
                        }
                        else
                        {
                            if (shape.GetData<int>("NUMBER3") == player.GetData<int>("WORKCHECK"))
                            {
                                NAPI.Entity.SetEntityPosition(player, Checkpoint2[0][shape.GetData<int>("NUMBER3")].Position + new Vector3(0, 0, 0));
                                NAPI.Entity.SetEntityRotation(player, new Vector3(0, 0, Checkpoint2[0][shape.GetData<int>("NUMBER3")].Heading));
                                player.PlayAnimation("anim@mp_snowball", "pickup_snowball", 47);
                                Main.OnAntiAnim(player);
                                player.SetData("WORKCHECK", -1);
                                NAPI.Task.Run(() =>
                                {
                                    try
                                    {
                                        player.StopAnimation();
                                        BasicSync.AttachObjectToPlayer(player, NAPI.Util.GetHashKey("prop_cs_trowel"), 4138, new Vector3(0, 0, 0), new Vector3(0, 0, 0));
                                        Notify.Send(player, NotifyType.Success, NotifyPosition.BottomCenter, $"Вы положили опилки.", 1500);
                                        player.SetData("PACKAGES", player.GetData<int>("PACKAGES") - 1);
                                        var payment = Convert.ToInt32(Sawdust_checkpointPayment * Group.GroupPayAdd[Main.Accounts[player].VipLvl] * Main.oldconfig.PaydayMultiplier);
                                        player.SetData("PAYMENT", player.GetData<int>("PAYMENT") + payment);
                                        Trigger.ClientEvent(player, "JobStatsInfoW", player.GetData<int>("PAYMENT"));

                                        Main.Players[player].Sawdust = Main.Players[player].Sawdust + 1;
                                        if (Main.Players[player].Lumberjack >= lumberjack2 && Main.Players[player].Sawdust >= sawdust1 && Main.Players[player].Woodlevel == 1)
                                        {
                                            Notify.Send(player, NotifyType.Success, NotifyPosition.BottomCenter, $"Вы достигли 2 уровня рубки леса.", 1500);
                                            Main.Players[player].Woodlevel = 2;
                                        }

                                        if (Main.Players[player].Woods >= woods1 && Main.Players[player].Sawdust >= sawdust2 && Main.Players[player].Woodlevel == 2)
                                        {
                                            Notify.Send(player, NotifyType.Success, NotifyPosition.BottomCenter, $"Вы достигли 3 уровня рубки леса.", 1500);
                                            Main.Players[player].Woodlevel = 3;
                                        }
                                        var check = WorkManager.rnd.Next(0, Checkpoint1[0].Count - 1);
                                        player.SetData("WORKCHECK", check);
                                        Trigger.ClientEvent(player, "createCheckpoint", 15, 1, Checkpoint1[0][check].Position - new Vector3(0, 0, 1.3), 2, 0, 255, 0, 0);
                                        Trigger.ClientEvent(player, "createWorkBlip", Checkpoint1[0][check].Position);

                                    }
                                    catch { }
                                }, 1000);
                            }
                        }
                    }
                }
                if (NAPI.Data.GetEntityData(player, "ON_WORK2") == 2) //Wood worker
                {
                    if (player.IsInVehicle) return;
                    if (NAPI.Data.GetEntityData(player, "INSTRUMENT") == 2) //Проверка пилы
                    {
                        if (shape.GetData<int>("NUMBER2") == player.GetData<int>("WORKCHECK"))
                        {
                            NAPI.Entity.SetEntityPosition(player, Checkpoint1[2][shape.GetData<int>("NUMBER2")].Position + new Vector3(0, 0, 0));
                            NAPI.Entity.SetEntityRotation(player, new Vector3(0, 0, Checkpoint1[2][shape.GetData<int>("NUMBER2")].Heading));
                            Main.OnAntiAnim(player);
                            Trigger.ClientEvent(player, "OpenMiniGameWW"); //Mini game
                        }
                    }
                    else
                    {
                        Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"У вас нет нужного инструмента. Возьмите его на складе.", 3000);
                        return;
                    }
                }
                if (NAPI.Data.GetEntityData(player, "ON_WORK2") == 3) //Loader
                {
                        if (!NAPI.Player.IsPlayerInAnyVehicle(player)) return;
                        var vehicle = player.Vehicle;
                        if (NAPI.Data.GetEntityData(vehicle, "TYPE") != "SAWMILL") return;

                    if (player.GetData<int>("PACKAGES") == 0)
                    {
                        if (shape.GetData<int>("NUMBER2") == player.GetData<int>("WORKCHECK"))
                        {   
                            Trigger.ClientEventInRange(player.Position, 550, "AttachObjectLoader", player);
                            Trigger.ClientEventInRange(player.Position, 550, "SetLoaderMass", player, 15000);
                            Occupation[player.GetData<int>("WORKCHECK")] = false;


                            Notify.Send(player, NotifyType.Success, NotifyPosition.BottomCenter, $"Отвезите доски на склад. Нажмите NUM8 или NUM5 на клавиатуре, чтобы поднять или опустить доски.", 1500);
                            player.SetData("PACKAGES", player.GetData<int>("PACKAGES") + 1);
                            player.SetData("WORKCHECK", -1);
                            var check = WorkManager.rnd.Next(0, Checkpoint2[1].Count - 1);
                            player.SetData("WORKCHECK", check);
                            Trigger.ClientEvent(player, "createCheckpoint", 15, 27, Checkpoint2[1][check].Position - new Vector3(0, 0, 0.3), 5, 0, 255, 0, 0); //Bigger checkpoint
                            Trigger.ClientEvent(player, "createWorkBlip", Checkpoint2[1][check].Position);

                        }
                    }
                    else
                    {
                        if (shape.GetData<int>("NUMBER3") == player.GetData<int>("WORKCHECK"))
                        {
                            Notify.Send(player, NotifyType.Warning, NotifyPosition.BottomCenter, $"Идет разгрузка. Подождите 5 секунд.", 3000);
                            NAPI.Data.SetEntityData(player, "WaitSawmill", true);
                            NAPI.Task.Run(() =>
                            {
                                try
                                {
                                    if (NAPI.Data.GetEntityData(player, "WaitSawmill") == false) return;
                                    NAPI.Data.SetEntityData(player, "WaitSawmill", false);
                                    player.SetData("WORKCHECK", -1);

                                    Trigger.ClientEventInRange(player.Position, 550, "DestroyObjectLoader", player);
                                    Trigger.ClientEventInRange(player.Position, 550, "SetLoaderMass", player, 5000);
                                    Notify.Send(player, NotifyType.Success, NotifyPosition.BottomCenter, $"Вы разгрузили доски.", 3000);
                                    player.SetData("PACKAGES", player.GetData<int>("PACKAGES") - 1);
                                    var payment = Convert.ToInt32(Loader_checkpointPayment * Group.GroupPayAdd[Main.Accounts[player].VipLvl] * Main.oldconfig.PaydayMultiplier);
                                    player.SetData("PAYMENT", player.GetData<int>("PAYMENT") + payment);
                                    Trigger.ClientEvent(player, "JobStatsInfoW", player.GetData<int>("PAYMENT"));

                                    Main.Players[player].Loader = Main.Players[player].Loader + 1;
                                    if (Main.Players[player].Woods >= woods2 && Main.Players[player].Loader >= loader && Main.Players[player].Woodlevel == 3)
                                    {
                                        Notify.Send(player, NotifyType.Success, NotifyPosition.BottomCenter, $"Вы достигли 4 уровня рубки леса.", 1500);
                                        Main.Players[player].Woodlevel = 4;
                                    }

                                    var check = WorkManager.rnd.Next(0, Checkpoint1[1].Count - 1);
                                    while (Occupation[check] == true)
                                    {
                                        check = WorkManager.rnd.Next(0, Checkpoint1[1].Count - 1);
                                    }
                                    Occupation[check] = true;
                                    player.SetData("WORKCHECK", check);
                                    Trigger.ClientEvent(player, "createCheckpoint", 15, 1, Checkpoint1[1][check].Position - new Vector3(0, 0, 1.3), 2, 0, 255, 0, 0);
                                    Trigger.ClientEvent(player, "createWorkBlip", Checkpoint1[1][check].Position);
                                    Trigger.ClientEventInRange(player.Position, 550, "CreateObjectLoader", player, Object[0][player.GetData<int>("WORKCHECK")]);
                                }
                                catch { }
                            }, 5000);

                        }
                    }
                }


            }
            catch (Exception e) { Log.Write("PlayerEnterCheckpoint: " + e.Message, nLog.Type.Error); }
        }
        private static void PlayerExitLoader(ColShape shape, Player player)
        {
            if (NAPI.Data.GetEntityData(player, "WaitSawmill") == false) return;
            Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"Вы покинули пункт разгрузки.", 3000);
            NAPI.Data.SetEntityData(player, "WaitSawmill", false);
        }

        [RemoteEvent("EndGameWW")]
        private static void Client_EndGameWW(Player player)
        {
            var check = WorkManager.rnd.Next(0, Checkpoint1[2].Count - 1);
            while (OccupationWW[check] == true)
            {
                check = WorkManager.rnd.Next(0, Checkpoint1[2].Count - 1);
            }
            OccupationWW[player.GetData<int>("WORKCHECK")] = false;
            player.SetData("WORKCHECK", -1);
            Main.OnAntiAnim(player);
            player.PlayAnimation("missheistfbi_fire", "break_door_a", 47); //cutting wood animation 

            NAPI.Task.Run(() =>
            {
                try
                {
                    Notify.Send(player, NotifyType.Success, NotifyPosition.BottomCenter, $"Вы обработали дерево.", 1500);
                    player.SetData("PACKAGES", player.GetData<int>("PACKAGES") + 1);
                    var payment = Convert.ToInt32(Woodworker_checkpointPayment * Group.GroupPayAdd[Main.Accounts[player].VipLvl] * Main.oldconfig.PaydayMultiplier);
                    player.SetData("PAYMENT", player.GetData<int>("PAYMENT") + payment);
                    Trigger.ClientEvent(player, "JobStatsInfoW", player.GetData<int>("PAYMENT"));
                    player.StopAnimation();
                    Main.OffAntiAnim(player);

                    Main.Players[player].Woods = Main.Players[player].Woods + 1;
                    if (Main.Players[player].Woods >= woods1 && Main.Players[player].Sawdust >= sawdust2 && Main.Players[player].Woodlevel == 2)
                    {
                        Notify.Send(player, NotifyType.Success, NotifyPosition.BottomCenter, $"Вы достигли 3 уровня рубки леса.", 1500);
                        Main.Players[player].Woodlevel = 3;
                    }

                    if (Main.Players[player].Woods >= woods2 && Main.Players[player].Loader >= loader && Main.Players[player].Woodlevel == 3)
                    {
                        Notify.Send(player, NotifyType.Success, NotifyPosition.BottomCenter, $"Вы достигли 4 уровня рубки леса.", 1500);
                        Main.Players[player].Woodlevel = 4;
                    }
                    OccupationWW[check] = true;

                    Trigger.ClientEvent(player, "createCheckpoint", 15, 1, Checkpoint1[2][check].Position - new Vector3(0, 0, 1.3), 2, 0, 255, 0, 0);
                    Trigger.ClientEvent(player, "createWorkBlip", Checkpoint1[2][check].Position);
                    player.SetData("WORKCHECK", check);
                }
                catch { }
            }, 5000);
        }
        #endregion

            #region vehicle spawn
        public static List<CarInfo> CarInfos = new List<CarInfo>();
        public static void sawmillCarsSpawner()
        {
            for (int a = 0; a < CarInfos.Count; a++)
            {
                var veh = NAPI.Vehicle.CreateVehicle(CarInfos[a].Model, CarInfos[a].Position, CarInfos[a].Rotation.Z, CarInfos[a].Color1, CarInfos[a].Color2, CarInfos[a].Number);
                Core.VehicleStreaming.SetEngineState(veh, false);
                NAPI.Data.SetEntityData(veh, "ACCESS", "WORK");
                NAPI.Data.SetEntityData(veh, "WORK", 14);
                NAPI.Data.SetEntityData(veh, "TYPE", "SAWMILL");
                NAPI.Data.SetEntityData(veh, "NUMBER", a);
                NAPI.Data.SetEntityData(veh, "ON_WORK", false);
                NAPI.Data.SetEntityData(veh, "DRIVER", null);
                veh.SetSharedData("PETROL", VehicleManager.VehicleTank[veh.Class]);
            }
        }

        public static void respawnCar(Vehicle veh)
        {
            try
            {
                int i = NAPI.Data.GetEntityData(veh, "NUMBER");
                NAPI.Entity.SetEntityPosition(veh, CarInfos[i].Position);
                NAPI.Entity.SetEntityRotation(veh, CarInfos[i].Rotation);
                VehicleManager.RepairCar(veh);
                Core.VehicleStreaming.SetEngineState(veh, false);
                Core.VehicleStreaming.SetLockStatus(veh, false);
                NAPI.Data.SetEntityData(veh, "WORK", 14);
                NAPI.Data.SetEntityData(veh, "TYPE", "SAWMILL");
                NAPI.Data.SetEntityData(veh, "NUMBER", i);
                NAPI.Data.SetEntityData(veh, "ON_WORK", false);
                NAPI.Data.SetEntityData(veh, "ACCESS", "WORK");
                NAPI.Data.SetEntityData(veh, "DRIVER", null);
                veh.SetSharedData("PETROL", VehicleManager.VehicleTank[veh.Class]);
            }
            catch (Exception e) { Log.Write("respawnCar: " + e.Message, nLog.Type.Error); }
        }

        #region если игрок садится в машину
        [ServerEvent(Event.PlayerEnterVehicle)]
        public void onPlayerEnterVehicleHandler(Player player, Vehicle vehicle, sbyte seatId)
        {
            try
            {
                if (NAPI.Data.GetEntityData(vehicle, "TYPE") != "SAWMILL") return;
                if (Main.Players[player].WorkID != JobWorkId)
                {
                    Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"Вы не работник лесообработки.", 3000);
                    VehicleManager.WarpPlayerOutOfVehicle(player);
                    return;
                }
                if (player.VehicleSeat == 0)
                {
                    if (Main.Players[player].LVL < JobsMinLVL)
                    {
                        Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"Необходим как минимум {JobsMinLVL} уровень", 3000);
                        return;
                    }
                    if (!Main.Players[player].Licenses[2])
                    {
                        Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"У Вас нет лицензии категории C", 3000);
                        VehicleManager.WarpPlayerOutOfVehicle(player);
                        return;
                    }   
                    if (NAPI.Data.GetEntityData(vehicle, "DRIVER") == null)
                    {
                        if (NAPI.Data.GetEntityData(player, "WORK") == null)
                        {

                            if (NAPI.Data.GetEntityData(player, "ON_WORK2") == 3) //Loader
                            {
                                if (NAPI.Data.GetEntityData(player, "INSTRUMENT") == 3) //Проверка ключа
                                {
                                    if (Main.Players[player].Money >= LoaderRentCost)
                                    {
                                        Trigger.ClientEvent(player, "openDialog", "SAWMILL_RENT", $"Арендовать погрузчик за ${LoaderRentCost}?");
                                    }
                                    else
                                    {
                                        Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"У Вас не хватает " + (LoaderRentCost - Main.Players[player].Money) + "$ на аренду погрузчика", 3000);
                                        VehicleManager.WarpPlayerOutOfVehicle(player);
                                    }
                                }
                                else
                                {
                                    Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"У вас нет ключей от погрузчика. Возьмите их на складе.", 3000);
                                    VehicleManager.WarpPlayerOutOfVehicle(player);
                                }
                            }

                            else
                            {
                                Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"Вам не требуется данный транспорт.", 3000);
                                VehicleManager.WarpPlayerOutOfVehicle(player);
                            }
                        }
                        else
                        {
                            Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"У Вас уже есть арендованный транспорт.", 3000);
                            VehicleManager.WarpPlayerOutOfVehicle(player);
                        }
                    }
                    else
                    {
                        if (NAPI.Data.GetEntityData(player, "WORK") != vehicle)
                        {
                            Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"У транспорта уже есть водитель", 3000);
                            VehicleManager.WarpPlayerOutOfVehicle(player);
                        }
                        else if (NAPI.Data.GetEntityData(player, "INSTRUMENT") == 3)
                            NAPI.Data.SetEntityData(player, "IN_WORK_CAR", true);
                        else
                            VehicleManager.WarpPlayerOutOfVehicle(player);
                    }
                }
                else
                {
                    Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"У транспорта уже есть водитель.", 3000);
                    VehicleManager.WarpPlayerOutOfVehicle(player);
                }
            }
            catch (Exception e) { Log.Write("PlayerEnterVehicle: " + e.Message, nLog.Type.Error); }
        }
        public static void acceptSawmillRent(Player player)
        {
            var vehicle = player.Vehicle;
            NAPI.Data.SetEntityData(vehicle, "ON_WORK", true);
            NAPI.Data.SetEntityData(vehicle, "DRIVER", player);
            NAPI.Data.SetEntityData(player, "WORK", vehicle);
            Core.VehicleStreaming.SetEngineState(vehicle, true);
            Trigger.ClientEventInRange(player.Position, 550, "SetLoaderMass", player, 5000);
            Notify.Send(player, NotifyType.Info, NotifyPosition.BottomCenter, $"Вы начали работу перевозчиком досок на погрузчике. Езжайте на красную точку, чтобы забрать доски.", 3000);

            MoneySystem.Wallet.Change(player, -LoaderRentCost);
            GameLog.Money($"player({Main.Players[player].UUID})", $"server", LoaderRentCost, $"sawmill_loaderRent");
                
            NAPI.Data.SetEntityData(player, "IN_WORK_CAR", true);
            var check = WorkManager.rnd.Next(0, Checkpoint1[1].Count - 1);
            player.SetData("WORKCHECK", check);
            Trigger.ClientEventInRange(player.Position, 550, "CreateObjectLoader", player, Object[0][player.GetData<int>("WORKCHECK")]);
            Trigger.ClientEventInRange(player.Position, 550, "SyncPoint", player, check, Checkpoint1[1].Count);

            Trigger.ClientEvent(player, "createCheckpoint", 15, 1, Checkpoint1[1][check].Position - new Vector3(0, 0, 1.3), 2, 0, 255, 0, 0);
            Trigger.ClientEvent(player, "createWorkBlip", Checkpoint1[1][check].Position);

        }
        #endregion

        #region Вышел из машины
        [ServerEvent(Event.PlayerExitVehicle)]
        public void onPlayerExitVehicleHandler(Player player, Vehicle vehicle)
        {
            try
            {
                if (NAPI.Data.GetEntityData(vehicle, "TYPE") == "SAWMILL" &&
                NAPI.Data.GetEntityData(player, "ON_WORK") &&
                NAPI.Data.GetEntityData(player, "WORK") == vehicle)
                {
                    Notify.Send(player, NotifyType.Warning, NotifyPosition.BottomCenter, $"Если Вы не сядете в транспорт через 60 секунд, то рабочий день закончится", 3000);
                    BasicSync.AttachObjectToPlayer(player, NAPI.Util.GetHashKey("p_car_keys_01"), 4169, new Vector3(0.05, 0, 0), new Vector3(0, 0, 0));
                    NAPI.Data.SetEntityData(player, "IN_WORK_CAR", false);
                    if (player.HasData("WORK_CAR_EXIT_TIMER"))
                    Timers.Stop(NAPI.Data.GetEntityData(player, "WORK_CAR_EXIT_TIMER"));
                    NAPI.Data.SetEntityData(player, "CAR_EXIT_TIMER_COUNT", 0);
                    NAPI.Data.SetEntityData(player, "WORK_CAR_EXIT_TIMER", Timers.Start(1000, () => timber_playerExitWorkVehicle(player, vehicle)));
                }
            }
            catch (Exception e) { Log.Write("PlayerExitVehicle: " + e.Message, nLog.Type.Error); }
        }
        private void timber_playerExitWorkVehicle(Player player, Vehicle vehicle)
        {
            NAPI.Task.Run(() =>
            {
                try
                {
                    if (!player.HasData("WORK_CAR_EXIT_TIMER")) return;
                    if (NAPI.Data.GetEntityData(player, "IN_WORK_CAR")) //Если игрок зашел обратно в транспорт 
                    {
                        Timers.Stop(NAPI.Data.GetEntityData(player, "WORK_CAR_EXIT_TIMER"));
                        NAPI.Data.ResetEntityData(player, "WORK_CAR_EXIT_TIMER");
                        return;
                    }
                    if (NAPI.Data.GetEntityData(player, "CAR_EXIT_TIMER_COUNT") > 60)
                    {
                        respawnCar(vehicle);
                        Notify.Send(player, NotifyType.Info, NotifyPosition.BottomCenter, $"Вы закончили рабочий день.", 3000);
                        Customization.ApplyCharacter(player);
                        BasicSync.DetachObject(player);
                        player.SetData("ON_WORK2", 0);
                        Trigger.ClientEvent(player, "deleteCheckpoint", 15);
                        Trigger.ClientEvent(player, "deleteWorkBlip");
                        Trigger.ClientEventInRange(player.Position, 550, "DestroyObjectLoader", player);
                        NAPI.Data.SetEntityData(player, "WaitSawmill", false);
                        Occupation[player.GetData<int>("WORKCHECK")] = false;
                        OccupationWW[player.GetData<int>("WORKCHECK")] = false;
                        player.SetData("WORKCHECK", -1);
                        Trigger.ClientEvent(player, "CloseJobStatsInfoW", player.GetData<int>("PAYMENT"));
                        NAPI.Data.SetEntityData(player, "INSTRUMENT", 0);
                        NAPI.Data.SetEntityData(player, "ON_WORK", false);
                        NAPI.Data.SetEntityData(player, "WORK", null);
                        Timers.Stop(NAPI.Data.GetEntityData(player, "WORK_CAR_EXIT_TIMER"));
                        NAPI.Data.ResetEntityData(player, "WORK_CAR_EXIT_TIMER");
                        Trigger.ClientEvent(player, "CloseJobStatsInfo", player.GetData<int>("PAYMENT"));
                        Notify.Send(player, NotifyType.Success, NotifyPosition.BottomCenter, $"+ {player.GetData<int>("PAYMENT")}$", 3000);
                        player.SetData("PAYMENT", 0);
                        return;
                    }
                    NAPI.Data.SetEntityData(player, "CAR_EXIT_TIMER_COUNT", NAPI.Data.GetEntityData(player, "CAR_EXIT_TIMER_COUNT") + 1);

                }
                catch (Exception e)
                {
                    Log.Write("Timer_PlayerExitWorkVehicle_Sawmill: \n" + e.ToString(), nLog.Type.Error);
                }
            });
        }

            #endregion

            #endregion
            #region Если игрок умер
            public static void Event_PlayerDeath(Player player, Player entityKiller, uint weapon)
        {
            try
            {
                if (!Main.Players.ContainsKey(player)) return;
                if (Main.Players[player].WorkID == JobWorkId && player.GetData<bool>("ON_WORK"))
                {
                    Customization.ApplyCharacter(player);
                    player.SetData("ON_WORK", false);
                    player.SetData("ON_WORK2", 0);
                    NAPI.Data.SetEntityData(player, "WORK", null);
                    Trigger.ClientEvent(player, "deleteCheckpoint", 15);
                    Trigger.ClientEvent(player, "deleteWorkBlip");
                    Trigger.ClientEventInRange(player.Position, 550, "DestroyObjectLoader", player);
                    NAPI.Data.SetEntityData(player, "WaitSawmill", false);
                    Occupation[player.GetData<int>("WORKCHECK")] = false;
                    OccupationWW[player.GetData<int>("WORKCHECK")] = false;
                    player.SetData("WORKCHECK", -1);
                    player.SetData("PACKAGES", 0);
                    player.SetData("INSTRUMENT", 0);
                    var vehicle = player.Vehicle;
                    respawnCar(vehicle);

                    player.StopAnimation();
                    Main.OffAntiAnim(player);
                    BasicSync.DetachObject(player);
                    //MoneySystem.Wallet.Change(player, player.GetData("PAYMENT"));

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
                if (Main.Players[player].WorkID == JobWorkId && player.GetData<bool>("ON_WORK"))
                {
                    Customization.ApplyCharacter(player);
                    player.SetData("ON_WORK", false);
                    player.SetData("ON_WORK2", 0);
                    NAPI.Data.SetEntityData(player, "WORK", null);
                    Trigger.ClientEvent(player, "deleteCheckpoint", 15);
                    Trigger.ClientEvent(player, "deleteWorkBlip");
                    Trigger.ClientEventInRange(player.Position, 550, "DestroyObjectLoader", player);
                    NAPI.Data.SetEntityData(player, "WaitSawmill", false);
                    Occupation[player.GetData<int>("WORKCHECK")] = false;
                    OccupationWW[player.GetData<int>("WORKCHECK")] = false;
                    player.SetData("WORKCHECK", -1);
                    player.SetData("PACKAGES", 0);
                    player.SetData("INSTRUMENT", 0);
                    var vehicle = player.Vehicle;
                    respawnCar(vehicle);

                    player.StopAnimation();
                    Main.OffAntiAnim(player);
                    BasicSync.DetachObject(player);
                    //MoneySystem.Wallet.Change(player, player.GetData("PAYMENT"));
                    player.SetData("PAYMENT", 0);
                }
            }
            catch (Exception e) { Log.Write("PlayerDisconnected: " + e.Message, nLog.Type.Error); }
        }
        #endregion
        internal class Checkpoints
        {
            public Vector3 Position { get; }
            public double Heading { get; }

            public Checkpoints(Vector3 pos, double rot)
            {
                Position = pos;
                Heading = rot;
            }
        }
    }
}
