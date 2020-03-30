using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization.Json;
using System.Web.Http;
using System.IO;
using ARFusenServer.Filters;

namespace ARFusenServer.Controllers
{
    [RoutePrefix("api/ArValue")]
    public class ArValueController : ApiController
    {
        [HttpGet]
        public string Get()
        {
            return "Success";
        }
        
        [LoginCheckFilter]
        [HttpGet]
        [Route("getList")]
        public string[] GetList()
        {
            var token = LoginToken.GetInstanceFromToken(this.Request.Headers.Authorization.Parameter);
            string dir = Shared.DataDirctory + "/Box_" + token.id;
            var list = new List<string>();
            foreach(string fname in Directory.GetFiles(dir)) { 
                list.Add(Path.GetFileNameWithoutExtension(fname));
            }
            return list.ToArray();
        }
        
        [LoginCheckFilter]
        [HttpGet]
        [Route("getItem/{itemNo}")]
        public ArViewData Get(string itemNo)
        {
            var token = LoginToken.GetInstanceFromToken(this.Request.Headers.Authorization.Parameter);
            string path = Shared.DataDirctory + "/Box_" + token.id + "/" + itemNo + ".json";
            return JsonSilializer<ArViewData>.ReadFile(path);
        }
        
        [LoginCheckFilter]
        [HttpPost]
        [Route("putItem")]
        public int Post([FromBody]ArViewData data)
        {
            var token = LoginToken.GetInstanceFromToken(this.Request.Headers.Authorization.Parameter);
            data.Code = token.id + "_" + data.No;
            string path = Shared.DataDirctory + "/Box_" + token.id + "/" + data.No + ".json";
            JsonSilializer<ArViewData>.WriteFile(path, data);
            return 0;
        }
        
        [HttpGet]
        [Route("getItemFromQr/{code}")]
        public ArViewData GetItemGuest(string code)
        {
            if (code == null) return new ArViewData();

            var spl = code.Split('_');
            if (spl.Length < 2) return new ArViewData();

            string path = Shared.DataDirctory + "/Box_" + spl[0] + "/" + spl[1] + ".json";
            if(!File.Exists(path)) return new ArViewData();

            return JsonSilializer<ArViewData>.ReadFile(path);
        }
    }
}