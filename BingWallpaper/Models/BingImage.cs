namespace BingWallpaper.Models;

/// <summary>
/// 必应每日壁纸图片信息
/// </summary>
public class BingImage
{
    public string Desc { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;

    public BingImage() { }

    public BingImage(string desc, string date, string url)
    {
        Desc = desc;
        Date = date;
        Url = url;
    }

    /// <summary>
    /// 获取简化的 URL（去掉参数部分）
    /// </summary>
    public string GetSimpleUrl()
    {
        int idx = Url.IndexOf('&');
        return idx >= 0 ? Url[..idx] : Url;
    }

    /// <summary>
    /// 获取缩略图 URL
    /// </summary>
    public string GetThumbnailUrl()
    {
        string smallUrl = GetSimpleUrl();
        return $"{smallUrl}&pid=hp&w=384&h=216&rs=1&c=4";
    }

    /// <summary>
    /// 获取大图 URL
    /// </summary>
    public string GetLargeUrl()
    {
        string smallUrl = GetSimpleUrl();
        return $"{smallUrl}&w=1000";
    }

    /// <summary>
    /// 格式化 Markdown 条目
    /// </summary>
    public string FormatMarkdown()
    {
        return $"{Date} | [{Desc}]({Url}) ";
    }

    /// <summary>
    /// 获取详情页路径
    /// </summary>
    public string GetDetailUrlPath()
    {
        string yyyymm = Date.Replace("-", "")[..6];
        string dd = Date[8..];
        return $"day/{yyyymm}/{dd}.html";
    }

    public override string ToString()
    {
        return $"![]({GetThumbnailUrl()}){Date} [download 4k]({Url})";
    }

    public string ToLarge()
    {
        return $"![]({GetLargeUrl()})Today: [{Desc}]({Url})";
    }

    public override bool Equals(object? obj)
    {
        if (obj is not BingImage other) return false;
        return Desc == other.Desc && Date == other.Date && Url == other.Url;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Desc, Date, Url);
    }
}