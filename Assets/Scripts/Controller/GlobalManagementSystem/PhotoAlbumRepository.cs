using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class PhotoAlbumEntry
{
    public string id;
    public string fileName;
    public string savedAtUtc;
    public string sceneName;
    public string stageId;
    public int width;
    public int height;
}

[Serializable]
public class PhotoAlbumSaveData
{
    public int version;
    public PhotoAlbumEntry[] entries;
}

public static class PhotoAlbumRepository
{
    private const int CurrentVersion = 1;
    private const string AlbumDirectoryName = "PhotoAlbum";
    private const string IndexFileName = "album_index.json";

#if UNITY_INCLUDE_TESTS
    private static string albumDirectoryPathOverride;
#endif

    public static string AlbumDirectoryPath
    {
        get
        {
#if UNITY_INCLUDE_TESTS
            if (!string.IsNullOrWhiteSpace(albumDirectoryPathOverride))
            {
                return albumDirectoryPathOverride;
            }
#endif

            return Path.Combine(Application.persistentDataPath, AlbumDirectoryName);
        }
    }

    public static string IndexPath => Path.Combine(AlbumDirectoryPath, IndexFileName);

#if UNITY_INCLUDE_TESTS
    public static IDisposable UseAlbumDirectoryForTests(string albumDirectoryPath)
    {
        string previousPath = albumDirectoryPathOverride;
        albumDirectoryPathOverride = albumDirectoryPath;
        return new AlbumDirectoryOverrideScope(previousPath);
    }
#endif

