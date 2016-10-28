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

        // GET: /<controller>/
        public IActionResult Index()
        {
            return View();
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
