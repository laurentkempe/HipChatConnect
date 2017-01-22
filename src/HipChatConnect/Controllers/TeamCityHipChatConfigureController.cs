using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;
using HipChatConnect.Controllers.Listeners.TeamCity;
using HipChatConnect.Core.Models;
using HipChatConnect.Services;
using Microsoft.AspNetCore.Mvc;

namespace HipChatConnect.Controllers
{
    [Route("/hipchat-configure", Name = "Configure")]
    public class TeamCityHipChatConfigureController : Controller
    {
        private readonly ITenantService _tenantService;
        private readonly TeamCityAggregator _teamCityAggregator;

        public TeamCityHipChatConfigureController(ITenantService tenantService, TeamCityAggregator teamCityAggregator)
        {
            _tenantService = tenantService;
            _teamCityAggregator = teamCityAggregator;
        }

        // GET: 
        public async Task<IActionResult> Index([FromQuery(Name = "signed_request")] string signedRequest)
        {
            if (await _tenantService.ValidateTokenAsync(signedRequest))
            {
                var jwtSecurityTokenHandler = new JwtSecurityTokenHandler();
                var readToken = jwtSecurityTokenHandler.ReadToken(signedRequest);

                var teamCityConfigurationViewModel = new TeamCityConfigurationViewModel();

                var serverBuildConfiguration = await _tenantService.GetConfigurationAsync<ServerBuildConfiguration>(readToken.Issuer);

                teamCityConfigurationViewModel.ServerUrl = serverBuildConfiguration.ServerRootUrl;
                teamCityConfigurationViewModel.BuildConfigurationIds = serverBuildConfiguration.BuildConfiguration.BuildConfigurationIds;
                teamCityConfigurationViewModel.MaxWaitDurationInMinutes = serverBuildConfiguration.BuildConfiguration.MaxWaitDurationInMinutes;

                return View(teamCityConfigurationViewModel);
            }

            return BadRequest();
        }

        [HttpPost("save")]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(TeamCityConfigurationViewModel teamCityConfigurationViewModel)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction("Index");
            }

            if (await _tenantService.ValidateTokenAsync(teamCityConfigurationViewModel.JwtToken))
            {
                var serverBuildConfiguration = new ServerBuildConfiguration
                {
                    ServerRootUrl = teamCityConfigurationViewModel.ServerUrl,
                };

                serverBuildConfiguration.BuildConfiguration.MaxWaitDurationInMinutes = teamCityConfigurationViewModel.MaxWaitDurationInMinutes;
                serverBuildConfiguration.BuildConfiguration.BuildConfigurationIds = teamCityConfigurationViewModel.BuildConfigurationIds;

                await _tenantService.SetConfigurationAsync(teamCityConfigurationViewModel.JwtToken, serverBuildConfiguration);

                return View("Index", teamCityConfigurationViewModel);
            }

            return RedirectToAction("Index");
        }
    }
}
