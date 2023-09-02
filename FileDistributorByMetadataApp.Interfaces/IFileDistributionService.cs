namespace FileDistributorByMetadataApp.Interfaces
{
    public interface IFileDistributionService
    {
        Task DistributeFilesByGpsCoordinatesAndDateAsync(string targetFolderPath, string destinationFolderPath, string languageCode,
            IProgress<int>? progress, CancellationToken cancellationToken);
    }
}
