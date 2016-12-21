namespace HipChatConnect.Core.Models
{
    public class InstallationData
    {
        public string capabilitiesUrl { get; set; }
        public string oauthId { get; set; }
        public string oauthSecret { get; set; }
        public int groupId { get; set; }
        public string roomId { get; set; }

        //Save the token endpoint URL along with the client credentials
        public string tokenUrl;
        //Save the API endpoint URL along with the client credentials
        public string apiUrl;
    }
}