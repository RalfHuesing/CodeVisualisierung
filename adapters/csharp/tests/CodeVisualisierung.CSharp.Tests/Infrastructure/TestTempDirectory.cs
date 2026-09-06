using System.Collections.Concurrent;
using System.Text;

namespace CodeVisualisierung.CSharp.Tests.Infrastructure;

/// <summary>
/// Verwaltet isolierte Testverzeichnisse unter dem Repo-Root <c>temp/</c>.
/// </summary>
public sealed class TestTempDirectory : IDisposable
{
    private const string DefaultPrefix = "csharp-test-";
    private const string RepositoryTempFolderName = "temp";
    private const string OwnerMarkerPrefix = ".csharp-test-owner-";
    private static readonly TimeSpan StaleDirectoryAge = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan[] DeleteRetryDelays =
    [
        TimeSpan.Zero,
        TimeSpan.FromMilliseconds(25),
        TimeSpan.FromMilliseconds(50),
        TimeSpan.FromMilliseconds(100)
    ];
    private static readonly ConcurrentDictionary<string, FileStream> ActiveDirectories = new(StringComparer.OrdinalIgnoreCase);
    private readonly string directoryPath;
    private int disposed;

    static TestTempDirectory()
    {
        AppDomain.CurrentDomain.ProcessExit += (_, _) => CleanupActiveDirectories();
    }

    private TestTempDirectory(string directoryPath)
    {
        this.directoryPath = directoryPath;
    }

    /// <summary>
    /// Absoluter Pfad zum temporären Testverzeichnis.
    /// </summary>
    public string DirectoryPath => directoryPath;

    /// <summary>
    /// Absoluter Pfad zum repo-lokalen Temp-Root.
    /// </summary>
    public static string RootTempDirectory => Path.Combine(FindRepositoryRoot(), RepositoryTempFolderName);

    /// <summary>
    /// Erstellt ein eindeutiges Testverzeichnis unter <c>&lt;RepoRoot&gt;/temp/</c>.
    /// </summary>
    public static TestTempDirectory Create(string prefix = DefaultPrefix)
    {
        prefix = NormalizePrefix(prefix);

        var root = RootTempDirectory;
        Directory.CreateDirectory(root);
        CleanupStaleDirectories(root);

        var path = Path.Combine(root, $"{prefix}{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);

        try
        {
            var marker = CreateOwnerMarker(path);
            if (!ActiveDirectories.TryAdd(path, marker))
            {
                marker.Dispose();
                throw new IOException($"Temporäres Testverzeichnis wurde doppelt registriert: {path}");
            }

            return new TestTempDirectory(path);
        }
        catch
        {
            DeleteOwnerMarker(path);
            TryDeleteDirectory(path);
            throw;
        }
    }

    /// <summary>
    /// Erstellt eine Datei innerhalb des verwalteten Testverzeichnisses.
    /// </summary>
    public string CreateFile(string relativePath, string content = "")
    {
        var path = GetPath(relativePath);
        var parent = Path.GetDirectoryName(path);
        if (parent is not null)
        {
            Directory.CreateDirectory(parent);
        }

        File.WriteAllText(path, content);
        return path;
    }

    /// <summary>
    /// Erstellt einen Unterordner innerhalb des verwalteten Testverzeichnisses.
    /// </summary>
    public string CreateSubdirectory(string relativePath)
    {
        var path = GetPath(relativePath);
        Directory.CreateDirectory(path);
        return path;
    }

    /// <summary>
    /// Liefert einen geprüften absoluten Pfad innerhalb des Testverzeichnisses.
    /// </summary>
    public string GetPath(string relativePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);
        if (Path.IsPathRooted(relativePath))
        {
            throw new ArgumentException("Der Temp-Pfad muss relativ sein.", nameof(relativePath));
        }

        var path = Path.GetFullPath(Path.Combine(directoryPath, relativePath));
        var directoryPrefix = directoryPath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!path.StartsWith(directoryPrefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Der Temp-Pfad muss innerhalb des Testverzeichnisses liegen.", nameof(relativePath));
        }

        return path;
    }

