﻿using System;
using System.Reactive.Concurrency;
using System.Threading.Tasks;
using HipChatConnect.Controllers.Listeners.TeamCity;
using HipChatConnect.Controllers.Listeners.TeamCity.Models;
using HipChatConnect.Services;
using HipChatConnect.Services.Impl;
using Microsoft.Extensions.Options;
using Microsoft.Reactive.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace HipChatConnect.Tests.Listeners.Teamcity
{
    [TestClass]
    public class TeamCityAggregatorTests
    {
        [TestMethod]
        public async Task HipChatRoomResult_ReceivedAllSuccessfulBuildsSteps_ExpectSuccessMessagePosted()
        {
            var teamcityBuildNotificationHandler = Substitute.For<ITeamcityBuildNotificationHandler>();
            var tenantService = makeTenantService(new[] {"build", "ndepend", "duplication", "publish"});
            var hipChatRoom = Substitute.For<IHipChatRoom>();

            var teamCityAggregator = new TeamCityAggregatorSUT(
                tenantService,
                teamcityBuildNotificationHandler,
                hipChatRoom,
                Substitute.For<IOptions<AppSettings>>());

            await teamCityAggregator.Initialization;
            teamCityAggregator.TestScheduler.Start();

            SendTeamcityBuildNotification(teamcityBuildNotificationHandler, "1", "build");
            SendTeamcityBuildNotification(teamcityBuildNotificationHandler, "1", "ndepend");
            SendTeamcityBuildNotification(teamcityBuildNotificationHandler, "1", "duplication");
            SendTeamcityBuildNotification(teamcityBuildNotificationHandler, "1", "publish");

            await hipChatRoom.Received(1).SendActivityCardAsync(
                Arg.Is<ActivityCardData>(x => x.Description == "Successfully built branch awesomeBranch"),
                Arg.Is("oAuth"));
        }

        [TestMethod]
        public async Task HipChatRoomResult_OneBuildMissingAndTimeout_ExpectFailureMessagePostedJustAfterTimeout()
        {
            var teamcityBuildNotificationHandler = Substitute.For<ITeamcityBuildNotificationHandler>();
            var tenantService = makeTenantService(new[] {"build", "ndepend", "duplication", "publish"});
            var hipChatRoom = Substitute.For<IHipChatRoom>();

            var teamCityAggregator = new TeamCityAggregatorSUT(
                tenantService,
                teamcityBuildNotificationHandler,
                hipChatRoom,
                Substitute.For<IOptions<AppSettings>>());

            await teamCityAggregator.Initialization;
            teamCityAggregator.TestScheduler.Start();

            SendTeamcityBuildNotification(teamcityBuildNotificationHandler, "1", "build");
            SendTeamcityBuildNotification(teamcityBuildNotificationHandler, "1", "ndepend");
            SendTeamcityBuildNotification(teamcityBuildNotificationHandler, "1", "duplication");

            teamCityAggregator.TestScheduler.AdvanceBy(TimeSpan.FromMinutes(9).Ticks);

            await hipChatRoom.Received(0).SendActivityCardAsync(Arg.Any<ActivityCardData>(), Arg.Is("oAuth"));

            teamCityAggregator.TestScheduler.AdvanceBy(TimeSpan.FromMinutes(1).Ticks);

            await hipChatRoom.Received(1).SendActivityCardAsync(
                Arg.Is<ActivityCardData>(x => x.Description == "Failed to build branch awesomeBranch"),
                Arg.Is("oAuth"));
        }

        [TestMethod]
        public async Task HipChatRoomResult_OneBuildMissingArrivesBeforeTimeout_ExpectSuccessMessagePosted()
        {
            var teamcityBuildNotificationHandler = Substitute.For<ITeamcityBuildNotificationHandler>();
            var tenantService = makeTenantService(new[] {"build", "ndepend", "duplication", "publish"});
            var hipChatRoom = Substitute.For<IHipChatRoom>();

            var teamCityAggregator = new TeamCityAggregatorSUT(
                tenantService,
                teamcityBuildNotificationHandler,
                hipChatRoom,
                Substitute.For<IOptions<AppSettings>>());

            await teamCityAggregator.Initialization;
            teamCityAggregator.TestScheduler.Start();

            SendTeamcityBuildNotification(teamcityBuildNotificationHandler, "1", "build");
            SendTeamcityBuildNotification(teamcityBuildNotificationHandler, "1", "ndepend");
            SendTeamcityBuildNotification(teamcityBuildNotificationHandler, "1", "duplication");

            teamCityAggregator.TestScheduler.AdvanceBy(TimeSpan.FromMinutes(9).Ticks);

            await hipChatRoom.Received(0).SendActivityCardAsync(Arg.Any<ActivityCardData>(), Arg.Is("oAuth"));

            SendTeamcityBuildNotification(teamcityBuildNotificationHandler, "1", "publish");

            await hipChatRoom.Received(1).SendActivityCardAsync(
                Arg.Is<ActivityCardData>(x => x.Description == "Successfully built branch awesomeBranch"),
                Arg.Is("oAuth"));
        }

        [TestMethod]
        public async Task HipChatRoomResult_2BuildStepsMissingButArrivingBeforeTimeout_ExpectSuccessMessagePosted()
        {
            var teamcityBuildNotificationHandler = Substitute.For<ITeamcityBuildNotificationHandler>();
            var tenantService = makeTenantService(new[] {"build", "ndepend", "duplication", "publish"});
            var hipChatRoom = Substitute.For<IHipChatRoom>();

            var teamCityAggregator = new TeamCityAggregatorSUT(
                tenantService,
                teamcityBuildNotificationHandler,
                hipChatRoom,
                Substitute.For<IOptions<AppSettings>>());

            await teamCityAggregator.Initialization;
            teamCityAggregator.TestScheduler.Start();

            SendTeamcityBuildNotification(teamcityBuildNotificationHandler, "1", "build");
            SendTeamcityBuildNotification(teamcityBuildNotificationHandler, "1", "ndepend");

            teamCityAggregator.TestScheduler.AdvanceBy(TimeSpan.FromMinutes(9).Ticks);

            SendTeamcityBuildNotification(teamcityBuildNotificationHandler, "1", "duplication");

            teamCityAggregator.TestScheduler.AdvanceBy(TimeSpan.FromMinutes(9).Ticks);

            await hipChatRoom.Received(0).SendActivityCardAsync(Arg.Any<ActivityCardData>(), Arg.Is("oAuth"));

            SendTeamcityBuildNotification(teamcityBuildNotificationHandler, "1", "publish");

            await hipChatRoom.Received(1).SendActivityCardAsync(
                Arg.Is<ActivityCardData>(x => x.Description == "Successfully built branch awesomeBranch"),
                Arg.Is("oAuth"));
        }

        [TestMethod]
        public async Task HipChatRoomResult_OneBuildStepFailed_ExpectFailureMessagePosted()
        {
            var teamcityBuildNotificationHandler = Substitute.For<ITeamcityBuildNotificationHandler>();
            var tenantService = makeTenantService(new[] {"build", "ndepend", "duplication", "publish"});
            var hipChatRoom = Substitute.For<IHipChatRoom>();

            var teamCityAggregator = new TeamCityAggregatorSUT(
                tenantService,
                teamcityBuildNotificationHandler,
                hipChatRoom,
                Substitute.For<IOptions<AppSettings>>());

            await teamCityAggregator.Initialization;
            teamCityAggregator.TestScheduler.Start();

            SendTeamcityBuildNotification(teamcityBuildNotificationHandler, "1", "build", "failed");

            await hipChatRoom.Received(1).SendActivityCardAsync(
                Arg.Is<ActivityCardData>(x => x.Description == "Failed to build branch awesomeBranch"),
                Arg.Is("oAuth"));
        }

        [TestMethod]
        public async Task HipChatRoomResult_2BuildsOneSuccessfulAndOneFailed_ExpectSuccessAndFailureMessagesPosted()
        {
            var teamcityBuildNotificationHandler = Substitute.For<ITeamcityBuildNotificationHandler>();
            var tenantService = makeTenantService(new[] {"build", "ndepend", "duplication", "publish"});
            var hipChatRoom = Substitute.For<IHipChatRoom>();

            var teamCityAggregator = new TeamCityAggregatorSUT(
                tenantService,
                teamcityBuildNotificationHandler,
                hipChatRoom,
                Substitute.For<IOptions<AppSettings>>());

            await teamCityAggregator.Initialization;
            teamCityAggregator.TestScheduler.Start();

            SendTeamcityBuildNotification(teamcityBuildNotificationHandler, "1", "duplication", branchName: "b1");
            SendTeamcityBuildNotification(teamcityBuildNotificationHandler, "2", "duplication", branchName: "b2");
            SendTeamcityBuildNotification(teamcityBuildNotificationHandler, "1", "ndepend", branchName: "b1");
            SendTeamcityBuildNotification(teamcityBuildNotificationHandler, "2", "ndepend", branchName: "b2");
            SendTeamcityBuildNotification(teamcityBuildNotificationHandler, "1", "build", branchName: "b1");
            SendTeamcityBuildNotification(teamcityBuildNotificationHandler, "2", "build", branchName: "b2");

            SendTeamcityBuildNotification(teamcityBuildNotificationHandler, "1", "publish", "failure", "b1");

            await hipChatRoom.Received(1).SendActivityCardAsync(
                Arg.Is<ActivityCardData>(x => x.Description == "Failed to build branch b1"),
                Arg.Is("oAuth"));

            SendTeamcityBuildNotification(teamcityBuildNotificationHandler, "2", "publish", branchName: "b2");

            await hipChatRoom.Received(1).SendActivityCardAsync(
                Arg.Is<ActivityCardData>(x => x.Description == "Successfully built branch b2"),
                Arg.Is("oAuth"));
        }

        [TestMethod]
        public async Task HipChatRoomResult_2BuildsOneSuccessfulAndOneTimedOut_ExpectSuccessAndFailureMessagesPosted()
        {
            var teamcityBuildNotificationHandler = Substitute.For<ITeamcityBuildNotificationHandler>();
            var tenantService = makeTenantService(new[] {"build", "ndepend", "duplication", "publish"});
            var hipChatRoom = Substitute.For<IHipChatRoom>();

            var teamCityAggregator = new TeamCityAggregatorSUT(
                tenantService,
                teamcityBuildNotificationHandler,
                hipChatRoom,
                Substitute.For<IOptions<AppSettings>>());

            await teamCityAggregator.Initialization;
            teamCityAggregator.TestScheduler.Start();

            SendTeamcityBuildNotification(teamcityBuildNotificationHandler, "1", "duplication", branchName: "b1");
            SendTeamcityBuildNotification(teamcityBuildNotificationHandler, "2", "duplication", branchName: "b2");
            SendTeamcityBuildNotification(teamcityBuildNotificationHandler, "1", "ndepend", branchName: "b1");
            SendTeamcityBuildNotification(teamcityBuildNotificationHandler, "2", "ndepend", branchName: "b2");
            SendTeamcityBuildNotification(teamcityBuildNotificationHandler, "1", "build", branchName: "b1");
            SendTeamcityBuildNotification(teamcityBuildNotificationHandler, "2", "build", branchName: "b2");

            await hipChatRoom.Received(0).SendActivityCardAsync(Arg.Any<ActivityCardData>(), Arg.Is("oAuth"));

            SendTeamcityBuildNotification(teamcityBuildNotificationHandler, "1", "publish");

            await hipChatRoom.Received(1).SendActivityCardAsync(
                Arg.Is<ActivityCardData>(x => x.Description == "Successfully built branch b1"),
                Arg.Is("oAuth"));

            teamCityAggregator.TestScheduler.AdvanceBy(TimeSpan.FromMinutes(10).Ticks);

            await hipChatRoom.Received(1).SendActivityCardAsync(
                Arg.Is<ActivityCardData>(x => x.Description == "Failed to build branch b2"),
                Arg.Is("oAuth"));
        }

        private static void SendTeamcityBuildNotification(
            ITeamcityBuildNotificationHandler teamcityBuildNotificationHandler,
            string buildNumber,
            string buildName,
            string buildResult = "success",
            string branchName = "awesomeBranch",
            string rootUrl = "aRootURL")
            => teamcityBuildNotificationHandler.NotificationReceived +=
                Raise.Event<EventHandler<TeamcityBuildNotification>>(
                    teamcityBuildNotificationHandler,
                    makeTeamcityBuildNotification(buildNumber, buildName, buildResult, branchName, rootUrl));

        private static TeamcityBuildNotification makeTeamcityBuildNotification(
            string buildNumber,
            string buildName,
            string buildResult = "success",
            string branchName = "awesomeBranch",
            string rootUrl = "aRootURL")
        {
            if (buildNumber == null) throw new ArgumentNullException(nameof(buildNumber));
            if (buildName == null) throw new ArgumentNullException(nameof(buildName));

            return new TeamcityBuildNotification(new TeamCityModel
            {
                build = new Build
                {
                    rootUrl = rootUrl,
                    buildNumber = buildNumber,
                    buildName = buildName,
                    buildResult = buildResult,
                    branchName = branchName
                }
            });
        }

        private static ITenantService makeTenantService(
            string[] buildConfigurationIds,
            string rootUrl = "aRootURL",
            int timeoutMinutes = 10)
        {
            if (buildConfigurationIds.Length == 0)
                throw new ArgumentException("Value cannot be an empty collection.", nameof(buildConfigurationIds));

            var serverBuildConfiguration = new ServerBuildConfiguration
            {
                ServerRootUrl = rootUrl,
                BuildConfiguration =
                {
                    MaxWaitDurationInMinutes = timeoutMinutes,
                    BuildConfigurationIds = string.Join(",", buildConfigurationIds)
                }
            };

            var tenantService = Substitute.For<ITenantService>();
            tenantService.GetAllConfigurationAsync<ServerBuildConfiguration>()
                .Returns(new[] {new Configuration<ServerBuildConfiguration>("oAuth", serverBuildConfiguration)});

            return tenantService;
        }

        private class TeamCityAggregatorSUT : TeamCityAggregator
        {
            public TeamCityAggregatorSUT(
                ITenantService tenantService,
                ITeamcityBuildNotificationHandler buildNotificationHandler,
                IHipChatRoom room,
                IOptions<AppSettings> settings)
                : base(tenantService, buildNotificationHandler, room, settings)
            {
            }

            protected override IScheduler Scheduler => TestScheduler;

            public TestScheduler TestScheduler { get; } = new TestScheduler();
        }
    }
}