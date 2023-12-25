using FileDistributorByMetadataApp.Interfaces;
using System.Reflection;
using System.Resources;

namespace FileDistributorByMetadataApp.Services.Services
{
    public class SeasonService : ISeasonService
    {
        public string GetSeason(DateTime dateTime, string language)
        {
            var resourceManager = new ResourceManager($"FileDistributorByMetadataApp.Services.Resources.{language}", Assembly.GetExecutingAssembly());
            int month = dateTime.Month;
            return month switch
            {
                12 => resourceManager.GetString("Winter")!,
                1 => resourceManager.GetString("Winter")!,
                2 => resourceManager.GetString("Winter")!,
                3 => resourceManager.GetString("Spring")!,
                4 => resourceManager.GetString("Spring")!,
                5 => resourceManager.GetString("Spring")!,
                6 => resourceManager.GetString("Summer")!,
                7 => resourceManager.GetString("Summer")!,
                8 => resourceManager.GetString("Summer")!,
                9 => resourceManager.GetString("Autumn")!,
                10 => resourceManager.GetString("Autumn")!,
                11 => resourceManager.GetString("Autumn")!,
                _ => null!
            };
        }
    }
}
