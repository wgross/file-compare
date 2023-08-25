namespace FileCompare.Model;

public class FileHash
{
    public required string Hash { get; set; }

    public DateTime Updated { get; set; }

    public required File File { get; set; }

    public int FileId { get; set; }

    public required FileStorage Storage { get; set; }

    public int StorageId { get; set; }

    public long Length { get; set; }

    public DateTime CreationTimeUtc { get; set; }

    public DateTime LastAccessTimeUtc { get; set; }

    public DateTime LastWriteTimeUtc { get; set; }
}