﻿using FileCompare.Dto;
using System.Net.Http.Json;
using System.Web;

namespace FileCompare.Client;

public class FileCompareClient
{
    private readonly HttpClient httpClient;

    public FileCompareClient(HttpClient httpClient) => this.httpClient = httpClient;

    public async Task<FileResponseDto[]> GetFilesAsync(string? path = null)
    {
        var response = string.IsNullOrEmpty(path)
            ? await this.httpClient.GetAsync("/files")
            : await this.httpClient.GetAsync($"/files?path={HttpUtility.UrlEncode(path)}");

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<FileResponseDto[]>() ?? Array.Empty<FileResponseDto>();
    }

    public async Task<FileComparisonDto[]> GetFileDifferencesAsync()
    {
        var response = await this.httpClient.GetAsync("/files/differences");

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<FileComparisonDto[]>() ?? Array.Empty<FileComparisonDto>();
    }

    public async Task AddFilesAsync(FileRequestDto[] files)
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

    public async Task<FileResponseDto[]> GetFileSingletonsAsync()
    {
        var response = await this.httpClient.GetAsync("/files/singletons");

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<FileResponseDto[]>() ?? Array.Empty<FileResponseDto>();
    }

    public async Task DeleteFileAsync(int id) => await this.httpClient.DeleteAsync($"/files/{id}");
}