# SimpleBin

轻量的 Windows 回收站托盘程序。

- 单击左键打开系统回收站。
- 单击右键显示带图标的“清空...”和“退出”，每项约 36px 高。
- 清空前由 Windows 显示确认框；退出直接关闭程序。
- 托盘图标区分回收站空/非空，并跟随系统深浅色主题。
- 安装后自动启用开机启动，卸载时移除启动项。

没有设置窗口、数量或大小预览，也不保存用户配置。重复启动会直接退出，不会创建第二个托盘图标。

要求：Windows 10.0.19041.0 或更新版本、.NET 10 Desktop Runtime。

构建：`dotnet build -c Release`。输出固定在项目内的 `SimpleBin/bin/Release`；发布输出固定在项目内的 `artifacts/publish`。

安装包使用 Velopack，显式包含桌面与开始菜单快捷方式：

```powershell
dotnet publish SimpleBin/SimpleBin.csproj -c Release --self-contained false
vpk pack -u SimpleBin -v 1.1.6 -p artifacts/publish -o artifacts/installer -e SimpleBin.exe -i SimpleBin/App.ico --shortcuts Desktop,StartMenuRoot --delta None
```

本机安装位置明确指定为项目内的 `artifacts/installed`，避免从打包应用启动安装器时落入其 AppData 虚拟目录：

```powershell
./artifacts/installer/SimpleBin-win-Setup.exe --silent --installto "$PWD/artifacts/installed"
```

安装与更新时创建桌面和开始菜单快捷方式，目标为安装目录的 `SimpleBin.exe`，图标明确使用同目录的 `App.ico`。
