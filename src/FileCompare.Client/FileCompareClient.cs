using FileCompare.Dto;
using System.Net.Http.Json;

namespace FileCompare.Client;

public class FileCompareClient
{
    private readonly HttpClient httpClient;

    public FileCompareClient(HttpClient httpClient) => this.httpClient = httpClient;

    public async Task<FileDto[]> GetFilesAsync()
    {
        var response = await this.httpClient.GetAsync("/files");

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<FileDto[]>() ?? Array.Empty<FileDto>();
    }

    public async Task<FileComparisonDto[]> GetFileDifferencesAsync()
    {
        var response = await this.httpClient.GetAsync("/files/differences");

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<FileComparisonDto[]>() ?? Array.Empty<FileComparisonDto>();
    }

    public async Task AddFilesAsync(FileDto[] files)
    {
        var response = await this.httpClient.PostAsJsonAsync("/files", files);

        response.EnsureSuccessStatusCode();
    }

    public async Task<FileComparisonDto[]> GetFileDuplicatesAsync()
    {
        var response = await this.httpClient.GetAsync("/files/duplicates");

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<FileComparisonDto[]>() ?? Array.Empty<FileComparisonDto>();
    }
}