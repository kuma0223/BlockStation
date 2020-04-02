using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using BlockStation.Filters;

namespace BlockStation
{
    public class Startup
    {
        public Startup(IConfiguration configuration) {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services) {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
            
            services.AddScoped<LoginCheckFilter>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env) {
            if (env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            } else {
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseMvc();

            //アプリケーションルートパス
            Shared.ContentRootPath = env.ContentRootPath;
            
            //設定読み込み
            Shared.DBPath = Configuration.GetSection("DBPath").Value;
            if (Shared.DBPath.StartsWith(".")) {
                Shared.DBPath = Shared.ContentRootPath + "/" + Shared.DBPath;
            }

            var tkey = Configuration.GetSection("TokenHashKey").Value;
            Shared.LoginTokenMaker = new TokenMaker<LoginToken>(tkey);

            //静的ファイルを使用する
            app.UseStaticFiles();
        }
    }
}
