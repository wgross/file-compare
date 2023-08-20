namespace FileCompare.Dto;

public record FileRequestDto(
    string Host,
    string Name,
    string FullName,
    string Hash,
    DateTime Updated,
    long Length,
    DateTime CreationTimeUtc,
    DateTime LastAccessTimeUtc,
    DateTime LastWriteTimeUtc
);