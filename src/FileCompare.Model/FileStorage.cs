namespace FileCompare.Model;

public class FileStorage
{
    public int Id { get; set; }
    public string Host { get; set; }

    public virtual ICollection<FileHash> Hashes { get; set; } = new HashSet<FileHash>();
}