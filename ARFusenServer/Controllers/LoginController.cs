using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization.Json;
using System.Web.Http;
using System.Text.RegularExpressions;

namespace ARFusenServer.Controllers
{
    /// <summary>
    /// ログイン処理コントロール
    /// </summary>
    [RoutePrefix("api/Login")]
    public class LoginController : ApiController
    {
        private static object lockobj = new object();

        /// <summary>
        /// ログイン要求
        /// </summary>
        [HttpPost]
        [Route("login")]
        public HttpResponseMessage Login([FromBody] LoginBody data)
        {
            var res = new HttpResponseMessage();
            var info = GetUserInfo(data.id);

            if (info != null && info.CheckPassword(data.password)) {
                //成功時 トークンを作成して返す
                var token = new LoginToken();
                token.id = data.id;
                token.time = DateTimeEx.ToLong(DateTime.Now);
                token.expi = 1440 * 60;

                string tokencode = Shared.TokenMaker.MakeToken(JsonSilializer<LoginToken>.ToJson(token));
                res.Content = new StringContent(tokencode);
                res.StatusCode = HttpStatusCode.OK;
            }
            else {
                //失敗時 401応答
                res.StatusCode = HttpStatusCode.Unauthorized;
                res.Content = new StringContent("ユーザーID/パスワードが異なります");
            }
            return res;
        }

        /// <summary>
        /// 新規登録
        /// </summary>
        [HttpPost]
        [Route("create")]
        public HttpResponseMessage Create([FromBody] CreateBody data)
        {
            var res = new HttpResponseMessage();
            res.StatusCode = HttpStatusCode.BadRequest;//400

            if(!Regex.IsMatch(data.id, @"[a-zA-Z0-9]{4,}")) {
                res.Content = new StringContent("英数字で4文字以上のIDを入力してください。");
                return res;
            }
            if(!Regex.IsMatch(data.password, @"^[a-zA-Z0-9!#%&\(\)\*\+,\-\.\/;<=>\?@\[\]\^_\{|\}~]{4,}$")) {
                res.Content = new StringContent("英数字で4文字以上のIDを入力してください。");
                return res;
            }
            if(!Regex.IsMatch(data.mail, @"^[^@]+@[^@]+$")) {
                res.Content = new StringContent("正しい形式のメールアドレスを入力してください。");
                return res;
            }

            try { 
                lock (lockobj) { 
                    //ダブりチェック
                    var path = Shared.DataDirctory + "/Users/" + data.id + ".json";
                    if (System.IO.File.Exists(path)) {
                        res.Content = new StringContent("このユーザーIDは既に使用されています。");
                        return res;
                    }

                    //アカウント作成
                    var info = new AccountInfo();
                    info.id = data.id;
                    info.password = data.password;
                    info.createTime = DateTimeEx.ToLong(DateTime.Now);
                    JsonSilializer<AccountInfo>.WriteFile(path, info);
                    //専用ディレクトリ作成
                    var dir = Shared.DataDirctory + "/Box_" + info.id;
                    System.IO.Directory.CreateDirectory(dir);
                }
                res.StatusCode = HttpStatusCode.OK;
            }catch(Exception) {
                res.StatusCode = HttpStatusCode.InternalServerError;
            }
            return res;
        }

        public AccountInfo GetUserInfo(string id)
        {
            if (id == null || id == "") return null;

            var path = Shared.DataDirctory + "/Users/" + id + ".json";
            if (!System.IO.File.Exists(path)) return null;

            var info = JsonSilializer<AccountInfo>.ReadFile(path);
            return info;
        }

        public class LoginBody
        {
            public string id;
            public string password;
        }
        public class CreateBody
        {
            public string id;
            public string password;
            public string name;
            public string mail;
        }
    }
}
