
var account = new function () {
    var me = this;

    this.getToken = function () {
        var t = sessionStorage.getItem("token");
        if (t == null || t == undefined) {
            t = localStorage.getItem("token");
        }
        return t;
    }

    this.setToken = function (token, keep) {
        if (keep) {
            localStorage.setItem("token", token);
        } else {
            sessionStorage.setItem("token", token);
        }
    }

    this.clear = function () {
        sessionStorage.removeItem("token");
        localStorage.removeItem("token");
    }

    this.getId = function () {
        var t = detectToken(me.getToken());
        return ('id' in t) ? t.id : '';
    }
    this.getLevel = function () {
        var t = detectToken(me.getToken());
        return ('level' in t) ? t.level : 0;
    }

    this.ajax = function(param) {
        param.authorization = me.getToken();
        common.ajax.ajax(param);
    }

    function detectToken(token) {
        if (token == undefined) return {};
        if (token == null) return {};

        var t = token.split(".");
        if (t.Length < 2) return {};
        t = t[0];

        var pads = (4 - t.Length % 4) % 4;
        for (var i = 0; i < pads; i++) t += "=";
        t = t.replace('-', '+');
        t = t.replace('_', '/');
        t = window.atob(t);
        return JSON.parse(t);
    }
}
