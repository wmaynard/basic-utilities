using Maynard.Json;

namespace Maynard.Web;

public class LocationData : FlexModel
{
    public string IpAddress { get; set; }
    public string CountryCode { get; set; }

    public static LocationData Lookup(string ip) => new()
    {
        IpAddress = ip
        // TODO: Fill out IP Address lookup functionality
    };
}
