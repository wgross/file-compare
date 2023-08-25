using FileCompare.Dto;
using FileCompare.Model;
using FileCompare.Persistence;
using Microsoft.EntityFrameworkCore;
using File = FileCompare.Model.File;

namespace FileCompare.Service.Services;

public class FileCatalogService
{
    private readonly FileDbContext fileDb;

    public FileCatalogService(FileDbContext fileDb)
    {
        this.fileDb = fileDb;
    }

    public async Task AddFileAsync(string catalogName, FileRequestDto fileDto)
    {
        var catalog = this.UpsertFileCatalog(catalogName, fileDto);
        var storage = this.UpsertFileStorage(fileDto, catalog);
        var file = this.UpsertFile(fileDto, catalog);

        await this.fileDb.SaveChangesAsync();

        var fileHash = this.UpsertFileHash(storage, file, fileDto);

        if (fileHash.Hash != fileDto.Hash)
        {
            fileHash.Hash = fileDto.Hash;
            fileHash.Updated = fileDto.Updated;
            fileHash.Length = fileDto.Length;
            fileHash.CreationTimeUtc = fileDto.CreationTimeUtc;
            fileHash.LastAccessTimeUtc = fileDto.LastAccessTimeUtc;
            fileHash.LastWriteTimeUtc = fileDto.LastWriteTimeUtc;

            await this.fileDb.SaveChangesAsync();
        }
    }

    private FileCatalog UpsertFileCatalog(string catalogName, FileRequestDto fileDto)
    {
        catalogName = catalogName.Trim();
        var existing = this.fileDb.FileCatalogs.FirstOrDefault(fc => fc.Name.Equals(catalogName));
        if (existing is null)
        {
            existing = new FileCatalog
            {
                Name = catalogName
            };
            this.fileDb.FileCatalogs.Add(existing);
        }

        return existing;
    }

    private FileHash UpsertFileHash(FileStorage fileStorage, File file, FileRequestDto fileDto)
    {
        var existing = this.fileDb.FileHashes.FirstOrDefault(fh => fh.StorageId == fileStorage.Id && fh.FileId == file.Id);
        if (existing is null)
        {
            existing = new FileHash
            {
                File = file,
                Storage = fileStorage,
                Hash = "--temp--"
            };
            this.fileDb.FileHashes.Add(existing);
        }

        return existing;
    }

    private File UpsertFile(FileRequestDto fileDto, FileCatalog fileCatalog)
    {
        var fullName = CleanupFullName(fileDto.FullName);
        var existing = this.fileDb.Files.FirstOrDefault(fs => fs.FullName.Equals(fullName) && fs.CatalogId == fileCatalog.Id);
        if (existing is null)
        {
            existing = new File
            {
                Catalog = fileCatalog,
                Name = fileDto.Name,
                FullName = fullName,
            };
            this.fileDb.Files.Add(existing);
        }
        return existing;
    }

    private static string CleanupFullName(string fullName) => fullName.Replace('\\', '/').Replace("//", "/").TrimStart('.').TrimStart('/');

    private FileStorage UpsertFileStorage(FileRequestDto file, FileCatalog fileCatalog)
    {
        var existing = this.fileDb.FileStorages.FirstOrDefault(fs => fs.Host.Equals(file.Host));
        if (existing is null)
        {
            existing = new FileStorage
            {
                Catalog = fileCatalog,
                Host = file.Host
            };
            this.fileDb.FileStorages.Add(existing);
        }
        return existing;
    }

    public IEnumerable<File> GetAllFiles(string catalogName, string? path)
        => this.GetAllFiles(path).Where(f => f.Catalog.Name.Equals(catalogName)).AsEnumerable();

    private IQueryable<File> GetAllFiles(string? prefix) => string.IsNullOrEmpty(prefix)
        ? this.fileDb.Files.Include(f => f.Catalog).Include(f => f.Hashes).ThenInclude(fh => fh.Storage)
        : this.fileDb.Files.Where(f => f.FullName.StartsWith(prefix)).Include(f => f.Catalog).Include(f => f.Hashes).ThenInclude(fh => fh.Storage);

    public async Task<IEnumerable<FileCatalog>> GetCatalogsAsync() => await this.fileDb.FileCatalogs.ToListAsync();
}