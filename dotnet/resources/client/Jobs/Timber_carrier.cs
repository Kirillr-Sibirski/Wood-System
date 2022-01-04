using GTANetworkAPI;
using System.Collections.Generic;
using System;
using NeptuneEvo.GUI;
using NeptuneEvo.Core;
using Redage.SDK;
using System.Timers;

namespace NeptuneEvo.Jobs
{
    class Timber : Script
    {
        //Зарплата (payment)
        private static int Payment = 100;

        private static int RentCost = 200; //Цена аренды

        private static int JobWorkId = 16; //Номер работы
        private static int JobsMinLVL = 2;
        private static nLog Log = new nLog("Timber");

        private static int woods2 = 40; // 4 уровень
        private static int loader = 20;
        private static ColShape menu = null;
        [ServerEvent(Event.ResourceStart)]
        public void Event_ResourceStart()
        {
            try
            {
                NAPI.Blip.CreateBlip(85, new Vector3(-1358.9165, 4481.1807, 25.765453), 1, 25, Main.StringToU16("Лесовозчик"), 255, 0, true, 0, 0); // Блип на карте
                var lumberjack = NAPI.ColShape.CreateCylinderColShape(new Vector3(-1556.7715, 4487.309, 20.148733), 4, 2, 0); //парковка прицепов
                var sawmill = NAPI.ColShape.CreateCylinderColShape(new Vector3(-542.9006, 5376.614, 70.5823), 4, 2, 0); //лесообработка
                menu = NAPI.ColShape.CreateCylinderColShape(new Vector3(-1358.5245, 4479.773, 25.238703), 4, 2, 0); //ped
                NAPI.TextLabel.CreateTextLabel("~w~Арендовать грузовик", new Vector3(-1358.9165, 4481.1807, 26.765453), 30f, 0.3f, 0, new Color(255, 255, 255), true, NAPI.GlobalDimension); // Над головой у бота
                NAPI.Marker.CreateMarker(1, new Vector3(-1358.5245, 4479.773, 24.238703) - new Vector3(0, 0, 0.7), new Vector3(), new Vector3(), 1, new Color(255, 255, 255, 220));
                menu.OnEntityEnterColShape += (shape, player) => {
                    try
                    {
                        player.SetData("INTERACTIONCHECK", 522);
                    }
                    catch (Exception ex) { Log.Write("timber.OnEntityEnterColShape: " + ex.Message, nLog.Type.Error); }
                };
                menu.OnEntityExitColShape += (shape, player) => {
                    try
                    {
                        player.SetData("INTERACTIONCHECK", 0);
                    }
                    catch (Exception ex) { Log.Write("timber.OnEntityExitColShape: " + ex.Message, nLog.Type.Error); }
                };
                lumberjack.OnEntityEnterColShape += (shape, player) => {
                    try
                    {
                            LumberjackPark(player);
                    }
                    catch (Exception ex) { Log.Write("timber.OnEntityEnterColShape: " + ex.Message, nLog.Type.Error); }
                };

                sawmill.OnEntityEnterColShape += (shape, player) => {
                    try
                    {
                        SawmillPark(player);
                    }
                    catch (Exception ex) { Log.Write("timber.OnEntityEnterColShape: " + ex.Message, nLog.Type.Error); }
                };
                sawmill.OnEntityExitColShape += (shape, player) => {
                    try
                    {
                        if (!Main.Players.ContainsKey(player)) return;
                        if (!player.GetData<bool>("ON_WORK")) return;
                        if (!NAPI.Player.IsPlayerInAnyVehicle(player)) return;

                        var vehicle = player.Vehicle;
                        if (NAPI.Data.GetEntityData(vehicle, "TYPE") != "TIMBER") return;
                        if (NAPI.Data.GetEntityData(player, "WaitTrailer") != true) return;

                        Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"Вы вышли из зоны разгрузки. Для того, чтобы продолжить разгрузку войдите в зону разгрузки.", 3000);
                        NAPI.Data.SetEntityData(player, "WaitTrailer", false);

                    }
                    catch (Exception ex) { Log.Write("timber.OnEntityExitColShape: " + ex.Message, nLog.Type.Error); }
                };


            }
            catch (Exception e) { Log.Write("ResourceStart: " + e.Message, nLog.Type.Error); }
        }

