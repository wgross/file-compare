namespace FileCompare.Dto;

public record FileDto(
    string Host,
    string Name,
    string FullName,
    string Hash);