using System.Text;
using System.Text.Json;
using BingWallpaper.Models;

namespace BingWallpaper.Services;

/// <summary>
/// 文件操作服务 - 按地区读写壁纸数据和生成文档
/// </summary>
public class FileService
{
    private readonly string _rootDir;
    private readonly string _region;
    private readonly string _prefix;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="rootDir">仓库根目录</param>
    /// <param name="region">地区代码，如 "en-US"</param>
    public FileService(string rootDir, string region)
    {
        _rootDir = rootDir;
        _region = region;

        // en-US → 根目录, zh-CN → zh-cn/
        _prefix = region.Equals("en-US", StringComparison.OrdinalIgnoreCase) ? "" : "zh-cn/";

        Console.WriteLine($"[INFO] FileService initialized: region={region}, prefix='{_prefix}'");
    }

    private string GetBingPath() => Path.Combine(_rootDir, _prefix, "bing-wallpaper.md");
    private string GetReadmePath() => Path.Combine(_rootDir, _prefix, "README.md");
    private string GetPictureDir() => Path.Combine(_rootDir, _prefix, "picture");
    private string GetDocsDir() => Path.Combine(_rootDir, _prefix, "docs");

    /// <summary>
    /// 读取现有的壁纸数据
    /// </summary>
    public async Task<List<BingImage>> ReadBingImagesAsync()
    {
        var images = new List<BingImage>();
        string bingPath = GetBingPath();

        if (!File.Exists(bingPath))
        {
            return images;
        }

        var lines = await File.ReadAllLinesAsync(bingPath);
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("## Bing"))
                continue;

            // 格式: yyyy-MM-dd | [desc](url)
            int pipeIdx = trimmed.IndexOf(" | ");
            if (pipeIdx < 0) continue;

            string date = trimmed[..pipeIdx].Trim();

            int descStart = trimmed.IndexOf("[", pipeIdx);
            int descEnd = trimmed.IndexOf("]", descStart);
            if (descStart < 0 || descEnd < 0) continue;

            string desc = trimmed[(descStart + 1)..descEnd];

            int urlStart = trimmed.LastIndexOf("(");
            int urlEnd = trimmed.LastIndexOf(")");
            if (urlStart < 0 || urlEnd < 0) continue;

            string url = trimmed[(urlStart + 1)..urlEnd];

            images.Add(new BingImage(desc, date, url));
        }

        Console.WriteLine($"[INFO] Read {images.Count} images from {bingPath}");
        return images;
    }

    /// <summary>
    /// 写入壁纸数据到 bing-wallpaper.md
    /// </summary>
    public async Task WriteBingImagesAsync(List<BingImage> images)
    {
        string bingPath = GetBingPath();
        Directory.CreateDirectory(Path.GetDirectoryName(bingPath)!);

        var sb = new StringBuilder();
        sb.AppendLine("## Bing Wallpaper");
        sb.AppendLine();

        foreach (var img in images)
        {
            sb.AppendLine(img.FormatMarkdown());
            sb.AppendLine();
        }

        await File.WriteAllTextAsync(bingPath, sb.ToString());
        Console.WriteLine($"[INFO] Written {images.Count} images to {bingPath}");
    }

    /// <summary>
    /// 写入 README.md
    /// </summary>
    public async Task WriteReadmeAsync(List<BingImage> images)
    {
        string readmePath = GetReadmePath();
        Directory.CreateDirectory(Path.GetDirectoryName(readmePath)!);

        var sb = new StringBuilder();
        sb.AppendLine("# Bing Wallpaper");
        sb.AppendLine();
        sb.AppendLine("> 必应每日超清壁纸（4K） Bing Daily Wallpaper (4K)");
        sb.AppendLine();
        sb.AppendLine("自动获取必应每日壁纸，每天更新。");
        sb.AppendLine();
        sb.AppendLine("## 最近壁纸");
        sb.AppendLine();

        // 只显示最近 10 张
        int count = Math.Min(images.Count, 10);
        for (int i = 0; i < count; i++)
        {
            sb.AppendLine(images[i].ToString());
            sb.AppendLine();
        }

        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine($"*总共 {images.Count} 张壁纸，每日自动更新*");

        await File.WriteAllTextAsync(readmePath, sb.ToString());
        Console.WriteLine($"[INFO] Written README.md to {readmePath}");
    }

    /// <summary>
    /// 写入月份信息文件
    /// </summary>
    public async Task WriteMonthInfoAsync(List<BingImage> images)
    {
        string pictureDir = GetPictureDir();
        Directory.CreateDirectory(pictureDir);

        var monthGroups = images
            .GroupBy(img => img.Date[..7])
            .OrderByDescending(g => g.Key);

        foreach (var group in monthGroups)
        {
            string month = group.Key;
            string monthFile = Path.Combine(pictureDir, $"{month}.md");

            var sb = new StringBuilder();
            sb.AppendLine($"# {month} Bing Wallpaper");
            sb.AppendLine();

            foreach (var img in group.OrderByDescending(i => i.Date))
            {
                sb.AppendLine(img.ToString());
                sb.AppendLine();
            }

            await File.WriteAllTextAsync(monthFile, sb.ToString());
        }

        Console.WriteLine($"[INFO] Written {monthGroups.Count()} month files to {pictureDir}");
    }

    /// <summary>
    /// 生成 JSON 数据文件
    /// </summary>
    public async Task WriteImagesJsonAsync(List<BingImage> images)
    {
        string docsDir = GetDocsDir();
        Directory.CreateDirectory(docsDir);
        string jsonPath = Path.Combine(docsDir, "images.json");

        var jsonImages = images.Select(img => new
        {
            date = img.Date,
            desc = img.Desc,
            url = img.Url
        });

        string json = JsonSerializer.Serialize(jsonImages,
            new JsonSerializerOptions { WriteIndented = true });

        await File.WriteAllTextAsync(jsonPath, json);
        Console.WriteLine($"[INFO] Written images.json to {jsonPath}");
    }
}