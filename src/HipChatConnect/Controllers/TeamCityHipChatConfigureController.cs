using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;
using HipChatConnect.Controllers.Listeners.TeamCity;
using HipChatConnect.Models;
using HipChatConnect.Services;
using Microsoft.AspNetCore.Mvc;

namespace HipChatConnect.Controllers
{
    [Route("/hipchat-configure", Name = "Configure")]
    public class TeamCityHipChatConfigureController : Controller
    {
        private readonly ITenantService _tenantService;
        private readonly TeamCityAggregator _aggregator;

        public TeamCityHipChatConfigureController(ITenantService tenantService, TeamCityAggregator aggregator)
        {
            _tenantService = tenantService;
            _aggregator = aggregator;
        }

        // GET: 
        public async Task<IActionResult> Index([FromQuery(Name = "signed_request")] string signedRequest)
        {
            if (await _tenantService.ValidateTokenAsync(signedRequest))
            {
                var jwtSecurityTokenHandler = new JwtSecurityTokenHandler();
                var readToken = jwtSecurityTokenHandler.ReadToken(signedRequest);

                var authenticationData = await _tenantService.GetTenantDataAsync(readToken.Issuer);

                var teamCityConfigurationViewModel = new TeamCityConfigurationViewModel();

                var tenantData = await _tenantService.GetTenantDataAsync(authenticationData.InstallationData.oauthId);

                teamCityConfigurationViewModel.ServerUrl = tenantData.Store.ContainsKey("ServerUrl") ? tenantData.Store["ServerUrl"] : string.Empty;
                teamCityConfigurationViewModel.BuildConfiguration = tenantData.Store.ContainsKey("BuildConfiguration") ? tenantData.Store["BuildConfiguration"] : string.Empty;

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
                await _tenantService.SetTenantDataAsync(teamCityConfigurationViewModel.JwtToken, "ServerUrl", teamCityConfigurationViewModel.ServerUrl);
                await _tenantService.SetTenantDataAsync(teamCityConfigurationViewModel.JwtToken, "BuildConfiguration", teamCityConfigurationViewModel.BuildConfiguration);

                _aggregator.Configure(teamCityConfigurationViewModel);

                return View("Index", teamCityConfigurationViewModel);
            }

            return RedirectToAction("Index");
        }
    }
}