    public override string ToString() => directoryPath;

    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, 1) == 1)
        {
            return;
        }

        if (ActiveDirectories.TryRemove(directoryPath, out var marker))
        {
            marker.Dispose();
        }

        TryDeleteDirectory(directoryPath);
        DeleteOwnerMarker(directoryPath);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var solutionPath = Path.Combine(directory.FullName, "adapters", "csharp", "CodeVisualisierung.CSharp.slnx");
            if (File.Exists(solutionPath))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Das Repository mit adapters/csharp/CodeVisualisierung.CSharp.slnx wurde nicht gefunden.");
    }

    private static string NormalizePrefix(string prefix)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);
        if (Path.GetFileName(prefix) != prefix || prefix.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new ArgumentException("Das Temp-Präfix muss ein gültiger Dateiname ohne Unterordner sein.", nameof(prefix));
        }

        return prefix.StartsWith(DefaultPrefix, StringComparison.OrdinalIgnoreCase)
            ? prefix
            : DefaultPrefix + prefix;
    }

    private static FileStream CreateOwnerMarker(string directoryPath)
    {
        var marker = new FileStream(GetOwnerMarkerPath(directoryPath), FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Read);
        var content = Encoding.UTF8.GetBytes($"pid={Environment.ProcessId};createdUtc={DateTime.UtcNow:O}{Environment.NewLine}");
        marker.Write(content, 0, content.Length);
        marker.Flush(flushToDisk: true);
        return marker;
    }

    private static void CleanupStaleDirectories(string root)
    {
        IEnumerable<string> directories;
        try
        {
            directories = Directory.EnumerateDirectories(root).ToArray();
        }
        catch (IOException)
        {
            return;
        }
        catch (UnauthorizedAccessException)
        {
            return;
        }

        foreach (var directory in directories)
        {
            if (ActiveDirectories.ContainsKey(directory) || !LooksLikeTestDirectory(directory))
            {
                continue;
            }

            var markerPath = GetOwnerMarkerPath(directory);
            if (File.Exists(markerPath) && !CanAcquireOwnerMarker(markerPath))
            {
                continue;
            }

            if (File.Exists(markerPath) || Directory.GetLastWriteTimeUtc(directory) < DateTime.UtcNow - StaleDirectoryAge)
            {
                TryDeleteDirectory(directory);
                DeleteOwnerMarker(directory);
            }
        }
    }

    private static bool LooksLikeTestDirectory(string directory)
    {
        var name = Path.GetFileName(directory);
        return name.StartsWith(DefaultPrefix, StringComparison.OrdinalIgnoreCase)
            && name.Length >= DefaultPrefix.Length + 32
            && Guid.TryParseExact(name[^32..], "N", out _);
    }

    private static bool CanAcquireOwnerMarker(string markerPath)
    {
        try
        {
            using var probe = new FileStream(markerPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static string GetOwnerMarkerPath(string directoryPath)
    {
        var parent = Directory.GetParent(directoryPath)?.FullName
            ?? throw new ArgumentException("Das Testverzeichnis muss einen Elternpfad besitzen.", nameof(directoryPath));
        return Path.Combine(parent, OwnerMarkerPrefix + Path.GetFileName(directoryPath));
    }

    private static void DeleteOwnerMarker(string directoryPath)
    {
        var markerPath = GetOwnerMarkerPath(directoryPath);
        try
        {
            if (File.Exists(markerPath))
            {
                File.SetAttributes(markerPath, FileAttributes.Normal);
                File.Delete(markerPath);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static void CleanupActiveDirectories()
    {
        foreach (var pair in ActiveDirectories.ToArray())
        {
            if (ActiveDirectories.TryRemove(pair.Key, out var marker))
            {
                marker.Dispose();
            }

            TryDeleteDirectory(pair.Key);
            DeleteOwnerMarker(pair.Key);
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        for (var attempt = 0; attempt < DeleteRetryDelays.Length; attempt++)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    return;
                }

                NormalizeAttributes(path);
                Directory.Delete(path, recursive: true);
                return;
            }
            catch (IOException) when (attempt < DeleteRetryDelays.Length - 1)
            {
                Thread.Sleep(DeleteRetryDelays[attempt + 1]);
            }
            catch (UnauthorizedAccessException) when (attempt < DeleteRetryDelays.Length - 1)
            {
                Thread.Sleep(DeleteRetryDelays[attempt + 1]);
            }
            catch (IOException)
            {
                return;
            }
            catch (UnauthorizedAccessException)
            {
                return;
            }
        }
    }

    private static void NormalizeAttributes(string path)
    {
        foreach (var entry in Directory.EnumerateFileSystemEntries(path, "*", SearchOption.AllDirectories))
        {
            File.SetAttributes(entry, FileAttributes.Normal);
        }

        File.SetAttributes(path, FileAttributes.Normal);
    }
}
