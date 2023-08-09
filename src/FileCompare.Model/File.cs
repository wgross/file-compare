namespace FileCompare.Model;

public class File
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string FullName { get; set; }

    public virtual ICollection<FileHash> Hashes { get; set; } = new HashSet<FileHash>();
}
