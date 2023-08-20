namespace FileCompare.Dto;

public record FileResponseDto(
    int Id,
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