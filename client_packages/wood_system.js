mp.peds.new(0x14D7B4E0, new mp.Vector3(-605.5319, 5304.9424, 70.3977), -81.193436); // ped Sawmill
mp.peds.new(0xEE75A00F, new mp.Vector3(-576.2773, 5367.021, 70.24318), -54.676945); // ped Warehouse
mp.peds.new(0x14D7B4E0, new mp.Vector3(-1358.9165, 4481.1807, 25.765453), -155.44876); // ped Timber
mp.peds.new(0xC5FEFADE, new mp.Vector3(-1582.1957, 4527.828, 17.784897), -97.64878); // ped Lumberjack
mp.peds.new(0x2F8845A3, new mp.Vector3(-567.93066, 5332.393, 70.214455), 81.09113); // ped Lic
global.wood = mp.browsers.new('package://cef/wood_system.html'); //статистика

//mapping
var second = mp.objects.new(192829538, new mp.Vector3(-1592.7021, 4522.9053, 15.886102)); //second
second.rotation = new mp.Vector3(-5, -5, 101.86086)
mp.objects.new(3609391016, new mp.Vector3(-1558.365, 4476.7183, 18.459589)); //far
var checkpoint_obj = mp.objects.new(2913410225, new mp.Vector3(-1594.726, 4510.456, 17.833836)); //checkpoint
checkpoint_obj.rotation = new mp.Vector3(-12, -4, 0)

// Job StatsInfo //
mp.events.add('JobStatsInfoW', (money) => {
    wood.execute('JobStatsInfoW.active=1');
    wood.execute(`JobStatsInfoW.set('${money}')`);
});
mp.events.add('CloseJobStatsInfoW', () => {
    wood.execute('JobStatsInfoW.active=0');
});
// Улучшенные блипы
var JobMenusBlip = [];
mp.events.add('JobMenusBlip', function (uid, type, position, names, dir) {
    if (typeof JobMenusBlip[uid] != "undefined") {
        JobMenusBlip[uid].destroy();
        JobMenusBlip[uid] = undefined;
    }
    if (dir != undefined) {
        JobMenusBlip[uid] = mp.blips.new(type, position,
            {
                name: names,
                scale: 1,
                color: 4,
                alpha: 255,
                drawDistance: 100,
                shortRange: false,
                rotation: 0,
                dimension: 0
            });
    }

});
mp.events.add('deleteJobMenusBlip', function (uid) {
    if (typeof JobMenusBlip[uid] == "undefined") return;
    JobMenusBlip[uid].destroy();
    JobMenusBlip[uid] = undefined;
});

// Job Lumberjack //
mp.events.add('OpenLumberjack', (money, level, work) => {
    if (global.menuCheck()) return;
    wood.execute(`Lumberjack.set('${money}', '${level}', '${work}')`);
    wood.execute('Lumberjack.active=1');
    global.menuOpen();
});
mp.events.add('CloseLumberjack', () => {
    wood.execute('Lumberjack.active=0');
    global.menuClose();
});
mp.events.add('enterJobLumberjack', (work) => {
    mp.events.callRemote('enterJobLumberjack', work);
});
//Mini game, Lumberjack
mp.events.add('OpenMiniGameLumber', () => {
    if (global.menuCheck()) return;
    wood.execute('GameLumber.active=1');
    wood.execute(`GameLumber.start()`);
    global.menuOpen();
});
mp.events.add('CloseMiniGameLumber', () => {
    wood.execute('GameLumber.active=0');
    global.menuClose();
});
mp.events.add('EndGameLumber', () => {
    mp.events.callRemote("EndGameLumber");
});
//Mini game, Wood Worker
mp.events.add('OpenMiniGameWW', () => {
    if (global.menuCheck()) return;
    wood.execute('GameWW.active=1');
    wood.execute(`GameWW.start()`);
    global.menuOpen();
});
mp.events.add('CloseMiniGameWW', () => {
    wood.execute('GameWW.active=0');
    global.menuClose();
});
mp.events.add('EndGameWW', () => {
    mp.events.callRemote("EndGameWW");
});
// Job Sawmill //
mp.events.add('OpenSawmill', (money, level, currentjob, work) => {
    if (global.menuCheck()) return;
    wood.execute(`Sawmill.set('${money}', '${level}', '${currentjob}', '${work}')`);
    wood.execute('Sawmill.active=1');
    global.menuOpen();
});
mp.events.add('CloseSawmill', () => {
    wood.execute('Sawmill.active=0');
    global.menuClose();
});
mp.events.add("selectJobSawmill", (jobid) => {
    if (new Date().getTime() - global.lastCheck < 1000) return;
    global.lastCheck = new Date().getTime();
    mp.events.callRemote("jobJoinSawmill", jobid);
});
mp.events.add('secusejobSawmill', (jobsid) => {
    wood.execute(`Sawmill.setnewjob('${jobsid}')`);
});

mp.events.add('enterJobSawmill', (work) => {
    mp.events.callRemote('enterJobSawmill', work);
});

//Checkpoint sync between players
var check_list = {};
mp.events.add('SyncPoint', (player, check, length) => {
    do { //while object is in array
        check = Math.floor(Math.random() * length);
    }
    while (containsObject(player))
    
    check_list[player] = check;
    //check_list.push(check);
    mp.events.callRemote('sawmill_sync', player, check);
});
mp.events.add('DeletePoint', (player) => { 
    for(var i in check_list){
        if(i = player)
        {
            delete check_list[i]; 
        }
      }
});
function containsObject(obj) {
    var i;
    for (i = 0; i < check_list.length; i++) {
        if (check_list[i] === obj) {
            return true;
        }
    }

    return false;
}
// Loader //
let carrier;
mp.events.add('CreateObjectLoader', function (player, position, rot) {
    if (player && mp.players.exists(player)) {
        if (!localplayer.isInAnyVehicle(true)) return;
        carrier = mp.objects.new(3108526058, position, rot);
    }
});
mp.events.add('AttachObjectLoader', function (player) {
    if (player && mp.players.exists(player)) {
        if (!localplayer.isInAnyVehicle(true)) return;
        if (!mp.objects.exists(carrier)) return;

        var veh = player.vehicle;

        var bone = veh.getBoneIndexByName("forks");
        carrier.attachTo(veh.handle, bone, 0.0, 1.1, -0.6, 0.0, 0.0, 90.0, false, false, true, false, 0, true);
    }
});
mp.events.add('DestroyObjectLoader', (player) => {
    if (player && mp.players.exists(player)) {
        if (!mp.objects.exists(carrier)) return;
        carrier.destroy();
    }
});
mp.events.add('SetLoaderMass', (player, mass) =>{
    if(player.vehicle == null) return;
    player.vehicle.setHandling("fMass", mass);
});
// Warehouse //
mp.events.add('OpenWarehouseSawmill', (currentjob, work, instrument) => {
    if (global.menuCheck()) return;
    wood.execute(`Warehouse.set(''${currentjob}', '${work}', '${instrument}')`);
    wood.execute('Warehouse.active=1');
    global.menuOpen();
});
mp.events.add('CloseWarehouseSawmill', () => {
    wood.execute('Warehouse.active=0');
    global.menuClose();
});
mp.events.add('takeObjectWarehouse_Sawmill', (object) =>{
    mp.events.callRemote("takeObjectWarehouse_Sawmill", object);
});
