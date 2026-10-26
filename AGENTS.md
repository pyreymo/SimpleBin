# 工作约束

- 除非用户明确要求，禁止旧配置兼容、迁移机制和保留已废弃实现。
- 禁止截图；不新增验证工程、验证脚本或 smoketest。
- 所有构建命令以本项目根目录为工作目录。构建输出只放 `SimpleBin/bin`，中间文件只放 `SimpleBin/obj`，发布与安装包只放 `artifacts`。
- 永远不要将构建、打包或安装输出放入系统临时目录、Codex 临时目录或 Codex 的 MSIX 虚拟 AppData。运行构建、打包工具前，将当前进程的 `TEMP` 和 `TMP` 指向项目内的 `artifacts/pack-work`。
- 本机安装必须显式指定绝对安装路径 `<项目根目录>/artifacts/installed`，禁止依赖安装器的默认 AppData 路径。从 Codex 调用安装器或已安装程序时，通过桌面 Explorer 的 `Shell.Application.ShellExecute` 启动，避免继承 Codex 的应用虚拟化上下文。
- 桌面与开始菜单快捷方式必须指向该安装目录的 `current/SimpleBin.exe`，图标必须明确指向同目录的 `App.ico`。
- 每项独立修改用独立 commit 跟踪；只要求构建通过，功能由用户验收。
