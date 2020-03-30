using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net;
using System.Net.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using System.Net.Http.Headers;

namespace ARFusenServer.Filters
{
    /// <summary>
    /// ログインチェックフィルター
    /// </summary>
    public class LoginCheckFilterAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            var auth = actionContext.Request.Headers.Authorization;
            if (auth == null || !CheckToken(auth.Parameter))
            {   //未認証 or 不正トークン
                actionContext.Response = new HttpResponseMessage(HttpStatusCode.Unauthorized);
                actionContext.Response.Headers.WwwAuthenticate.Add(new AuthenticationHeaderValue("Bearer", "realm=\"\""));
                actionContext.Response.Content = new StringContent("ログイン認証が必要です。");
            }
            base.OnActionExecuting(actionContext);
        }

        private bool CheckToken(string token)
        {
            if (token == null || token == "") return false;
            if (!Shared.TokenMaker.CheckToken(token)) return false;

            //有効期限
            var tokenbody = LoginToken.GetInstanceFromToken(token);
            if (DateTimeEx.ToDateTime(tokenbody.time).AddSeconds(tokenbody.expi) < DateTime.Now)
                return false;

            return true;
        }
    }
}