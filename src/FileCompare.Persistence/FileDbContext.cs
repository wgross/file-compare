using FileCompare.Model;
using Microsoft.EntityFrameworkCore;
using File = FileCompare.Model.File;

namespace FileCompare.Persistence;

public class FileDbContext : DbContext
{
    public DbSet<File> Files { get; set; }
    public DbSet<FileStorage> FileStorages { get; set; }
    public DbSet<FileHash> FileHashes { get; set; }

    public FileDbContext(DbContextOptions options)
        : base(options)
    { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var fileBuilder = modelBuilder.Entity<File>();

        fileBuilder.HasKey(e => e.Id);
        fileBuilder.Property(e => e.Id).IsRequired().ValueGeneratedOnAdd();
        fileBuilder.Property(f => f.FullName).IsRequired().HasMaxLength(255); // common length of path at windows.
        fileBuilder.HasIndex(f => f.FullName).IsUnique(unique: true);

        var fileHashBuilder = modelBuilder.Entity<FileHash>();

        fileHashBuilder.HasKey(e => new { e.FileId, e.StorageId });
        fileHashBuilder.Property(e => e.Hash).HasMaxLength(64).IsRequired(); // SHA256 as string
        fileHashBuilder.HasOne(fh => fh.File).WithMany(f => f.Hashes).HasForeignKey(fh => fh.FileId);
        fileHashBuilder.HasOne(fs => fs.Storage).WithMany(fs => fs.Hashes).HasForeignKey(fh => fh.StorageId);

        var fileStorageBuilder = modelBuilder.Entity<FileStorage>();

        fileStorageBuilder.HasKey(e => e.Id);
        fileStorageBuilder.Property(e => e.Id).IsRequired().ValueGeneratedOnAdd();
    }
}