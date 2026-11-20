# 小红书桌面版（XhsDesktop）

[中文](#小红书桌面版xhadesktop) · [English](#xiaohongshu-desktop-xhsdesktop)

用 C# WPF + WebView2 把小红书网页版装进一个独立的 Windows 窗口，界面做成 Word 文档样式：外层是 Word 的标题栏、功能区和状态栏，中间文档页面里显示小红书当前内容。

> 直接下载：[Releases](https://github.com/wstaery/XhsDesktop/releases) 中的 `XhsDesktop.exe`，双击即可运行，无需安装 .NET。
>
> 百度贴吧版已拆分到独立仓库：[TiebaDesktop](https://github.com/wstaery/TiebaDesktop)。

## 功能

- 独立 Windows 窗口，可自由拉伸、最大化，支持多显示器与高 DPI
- 直接加载 `www.xiaohongshu.com`，扫码登录，登录态自动保存（Cookie 持久化，重启免登录）
- Word 风格外壳：自定义标题栏、八个功能区选项卡、文档页面、状态栏
- 窗口尺寸/位置记忆，下次启动自动恢复
- 窗口置顶、关闭最小化到托盘（托盘菜单可显示/退出）
- 推广/广告请求拦截，可一键开关
- 单实例运行，重复启动会唤醒已有窗口
- 站内链接当前窗口打开，站外链接交给系统浏览器
- 页面下载走系统「另存为」对话框

## 环境要求

- Windows 10 / 11 x64
- Microsoft Edge WebView2 Runtime（Win10/11 一般已内置，缺失可从微软官网安装）
- 开发：.NET 9 SDK

## 编译运行

```powershell
git clone https://github.com/wstaery/XhsDesktop.git
cd XhsDesktop

# 调试运行
dotnet run

# 编译
dotnet build -c Debug
```

## 打包为独立 exe

```powershell
dotnet publish -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:EnableCompressionInSingleFile=true -o publish
```

产物为 `publish\XhsDesktop.exe`，自包含单文件，无需安装 .NET，双击即可运行（首次启动自解压较慢属正常）。

## 使用

- 登录：页面右上角「登录」→ 手机扫码
- 浏览器操作：功能区「开始 → 浏览」里的 后退 / 前进 / 刷新 / 首页
- 窗口设置：底部状态栏右侧「置顶」「拦截」「托盘」
- 关闭（×）会最小化到托盘，真正退出请用托盘图标右键「退出」
- 拖动标题栏移动窗口，双击标题栏最大化/还原，拖拽边缘缩放

## 配置文件

设置文件：`%APPDATA%\XhsDesktop\settings.json`

```json
{
  "window": { "left": 100, "top": 80, "width": 1200, "height": 800, "maximized": false },
  "alwaysOnTop": false,
  "blockAds": true,
  "blockedUrlSubstrings": [
    "ads.xiaohongshu.com",
    "ad.xiaohongshu.com",
    "advertise",
    "cm.xiaohongshu.com",
    "/api/sns/web/v1/ad"
  ],
  "extraCss": ""
}
```

- `blockedUrlSubstrings`：命中这些子串的请求会被拦截，可按需增删
- `extraCss`：非空时会注入页面，用于隐藏残留广告位

登录数据：`%LOCALAPPDATA%\XhsDesktop\UserData`（WebView2 用户数据目录）

## 项目结构

```
XhsDesktop/
├─ App.xaml / App.xaml.cs          单实例与启动
├─ MainWindow.xaml / .cs           Word 外壳与 WebView2
├─ Services/AppSettings.cs         设置与窗口状态持久化
├─ Services/AdBlocker.cs           请求级广告拦截
├─ Assets/app.ico                  应用图标
└─ app.manifest                    PerMonitorV2 DPI 声明
```

## 说明

- 本项目只是网页容器，所有内容与登录均由小红书官方网页提供，与官方无关联
- 若站点要求滑块验证，需在窗口内手动完成
- 站点前端改版可能导致广告拦截规则失效，可在 `settings.json` 中补充

---

# Xiaohongshu Desktop (XhsDesktop)

[中文](#小红书桌面版xhadesktop) · [English](#xiaohongshu-desktop-xhsdesktop)

A standalone Windows app built with C# WPF + WebView2 that wraps the Xiaohongshu (RED) web version. The outer shell mimics Microsoft Word — title bar, ribbon, document page and status bar — while the document page shows the live Xiaohongshu website.

> Baidu Tieba Desktop now lives in its own repository: [TiebaDesktop](https://github.com/wstaery/TiebaDesktop).

> Download `XhsDesktop.exe` from [Releases](https://github.com/wstaery/XhsDesktop/releases) and run it directly. No .NET installation required.

## Features

- Standalone resizable Windows window, maximize support, multi-monitor and high-DPI aware
- Loads `www.xiaohongshu.com` directly; QR-code login with persisted cookies (stay signed in after restart)
- Word-style shell: custom title bar, eight ribbon tabs, document page, status bar
- Remembers window size and position between launches
- Always-on-top toggle, close-to-tray with tray menu (show / exit)
- Request-level promo/ad blocking with a one-click toggle
- Single-instance: launching again activates the existing window
- In-site links open in the current window; external links open in the system browser
- Downloads use the standard Save As dialog

## Requirements

- Windows 10 / 11 x64
- Microsoft Edge WebView2 Runtime (usually preinstalled on Windows 10/11)
- For development: .NET 9 SDK

## Build and run

```powershell
git clone https://github.com/wstaery/XhsDesktop.git
cd XhsDesktop

# Run in debug
dotnet run

# Build
dotnet build -c Debug
```

## Publish a standalone exe

```powershell
dotnet publish -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:EnableCompressionInSingleFile=true -o publish
```

The output is `publish\XhsDesktop.exe` — a self-contained single file. Double-click to run; the first launch takes a few seconds while it self-extracts.

## Usage

- Login: click “登录” at the top-right of the page, then scan the QR code with the mobile app
- Navigation: 后退 / 前进 / 刷新 / 首页 (Back / Forward / Refresh / Home) under ribbon “开始 → 浏览”
- Window options: “置顶” (Always on top), “拦截” (Ad blocking), “托盘” (Tray) at the right side of the status bar
- Closing the window (×) hides it to the tray; use the tray icon’s “退出” to quit completely
- Drag the title bar to move, double-click it to maximize/restore, drag edges to resize

## Configuration

Settings file: `%APPDATA%\XhsDesktop\settings.json`

```json
{
  "window": { "left": 100, "top": 80, "width": 1200, "height": 800, "maximized": false },
  "alwaysOnTop": false,
  "blockAds": true,
  "blockedUrlSubstrings": [
    "ads.xiaohongshu.com",
    "ad.xiaohongshu.com",
    "advertise",
    "cm.xiaohongshu.com",
    "/api/sns/web/v1/ad"
  ],
  "extraCss": ""
}
```

- `blockedUrlSubstrings`: requests containing any of these substrings are blocked; add or remove entries as needed
- `extraCss`: when non-empty, injected into the page to hide leftover ad slots

Login data: `%LOCALAPPDATA%\XhsDesktop\UserData` (WebView2 user data folder)

## Project structure

```
XhsDesktop/
├─ App.xaml / App.xaml.cs          Single instance and startup
├─ MainWindow.xaml / .cs           Word shell and WebView2 host
├─ Services/AppSettings.cs         Settings and window geometry persistence
├─ Services/AdBlocker.cs           Request-level ad blocking
├─ Assets/app.ico                  Application icon
└─ app.manifest                    PerMonitorV2 DPI declaration
```

## Notes

- This project is only a web container. All content and login are provided by the official Xiaohongshu website; it is not affiliated with Xiaohongshu
- If the site presents a slider CAPTCHA, solve it manually in the window
- Site redesigns may break the ad-blocking rules; extend them in `settings.json`
