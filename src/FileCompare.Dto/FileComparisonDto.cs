namespace FileCompare.Dto;

public record FileHashDto(
    string Host,
    string Hash,
    DateTime Updated,
    long Length,
    DateTime CreationTimeUtc,
    DateTime LastAccessTimeUtc,
    DateTime LastWriteTimeUtc);

public record FileComparisonDto(string Name, string FullName, FileHashDto[] Hashes);