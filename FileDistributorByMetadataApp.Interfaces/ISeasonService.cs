namespace FileDistributorByMetadataApp.Interfaces
{
    public interface ISeasonService
    {
        string GetSeason(DateTime dateTime, string language);
    }
}
