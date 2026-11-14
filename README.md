# FreezeIsland

ClassIsland 2.1 的键鼠锁定插件：按全局热键锁定键盘与鼠标，再次按下解锁。

## 功能

- 全局热键进入可配置倒计时后锁定键盘与鼠标；倒计时期间再次按下可取消，锁定后再次按下解锁。
- 键盘与鼠标可分别开启。鼠标可细分左键、中键、右键、侧键、滚轮与指针移动。
- 实验性功能开关：开启后启用键盘分区锁定与排除按键，关闭时锁定整个键盘且排除按键不生效。
- 倒计时、锁定、解锁时显示全屏提醒，显示时长可配置。
- 主界面组件显示当前状态与解锁热键，显示内容、文案、图标颜色与字号均可配置。
- 自动化：行动 `锁定键鼠`、`解锁键鼠`、`切换键鼠锁定`；触发器 `键鼠锁定状态变化`；规则 `键鼠锁定状态是`、`锁定的范围包含`。
- Winlogon 安全桌面出现、ClassIsland 退出或插件卸载时自动解锁。

锁定期间按 `Ctrl + Alt + Del` 打开 Windows 安全选项即可恢复操作，插件检测到安全桌面后会自动解锁。

## 安装

1. 从 GitHub Releases 下载 `FreezeIsland.cipx`。
2. 放入 `<ClassIsland 数据目录>\Cache\PluginPackages\`。
3. 重启 ClassIsland。插件会解压到 `<数据目录>\Plugins\winddrift.freezeisland`。

## 构建

需要 .NET 8 SDK。

```powershell
dotnet build src/FreezeIsland/FreezeIsland.csproj -c Release
```

产物为 `src/FreezeIsland/cipx/FreezeIsland.cipx`。

## 许可证

[MIT](LICENSE)
