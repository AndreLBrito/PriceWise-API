namespace PriceWise.Application.Exports;

public sealed class CsvExportOptions
{
    public const string SectionName = "CsvExport";

    public int MaxRows { get; set; } = 10_000;

    public string DateFormat { get; set; } = "yyyy-MM-dd HH:mm:ss";
}
