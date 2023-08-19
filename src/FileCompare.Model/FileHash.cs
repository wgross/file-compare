namespace FileCompare.Model;

public class FileHash
{
    public string Hash { get; set; }
    public DateTime Updated { get; set; }

    public File File { get; set; }
    public int FileId { get; set; }

    public FileStorage Storage { get; set; }
    public int StorageId { get; set; }

    public long Length { get; set; }

    public DateTime CreationTimeUtc { get; set; }

    public DateTime LastAccessTimeUtc { get; set; }

    public DateTime LastWriteTimeUtc { get; set; }
}