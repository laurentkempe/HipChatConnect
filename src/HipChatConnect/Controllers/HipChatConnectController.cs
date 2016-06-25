using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Nubot.Plugins.Samples.HipChatConnect.Models;
using System.Linq;

namespace HipChatConnect.Controllers
{
    [Route("/hipchat")]
    public class HipChatConnectController : Controller
    {
        private readonly IOptions<AppSettings> _settings;
        private static readonly ConcurrentDictionary<string, InstallationData> InstallationStore = new ConcurrentDictionary<string, InstallationData>();
        private static readonly ConcurrentDictionary<string, ExpiringAccessToken> AccessTokenStore = new ConcurrentDictionary<string, ExpiringAccessToken>();
        private readonly string _baseUri;

        public HipChatConnectController(IOptions<AppSettings> settings)
        {
            _settings = settings;

            _baseUri = "https://387c0fe3.ngrok.io";
            //_baseUri = _settings.Value?.BaseUrl ?? "http://localhost:52060/";
        }

        [HttpGet("atlassian-connect.json")]
        public async Task<string> GetCapabilityDescriptor()
        {
            return await Task.FromResult(GetCapabilitiesDescriptor(_baseUri));
        }

        [HttpPost("installed")]
        public async Task<HttpStatusCode> Installable([FromBody]InstallationData installation)
        {
            InstallationStore.TryAdd(installation.oauthId, installation);

            var capabilitiesRoot = await RetrieveCapabilitiesDocument(installation.capabilitiesUrl);

            installation.tokenUrl = capabilitiesRoot.capabilities.oauth2Provider.tokenUrl;
            installation.apiUrl = capabilitiesRoot.capabilities.hipchatApiProvider.url;

            return HttpStatusCode.OK;
        }

        [HttpGet("uninstalled")]
        public async Task<IActionResult> UnInstalled([FromQuery(Name = "redirect_url")]string redirectUrl,
                                                     [FromQuery(Name = "installable_url")]string installableUrl)
        {
            var client = new HttpClient();

            var httpResponse = await client.GetAsync(installableUrl);
            httpResponse.EnsureSuccessStatusCode();

            var installationData = await httpResponse.Content.ReadAsAsync<InstallationData>();

            InstallationStore.TryRemove(installationData.oauthId, out installationData);
            ExpiringAccessToken expiringAccessToken;
            AccessTokenStore.TryRemove(installationData.oauthId, out expiringAccessToken);

            return await Task.FromResult(Redirect(redirectUrl));
        }

        [HttpGet("glance")]
        public string GetGlance([FromQuery(Name = "signed_request")]string signedRequest)
        {
            if (ValidateJWT(signedRequest))
            {
                return BuildGlance();
            }

            return ""; //HttpStatusCode.Forbidden;
        }

        [HttpGet("updateGlance")]
        public async Task UpdateGlance([FromQuery(Name = "u")]string updateText)
        {
            foreach (var installationData in InstallationStore.Values)
            {
                var glanceData = new
                {
                    glance = new[]
                    {
                        new
                        {
                            key = "nubot.glance", // see installation descriptor
                            content = BuildGlanceString(updateText)
                        }
                    }
                };

                using (var client = new HttpClient())
                {
                    var accessToken = await GetAccessTokenAsync(installationData.oauthId);

                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.access_token);
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    var stringContent = new StringContent(JsonConvert.SerializeObject(glanceData));
                    stringContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                    var roomGlanceUpdateUri = new Uri($"{installationData.apiUrl}addon/ui/room/{installationData.roomId}");
                    var httpResponseMessage = await client.PostAsync(roomGlanceUpdateUri, stringContent);
                    httpResponseMessage.EnsureSuccessStatusCode();
                }
            }
        }

