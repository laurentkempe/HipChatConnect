using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HipChatConnect.Controllers.Listeners.TeamCity.Models;
using Microsoft.Extensions.Options;

namespace HipChatConnect.Controllers.Listeners.TeamCity
{
    public class TeamCityMessageBuilder
    {
        private readonly int _expectedBuildCount;
        private readonly IOptions<AppSettings> _settings;

        public TeamCityMessageBuilder(int expectedBuildCount, IOptions<AppSettings> settings)
        {
            _expectedBuildCount = expectedBuildCount;
            _settings = settings;
        }

        public string BuildMessage(IList<TeamCityModel> buildStatuses, out bool notify)
        {
            var success = buildStatuses.Count == _expectedBuildCount &&
                          buildStatuses.All(buildStatus => IsSuccessfulBuild(buildStatus.build));

            notify = !success;

            return success ? BuildSuccessMessage(buildStatuses.First().build) :
                BuildFailureMessage(buildStatuses.Select(m => m.build).ToList());
        }

        public ActivityCardData BuildActivityCard(IList<TeamCityModel> buildStatuses)
        {
            var success = buildStatuses.Count == _expectedBuildCount &&
                          buildStatuses.All(buildStatus => IsSuccessfulBuild(buildStatus.build));

            return success ? BuildSuccessActivityCard(buildStatuses.First().build) :
                BuildFailureActivityCard(buildStatuses.Select(m => m.build).ToList());
        }

        private ActivityCardData BuildSuccessActivityCard(Build build)
        {
            return new SuccessfulTeamCityBuildActivityCardData(_settings?.Value?.BaseUrl)
            {
                Title = $"Successfully built {build.projectName} on agent {build.agentName} triggered by {build.triggeredBy}",
                Description = $"Successfully built branch {build.branchName}",
                Url = $"{build.buildStatusUrl}",
                ActivityHtml = BuildActivityHtml(build, "<strong>Successfully</strong> built")
            };
        }

        private ActivityCardData BuildFailureActivityCard(List<Build> builds)
        {
            var build = builds.First();

            return new FailedTeamCityBuildActivityCardData(_settings?.Value?.BaseUrl)
            {
                Title = $"Failed to build {build.projectName} on agent {build.agentName} triggered by {build.triggeredBy}",
                Description = $"Failed to built branch {build.branchName}",
                Url = $"{build.buildStatusUrl}",
                ActivityHtml = BuildActivityHtml(build, "<strong>Failed</strong> to build")
            };
        }

        private static string BuildActivityHtml(Build build, string introductionMessage)
        {
            return introductionMessage + $@" {build.projectName} branch {build.branchName} with build number <a href='{build.buildStatusUrl}'><strong>{build.buildNumber}</strong></a>.";
        }

        private static bool IsSuccessfulBuild(Build b)
        {
            return b.buildResult.Equals("success", StringComparison.OrdinalIgnoreCase);
        }

        private static string BuildSuccessMessage(Build build)
        {
            var stringBuilder = new StringBuilder();

            stringBuilder
                .AppendFormat( //todo externalize this in settings
                    @"<img src='http://ci.innoveo.com/img/buildStates/buildSuccessful.png' height='16' width='16'/><strong>Successfully</strong> built {0} branch {1} with build number <a href=""{2}""><strong>{3}</strong></a>",
                    build.projectName, build.branchName, build.buildStatusUrl, build.buildNumber);

            return stringBuilder.ToString();
        }

        private static string BuildFailureMessage(List<Build> builds)
        {
            var failedBuilds = builds.Where(b => !IsSuccessfulBuild(b)).ToList();

            var build = builds.First();
            var stringBuilder = new StringBuilder();

            stringBuilder
                .AppendFormat( //todo externalize this in settings
                    @"<img src='http://ci.innoveo.com/img/buildStates/buildFailed.png' height='16' width='16'/><strong>Failed</strong> to build {0} branch {1} with build number <a href=""{2}""><strong>{3}</strong></a>. Failed build(s) ",
                    build.projectName, build.branchName, build.buildStatusUrl, build.buildNumber);


            stringBuilder.Append(
                string.Join(", ",
                    failedBuilds.Select(fb => string.Format(@"<a href=""{0}""><strong>{1}</strong></a>", fb.buildStatusUrl, fb.buildName))));

            return stringBuilder.ToString();
        }
    }
}