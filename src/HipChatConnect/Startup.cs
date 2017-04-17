using System;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using HipChatConnect.Services;
using HipChatConnect.Services.Impl;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using MediatR;
using HipChatConnect.Controllers.Listeners.TeamCity;
using StackExchange.Redis;
using System.Linq;
using Microsoft.Bot.Connector;

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
            services.AddSingleton(_ => Configuration);

            services.Configure<AppSettings>(settings =>
            {
                settings.BaseUrl = Configuration["BASE_URL"];
                settings.TeamsNubotTeamCityIncomingWebhookUrl = Configuration["TEAMSNUBOTTEAMCITYINCOMINGWEBHOOK_URL"];
            });

            services.AddSingleton<HttpClient>();
            services.AddSingleton<ITenantService, TenantService>();
            services.AddSingleton<IHipChatRoom, HipChatRoom>();
            services.AddSingleton<TeamCityAggregator>();

            services.AddSingleton<IConnectionMultiplexer, ConnectionMultiplexer>(
                provider => ConnectionMultiplexer.Connect(GetRedisIpConfiguration()));

            services.AddSingleton(provider =>
            {
                var connectionMultiplexer = provider.GetService<IConnectionMultiplexer>();
                return connectionMultiplexer.GetDatabase();
            });

            // Add framework services.
            services.AddCors();
            services.AddMvc(options =>
            {
                options.Filters.Add(typeof(TrustServiceUrlAttribute));
            });

            services.AddMediatR(typeof(Startup));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            app.UseStaticFiles(new StaticFileOptions
            {
                OnPrepareResponse = context =>
                {
                    var headers = context.Context.Response.GetTypedHeaders();
                    headers.CacheControl = new CacheControlHeaderValue
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

            app.UseBotAuthentication(new StaticCredentialProvider(
                Configuration.GetSection(MicrosoftAppCredentials.MicrosoftAppIdKey)?.Value,
                Configuration.GetSection(MicrosoftAppCredentials.MicrosoftAppPasswordKey)?.Value));

            app.UseMvc();

            var teamCityAggregator = app.ApplicationServices.GetService<TeamCityAggregator>();
            teamCityAggregator.Initialization.GetAwaiter().GetResult();
        }

        private string GetRedisIpConfiguration()
        {
            var redisUrl = Configuration["REDIS_URL"];
            
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") != "Development")
            {
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
