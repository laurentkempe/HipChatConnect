using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using HipChatConnect.Controllers.Listeners.TeamCity.Models;
using HipChatConnect.Core.ReactiveExtensions;
using HipChatConnect.Services;
using Microsoft.Extensions.Options;

namespace HipChatConnect.Controllers.Listeners.TeamCity
{
    public class TeamCityAggregator
    {
        private readonly ITenantService _tenantService;
        private readonly IOptions<AppSettings> _settings;
        private Subject<TeamCityModel> _subject;

        public TeamCityAggregator(ITenantService tenantService, IHipChatRoom room, IOptions<AppSettings> settings)
        {
            _tenantService = tenantService;
            _settings = settings;
            Room = room;

            _subject = new Subject<TeamCityModel>();

            Initialization = InitializeFromConfigurationsAsync();
        }

        public Task Initialization { get; }

        private IHipChatRoom Room { get; }

        protected virtual IScheduler Scheduler => DefaultScheduler.Instance;

        public async Task Handle(TeamcityBuildNotification notification)
        {
            _subject.OnNext(notification.TeamCityModel);
        }

        public async Task ReInitializeFromConfigurationAsync()
        {
            _subject.Dispose();

            _subject = new Subject<TeamCityModel>();

            await InitializeFromConfigurationsAsync();
        }

        private async Task InitializeFromConfigurationsAsync()
        {
            var allConfigurations = await _tenantService.GetAllConfigurationAsync<ServerBuildConfiguration>();

            if (allConfigurations == null) return;

            foreach (var configuration in allConfigurations)
            {
                var buildConfiguration = configuration.Data.BuildConfiguration;

                var maxWaitDuration = TimeSpan.FromMinutes(buildConfiguration.MaxWaitDurationInMinutes);

                var buildExternalTypeIds = buildConfiguration.BuildConfigurationIds.Split(',');
                var expectedBuildCount = buildExternalTypeIds.Length;

                var buildsPerBuildNumber = _subject.GroupBy(model => model.build.buildNumber);

                buildsPerBuildNumber.Subscribe(
                    grp => grp.BufferUntilInactive(maxWaitDuration, Scheduler, expectedBuildCount).Take(1).Subscribe(
                        async builds => await SendNotificationAsync(builds, expectedBuildCount, configuration.OAuthId)));
            }
        }

        private async Task SendNotificationAsync(IList<TeamCityModel> buildStatuses, int expectedBuildCount, string oauthId)
        {
            var activityCardData = new TeamCityMessageBuilder(expectedBuildCount, _settings).BuildActivityCard(buildStatuses);

            await Room.SendActivityCardAsync(activityCardData, oauthId);
        }
    }
}