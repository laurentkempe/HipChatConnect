using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using HipChatConnect.Controllers.Listeners.TeamCity.Models;
using HipChatConnect.Core.Models;
using HipChatConnect.Core.ReactiveExtensions;
using HipChatConnect.Services;

namespace HipChatConnect.Controllers.Listeners.TeamCity
{
    public class TeamCityAggregator
    {
        private readonly ITenantService _tenantService;

        public TeamCityAggregator(ITenantService tenantService, IHipChatRoom room)
        {
            _tenantService = tenantService;
            Room = room;
        }

        private Subject<TeamCityModel> Subject { get; set; }

        private IHipChatRoom Room { get; }

        protected virtual IScheduler Scheduler => DefaultScheduler.Instance;

        public int ExpectedBuildCount { get; set; }


        public void Handle(TeamcityBuildNotification notification)
        {
            if (Subject == null)
            {
                Subject = new Subject<TeamCityModel>();

                var buildBuildTypeId = notification.TeamCityModel.build.buildTypeId; //e.g. SkyeEditor_Features_Publish
                var rootUrl = notification.TeamCityModel.build.rootUrl;

                //var matchingConfigurations = SearchConfigurationsFor(rootUrl, buildBuildTypeId);

                //var maxWaitDuration = TimeSpan.FromMinutes(10.0); //todo add this on the configuration line


                //var buildExternalTypeIds = teamCityConfigurationViewModel.BuildConfigurationIds.Split(',');
                //ExpectedBuildCount = buildExternalTypeIds.Length;

                //var buildsPerBuildNumber = Subject.GroupBy(model => model.build.buildNumber);

                //buildsPerBuildNumber.Subscribe(
                //    grp => grp.BufferUntilInactive(maxWaitDuration, Scheduler, ExpectedBuildCount).Take(1).Subscribe(
                //        async list => await SendNotificationAsync(list)));
            }

            Subject.OnNext(notification.TeamCityModel);
        }

        //public void Configure(TeamCityConfigurationViewModel teamCityConfigurationViewModel)
        //{
        //    //todo we should not read the token from here but we should search in the Tenant Store!
        //    var jwtSecurityTokenHandler = new JwtSecurityTokenHandler();
        //    var readToken = jwtSecurityTokenHandler.ReadToken(teamCityConfigurationViewModel.JwtToken);

        //    Subject = new Subject<TeamCityModel>();

        //    var maxWaitDuration = TimeSpan.FromMinutes(10.0); //todo add this on the configuration line

        //    var buildExternalTypeIds = teamCityConfigurationViewModel.BuildConfigurationIds.Split(',');
        //    ExpectedBuildCount = buildExternalTypeIds.Length;

        //    var buildsPerBuildNumber = Subject.GroupBy(model => model.build.buildNumber);

        //    buildsPerBuildNumber.Subscribe(
        //        grp => grp.BufferUntilInactive(maxWaitDuration, Scheduler, ExpectedBuildCount).Take(1).Subscribe(
        //            async list => await SendNotificationAsync(list)));
        //}

        private async Task SendNotificationAsync(IList<TeamCityModel> buildStatuses, string oauthId)
        {
            bool notify;
            var message = new TeamCityMessageBuilder(ExpectedBuildCount).BuildMessage(buildStatuses, out notify);

            await Room.SendMessageAsync(message, oauthId);
        }

        //private void SearchConfigurationsFor(string rootUrl, string buildBuildTypeId)
        //{
        //    var allConfigurations = _tenantService.GetAllConfigurationAsync<Configuration<ServerBuildConfiguration>>();

        //    foreach (var configuration in allConfigurations)
        //        if (configuration.RootUrl == rootUrl && configuration.) 
        //}
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
            BuildConfigurations = new List<BuildConfiguration>();
            ServerRootUrl = string.Empty;
        }

        public string ServerRootUrl { get; set; }

        public List<BuildConfiguration> BuildConfigurations { get; set; }
    }
}