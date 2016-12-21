using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;
using HipChatConnect.Core.Models;
using HipChatConnect.Services;
using Microsoft.AspNetCore.Mvc;

namespace HipChatConnect.Controllers
{
    [Route("/hipchat-configure", Name = "Configure")]
    public class TeamCityHipChatConfigureController : Controller
    {
        private readonly ITenantService _tenantService;

        public TeamCityHipChatConfigureController(ITenantService tenantService)
        {
            _tenantService = tenantService;
        }

        // GET: 
        public async Task<IActionResult> Index([FromQuery(Name = "signed_request")] string signedRequest)
        {
            if (await _tenantService.ValidateTokenAsync(signedRequest))
            {
                var jwtSecurityTokenHandler = new JwtSecurityTokenHandler();
                var readToken = jwtSecurityTokenHandler.ReadToken(signedRequest);

                var teamCityConfigurationViewModel = new TeamCityConfigurationViewModel();

                var dictionary = await _tenantService.GetConfigurationAsync(readToken.Issuer);

                teamCityConfigurationViewModel.ServerUrl = dictionary.ContainsKey("ServerUrl") ? dictionary["ServerUrl"] : string.Empty;
                teamCityConfigurationViewModel.BuildConfiguration = dictionary.ContainsKey("BuildConfiguration") ? dictionary["BuildConfiguration"] : string.Empty;

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
                await _tenantService.SetConfigurationAsync(teamCityConfigurationViewModel.JwtToken, "ServerUrl", teamCityConfigurationViewModel.ServerUrl);
                await _tenantService.SetConfigurationAsync(teamCityConfigurationViewModel.JwtToken, "BuildConfiguration", teamCityConfigurationViewModel.BuildConfiguration);

                return View("Index", teamCityConfigurationViewModel);
            }

            return RedirectToAction("Index");
        }
    }
}
