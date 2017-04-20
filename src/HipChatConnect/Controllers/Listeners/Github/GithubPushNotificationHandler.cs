using System.Threading.Tasks;
using MediatR;

namespace HipChatConnect.Controllers.Listeners.Github
{
    public class GithubPushNotificationHandler : IAsyncNotificationHandler<GithubPushNotification>
    {
        private GithubAggregator Aggregator { get; }

        public GithubPushNotificationHandler(GithubAggregator  githubAggregator)
        {
            Aggregator = githubAggregator;
        }

        public async Task Handle(GithubPushNotification notification)
        {
            await Aggregator.Handle(notification);
        }
    }
}