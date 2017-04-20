using System.Net;
using System.Threading.Tasks;
using HipChatConnect.Controllers.Listeners.Github.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HipChatConnect.Controllers.Listeners.Github
{
    public class GithubListenerController : Controller
    {
        private readonly IMediator _mediator;

        public GithubListenerController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<HttpStatusCode> Build([FromBody] GithubModel githubModel)
        {
            await _mediator.Publish(new GithubPushNotification(githubModel));

            return HttpStatusCode.OK;
        }

    }
}
