using System.Net;
using System.Threading.Tasks;
using HipChatConnect.Controllers.Listeners.TeamCity.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HipChatConnect.Controllers.Listeners.TeamCity
{
    [Route("/teamcity/listener")]
    public class TeamCityListenerController : Controller
    {
        private readonly IMediator _mediator;

        public TeamCityListenerController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<HttpStatusCode> Build([FromBody]TeamCityModel teamCityModel)
        {
            await _mediator.Publish(new TeamcityBuildNotification(teamCityModel));

            return HttpStatusCode.OK;
        }
    }
}
