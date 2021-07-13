using MeowvBlog.API.Configurations;
using MeowvBlog.API.Infrastructure;
using MeowvBlog.API.Jobs;
using MeowvBlog.API.Models.Dto.Response;
using MeowvBlog.API.Swagger;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MeowvBlog.Web
{
    public class Program
    {
        /// <summary>
        /// Main�������������
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static async Task Main(string[] args)
        {
            // ��ʼ���������˿�5002�����Nginx�˿�ת��
            await Host.CreateDefaultBuilder(args)
                      .ConfigureWebHostDefaults(builder =>
                      {
                          builder.ConfigureKestrel(options => { options.AddServerHeader = false; })
                                 .UseUrls("http://*:5002")
                                 .UseStartup<Program>();
                      }).Build().RunAsync();
        }

        /// <summary>
        /// This method gets called by the runtime. Use this method to add services to the container.
        /// </summary>
        /// <param name="services"></param>
        public void ConfigureServices(IServiceCollection services)
        {
            // ע���������������֧��NewtonsoftJson
            services.AddControllers().AddNewtonsoftJson();
            // ע��Sqlite���ݿ�������
            services.AddDbContext<MeowvBlogDBContext>();
            // ע�����BackgroundService�ļ򵥶�ʱ����
            services.AddTransient<IHostedService, RemindJob>();
            // ·������
            services.AddRouting(options =>
            {
                // ����URLΪСд
                options.LowercaseUrls = true;
                // �����ɵ�URL�������б��
                options.AppendTrailingSlash = true;
            });
            // Swagger��չ
            services.AddSwagger();
            // �����֤֮JWT
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(options =>
                    {
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            // �Ƿ���֤�䷢��
                            ValidateIssuer = true,
                            // �Ƿ���֤����Ⱥ��
                            ValidateAudience = true,
                            // �Ƿ���֤������
                            ValidateLifetime = true,
                            // ��֤Token��ʱ��ƫ����
                            ClockSkew = TimeSpan.FromSeconds(30),
                            // �Ƿ���֤��ȫ��Կ
                            ValidateIssuerSigningKey = true,
                            // ����Ⱥ��
                            ValidAudience = AppSettings.JWT.Domain,
                            // �䷢��
                            ValidIssuer = AppSettings.JWT.Domain,
                            // ��ȫ��Կ
                            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(AppSettings.JWT.SecurityKey))
                        };
                    });
            // ��֤��Ȩ
            services.AddAuthorization();
            // �����Ӧ����
            services.AddResponseCaching();
            // MVC����
            services.AddMvcCore(options =>
            {
                // ���һ����Ӧ�����Ĭ������
                options.CacheProfiles.Add("default", new CacheProfile { Duration = 100 });
            }).SetCompatibilityVersion(CompatibilityVersion.Latest);
            // Http����
            services.AddHttpClient();
        }

        /// <summary>
        /// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        /// </summary>
        /// <param name="app"></param>
        /// <param name="env"></param>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // ������������������
            if (env.IsDevelopment())
            {
                // �����쳣ҳ��
                app.UseDeveloperExceptionPage();
            }
            
            // ʹ��HSTS���м�������м��������ϸ��䰲ȫͷ
            app.UseHsts();
            // ��һ����Ƕ������м��ί����ӵ�Ӧ�ó��������ܵ��У����ж��Ƿ���Ȩ
            app.Use(async (context, next) =>
            {
                await next();
                if (context.Response.StatusCode == (int)HttpStatusCode.Unauthorized)
                {
                    var response = new Response { Msg = "Unauthorized" };
                    var content = JsonConvert.SerializeObject(response, Formatting.None, new JsonSerializerSettings
                    {
                        ContractResolver = new CamelCasePropertyNamesContractResolver()
                    });
                    await context.Response.WriteAsync(content);
                }
            });
            // ת������ͷ������ǰ������� Nginx ʹ�ã���ȡ�û���ʵIP
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });
            // ·��
            app.UseRouting();
            // ��Ӧ����
            app.UseResponseCaching();
            // ����
            app.UseCors();
            // �����֤
            app.UseAuthentication();
            // ��֤��Ȩ
            app.UseAuthorization();
            // HTTP => HTTPS
            app.UseHttpsRedirection();
            // Swagger
            app.UseSwagger();
            // SwaggerUI
            app.UseSwaggerUI();
            // ·��ӳ��
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}