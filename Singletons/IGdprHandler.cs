namespace Maynard.Singletons;

public interface IGdprHandler
{
    public long ProcessGdprRequest(string accountId, string dummyText);
}