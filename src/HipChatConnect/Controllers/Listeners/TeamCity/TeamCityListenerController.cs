using System.Net;
using System.Threading.Tasks;
using HipChatConnect.Controllers.Listeners.TeamCity.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace HipChatConnect.Controllers.Listeners.TeamCity
{
    [Route("/teamcity/listener")]
    public class TeamCityListenerController : Controller
    {
        private readonly IMediator _mediator;
        private readonly ILogger<TeamCityListenerController> _logger;

        public TeamCityListenerController(IMediator mediator, ILogger<TeamCityListenerController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        [HttpPost]
        public async Task<HttpStatusCode> Build([FromBody]TeamCityModel teamCityModel)
        {
            _logger.LogInformation($"Received status for {teamCityModel.build.buildName} from ${teamCityModel.build.rootUrl} with status ${teamCityModel.build.buildStatus}");

            await _mediator.Publish(new TeamcityBuildNotification(teamCityModel));

            return HttpStatusCode.OK;
        }
    }
}
