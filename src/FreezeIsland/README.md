# FreezeIsland

FreezeIsland 是 ClassIsland 2.1 的键鼠锁定插件。

## 功能

- 默认热键 `Ctrl + Alt + F`：按下后进入可配置倒计时，再锁定键盘与鼠标按键/滚轮；再次按下解锁。
- 锁定范围：键盘、鼠标按键/滚轮；可允许鼠标指针继续移动。
- 倒计时：0–10 秒可配置；倒计时期间再次按热键可取消。
- 全屏提醒：倒计时开始、已锁定、已解锁均显示加粗文字与左侧警示图标，时长 1–10 秒可配置。
- 主界面组件：“FreezeIsland 锁定状态”显示当前状态与解锁热键，可在组件设置中自定义显示内容、文案、图标与字号。
- 自动化：
  - 行动：`锁定键鼠`、`解锁键鼠`、`切换键鼠锁定`；
  - 触发器：`键鼠锁定状态变化`，可选择“已锁定/已解锁”；
  - “锁定键鼠”行动在自动化恢复时自动解锁。
- 安全桌面保护：Ctrl+Alt+Del、UAC、登录/锁屏等 Winlogon 安全桌面出现时自动解锁。
- 插件卸载、ClassIsland 退出或进程异常结束时自动解除输入阻断。
- 插件启动后默认未锁定。

## 安装

1. 将 `FreezeIsland.cipx` 放入 ClassIsland 的插件包缓存目录：

   ```text
   <ClassIsland 数据目录>\Cache\PluginPackages\FreezeIsland.cipx
   ```

2. 重启 ClassIsland。应用会自动解压到 `<数据目录>\Plugins\winddrift.freezeisland`。
3. 打开【应用设置】→【组件】，可添加“FreezeIsland 锁定状态”组件；插件设置页中可调整热键、锁定范围、倒计时与提醒时长。

## 使用

- 按 `Ctrl + Alt + F` 进入倒计时；倒计时结束后锁定。
- 锁定后再次按 `Ctrl + Alt + F` 解锁。
- 默认锁定键盘与鼠标按键/滚轮，但允许鼠标指针移动。
- 紧急恢复：按 `Ctrl + Alt + Del` 打开 Windows 安全选项，或打开任务管理器结束 ClassIsland 进程。安全桌面出现时插件也会自动解锁。

## 组件设置

在编辑模式下打开“FreezeIsland 锁定状态”的组件设置，可以调整：

- 显示内容：图标、文字、解锁热键、倒计时秒数；可开启“未锁定时隐藏组件”（编辑模式仍显示）。
- 状态文案：未锁定、倒计时、已锁定三段文字，支持 `{hotkey}` 与 `{seconds}` 占位符；留空时只显示图标。
- 图标：大小（12–48）与三种状态的颜色。
- 文字：默认跟随 ClassIsland 高级设置中的正文字号；开启“自定义文字大小”后可单独设置 12–40。

组件设置按组件实例保存。

## 配置

配置保存在：

```text
<ClassIsland 数据目录>\Config\Plugins\winddrift.freezeisland\settings.json
```

默认值：

```json
{
  "Hotkey": "Ctrl+Alt+F",
  "LockKeyboard": true,
  "LockMouse": true,
  "AllowMouseMovement": true,
  "CountdownSeconds": 5,
  "NotificationDurationSeconds": 3
}
```

## 自动化

在 ClassIsland 的自动化设置中添加：

- 行动：`锁定键鼠`、`解锁键鼠`、`切换键鼠锁定`。
- 触发器：`键鼠锁定状态变化`，设置“已锁定”或“已解锁”。

`锁定键鼠` 会遵循插件设置中的倒计时；`解锁键鼠` 立即生效。

## 构建

需要 .NET 8 SDK。

```powershell
dotnet restore src/FreezeIsland/FreezeIsland.csproj
dotnet build src/FreezeIsland/FreezeIsland.csproj -c Release -p:GenerateHashSummary=false
```

产物：

```text
src/FreezeIsland/cipx/FreezeIsland.cipx
```
