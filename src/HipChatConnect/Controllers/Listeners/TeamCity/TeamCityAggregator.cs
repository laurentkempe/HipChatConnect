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

        public TeamCityAggregator(ITenantService tenantService, IHipChatRoom room, IOptions<AppSettings> settings)
        {
            _tenantService = tenantService;
            _settings = settings;
            Room = room;

            Subject = new Subject<TeamCityModel>();
        }

        private Subject<TeamCityModel> Subject { get; set; }

        private IHipChatRoom Room { get; }

        protected virtual IScheduler Scheduler => DefaultScheduler.Instance;

        public int ExpectedBuildCount { get; set; }

        public async Task Handle(TeamcityBuildNotification notification)
        {
            var buildBuildTypeId = notification.TeamCityModel.build.buildTypeId; //e.g. SkyeEditor_Features_Publish
            var rootUrl = notification.TeamCityModel.build.rootUrl;

            var matchingConfiguration = await SearchConfigurationsFor(rootUrl, buildBuildTypeId);

            if (matchingConfiguration != null)
            {
                var buildConfiguration = matchingConfiguration.Data.BuildConfiguration;
                var maxWaitDuration = TimeSpan.FromMinutes(buildConfiguration.MaxWaitDurationInMinutes);

                var buildExternalTypeIds = buildConfiguration.BuildConfigurationIds.Split(',');
                ExpectedBuildCount = buildExternalTypeIds.Length;

                var buildsPerBuildNumber = Subject.GroupBy(model => model.build.buildNumber);

                buildsPerBuildNumber.Subscribe(
                    grp => grp.BufferUntilInactive(maxWaitDuration, Scheduler, ExpectedBuildCount).Take(1).Subscribe(
                        async list => await SendNotificationAsync(list, matchingConfiguration.OAuthId)));
            }

            Subject.OnNext(notification.TeamCityModel);
        }

        private async Task SendNotificationAsync(IList<TeamCityModel> buildStatuses, string oauthId)
        {
            var activityCardData = new TeamCityMessageBuilder(ExpectedBuildCount, _settings).BuildActivityCard(buildStatuses);

            await Room.SendActivityCardAsync(activityCardData, oauthId);
        }

        private async Task<IConfiguration<ServerBuildConfiguration>> SearchConfigurationsFor(string rootUrl, string buildBuildTypeId)
        {
            var allConfigurations = await _tenantService.GetAllConfigurationAsync<ServerBuildConfiguration>();

            foreach (var configuration in allConfigurations)
            {
                if (configuration.Data.ServerRootUrl == rootUrl)
                {
                    if (configuration.Data.BuildConfiguration.BuildConfigurationIds.Contains(buildBuildTypeId))
                    {
                        return configuration;
                    }
                }
            }

            return null;
        }
    }

    public class BuildConfiguration
    {
        public BuildConfiguration()
        {
            MaxWaitDurationInMinutes = 0.0;
            BuildConfigurationIds = string.Empty;
        }

        public double MaxWaitDurationInMinutes { get; set; }

        public string BuildConfigurationIds { get; set; }
    }

    public class ServerBuildConfiguration
    {
        public ServerBuildConfiguration()
        {
            BuildConfiguration = new BuildConfiguration();
            ServerRootUrl = string.Empty;
        }

        public string ServerRootUrl { get; set; }

        public BuildConfiguration BuildConfiguration { get; set; }
    }
}