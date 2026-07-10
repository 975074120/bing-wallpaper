using BingWallpaper.Models;
using BingWallpaper.Services;

// ============================================================
// Bing Wallpaper - 必应每日壁纸抓取工具 (.NET 10 / C#)
// 原项目: https://github.com/niumoo/bing-wallpaper
// 功能: 每日自动抓取 Bing 每日壁纸，按地区分别输出
//       en-US → 根目录, zh-CN → zh-cn/ 文件夹
// 容错: API 返回最近 8 天数据，自动检查并补全缺失的日期
// 去重: 按 Date + SimpleUrl 去重（忽略分辨率参数），
//       并按地区过滤（en-US 只留 _EN-US 图片，zh-CN 只留 _ZH-CN 图片）
// ============================================================

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
        var bingApi = new BingApiService();
        var fileService = new FileService(rootDir, region);

        // 1. 从 Bing API 获取最近 8 天的壁纸
        Console.WriteLine($"[INFO] Fetching recent wallpapers (up to 8 days) for {region}...");
        List<BingImage> apiImages = await bingApi.FetchRecentAsync(region);

        if (apiImages.Count == 0)
        {
            Console.WriteLine($"[WARN] No images fetched from Bing API for region {region}.");
            failCount++;
            continue;
        }

        Console.WriteLine($"[INFO] API returned {apiImages.Count} images, date range: {apiImages.Min(i => i.Date)} ~ {apiImages.Max(i => i.Date)}");
        foreach (var img in apiImages)
        {
            Console.WriteLine($"[INFO]   - {img.Date}: {img.Desc}");
        }

        // 2. 读取该地区现有数据
        List<BingImage> existingImages = await fileService.ReadBingImagesAsync();
        int beforeFilter = existingImages.Count;

        // 3. 按地区过滤：en-US 只保留含 _EN-US 的图片，zh-CN 只保留含 _ZH-CN 的图片
        string regionMarker = region.Equals("en-US", StringComparison.OrdinalIgnoreCase) ? "_EN-US" : "_ZH-CN";
        int filteredOut = existingImages.RemoveAll(img => !img.Url.Contains(regionMarker, StringComparison.OrdinalIgnoreCase));
        if (filteredOut > 0)
        {
            Console.WriteLine($"[INFO] Filtered out {filteredOut} images from other region (marker: {regionMarker})");
        }

        // 4. 按 Date + SimpleUrl 去重（SimpleUrl 去掉分辨率参数，同图不同分辨率视为重复）
        var dedupMap = new Dictionary<string, BingImage>(StringComparer.OrdinalIgnoreCase);
        foreach (var img in existingImages)
        {
            string key = $"{img.Date}|{img.GetSimpleUrl()}";
            dedupMap[key] = img;  // overwrite keeps the last one
        }
        int dedupRemoved = beforeFilter - filteredOut - dedupMap.Count;
        if (dedupRemoved > 0)
        {
            Console.WriteLine($"[INFO] Removed {dedupRemoved} resolution-duplicates from existing data");
        }
        existingImages = [.. dedupMap.Values];

        Console.WriteLine($"[INFO] Existing images for {region}: {existingImages.Count}");

        // 5. 容错合并：检查 API 返回的每张图是否已在现有数据中
        var mergedSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var img in existingImages)
        {
            mergedSet.Add($"{img.Date}|{img.GetSimpleUrl()}");
        }

        int insertedCount = 0;
        foreach (var apiImg in apiImages)
        {
            string key = $"{apiImg.Date}|{apiImg.GetSimpleUrl()}";
            if (!mergedSet.Contains(key))
            {
                Console.WriteLine($"[INFO] Missing image found: {apiImg.Date} - {apiImg.Desc}, inserting...");
                existingImages.Add(apiImg);
                mergedSet.Add(key);
                insertedCount++;
            }
        }

        // 6. 再次全局去重（防止 API 返回的图片间也有重复）
        var finalDedup = new Dictionary<string, BingImage>(StringComparer.OrdinalIgnoreCase);
        foreach (var img in existingImages)
        {
            string key = $"{img.Date}|{img.GetSimpleUrl()}";
            finalDedup[key] = img;
        }
        existingImages = [.. finalDedup.Values];

        // 7. 按日期降序排序
        existingImages = existingImages
            .OrderByDescending(i => i.Date)
            .ThenBy(i => i.GetSimpleUrl())
            .ToList();

        Console.WriteLine($"[INFO] After merge+dedup: {existingImages.Count} images (inserted {insertedCount} new)");

        // 8. 写入文件
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