        #region Checkpoints
        private static List<Checkpoint> Checkpoints1 = new List<Checkpoint>()
        {
            new Checkpoint(new Vector3(-1556.7715, 4487.309, 18.648733), 32.77933), // Стоянка прицепов
        };
        private static List<Checkpoint> Checkpoints2 = new List<Checkpoint>()
        {
            new Checkpoint(new Vector3(-542.9006, 5376.614, 69.5823), -118.9055), // Разгрузка
        };
        //Trailers
        private static List<Checkpoint> SpawnTrailers = new List<Checkpoint>()
        {
            new Checkpoint(new Vector3(-1570.4502, 4488.864, 21.867687), 17.762112), // 1
            new Checkpoint(new Vector3(-1566.4899, 4488.9897, 21.29847), 16.908405), // 2
            new Checkpoint(new Vector3(-1561.2106, 4486.384, 20.57094), 19.297632), // 3
        };
        private static List<Vector3> SpawnTrailersRot = new List<Vector3>()
        {
            new Vector3(0, 0, 39.935566), // 1
            new Vector3(0, 0, 13.934747), // 2
            new Vector3(0, 0, -100.31876), // 3
            new Vector3(0, 0, 51.686947), // 4
            new Vector3(0, 0, 3.3121088), // 5
        };
        private static List<int> LastTrailerSpawn = new List<int>()
        {
            0,
        };
        #endregion

