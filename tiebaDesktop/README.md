# 百度贴吧桌面版（TiebaDesktop）

[中文](#百度贴吧桌面版tiebaDesktop) · [English](#baidu-tieba-desktop-tiebadesktop)

用 C# WPF + WebView2 把百度贴吧网页版装进一个独立的 Windows 窗口，界面沿用同仓库小红书桌面版的 Word 文档风格：外层是 Word 的标题栏、功能区和状态栏，中间文档页面里显示贴吧当前内容。

## 功能

- 独立 Windows 窗口，可自由拉伸、最大化，支持多显示器与高 DPI
- 直接加载 `tieba.baidu.com`，扫码登录百度账号，登录态自动保存（Cookie 持久化，重启免登录）
- Word 风格外壳：自定义标题栏、八个功能区选项卡、文档页面、状态栏
- 窗口尺寸/位置记忆，下次启动自动恢复
- 窗口置顶、关闭最小化到托盘（托盘菜单可显示/退出）
- 百度推广/统计请求拦截，可一键开关
- 单实例运行，重复启动会唤醒已有窗口
- 百度站内链接当前窗口打开，站外链接交给系统浏览器
- 页面下载走系统「另存为」对话框

## 环境要求

- Windows 10 / 11 x64
- Microsoft Edge WebView2 Runtime（Win10/11 一般已内置，缺失可从微软官网安装）
- 开发：.NET 9 SDK

## 编译运行

```powershell
git clone https://github.com/wstaery/XhsDesktop.git
cd XhsDesktop\tiebaDesktop

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

产物为 `publish\TiebaDesktop.exe`，自包含单文件，无需安装 .NET，双击即可运行（首次启动自解压较慢属正常）。

## 使用

- 登录：页面右上角「登录」→ 扫码或账号密码登录
- 浏览器操作：功能区「开始 → 浏览」里的 后退 / 前进 / 刷新 / 首页
- 窗口设置：底部状态栏右侧「置顶」「拦截」「托盘」
- 关闭（×）会最小化到托盘，真正退出请用托盘图标右键「退出」
- 拖动标题栏移动窗口，双击标题栏最大化/还原，拖拽边缘缩放

## 配置文件

设置文件：`%APPDATA%\TiebaDesktop\settings.json`

```json
{
  "window": { "left": 100, "top": 80, "width": 1200, "height": 800, "maximized": false },
  "alwaysOnTop": false,
  "blockAds": true,
  "blockedUrlSubstrings": [
    "cpro.baidu.com",
    "pos.baidu.com",
    "cbjs.baidu.com",
    "hm.baidu.com",
    "eclick.baidu.com"
  ],
  "extraCss": ""
}
```

- `blockedUrlSubstrings`：命中这些子串的请求会被拦截，可按需增删
- `extraCss`：非空时会注入页面，用于隐藏残留广告位

登录数据：`%LOCALAPPDATA%\TiebaDesktop\UserData`（WebView2 用户数据目录）

## 项目结构

```
tiebaDesktop/
├─ App.xaml / App.xaml.cs          单实例与启动
├─ MainWindow.xaml / .cs           Word 外壳与 WebView2
├─ Services/AppSettings.cs         设置与窗口状态持久化
├─ Services/AdBlocker.cs           请求级广告拦截
├─ Assets/app.ico                  应用图标
└─ app.manifest                    PerMonitorV2 DPI 声明
```

## 说明

- 本项目只是网页容器，所有内容与登录均由百度贴吧官方网页提供，与百度官方无关联
- 若站点要求滑块/短信验证，需在窗口内手动完成
- 站点前端改版可能导致广告拦截规则失效，可在 `settings.json` 中补充

---

# Baidu Tieba Desktop (TiebaDesktop)

[中文](#百度贴吧桌面版tiebaDesktop) · [English](#baidu-tieba-desktop-tiebadesktop)

A standalone Windows app built with C# WPF + WebView2 that wraps the Baidu Tieba web version. It shares the Word-style shell design of the Xiaohongshu desktop app in this repository: a Word title bar, ribbon and status bar around a document page that shows the live Tieba website.

## Features

- Standalone resizable Windows window, maximize support, multi-monitor and high-DPI aware
- Loads `tieba.baidu.com` directly; QR-code login with persisted cookies (stay signed in after restart)
- Word-style shell: custom title bar, eight ribbon tabs, document page, status bar
- Remembers window size and position between launches
- Always-on-top toggle, close-to-tray with tray menu (show / exit)
- Request-level Baidu promo/tracking blocking with a one-click toggle
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
cd XhsDesktop\tiebaDesktop

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

The output is `publish\TiebaDesktop.exe` — a self-contained single file. Double-click to run; the first launch takes a few seconds while it self-extracts.

## Usage

- Login: click “登录” at the top-right of the page and sign in with QR code or account
- Navigation: 后退 / 前进 / 刷新 / 首页 (Back / Forward / Refresh / Home) under ribbon “开始 → 浏览”
- Window options: “置顶” (Always on top), “拦截” (Ad blocking), “托盘” (Tray) at the right side of the status bar
- Closing the window (×) hides it to the tray; use the tray icon’s “退出” to quit completely
- Drag the title bar to move, double-click it to maximize/restore, drag edges to resize

## Configuration

Settings file: `%APPDATA%\TiebaDesktop\settings.json`

```json
{
  "window": { "left": 100, "top": 80, "width": 1200, "height": 800, "maximized": false },
  "alwaysOnTop": false,
  "blockAds": true,
  "blockedUrlSubstrings": [
    "cpro.baidu.com",
    "pos.baidu.com",
    "cbjs.baidu.com",
    "hm.baidu.com",
    "eclick.baidu.com"
  ],
  "extraCss": ""
}
```

- `blockedUrlSubstrings`: requests containing any of these substrings are blocked; add or remove entries as needed
- `extraCss`: when non-empty, injected into the page to hide leftover ad slots

Login data: `%LOCALAPPDATA%\TiebaDesktop\UserData` (WebView2 user data folder)

## Project structure

```
tiebaDesktop/
├─ App.xaml / App.xaml.cs          Single instance and startup
├─ MainWindow.xaml / .cs           Word shell and WebView2 host
├─ Services/AppSettings.cs         Settings and window geometry persistence
├─ Services/AdBlocker.cs           Request-level ad blocking
├─ Assets/app.ico                  Application icon
└─ app.manifest                    PerMonitorV2 DPI declaration
```

## Notes

- This project is only a web container. All content and login are provided by the official Baidu Tieba website; it is not affiliated with Baidu
- If the site presents a slider or SMS CAPTCHA, complete it manually in the window
- Site redesigns may break the ad-blocking rules; extend them in `settings.json`
