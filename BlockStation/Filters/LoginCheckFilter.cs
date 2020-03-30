using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Controllers;
using BlockStation;

namespace BlockStation.Filters
{
    /// <summary>
    /// ログインチェックフィルター
    /// </summary>
    public class LoginCheckFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext actionContext) {
            var auth = actionContext.HttpContext.Request.Headers["Authorization"].ToString().Split(' ');

            if (auth.Length < 2 || auth[0] != "Bearer" || !CheckToken(auth[1])) {
                //未認証 or 不正トークン
                
                actionContext.Result = new Microsoft.AspNetCore.Mvc.ContentResult(){
                    Content = "ログイン認証が必要です。",
                    StatusCode = (int)HttpStatusCode.Unauthorized
                };
            }
            actionContext.HttpContext.Response.Headers["WWWAuthenticate"].Append("Bearer realm=\"\"");
        }
        
        void IActionFilter.OnActionExecuted(ActionExecutedContext context) {
        }

        private bool CheckToken(string token) {
            if (token == null || token == "") return false;
            if (!Shared.LoginTokenMaker.CheckToken(token)) return false;

            //有効期限
            var tokenbody = Shared.LoginTokenMaker.AnalyseToken(token);
            if(tokenbody.ExpirationDate < DateTime.Now) {
                return false;
            }

            return true;
        }
    }
}