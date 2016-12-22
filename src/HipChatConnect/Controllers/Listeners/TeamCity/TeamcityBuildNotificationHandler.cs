using System.Threading.Tasks;
using MediatR;

namespace HipChatConnect.Controllers.Listeners.TeamCity
{
    public class TeamcityBuildNotificationHandler : IAsyncNotificationHandler<TeamcityBuildNotification>
    {
        private TeamCityAggregator Aggregator { get; }

        public TeamcityBuildNotificationHandler(TeamCityAggregator aggregator)
        {
            Aggregator = aggregator;
        }

        public async Task Handle(TeamcityBuildNotification notification)
        {
            await Aggregator.Handle(notification);
        }
    }
}