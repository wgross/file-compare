using FileCompare.Client;
using FileCompare.Dto;
using FileCompare.Persistence;

namespace FileCompare.Service.Test;

public class FileCompareWebApiTest
{
    private readonly FileCompareTestHostFactory factory;
    private readonly FileCompareClient client;

    public FileCompareWebApiTest()
    {
        InMemoryDbContextOptionsBuilder.Default.Dispose();

        this.factory = new FileCompareTestHostFactory();
        this.client = new(this.factory.CreateClient());
    }

    [Fact]
    public async Task Read_empty_files_from_DB()
    {
        // ACT
        var result = await this.client.GetFilesAsync();

        // ASSERT
        Assert.Empty(result!);
    }

    [Theory]
    [InlineData("./fullName")]
    [InlineData("fullName")]
    [InlineData(".\\fullName")]
    public async Task Add_file_and_read_again(string fullName)
    {
        // ARRANGE
        var now = DateTime.UtcNow;

        // ACT
        await this.client.AddFilesAsync(new[] { new FileRequestDto("host", "name", fullName, "hash", now.AddMinutes(-1), 1, now, now.AddHours(1), now.AddDays(1)) });

        // ASSERT
        var result = await this.client.GetFilesAsync();

        Assert.Single(result!);
        Assert.NotEqual(0, result.First().Id);
        Assert.Equal("host", result.First().Host);
        Assert.Equal("name", result.First().Name);
        Assert.Equal("fullName", result.First().FullName);
        Assert.Equal("hash", result.First().Hash);
        Assert.Equal(now.AddMinutes(-1), result.First().Updated);
        Assert.Equal(1, result.First().Length);
        Assert.Equal(now, result.First().CreationTimeUtc);
        Assert.Equal(now.AddHours(1), result.First().LastAccessTimeUtc);
        Assert.Equal(now.AddDays(1), result.First().LastWriteTimeUtc);
    }

    [Theory]
    [InlineData("./full/name")]
    [InlineData("full\\name")]
    [InlineData(".\\full\\name")]
    public async Task Add_file_and_read_again_with_prefix(string fullName)
    {
        // ARRANGE
        var now = DateTime.UtcNow;

        // ACT
        await this.client.AddFilesAsync(new[]
        {
            new FileRequestDto("host", "name", "nomatch", "hash", now, 1, now, now,now),
            new FileRequestDto("host", "name", fullName, "hash",now.AddMinutes(-1), 1,now, now.AddHours(1), now.AddDays(1))
        });

        // ASSERT
        var result = await this.client.GetFilesAsync(path: "full");

        Assert.Single(result!);
        Assert.NotEqual(0, result.First().Id);
        Assert.Equal("host", result.First().Host);
        Assert.Equal("name", result.First().Name);
        Assert.Equal("full/name", result.First().FullName);
        Assert.Equal("hash", result.First().Hash);
        Assert.Equal(now.AddMinutes(-1), result.First().Updated);
        Assert.Equal(1, result.First().Length);
        Assert.Equal(now, result.First().CreationTimeUtc);
        Assert.Equal(now.AddHours(1), result.First().LastAccessTimeUtc);
        Assert.Equal(now.AddDays(1), result.First().LastWriteTimeUtc);
    }

    [Fact]
    public async Task Read_differences_from_db()
    {
        // ARRANGE
        var now = DateTime.UtcNow;

        await this.client.AddFilesAsync(new[]
        {
            new FileRequestDto("host1", "name", "fullName", "hash1",now.AddMinutes(-1),1, now, now.AddHours(1), now.AddDays(1)),
            new FileRequestDto("host2", "name", "fullName", "hash2",now.AddMinutes(-2),2, now, now.AddHours(2), now.AddDays(2))
        });

        // ACT
        var result = await this.client.GetFileDifferencesAsync();

        // ASSERT
        Assert.Single(result!);
        var difference = result.First();

        Assert.NotEqual(0, difference.Id);
        Assert.Equal("name", difference.Name);
        Assert.Equal("fullName", difference.FullName);
        Assert.Equal(2, difference.Hashes.Length);

        var hash1 = difference.Hashes[0];
        Assert.Equal("host1", hash1.Host);
        Assert.Equal("hash1", hash1.Hash);
        Assert.Equal(now.AddMinutes(-1), hash1.Updated);
        Assert.Equal(1, hash1.Length);
        Assert.Equal(now, hash1.CreationTimeUtc);
        Assert.Equal(now.AddHours(1), hash1.LastAccessTimeUtc);
        Assert.Equal(now.AddDays(1), hash1.LastWriteTimeUtc);

        var hash2 = difference.Hashes[1];
        Assert.Equal("host2", hash2.Host);
        Assert.Equal("hash2", hash2.Hash);
        Assert.Equal(now.AddMinutes(-2), hash2.Updated);
        Assert.Equal(2, hash2.Length);
        Assert.Equal(now, hash2.CreationTimeUtc);
        Assert.Equal(now.AddHours(2), hash2.LastAccessTimeUtc);
        Assert.Equal(now.AddDays(2), hash2.LastWriteTimeUtc);
    }

