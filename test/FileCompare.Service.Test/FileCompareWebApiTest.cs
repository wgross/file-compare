using FileCompare.Client;
using FileCompare.Dto;
using FileCompare.Persistence;
using System.Net.Http.Headers;

namespace FileCompare.Service.Test;

public class FileCompareWebApiTest
{
    private readonly FileCompareTestHostFactory factory;

    public HttpClient httpClient { get; }

    private readonly FileCatalogClient client;

    public FileCompareWebApiTest()
    {
        InMemoryDbContextOptionsBuilder.Default.Dispose();

        this.factory = new FileCompareTestHostFactory();
        this.httpClient = this.factory.CreateClient();
        this.httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        this.client = new(this.httpClient);
    }

    [Fact]
    public async Task Read_empty_files_from_DB()
    {
        // ACT
        var result = await this.client.GetFilesAsync("blah");

        // ASSERT
        Assert.Empty(result!);
    }

    [Theory]
    [InlineData("catalog")]
    [InlineData(" Catalog")]
    [InlineData("CATALOG ")]
    public async Task Add_catalog_and_read_again(string catalogName)
    {
        // ARRANGE
        var now = DateTime.UtcNow;

        // ACT
        await this.client.AddFilesAsync(catalogName, new[]
        {
            new UpsertFileRequestDto(
                "host",
                "name",
                "fullName",
                "hash",
                now.AddMinutes(-1),
                1,
                now,
                now.AddHours(1),
                now.AddDays(1))
        });

        // ASSERT
        var result = await this.client.GetCatalogsAsync();

        Assert.Equal(catalogName.Trim(), result.Single().Name);
    }

    [Theory]
    [InlineData("./fullName", "fullName")]
    [InlineData("fullName", "fullName")]
    [InlineData(".\\fullName", "fullName")]
    public async Task Add_file_and_read_again(string fullNameAdd, string fullNameRead)
    {
        // ARRANGE
        var now = DateTime.UtcNow;

        // ACT
        await this.client.AddFilesAsync("catalog", new[]
        {
            new UpsertFileRequestDto(
                "host",
                "name",
                fullNameAdd,
                "hash",
                now.AddMinutes(-1),
                1,
                now,
                now.AddHours(1),
                now.AddDays(1))
        });

        // ASSERT
        var result = await this.client.GetFilesAsync("catalog");

        Assert.Single(result!);
        Assert.NotEqual(0, result.First().Id);
        Assert.Equal("host", result.First().Host);
        Assert.Equal("name", result.First().Name);
        Assert.Equal(fullNameRead, result.First().FullName);
        Assert.Equal("hash", result.First().Hash);
        Assert.Equal(now.AddMinutes(-1), result.First().Updated);
        Assert.Equal(1, result.First().Length);
        Assert.Equal(now, result.First().CreationTimeUtc);
        Assert.Equal(now.AddHours(1), result.First().LastAccessTimeUtc);
        Assert.Equal(now.AddDays(1), result.First().LastWriteTimeUtc);
    }

    [Theory]
    [InlineData("./fullName")]
    [InlineData("fullName")]
    [InlineData(".\\fullName")]
    public async Task Add_files_tow_two_catalogs_and_read_again(string fullName)
    {
        // ARRANGE
        var now = DateTime.UtcNow;

        // ACT
        await this.client.AddFilesAsync("catalog1", new[]
        {
            new UpsertFileRequestDto(
                "host",
                "name",
                fullName,
                "hash",
                now.AddMinutes(-1),
                1,
                now,
                now.AddHours(1),
                now.AddDays(1))
        });
        await this.client.AddFilesAsync("catalog2", new[]
        {
            new UpsertFileRequestDto(
                "host",
                "name",
                fullName,
                "hash",
                now.AddMinutes(-1),
                1,
                now,
                now.AddHours(1),
                now.AddDays(1))
        });

        // ASSERT
        var result1 = await this.client.GetFilesAsync("catalog1");

        Assert.Single(result1!);
        Assert.NotEqual(0, result1.First().Id);
        Assert.Equal("host", result1.First().Host);
        Assert.Equal("name", result1.First().Name);
        Assert.Equal("fullName", result1.First().FullName);
        Assert.Equal("hash", result1.First().Hash);
        Assert.Equal(now.AddMinutes(-1), result1.First().Updated);
        Assert.Equal(1, result1.First().Length);
        Assert.Equal(now, result1.First().CreationTimeUtc);
        Assert.Equal(now.AddHours(1), result1.First().LastAccessTimeUtc);
        Assert.Equal(now.AddDays(1), result1.First().LastWriteTimeUtc);

        // ASSERT
        var result2 = await this.client.GetFilesAsync("catalog2");

        Assert.Single(result2!);
        Assert.NotEqual(0, result2.First().Id);
        Assert.Equal("host", result2.First().Host);
        Assert.Equal("name", result2.First().Name);
        Assert.Equal("fullName", result2.First().FullName);
        Assert.Equal("hash", result2.First().Hash);
        Assert.Equal(now.AddMinutes(-1), result2.First().Updated);
        Assert.Equal(1, result2.First().Length);
        Assert.Equal(now, result2.First().CreationTimeUtc);
        Assert.Equal(now.AddHours(1), result2.First().LastAccessTimeUtc);
        Assert.Equal(now.AddDays(1), result2.First().LastWriteTimeUtc);
    }

