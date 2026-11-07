# FreezeIsland

FreezeIsland 是 ClassIsland 2.1 的键鼠锁定插件。

## 功能

- 默认热键 `Ctrl + Alt + F`：按下后进入可配置倒计时，再锁定键盘与鼠标按键/滚轮；再次按下解锁。
- 锁定范围：键盘、鼠标按键/滚轮；可允许鼠标指针继续移动。
- 倒计时：0–10 秒可配置；倒计时期间再次按热键可取消。
- 全屏提醒：倒计时开始、已锁定、已解锁均显示纯文字提示，时长 1–10 秒可配置。
- 主界面组件：“FreezeIsland 锁定状态”显示当前状态与解锁热键。
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