    [Fact]
    public async Task Read_duplicates_from_db()
    {
        // ARRANGE
        var now = DateTime.UtcNow;
        await this.client.AddFilesAsync(new[]
        {
            new FileRequestDto("host1", "name", "fullName", "hash1",now.AddMinutes(-1),1, now, now.AddHours(1), now.AddDays(1)),
            new FileRequestDto("host2", "name", "fullName", "hash1",now.AddMinutes(-2),2, now, now.AddHours(2), now.AddDays(2))
        });

        // ACT
        var result = await this.client.GetFileDuplicatesAsync();

        // ASSERT
        // duplicates have same hash, other properties are ignored
        Assert.Single(result!);
        var duplicate = result.First();

        Assert.NotEqual(0, duplicate.Id);
        Assert.Equal("name", duplicate.Name);
        Assert.Equal("fullName", duplicate.FullName);
        Assert.Equal(2, duplicate.Hashes.Length);

        var hash1 = duplicate.Hashes[0];
        Assert.Equal("host1", hash1.Host);
        Assert.Equal("hash1", hash1.Hash);
        Assert.Equal(now.AddMinutes(-1), hash1.Updated);
        Assert.Equal(1, hash1.Length);
        Assert.Equal(now, hash1.CreationTimeUtc);
        Assert.Equal(now.AddHours(1), hash1.LastAccessTimeUtc);
        Assert.Equal(now.AddDays(1), hash1.LastWriteTimeUtc);

        var hash2 = duplicate.Hashes[1];
        Assert.Equal("host2", hash2.Host);
        Assert.Equal("hash1", hash2.Hash);
        Assert.Equal(now.AddMinutes(-2), hash2.Updated);
        Assert.Equal(2, hash2.Length);
        Assert.Equal(now, hash2.CreationTimeUtc);
        Assert.Equal(now.AddHours(2), hash2.LastAccessTimeUtc);
        Assert.Equal(now.AddDays(2), hash2.LastWriteTimeUtc);
    }

    [Fact]
    public async Task Read_singletons_from_db()
    {
        // ARRANGE
        var now = DateTime.UtcNow;
        await this.client.AddFilesAsync(new[]
        {
            new FileRequestDto("host1", "name", "fullName1", "hash1", now.AddMinutes(-1), 1, now, now.AddHours(1), now.AddDays(1)),
            new FileRequestDto("host2", "name", "fullName2", "hash1", now.AddMinutes(-2), 2, now, now.AddHours(2), now.AddDays(2))
        });

        // ACT
        var result = await this.client.GetFileSingletonsAsync();

        // ASSERT
        // duplicates have same hash, other properties are ignored
        Assert.Equal(2, result.Length);

        var singleton = result[0];

        Assert.NotEqual(0, singleton.Id);
        Assert.Equal("name", singleton.Name);
        Assert.Equal("fullName1", singleton.FullName);
        Assert.Equal("host1", singleton.Host);
        Assert.Equal("hash1", singleton.Hash);
        Assert.Equal(now.AddMinutes(-1), singleton.Updated);
        Assert.Equal(1, singleton.Length);
        Assert.Equal(now, singleton.CreationTimeUtc);
        Assert.Equal(now.AddHours(1), singleton.LastAccessTimeUtc);
        Assert.Equal(now.AddDays(1), singleton.LastWriteTimeUtc);
    }

    [Fact]
    public async Task Add_file_and_delete()
    {
        // ARRANGE
        var now = DateTime.UtcNow;

        await this.client.AddFilesAsync(new[] { new FileRequestDto("host", "name", "fullName", "hash", now.AddMinutes(-1), 1, now, now.AddHours(1), now.AddDays(1)) });

        var file = (await this.client.GetFilesAsync()).Single();

        // ASSERT
        await this.client.DeleteFileAsync(file.Id);

        // ASSERT
        Assert.Empty(await this.client.GetFilesAsync());
    }
}