    [Fact]
    public async Task Add_file_and_read_again_with_variant_catalogName()
    {
        // ARRANGE
        var now = DateTime.UtcNow;

        // ACT
        await this.client.AddFilesAsync("catalog", new[]
        {
            new UpsertFileRequestDto(
                "host",
                "name",
                "fullName",
                "hash",
                now.AddMinutes(-1),
                1,
                now,
                now.AddHours(1),
                now.AddDays(1))
        });

        // ASSERT
        var result = await this.client.GetFilesAsync("CATALOG");

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
    public async Task Add_file_and_read_again_with_prefix(string fullNameAdd)
    {
        // ARRANGE
        var now = DateTime.UtcNow;

        // ACT
        await this.client.AddFilesAsync("catalog", new[]
        {
            new UpsertFileRequestDto("host", "name", "nomatch", "hash", now, 1, now, now,now),
            new UpsertFileRequestDto("host", "name", fullNameAdd, "hash",now.AddMinutes(-1), 1,now, now.AddHours(1), now.AddDays(1))
        });

        // ASSERT
        var result = await this.client.GetFilesAsync(catalogName: "catalog", path: "full");

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
    public async Task Read_differences_from_two_catalogs_from_db()
    {
        // ARRANGE
        var now = DateTime.UtcNow;

        await this.client.AddFilesAsync("catalog1", new[]
        {
            // this file is different at two hosts
            new UpsertFileRequestDto("host1", "name", "fullName", "hash1",now.AddMinutes(-1),1, now, now.AddHours(1), now.AddDays(1)),
            new UpsertFileRequestDto("host2", "name", "fullName", "hash2",now.AddMinutes(-2),2, now, now.AddHours(2), now.AddDays(2))
        });
        await this.client.AddFilesAsync("catalog2", new[]
        {
            // this file is different at two hosts
            new UpsertFileRequestDto("host1", "name", "fullName", "hash1",now.AddMinutes(-1),1, now, now.AddHours(1), now.AddDays(1)),
            new UpsertFileRequestDto("host2", "name", "fullName", "hash2",now.AddMinutes(-2),2, now, now.AddHours(2), now.AddDays(2))
        });
        // ACT
        var result1 = await this.client.GetFileDifferencesAsync("catalog1");
        var result2 = await this.client.GetFileDifferencesAsync("catalog2");

        // ASSERTs
        Assert.Single(result1!);
        var difference = result1.First();

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

        Assert.Single(result2!);

        difference = result2.First();

        Assert.NotEqual(0, difference.Id);
        Assert.Equal("name", difference.Name);
        Assert.Equal("fullName", difference.FullName);
        Assert.Equal(2, difference.Hashes.Length);

        hash1 = difference.Hashes[0];

        Assert.Equal("host1", hash1.Host);
        Assert.Equal("hash1", hash1.Hash);
        Assert.Equal(now.AddMinutes(-1), hash1.Updated);
        Assert.Equal(1, hash1.Length);
        Assert.Equal(now, hash1.CreationTimeUtc);
        Assert.Equal(now.AddHours(1), hash1.LastAccessTimeUtc);
        Assert.Equal(now.AddDays(1), hash1.LastWriteTimeUtc);

        hash2 = difference.Hashes[1];

        Assert.Equal("host2", hash2.Host);
        Assert.Equal("hash2", hash2.Hash);
        Assert.Equal(now.AddMinutes(-2), hash2.Updated);
        Assert.Equal(2, hash2.Length);
        Assert.Equal(now, hash2.CreationTimeUtc);
        Assert.Equal(now.AddHours(2), hash2.LastAccessTimeUtc);
        Assert.Equal(now.AddDays(2), hash2.LastWriteTimeUtc);
    }

    [Fact]
    public async Task Read_duplicates_of_two_catalogs_from_db()
    {
        // ARRANGE
        var now = DateTime.UtcNow;
        await this.client.AddFilesAsync("catalog1", new[]
        {
            // this file is a duplicat at two hosts
            new UpsertFileRequestDto("host1", "name", "fullName", "hash1",now.AddMinutes(-1),1, now, now.AddHours(1), now.AddDays(1)),
            new UpsertFileRequestDto("host2", "name", "fullName", "hash1",now.AddMinutes(-2),2, now, now.AddHours(2), now.AddDays(2))
        });
        await this.client.AddFilesAsync("catalog2", new[]
        {
            // this file is a duplicate at two hosts
            new UpsertFileRequestDto("host1", "name", "fullName", "hash1",now.AddMinutes(-1),1, now, now.AddHours(1), now.AddDays(1)),
            new UpsertFileRequestDto("host2", "name", "fullName", "hash1",now.AddMinutes(-2),2, now, now.AddHours(2), now.AddDays(2))
        });

        // ACT
        var result1 = await this.client.GetFileDuplicatesAsync("catalog1");
        var result2 = await this.client.GetFileDuplicatesAsync("catalog2");

        // ASSERT
        // duplicates have same hash, other properties are ignored
        Assert.Single(result1!);
        var duplicate = result1.First();

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

        Assert.Single(result1!);

        duplicate = result2.First();

        Assert.NotEqual(0, duplicate.Id);
        Assert.Equal("name", duplicate.Name);
        Assert.Equal("fullName", duplicate.FullName);
        Assert.Equal(2, duplicate.Hashes.Length);

        hash1 = duplicate.Hashes[0];

        Assert.Equal("host1", hash1.Host);
        Assert.Equal("hash1", hash1.Hash);
        Assert.Equal(now.AddMinutes(-1), hash1.Updated);
        Assert.Equal(1, hash1.Length);
        Assert.Equal(now, hash1.CreationTimeUtc);
        Assert.Equal(now.AddHours(1), hash1.LastAccessTimeUtc);
        Assert.Equal(now.AddDays(1), hash1.LastWriteTimeUtc);

        hash2 = duplicate.Hashes[1];

        Assert.Equal("host2", hash2.Host);
        Assert.Equal("hash1", hash2.Hash);
        Assert.Equal(now.AddMinutes(-2), hash2.Updated);
        Assert.Equal(2, hash2.Length);
        Assert.Equal(now, hash2.CreationTimeUtc);
        Assert.Equal(now.AddHours(2), hash2.LastAccessTimeUtc);
        Assert.Equal(now.AddDays(2), hash2.LastWriteTimeUtc);
    }

    [Fact]
    public async Task Read_singletons_of_two_catalogs_from_db()
    {
        // ARRANGE
        var now = DateTime.UtcNow;
        await this.client.AddFilesAsync("catalog1", new[]
        {
            // this is a singleton at host1
            new UpsertFileRequestDto("host1", "name", "fullName1", "hash1", now.AddMinutes(-1), 1, now, now.AddHours(1), now.AddDays(1)),
            // this file is at two hosts in the catalog
            new UpsertFileRequestDto("host2", "name", "fullName2", "hash1", now.AddMinutes(-2), 2, now, now.AddHours(2), now.AddDays(2)),
            new UpsertFileRequestDto("host3", "name", "fullName2", "hash1", now.AddMinutes(-2), 2, now, now.AddHours(2), now.AddDays(2))
        });
        await this.client.AddFilesAsync("catalog2", new[]
        {
            // this is a singleton at host1
            new UpsertFileRequestDto("host1", "name", "fullName1", "hash1", now.AddMinutes(-1), 1, now, now.AddHours(1), now.AddDays(1)),
            // this file is at two hosts in the catalog
            new UpsertFileRequestDto("host2", "name", "fullName2", "hash1", now.AddMinutes(-2), 2, now, now.AddHours(2), now.AddDays(2)),
            new UpsertFileRequestDto("host3", "name", "fullName2", "hash1", now.AddMinutes(-2), 2, now, now.AddHours(2), now.AddDays(2))
        });

        // ACT
        var result1 = await this.client.GetFileSingletonsAsync("catalog1");
        var result2 = await this.client.GetFileSingletonsAsync("catalog2");

        // ASSERT
        // duplicates have same hash, other properties are ignored
        Assert.Single(result1);

        var singleton = result1[0];

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

        // duplicates have same hash, other properties are ignored
        Assert.Single(result2);

        singleton = result1[0];

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

        await this.client.AddFilesAsync("catalog", new[]
        {
            new UpsertFileRequestDto("host", "name", "fullName", "hash", now.AddMinutes(-1), 1, now, now.AddHours(1), now.AddDays(1))
        });

        var file = (await this.client.GetFilesAsync("catalog")).Single();

        // ASSERT
        await this.client.DeleteFileAsync("catalog", file.Id);

        // ASSERT
        Assert.Empty(await this.client.GetFilesAsync("catalog"));
    }
}