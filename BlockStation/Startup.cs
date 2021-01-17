using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using BlockStation.Filters;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BlockStation
{
    public class Startup
    {
        IWebHostEnvironment _env;

        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration, IWebHostEnvironment env) {
            Configuration = configuration;
            _env = env;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services) {

            //フィルタ
            services.AddScoped<LoginCheckFilter>();

            //クロスドメイン許可
            if (_env.IsDevelopment()) {
                services.AddCors();
            }

            //コントローラ
            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory) {
            if (env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();
            app.UseRouting();
            //app.UseAuthorization();

            if (env.IsDevelopment()) {
                //静的ファイルを使用する
                app.UseStaticFiles();
                //クロスドメイン許可
                app.UseCors(builder=>{
                    builder.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader();
                });
            }

            app.UseEndpoints(endpoints => {
                endpoints.MapControllers();
            });

            //アプリケーションルートパス
            Shared.ContentRootPath = env.ContentRootPath;

            //設定読み込み
            Shared.DBPath = Configuration.GetSection("DBPath").Value;
            if (Shared.DBPath.StartsWith(".")) {
                Shared.DBPath = Shared.ContentRootPath + "/" + Shared.DBPath;
            }

            //ロガー
            var logprov = new MyLoggerProvider(Configuration.GetSection("MyLogging"));
            loggerFactory.AddProvider(logprov);

            logprov.CreateLogger("App").LogInformation("Startup");

            //共通部品
            var tkey = Configuration.GetSection("TokenHashKey").Value;
            Shared.LoginTokenMaker = new TokenMaker<LoginToken>(tkey);
            Shared.RefreshTokenMaker = new TokenMaker<RefreshToken>(tkey);
        }
    }
}
