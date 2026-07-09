using BingWallpaper.Models;
using BingWallpaper.Services;

// ============================================================
// Bing Wallpaper - 必应每日壁纸抓取工具 (.NET 10 / C#)
// 原项目: https://github.com/niumoo/bing-wallpaper
// 功能: 每日自动抓取 Bing 每日壁纸，生成 Markdown 和 HTML
// ============================================================

// 参数: 可指定工作目录，默认为当前目录
string rootDir = args.Length > 0 ? args[0] : Directory.GetCurrentDirectory();
Console.WriteLine($"[INFO] Bing Wallpaper - Working directory: {rootDir}");

try
{
    // 1. 初始化服务
    var bingApi = new BingApiService();
    var fileService = new FileService(rootDir);

    // 2. 从 Bing API 获取最新壁纸
    Console.WriteLine("[INFO] Fetching Bing wallpapers...");
    List<BingImage> newImages = await bingApi.FetchAllAsync();
    Console.WriteLine($"[INFO] Fetched {newImages.Count} new images");

    if (newImages.Count == 0)
    {
        Console.WriteLine("[WARN] No images fetched from Bing API.");
        return;
    }

    // 3. 读取现有数据
    List<BingImage> existingImages = await fileService.ReadBingImagesAsync();
    Console.WriteLine($"[INFO] Existing images: {existingImages.Count}");

    // 4. 合并新数据（去重）
    // 新的图片插入到最前面
    var allImages = new List<BingImage>();
    foreach (var newImg in newImages)
    {
        if (!existingImages.Any(e => e.Date == newImg.Date && e.Url == newImg.Url))
        {
            allImages.Add(newImg);
        }
    }
    // 追加已有的
    allImages.AddRange(existingImages);

    // 再次全局去重（按 Date+Url 唯一性）
    var seen = new HashSet<(string Date, string Url)>();
    var deduplicated = new List<BingImage>();
    foreach (var img in allImages)
    {
        if (seen.Add((img.Date, img.Url)))
        {
            deduplicated.Add(img);
        }
    }

    Console.WriteLine($"[INFO] After dedup: {deduplicated.Count} images");

    // 5. 写入文件
    await fileService.WriteBingImagesAsync(deduplicated);
    await fileService.WriteReadmeAsync(deduplicated);
    await fileService.WriteMonthInfoAsync(deduplicated);
    await fileService.WriteImagesJsonAsync(deduplicated);

    Console.WriteLine("[INFO] All files updated successfully!");

    // 6. 输出今日壁纸信息
    if (deduplicated.Count > 0)
    {
        var today = deduplicated[0];
        Console.WriteLine();
        Console.WriteLine("================= Today's Bing Wallpaper =================");
        Console.WriteLine($"  Date:      {today.Date}");
        Console.WriteLine($"  Copyright: {today.Desc}");
        Console.WriteLine($"  URL:       {today.Url}");
        Console.WriteLine("==========================================================");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"[ERROR] {ex.Message}");
    Console.WriteLine(ex.StackTrace);
    Environment.ExitCode = 1;
}