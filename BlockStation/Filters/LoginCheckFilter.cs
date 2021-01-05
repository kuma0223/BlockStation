using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Controllers;
using BlockStation;
using System.Text.RegularExpressions;

namespace BlockStation.Filters
{
    /// <summary>
    /// ログインチェックフィルター
    /// </summary>
    public class LoginCheckFilter : IActionFilter
    {
        private static Regex regex = new Regex(@"Bearer\s+(.*)");

        public void OnActionExecuting(ActionExecutingContext actionContext) {
            var auth = actionContext.HttpContext.Request.Headers["Authorization"].ToString();
            var mc = regex.Match(auth);

            if (!mc.Success || !CheckToken(mc.Groups[1].Value)) {
                //未認証 or 不正トークン
                actionContext.Result = new ContentResult(){
                    Content = "Please login",
                    StatusCode = (int)HttpStatusCode.Unauthorized
                };
            }
            actionContext.HttpContext.Response.Headers["WWWAuthenticate"].Append("Bearer");
        }
        
        void IActionFilter.OnActionExecuted(ActionExecutedContext context) {
        }

        private bool CheckToken(string token) {
            if (token == null || token == "") return false;
            
            //改ざん検知
            if (!Shared.LoginTokenMaker.CheckToken(token)){
                return false;
            }
            //有効期限
            var tokenbody = Shared.LoginTokenMaker.AnalyseToken(token);
            if(tokenbody.ExpirationTime < DateTime.Now) {
                return false;
            }
            //無効トークン
            //なし

            return true;
        }
    }
}