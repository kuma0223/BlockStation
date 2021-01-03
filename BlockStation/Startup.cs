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
        public Startup(IConfiguration configuration) {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services) {
            services.AddControllers();

            //�t�B���^�o�^
            services.AddScoped<LoginCheckFilter>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
            if (env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();
            app.UseRouting();
            //app.UseAuthorization();

            app.UseEndpoints(endpoints => {
                endpoints.MapControllers();
            });

            //�ÓI�t�@�C�����g�p����
            if (env.IsDevelopment()) {
                app.UseStaticFiles();
            }

            //�A�v���P�[�V�������[�g�p�X
            Shared.ContentRootPath = env.ContentRootPath;

            ////�ݒ�ǂݍ���
            Shared.DBPath = Configuration.GetSection("DBPath").Value;
            if (Shared.DBPath.StartsWith(".")) {
                Shared.DBPath = Shared.ContentRootPath + "/" + Shared.DBPath;
            }

            //���ʕ��i
            var tkey = "abcdefg";//Configuration.GetSection("TokenHashKey").Value;
            Shared.LoginTokenMaker = new TokenMaker<LoginToken>(tkey);
            Shared.RefreshTokenMaker = new TokenMaker<RefreshToken>(tkey);
        }
    }
}
