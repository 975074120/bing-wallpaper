using System.Text;
using BingWallpaper.Models;

namespace BingWallpaper.Services;

/// <summary>
/// 文件操作服务 - 读写壁纸数据和生成文档
/// </summary>
public class FileService
{
    private readonly string _bingPath;
    private readonly string _readmePath;
    private readonly string _pictureDir;
    private readonly string _docsDir;

    public FileService(string rootDir)
    {
        _bingPath = Path.Combine(rootDir, "bing-wallpaper.md");
        _readmePath = Path.Combine(rootDir, "README.md");
        _pictureDir = Path.Combine(rootDir, "picture");
        _docsDir = Path.Combine(rootDir, "docs");
    }

    /// <summary>
    /// 读取现有的壁纸数据
    /// </summary>
    public async Task<List<BingImage>> ReadBingImagesAsync()
    {
        var images = new List<BingImage>();

        if (!File.Exists(_bingPath))
        {
            return images;
        }

        var lines = await File.ReadAllLinesAsync(_bingPath);
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

        Console.WriteLine($"[INFO] Read {images.Count} images from {_bingPath}");
        return images;
    }

    /// <summary>
    /// 写入壁纸数据到 bing-wallpaper.md
    /// </summary>
    public async Task WriteBingImagesAsync(List<BingImage> images)
    {
        var sb = new StringBuilder();
        sb.AppendLine("## Bing Wallpaper");
        sb.AppendLine();

        foreach (var img in images)
        {
            sb.AppendLine(img.FormatMarkdown());
            sb.AppendLine();
        }

        await File.WriteAllTextAsync(_bingPath, sb.ToString());
        Console.WriteLine($"[INFO] Written {images.Count} images to {_bingPath}");
    }

    /// <summary>
    /// 写入 README.md
    /// </summary>
    public async Task WriteReadmeAsync(List<BingImage> images)
    {
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

        await File.WriteAllTextAsync(_readmePath, sb.ToString());
        Console.WriteLine($"[INFO] Written README.md");
    }

    /// <summary>
    /// 写入月份信息文件
    /// </summary>
    public async Task WriteMonthInfoAsync(List<BingImage> images)
    {
        var monthGroups = images
            .GroupBy(img => img.Date[..7])
            .OrderByDescending(g => g.Key);

        foreach (var group in monthGroups)
        {
            string month = group.Key;
            string monthFile = Path.Combine(_pictureDir, $"{month}.md");

            Directory.CreateDirectory(_pictureDir);

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
    }

    /// <summary>
    /// 生成 JSON 数据文件
    /// </summary>
    public async Task WriteImagesJsonAsync(List<BingImage> images)
    {
        Directory.CreateDirectory(_docsDir);
        string jsonPath = Path.Combine(_docsDir, "images.json");

        var jsonImages = images.Select(img => new
        {
            date = img.Date,
            desc = img.Desc,
            url = img.Url
        });

        string json = System.Text.Json.JsonSerializer.Serialize(jsonImages,
            new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

        await File.WriteAllTextAsync(jsonPath, json);
    }
}