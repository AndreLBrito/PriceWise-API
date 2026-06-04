namespace PriceWise.Application.Exports.Dtos;

public sealed record CsvExportResponse(
    string FileName,
    string ContentType,
    string Content);
