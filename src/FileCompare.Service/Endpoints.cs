﻿using FileCompare.Dto;
using FileCompare.Persistence;
using FileCompare.Service.Services;
using Microsoft.AspNetCore.Mvc;

public static class Endpoints
{
    #region Get Catalogs: GET catalogs

    public static RouteHandlerBuilder MapGetCatalogs(this WebApplication app) => app.MapGet("catalogs", GetCatalogs);

    private static async Task<IResult> GetCatalogs([FromServices] FileCatalogService fileCatalog)
    {
        var catalogs = await fileCatalog.GetCatalogsAsync();

        return Results.Ok(catalogs.Select(c => new FileCatalogDto(Name: c.Name)));
    }

    #endregion Get Catalogs: GET catalogs

    #region Get Differences: GET catalogs/{catalogName}/files/differences

    public static RouteHandlerBuilder MapGetDifferences(this WebApplication app) => app.MapGet("catalogs/{catalogName}/files/differences", GetDifferences);

    private static IResult GetDifferences([FromServices] FileDbContext dbx, [FromRoute] string catalogName)
    {
        var fileHashGroups =
            from fh in dbx.FileHashes
            join f in dbx.Files on fh.FileId equals f.Id
            join s in dbx.FileStorages on fh.StorageId equals s.Id
            join c in dbx.FileCatalogs on f.CatalogId equals c.Id
            where c.Name.Equals(catalogName)
            group fh by new { fh.FileId } into fhg
            // has multiple hashes but some  are different
            where fhg.Count() > 1 && fhg.Select(fhg => fhg.Hash).Distinct().Count() > 1
            select new FileComparisonDto(
                fhg.First().File.Id,
                fhg.First().File.Name,
                fhg.First().File.FullName,
                fhg.Select(fh => new FileHashDto(
                    fh.Storage.Host,
                    fh.Hash,
                    fh.Updated,
                    fh.Length,
                    fh.CreationTimeUtc,
                    fh.LastAccessTimeUtc,
                    fh.LastWriteTimeUtc)).ToArray());

        return Results.Ok(fileHashGroups);
    }

    #endregion Get Differences: GET catalogs/{catalogName}/files/differences

    #region Get Duplicates: GET catalogs/{catalogName}/files/duplicates

    public static RouteHandlerBuilder MapGetDuplicates(this WebApplication app) => app.MapGet("catalogs/{catalogName}/files/duplicates", GetDuplicates);

    private static IResult GetDuplicates([FromServices] FileDbContext dbx, [FromRoute] string catalogName)
    {
        var fileHashGroups =
            from fh in dbx.FileHashes
            join f in dbx.Files on fh.FileId equals f.Id
            join s in dbx.FileStorages on fh.StorageId equals s.Id
            join c in dbx.FileCatalogs on f.CatalogId equals c.Id
            where c.Name.Equals(catalogName)
            group fh by new { fh.FileId } into fhg
            // has multiple hashes but all hashes are the identical
            where fhg.Count() > 1 && fhg.Select(fhg => fhg.Hash).Distinct().Count() == 1
            select new FileComparisonDto(
                fhg.First().File.Id,
                fhg.First().File.Name,
                fhg.First().File.FullName,
                fhg.Select(fh => new FileHashDto(
                    fh.Storage.Host,
                    fh.Hash,
                    fh.Updated,
                    fh.Length,
                    fh.CreationTimeUtc,
                    fh.LastAccessTimeUtc,
                    fh.LastWriteTimeUtc)).ToArray());

        return Results.Ok(fileHashGroups);
    }

    #endregion Get Duplicates: GET catalogs/{catalogName}/files/duplicates

    #region Get Singletons: GET catalogs/{catalogName}/files/singletons

    public static RouteHandlerBuilder MapGetSingletons(this WebApplication app) => app.MapGet("catalogs/{catalogName}/files/singletons", GetSingletons);

    private static IResult GetSingletons([FromServices] FileDbContext dbx, [FromRoute] string catalogName)
    {
        var fileHashGroups =
           from fh in dbx.FileHashes
           join f in dbx.Files on fh.FileId equals f.Id
           join s in dbx.FileStorages on fh.StorageId equals s.Id
           join c in dbx.FileCatalogs on f.CatalogId equals c.Id
           where c.Name == catalogName
           group fh by new { fh.FileId } into fhg
           // has only one hash entry at all
           where fhg.Count() == 1
           select new FileResponseDto(
                fhg.First().File.Id,
                fhg.First().Storage.Host,
                fhg.First().File.Name,
                fhg.First().File.FullName,
                fhg.First().Hash,
                fhg.First().File.Hashes.First().Updated,
                fhg.First().Length,
                fhg.First().CreationTimeUtc,
                fhg.First().LastAccessTimeUtc,
                fhg.First().LastWriteTimeUtc);

        return Results.Ok(fileHashGroups);
    }

    #endregion Get Singletons: GET catalogs/{catalogName}/files/singletons

    #region Get Files: GET catalogs/{catalogName}/files

    public static RouteHandlerBuilder MapGetFiles(this WebApplication app) => app.MapGet("catalogs/{catalogName}/files", GetFiles);

    private static IEnumerable<FileResponseDto> MapAllFiles(FileCatalogService fileCatalog, string catalogName, string? path)
    {
        foreach (var file in fileCatalog.GetAllFiles(catalogName, path))
            foreach (var hash in file.Hashes)
                yield return new FileResponseDto(
                    file.Id,
                    hash.Storage.Host,
                    file.Name,
                    file.FullName,
                    hash.Hash,
                    hash.Updated,
                    hash.Length,
                    hash.CreationTimeUtc,
                    hash.LastAccessTimeUtc,
                    hash.LastWriteTimeUtc);
    }

    private static IResult GetFiles([FromServices] FileCatalogService fileCatalog, [FromRoute] string catalogName, [FromQuery] string? path)
        => Results.Ok(MapAllFiles(fileCatalog, catalogName, path).ToArray());

    #endregion Get Files: GET catalogs/{catalogName}/files

    #region Upsert files in database: POST catalogs/{catalogName}/files

    public static RouteHandlerBuilder MapAddFiles(this WebApplication app) => app.MapPost("catalogs/{catalogName}/files", AddFiles);

    private static async Task<IResult> AddFiles([FromServices] FileCatalogService fileCatalog, [FromRoute] string catalogName, FileRequestDto[] files)
    {
        foreach (var file in files)
            await fileCatalog.AddFileAsync(catalogName, file);

        return Results.Ok();
    }

    #endregion Upsert files in database: POST catalogs/{catalogName}/files

    #region Delete file: DELETE catalogs/{catalogName}/files/{id}

    public static RouteHandlerBuilder MapDeleteFile(this WebApplication app) => app.MapDelete("catalogs/{catalogName}/files/{id}", DeleteFile);

    private static async Task<IResult> DeleteFile([FromServices] FileDbContext dbx, [FromRoute] string catalogName, [FromRoute] int id)
    {
        var file = await dbx.Files.FindAsync(id);
        if (file is null)
            return Results.NotFound();

        dbx.Files.Remove(file);
        await dbx.SaveChangesAsync();

        return Results.Ok();
    }

    #endregion Delete file: DELETE catalogs/{catalogName}/files/{id}
}