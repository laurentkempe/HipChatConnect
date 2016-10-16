using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using HipChatConnect.Core.Cache;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Nubot.Plugins.Samples.HipChatConnect.Models;
using HipChatConnect.Models;

namespace HipChatConnect.Controllers
{
    [Route("/hipchat")]
    public class HipChatConnectController : Controller
    {
        private readonly ICache _cache;
        private readonly string _baseUri;

        public HipChatConnectController(IOptions<AppSettings> settings, ICache cache)
        {
            _cache = cache;

            _baseUri = settings.Value?.BaseUrl ?? "http://localhost:52060/";
        }

        [HttpGet("atlassian-connect.json")]
        public async Task<string> GetCapabilityDescriptor()
        {
            return await Task.FromResult(GetCapabilitiesDescriptor(_baseUri));
        }

        [HttpPost("installed")]
        public async Task<HttpStatusCode> Installable([FromBody]InstallationData installation)
        {
            var capabilitiesRoot = await RetrieveCapabilitiesDocumentAsync(installation.capabilitiesUrl);

            installation.tokenUrl = capabilitiesRoot.capabilities.oauth2Provider.tokenUrl;
            installation.apiUrl = capabilitiesRoot.capabilities.hipchatApiProvider.url;

            await _cache.SetAsync(installation.oauthId, new AuthenticationData { InstallationData = installation });

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

            await _cache.RemoveAsync(installationData.oauthId);

            return await Task.FromResult(Redirect(redirectUrl));
        }

        [HttpGet("glance")]
        public async Task<string> GetGlance([FromQuery(Name = "signed_request")]string signedRequest)
        {
            if (await ValidateJWT(signedRequest))
            {
                return BuildGlance();
            }

            return ""; //HttpStatusCode.Forbidden;
        }

        //[HttpGet("updateGlance")]
        //public async Task UpdateGlance([FromQuery(Name = "u")]string updateText)
        //{
        //    foreach (var installationData in InstallationStore.Values)
        //    {
        //        var glanceData = new
        //        {
        //            glance = new[]
        //            {
        //                new
        //                {
        //                    key = "nubot.glance", // see installation descriptor
        //                    content = BuildGlanceString(updateText)
        //                }
        //            }
        //        };

        //        using (var client = new HttpClient())
        //        {
        //            var accessToken = await GetAccessTokenAsync(installationData.oauthId);

        //            client.DefaultRequestHeaders.Accept.Clear();
        //            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.access_token);
        //            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        //            var stringContent = new StringContent(JsonConvert.SerializeObject(glanceData));
        //            stringContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        //            var roomGlanceUpdateUri = new Uri($"{installationData.apiUrl}addon/ui/room/{installationData.roomId}");
        //            var httpResponseMessage = await client.PostAsync(roomGlanceUpdateUri, stringContent);
        //            httpResponseMessage.EnsureSuccessStatusCode();
        //        }
        //    }
        //}

        static int openCounter = 0;

