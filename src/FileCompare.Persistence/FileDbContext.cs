using FileCompare.Model;
using Microsoft.EntityFrameworkCore;
using File = FileCompare.Model.File;

namespace FileCompare.Persistence;

public class FileDbContext : DbContext
{
    public DbSet<File> Files { get; set; }
    public DbSet<FileStorage> FileStorages { get; set; }
    public DbSet<FileHash> FileHashes { get; set; }
    public DbSet<FileCatalog> FileCatalogs { get; set; }

    public FileDbContext(DbContextOptions options)
        : base(options)
    { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var fileCatalogBuilder = modelBuilder.Entity<FileCatalog>();

        fileCatalogBuilder.HasKey(fc => fc.Id);
        fileCatalogBuilder.Property(fc => fc.Name).IsRequired().HasMaxLength(50).UseCollation("nocase");
        fileCatalogBuilder.HasIndex(fc => fc.Name).IsUnique();

        var fileBuilder = modelBuilder.Entity<File>();

        fileBuilder.HasKey(f => f.Id);
        fileBuilder.Property(f => f.Id).IsRequired().ValueGeneratedOnAdd();
        fileBuilder.Property(f => f.FullName).IsRequired().HasMaxLength(255); // common length of path at windows.
        fileBuilder.HasIndex(f => new { f.CatalogId, f.FullName }).IsUnique(unique: true);
        fileBuilder.HasOne(fileBuilder => fileBuilder.Catalog).WithMany().HasForeignKey(f => f.CatalogId);

        var fileHashBuilder = modelBuilder.Entity<FileHash>();

        fileHashBuilder.HasKey(fh => new { fh.FileId, fh.StorageId });
        fileHashBuilder.Property(fh => fh.Hash).HasMaxLength(64).IsRequired(); // SHA256 as string
        fileHashBuilder.HasOne(fh => fh.File).WithMany(f => f.Hashes).HasForeignKey(fh => fh.FileId);
        fileHashBuilder.HasOne(fh => fh.Storage).WithMany(fs => fs.Hashes).HasForeignKey(fh => fh.StorageId);

        var fileStorageBuilder = modelBuilder.Entity<FileStorage>();

        fileStorageBuilder.HasKey(fs => fs.Id);
        fileStorageBuilder.Property(fs => fs.Id).IsRequired().ValueGeneratedOnAdd();
        fileStorageBuilder.HasOne(fs => fs.Catalog).WithMany().HasForeignKey(fs => fs.CatalogId);
    }
}