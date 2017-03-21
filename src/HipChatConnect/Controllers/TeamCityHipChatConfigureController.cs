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
        private readonly TeamCityAggregator _teamCityAggregator;
        private readonly ITenantService _tenantService;

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

                var serverBuildConfiguration =
                    await _tenantService.GetConfigurationAsync<ServerBuildConfiguration>(readToken.Issuer);

                teamCityConfigurationViewModel.ServerUrl = serverBuildConfiguration.ServerRootUrl;

                foreach (var buildConfiguration in serverBuildConfiguration.BuildConfigurations)
                    teamCityConfigurationViewModel.BuildConfigurations.Add(new BuildConfigurationViewModel
                    {
                        BuildConfigurationIds = buildConfiguration.BuildConfigurationIds,
                        MaxWaitDurationInMinutes = buildConfiguration.MaxWaitDurationInMinutes
                    });

                return View(teamCityConfigurationViewModel);
            }

            return BadRequest();
        }

        [HttpPost("save")]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(TeamCityConfigurationViewModel teamCityConfigurationViewModel)
        {
            if (!ModelState.IsValid)
                return RedirectToAction("Index");

            if (await _tenantService.ValidateTokenAsync(teamCityConfigurationViewModel.JwtToken))
            {
                var serverBuildConfiguration = new ServerBuildConfiguration
                {
                    ServerRootUrl = teamCityConfigurationViewModel.ServerUrl
                };

                foreach (var buildConfigurationViewModel in teamCityConfigurationViewModel.BuildConfigurations)
                    serverBuildConfiguration.BuildConfigurations.Add(new BuildConfiguration
                    {
                        BuildConfigurationIds = buildConfigurationViewModel.BuildConfigurationIds,
                        MaxWaitDurationInMinutes = buildConfigurationViewModel.MaxWaitDurationInMinutes
                    });

                await _tenantService.SetConfigurationAsync(teamCityConfigurationViewModel.JwtToken,
                    serverBuildConfiguration);

                await _teamCityAggregator.ReInitializeFromConfigurationAsync();

                return View("Index", teamCityConfigurationViewModel);
            }

            return RedirectToAction("Index");
        }
    }
}