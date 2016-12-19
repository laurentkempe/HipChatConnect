using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using HipChatConnect.Controllers.Listeners.TeamCity.Models;
using HipChatConnect.Core.ReactiveExtensions;
using HipChatConnect.Models;
using HipChatConnect.Services;

namespace HipChatConnect.Controllers.Listeners.TeamCity
{
    public class TeamCityAggregator
    {
        private string _oauthId;

        public TeamCityAggregator(ITenantService tenantService, IHipChatRoom room)
        {
            TenantService = tenantService;
            Room = room;
        }

        private Subject<TeamCityModel> Subject { get; set; }

        private ITenantService TenantService { get; }

        private IHipChatRoom Room { get; }

        protected virtual IScheduler Scheduler => DefaultScheduler.Instance;


        private async Task SendNotificationAsync(IList<TeamCityModel> buildStatuses)
        {
            bool notify;
            var message = new TeamCityMessageBuilder(ExpectedBuildCount).BuildMessage(buildStatuses, out notify);

            await Room.SendMessageAsync(message, _oauthId);
        }

        public int ExpectedBuildCount { get; set; }

        public void Handle(TeamcityBuildNotification notification)
        {
            Subject.OnNext(notification.TeamCityModel);
        }

        public void Configure(TeamCityConfigurationViewModel teamCityConfigurationViewModel)
        {
            //todo we should not read the token from here but we should search in the Tenant Store!
            var jwtSecurityTokenHandler = new JwtSecurityTokenHandler();
            var readToken = jwtSecurityTokenHandler.ReadToken(teamCityConfigurationViewModel.JwtToken);

            _oauthId = readToken.Issuer;

            Subject = new Subject<TeamCityModel>();

            var maxWaitDuration = TimeSpan.FromMinutes(10.0); //todo add this on the configuration line

            var buildExternalTypeIds = teamCityConfigurationViewModel.BuildConfiguration.Split(',');
            ExpectedBuildCount = buildExternalTypeIds.Length;

            var buildsPerBuildNumber = Subject.GroupBy(model => model.build.buildNumber);

            buildsPerBuildNumber.Subscribe(
                grp => grp.BufferUntilInactive(maxWaitDuration, Scheduler, ExpectedBuildCount).Take(1).Subscribe(
                    async list => await SendNotificationAsync(list)));
        }
    }
}