using FileCompare.Dto;
using FileCompare.Model;
using FileCompare.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using File = FileCompare.Model.File;

public static class Endpoints
{
    #region Get Differences: GET /files/differences

    public static RouteHandlerBuilder MapGetDifferences(this WebApplication app) => app.MapGet("/files/differences", GetDifferences);

    private static IResult GetDifferences([FromServices] FileDbContext dbx)
    {
        var fileHashGroups =
            from fh in dbx.FileHashes
            join f in dbx.Files on fh.FileId equals f.Id
            join s in dbx.FileStorages on fh.StorageId equals s.Id
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

    #endregion Get Differences: GET /files/differences

    #region GetDuplicates: GET /files/duplicates

    public static RouteHandlerBuilder MapGetDuplicates(this WebApplication app) => app.MapGet("/files/duplicates", GetDuplicates);

    private static IResult GetDuplicates([FromServices] FileDbContext dbx)
    {
        var fileHashGroups =
            from fh in dbx.FileHashes
            join f in dbx.Files on fh.FileId equals f.Id
            join s in dbx.FileStorages on fh.StorageId equals s.Id
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

    #endregion GetDuplicates: GET /files/duplicates

    #region GetSingletons: GET /files/singletons

    public static RouteHandlerBuilder MapGetSingletons(this WebApplication app) => app.MapGet("/files/singletons", GetSingletons);

    private static IResult GetSingletons([FromServices] FileDbContext dbx)
    {
        var fileHashGroups =
           from fh in dbx.FileHashes
           join f in dbx.Files on fh.FileId equals f.Id
           join s in dbx.FileStorages on fh.StorageId equals s.Id
           group fh by new { fh.FileId } into fhg
           // has only one hash entry at all
           where fhg.Count() == 1
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

    #endregion GetSingletons: GET /files/singletons

    #region Get Files: GET /files

    public static RouteHandlerBuilder MapGetFiles(this WebApplication app) => app.MapGet("/files", GetFiles);

    private static IQueryable<File> AllFilesExpanded(FileDbContext dbx, string prefix)
        => string.IsNullOrEmpty(prefix)
        ? dbx.Files.Include(f => f.Hashes).ThenInclude(fh => fh.Storage)
        : dbx.Files.Where(f => f.FullName.StartsWith(prefix)).Include(f => f.Hashes).ThenInclude(fh => fh.Storage);

    private static IEnumerable<FileResponseDto> AllFilesMapped(FileDbContext dbx, string? path)
    {
        foreach (var file in AllFilesExpanded(dbx, path))
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

    private static IResult GetFiles([FromServices] FileDbContext dbx, [FromQuery] string? path)
        => Results.Ok(AllFilesMapped(dbx, path).ToArray());

    #endregion Get Files: GET /files

    #region Upsert files in database: POST /files

    public static RouteHandlerBuilder MapAddFiles(this WebApplication app) => app.MapPost("/files", AddFiles);

    private static async Task<IResult> AddFiles([FromServices] FileDbContext dbx, FileRequestDto[] files)
    {
        foreach (var file in files)
            await AddFile(dbx, file);

        return Results.Ok();
    }

    public static async Task AddFile(FileDbContext dbx, FileRequestDto fileDto)
    {
        var storage = UpsertFileStorage(dbx, fileDto);
        var file = UpsertFile(dbx, fileDto);

        await dbx.SaveChangesAsync();

        var fileHash = UpsertFileHash(dbx, storage, file, fileDto);

        if (fileHash.Hash != fileDto.Hash)
        {
            fileHash.Hash = fileDto.Hash;
            fileHash.Updated = fileDto.Updated;
            fileHash.Length = fileDto.Length;
            fileHash.CreationTimeUtc = fileDto.CreationTimeUtc;
            fileHash.LastAccessTimeUtc = fileDto.LastAccessTimeUtc;
            fileHash.LastWriteTimeUtc = fileDto.LastWriteTimeUtc;

            await dbx.SaveChangesAsync();
        }
    }

    private static FileHash UpsertFileHash(FileDbContext dbx, FileStorage fileStorage, File file, FileRequestDto fileDto)
    {
        var existing = dbx.FileHashes.FirstOrDefault(fh => fh.StorageId == fileStorage.Id && fh.FileId == file.Id);
        if (existing is null)
        {
            existing = new FileHash
            {
                File = file,
                Storage = fileStorage,
            };
            dbx.FileHashes.Add(existing);
        }

        return existing;
    }

    private static File UpsertFile(FileDbContext dbx, FileRequestDto fileDto)
    {
        var fullName = CleanupFullName(fileDto.FullName);
        var existing = dbx.Files.FirstOrDefault(fs => fs.FullName.Equals(fullName));
        if (existing is null)
        {
            existing = new File
            {
                Name = fileDto.Name,
                FullName = fullName,
            };
            dbx.Files.Add(existing);
        }
        return existing;
    }

    private static string CleanupFullName(string fullName) => fullName.Replace('\\', '/').Replace("//", "/").TrimStart('.').TrimStart('/');

    private static FileStorage UpsertFileStorage(FileDbContext dbx, FileRequestDto file)
    {
        var existing = dbx.FileStorages.FirstOrDefault(fs => fs.Host.Equals(file.Host));
        if (existing is null)
        {
            existing = new FileStorage { Host = file.Host };
            dbx.FileStorages.Add(existing);
        }
        return existing;
    }

    #endregion Upsert files in database: POST /files

    #region Deleöte file: DELETE /files/{id}

    public static RouteHandlerBuilder MapDeleteFile(this WebApplication app) => app.MapDelete("/files/{id}", DeleteFile);

    private static async Task<IResult> DeleteFile([FromServices] FileDbContext dbx, [FromRoute] int id)
    {
        var file = await dbx.Files.FindAsync(id);
        if (file is null)
            return Results.NotFound();

        dbx.Files.Remove(file);
        await dbx.SaveChangesAsync();

        return Results.Ok();
    }

    #endregion Deleöte file: DELETE /files/{id}
}