        [HttpGet("sendMessage")]
        public async Task SendMessage([FromQuery(Name = "m")] string msg)
        {
            var installationData = InstallationStore.Values.FirstOrDefault();
            if (installationData == null) return;

            using (var client = new HttpClient())
            {
                var accessToken = await GetAccessTokenAsync(installationData.oauthId);

                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.access_token);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var messageData = new
                {
                    color = "gray",
                    message = msg,
                    message_format = "html"
                };

                var stringContent = new StringContent(JsonConvert.SerializeObject(messageData));
                stringContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                var roomGlanceUpdateUri = new Uri($"{installationData.apiUrl}room/{installationData.roomId}/notification");
                var httpResponseMessage = await client.PostAsync(roomGlanceUpdateUri, stringContent);
                httpResponseMessage.EnsureSuccessStatusCode();
            }
        }

        [HttpGet("sendCardMessage")]
        public async Task SendCardMessage([FromQuery(Name = "m")]string msg, [FromQuery(Name = "d")] string description)
        {
            var installationData = InstallationStore.Values.FirstOrDefault();
            if (installationData == null) return;

            using (var client = new HttpClient())
            {
                var accessToken = await GetAccessTokenAsync(installationData.oauthId);

                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.access_token);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var messageData = new
                {
                    color = "gray",
                    message = msg,
                    message_format = "text",
                    card = new
                    {
                        style = "application",
                        id = "some_id",
                        url = "http://laurentkempe.com",
                        title = "Such awesome. Very API. Wow!",
                        description = description,
                        thumbnail = new
                        {
                            url = "https://pbs.twimg.com/profile_images/582836487776944129/cslDTKEq.jpg"
                        }
                    }
                };

                var stringContent = new StringContent(JsonConvert.SerializeObject(messageData));
                stringContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                var roomGlanceUpdateUri = new Uri($"{installationData.apiUrl}room/{installationData.roomId}/notification");
                var httpResponseMessage = await client.PostAsync(roomGlanceUpdateUri, stringContent);
                httpResponseMessage.EnsureSuccessStatusCode();
            }
        }

        [HttpGet("sidebar")]
        public IActionResult Sidebar([FromQuery(Name = "signed_request")]string signedRequest)
        {
            if (ValidateJWT(signedRequest))
            {
                return Redirect("/nubot/index.html");
            }

            return BadRequest();
        }

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
                        callbackUrl = $"{baseUri}/hipchat/installed",
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

        private async Task<CapabilitiesRoot> RetrieveCapabilitiesDocument(string capabilitiesUrl)
        {
            var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(new Uri(capabilitiesUrl));
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsAsync<CapabilitiesRoot>();
        }

        private async Task<AccessToken> GetAccessTokenAsync(string oauthId)
        {
            ExpiringAccessToken expiringAccessToken;
            if (!AccessTokenStore.TryGetValue(oauthId, out expiringAccessToken) || IsExpired(expiringAccessToken))
            {
                var accessToken = await RefreshAccessToken(oauthId);
                return accessToken.Token;
            }

            return await Task.FromResult(expiringAccessToken.Token);
        }

        private static async Task<ExpiringAccessToken> RefreshAccessToken(string oauthId)
        {
            InstallationData installation;
            if (!InstallationStore.TryGetValue(oauthId, out installation))
            {
                return null;
            }

            var client = new HttpClient();

            var dataContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("scope", "send_notification")
            });

            var credentials = Encoding.ASCII.GetBytes($"{installation.oauthId}:{installation.oauthSecret}");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(credentials));

            var tokenResponse = await client.PostAsync(new Uri(installation.tokenUrl), dataContent);
            var accessToken = await tokenResponse.Content.ReadAsAsync<AccessToken>();

            var expiringAccessToken = new ExpiringAccessToken
            {
                Token = accessToken,
                ExpirationTimeStamp = DateTime.Now + TimeSpan.FromTicks((accessToken.expires_in - 60)*1000)
            };

            AccessTokenStore.AddOrUpdate(oauthId, expiringAccessToken, (s, token) => token);

            return expiringAccessToken;
        }

        private bool IsExpired(ExpiringAccessToken accessToken)
        {
            return accessToken.ExpirationTimeStamp < DateTime.Now;
        }

        private bool ValidateJWT(string jwt)
        {
            var jwtSecurityTokenHandler = new JwtSecurityTokenHandler();
            var readToken = jwtSecurityTokenHandler.ReadToken(jwt);

            InstallationData installationData;
            if (!InstallationStore.TryGetValue(readToken.Issuer, out installationData))
            {
                return false;
            }

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

        private static string BuildGlance(string status = "GOOD")
        {
            var response = BuildGlanceString(status);

            return JsonConvert.SerializeObject(response);
        }

        private static object BuildGlanceString(string status = "GOOD")
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
                    customData = new {customAttr = "customValue"}
                }
            };

            return response;
        }
    }
}