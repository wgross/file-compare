using FileCompare.Client;
using FileCompare.Dto;
using System.Security.Cryptography;

namespace FileCompare.Console;

/// <summary>
/// Program class for the console application
/// </summary>
public class Program
{
    private static readonly SHA256 hash = SHA256.Create();

    public static async Task<int> Main(DirectoryInfo path)
    {
        System.Console.WriteLine(path.FullName);

        var client = new FileCompareClient(new HttpClient
        {
            BaseAddress = new("http://localhost:5000")
        });

        var filesInChunksOfTen = Directory.EnumerateFiles(path.FullName, "*", SearchOption.AllDirectories)
            .Select(x => new FileInfo(x))
            .Select(x => (File: x, Hash: HashFile(x)))
            .Where(x => x is { Hash: { Ok: true } })
            .Select(x => new FileDto(Environment.MachineName, x.File.Name, Path.GetRelativePath(Environment.CurrentDirectory, x.File.FullName), x.Hash.Hash))
            .Chunk(10);

        foreach (var chunk in filesInChunksOfTen)
        {
            System.Console.WriteLine(string.Join(",", chunk.Select(x => x.Name)));

            await client.AddFilesAsync(chunk);
        }

        return 0;
    }

    private static (bool Ok, string? Hash) HashFile(FileInfo file)
    {
        try
        {
            using var fileStream = new FileStream(file.FullName, FileMode.Open);

            return (true, BitConverter.ToString(hash.ComputeHash(fileStream)));
        }
        catch (Exception ex)
        {
            System.Console.Error.WriteLine(ex.Message);

            return (false, null);
        }
    }
}