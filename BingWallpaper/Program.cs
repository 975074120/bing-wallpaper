using BingWallpaper.Models;
using BingWallpaper.Services;

// ============================================================
// Bing Wallpaper - 必应每日壁纸抓取工具 (.NET 10 / C#)
// 原项目: https://github.com/niumoo/bing-wallpaper
// 功能: 每日自动抓取 Bing 每日壁纸，按地区分别输出
//       en-US → 根目录, zh-CN → zh-cn/ 文件夹
// 容错: API 返回最近 8 天数据，自动检查并补全缺失的日期
// ============================================================

// 参数: 可指定工作目录，默认为当前目录
string rootDir = args.Length > 0 ? args[0] : Directory.GetCurrentDirectory();
Console.WriteLine($"[INFO] Bing Wallpaper - Working directory: {rootDir}");
Console.WriteLine($"[INFO] Regions: {string.Join(", ", BingApiService.Regions)}");

int successCount = 0;
int failCount = 0;

foreach (string region in BingApiService.Regions)
{
    Console.WriteLine();
    Console.WriteLine($"========== Processing region: {region} ==========");

    try
    {
        // 1. 初始化服务
        var bingApi = new BingApiService();
        var fileService = new FileService(rootDir, region);

        // 2. 从 Bing API 获取最近 8 天的壁纸（用于容错补缺）
        Console.WriteLine($"[INFO] Fetching recent wallpapers (up to 8 days) for {region}...");
        List<BingImage> apiImages = await bingApi.FetchRecentAsync(region);

        if (apiImages.Count == 0)
        {
            Console.WriteLine($"[WARN] No images fetched from Bing API for region {region}.");
            failCount++;
            continue;
        }

        Console.WriteLine($"[INFO] API returned {apiImages.Count} images for {region}, date range: {apiImages.Min(i => i.Date)} ~ {apiImages.Max(i => i.Date)}");
        foreach (var img in apiImages)
        {
            Console.WriteLine($"[INFO]   - {img.Date}: {img.Desc}");
        }

        // 3. 读取该地区现有数据
        List<BingImage> existingImages = await fileService.ReadBingImagesAsync();
        Console.WriteLine($"[INFO] Existing images for {region}: {existingImages.Count}");

        // 4. 容错合并：检查 API 返回的每张图是否已在现有数据中，缺失则插入
        int insertedCount = 0;
        var mergedSet = new HashSet<string>(existingImages.Select(i => $"{i.Date}|{i.Url}"));

        foreach (var apiImg in apiImages)
        {
            string key = $"{apiImg.Date}|{apiImg.Url}";
            if (!mergedSet.Contains(key))
            {
                Console.WriteLine($"[INFO] Missing image found: {apiImg.Date} - {apiImg.Desc}, inserting...");
                existingImages.Add(apiImg);
                mergedSet.Add(key);
                insertedCount++;
            }
        }

        // 5. 按日期降序排序（最新的在前）
        existingImages = existingImages
            .OrderByDescending(i => i.Date)
            .ThenBy(i => i.Url)
            .ToList();

        Console.WriteLine($"[INFO] After merge+dedup for {region}: {existingImages.Count} images (inserted {insertedCount} new)");

        // 6. 写入文件
        await fileService.WriteBingImagesAsync(existingImages);
        await fileService.WriteReadmeAsync(existingImages);
        await fileService.WriteMonthInfoAsync(existingImages);
        await fileService.WriteImagesJsonAsync(existingImages);

        Console.WriteLine($"[INFO] Region {region} - All files updated successfully!");
        successCount++;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERROR] Region {region} failed: {ex.Message}");
        Console.WriteLine(ex.StackTrace);
        failCount++;
    }
}

Console.WriteLine();
Console.WriteLine("========================================");
Console.WriteLine($"[INFO] Total: {BingApiService.Regions.Length} regions, {successCount} succeeded, {failCount} failed");
Console.WriteLine("========================================");