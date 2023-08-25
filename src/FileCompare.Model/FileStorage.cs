namespace FileCompare.Model;

public class FileStorage
{
    public int Id { get; set; }

    public required string Host { get; set; }

    public virtual ICollection<FileHash> Hashes { get; set; } = new HashSet<FileHash>();

    public required FileCatalog Catalog { get; set; }

    public int CatalogId { get; set; }
}