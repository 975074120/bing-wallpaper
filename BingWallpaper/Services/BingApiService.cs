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

    public static readonly string[] Regions = { "en-US", "zh-CN" };

    private readonly HttpClient _httpClient;

    public BingApiService()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
    }

    /// <summary>
    /// 获取指定地区最近 N 天的壁纸列表（API 返回 8 天，全部取回用于容错补缺）
    /// </summary>
    public async Task<List<BingImage>> FetchRecentAsync(string region)
    {
        string apiUrl = string.Format(BingApiTemplate, region);
        string json = await _httpClient.GetStringAsync(apiUrl);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var imagesArray = root.GetProperty("images");

        var results = new List<BingImage>();

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

            results.Add(new BingImage(copyright, enddate, url));
        }

        return results;
    }
}