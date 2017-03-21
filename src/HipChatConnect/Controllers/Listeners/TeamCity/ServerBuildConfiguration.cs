using System.Collections.Generic;

namespace HipChatConnect.Controllers.Listeners.TeamCity
{
    public class ServerBuildConfiguration
    {
        public ServerBuildConfiguration()
        {
            ServerRootUrl = string.Empty;
            BuildConfigurations = new List<BuildConfiguration>();
        }

        public string ServerRootUrl { get; set; }

        public List<BuildConfiguration> BuildConfigurations { get; }
    }
}