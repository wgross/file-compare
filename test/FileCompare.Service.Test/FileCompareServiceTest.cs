using FileCompare.Dto;
using System.Net.Http.Json;

namespace FileCompare.Service.Test;

public class FileCompareServiceTest
{
    private readonly FileCompareTestHostFactory factory;
    private readonly HttpClient client;

    public FileCompareServiceTest()
    {
        this.factory = new FileCompareTestHostFactory();
        this.client = this.factory.CreateClient();
    }

    [Fact]
    public async Task Read_files_from_DB()
    {
        // ACT
        var result = await this.client.GetAsync("/files");

        // ASSERT
        Assert.True(result.IsSuccessStatusCode);

        var files = await result.Content.ReadFromJsonAsync<IEnumerable<FileDto>>();

        Assert.Empty(files!);
    }

    [Fact]
    public async Task Add_file_and_read_again()
    {
        // ACT
        var result = await this.client.PostAsJsonAsync("/files", new[] { new FileDto("host", "name", "fullname", "hash") });

        // ASSERT
        Assert.True(result.IsSuccessStatusCode);

        var files = await this.client.GetFromJsonAsync<FileDto[]>("/files");

        Assert.Single(files!);
        Assert.Equal("host", files.First().Host);
        Assert.Equal("name", files.First().Name);
        Assert.Equal("fullname", files.First().FullName);
        Assert.Equal("hash", files.First().Hash);
    }

    [Fact]
    public async Task Read_differences_from_db()
    {
        // ARRANGE
        await this.client.PostAsJsonAsync("/files", new[] { new FileDto("host1", "name", "fullname", "hash1") });
        await this.client.PostAsJsonAsync("/files", new[] { new FileDto("host2", "name", "fullname", "hash2") });

        // ACT
        var response = await this.client.GetAsync("/files/differences");

        // ASSERT
        Assert.True(response.IsSuccessStatusCode);
        var differences = await response.Content.ReadFromJsonAsync<IEnumerable<FileDifferenceDto>>();

        Assert.Single(differences!);
        var difference = differences.First();

        Assert.Equal("name", difference.Name);
        Assert.Equal("fullname", difference.FullName);
        Assert.Equal(2, difference.Hashes.Length);

        var hash1 = difference.Hashes[0];
        Assert.Equal("host1", hash1.Host);
        Assert.Equal("hash1", hash1.Hash);

        var hash2 = difference.Hashes[1];
        Assert.Equal("host2", hash2.Host);
        Assert.Equal("hash2", hash2.Hash);
    }
}