namespace Maynard.Tools.Extensions;

public static class DateTimeExtension
{
    public static string GetLocalTimezoneAbbreviation(this DateTime date)
    {
        bool isDst = TimeZoneInfo.Local.IsDaylightSavingTime(date);
        
        return TimeZoneInfo.Local.StandardName switch
        {
            "Pacific Standard Time" => isDst ? "PDT" : "PST",
            "Eastern Standard Time" => isDst ? "EDT" : "EST",
            "Central Standard Time" => isDst ? "CDT" : "CST",
            "Mountain Standard Time" => isDst ? "MDT" : "MST",
            "Universal Standard Time" => "UTC",
            _ => TimeZoneInfo.Local.Id // fallback to full name if unknown
        };
    }
}