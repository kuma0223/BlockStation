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
    /// クロスドメイン有効フィルター
    /// </summary>
    public class AllowOriginFilter : IResultFilter
    {
        public void OnResultExecuted(ResultExecutedContext context) {
        }

        public void OnResultExecuting(ResultExecutingContext context) {
            context.HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", "*");
        }
    }
}