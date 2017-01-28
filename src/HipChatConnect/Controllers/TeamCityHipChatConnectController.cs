using System;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using HipChatConnect.Core.Models;
using HipChatConnect.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace HipChatConnect.Controllers
{
    [Route("/hipchat/teamcity")]
    public class TeamCityHipChatConnectController : Controller
    {
        private readonly ITenantService _tenantService;
        private readonly string _baseUri;

        public TeamCityHipChatConnectController(IOptions<AppSettings> settings, ITenantService tenantService)
        {
            _tenantService = tenantService;

            _baseUri = settings.Value?.BaseUrl ?? "http://localhost:52060/";
        }

        [HttpGet("atlassian-connect.json")]
        public async Task<string> GetCapabilityDescriptor()
        {
            return await Task.FromResult(GetCapabilitiesDescriptor(_baseUri));
        }

        [HttpPost("install")]
        public async Task<HttpStatusCode> Install([FromBody]InstallationData installation)
        {
            await _tenantService.CreateAsync(installation);

            return HttpStatusCode.OK;
        }

        [HttpGet("configure")]
        public async Task<IActionResult> Configure([FromQuery(Name = "signed_request")] string signedRequest)
        {
            if (await _tenantService.ValidateTokenAsync(signedRequest))
            {
                return Redirect($"/hipchat-configure?signed_request={signedRequest}");
            }

            return BadRequest();
        }

        [HttpGet("uninstall")]
        public async Task<IActionResult> UnInstall([FromQuery(Name = "redirect_url")]string redirectUrl,
                                                   [FromQuery(Name = "installable_url")]string installableUrl)
        {
            var client = new HttpClient();

            var httpResponse = await client.GetAsync(installableUrl);
            httpResponse.EnsureSuccessStatusCode();

            var installationData = JsonConvert.DeserializeObject<InstallationData>(await httpResponse.Content.ReadAsStringAsync());

            await _tenantService.RemoveAsync(installationData.oauthId);

            return await Task.FromResult(Redirect(redirectUrl));
        }

        [HttpGet("glance")]
        public async Task<string> GetGlance([FromQuery(Name = "signed_request")]string signedRequest)
        {
            if (await _tenantService.ValidateTokenAsync(signedRequest))
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
                var accessToken = await _tenantService.GetAccessTokenAsync(installationData.oauthId);

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
        //public async Task SendMessageAsync([FromQuery(Name = "m")] string msg)
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
            if (await _tenantService.ValidateTokenAsync(signedRequest))
            {
                var jwtSecurityTokenHandler = new JwtSecurityTokenHandler();
                var readToken = jwtSecurityTokenHandler.ReadToken(signedRequest);

                var installationData = await _tenantService.GetInstallationDataAsync(readToken.Issuer);

                await UpdateGlance(installationData);

                await SendMessage($"You opened it {openCounter} time(s)", installationData);

                await SendCardMessage($"You opened it {openCounter} time(s)", $"AWESOME {openCounter} time!", installationData);

                return Redirect("/nubot/index.html");
            }

            return BadRequest();
        }

        public async Task SendMessage(string msg, InstallationData installationData)
        {
            using (var client = new HttpClient())
            {
                var accessToken = await _tenantService.GetAccessTokenAsync(installationData.oauthId);

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
                var accessToken = await _tenantService.GetAccessTokenAsync(installationData.oauthId);

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
                name = "Nubot - TeamCity",
                description = "A nubot add-on for TeamCity.",
                key = "nubot-teamcity-2",
                links = new
                {
                    self = $"{baseUri}/hipchat/teamcity/atlassian-connect.json",
                    homepage = $"{baseUri}/hipchat/teamcity/atlassian-connect.json"
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
                        callbackUrl = $"{baseUri}/hipchat/teamcity/install",
                        uninstalledUrl = $"{baseUri}/hipchat/teamcity/uninstall"
                    },
                    configurable = new
                    {
                        url = $"{baseUri}/hipchat/teamcity/configure"
                    },
                    glance = new[]
                    {
                        new
                        {
                            name = new
                            {
                                value = "TeamCity"
                            },
                            queryUrl = $"{baseUri}/hipchat/teamcity/glance",
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
                            url = $"{baseUri}/hipchat/teamcity/sidebar"
                        }
                    }
                }
            };

            return JsonConvert.SerializeObject(capabilitiesDescriptor);
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