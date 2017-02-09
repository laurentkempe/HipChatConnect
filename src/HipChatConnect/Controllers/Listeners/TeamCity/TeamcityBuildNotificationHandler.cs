using System.Threading.Tasks;
using MediatR;

namespace HipChatConnect.Controllers.Listeners.TeamCity
{
    public class TeamcityBuildNotificationHandler : IAsyncNotificationHandler<TeamcityBuildNotification>
    {
        public TeamcityBuildNotificationHandler(TeamCityAggregator aggregator)
        {
            Aggregator = aggregator;
        }

        private TeamCityAggregator Aggregator { get; }

        public async Task Handle(TeamcityBuildNotification notification)
        {
            await Aggregator.Handle(notification);
        }
    }
}