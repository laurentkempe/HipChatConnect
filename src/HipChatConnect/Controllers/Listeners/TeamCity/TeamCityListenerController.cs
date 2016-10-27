using System.Net;
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

        [HttpPost("/")]
        public HttpStatusCode Build([FromBody]TeamCityModel teamCityModel)
        {
            _mediator.Publish(new TeamcityBuildNotification(teamCityModel));

            return HttpStatusCode.OK;
        }
    }
}
