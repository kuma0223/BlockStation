
window.Auth = new function () {
    var me = this
    var keyL = "auth.login"
    var keyR = "auth.refresh"

    //method
    this.ajax = ajaxWithAuth;
    this.login = login;
    this.clear = clear;

    function getLoginToken() {
        let itm = localStorage.getItem(keyL)
        return (itm != null) ? itm : sessionStorage.getItem(keyL)
    }

    function getRefreshToken() {
        let itm = localStorage.getItem(keyR)
        return (itm != null) ? itm : sessionStorage.getItem(keyR)
    }

    function setToken(login, refresh, keep) {
        if (keep) {
            localStorage.setItem(keyL, login)
            localStorage.setItem(keyR, refresh)
        } else {
            sessionStorage.setItem(keyL, login)
            sessionStorage.setItem(keyR, refresh)
        }
    }

    function clear() {
        localStorage.removeItem(keyL)
        localStorage.removeItem(keyR)
        sessionStorage.removeItem(keyL)
        sessionStorage.removeItem(keyR)
    }

    function isKeep() {
        return localStorage.getItem(keyR) != null
    }


    function detectToken(token) {
        if (token == null) return {};

        let t = token.split(".");
        if (t.Length < 3) return {};
        t = t[1];

        let pads = (4 - t.Length % 4) % 4;
        for (let i = 0; i < pads; i++) t += "=";
        t = t.replace('-', '+');
        t = t.replace('_', '/');
        t = window.atob(t);
        return JSON.parse(t);
    }

    function copyHash(org) {
        let ret = {}
        Object.keys(org).forEach(function (key) {
            ret[key] = org[key]
        })
        return ret;
    }

    function ajaxWithAuth(options) {
        var callbackOrg = ("callback" in options) ? options.callback : function () { };

        //トークン切れ事前検知
        //クライアントの時刻がおかしいときに変なリクエストになるのでやめ
        //let ltokenObj = detectToken(ltoken)
        //if (ltoken != null && ltokenObj.exp < (new Date().getTime() / 1000 - 60)) {
        //    refresh(refreshed)
        //    return
        //}

        request();

        function request() {
            let ltoken = getLoginToken()

            var opts = copyHash(options)
            if (!("headers" in opts)) {
                opts.headers = {}
            }
            if (ltoken != null) {
                opts.headers["Authorization"] = "Bearer " + ltoken
            }
            opts.callback = responsed

            ajax(opts)
        }
        function responsed(event) {
            let rtoken = getRefreshToken()

            if (event.status == 401 && rtoken != null) {
                //トークン切れ
                refresh(refreshed)
            } else {
                callbackOrg(event)
            }
        }
        function refreshed(event) {
            if (event.success) {
                //再リクエスト
                request()
            } else {
                callbackOrg(event)
            }
        }
    }

    function refresh(callback) {
        let rtoken = getRefreshToken();

        ajax({
            type: "POST",
            url: "/api/user/refresh",
            data: rtoken,
            timeout: 5000,
            callback: function(event){
                if (event.success) {
                    let obj = JSON.parse(event.response)
                    let lt = obj["loginToken"]
                    let rt = obj["refreshToken"]
                    setToken(lt, rt, isKeep())
                } else {
                    clear()
                }
                callback(event);
            }
        })
    }

    function login(id, password, keep, callback) {
        ajax({
            type: "POST",
            url: "/api/user/login",
            data: { id: id, password: password },
            timeout: 5000,
            callback: function (event) {
                if (event.success) {
                    let obj = JSON.parse(event.response)
                    let lt = obj["loginToken"]
                    let rt = obj["refreshToken"]
                    setToken(lt, rt, isKeep())
                } else {
                    clear()
                }
                callback(event);
            }
        })
    }

    function ajax(options) {
        //type, url, headers, data, timeout, callback
        var op = options
        var req = new XMLHttpRequest();

        req.onload = onload;
        req.onerror = onerror;
        req.onabort = onabort;
        req.ontimeout = ontimeout;

        try {
            req.open(op.type, op.url);
            if ("timeout" in op)
                req.timeout = op.timeout;

            //HTTPヘッダ
            req.setRequestHeader("Content-Type", "application/json;charset=UTF-8");
            if ("headers" in op) {
                Object.keys(op.headers).forEach(function (key) {
                    req.setRequestHeader(key, op.headers[key]);
                })
            }

            //送信
            if ("data" in op) {
                req.send(JSON.stringify(op.data));
            } else {
                req.send();
            }
        }catch (e) {
            beCallback(false, "通信を開始できませんでした。" + e.message);
        }

        function onload() {
            if (200 <= req.status && req.status < 300) {
                beCallback(true, "");
            } else {
                beCallback(false, "正常以外の応答が返されました。" + req.status + "/" + req.statusText);
            }
        }
        function onerror(e) {
            beCallback(false, "通信異常が発生しました。" + e.message);
        }
        function onabort() {
            beCallback(false, "通信中に割り込みが発生しました。");
        }
        function ontimeout() {
            beCallback(false, "通信がタイムアウトしました。");
        }
        function beCallback(success, message) {
            var event = {
                success: success,
                status: req.status,
                response: req.response,
                message: message,
            };
            if ("callback" in op) {
                op.callback(event);
            }
        }
    }
}