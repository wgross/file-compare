namespace FileCompare.Model;

public class File
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public required string FullName { get; set; }

    public virtual ICollection<FileHash> Hashes { get; set; } = new HashSet<FileHash>();

    public required FileCatalog Catalog { get; set; }

    public int CatalogId { get; set; }
}