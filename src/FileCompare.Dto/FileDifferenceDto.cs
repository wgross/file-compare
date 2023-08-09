namespace FileCompare.Dto;

public record FileHashDto(string Host, string Hash, DateTime Updated);
public record FileDifferenceDto(string Name, string FullName, FileHashDto[] Hashes);