        #region Когда заходишь в чекпоинт
        public static void LumberjackPark(Player player)
        {
            try
            {
                if (!Main.Players.ContainsKey(player)) return;
                if (!player.GetData<bool>("ON_WORK")) return;
                if (!NAPI.Player.IsPlayerInAnyVehicle(player)) return;

                var vehicle = player.Vehicle;
                if (NAPI.Data.GetEntityData(vehicle, "TYPE") != "TIMBER") return;
                if (player.GetData<int>("PACKAGES") == 0)
                {
                    Trigger.ClientEvent(player, "deleteCheckpoint", 3);
                    Notify.Send(player, NotifyType.Success, NotifyPosition.BottomCenter, $"Прицепите прицеп и езжайте на лесообработку. Если вы потеряете прицеп, пропишите команду /findtrailer.", 3000);
                    player.SetData("PACKAGES", 1);
                    player.SetData("WORKCHECK", -1);

                    var spawnI = 0;
                    do
                    {
                        spawnI = WorkManager.rnd.Next(0, SpawnTrailers.Count - 1); //Trailers 
                    } while (spawnI == LastTrailerSpawn[0]);
                    LastTrailerSpawn[0] = spawnI;

                    var trailer = NAPI.Vehicle.CreateVehicle(VehicleHash.Trailerlogs, SpawnTrailers[spawnI].Position, SpawnTrailersRot[spawnI].Z, 0, 0);
                    player.SetData("TRAILER", trailer);
                    

                    var check = WorkManager.rnd.Next(0, Checkpoints2.Count - 1);
                    player.SetData("WORKCHECK", check);
                    Trigger.ClientEvent(player, "createCheckpoint", 3, 1, Checkpoints2[check].Position, 5, 0, 255, 0, 0);
                    Trigger.ClientEvent(player, "createWorkBlip", Checkpoints2[check].Position);
                    Trigger.ClientEvent(player, "createWaypoint", Checkpoints2[check].Position.X, Checkpoints2[check].Position.Y);
                    NAPI.Data.SetEntityData(player, "TRAILER_ARG", true);
                }
            }
            catch (Exception e) { Log.Write("EnterLumberjackPark: " + e.Message, nLog.Type.Error); }
        }
        public static void SawmillPark(Player player)
        {
            if (!Main.Players.ContainsKey(player)) return;
            if (!player.GetData<bool>("ON_WORK")) return;
            if (!NAPI.Player.IsPlayerInAnyVehicle(player)) return;

            var vehicle = player.Vehicle;
            if (NAPI.Data.GetEntityData(vehicle, "TYPE") != "TIMBER") return;
        
            if (player.GetData<int>("PACKAGES") == 1) 
            {
                Vehicle trailer = player.GetData<Vehicle>("TRAILER");
                float X = trailer.Position.X - player.Position.X;
                float Y = trailer.Position.Y - player.Position.Y;
                float Z = trailer.Position.Z - player.Position.Z;
                if (X < 10 && X > -10 && Y < 20 && Y > -20 && Z < 3 && Z > -3)
                {
                    Notify.Send(player, NotifyType.Info, NotifyPosition.BottomCenter, $"Подождите 15 секунд пока прицеп разгружается.", 3000);
                    NAPI.Data.SetEntityData(player, "WaitTrailer", true);
                    NAPI.Task.Run(() =>
                    {
                        try
                        {
                            if (NAPI.Data.GetEntityData(player, "WaitTrailer") == false) return;
                            NAPI.Data.SetEntityData(player, "TRAILER_ARG", false);
                            player.SetData("WORKCHECK", -1);
                            Notify.Send(player, NotifyType.Success, NotifyPosition.BottomCenter, $"Прицеп был разгружен, можете ехать.", 3000);
                            NAPI.Data.SetEntityData(player, "WaitTrailer", false);
                            player.SetData("PACKAGES", 0);
                            var payment = Convert.ToInt32(Payment * Group.GroupPayAdd[Main.Accounts[player].VipLvl] * Main.oldconfig.PaydayMultiplier);
                            MoneySystem.Wallet.Change(player, payment);
                            NAPI.Task.Run(() =>
                            {
                                try
                                {
                                    if (player.HasData("TRAILER"))
                                    {
                                        NAPI.Entity.DeleteEntity(player.GetData<Vehicle>("TRAILER"));
                                        player.ResetData("TRAILER");
                                    }
                                }
                                catch { }
                            });
                            var check = WorkManager.rnd.Next(0, Checkpoints1.Count - 1);
                            player.SetData("WORKCHECK", check);
                            Trigger.ClientEvent(player, "createCheckpoint", 3, 1, Checkpoints1[check].Position, 5, 0, 255, 0, 0);
                            Trigger.ClientEvent(player, "createWorkBlip", Checkpoints1[check].Position);
                            Trigger.ClientEvent(player, "createWaypoint", Checkpoints1[check].Position.X, Checkpoints1[check].Position.Y);
                        }
                        catch { }
                    }, 15000);
                }
                else
                {
                    Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"У вас нет прицепа.", 3000);
                }
            }
        }

        #endregion

        #region vehicle spawn
        public static List<CarInfo> CarInfos = new List<CarInfo>();

        public static void respawnCar(Vehicle veh)
        {
            try
            {
                NAPI.Entity.DeleteEntity(veh);
                NAPI.Data.SetEntityData(menu, "CARS", NAPI.Data.GetEntityData(menu, "CARS") -1);
                /*int i = NAPI.Data.GetEntityData(veh, "NUMBER");
                NAPI.Entity.SetEntityPosition(veh, CarInfos[i].Position);
                NAPI.Entity.SetEntityRotation(veh, CarInfos[i].Rotation);
                VehicleManager.RepairCar(veh);
                Core.VehicleStreaming.SetEngineState(veh, false);
                Core.VehicleStreaming.SetLockStatus(veh, false);
                NAPI.Data.SetEntityData(menu, "CARS", -1);
                NAPI.Data.SetEntityData(veh, "WORK", 16);
                NAPI.Data.SetEntityData(veh, "TYPE", "TIMBER");
                NAPI.Data.SetEntityData(veh, "NUMBER", i);
                NAPI.Data.SetEntityData(veh, "ON_WORK", false);
                NAPI.Data.SetEntityData(veh, "ACCESS", "WORK");
                NAPI.Data.SetEntityData(veh, "DRIVER", null);
                veh.SetSharedData("PETROL", VehicleManager.VehicleTank[veh.Class]);*/
            }
            catch (Exception e) { Log.Write("respawnCar: " + e.Message, nLog.Type.Error); }
        }
        #endregion
        #region если игрок садится в машину
        [ServerEvent(Event.PlayerEnterVehicle)]
        public void onPlayerEnterVehicleHandler(Player player, Vehicle vehicle, sbyte seatid)
        {
            try
            {
                if (NAPI.Data.GetEntityData(vehicle, "TYPE") != "TIMBER") return;
                if (player.VehicleSeat == 0)
                {
                    if (!NAPI.Data.GetEntityData(vehicle, "ON_WORK"))
                    {
                        if (NAPI.Data.GetEntityData(player, "PLAYER_WORKING") != JobWorkId)
                        {
                            Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"Вы не работаете на этой работе.", 3000);
                            VehicleManager.WarpPlayerOutOfVehicle(player);
                        }
                        else
                        {
                            Core.VehicleStreaming.SetEngineState(vehicle, true);
                            NAPI.Data.SetEntityData(vehicle, "DRIVER", player);

                            var rnd = WorkManager.rnd.Next(0, Checkpoints1.Count - 1); //Trailers
                            Trigger.ClientEvent(player, "createCheckpoint", 3, 1, Checkpoints1[rnd].Position, 5, 0, 255, 0, 0);
                            Trigger.ClientEvent(player, "createWaypoint", Checkpoints1[rnd].Position.X, Checkpoints1[rnd].Position.Y);
                            Trigger.ClientEvent(player, "createWorkBlip", Checkpoints1[rnd].Position);
                            NAPI.Data.SetEntityData(player, "IN_WORK_CAR", true);
                        }
                    }
                    else
                    {
                        if (NAPI.Data.GetEntityData(player, "WORK") != vehicle)
                        {
                            Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"У транспорта уже есть водитель", 3000);
                            VehicleManager.WarpPlayerOutOfVehicle(player);
                        }
                        else
                            NAPI.Data.SetEntityData(player, "IN_WORK_CAR", true);
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
        public static void acceptTimberRent(Player player)
        {
            if (Main.Players[player].LVL < JobsMinLVL)
            {
                Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"Необходим как минимум {JobsMinLVL} уровень", 3000);
                return;
            }
            if (!Main.Players[player].Licenses[2])
            {
                Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"У Вас нет лицензии категории C", 3000);
                return;
            }
            if (Main.Players[player].Woodlevel < 4)
            {
                Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"Вы должны достигнуть 4 уровня в рубке дерева. Нужно обработать еще {woods2 - Main.Players[player].Woods} деревьев и перевезти {loader - Main.Players[player].Loader} досок на погрузчике.", 3000);
                return;
            }
            if (NAPI.Data.GetEntityData(menu, "CARS") >= 30)
            {
                Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"На сервере заспавнено слишком много лесовозов, лимит 30 машин.", 3000);
                return;
            }
            Vehicle vehicle = player.GetData<Vehicle>("WORK");
            if (NAPI.Data.GetEntityData(player, "ON_WORK") == true)
            {
                NAPI.Task.Run(() =>
                {
                    try
                    {
                        if (player.HasData("TRAILER"))
                        {
                            NAPI.Entity.DeleteEntity(player.GetData<Vehicle>("TRAILER"));
                            player.ResetData("TRAILER");
                        }
                    }
                    catch { }
                });
                respawnCar(vehicle);
                Notify.Send(player, NotifyType.Info, NotifyPosition.BottomCenter, $"Вы закончили рабочий день", 3000);
                Customization.ApplyCharacter(player);
                NAPI.Data.SetEntityData(player, "IN_WORK_CAR", false);
                player.SetData("ON_WORK2", 0);
                player.SetData("PLAYER_WORKING", 0);
                player.SetData("PACKAGES", 0);
                NAPI.Data.SetEntityData(player, "WaitTrailer", false);
                Trigger.ClientEvent(player, "deleteCheckpoint", 3);
                Trigger.ClientEvent(player, "deleteWorkBlip");
                NAPI.Data.SetEntityData(player, "ON_WORK", false);
                NAPI.Data.SetEntityData(player, "WORK", null);
                Timers.Stop(NAPI.Data.GetEntityData(player, "WORK_CAR_EXIT_TIMER"));
                NAPI.Data.ResetEntityData(player, "WORK_CAR_EXIT_TIMER");
                NAPI.Data.SetEntityData(player, "TRAILER_ARG", false);
                return;
            }
            NAPI.Data.SetEntityData(player, "ON_WORK", true);
            player.SetData("ON_WORK2", 1);
            player.SetData("PLAYER_WORKING", JobWorkId);
            MoneySystem.Wallet.Change(player, -RentCost);
            GameLog.Money($"player({Main.Players[player].UUID})", $"server", RentCost, $"timberRent");

            NAPI.Data.SetEntityData(menu, "CARS", NAPI.Data.GetEntityData(menu, "CARS") + 1);
            player.SetData("PACKAGES", 0);
            var rnd = WorkManager.rnd.Next(0, 5); //Trailers
            var veh = NAPI.Vehicle.CreateVehicle(CarInfos[rnd].Model, CarInfos[rnd].Position, CarInfos[rnd].Rotation.Z, CarInfos[rnd].Color1, CarInfos[rnd].Color2, CarInfos[rnd].Number);
            Core.VehicleStreaming.SetEngineState(veh, false);
            NAPI.Data.SetEntityData(veh, "ACCESS", "WORK");
            NAPI.Data.SetEntityData(veh, "WORK", 16);
            NAPI.Data.SetEntityData(veh, "TYPE", "TIMBER");
            NAPI.Data.SetEntityData(veh, "NUMBER", rnd);
            NAPI.Data.SetEntityData(veh, "ON_WORK", false);
            NAPI.Data.SetEntityData(veh, "DRIVER", null);
            veh.SetSharedData("PETROL", VehicleManager.VehicleTank[veh.Class]);
            NAPI.Data.SetEntityData(player, "WORK", veh);
            NAPI.Data.SetEntityData(player, "WORKCHECK", 0);
            Notify.Send(player, NotifyType.Info, NotifyPosition.BottomCenter, $"Вы арендовали лесовоз.", 3000);
        }
        #endregion

        #region Вышел из машины
        [ServerEvent(Event.PlayerExitVehicle)]
        public void onPlayerExitVehicleHandler(Player player, Vehicle vehicle)
        {
            try
            {
                if (NAPI.Data.GetEntityData(vehicle, "TYPE") == "TIMBER" &&
                NAPI.Data.GetEntityData(player, "ON_WORK") &&
                NAPI.Data.GetEntityData(player, "WORK") == vehicle)
                {
                    Notify.Send(player, NotifyType.Warning, NotifyPosition.BottomCenter, $"Если Вы не сядете в транспорт через 60 секунд, то рабочий день закончится", 3000);
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
                        NAPI.Task.Run(() =>
                        {
                            try
                            {
                                if (player.HasData("TRAILER"))
                                {
                                    NAPI.Entity.DeleteEntity(player.GetData<Vehicle>("TRAILER"));
                                    player.ResetData("TRAILER");
                                }
                            }
                            catch { }
                        });
                        respawnCar(vehicle);
                        Notify.Send(player, NotifyType.Info, NotifyPosition.BottomCenter, $"Вы закончили рабочий день", 3000);
                        Customization.ApplyCharacter(player);
                        NAPI.Data.SetEntityData(player, "IN_WORK_CAR", false);
                        player.SetData("ON_WORK2", 0);
                        player.SetData("PLAYER_WORKING", 0);
                        player.SetData("PACKAGES", 0);
                        NAPI.Data.SetEntityData(player, "WaitTrailer", false);
                        Trigger.ClientEvent(player, "deleteCheckpoint", 3);
                        Trigger.ClientEvent(player, "deleteWorkBlip");
                        NAPI.Data.SetEntityData(player, "ON_WORK", false);
                        NAPI.Data.SetEntityData(player, "WORK", null);
                        Timers.Stop(NAPI.Data.GetEntityData(player, "WORK_CAR_EXIT_TIMER"));
                        NAPI.Data.ResetEntityData(player, "WORK_CAR_EXIT_TIMER");
                        NAPI.Data.SetEntityData(player, "TRAILER_ARG", false);
                        return;
                    }
                    NAPI.Data.SetEntityData(player, "CAR_EXIT_TIMER_COUNT", NAPI.Data.GetEntityData(player, "CAR_EXIT_TIMER_COUNT") + 1);

                }
                catch (Exception e)
                {
                    Log.Write("Timer_PlayerExitWorkVehicle_Timber: \n" + e.ToString(), nLog.Type.Error);
                }
            });
        }

        #endregion

        #region Если игрок умер
        public static void Event_PlayerDeath(Player player, Player entityKiller, uint weapon)
        {
            try
            {
                if (!Main.Players.ContainsKey(player)) return;
                if (player.GetData<int>("PLAYER_WORKING") != JobWorkId) return;
                if (player.GetData<bool>("WORK") && player.GetData<bool>("ON_WORK"))
                {
                    Customization.ApplyCharacter(player);
                    player.SetData("ON_WORK", false);
                    player.SetData("ON_WORK2", 0);
                    NAPI.Data.SetEntityData(player, "WORK", null);
                    player.SetData("PLAYER_WORKING", 0);
                    Trigger.ClientEvent(player, "deleteCheckpoint", 3);
                    Trigger.ClientEvent(player, "deleteWorkBlip");
                    player.SetData("PACKAGES", 0);
                    NAPI.Data.SetEntityData(player, "WaitTrailer", false);
                    var vehicle = player.Vehicle;
                    respawnCar(vehicle);

                    player.StopAnimation();
                    Main.OffAntiAnim(player);
                    BasicSync.DetachObject(player);
                    //MoneySystem.Wallet.Change(player, player.GetData("PAYMENT"));
                    NAPI.Data.SetEntityData(player, "TRAILER_ARG", false);

                    Trigger.ClientEvent(player, "CloseJobStatsInfo", player.GetData<int>("PAYMENT"));
                    Notify.Send(player, NotifyType.Success, NotifyPosition.BottomCenter, $"+ {player.GetData<int>("PAYMENT")}$", 3000);
                    player.SetData("PAYMENT", 0);
                    NAPI.Task.Run(() =>
                    {
                        try
                        {
                            if (player.HasData("TRAILER"))
                            {
                                NAPI.Entity.DeleteEntity(player.GetData<Vehicle>("TRAILER"));
                                player.ResetData("TRAILER");
                            }
                        }
                        catch { }
                    });
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
                if (player.GetData<bool>("WORK") && player.GetData<bool>("ON_WORK"))
                {
                    Customization.ApplyCharacter(player);
                    player.SetData("ON_WORK", false);
                    player.SetData("ON_WORK2", 0);
                    NAPI.Data.SetEntityData(player, "WORK", null);
                    player.SetData("PLAYER_WORKING", 0);
                    Trigger.ClientEvent(player, "deleteCheckpoint", 3);
                    Trigger.ClientEvent(player, "deleteWorkBlip");
                    player.SetData("PACKAGES", 0);
                    NAPI.Data.SetEntityData(player, "WaitTrailer", false);
                    var vehicle = player.Vehicle;
                    respawnCar(vehicle);

                    player.StopAnimation();
                    Main.OffAntiAnim(player);
                    BasicSync.DetachObject(player);
                    //MoneySystem.Wallet.Change(player, player.GetData("PAYMENT"));
                    player.SetData("PAYMENT", 0);
                    NAPI.Data.SetEntityData(player, "TRAILER_ARG", false);
                    NAPI.Task.Run(() =>
                    {
                        try
                        {
                            if (player.HasData("TRAILER"))
                            {
                                NAPI.Entity.DeleteEntity(player.GetData<Vehicle>("TRAILER"));
                                player.ResetData("TRAILER");
                            }
                        }
                        catch { }
                    });
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
