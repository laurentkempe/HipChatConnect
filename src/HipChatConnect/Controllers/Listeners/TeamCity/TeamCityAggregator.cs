using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using HipChatConnect.Controllers.Listeners.TeamCity.Models;
using HipChatConnect.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HipChatConnect.Controllers.Listeners.TeamCity
{
    public class TeamCityAggregator
    {
        private readonly IOptions<AppSettings> _settings;
        private readonly ILogger<TeamCityAggregator> _logger;
        private readonly SerialDisposable _subscription = new SerialDisposable();
        private readonly ITenantService _tenantService;

        public TeamCityAggregator(ITenantService tenantService,
            IHipChatRoom room,
            IOptions<AppSettings> settings, ILogger<TeamCityAggregator> logger)
        {
            _tenantService = tenantService;
            _settings = settings;
            _logger = logger;
            Room = room;

            Initialization = InitializeFromConfigurationsAsync();
        }

        public Task Initialization { get; }

        private IHipChatRoom Room { get; }

        protected virtual IScheduler Scheduler => DefaultScheduler.Instance;

        public async Task ReInitializeFromConfigurationAsync() => await InitializeFromConfigurationsAsync();

        public async Task Handle(TeamcityBuildNotification notification)
        {
            await Task.Run(() => NotificationReceived?.Invoke(this, notification));
        }

        private event EventHandler<TeamcityBuildNotification> NotificationReceived;

        private async Task InitializeFromConfigurationsAsync()
        {
            var rawConfigurations = await _tenantService.GetAllConfigurationAsync<ServerBuildConfiguration>();
            if (rawConfigurations == null) return;

            var buildConfigurations = rawConfigurations.Select(c => new BuildConfiguration(
                    c.Data.ServerRootUrl,
                    c.OAuthId,
                    c.Data.BuildConfiguration.BuildConfigurationIds.Split(',').ToList(),
                    (int) c.Data.BuildConfiguration.MaxWaitDurationInMinutes))
                .ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);

            foreach (var buildConfiguration in buildConfigurations)
            {
                _logger.LogInformation($"Configuration initialized for {buildConfiguration.Key} with buildSteps [{string.Join(",", buildConfiguration.Value.BuildSteps) }] [{buildConfiguration.Value.TimeoutMinutes}], [{buildConfiguration.Value.Name}]");
            }

            _subscription.Disposable =
                Observable.FromEventPattern<EventHandler<TeamcityBuildNotification>, TeamcityBuildNotification>(
                        x => NotificationReceived += x,
                        x => NotificationReceived -= x)
                    .Where(@event => buildConfigurations.ContainsKey(@event.EventArgs.TeamCityModel.build.rootUrl))
                    .Select(@event => new
                    {
                        @event.EventArgs.TeamCityModel,
                        Configuration = buildConfigurations[@event.EventArgs.TeamCityModel.build.rootUrl]
                    })
                    .Where(x => x.Configuration.BuildSteps.Contains(x.TeamCityModel.build.buildName))
                    .Synchronize()
                    .GroupByUntil(
                        x =>
                            new
                            {
                                RootUrl = x.TeamCityModel.build.rootUrl,
                                BuildNumber = x.TeamCityModel.build.buildNumber,
                                x.Configuration
                            },
                        group =>
                        {
                            _logger.LogInformation($"Group created for buildNumber [{group.Key.BuildNumber}], [{group.Key.RootUrl}]");

                            // this method is called just the first time a new group is created and returns an observable,
                            // a group is closed when that observable emits a value

                            // this buffer will emit a value (a list) when either there's one element, or a timeout (sliding window).
                            // When an element arrives before the timeout,  a buffer is emitted and the timeout timer restarted
                            var timeoutBuffer = group.Buffer(
                                    TimeSpan.FromMinutes(group.Key.Configuration.TimeoutMinutes), 1,
                                    Scheduler)
                                // but then we discard the lists with one element as we use them just to restart the timeout timer
                                .Where(buffer => buffer.Count < 1);

                            var buildSteps = group.Key.Configuration.BuildSteps.Select(
                                buildStep =>
                                    group.Where(
                                        element =>
                                            element.TeamCityModel.build.buildName.Equals(buildStep,
                                                StringComparison.OrdinalIgnoreCase)).Take(1)).Merge();

                            // this is observable will emit a value when the last build notification arrives for a group
                            var allStepsCompleted =
                                buildSteps.Skip(group.Key.Configuration.BuildSteps.Count - 1);

                            // then we close the group when either there's a timeout (we will have less builds than the total)
                            // or when the group is full
                            return timeoutBuffer.Amb<object>(allStepsCompleted);
                        })
                    .Subscribe(async x =>
                    {
                        // the group is emitted with the first element, we then have to wait for it to complete
                        var group = await x.ToList().SingleAsync();
                        var teamCityModels = group.Select(g => g.TeamCityModel).ToList();
                        var conf = x.Key.Configuration;

                        _logger.LogInformation(
                            $"Received all build status for ${x.Key.BuildNumber}, sending message");


                        await SendHipChatInformationAsync(conf, teamCityModels);
                        await SendTeamsInformationAsync(conf, teamCityModels);
                    });
        }

        private async Task SendTeamsInformationAsync(BuildConfiguration conf, List<TeamCityModel> teamCityModels)
        {
            var activityCardData =
                new TeamCityMessageBuilder(conf.BuildSteps.Count, _settings).BuildTeamsActivityCard(
                    teamCityModels);

            await Room.SendTeamsActivityCardAsync(activityCardData);
        }

        private async Task SendHipChatInformationAsync(BuildConfiguration conf, List<TeamCityModel> teamCityModels)
        {
            var activityCardData =
                new TeamCityMessageBuilder(conf.BuildSteps.Count, _settings).BuildActivityCard(
                    teamCityModels);

            await Room.SendActivityCardAsync(activityCardData, conf.OAuthId);
        }

        private class BuildConfiguration
        {
            public BuildConfiguration(string name, string oAuthId, IEnumerable<string> buildSteps, int timeoutMinutes)
            {
                Name = name;
                OAuthId = oAuthId;
                BuildSteps = buildSteps.ToList();
                TimeoutMinutes = timeoutMinutes;
            }

            public string Name { get; }

            public string OAuthId { get; }

            public IReadOnlyList<string> BuildSteps { get; }

            public int TimeoutMinutes { get; }
        }
    }
}