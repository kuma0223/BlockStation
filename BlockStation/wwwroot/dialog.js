//DOM操作でダイアログを表示します。
//ダイアログを多重に開くことはできません。

window.Dialog = new function(){
    var me = this;
    
    var _screen;    //ダイアログ挿入用タグエレメント
    var _dialog;    //最終表示ダイアタグエレメント
    var _bank;      //過去ダイアログの保管用タグエレメント
    var _callback;  //閉じた際のコールバック
    var _dialogOn = false; //表示状態

    //メッセージボックス用定義
    var messageboxCode = `
        <div class='dialog messagebox'>
            <div style="display:flex; align-items:center; padding:15px">
            
                <svg class="messagebox_icons" width="45" height="45" viewBox="0 0 45 45" preserveAspectRatio="none"
                    style="display: block; min-width:45px; min-heigh:45px; margin-right:10px">
                    <g class="messagebox_icon_info" style="display:none">
                        <rect x=2 y=5 width="5" height="6" fill="#20A050"></rect>
                        <rect x=2 y=15 width="5" height="20" fill="#20A050"></rect>
                        <g transform="translate(10,10)">
                            <rect width="30" height="25" fill="#554050"></rect>
                            <rect x=5  y=8 width="3" height="3" fill="white"></rect>
                            <rect x=22 y=8 width="3" height="3" fill="white"></rect>
                            <rect x=7  y=17 width="16" height="4" fill="white"></rect>
                        </g>
                    </g>
                    <g class="messagebox_icno_warn" style="display:none">
                        <rect x=2 y=5 width="5" height="20" fill="#C04050"></rect>
                        <rect x=2 y=30 width="5" height="6" fill="#C04050"></rect>
                        <g transform="translate(10,10)">
                            <rect width="30" height="25" fill="#554050"></rect>
                            <rect x=5  y=8 width="3" height="3" fill="white"></rect>
                            <rect x=22 y=8 width="3" height="3" fill="white"></rect>
                            <rect x=12  y=16 width="6" height="5" fill="white"></rect>
                        </g>
                    </g>
                </svg>
                
                <p style='min-height:0px;min-width:250px;'>【】</p>
            </div>
            
            <div style='text-align:center;padding:8px;display:none' class='messagebox_buttons_okcancel'>
                <span class='button' onclick='Dialog.close(true)'>ＯＫ</span>
                <span class='button' onclick='Dialog.close(false)'>キャンセル</span>
            </div>
            <div style='text-align:center;padding:8px;display:none' class='messagebox_buttons_ok'>
                <span class='button' onclick='Dialog.close(true)'>ＯＫ</span>
            </div>
        </div>`;

    this.Messagebox_Buttons_OkCancel = "messagebox_buttons_okcancel";
    this.Messagebox_Buttons_Ok = "messagebox_buttons_ok";
    
    this.Messagebox_Icon_Info = "messagebox_icon_info";
    this.Messagebox_Icon_Warn = "messagebox_icno_warn";
    
    //----------------------------------------

    window.addEventListener("DOMContentLoaded", function(){
        //最初にダイアログ用タグをbodyに追加。
        _screen = document.createElement("div");
        _screen.id = "dialogScreen";
        document.body.appendChild(_screen);

        _bank = document.createElement("div");
        _bank.id = "dialogBank";
        _bank.style.display = "none";
        document.body.appendChild(_bank);
    });

    //----------------------------------------

    /**
     * メッセージボックスを表示します。
     * コールバックの引数には OK:true,キャンセル:false が渡されます。
     * @param {string} message 表示文字列
     * @param {string} buttons Messagebox_Button_メンバから選択
     * @param {function} callback ダイアログを閉じたときに呼ばれるコールバック
     */
    this.messagebox = function(message, buttons, icon, callback){
        _callback = callback;
        //古いダイアログを除去
        inToBank();
        _dialog = null;
        //改行の置換
        message = message.replace(/\n/g,"<br/>");
        //タグ挿入
        _screen.innerHTML = messageboxCode.replace("【】", message);
        _dialog = _screen.getElementsByClassName("dialog")[0];
        //ボタン選択
        if(buttons === undefined) buttons=me.Messagebox_Buttons_Ok;
        var buttonsElement = _dialog.getElementsByClassName(buttons)[0];
        if(buttonsElement) buttonsElement.style.display = "block";
        //アイコン
        if(!icon) _dialog.getElementsByClassName("messagebox_icons")[0].style.display = "none";
        var iconElement = _dialog.getElementsByClassName(icon)[0];
        if(iconElement) iconElement.style.display = "block";
        //表示
        _screen.classList.add("on");
        _dialog.classList.add("on");
        _dialogOn = true;
    }
    
    this.messageboxAsync = async function (message, buttons, icon) {
        return new Promise(resolve => {
            me.messagebox(message, buttons, icon, resolve);
        })
    }

    /**
     * 指定したタグエレメントをダイアログとして表示します。
     * エレメントはDOMから除去され、ダイアログ領域（div#dialgScreen）に再挿入されます。
     * その後に別のダイアログを表示すると非表示の保管領域（div#dialogBank）に移動されます。
     *
     * 指定するエレメントは以下を満たす必要があります。
     *  ・dialogクラスを設定している
     *  ・Dialog.close を呼び出すコードがある
     *
     * @param {HTMLElement} element 表示エレメント
     * @param {function} callback ダイアログを閉じた時に呼ばれるコールバック
     */
    this.showElement = function(element, callback){
        if(element === null || element === undefined) return;
        _callback = callback;
        
        //エレメントを今の親から削除
        if(element.parentElement){
            element.parentElement.removeChild(element);
        }

        //古いダイアログを除去
        inToBank();
        _dialog = null;

        //スクリーンへ挿入、表示
        _screen.appendChild(element);
        _dialog = element;
        
        _screen.classList.add("on");
        _dialog.classList.add("on");
        _dialogOn = true;
    }

    this.showElementAsync = async function (element, callback) {
        return new Promise(resolve => {
            me.showElement(element, resolve);
        })
    }

    /**
     * ダイアログを閉じます。
     * 引数はそのままコールバックへ引き渡されます。
     * @param {any} result コールバック引数
     */
    this.close = function(result){
        if(_dialogOn){
            _screen.classList.remove("on");
            _dialog.classList.remove("on");
            _dialogOn = false;

            if(_callback !== undefined && _callback !== null){
                _callback(result);
            }
        }
    }

    function inToBank(){
        var dlgs = _screen.getElementsByClassName("dialog");
        if(dlgs.length < 1){ return; }
        
        var dlg = dlgs[0];
        if(!dlgs[0].classList.contains("messagebox")){
            _screen.removeChild(dlg);
            _bank.appendChild(dlg);
        }
        //中をきれいに
        while(_screen.firstChild){ _screen.removeChild(_screen.firstChild); }
    }
}