using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using HipChatConnect.Core.Cache;
using HipChatConnect.Core.Cache.Impl;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using StackExchange.Redis;

namespace HipChatConnect
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<AppSettings>(settings =>
            {
                settings.BaseUrl = Configuration["BASE_URL"];
            });

            services.AddDistributedRedisCache(option =>
            {
                option.Configuration = GetRedisIpConfiguration();
                option.InstanceName = "master";
            });

            services.AddSingleton<ICache, Cache>();

            // Add framework services.
            services.AddCors();
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            app.UseStaticFiles(new StaticFileOptions()
            {
                OnPrepareResponse = (context) =>
                {
                    var headers = context.Context.Response.GetTypedHeaders();
                    headers.CacheControl = new CacheControlHeaderValue()
                    {
                        NoCache = true
                    };
                }
            });
            app.UseCors(builder =>
            {
                builder.WithOrigins("*")
                    .WithMethods("GET")
                    .WithHeaders("accept", "content-type", "origin");
            });
            app.UseMvc();
        }

        private string GetRedisIpConfiguration()
        {
            var redisUrl = Configuration["REDIS_URL"];

            var config = ConfigurationOptions.Parse(redisUrl);

            var addressEndpoint = config.EndPoints.First() as DnsEndPoint;
            var port = addressEndpoint.Port;

            var isIp = IsIpAddress(addressEndpoint.Host);
            if (!isIp)
            {
                //Please Don't use this line in blocking context. Please remove ".Result"
                //Just for test purposes
                var ip = Dns.GetHostEntryAsync(addressEndpoint.Host).Result;

                return $"{ip.AddressList.First()}:{port}";
            }

            return redisUrl;
        }

        bool IsIpAddress(string host)
        {
            string ipPattern = @"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b";
            return Regex.IsMatch(host, ipPattern);
        }


    }
}
