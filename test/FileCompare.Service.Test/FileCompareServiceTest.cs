using FileCompare.Client;
using FileCompare.Dto;
using FileCompare.Persistence;

namespace FileCompare.Service.Test;

public class FileCompareServiceTest
{
    private readonly FileCompareTestHostFactory factory;
    private readonly FileCompareClient client;

    public FileCompareServiceTest()
    {
        InMemoryDbContextOptionsBuilder.Default.Dispose();

        this.factory = new FileCompareTestHostFactory();
        this.client = new(this.factory.CreateClient());
    }

    [Fact]
    public async Task Read_files_from_DB()
    {
        // ACT
        var result = await this.client.GetFilesAsync();

        // ASSERT
        Assert.Empty(result!);
    }

    [Theory]
    [InlineData("./fullname")]
    [InlineData("fullname")]
    [InlineData(".\\fullname")]
    public async Task Add_file_and_read_again(string fullName)
    {
        // ACT
        await this.client.AddFilesAsync(new[] { new FileDto("host", "name", fullName, "hash") });

        // ASSERT
        var result = await this.client.GetFilesAsync();

        Assert.Single(result!);
        Assert.Equal("host", result.First().Host);
        Assert.Equal("name", result.First().Name);
        Assert.Equal("fullname", result.First().FullName);
        Assert.Equal("hash", result.First().Hash);
    }

    [Theory]
    [InlineData("./full/name")]
    [InlineData("full\\name")]
    [InlineData(".\\full\\name")]
    public async Task Add_file_and_read_again_with_prefix(string fullName)
    {
        // ACT
        await this.client.AddFilesAsync(new[]
        {
            new FileDto("host", "name", "nomatch", "hash"),
            new FileDto("host", "name", fullName, "hash")
        });

        // ASSERT
        var result = await this.client.GetFilesAsync(path: "full");

        Assert.Single(result!);
        Assert.Equal("host", result.First().Host);
        Assert.Equal("name", result.First().Name);
        Assert.Equal("full/name", result.First().FullName);
        Assert.Equal("hash", result.First().Hash);
    }

    [Fact]
    public async Task Read_differences_from_db()
    {
        // ARRANGE
        await this.client.AddFilesAsync(new[]
        {
            new FileDto("host1", "name", "fullname", "hash1"),
            new FileDto("host2", "name", "fullname", "hash2")
        });

        // ACT
        var result = await this.client.GetFileDifferencesAsync();

        // ASSERT
        Assert.Single(result!);
        var difference = result.First();

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

    [Fact]
    public async Task Read_duplicates_from_db()
    {
        // ARRANGE
        await this.client.AddFilesAsync(new[]
        {
            new FileDto("host1", "name", "fullname", "hash1"),
            new FileDto("host2", "name", "fullname", "hash1")
        });

        // ACT
        var result = await this.client.GetFileDuplicatesAsync();

        // ASSERT
        Assert.Single(result!);
        var difference = result.First();

        Assert.Equal("name", difference.Name);
        Assert.Equal("fullname", difference.FullName);
        Assert.Equal(2, difference.Hashes.Length);

        var hash1 = difference.Hashes[0];
        Assert.Equal("host1", hash1.Host);
        Assert.Equal("hash1", hash1.Hash);

        var hash2 = difference.Hashes[1];
        Assert.Equal("host2", hash2.Host);
        Assert.Equal("hash1", hash2.Hash);
    }
}