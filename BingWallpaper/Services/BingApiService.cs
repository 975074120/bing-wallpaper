using System.Text.Json;
using BingWallpaper.Models;

namespace BingWallpaper.Services;

/// <summary>
/// 必应 API 服务 - 获取每日壁纸信息
/// </summary>
public class BingApiService
{
    private const string BingApiTemplate = "https://global.bing.com/HPImageArchive.aspx?format=js&idx=0&n=8&pid=hp&FORM=BEHPTB&uhd=1&setmkt={0}&setlang=en";
    private const string BingUrl = "https://cn.bing.com";

    private static readonly string[] Regions = { "en-US", "zh-CN" };

    private readonly HttpClient _httpClient;

    public BingApiService()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
    }

    /// <summary>
    /// 获取所有地区的壁纸
    /// </summary>
    public async Task<List<BingImage>> FetchAllAsync()
    {
        var allImages = new List<BingImage>();
        foreach (var region in Regions)
        {
            try
            {
                var images = await FetchByRegionAsync(region);
                allImages.AddRange(images);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WARN] Failed to fetch region {region}: {ex.Message}");
            }
        }
        return allImages;
    }

    /// <summary>
    /// 获取指定地区的壁纸
    /// </summary>
    public async Task<List<BingImage>> FetchByRegionAsync(string region)
    {
        string apiUrl = string.Format(BingApiTemplate, region);
        string json = await _httpClient.GetStringAsync(apiUrl);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var imagesArray = root.GetProperty("images");

        var result = new List<BingImage>();
        foreach (var item in imagesArray.EnumerateArray())
        {
            string url = BingUrl + item.GetProperty("url").GetString();
            string enddate = item.GetProperty("enddate").GetString()!;
            string copyright = item.GetProperty("copyright").GetString() ?? "";

            // 转换日期格式: yyyyMMdd -> yyyy-MM-dd
            if (DateTime.TryParseExact(enddate, "yyyyMMdd", null,
                    System.Globalization.DateTimeStyles.None, out var dt))
            {
                enddate = dt.ToString("yyyy-MM-dd");
            }

            result.Add(new BingImage(copyright, enddate, url));
        }

        return result;
    }
}