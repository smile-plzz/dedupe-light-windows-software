namespace DedupeLight;

public enum RuleKind
{
    Path,
    Extension,
    FolderName
}

public sealed record ExclusionRule(string Path, bool IsFolder = false, RuleKind Kind = RuleKind.Path, bool Enabled = true)
{
    public override string ToString() => Kind switch
    {
        RuleKind.Extension => $"Pattern  ·  {Path}",
        RuleKind.FolderName => $"Folder Name  ·  {Path}",
        _ => $"{(IsFolder ? "Folder" : "File")}  ·  {Path}"
    };
}

public static class ExclusionRules
{
    public static readonly IReadOnlyList<ExclusionRule> CommonPresets = new[]
    {
        new ExclusionRule("node_modules", true, RuleKind.FolderName),
        new ExclusionRule(".git", true, RuleKind.FolderName),
        new ExclusionRule(".vs", true, RuleKind.FolderName),
        new ExclusionRule("bin", true, RuleKind.FolderName),
        new ExclusionRule("obj", true, RuleKind.FolderName),
        new ExclusionRule("__pycache__", true, RuleKind.FolderName),
        new ExclusionRule(".gradle", true, RuleKind.FolderName),
        new ExclusionRule("*.tmp", false, RuleKind.Extension),
        new ExclusionRule("*.bak", false, RuleKind.Extension),
        new ExclusionRule("*.log", false, RuleKind.Extension),
        new ExclusionRule("*.cache", false, RuleKind.Extension),
        new ExclusionRule("Thumbs.db", false, RuleKind.Extension),
        new ExclusionRule(".DS_Store", false, RuleKind.Extension),
        new ExclusionRule("$Recycle.Bin", true, RuleKind.FolderName),
        new ExclusionRule("System Volume Information", true, RuleKind.FolderName)
    };

    public static string Normalize(string path, RuleKind kind = RuleKind.Path)
    {
        if (kind == RuleKind.Extension)
        {
            var clean = path.Trim();
            if (clean.StartsWith("*.")) clean = clean[1..];
            else if (!clean.StartsWith('.') && !clean.Contains('.')) clean = "." + clean;
            return clean.ToLowerInvariant();
        }
        if (kind == RuleKind.FolderName)
        {
            return path.Trim().Trim(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);
        }
        if (!System.IO.Path.IsPathFullyQualified(path)) throw new ArgumentException("Use an absolute file or folder path.");
        return System.IO.Path.TrimEndingDirectorySeparator(System.IO.Path.GetFullPath(path));
    }

    public static bool Matches(string path, IEnumerable<ExclusionRule>? rules)
    {
        if (rules is null) return false;
        string? candidate = null;
        string[]? segments = null;
        string? ext = null;

        foreach (var rule in rules)
        {
            if (!rule.Enabled) continue;

            if (rule.Kind == RuleKind.Extension)
            {
                ext ??= System.IO.Path.GetExtension(path);
                var ruleExt = rule.Path.TrimStart('*');
                if (!ruleExt.StartsWith('.') && !ruleExt.Contains('.')) ruleExt = "." + ruleExt;

                if (!string.IsNullOrEmpty(ext) && ext.Equals(ruleExt, StringComparison.OrdinalIgnoreCase)) return true;
                if (System.IO.Path.GetFileName(path).Equals(rule.Path, StringComparison.OrdinalIgnoreCase)) return true;
            }
            else if (rule.Kind == RuleKind.FolderName)
            {
                segments ??= path.Split([System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries);
                if (segments.Any(s => s.Equals(rule.Path, StringComparison.OrdinalIgnoreCase))) return true;
            }
            else
            {
                try
                {
                    candidate ??= Normalize(path, RuleKind.Path);
                    var excluded = Normalize(rule.Path, RuleKind.Path);
                    if (candidate.Equals(excluded, StringComparison.OrdinalIgnoreCase)) return true;
                    if (!rule.IsFolder) continue;
                    var prefix = System.IO.Path.EndsInDirectorySeparator(excluded) ? excluded : excluded + System.IO.Path.DirectorySeparatorChar;
                    if (candidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return true;
                }
                catch
                {
                }
            }
        }
        return false;
    }

    public static List<ExclusionRule> Validate(IEnumerable<ExclusionRule> rules) =>
        rules.Select(r => new ExclusionRule(Normalize(r.Path, r.Kind), r.IsFolder, r.Kind, r.Enabled))
            .DistinctBy(r => (r.Path.ToUpperInvariant(), r.Kind, r.IsFolder)).ToList();
}

public static class RecyclePolicy
{
    public static List<int> FullySelectedGroups(IEnumerable<DuplicateItem> items, IEnumerable<DuplicateItem> chosen)
    {
        var paths = chosen.Select(x => x.Path).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return items.GroupBy(x => x.Group).Where(g => g.All(x => paths.Contains(x.Path))).Select(g => g.Key).ToList();
    }
}
