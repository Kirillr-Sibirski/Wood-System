var JobStatsInfoW = new Vue({
    el: ".JobStatsInfoW",
    data: {
        active: false,
        money: "1",
    },
    methods: {
        set: function (money) {
            this.money = money;
        }
    }
});

var Sawmill = new Vue({
    el: ".Sawmill",
    data: {
        active: false,
        header: "Работник лесообработки",
        money: "1",
        jobid: 14,
        work: 0,
    },
    methods: {
        set: function (money, level, currentjob, work) {
            this.money = money;
            this.level = level;
            this.jobid = currentjob;
            this.work = work;
        },
        exit: function () {
            this.active = false;
            mp.trigger('CloseSawmill');
        },
        setnewjob: function (jobsid) {
            this.jobid = jobsid;
        },
        enterJob: function (work) {
            mp.trigger('CloseSawmill');
            mp.trigger("enterJobSawmill", work);
        },
        selectJob: function (jobid) {
            mp.trigger("selectJobSawmill", jobid);
        }
    }
});

var Warehouse = new Vue({
    el: ".Warehouse",
    data: {
        active: false,
        header: "Склад",
        money: "1",
        jobid: 14,
        work: 0,
    },
    methods: {
        set: function (money, level, currentjob, work) {
            this.level = level;
            this.jobid = currentjob;
            this.work = work;
        },
        exit: function () {
            this.active = false;
            mp.trigger('CloseWarehouseSawmill');
        },
        takeObject: function (object) {
            mp.trigger('CloseWarehouseSawmill');
            mp.trigger("takeObjectWarehouse_Sawmill", object);
        },
    }
});

var Lumberjack = new Vue({
    el: ".Lumberjack",
    data: {
        active: false,
        header: "Лесоруб",
        money: "1",
        jobid: 15,
        work: 0,
    },
    methods: {
        set: function (money, level, work) {
            this.money = money;
            this.level = level;
            this.work = work;
        },
        exit: function () {
            this.active = false;
            mp.trigger('CloseLumberjack');
        },
        enterJob: function (work) {
            mp.trigger('CloseLumberjack');
            mp.trigger("enterJobLumberjack", work);
        },
    }
});
var GameLumber = new Vue({
    el: ".GameLumber",
    data: {
        active: false,
    },
    methods: {
        start: function () {
            let bar = document.getElementById('pr__bar')
            let btn = document.getElementById('btn')
            let width = 50
            bar.style.width = width + '%'

            function barState() {
                let id = setInterval(() => {
                    width -= 1
                    if (width <= 0) {
                        width = 0
                    } else if (width >= 75) {
                        clearInterval(id)
                        mp.trigger('EndGameLumber')
                        mp.trigger('CloseMiniGameLumber')
                        return;
                    }
                    bar.style.width = width + '%'
                }, 200)

                btn.addEventListener('click', () => {
                    width += 5
                })
            }

            barState()
        }
    }
});
var GameWW = new Vue({
    el: ".GameWW",
    data: {
        active: false,
    },
    methods: {
        start: function () {
            let bar = document.getElementById('pr__bar')
            let btn = document.getElementById('btn')
            let width = 50
            bar.style.width = width + '%'

            function barState() {
                let id = setInterval(() => {
                    width -= 1
                    if (width <= 0) {
                        width = 0
                    } else if (width >= 75) {
                        clearInterval(id)
                        mp.trigger('EndGameWW')
                        mp.trigger('CloseMiniGameWW')
                        return;
                    }
                    bar.style.width = width + '%'
                }, 200)

                btn.addEventListener('click', () => {
                    width += 5
                })
            }

            barState()
        }
    }
});