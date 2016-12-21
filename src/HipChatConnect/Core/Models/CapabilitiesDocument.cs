namespace HipChatConnect.Core.Models
{
    public class CapabilitiesDocument
    {
        public Capabilities capabilities { get; set; }
        public string description { get; set; }
        public string key { get; set; }
        public Links links { get; set; }
        public string name { get; set; }
        public Vendor vendor { get; set; }
    }
}