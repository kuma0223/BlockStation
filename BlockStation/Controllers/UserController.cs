using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using BlockStation.Filters;
using BlockStation;
using System.Net;
using Microsoft.Extensions.Logging;

namespace BlockStation.Controllers
{
    /// <summary>
    /// ユーザー処理コントロール
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private static object updatelock = new object();
        private static SQLiteAdapter con;
        private ILogger logger;

        public UserController(ILogger<UserController> logger) {
            this.logger = logger;
            if(con == null) {
                con = new SQLiteAdapter("Data Source=" + Shared.DBPath);
                con.Open();
            }
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
            logger.LogInformation("Call LOGIN");
            var info = GetUserInfo(data.id);

            //ユーザー情報チェック
            if (info != null && info.CheckPassword(data.password)) {
                //成功 トークンを作成して返す
                var token = new LoginToken();
                token.id = data.id;
                token.level = info.level;
                token.TimeValue = DateTime.Now;
                token.expi = 24 * 60 * 60;

                string tokencode = Shared.LoginTokenMaker.MakeToken(token);
                return Ok(tokencode);
            } else {
                //失敗 401応答
                var res = new ContentResult();
                res.Content = "ユーザーID/パスワードが異なります";
                res.StatusCode = (int)HttpStatusCode.Unauthorized;
                return res;
            }
        }

        /// <summary>
        /// ログイントークンの更新
        /// </summary>
        [HttpGet]
        [Route("reload")]
        [ServiceFilter(typeof(LoginCheckFilter))]
        public ActionResult Reload() {
            //フィルタを通ってればOK
            var auth = Request.Headers["Authorization"].ToString().Split(' ')[1];
            var token = Shared.LoginTokenMaker.AnalyseToken(auth);
            token.TimeValue = DateTime.Now;

            return Ok(Shared.LoginTokenMaker.MakeToken(token));
        }

        /// <summary>
        /// 新規登録
        /// </summary>
        [HttpPost]
        [Route("create")]
        [ServiceFilter(typeof(LoginCheckFilter))]
        public ActionResult Create([FromBody] CreateBody data)
        {
            if(!Regex.IsMatch(data.id, @"[a-zA-Z0-9]{4,}")) {
                return BadRequest("英数字で4文字以上のIDを入力してください。");
            }
            if(!Regex.IsMatch(data.password, @"^[a-zA-Z0-9!#%&\(\)\*\+,\-\.\/;<=>\?@\[\]\^_\{|\}~]{4,}$")) {
                return BadRequest("英数字で4文字以上のパスワードを入力してください。");
            }
            if(!Regex.IsMatch(data.mail, @"^[^@]+@[^@]+$")) {
                return BadRequest("正しい形式のメールアドレスを入力してください。");
            }

            try {
                lock (updatelock) {
                    //ダブりチェック
                    if (GetUserInfo(data.id) != null) {
                        return BadRequest("このIDは既に使用されています。");
                    }
                    //アカウント作成
                    var info = new UserInfo();
                    info.id = data.id;
                    info.name = data.name;
                    info.password = data.password;
                    info.mail = data.mail;
                    info.level = 1;
                    info.create_time = DateTime.Now;
                    CreateUser(info);
                    return Ok();
                }
            }catch(Exception) {
                var res = new ContentResult();
                res.StatusCode = (int)HttpStatusCode.InternalServerError;
                return res;
            }
        }
        
        /// <summary>
        /// 情報更新
        /// </summary>
        [HttpPost]
        [Route("update")]
        [ServiceFilter(typeof(LoginCheckFilter))]
        public ActionResult Update([FromBody] CreateBody data) {
            var info = new UserInfo();
            info.id = data.id;
            info.name = data.name;
            info.password = data.password;
            info.mail = data.mail;
            info.level = data.level;

            try {
                lock (updatelock) {
                    UpdateUser(info);
                }
            } catch (Exception) {
                return new BadRequestResult();
            }
            return Ok();
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
            return Ok(ret);
        }

        /// <summary>
        /// ユーザーリスト取得
        /// </summary>
        [HttpGet]
        [Route("list")]
        [ServiceFilter(typeof(LoginCheckFilter))]
        public ActionResult UserList() {
            var infos = GetUserInfo();
            infos.Sort((x, y) => x.id.CompareTo(y.id));

            var str = new StringBuilder();
            foreach (var info in infos) {
                if (str.Length > 0) str.Append(',');
                str.Append("{");
                str.Append($"\"id\":\"{info.id}\",");
                str.Append($"\"name\":\"{info.name}\"");
                str.Append("}");
            }
            var ret = new ContentResult();
            ret.ContentType = "application/json";
            ret.Content = "[" + str + "]";
            ret.StatusCode = (int)HttpStatusCode.OK;
            return ret;
        }

        /// <summary>
        /// ユーザーレベル取得
        /// </summary>
        [HttpGet]
        [Route("level")]
        public ActionResult Level([FromQuery]string id) {
            var info = GetUserInfo(id);
            if (info == null) return NotFound();
            return Ok(info == null ? 0 : info.level);
        }

        //◆━━━━━━━━━━━━━━━━━━━━━━━━━━━━◆
        //DB Access
        
        private List<UserInfo> GetUserInfo() {
            return con.SelectAll<UserInfo>("DT_USERS");
        }
        private UserInfo GetUserInfo(string id) {
            return con.Select<UserInfo>("DT_USERS", $"ID='{id}'");
        }
        private bool CreateUser(UserInfo info) {
            return con.Insert("DT_USERS", info);
        }
        private bool UpdateUser(UserInfo info) {
            string sql = "";
            Action<string, object> add = (name, value) => {
                if(sql.Length > 0) sql += ",";
                sql += name + "=" + con.ToSql(value);
            };

            if(info.level > 0)   add("LEVEL", info.level);
            if(info.mail != null) add("MAIL",info.mail);
            if(info.name != null) add("NAME",info.name);
            if(info.pass_code != null) add("PASS_CODE", info.pass_code);
            
            if(sql.Length==0) return true; 

            sql = "UPDATE DT_USERS SET " + sql + " WHERE ID=" + info.id;
            return con.ExecuteNonQuery(sql) == 1;
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