        public async Task UpdateGlance(InstallationData installationData)
        {
            openCounter++;

            var glanceData = new
            {
                glance = new[]
                {
                        new
                        {
                            key = "nubot.glance", // see installation descriptor
                            content = BuildGlanceString(openCounter.ToString())
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

        //[HttpGet("sendMessage")]
        //public async Task SendMessage([FromQuery(Name = "m")] string msg)
        //{
        //    var installationData = InstallationStore.Values.FirstOrDefault();
        //    if (installationData == null) return;

        //    using (var client = new HttpClient())
        //    {
        //        var accessToken = await GetAccessTokenAsync(installationData.oauthId);

        //        client.DefaultRequestHeaders.Accept.Clear();
        //        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.access_token);
        //        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        //        var messageData = new
        //        {
        //            color = "gray",
        //            message = msg,
        //            message_format = "html"
        //        };

        //        var stringContent = new StringContent(JsonConvert.SerializeObject(messageData));
        //        stringContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        //        var roomGlanceUpdateUri = new Uri($"{installationData.apiUrl}room/{installationData.roomId}/notification");
        //        var httpResponseMessage = await client.PostAsync(roomGlanceUpdateUri, stringContent);
        //        httpResponseMessage.EnsureSuccessStatusCode();
        //    }
        //}

        //[HttpGet("sendCardMessage")]
        //public async Task SendCardMessage([FromQuery(Name = "m")]string msg, [FromQuery(Name = "d")] string description)
        //{
        //    var installationData = InstallationStore.Values.FirstOrDefault();
        //    if (installationData == null) return;

        //    using (var client = new HttpClient())
        //    {
        //        var accessToken = await GetAccessTokenAsync(installationData.oauthId);

        //        client.DefaultRequestHeaders.Accept.Clear();
        //        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.access_token);
        //        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        //        var messageData = new
        //        {
        //            color = "gray",
        //            message = msg,
        //            message_format = "text",
        //            card = new
        //            {
        //                style = "application",
        //                id = "some_id",
        //                url = "http://laurentkempe.com",
        //                title = "Such awesome. Very API. Wow!",
        //                description = description,
        //                thumbnail = new
        //                {
        //                    url = "https://pbs.twimg.com/profile_images/582836487776944129/cslDTKEq.jpg"
        //                }
        //            }
        //        };

        //        var stringContent = new StringContent(JsonConvert.SerializeObject(messageData));
        //        stringContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        //        var roomGlanceUpdateUri = new Uri($"{installationData.apiUrl}room/{installationData.roomId}/notification");
        //        var httpResponseMessage = await client.PostAsync(roomGlanceUpdateUri, stringContent);
        //        httpResponseMessage.EnsureSuccessStatusCode();
        //    }
        //}

        [HttpGet("sidebar")]
        public async Task<IActionResult> Sidebar([FromQuery(Name = "signed_request")]string signedRequest)
        {
            if (await ValidateJWT(signedRequest))
            {
                var jwtSecurityTokenHandler = new JwtSecurityTokenHandler();
                var readToken = jwtSecurityTokenHandler.ReadToken(signedRequest);

                var authenticationData = await GetAuthenticationDataFromCacheAsync(readToken.Issuer);

                await UpdateGlance(authenticationData.InstallationData);

                await SendMessage($"You opened it {openCounter} time(s)", authenticationData.InstallationData);

                await SendCardMessage($"You opened it {openCounter} time(s)", $"AWESOME {openCounter} time!",
                        authenticationData.InstallationData);

                return Redirect("/nubot/index.html");
            }

            return BadRequest();
        }

        public async Task SendMessage(string msg, InstallationData installationData)
        {
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

        public async Task SendCardMessage(string msg, string description, InstallationData installationData)
        {
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

        private static string GetCapabilitiesDescriptor(string baseUri)
        {
            var capabilitiesDescriptor = new
            {
                name = "Nubot",
                description = "An add-on to talk to Nubot.",
                key = "nubot",
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

        private async Task<CapabilitiesRoot> RetrieveCapabilitiesDocumentAsync(string capabilitiesUrl)
        {
            var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(new Uri(capabilitiesUrl));
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsAsync<CapabilitiesRoot>();
        }

        private async Task<AccessToken> GetAccessTokenAsync(string oauthId)
        {
            var authenticationData = await _cache.GetAsync<AuthenticationData>(oauthId);

            if (IsExpired(authenticationData.Token))
            {
                var accessToken = await RefreshAccessToken(oauthId);
                return accessToken.Token;
            }

            return await Task.FromResult(authenticationData.Token.Token);
        }

        private async Task<ExpiringAccessToken> RefreshAccessToken(string oauthId)
        {
            var authenticationData = await GetAuthenticationDataFromCacheAsync(oauthId);

            var client = new HttpClient();

            var dataContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("scope", "send_notification")
            });

            var credentials = Encoding.ASCII.GetBytes($"{authenticationData.InstallationData.oauthId}:{authenticationData.InstallationData.oauthSecret}");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(credentials));

            var tokenResponse = await client.PostAsync(new Uri(authenticationData.InstallationData.tokenUrl), dataContent);
            var accessToken = await tokenResponse.Content.ReadAsAsync<AccessToken>();

            var expiringAccessToken = new ExpiringAccessToken
            {
                Token = accessToken,
                ExpirationTimeStamp = DateTime.Now + TimeSpan.FromTicks((accessToken.expires_in - 60) * 1000)
            };

            authenticationData.Token = expiringAccessToken;
            await _cache.SetAsync(oauthId, authenticationData);

            return expiringAccessToken;
        }

        private async Task<AuthenticationData> GetAuthenticationDataFromCacheAsync(string oauthId)
        {
            return await _cache.GetAsync<AuthenticationData>(oauthId);
        }

        private bool IsExpired(ExpiringAccessToken accessToken)
        {
            return accessToken == null || accessToken.ExpirationTimeStamp < DateTime.Now;
        }

        private async Task<bool> ValidateJWT(string jwt)
        {
            var jwtSecurityTokenHandler = new JwtSecurityTokenHandler();
            var readToken = jwtSecurityTokenHandler.ReadToken(jwt);

            var authenticationData = await GetAuthenticationDataFromCacheAsync(readToken.Issuer);
            var installationData = authenticationData.InstallationData;

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