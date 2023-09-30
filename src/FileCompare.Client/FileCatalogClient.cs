using FileCompare.Dto;
using System.Net.Http.Json;
using System.Web;

namespace FileCompare.Client;

public class FileCatalogClient
{
    private readonly HttpClient httpClient;

    public FileCatalogClient(HttpClient httpClient) => this.httpClient = httpClient;

    public async Task<FileResponseDto[]> GetFilesAsync(string catalogName, string? path = null)
    {
        var response = string.IsNullOrEmpty(path)
            ? await this.httpClient.GetAsync($"catalogs/{catalogName}/files")
            : await this.httpClient.GetAsync($"catalogs/{catalogName}/files?path={HttpUtility.UrlEncode(path)}");

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<FileResponseDto[]>() ?? Array.Empty<FileResponseDto>();
    }

    public async Task<FileComparisonDto[]> GetFileDifferencesAsync(string catalogName)
    {
        var response = await this.httpClient.GetAsync($"catalogs/{catalogName}/files/differences");

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<FileComparisonDto[]>() ?? Array.Empty<FileComparisonDto>();
    }

    public async Task AddFilesAsync(string catalogName, UpsertFileRequestDto[] files)
    {
        var response = await this.httpClient.PostAsJsonAsync($"catalogs/{catalogName}/files", files);

        response.EnsureSuccessStatusCode();
    }

    public async Task<FileComparisonDto[]> GetFileDuplicatesAsync(string catalogName)
    {
        var response = await this.httpClient.GetAsync($"catalogs/{catalogName}/files/duplicates");

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<FileComparisonDto[]>() ?? Array.Empty<FileComparisonDto>();
    }

    public async Task<FileResponseDto[]> GetFileSingletonsAsync(string catalogName)
    {
        var response = await this.httpClient.GetAsync($"catalogs/{catalogName}/files/singletons");

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<FileResponseDto[]>() ?? Array.Empty<FileResponseDto>();
    }

    public async Task DeleteFileAsync(string catalogName, int id) => await this.httpClient.DeleteAsync($"catalogs/{catalogName}/files/{id}");

    public async Task<FileCatalogDto[]> GetCatalogsAsync()
    {
        var response = await this.httpClient.GetAsync("catalogs");

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<FileCatalogDto[]>() ?? Array.Empty<FileCatalogDto>();
    }
}