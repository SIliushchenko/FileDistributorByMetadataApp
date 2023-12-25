using System.Net.Http.Json;
using System.Text;
using FileDistributorByMetadataApp.Interfaces;
using FileDistributorByMetadataApp.Services.Models;
using MetadataExtractor;
using NLog;
using Polly;
using Polly.Retry;
using Directory = System.IO.Directory;

namespace FileDistributorByMetadataApp.Services.Services
{
    public class FileDistributionService : IFileDistributionService
    {
        private const int DividerForMinutes = 60;
        private const int DividerForSeconds = 3600;
        private const int RetryCount = 3;
        private readonly AsyncRetryPolicy<LocationResponse> _asyncRetryPolicy;
        private readonly ISeasonService _seasonService;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static readonly HttpClient HttpClient = new()
        {
            BaseAddress = new Uri(@"https://nominatim.openstreetmap.org/")
        };
        private static readonly List<string> OldFolderPaths = new();

        public FileDistributionService(ISeasonService seasonService)
        {
            _seasonService = seasonService;
            var jitter = new Random();
            _asyncRetryPolicy = Policy<LocationResponse>
                .Handle<HttpRequestException>()
                .WaitAndRetryAsync(RetryCount, retryAttempt =>
                    TimeSpan.FromMilliseconds(Math.Pow(2, retryAttempt)) + TimeSpan.FromMilliseconds(jitter.Next(0, 1000)),
                    (_, _, retryCount, _) =>
                    {
                        Logger.Warn($"Couldn't get location information from the API endpoint for the file - Retrying #{retryCount}");
                    });
        }

        public async Task DistributeFilesByGpsCoordinatesAndDateAsync(string targetFolderPath, string destinationFolderPath, string languageCode = "ua",
            IProgress<int>? progress = null, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            var filePaths = GetFilePaths(targetFolderPath).ToList();
            var fileData = new List<FileData>();
            await foreach (var fileMeta in GetFileMetadataAsync(filePaths, languageCode, progress).ConfigureAwait(false))
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }
                fileData.Add(fileMeta);
            }

