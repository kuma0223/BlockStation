//初期化
//window.addEventListener("DOMContentLoaded", function(){
//});

var common = new function () {

    //--------------------
    //Object全般
    this.object = new function(){
        this.copyHash = function(org){
            var ret = {};
            Object.keys(org).forEach(function (key) {
                ret[key] = org[key];
            });
            return ret;
        }
    }

    //--------------------
    //HTMLタグ
    this.html = new function(){

        /** URLパラメータを分解しハッシュで返します。
         * @param {string} str クエリ文字列。未指定の場合location.searchを使用。
         * @return {Object} key-valueのハッシュ
         */
        this.getUrlQuerys = function(str){
            if(str == undefined) str = location.search;
            var spl = str.substring(1).split("&");
            var ret = {};
            spl.forEach(function(entry){
                var spl2 = entry.split("=");
                ret[spl2[0]] = spl2[1];
            });
            return ret;
        }

        /** ハッシュをURLパラメータ文字列へ変換します。
        * @param {Object} key-valueのハッシュ
        * @return {string} "key1=value1&key2=value2..."の文字列
        */
        this.toUrlQuerys = function (map) {
            if(map.length == 0) return "";
            var str = "";
            Object.keys(map).forEach(function (key) {
                if (str.length > 0) str += "&";
                if (""+map[key] != "") str += key + "=" + map[key];
            });
            return str;
        }

        /**
         * 単位付きの数値文字列に指定の数値を足します。
         * 例： fx("50px", 10, "px") = "60px"
         * @param {string} valstr 現在値
         * @param {number} add 加算値
         * @param {unit} 単位
         */
        this.addUnitedNum = function(valstr, add, unit){
            valstr = valstr.replace(unit, "");
            var i = Number(valstr);
            return (i + add) + unit;
        }

        /**
         * ハッシュをstyle属性の文字列に変換します。
         */
        this.toStyle = function(obj){
            if (obj == null || obj == undefined) return "";
            var ret = "";
            Object.keys(obj).forEach(function (key) {
                ret += key + ":" + obj[key] + ";";
            });
            return ret;
        }
    }

    //--------------------
    //Ajax

    this.ajax = new function(){
        var DefaultParam = {
            type: "", //指定必須 GET POSTなど
            url:"",   //指定必須
            data:{},
            timeout:20000,
            contentType: "application/json; charset=UTF-8",
            authorization: null,
            callback: function (event) { }
        }

        /**
         * httpリクエストを送信し応答を取得します。
         * パラメータはDefaultParamを参照してください。
         */
        this.ajax = function(param){
            try{
                var req = new XMLHttpRequest();

                req.onload = onload;
                req.onerror = onerror;
                req.onabort = onabort;
                req.ontimeout = ontimeout;

                req.open(param.type, param.url);
                req.timeout = (param.timeout !== undefined ? param.timeout : DefaultParam.timeout);
                //HTTPヘッダ
                req.setRequestHeader("Content-Type",
                    param.contentType !== undefined ? param.contentType : DefaultParam.contentType);
                req.setRequestHeader("Authorization",
                    param.authorization !== undefined ? ("Bearer " + param.authorization) : DefaultParam.authorization);
                //送信
                req.send(JSON.stringify(param.data!==undefined ? param.data : DefaultParam.data));
            }catch(e){
                beCallback(false, "通信を開始できませんでした。" + e.message);
            }
            function onload(){
                if (req.status == 200) {
                    beCallback(true, "");
                }else if (req.status == 401) {
                    beCallback(false, "ログインが必要です。" + req.status + "/" + req.statusText);
                }else{
                    beCallback(false, "通信に失敗しました。" + req.status + "/" + req.statusText);
                }
            }
            function onerror(e){
                beCallback(false, "通信異常が発生しました。");
            }
            function onabort(){
                beCallback(false, "通信中に割り込みが発生しました。");
            }
            function ontimeout(){
                beCallback(false, "通信がタイムアウトしました。");
            }

            function beCallback(success, message){
                var callback = (param.callback !== undefined ? param.callback : DefaultParam.callback);
                var event = {
                    success:success,
                    response:req.response,
                    responseType:req.responseType,
                    status:req.status,
                    message:message
                };
                callback(event);
            }
        }
    }
};

/**
 * ダイアログ操作クラス
 * DOM操作でダイアログを表示します。
 * ダイアログを多重に開くことはできません。
 */
