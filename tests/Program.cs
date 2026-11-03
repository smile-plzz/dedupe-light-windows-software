using DedupeLight;
using System.Collections.Concurrent;

var root = Path.Combine(Path.GetTempPath(), "dedupe-checks-" + Guid.NewGuid());
Directory.CreateDirectory(root);
try
{
    var data = Enumerable.Repeat((byte)7, 200000).ToArray();
    File.WriteAllBytes(Path.Combine(root, "a.bin"), data);
    File.WriteAllBytes(Path.Combine(root, "b.bin"), data);
    var cache = Path.Combine(root, "cache.json");
    var reports = new Reports();
    var options = new ScanOptions(0, true);
    var first = await DuplicateScanner.FindAsync(root, cache, options, reports, CancellationToken.None);
    Check(first.Items.Count == 2 && first.FullHashes == 2, "exact duplicates verified");
    var second = await DuplicateScanner.FindAsync(root, cache, options, reports, CancellationToken.None);
    Check(second.Items.Count == 2 && second.CacheHits == 2, "warm scan reuses hashes");
    // Change both samples identically but only one middle block: stale full hashes must not survive.
    data[0] = 8; File.WriteAllBytes(Path.Combine(root, "a.bin"), data);
    data[100000] = 9; File.WriteAllBytes(Path.Combine(root, "b.bin"), data);
    File.SetLastWriteTimeUtc(Path.Combine(root, "a.bin"), DateTime.UtcNow.AddMinutes(1));
    File.SetLastWriteTimeUtc(Path.Combine(root, "b.bin"), DateTime.UtcNow.AddMinutes(1));
    var changed = await DuplicateScanner.FindAsync(root, cache, options, reports, CancellationToken.None);
    Check(changed.Items.Count == 0 && changed.FullHashes == 2, "changed files invalidate full hashes");
    File.WriteAllText(cache, "invalid JSON");
    Check((await DuplicateScanner.FindAsync(root, cache, options, reports, CancellationToken.None)).Items.Count == 0, "corrupt cache recovers");

    var fileRules = new[] { new ExclusionRule(Path.Combine(root, "a.bin"), false) };
    Check(ExclusionRules.Matches(Path.Combine(root, "a.bin"), fileRules), "exact file excluded");
    Check(!ExclusionRules.Matches(Path.Combine(root, "b.bin"), fileRules), "other copy remains included");
    var folder = Path.Combine(root, "protected"); Directory.CreateDirectory(folder);
    File.WriteAllBytes(Path.Combine(root, "b.bin"), data);
    File.WriteAllBytes(Path.Combine(folder, "copy.bin"), data);
    var folderRules = new[] { new ExclusionRule(folder, true) };
    Check(ExclusionRules.Matches(Path.Combine(folder, "nested", "copy.bin"), folderRules), "nested folder exclusion");
    Check(!ExclusionRules.Matches(folder + "-other" + Path.DirectorySeparatorChar + "copy.bin", folderRules), "folder prefix boundary");
    var excluded = await DuplicateScanner.FindAsync(root, cache, new ScanOptions(0, true, folderRules), reports, CancellationToken.None);
    Check(excluded.Items.Count == 0, "excluded folder skipped before duplicate detection");
    var restored = await DuplicateScanner.FindAsync(root, cache, options, reports, CancellationToken.None);
    Check(restored.Items.Count == 2, "removing exclusion restores duplicates on next scan");

    // Pattern and extension exclusions
    var extRules = new[] { new ExclusionRule("*.tmp", false, RuleKind.Extension, true) };
    Check(ExclusionRules.Matches(Path.Combine(root, "temp.tmp"), extRules), "extension pattern excluded");
    Check(!ExclusionRules.Matches(Path.Combine(root, "temp.bin"), extRules), "other extension included");

    // Disabled rule behavior
    var disabledRules = new[] { new ExclusionRule("*.tmp", false, RuleKind.Extension, false) };
    Check(!ExclusionRules.Matches(Path.Combine(root, "temp.tmp"), disabledRules), "disabled rule ignored");

    // Folder name wildcard exclusions
    var folderWildcards = new[] { new ExclusionRule("node_modules", true, RuleKind.FolderName, true) };
    Check(ExclusionRules.Matches(Path.Combine(root, "deep", "nested", "node_modules", "package.json"), folderWildcards), "folder name wildcard matches nested path");
    Check(!ExclusionRules.Matches(Path.Combine(root, "deep", "nested", "src", "package.json"), folderWildcards), "folder name wildcard preserves non-matching directories");

    // File categories
    Check(FileCategoryHelper.GetCategory("photo.jpg") == FileCategory.Images, "category images");
    Check(FileCategoryHelper.GetCategory("clip.mp4") == FileCategory.Videos, "category videos");
    Check(FileCategoryHelper.GetCategory("track.mp3") == FileCategory.Audio, "category audio");
    Check(FileCategoryHelper.GetCategory("sheet.csv") == FileCategory.Documents, "category documents");
    Check(FileCategoryHelper.GetCategory("code.cs") == FileCategory.Code, "category code");
    Check(FileCategoryHelper.GetCategory("bundle.zip") == FileCategory.Archives, "category archives");
    Check(ExclusionRules.CommonPresets.Count >= 10, "common presets populated");

    var saved = new AppSettings(Exclusions: folderRules.ToList()); var settingsPath = Path.Combine(root, "settings.json");
    saved.Save(settingsPath); Check(AppSettings.Load(settingsPath).Exclusions?.Count == 1, "exclusions persist");
    var legacyPath = Path.Combine(root, "legacy.json"); File.WriteAllText(legacyPath, "{\"LastPath\":\"\",\"KeepRule\":0}");
    Check(AppSettings.Load(legacyPath).Exclusions is null, "legacy settings migrate without exclusions");
    var pair = restored.Items;
    Check(RecyclePolicy.FullySelectedGroups(pair, [pair[0]]).Count == 0, "keeping a copy needs no override");
    Check(RecyclePolicy.FullySelectedGroups(pair, pair).Count == 1, "all listed copies require override");
    Check(RecyclePolicy.FullySelectedGroups(pair, [pair[0], pair[0]]).Count == 0, "duplicate selection cannot bypass copy check");
    using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
    try { await DuplicateScanner.FindAsync(root, cache, options, reports, cancelled.Token); throw new Exception("Cancellation failed"); }
    catch (OperationCanceledException) { Console.WriteLine("PASS cancellation"); }
    // A context that fails on capture exposes accidental work scheduled back to a UI context.
    SynchronizationContext.SetSynchronizationContext(new RejectContext());
    var task = DuplicateScanner.FindAsync(root, cache, options, reports, CancellationToken.None);
    SynchronizationContext.SetSynchronizationContext(null);
    await task;
    Console.WriteLine("PASS scanner independent of caller synchronization context");
}
finally { Directory.Delete(root, true); }
static void Check(bool condition, string name) { if (!condition) throw new Exception(name); Console.WriteLine("PASS " + name); }
sealed class Reports : IProgress<string> { public void Report(string message) { } }
sealed class RejectContext : SynchronizationContext { public override void Post(SendOrPostCallback d, object? state) => throw new Exception("Captured UI context"); }
namespace DedupeLight { public static class MainForm { public static string FormatSize(long size) => size.ToString(); } }
