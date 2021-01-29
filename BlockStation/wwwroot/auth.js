window.Auth = new function () {
    //const me = this
    const keyL = "auth.login"
    const keyR = "auth.refresh"
    const Setting = window.Setting

    //methods
    this.ajax = ajaxWithAuth;
    this.login = login;
    this.logout = clear;
    this.isKeep = isKeep;
    this.refresh = refresh;
    this.getLoginToken = getLoginToken;
    this.getRefreshToken = getRefreshToken;
    this.getAuthState = function(){
        return detect(getLoginToken())
    };
    this.getAuthorizationHeader = function () {
        return 'Bearer ' + getLoginToken()
    };

    //functions
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

    function copyHash(org) {
        let ret = {}
        if (!org) return ret

        Object.keys(org).forEach(function (key) {
            ret[key] = org[key]
        })
        return ret;
    }

    /**
     * トークンを解析しペイロードを取得します。
     * ログインしていない場合nullが返ります。
     * @param {any} token トークン
     * @returns {object} ペイロード
     */
    function detect(token) {
        if (token == null) return null;

        let t = token.split(".");
        if (t.Length < 3) return null;
        t = t[1];

        let pads = (4 - t.Length % 4) % 4;
        for (let i = 0; i < pads; i++) t += "=";
        t = t.replace('-', '+');
        t = t.replace('_', '/');
        t = window.atob(t);
        return JSON.parse(t);
    }

    /**
     * 認証付きのリクエストを送信します。
     * option {
     *      url: 必須
     *      method: 既定 "GET"
     *      headers: 既定 {}
     *      body: 既定 undefined
     * }
     * return {
     *      ok: true|false
     *      status: httpレスポンスコード
     *      statusText:  httpレスポンスコード名
     *      body: bodyテキスト
     * }
     * @param {object} options リクエスト設定
     * @returns {object} レスポンス情報
     */
    async function ajaxWithAuth(options) {
        //事前のトークン有効チェックはクライアント時刻に
        //左右されるのでやらない

        let ret = await request(0)
        return ret

        async function request(retry) {
            let opts = copyHash(options)
            opts.headers = copyHash(options.headers)

            let ltoken = getLoginToken()
            if (ltoken != null) {
                opts.headers['Authorization'] = 'Bearer ' + ltoken
            }

            let res = await ajax(opts)
            let rtoken = getRefreshToken()

            if (res.status == 401 && rtoken != null && retry < 1) {
                //トークン切れ,更新して再リクエスト
                let isRef = await refresh()
                if (isRef) {
                    return await request(retry + 1)
                }
            }
            return res
        }
    }

    /**
     * トークンを更新します。
     * @returns {boolean} 成否
     */
    async function refresh() {
        let rtoken = getRefreshToken();
        let res = await ajax({
            url: Setting.api+"user/refresh",
            method: "POST",
            body: { refreshToken: rtoken },
        })

        if (res.ok) {
            let obj = JSON.parse(res.body)
            let lt = obj["loginToken"]
            let rt = obj["refreshToken"]
            setToken(lt, rt, isKeep())
        } else {
            if (res > 0) {
                //通信エラーのときはクリアしない
                //(ログアウトしない)
                clear()
            }
        }
        return res.ok
    }

    /**
     * ログインします。
     * @param {string} id ユーザID
     * @param {string} password パスワード
     * @param {boolean} keep ログインを維持するか
     * @returns {object} okとstatusを持つ連想配列
     */
    async function login(id, password, keep) {
        var res = await ajax({
            method: "POST",
            url: Setting.api+"user/login",
            body: { id: id, password: password },
        })

        if (res.ok) {
            let obj = JSON.parse(res.body)
            let lt = obj["loginToken"]
            let rt = obj["refreshToken"]
            setToken(lt, rt, keep)
        } else {
            clear()
        }

        return {
            ok: res.ok,
            status: res.status,
        }
    }

    async function ajax(options) {
        let init = {}
        init.method = options.method || "GET"
        init.headers = options.headers || {}
        if ("body" in options) {
            init.headers["Content-Type"] = "application/json;charset=UTF-8"
            init.body = JSON.stringify(options.body)
        }

        try {
            let res = await fetch(options.url, init)
            let ret = {
                ok: res.ok,
                status: res.status,
                statusText: res.statusText,
            }
            ret.body = await res.text()
            return ret
        } catch (ex) {
            let ret = {
                ok: false,
                status: -1,
                statusText: "" + ex,
                body: "" + ex,
            }
            return ret
        }
    }


    /* XMLHttpRequest版
    function ajaxWithAuth_(options) {
        var callbackOrg = ("callback" in options) ? options.callback : function () { };

        //トークン切れ事前検知
        //クライアントの時刻がおかしいときに変なリクエストになるのでやらない
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
                //トークン更新
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

    function refresh_(callback) {
        let rtoken = getRefreshToken();

        ajax({
            type: "POST",
            url: "/api/user/refresh",
            data: { refreshToken: rtoken },
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

    function login_(id, password, keep, callback) {
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
                    setToken(lt, rt, keep)
                } else {
                    clear()
                }
                callback(event);
            }
        })
    }

    function ajax_(options) {
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
    */
}

export const Auth = window.Auth