var dialog = new function(){
    var me = this;
    this.screen;    //ダイアログ挿入用タグエレメント
    this.dialog;    //最終表示ダイアタグエレメント
    this.bank;      //過去ダイアログの保管用タグエレメント
    this.callback;  //閉じた際のコールバック
    this.dialogOn = false; //表示状態

    //メッセージボックス用定義
    var messageboxCode = "<div class='dialog messagebox'><p style='padding:15px;min-height:0px;min-width:250px'>【】</p><div style='text-align:center;padding:8px;display:none' class='messagebox_buttons_okcancel'><span class='button' onclick='dialog.close(true)'>ＯＫ</span><span class='button' onclick='dialog.close(false)'>キャンセル</span></div><div style='text-align:center;padding:8px;display:none' class='messagebox_buttons_ok'><span class='button' onclick='dialog.close(true)'>ＯＫ</span></div></div>";
    this.Messagebox_Buttons_OkCancel = "messagebox_buttons_okcancel";
    this.Messagebox_Buttons_Ok = "messagebox_buttons_ok";
    
    //----------------------------------------

    window.addEventListener("DOMContentLoaded", function(){
        //最初にダイアログ用タグをbodyに追加。
        me.screen = document.createElement("div");
        me.screen.id = "dialogScreen";
        document.body.appendChild(me.screen);

        me.bank = document.createElement("div");
        me.bank.id = "dialogBank";
        me.bank.style.display = "none";
        document.body.appendChild(me.bank);
    });

    //----------------------------------------

    /**
     * メッセージボックスを表示します。
     * コールバックの引数には OK:true,キャンセル:false が渡されます。
     * @param {string} message 表示文字列
     * @param {string} buttons Messagebox_Button_メンバから選択
     * @param {function} callback ダイアログを閉じたときに呼ばれるコールバック
     */
    this.messagebox = function(message, buttons, callback){
        me.callback = callback;
        //古いダイアログを除去
        inToBank();
        me.dialog = null;
        //改行の置換
        message = message.replace(/\n/g,"<br/>");
        //タグ挿入
        me.screen.innerHTML = messageboxCode.replace("【】", message);
        me.dialog = me.screen.getElementsByClassName("dialog")[0];
        //ボタン選択
        if(buttons === undefined) buttons=me.Messagebox_Buttons_Ok;
        var buttonsElement = me.dialog.getElementsByClassName(buttons)[0];
        if(buttonsElement) buttonsElement.style.display = "block";
        //表示
        me.screen.classList.add("on");
        me.dialog.classList.add("on");
        me.dialogOn = true;
    }

    /**
     * 指定したタグエレメントをダイアログとして表示します。
     * エレメントはDOMから除去され、ダイアログ領域（div#dialgScreen）に再挿入されます。
     * その後に別のダイアログを表示すると非表示の保管領域（div#dialogBank）に移動されます。
     *
     * 指定するエレメントは以下を満たす必要があります。
     *  ・dialogクラスを設定している
     *  ・dialog.close を呼び出すコードがある
     *  ・riotタグが含まれる場合、マウントが完了している
     *
     * @param {HTMLElement} element 表示エレメント
     * @param {function} callback ダイアログを閉じた時に呼ばれるコールバック
     */
    this.showElement = function(element, callback){
        if(element === null || element === undefined) return;
        me.callback = callback;
        
        //エレメントを今の親から削除
        if(element.parentElement){
            element.parentElement.removeChild(element);
        }

        //古いダイアログを除去
        inToBank();
        me.dialog = null;

        //スクリーンへ挿入、表示
        me.screen.appendChild(element);
        me.dialog = element;
        
        me.screen.classList.add("on");
        me.dialog.classList.add("on");
        me.dialogOn = true;
    }

    /**
     * ダイアログを閉じます。
     * 引数はそのままコールバックへ引き渡されます。
     * @param {Object} result コールバック引数
     */
    this.close = function(result){
        if(me.dialogOn){
            me.screen.classList.remove("on");
            me.dialog.classList.remove("on");
            me.dialogOn = false;

            if(me.callback !== undefined && me.callback !== null){
                me.callback(result);
            }
        }
    }

    function inToBank(){
        var dlgs = me.screen.getElementsByClassName("dialog");
        if(dlgs.length < 1){ return; }
        
        var dlg = dlgs[0];
        if(!dlgs[0].classList.contains("messagebox")){
            me.screen.removeChild(dlg);
            me.bank.appendChild(dlg);
        }
        //中をきれいに
        while(me.screen.firstChild){ me.screen.removeChild(me.screen.firstChild); }
    }
}