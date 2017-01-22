using System.Collections.Generic;

namespace HipChatConnect.Controllers.Listeners.TeamCity
{
    public class ServerBuildConfiguration
    {
        public ServerBuildConfiguration()
        {
            ServerRootUrl = string.Empty;
            BuildConfiguration = new BuildConfiguration();
        }

        public string ServerRootUrl { get; set; }

        public BuildConfiguration BuildConfiguration { get; }
    }
}