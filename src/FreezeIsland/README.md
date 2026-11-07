# FreezeIsland

FreezeIsland 是 ClassIsland 2.1 的键鼠锁定插件。它使用全局热键切换键盘/鼠标锁定，提供全屏提醒、主界面状态组件、设置页与自动化行动/触发器。

## 功能

- 默认热键 `Ctrl + Alt + F`：按下后进入可配置倒计时，再锁定键盘与鼠标按键/滚轮；再次按下解锁。
- 可配置锁定范围：键盘、鼠标、两者；可允许鼠标指针继续移动。
- 全屏提醒：倒计时开始、已锁定、已解锁。
- 主界面组件：显示当前锁定状态与解锁热键。
- 自动化：锁定/解锁/切换行动，以及键鼠锁定状态变化触发器。
- 安全桌面保护：Ctrl+Alt+Del、UAC 等 Winlogon 安全桌面出现时自动解锁。
- 插件退出/ClassIsland 退出时自动解除锁定。

## 使用

1. 安装 `FreezeIsland.cipx` 并重启 ClassIsland。
2. 在设置中打开 FreezeIsland 页面，确认热键与锁定范围。
3. 在主界面编辑模式中添加“FreezeIsland 锁定状态”组件（可选）。
4. 按热键锁定/解锁。

紧急恢复：按 `Ctrl + Alt + Del` 打开安全选项，或打开任务管理器结束 ClassIsland 进程。

## 构建

需要 .NET 8 SDK。

```powershell
dotnet build src/FreezeIsland/FreezeIsland.csproj -c Release -p:GenerateHashSummary=false
```

产物位于 `src/FreezeIsland/cipx/FreezeIsland.cipx`。
