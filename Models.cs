namespace DedupeLight;

public enum FileCategory
{
    All,
    Images,
    Videos,
    Audio,
    Documents,
    Code,
    Archives,
    Other
}

public static class FileCategoryHelper
{
    static readonly HashSet<string> ImageExts = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".tiff", ".tif", ".svg", ".ico", ".heic", ".raw"
    };

    static readonly HashSet<string> VideoExts = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".flv", ".webm", ".m4v", ".3gp", ".ts"
    };

    static readonly HashSet<string> AudioExts = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp3", ".wav", ".flac", ".aac", ".ogg", ".m4a", ".wma", ".opus", ".aiff"
    };

    static readonly HashSet<string> DocExts = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".rtf", ".odt", ".csv", ".epub"
    };

    static readonly HashSet<string> CodeExts = new(StringComparer.OrdinalIgnoreCase)
    {
        ".cs", ".js", ".ts", ".jsx", ".tsx", ".html", ".css", ".scss", ".json", ".xml", ".py", ".cpp",
        ".c", ".h", ".hpp", ".java", ".go", ".rs", ".php", ".sql", ".sh", ".ps1", ".yaml", ".yml", ".md", ".toml"
    };

    static readonly HashSet<string> ArchiveExts = new(StringComparer.OrdinalIgnoreCase)
    {
        ".zip", ".rar", ".7z", ".tar", ".gz", ".bz2", ".xz", ".iso", ".tgz"
    };

    public static FileCategory GetCategory(string path)
    {
        var ext = System.IO.Path.GetExtension(path);
        if (string.IsNullOrEmpty(ext)) return FileCategory.Other;
        if (ImageExts.Contains(ext)) return FileCategory.Images;
        if (VideoExts.Contains(ext)) return FileCategory.Videos;
        if (AudioExts.Contains(ext)) return FileCategory.Audio;
        if (DocExts.Contains(ext)) return FileCategory.Documents;
        if (CodeExts.Contains(ext)) return FileCategory.Code;
        if (ArchiveExts.Contains(ext)) return FileCategory.Archives;
        return FileCategory.Other;
    }
}

public sealed class DuplicateItem
{
    public bool Selected { get; set; }
    public int Group { get; init; }
    public required string Path { get; init; }
    public required string Hash { get; init; }
    public long Size { get; init; }
    public DateTime ModifiedUtc { get; init; }
    public string Name => System.IO.Path.GetFileName(Path);
    public string Folder => System.IO.Path.GetDirectoryName(Path) ?? "";
    public string Extension => System.IO.Path.GetExtension(Path).ToLowerInvariant();
    public string SizeText => MainForm.FormatSize(Size);
    public string ModifiedText => ModifiedUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
    public FileCategory Category => FileCategoryHelper.GetCategory(Path);
}

public sealed record ScanOptions(long MinimumBytes, bool IncludeHiddenSystem, IReadOnlyList<ExclusionRule>? Exclusions = null);
public sealed record ScanResult(List<DuplicateItem> Items, int FilesSeen, int SizeCandidates, int FullHashes, int CacheHits, int Skipped);
