using System;
using System.Linq;
using Microsoft.Web.WebView2.Core;

namespace XhsDesktop.Services;

/// <summary>
/// 请求级推广/统计拦截：命中 settings.json 中配置的子串时直接返回 403，不加载资源。
/// 只拦截推广与埋点类地址，不拦截 feed 接口，避免正文加载失败。
/// </summary>
public sealed class AdBlocker
{
    private readonly CoreWebView2Environment _environment;
    private readonly AppSettings _settings;

    public AdBlocker(CoreWebView2 core, CoreWebView2Environment environment, AppSettings settings)
    {
        _environment = environment;
        _settings = settings;

        core.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
        core.WebResourceRequested += OnWebResourceRequested;
    }

    public int BlockedCount { get; private set; }

    private void OnWebResourceRequested(object? sender, CoreWebView2WebResourceRequestedEventArgs e)
    {
        if (!_settings.BlockAds) return;

        var patterns = _settings.BlockedUrlSubstrings;
        if (patterns is null || patterns.Length == 0) return;

        var url = e.Request.Uri;
        var blocked = patterns.Any(p =>
            !string.IsNullOrWhiteSpace(p) &&
            url.Contains(p.Trim(), StringComparison.OrdinalIgnoreCase));

        if (!blocked) return;

        e.Response = _environment.CreateWebResourceResponse(
            null, 403, "Blocked", "Access-Control-Allow-Origin: *");
        BlockedCount++;
    }
}