    public static bool HasEntries()
    {
        try
        {
            if (!Directory.Exists(AlbumDirectoryPath))
            {
                return false;
            }

            if (Directory.GetFiles(AlbumDirectoryPath, "*.png").Length > 0)
            {
                return true;
            }

            PhotoAlbumSaveData saveData = ReadSaveData();
            return saveData != null && saveData.entries != null && saveData.entries.Length > 0;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"检查相册数据失败：{exception.Message}");
            return false;
        }
    }

    public static IReadOnlyList<PhotoAlbumEntry> LoadEntries()
    {
        EnsureAlbumDirectory();

        PhotoAlbumSaveData saveData = ReadSaveData();
        List<PhotoAlbumEntry> entries = new List<PhotoAlbumEntry>();
        bool hasIndexChanges = saveData == null || saveData.version != CurrentVersion;

        if (saveData != null && saveData.entries != null)
        {
            for (int i = 0; i < saveData.entries.Length; i++)
            {
                PhotoAlbumEntry entry = saveData.entries[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.fileName))
                {
                    hasIndexChanges = true;
                    continue;
                }

                string photoPath = GetPhotoPath(entry);
                if (!File.Exists(photoPath))
                {
                    hasIndexChanges = true;
                    continue;
                }

                if (string.IsNullOrWhiteSpace(entry.id))
                {
                    entry.id = Guid.NewGuid().ToString("N");
                    hasIndexChanges = true;
                }

                if (string.IsNullOrWhiteSpace(entry.savedAtUtc))
                {
                    entry.savedAtUtc = File.GetLastWriteTimeUtc(photoPath).ToString("O");
                    hasIndexChanges = true;
                }

                entry.sceneName = string.IsNullOrWhiteSpace(entry.sceneName) ? "UnknownScene" : entry.sceneName;
                entry.stageId = entry.stageId ?? string.Empty;
                entry.width = Mathf.Max(0, entry.width);
                entry.height = Mathf.Max(0, entry.height);
                entries.Add(entry);
            }
        }

        SortEntries(entries);

        if (hasIndexChanges)
        {
            WriteSaveData(entries);
        }

        return entries;
    }

    public static PhotoAlbumEntry SaveCapture(
        byte[] pngBytes,
        int width,
        int height,
        string sceneName,
        string stageId)
    {
        if (pngBytes == null || pngBytes.Length == 0)
        {
            return null;
        }

        EnsureAlbumDirectory();

        try
        {
            DateTime utcNow = DateTime.UtcNow;
            string fileName = BuildUniqueFileName(utcNow);
            string photoPath = Path.Combine(AlbumDirectoryPath, fileName);
            File.WriteAllBytes(photoPath, pngBytes);

            PhotoAlbumEntry entry = new PhotoAlbumEntry
            {
                id = Guid.NewGuid().ToString("N"),
                fileName = fileName,
                savedAtUtc = utcNow.ToString("O"),
                sceneName = string.IsNullOrWhiteSpace(sceneName) ? "UnknownScene" : sceneName,
                stageId = string.IsNullOrWhiteSpace(stageId) ? string.Empty : stageId,
                width = Mathf.Max(0, width),
                height = Mathf.Max(0, height)
            };

            List<PhotoAlbumEntry> entries = new List<PhotoAlbumEntry>(LoadEntries())
            {
                entry
            };
            SortEntries(entries);
            WriteSaveData(entries);
            return entry;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"保存留念截图失败：{exception.Message}");
            return null;
        }
    }

    public static bool DeleteEntry(PhotoAlbumEntry entry)
    {
        if (entry == null ||
            (string.IsNullOrWhiteSpace(entry.id) && string.IsNullOrWhiteSpace(entry.fileName)))
        {
            return false;
        }

        try
        {
            List<PhotoAlbumEntry> existingEntries = new List<PhotoAlbumEntry>(LoadEntries());
            bool removed = false;

            for (int i = existingEntries.Count - 1; i >= 0; i--)
            {
                PhotoAlbumEntry existingEntry = existingEntries[i];
                if (!IsSameEntry(existingEntry, entry))
                {
                    continue;
                }

                DeletePhotoFile(existingEntry);
                existingEntries.RemoveAt(i);
                removed = true;
            }

            if (!removed)
            {
                return false;
            }

            WriteSaveData(existingEntries);
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"删除留念截图失败：{exception.Message}");
            return false;
        }
    }

    public static Texture2D LoadTexture(PhotoAlbumEntry entry)
    {
        if (entry == null || string.IsNullOrWhiteSpace(entry.fileName))
        {
            return null;
        }

        string photoPath = GetPhotoPath(entry);
        if (!File.Exists(photoPath))
        {
            return null;
        }

        try
        {
            byte[] imageBytes = File.ReadAllBytes(photoPath);
            if (imageBytes == null || imageBytes.Length == 0)
            {
                return null;
            }

            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(imageBytes, false))
            {
                UnityEngine.Object.Destroy(texture);
                return null;
            }

            texture.name = entry.fileName;
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            return texture;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"读取留念截图失败：{exception.Message}");
            return null;
        }
    }

    public static string GetPhotoPath(PhotoAlbumEntry entry)
    {
        return entry == null
            ? string.Empty
            : Path.Combine(AlbumDirectoryPath, entry.fileName ?? string.Empty);
    }

    public static void ClearAll()
    {
        try
        {
            if (!Directory.Exists(AlbumDirectoryPath))
            {
                return;
            }

            Directory.Delete(AlbumDirectoryPath, true);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"清空相册失败：{exception.Message}");
        }
    }

    private static bool IsSameEntry(PhotoAlbumEntry left, PhotoAlbumEntry right)
    {
        if (left == null || right == null)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(left.id) &&
            !string.IsNullOrWhiteSpace(right.id) &&
            string.Equals(left.id, right.id, StringComparison.Ordinal))
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(left.fileName) &&
               !string.IsNullOrWhiteSpace(right.fileName) &&
               string.Equals(left.fileName, right.fileName, StringComparison.Ordinal);
    }

    private static void DeletePhotoFile(PhotoAlbumEntry entry)
    {
        string photoPath = GetPhotoPath(entry);
        if (!File.Exists(photoPath))
        {
            return;
        }

        File.Delete(photoPath);
    }

    private static void EnsureAlbumDirectory()
    {
        Directory.CreateDirectory(AlbumDirectoryPath);
    }

    private static PhotoAlbumSaveData ReadSaveData()
    {
        try
        {
            if (!File.Exists(IndexPath))
            {
                return null;
            }

            string rawJson = File.ReadAllText(IndexPath);
            if (string.IsNullOrWhiteSpace(rawJson))
            {
                return null;
            }

            return JsonUtility.FromJson<PhotoAlbumSaveData>(rawJson);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"读取相册索引失败：{exception.Message}");
            return null;
        }
    }

    private static void WriteSaveData(List<PhotoAlbumEntry> entries)
    {
        try
        {
            EnsureAlbumDirectory();
            PhotoAlbumSaveData saveData = new PhotoAlbumSaveData
            {
                version = CurrentVersion,
                entries = entries != null ? entries.ToArray() : Array.Empty<PhotoAlbumEntry>()
            };
            File.WriteAllText(IndexPath, JsonUtility.ToJson(saveData, true));
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"写入相册索引失败：{exception.Message}");
        }
    }

    private static string BuildUniqueFileName(DateTime utcNow)
    {
        string timestamp = utcNow.ToString("yyyyMMdd_HHmmss_fff");
        string baseName = $"photo_{timestamp}";

        for (int attempt = 0; attempt < 1000; attempt++)
        {
            string suffix = attempt == 0 ? string.Empty : $"_{attempt}";
            string fileName = $"{baseName}{suffix}.png";
            if (!File.Exists(Path.Combine(AlbumDirectoryPath, fileName)))
            {
                return fileName;
            }
        }

        return $"photo_{timestamp}_{Guid.NewGuid():N}.png";
    }

    private static void SortEntries(List<PhotoAlbumEntry> entries)
    {
        if (entries == null)
        {
            return;
        }

        entries.Sort((left, right) =>
            string.Compare(
                right != null ? right.savedAtUtc : string.Empty,
                left != null ? left.savedAtUtc : string.Empty,
                StringComparison.Ordinal));
    }

#if UNITY_INCLUDE_TESTS
    private sealed class AlbumDirectoryOverrideScope : IDisposable
    {
        private readonly string previousPath;
        private bool disposed;

        public AlbumDirectoryOverrideScope(string previousPath)
        {
            this.previousPath = previousPath;
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            albumDirectoryPathOverride = previousPath;
            disposed = true;
        }
    }
#endif
}
