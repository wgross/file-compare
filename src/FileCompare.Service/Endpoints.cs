using FileCompare.Dto;
using FileCompare.Model;
using FileCompare.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using File = FileCompare.Model.File;

public static class Endpoints
{
    #region Get Differences

    public static RouteHandlerBuilder MapGetDifferences(this WebApplication app) => app.MapGet("/files/differences", GetDifferences);

    private static IResult GetDifferences([FromServices] FileDbContext dbx)
    {
        var fileHashGroups =
            from fh in dbx.FileHashes
            join f in dbx.Files on fh.FileId equals f.Id
            join s in dbx.FileStorages on fh.StorageId equals s.Id
            group fh by new { fh.FileId } into fhg
            where fhg.Count() > 1 && fhg.Select(fhg => fhg.Hash).Distinct().Count() > 1
            select new FileDifferenceDto(
                fhg.First().File.Name,
                fhg.First().File.FullName,
                fhg.Select(fh => new FileHashDto(fh.Storage.Host, fh.Hash, fh.Updated)).ToArray());

        return Results.Ok(fileHashGroups.ToArray());
    }

    #endregion Get Differences

    #region Get Files

    public static RouteHandlerBuilder MapGetFiles(this WebApplication app) => app.MapGet("/files", GetFiles);

    private static IEnumerable<File> AllFilesExpanded(FileDbContext dbx)
        => dbx.Files.Include(f => f.Hashes).ThenInclude(fh => fh.Storage);

    private static IEnumerable<FileDto> AllFilesMapped([FromServices] FileDbContext dbx)
    {
        foreach (var file in AllFilesExpanded(dbx))
            foreach (var hash in file.Hashes)
                yield return new FileDto(hash.Storage.Host, file.Name, file.FullName, hash.Hash);
    }

    private static IResult GetFiles([FromServices] FileDbContext dbx)
        => Results.Ok(AllFilesMapped(dbx).ToArray());

    #endregion Get Files

    #region Upsert files in database

    public static RouteHandlerBuilder MapAddFiles(this WebApplication app) => app.MapPost("/files", AddFiles);

    private static async Task<IResult> AddFiles([FromServices] FileDbContext dbx, FileDto[] files)
    {
        foreach (var file in files)
            await AddFile(dbx, file);

        return Results.Ok();
    }

    public static async Task AddFile(FileDbContext dbx, FileDto fileDto)
    {
        var storage = UpsertFileStorage(dbx, fileDto);
        var file = UpsertFile(dbx, fileDto);

        await dbx.SaveChangesAsync();

        var fileHash = UpsertFileHash(dbx, storage, file, fileDto);

        if (fileHash.Hash != fileDto.Hash)
        {
            fileHash.Hash = fileDto.Hash;
            fileHash.Updated = DateTime.Now;

            await dbx.SaveChangesAsync();
        }
    }

    private static FileHash UpsertFileHash(FileDbContext dbx, FileStorage fileStorage, File file, FileDto fileDto)
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

    private static File UpsertFile(FileDbContext dbx, FileDto file)
    {
        var existing = dbx.Files.FirstOrDefault(fs => fs.FullName.Equals(file.FullName));
        if (existing is null)
        {
            existing = new File
            {
                Name = file.Name,
                FullName = file.FullName,
            };
            dbx.Files.Add(existing);
        }
        return existing;
    }

    private static FileStorage UpsertFileStorage(FileDbContext dbx, FileDto file)
    {
        var existing = dbx.FileStorages.FirstOrDefault(fs => fs.Host.Equals(file.Host));
        if (existing is null)
        {
            existing = new FileStorage { Host = file.Host };
            dbx.FileStorages.Add(existing);
        }
        return existing;
    }

    #endregion Upsert files in database
}