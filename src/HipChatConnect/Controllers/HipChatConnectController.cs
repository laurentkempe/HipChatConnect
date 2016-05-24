using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nubot.Plugins.Samples.HipChatConnect.Models;
using StackExchange.Redis;

namespace HipChatConnect.Controllers
{
    [Route("/hipchat")]
    public class HipChatConnectController : Controller
    {
        private readonly IOptions<AppSettings> _settings;
        private readonly IDatabase _database;

        public HipChatConnectController(IOptions<AppSettings> settings, IDatabase database)
        {
            _settings = settings;
            _database = database;
        }

        [HttpGet("atlassian-connect.json")]
        public async Task<string> Get()
        {
            var baseUri = "https://f1eeae2d.ngrok.io";
            //var baseUri = _settings.Value?.NGrokUrl ?? "http://localhost:52060/";

            return await Task.FromResult(GetCapabilitiesDescriptor(baseUri));
        }

        [HttpPost("installable")]
        public async Task<HttpStatusCode> Installable([FromBody]InstallationData installationData)
        {
            await _database.StringSetAsync(installationData.oauthId, JsonConvert.SerializeObject(installationData));

            var capabilitiesRoot = await GetCapabilitiesRoot(installationData);

            var accessToken = await GetAccessToken(installationData, capabilitiesRoot);

            await _database.StringSetAsync(installationData.oauthId, JsonConvert.SerializeObject(installationData));

            return await _database.StringSetAsync("accessToken", JsonConvert.SerializeObject(accessToken)) ? HttpStatusCode.OK : HttpStatusCode.NotFound;
        }

        [HttpGet("uninstalled")]
        public async Task<IActionResult> UnInstalled([FromQuery(Name = "redirect_url")]string redirectUrl,
                                                     [FromQuery(Name = "installable_url")]string installableUrl)
        {
            var client = new HttpClient();

            var httpResponse = await client.GetAsync(installableUrl);
            httpResponse.EnsureSuccessStatusCode();

            var jObject = await httpResponse.Content.ReadAsAsync<JObject>();

            await _database.KeyDeleteAsync((string) jObject["oauthId"]);

            return await Task.FromResult(Redirect(redirectUrl));
        }

        [HttpGet("glance")]
        public string GetGlance([FromQuery(Name = "signed_request")]string signedRequest)
        {
            if (ValidateToken(signedRequest))
            {
                return BuildInitialGlance();
            }

            return "";
        }

        [HttpGet("sidebar")]
        public IActionResult Sidebar([FromQuery(Name = "signed_request")]string signedRequest)
        {
            if (ValidateToken(signedRequest))
            {
                return Redirect("/nubot/index.html");
            }

            return BadRequest();
        }

        //[HttpGet("updateGlance")]
        //public string UpdateGlance([FromQuery(Name = "u")]string updateText)
        //{
        //    var buildInitialGlance = BuildInitialGlance(updateText);

        //    var installationData = (InstallationData)Cache.Values.First();
        //    installationData.
        //}

        /*
        function updateGlanceData(oauthId, roomId, glanceData) {
            var installation = installationStore[oauthId];
            var roomGlanceUpdateUrl = installation.apiUrl + 'addon/ui/room/' + roomId;

            getAccessToken(oauthId, function (token) {
                request.post(roomGlanceUpdateUrl, {
                    auth: {
                        bearer: token['access_token']
                    },
                    json: {
                        glance: [{
                            key: "sample-glance",
                            content: glanceData
                        }]
                    }
                }, function (err, response, body) {
                    logger.info(response);
                    logger.info(err || response.statusCode, roomGlanceUpdateUrl)
                });
            });
        }*/
        private static string GetCapabilitiesDescriptor(string baseUri)
        {
            var capabilitiesDescriptor = new
            {
                name = "Nubot",
                description = "An add-on to talk to Nubot.",
                key = "nubot-addon",
                links = new
                {
                    self = $"{baseUri}/hipchat/atlassian-connect.json",
                    homepage = $"{baseUri}/hipchat/atlassian-connect.json"
                },
                vendor = new
                {
                    name = "Laurent Kempe",
                    url = "http://laurentkempe.com"
                },
                capabilities = new
                {
                    hipchatApiConsumer = new
                    {
                        scopes = new[]
                        {
                            "send_notification",
                            "view_room"
                        }
                    },
                    installable = new
                    {
                        callbackUrl = $"{baseUri}/hipchat/installable",
                        uninstalledUrl = $"{baseUri}/hipchat/uninstalled"
                    },
                    glance = new[]
                    {
                        new
                        {
                            name = new
                            {
                                value = "Hello TC"
                            },
                            queryUrl = $"{baseUri}/hipchat/glance",
                            key = "nubot.glance",
                            target = "nubot.sidebar",
                            icon = new Icon
                            {
                                url = $"{baseUri}/nubot/TC.png",
                                url2 = $"{baseUri}/nubot/TC2.png"
                            }
                        }
                    },
                    webPanel = new[]
                    {
                        new
                        {
                            key = "nubot.sidebar",
                            name = new
                            {
                                value = "Nubot"
                            },
                            icon = new Icon
                            {
                                url = $"{baseUri}/nubot/TC.png",
                                url2 = $"{baseUri}/nubot/TC2.png"
                            },
                            location = "hipchat.sidebar.right",
                            url = $"{baseUri}/hipchat/sidebar"
                        }
                    }
                }
            };

            return JsonConvert.SerializeObject(capabilitiesDescriptor);
        }

        private async Task<CapabilitiesRoot> GetCapabilitiesRoot(InstallationData installationData)
        {
            var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(new Uri(installationData.capabilitiesUrl));
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsAsync<CapabilitiesRoot>();
        }

        private async Task<AccessToken> GetAccessToken(InstallationData installationData, CapabilitiesRoot capabilitiesRoot)
        {
            var client = new HttpClient();

            var dataContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("scope", "send_notification")
            });

            var credentials = Encoding.ASCII.GetBytes($"{installationData.oauthId}:{installationData.oauthSecret}");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(credentials));

            var tokenResponse =
                await client.PostAsync(new Uri(capabilitiesRoot.capabilities.oauth2Provider.tokenUrl), dataContent);
            return await tokenResponse.Content.ReadAsAsync<AccessToken>();
        }

        private bool ValidateToken(string jwt)
        {
            var jwtSecurityTokenHandler = new JwtSecurityTokenHandler();
            var readToken = jwtSecurityTokenHandler.ReadToken(jwt);

            var redisValue = _database.StringGet(readToken.Issuer);

            if (!redisValue.HasValue)
            {
                return false;
            }

            var installationData = JsonConvert.DeserializeObject<InstallationData>(redisValue);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = installationData.oauthId,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(installationData.oauthSecret)),
                ValidateAudience = false,
                ValidateLifetime = true
            };

            try
            {
                SecurityToken token;
                var validatedToken = jwtSecurityTokenHandler.ValidateToken(jwt, validationParameters, out token);
                return validatedToken != null;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static string BuildInitialGlance(string status = "GOOD")
        {
            var response = new
            {
                label = new
                {
                    type = "html",
                    value = "<b>4</b> Builds"
                },
                status = new
                {
                    type = "lozenge",
                    value = new
                    {
                        label = status,
                        type = "success"
                    }
                },
                metadata = new
                {
                    customData = new { customAttr = "customValue" }
                }
            };

            return JsonConvert.SerializeObject(response);
        }
    }
}