            DistributeFiles(fileData, destinationFolderPath, cancellationToken);
            DeleteOldFolders(OldFolderPaths);
        }

        private static void DistributeFiles(IEnumerable<FileData> fileData, string destinationFolderPath,
            CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }
            var directoryNameBuilder = new StringBuilder();
            var groupedFileMetadata = fileData.GroupBy(x => new
            {
                x.City,
                x.Country,
                x.Year,
                x.Village,
                x.Season
            }).Select(g => new
            {
                GroupedData = g.Key,
                FilePaths = g.Select(x => x.FilePath).ToList()
            }).ToList();

            foreach (var grpFileMetadata in groupedFileMetadata)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }
                directoryNameBuilder.Append(destinationFolderPath);
                directoryNameBuilder.Append($@"\{grpFileMetadata.GroupedData.Country} {(string.IsNullOrEmpty(grpFileMetadata.GroupedData.City)
                    ? grpFileMetadata.GroupedData.Village : grpFileMetadata.GroupedData.City)} ");
                if (grpFileMetadata.GroupedData.Year != DateTime.MinValue.Year)
                {
                    directoryNameBuilder.Append($"{grpFileMetadata.GroupedData.Year} ");
                    directoryNameBuilder.Append($"{grpFileMetadata.GroupedData.Season}");
                }
                var nerDirectoryInfo = Directory.CreateDirectory(directoryNameBuilder.ToString());
                directoryNameBuilder.Clear();

                foreach (var filePath in grpFileMetadata.FilePaths)
                {
                    var fileName = Path.GetFileName(filePath);
                    File.Move(filePath, nerDirectoryInfo.FullName + $@"\{fileName}");
                }
            }
        }

        private static IEnumerable<string> GetFilePaths(string targetFolderPath)
        {
            var filesPaths = new List<string>();

            ProcessDirectory(targetFolderPath, filesPaths);

            return filesPaths;
        }

        private static void ProcessDirectory(string directoryPath, List<string> filesPaths)
        {
            if (Directory.Exists(directoryPath))
            {
                filesPaths.AddRange(Directory.GetFiles(directoryPath));

                foreach (var subDirectory in Directory.GetDirectories(directoryPath))
                {
                    OldFolderPaths.Add(subDirectory);
                    ProcessDirectory(subDirectory, filesPaths);
                }
            }
        }

        private async IAsyncEnumerable<FileData> GetFileMetadataAsync(IReadOnlyList<string> filePaths, string languageCode, IProgress<int>? progress = null)
        {
            int counter = 0;
            int filePathsLength = filePaths.ToList().Count;
            foreach (var filePath in filePaths)
            {
                var fileMetadata = await GetFileMetadataAsync(filePath);
                if (fileMetadata is null)
                {
                    continue;
                }
                var locationResponse = await _asyncRetryPolicy.ExecuteAsync(async () => (await HttpClient.GetFromJsonAsync<LocationResponse>(
                    $"reverse?lat={fileMetadata.Latitude}&lon={fileMetadata.Longitude}&format=json&email=ilyushchenko.s@gmail.com&accept-language={languageCode}"))!)
                    .ConfigureAwait(false);

                if (progress is not null)
                {
                    counter++;
                    var percentComplete = counter * 100 / filePathsLength;
                    progress.Report(percentComplete);
                }
                yield return new FileData
                {
                    FilePath = filePath,
                    Village = locationResponse!.Address.Village,
                    City = locationResponse.Address.City,
                    Country = locationResponse.Address.Country,
                    Year = fileMetadata.CreatedDateTime.Year,
                    Season = _seasonService.GetSeason(fileMetadata.CreatedDateTime, languageCode)
                };
            }
        }

        private static async Task<FileMetadata> GetFileMetadataAsync(string filePath)
        {
            try
            {
                await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                var metadata = ImageMetadataReader.ReadMetadata(stream).SelectMany(x => x.Tags).ToList();
                var gpsMetadata = metadata
                    .Where(x => x.Name is "GPS Location" or "GPS Latitude" or "GPS Longitude").ToList();

                if (!gpsMetadata.Any())
                {
                    Logger.Info($"The file '{filePath}' does not have GPS metadata.");
                    return null!;
                }

                var gpsLocation = gpsMetadata.FirstOrDefault(x => x.Name == "GPS Location");
                var dateTime = metadata.FirstOrDefault(x => x.Name is "Creation Date");

                if (gpsLocation is not null)
                {
                    var latitudeAndLongitude = gpsLocation.Description!
                        .Split('+', StringSplitOptions.RemoveEmptyEntries).ToList();
                    return new FileMetadata
                    {
                        Latitude = Convert.ToDecimal(latitudeAndLongitude[0]),
                        Longitude = Convert.ToDecimal(latitudeAndLongitude[1]),
                        CreatedDateTime = dateTime != null ? DateTime.ParseExact(dateTime.Description!, "ddd MMM dd HH:mm:ss zzz yyyy", null) : DateTime.MinValue
                    };
                }

                if (gpsMetadata.Count == 1)
                {
                    throw new ImageProcessingException("The GPS metadata has incorrect format.");
                }

                dateTime ??= metadata.FirstOrDefault(x => x.Name is "Date/Time");

                var latitude = gpsMetadata[0].Description!.Split(new[] { '°', '\'', '\"' },
                    StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                var longitude = gpsMetadata[1].Description!.Split(new[] { '°', '\'', '\"' },
                    StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

                return new FileMetadata
                {
                    Latitude = Convert.ToDecimal(latitude[0]) + Convert.ToDecimal(latitude[1]) / DividerForMinutes +
                               Convert.ToDecimal(latitude[2]) / DividerForSeconds,
                    Longitude = Convert.ToDecimal(longitude[0]) + Convert.ToDecimal(longitude[1]) / DividerForMinutes +
                                Convert.ToDecimal(longitude[2]) / DividerForSeconds,
                    CreatedDateTime = dateTime != null ? DateTime.ParseExact(dateTime.Description!, "yyyy:MM:dd HH:mm:ss", null) : DateTime.MinValue
                };

            }
            catch (ImageProcessingException e)
            {
                Logger.Error(e.Message);
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
            }

            return null!;
        }

        private static void DeleteOldFolders(IEnumerable<string> folderPaths)
        {
            foreach (var folderPath in folderPaths)
            {
                if (Directory.GetFiles(folderPath).Length == 0)
                {
                    Directory.Delete(folderPath);
                }
            }

        }
    }
}