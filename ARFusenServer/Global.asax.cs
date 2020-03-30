using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Routing;

namespace ARFusenServer
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            Shared.RootPath = System.Web.HttpContext.Current.Server.MapPath("./");
            GlobalConfiguration.Configure(WebApiConfig.Register);

            var dir = ConfigurationManager.AppSettings["DataDirectory"];
            Shared.DataDirctory = Shared.GetAbsolutePath(dir);

            var key = ConfigurationManager.AppSettings["AuthTokenKey"];
            Shared.TokenMaker = new AuthTokenMaker(key);
        }
    }

    public static class Shared
    {
        public static string RootPath { get; set; }
        public static string DataDirctory { get; set; }

        public static AuthTokenMaker TokenMaker { get; set; }

        public static string GetAbsolutePath(string path)
        {
            if (path.Length >= 2 && path[1] == ':')
                return path;
            else
                return (Shared.RootPath + path);
        }
    }

    public static class DateTimeEx
    {
        private static DateTime BaseTime = new DateTime(1970, 1, 1, 9, 0, 0, 0);

        public static DateTime ToDateTime(long sec)
        {
            return BaseTime.AddSeconds(sec);
        }
        public static long ToLong(DateTime d)
        {
            return (long)(d - BaseTime).TotalSeconds;
        }
    }
}
