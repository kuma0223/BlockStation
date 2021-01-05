using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using BlockStation.Filters;
using BlockStation.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BlockStation.Controllers
{
    /// <summary>
    /// ユーザー処理および認証のコントロール
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly ILogger _logger;
        private static SQLiteAdapter con;

        public UserController(ILogger<UserController> logger) {
            _logger = logger;
            con = new SQLiteAdapter(Shared.DBPath);
            con.Open();
        }

        [HttpGet]
        public ActionResult<string> Get() {
            return "success";
        }

        [HttpGet]
        [Route("filter")]
        [ServiceFilter(typeof(LoginCheckFilter))]
        public ActionResult<string> LoginFiltered() {
            return "success";
        }
        
        /// <summary>
        /// ログイン要求
        /// </summary>
        [HttpGet]
        [Route("login")]
        public ActionResult Login_Get([FromQuery]string id, [FromQuery]string password) {
            return Login(new LoginRequest() { id = id, password = password });
        }

        /// <summary>
        /// ログイン要求
        /// </summary>
        [HttpPost]
        [Route("login")]
        public ActionResult Login([FromBody] LoginRequest data) {
            //logger.LogInformation("Call LOGIN");

            var info = con.SelectFirst<UserInfo>(
                $"SELECT * FROM DT_USERS WHERE ID='{data.id}'");

            if(info == null) {
                return Unauthorized("Incorrect id.");
            }

            if (info.CheckPassword(data.password)) {
                var res = MakeTokens(info);
                return Ok(res);
            } else {
                return Unauthorized("Incorrect password.");
            }
        }

        /// <summary>
        /// トークン再発行
        /// </summary>
        [HttpPost]
        [Route("refresh")]
        public ActionResult Refresh([FromBody] RefreshRequest data) {
            //不正トークン
            if (!Shared.RefreshTokenMaker.CheckToken(data.refreshToken)) {
                return Unauthorized("Illegal token.");
            }

            RefreshToken rtoken;
            try {
                rtoken = Shared.RefreshTokenMaker.AnalyseToken(data.refreshToken);
            }catch(Exception ex) {
                return Unauthorized("Illegal token.");
            }

            //期限切れ
            if (rtoken.ExpirationTime < DateTime.Now) {
                return Unauthorized("Token expired.");
            }

            //情報が更新されている
            var info = con.SelectFirst<UserInfo>(
                $"SELECT * FROM DT_USERS WHERE ID='{rtoken.id}'");

            if (rtoken.IssuedAt < DateTime.MinValue) {
                return Unauthorized("Token expired.");
            }

            return Ok(MakeTokens(info));
        }

        private LoginResponse MakeTokens(UserInfo info) {
            var res = new LoginResponse();
            DateTime now = DateTime.Now;

            var tokenL = new LoginToken();
            tokenL.id = info.id;
            tokenL.level = 0;
            tokenL.IssuedAt = now;
            tokenL.ExpirationTime = now.AddMinutes(1);
            res.loginToken = Shared.LoginTokenMaker.MakeToken(tokenL);

            var tokenR = new RefreshToken();
            tokenR.id = info.id;
            tokenR.IssuedAt = now;
            tokenR.ExpirationTime = now.AddMonths(1);
            res.refreshToken = Shared.RefreshTokenMaker.MakeToken(tokenR);

            return res;
        }

        //------------------------------
        //Request/Response Body

        public class LoginRequest
        {
            public string id { get; set; }
            public string password { get; set; }
        }
        public class LoginResponse
        {
            public string loginToken { get; set; }
            public string refreshToken { get; set; }
        }
        public class RefreshRequest
        {
            public string refreshToken { get; set; }
        }

        public class UserInfo
        {
            /// <summary>
            /// ユーザーID
            /// </summary>
            [DataMember]
            public string id { get; set; }

            /// <summary>
            /// 名前
            /// </summary>
            [DataMember]
            public string name { get; set; }

            /// <summary>
            /// メアド
            /// </summary>
            [DataMember]
            public string mail { get; set; }

            /// <summary>
            /// エンコードパスワード
            /// </summary>
            [DataMember]
            public string pass_code { get; set; }

            /// <summary>
            /// 登録日
            /// </summary>
            [DataMember]
            public DateTime create_time { get; set; }

            /// <summary>
            /// ユーザレベル
            /// </summary>
            [DataMember]
            public int level { get; set; }

            /// <summary>
            /// パスワード平文
            /// </summary>
            public string password {
                set { pass_code = ToPasscode(value); }
            }

            public bool CheckPassword(string password) {
                var code = ToPasscode(password);
                return code == pass_code;
            }

            private string ToPasscode(string value) {
                if (value == null) value = "";
                var sha = new System.Security.Cryptography.SHA256CryptoServiceProvider();
                var bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(value));
                var buil = new System.Text.StringBuilder(bytes.Length * 2);
                foreach (var x in bytes) buil.Append(x.ToString("X2"));
                return buil.ToString();
            }
        }
    }
}
