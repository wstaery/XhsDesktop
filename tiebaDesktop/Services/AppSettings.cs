using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TiebaDesktop.Services;

public sealed class WindowGeometry
{
    public double Left { get; set; } = 100;
    public double Top { get; set; } = 80;
    public double Width { get; set; } = 1200;
    public double Height { get; set; } = 800;
    public bool Maximized { get; set; }
}

public sealed class AppSettings
{
    public static readonly string[] DefaultBlockedUrlSubstrings =
    {
        "cpro.baidu.com",
        "pos.baidu.com",
        "cbjs.baidu.com",
        "hm.baidu.com",
        "eclick.baidu.com"
    };

    public WindowGeometry Window { get; set; } = new();

    public bool AlwaysOnTop { get; set; }

    public bool BlockAds { get; set; } = true;

    public string[] BlockedUrlSubstrings { get; set; } = (string[])DefaultBlockedUrlSubstrings.Clone();

    public string ExtraCss { get; set; } = string.Empty;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    [JsonIgnore]
    public static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "TiebaDesktop",
        "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath), JsonOptions);
                if (settings != null)
                {
                    if (settings.Window is null) settings.Window = new WindowGeometry();
                    if (settings.BlockedUrlSubstrings is null || settings.BlockedUrlSubstrings.Length == 0)
                        settings.BlockedUrlSubstrings = (string[])DefaultBlockedUrlSubstrings.Clone();
                    if (settings.ExtraCss is null) settings.ExtraCss = string.Empty;
                    return settings;
                }
            }
        }
        catch
        {
            // 配置损坏时回退默认值，不影响启动。
        }

        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            var directory = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, JsonOptions));
        }
        catch
        {
            // 写入失败不阻断交互。
        }
    }
}
