/* 共通スクリプト 基本全ページで読み込むことを前提とします */
/// <reference path="setting.js" />

//初期化
window.addEventListener("DOMContentLoaded", function(){
    //タイトル設定
    if(typeof setting !== "undefined"){
        document.title = setting.title;
    }
});

//◆━━━━━━━━━━━━━━━━━━━━━━━━━━━━◆

/**
 * アカウント状態クラス
 */
var account = new function () {
    this.getToken = function () {
        var t = sessionStorage.getItem("token");
        if (t == null || t == undefined) {
            t = localStorage.getItem("token");
        }
        return t;
    }
    this.getId = function () {
        var t = sessionStorage.getItem("id");
        if (t == null || t == undefined) {
            t = localStorage.getItem("id");
        }
        return t;
    }
    this.clear = function () {
        sessionStorage.removeItem("id");
        sessionStorage.removeItem("token");
        localStorage.removeItem("id");
        localStorage.removeItem("token");
    }
}

//◆━━━━━━━━━━━━━━━━━━━━━━━━━━━━◆

/**
 * 汎用関数群
 * このオブジェクトに属する者はすべて静的で状態を持たない関数もしくは定数とする。
 */
var common = new function(){
    //------------------------------------------------------------
    //デバッグ用
    this.debug = new function(){
        var StatusBar = true;         //ステータスバー表示有

         /** ブレークポイント設定用 */
        this.break = function(){
            var abc=0;
        }
        /** ステータスバーにデバッグ情報を表示します */
        this.statusBar = function(msg){
            if(StatusBar) window.status = msg;
        }
    }
    
    //------------------------------------------------------------
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

    //------------------------------------------------------------
    //Date型関係
    this.date = new function(){
        var me = this;

        //通秒が0となる日時。日本タイムゾーンだと以下のgetTime()が0になる。
        //取得データはサーバ環境依存、js側はクライアント環境依存なのに注意
        //（取得データの通秒とgetTime()は互換無しと考えるべき）
        this.zero = new Date(1970, 0, 1, 9, 0, 0, 0);

        /** 
         * 日付型をフォーマット変換します。
         * 各フォーマット子の間は分割が必要です（yyyyMMddは不可、yyyy/MM/ddは可）。
         * 複合フォーマットは正規表現や置換があるので format(d, "HH")+":"+ format(d, "mm") としたほうが早いです。
         * @param {Date} d 日時
         * @param {string} format フォーマット
         */
        this.format = function(d, format){
            var ret = dateFormatParts(d, format);
            if(ret != "") return ret;

            var splited = format.split(/[^yMHdms]+/);
            ret = format;
            for(var i=0; i<splited.length; i++){
                ret = ret.replace(splited[i], dateFormatParts(d, splited[i]));
            }
            return ret;
        
            //部品
            function dateFormatParts(d, format){
                switch(format){
                    case "yy": return("00" + d.getFullYear()).substr(-2);
                    case "yyyy": return("0000" + d.getFullYear()).substr(-4);
                    case "MM": return ("00" + (d.getMonth()+1)).substr(-2);
                    case "dd": return ("00" + d.getDate()).substr(-2);
                    case "HH": return ("00" + d.getHours()).substr(-2);
                    case "mm": return ("00" + d.getMinutes()).substr(-2);
                    case "ss": return ("00" + d.getSeconds()).substr(-2);
                    default : return "";
                }
            }
        }

        /**
         * 日時文字列（yyyy/MM/dd HH:mm:ss.lll等）から日時型を生成します。
         * 数字以外の文字を分割子とし、順序は年からでなければなりません。
         * 時以降の最下位桁を省略した場合は0となります。
         * 時は24時間表記で24時とした場合は翌日0時となります。
         * @param {String} str 日時文字列
         * @return 日付型
         */
        this.fromStr = function(str){
            var splited = str.split(/[^0-9]+/);
            var ret = new Date(splited[0], splited[1]-1, splited[2], 0, 0, 0, 0);
            if(splited.length > 3) ret.setHours(splited[3]);
            if(splited.length > 4) ret.setMinutes(splited[4]);
            if(splited.length > 5) ret.setSeconds(splited[5]);
            if(splited.length > 6) ret.setMilliseconds(splited[6]);
            return ret;
        }

        /**
         * 日時型の分を指定の間隔で丸めます。また分未満の値は全て0とします。
         * @param {Date} d 対象日時（値が変更されます）
         * @param {number} span (0<span<=60)となる分間隔
         * @return {Date} 丸めた日時。ただし引数に渡したインスタンスです。
         */
        this.cutMinutes = function(d, span){
			if(span==0) span=60;
            d.setSeconds(0); d.setMilliseconds(0);
            var min = d.getMinutes();
            d.setMinutes(min - (min % span));
            return d;
        }

        /**
         * シリアライズされた.NetのDateTime型からgetTime()値を取り出します。
         * @param {string} シリアライズされたDateTime型文字列
         * @return {number} 変換不能の場合NaN
         */
        this.fromNetGetTime = function(str){
            //ほんとにこの方法で全パターンでいいか検証が必要かも。
            if(!str || str.length < 6){ return NaN; }
            return parseInt(str.substr(6));
        }

        /**
         * シリアライズされた.NetのDateTime型からDate型を作成します。
         * @param {string} シリアライズされたDateTime型文字列
         * @return {Date} 変換不能の場合null
         */
        this.fromNet = function(str){
            var t = me.fromNetGetTime(str);
            if(t == NaN) return null;
            return new Date(t);
        }

        /**
         * 通秒（UNIX時間）を日時型に変換します。
         * @param {number} sec 通秒
         * @return {Date} 日時
         */
        this.fromTotalSec = function(sec){
            var t = new Date(me.zero.getTime());
            t.setSeconds(t.getSeconds() + sec);
            return t;
        }
        /**
         * 日時型を通秒（UNIX時刻）に変換します。
         * @param {Date} d 日時
         * @return {number} 通秒
         */
        this.toTotalSec = function(d){
            return Math.floor((d.getTime() - me.zero.getTime()) / 1000);
        }
        /**
         * 通分（UNIX時間）を日時型に変換します。
         * @param {number} sec 通秒
         * @return {Date} 日時
         */
        this.fromTotalMin = function(min){
            var t = new Date(me.zero.getTime());
            t.setMinutes(t.getMinutes() + min);
            return t;
        }
        /**
         * 日時型を通分（UNIX時刻）に変換します。
         * @param {Date} d 日時
         * @return {number} 通秒
         */
        this.toTotalMin = function(d){
            return Math.floor((d.getTime() - me.zero.getTime()) / 1000 / 60);
        }
    }

    //------------------------------------------------------------
    //数値型
    this.number = new function(){
        var me = this;

        /**
         * 整数か否か判定します。
         * @param {number} x 数値
         * @return {bool}
         */
        this.isInteger = function(x){
            return /^-?[0-9]+$/.test(x);
        }
        
        /**
         * 整数部の桁数を返します。
         * @param {number} x 数値
         * @return {number}
         */
        this.getIDigit = function(x){
            if(x<0) x=-x;
            x = Math.floor(x);
            return x.toString().length;
        }

        /**
         * 整数を0埋め8桁の16進数表現に変更します。
         * @param {number} x 32bit範囲内の整数
         */
        this.toIntHexString = function(x){
             //負値は32bitの2の補数化
            if(x < 0){ x = 0x100000000 - (-x); }
            return ("00000000" + x.toString(16)).substr(-8);
        }
    }
    
    //------------------------------------------------------------
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
         * flexgridを使用して渡されたhtmlエレメントの中身を
         * 両端ぞろえにします。
         */
        this.toJustify = function(element){
            var txt = element.innerText;
            var inner = "";
            for(var i=0; i<txt.length; i++){
                inner += "<span>" + txt.substr(i, 1) + "</span>";
            }
            element.style["display"] = "flex";
            element.style["flex-direction"] = "row";
            element.style["justify-content"] = "space-between";
            element.innerHTML = inner;
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


    //------------------------------------------------------------
    //Ajax

    this.ajax = new function(){
        var DefaultParam = {
            type: "", //指定必須 GET POSTなど。Webサービスの呼び出しはPOST。
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

    //------------------------------------------------------------
    //Riot.js関係

    this.riot = new function(){
        /**
         * カスタムタグを名称検索します。
         * @param tags riot.moun()で返されるタグオブジェクト群
         * @param {string} name 取得対象
         * @return 最初に見つかったタグオブジェクト
         */
        this.tagFromName = function(tags, name){
            for(var i=0; i<tags.length; i++){
                if(tags[i].opts.name === name) { return tags[i]; }
            }
            return null;
        }

        /**
         * this.refs.xxxに対しforEachを掛けます。listが単体オブジェの場合でも実行されます。
         * Riot.js V3で、子タグのupdateをするとrefs.xxx[]の順序が変更されるため、一時配列を作成して回す。
         * 以前のtagsはこうならなかった。これは仕様なのか？バグなのか？
         * 実行順序が保障されないことに注意。
         * @param {list} 対象の配列（this.refs.xxx）
         * @param {func} 処理 
         */
        this.refEach = function(list, func){
            var tmp = [];
            if (list == undefined) { return; }
            else if(Array.isArray(list)){ list.forEach(function(item){ tmp.push(item); }); }
            else{ tmp.push(list); }
            tmp.forEach(func);
        }
    }
};

//◆━━━━━━━━━━━━━━━━━━━━━━━━━━━━◆

/**
 * フッター操作クラス
 */
var footer = new function(){
    var text = "";  //現在状態
    var state = ""; //〃

    /**
     * 現在表示中の文字列を取得します。
     */
    this.getText = function () {
        return text;
    }

    /**
     * 表示をクリアします。
     */
    this.clear = function () {
        setFooter("", "");
    }

    /**
     * メッセージ表示を行います。
     * @param {string} message 表示メッセージ
     */
    this.message = function (message) {
        setFooter(message, "message");
    }

    /**
     * エラー表示を行います。
     * @param {string} message 表示メッセージ
     */
    this.error = function(message){
        setFooter(message, "error");
    }

    function setFooter(message, cls){
        if(text !== message || state !== cls){
            var elements = document.getElementsByTagName("footer");
            if(elements.length > 0){
                elements[0].innerText = message;
                elements[0].className = cls;
            }
            state = cls; text = message;
        }
    }
}

//◆━━━━━━━━━━━━━━━━━━━━━━━━━━━━◆

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