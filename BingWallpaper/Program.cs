using BingWallpaper.Models;
using BingWallpaper.Services;

// ============================================================
// Bing Wallpaper - 必应每日壁纸抓取工具 (.NET 10 / C#)
// 原项目: https://github.com/niumoo/bing-wallpaper
// 功能: 每日自动抓取 Bing 每日壁纸，按地区分别输出
//       en-US → 根目录, zh-CN → zh-cn/ 文件夹
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

        // 2. 从 Bing API 获取今日壁纸
        Console.WriteLine($"[INFO] Fetching today's wallpaper for {region}...");
        BingImage? todayImage = await bingApi.FetchTodayAsync(region);

        if (todayImage == null)
        {
            Console.WriteLine($"[WARN] No image fetched from Bing API for region {region}.");
            failCount++;
            continue;
        }

        Console.WriteLine($"[INFO] Today's wallpaper ({region}): {todayImage.Date} - {todayImage.Desc}");

        // 3. 读取该地区现有数据
        List<BingImage> existingImages = await fileService.ReadBingImagesAsync();
        Console.WriteLine($"[INFO] Existing images for {region}: {existingImages.Count}");

        // 4. 合并新数据（去重）
        var mergedImages = new List<BingImage> { todayImage };
        foreach (var img in existingImages)
        {
            // 按 Date+Url 去重
            if (!mergedImages.Any(e => e.Date == img.Date && e.Url == img.Url))
            {
                mergedImages.Add(img);
            }
        }

        Console.WriteLine($"[INFO] After merge+dedup for {region}: {mergedImages.Count} images");

        // 5. 写入文件
        await fileService.WriteBingImagesAsync(mergedImages);
        await fileService.WriteReadmeAsync(mergedImages);
        await fileService.WriteMonthInfoAsync(mergedImages);
        await fileService.WriteImagesJsonAsync(mergedImages);

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

if (failCount > 0)
{
    Environment.ExitCode = 1;
}