using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using BlockStation.Filters;
using BlockStation;
using System.Net;
using System.Data;
using System.Data.SQLite;

namespace BlockStation.Controllers
{
    /// <summary>
    /// ユーザー処理コントロール
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private static object lockobj = new object();
        private static SQLiteConnection con;

        public UserController() {
            lock (lockobj) {
                if(con == null || con.State != ConnectionState.Open) {
                    con = new SQLiteConnection("Data Source=" + Shared.DBPath);
                    con.Open();
                }
            }
        }

        ~UserController() {
            //lock (lockobj) {
            //    connection.Close();
            //}
        }

        /// <summary>
        /// ログイン要求
        /// </summary>
        [HttpGet]
        [Route("login")]
        public ActionResult<string> Login_Get([FromQuery]string id, [FromQuery]string password) {
            return Login_Post(new LoginBody() { id = id, password = password });
        }

        /// <summary>
        /// ログイン要求
        /// </summary>
        [HttpPost]
        [Route("login")]
        public ActionResult<string> Login_Post([FromBody] LoginBody data)
        {
            var res = new ContentResult();
            var info = GetUserInfo(data.id);

            //ユーザー情報チェック
            if (info != null && info.CheckPassword(data.password)) {
                //成功 トークンを作成して返す
                var token = new LoginToken();
                token.id        = data.id;
                token.level     = info.level;
                token.TimeValue = DateTime.Now;
                token.expi      = 60 * 60;

                string tokencode =　Shared.LoginTokenMaker.MakeToken(token);
                res.Content = tokencode;
                
                res.StatusCode = (int)HttpStatusCode.OK;
            }
            else {
                //失敗 401応答
                res.Content = "ユーザーID/パスワードが異なります";
                res.StatusCode = (int)HttpStatusCode.Unauthorized;
            }
            return res;
        }

        /// <summary>
        /// 新規登録
        /// </summary>
        [HttpPost]
        [Route("create")]
        public ActionResult Create([FromBody] CreateBody data)
        {
            var res = new ContentResult();
            res.StatusCode = (int)HttpStatusCode.BadRequest;//400

            if(!Regex.IsMatch(data.id, @"[a-zA-Z0-9]{4,}")) {
                res.Content = "英数字で4文字以上のIDを入力してください。";
                return res;
            }
            if(!Regex.IsMatch(data.password, @"^[a-zA-Z0-9!#%&\(\)\*\+,\-\.\/;<=>\?@\[\]\^_\{|\}~]{4,}$")) {
                res.Content = "英数字で4文字以上のパスワードを入力してください。";
                return res;
            }
            if(!Regex.IsMatch(data.mail, @"^[^@]+@[^@]+$")) {
                res.Content = "正しい形式のメールアドレスを入力してください。";
                return res;
            }

            try { 
                lock (lockobj) {
                    //ダブりチェック
                    if(GetUserInfo(data.id) != null) {
                        res.Content = "このIDは既に使用されています。";
                        return res;
                    }
                    //アカウント作成
                    var info = new UserInfo();
                    info.id          = data.id;
                    info.name        = data.name;
                    info.password    = data.password;
                    info.mail        = data.mail;
                    info.level       = 1;
                    info.create_time = DateTime.Now;
                    CreateUser(info);
                }
                res.StatusCode = (int)HttpStatusCode.OK;
            }catch(Exception) {
                res.StatusCode = (int)HttpStatusCode.InternalServerError;
            }
            return res;
        }
        
        /// <summary>
        /// 情報更新
        /// </summary>
        [HttpPost]
        [Route("update")]
        [ServiceFilter(typeof(LoginCheckFilter))]
        public ActionResult Update([FromBody] CreateBody data) {
                var info = new UserInfo();
                info.id          = data.id;
                info.name        = data.name;
                info.password    = data.password;
                info.mail        = data.mail;
                info.level       = data.level;
            
            try {
                lock (lockobj) {
                    UpdateUser(info);
                }
            } catch (Exception) {
                return new BadRequestResult();
            }
            return new OkResult();
        }

        /// <summary>
        /// ユーザー情報取得
        /// </summary>
        [HttpGet]
        [Route("info")]
        [ServiceFilter(typeof(LoginCheckFilter))]
        public ActionResult UserInfo([FromQuery]string id) {
            var info = GetUserInfo(id);
            if (info == null) {
                return new NotFoundResult();
            }
            var ret = new InfoBody();
            ret.id = info.id;
            ret.name = info.name;
            ret.mail = info.mail;
            ret.level = info.level;
            return new JsonResult(ret);
        }

        /// <summary>
        /// ユーザーリスト取得
        /// </summary>
        [HttpGet]
        [Route("list")]
        [ServiceFilter(typeof(LoginCheckFilter))]
        public ActionResult UserList() {
            List<UserInfo> infos = GetUserInfo();
            var list = new List<InfoBody>();
            foreach (var info in infos) {
                var ret = new InfoBody();
                ret.id = info.id;
                ret.name = info.name;
                list.Add(ret);
            }
            return new JsonResult(list);
        }

        /// <summary>
        /// ユーザーレベル取得
        /// </summary>
        [HttpGet]
        [Route("level")]
        public ActionResult Level([FromQuery]string id) {
            var info = GetUserInfo(id);
            if(info == null) return NotFound();
            return new JsonResult(info==null ? 0 : info.level);
        }

        //◆━━━━━━━━━━━━━━━━━━━━━━━━━━━━◆
        //DB Access
        private List<UserInfo> GetUserInfo() {
            var users = RDBUtility.SelectAll<UserInfo>(con, "DT_USERS");
            users.Sort((a, b) =>  StringComparer.OrdinalIgnoreCase.Compare(a.id, b.id));
            return users;
        }
        private UserInfo GetUserInfo(string id) {
            return RDBUtility.Select<UserInfo>(con, "DT_USERS", $"ID='{id}'");
        }
        private bool CreateUser(UserInfo info) {
            return RDBUtility.Insert(con, "DT_USERS", info);
        }
        private bool UpdateUser(UserInfo info) {
            string sql = "";
            Action<string, object> add = (name, value) => {
                if(sql.Length > 0) sql += ",";
                sql += name + "=" + RDBUtility.ToSqlString(value);
            };

            if(info.level > 0)   add("LEVEL", info.level);
            if(info.mail != null) add("MAIL",info.mail);
            if(info.name != null) add("NAME",info.name);
            if(info.pass_code != null) add("PASS_CODE", info.pass_code);
            
            if(sql.Length==0) return true; 

            sql = "UPDATE DT_USERS SET " + sql + " WHERE ID=" + info.id;

            using(var cmd = con.CreateCommand()) {
                cmd.CommandText = sql;
                return cmd.ExecuteNonQuery()==1;
            }
        }

        //◆━━━━━━━━━━━━━━━━━━━━━━━━━━━━◆
        //Request/Response Body
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
            public int level;
        }
        public class InfoBody
        {
            public string id;
            public string name;
            public string mail;
            public int level;
        }
    }
}
