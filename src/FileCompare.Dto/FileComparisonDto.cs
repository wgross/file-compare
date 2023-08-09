namespace FileCompare.Dto;

public record FileHashDto(string Host, string Hash, DateTime Updated);
public record FileComparisonDto(string Name, string FullName, FileHashDto[] Hashes);