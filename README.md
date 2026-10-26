# SimpleBin

一个轻量的 Windows 回收站托盘工具。

- 左键单击托盘图标，打开回收站。
- 右键选择“清空...”清空回收站，删除前由系统确认；选择“退出”关闭程序。
- 图标区分回收站空/非空状态，并跟随系统深浅色主题。
- 安装后自动启用开机启动。

运行要求：Windows 10（19041）或更新版本，以及 .NET 10 Desktop Runtime。

从源码构建：

```powershell
dotnet build -c Release
```
