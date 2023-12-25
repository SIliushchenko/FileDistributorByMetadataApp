using FileDistributorByMetadataApp.Services.Services;

namespace FileDistributorByMetadataApp.UnitTests
{
    public class SeasonServiceUnitTests
    {
        [Fact]
        public void GetSeason_DateTimeAndLanguageCode_CorrectSeasonName()
        {
            var seasonService = new SeasonService();
            var dateTime = new DateTime(2022, 5, 10);

            var actualSeason = seasonService.GetSeason(dateTime, "en");

            Assert.Equal("Spring", actualSeason);
        }
    }
}