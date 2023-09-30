using FileCompare.Client;
using FileCompare.Dto;
using Serilog;
using System.Security.Cryptography;

namespace FileCompare.Console;

/// <summary>
/// Program class for the console application
/// </summary>
public class Program
{
    private static readonly MD5 hash = MD5.Create();

    static Program()
    {
        Log.Logger = new LoggerConfiguration()
          // write logging message template expanded to plain text
          .WriteTo.File(
              path: @"logs\fccli-.log",
              outputTemplate: "[{Timestamp:yyyMMdd-HH:mm:ss.fff}][{Level:u3}][{SourceContext}]:{Message:lj}{NewLine}{Exception}",
              rollingInterval: RollingInterval.Day)
          .WriteTo.Console()
          .CreateLogger();
    }

    public static async Task<int> Main(string catalog, DirectoryInfo path, string uri)
    {
        Log.Logger.Information("Updating directory: {directory} in the catalog {catalog} at {uri}",
            path.FullName,
            catalog,
            uri);

        // Render the table to the console

        var currentDirectory = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(path.FullName);

            var client = new FileCatalogClient(new HttpClient
            {
                BaseAddress = new(uri)
            });

            await ProcessDirectoryAsCatalog(catalog, path, client);
        }
        finally
        {
            Directory.SetCurrentDirectory(currentDirectory);
        }
        return 0;
    }

    private static async Task ProcessDirectoryAsCatalog(string catalogName, DirectoryInfo path, FileCatalogClient client)
    {
        var chunk = new List<UpsertFileRequestDto>();

        await foreach (var modifiedFile in HashModifiedFilesInCatalog(catalogName, path, client))
        {
            chunk.Add(modifiedFile);

            Log.Logger.Debug("Updating: {file}", modifiedFile.FullName);

            if (chunk.Count > 10)
            {
                await client.AddFilesAsync(catalogName, chunk.ToArray());

                foreach (var file in chunk)
                    Log.Logger.Information("Upserted: {file}", file.FullName);

                chunk.Clear();
            }
        }

        if (chunk.Any())
        {
            await client.AddFilesAsync(catalogName, chunk.ToArray());

            foreach (var file in chunk)
                Log.Logger.Information("Upserted: {file}", file.FullName);
        }
    }

    private static async IAsyncEnumerable<UpsertFileRequestDto> HashModifiedFilesInCatalog(string catalogName, DirectoryInfo path, FileCatalogClient fileCatalogClient)
    {
        await foreach (var modifiedFile in FindModifiedFilesInCatalog(catalogName, path, fileCatalogClient))
        {
            var hash = HashFile(modifiedFile);

            if (hash.Ok)
            {
                yield return new UpsertFileRequestDto(
                    Host: Environment.MachineName,
                    Name: modifiedFile.Name,
                    FullName: Path.GetRelativePath(Environment.CurrentDirectory, modifiedFile.FullName),
                    Hash: hash.Hash!, // Hash is null is excluded above
                    Updated: DateTime.UtcNow,
                    Length: modifiedFile.Length,
                    CreationTimeUtc: modifiedFile.CreationTimeUtc,
                    LastAccessTimeUtc: modifiedFile.LastAccessTimeUtc,
                    LastWriteTimeUtc: modifiedFile.LastWriteTimeUtc);
            }
        }
    }

    private static async IAsyncEnumerable<FileInfo> FindModifiedFilesInCatalog(string catalogName, DirectoryInfo path, FileCatalogClient fileCatalogClient)
    {
        foreach (var localFile in Directory.EnumerateFiles(path.FullName, "*", SearchOption.AllDirectories).Select(x => new FileInfo(x)))
        {
            var fileRelativePath = Path.GetRelativePath(Environment.CurrentDirectory, localFile.FullName);

            var cataloguedFiles = await fileCatalogClient.GetFilesAsync(catalogName, fileRelativePath);

            var cataloguedFile = cataloguedFiles.FirstOrDefault(x => x.Host.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase));

            if (cataloguedFile is null || !MatchLocalToCatalog(localFile, cataloguedFile))
            {
                Log.Logger.Debug("File: {file} will be upserted to the catalog.", localFile.FullName);

                yield return localFile;
            }
            else Log.Logger.Debug("File: {file} is up-to-date in the catalog.", localFile.FullName);
        }
    }

    private static bool MatchLocalToCatalog(FileInfo localFile, FileResponseDto cataloguedFile)
    {
        // host and path should already match, I'm interested in the meta data
        if (localFile.Length != cataloguedFile.Length)
            return false;
        if (localFile.CreationTimeUtc != cataloguedFile.CreationTimeUtc)
            return false;
        if (localFile.LastWriteTimeUtc != cataloguedFile.LastWriteTimeUtc)
            return false;

        return true;
    }

    private static (bool Ok, string? Hash) HashFile(FileInfo file)
    {
        try
        {
            Log.Logger.Debug("Hashing file {file}", file.FullName);

            using var fileStream = file.OpenRead();

            return (true, BitConverter.ToString(hash.ComputeHash(fileStream)));
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Error hashing file: {file}", file.FullName);

            return (false, null);
        }
    }
}