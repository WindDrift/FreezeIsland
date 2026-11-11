# FreezeIsland

FreezeIsland 是 ClassIsland 2.1 的键鼠锁定插件。

## 功能

- 默认热键 `Ctrl + Alt + F`：按下后进入可配置倒计时，再锁定键盘与鼠标按键/滚轮；再次按下解锁。
- 锁定范围：键盘与鼠标独立控制。键盘分区锁定属于实验性功能，需在设置页底部开启【实验性功能】后才生效；关闭时锁定整个键盘。鼠标可细分左键、中键、右键、侧键、滚轮与指针移动。
- 排除按键：属于实验性功能，需在设置页底部开启【实验性功能】后才生效；关闭时排除按键不生效。开启后可在锁定期间放行指定的键盘按键（支持音量、媒体等特殊按键），在设置页点击“添加按键”后按键即可加入，支持逐个移除或清空。
- 倒计时：0–10 秒可配置；倒计时期间再次按热键可取消。
- 全屏提醒：倒计时开始、已锁定、已解锁均显示加粗文字与左侧警示图标，时长 1–10 秒可配置。
- 主界面组件：“FreezeIsland 锁定状态”显示当前状态与解锁热键，可在组件设置中自定义显示内容、文案、图标与字号。
- 自动化：
  - 行动：`锁定键鼠`、`解锁键鼠`、`切换键鼠锁定`；
  - 触发器：`键鼠锁定状态变化`，可选择“已锁定/已解锁”；
  - 规则：`键鼠锁定状态是`、`锁定的范围包含`，可作为工作流的规则集条件；
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
- 默认锁定全部键盘分区与鼠标按键/滚轮，但允许鼠标指针移动。
- 键盘分区锁定需先在设置页底部开启【实验性功能】；未开启时锁定整个键盘，分区的开关不生效。
- 在设置页展开“锁定键盘”或“锁定鼠标”，可关闭不需要拦截的子分区；主开关关闭时对应子开关不生效。
- 排除按键需先在设置页底部开启【实验性功能】；未开启时排除按键不生效。开启后打开“排除按键”，点击“添加按键”并按下要放行的键，Esc 取消。排除键只放行该键本身；若要放行组合键，需要同时排除其修饰键。
- 真正的 Fn 键由键盘固件直接处理，Windows 低级钩子无法捕获；F1–F12（FN 区）由“功能键区”控制。
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
  "LockKeyboardLetters": true,
  "LockKeyboardNumberRow": true,
  "LockKeyboardSymbols": true,
  "LockKeyboardFunctionKeys": true,
  "LockKeyboardNumpad": true,
  "LockKeyboardNavigation": true,
  "LockKeyboardControlKeys": true,
  "LockKeyboardModifiers": true,
  "LockKeyboardMediaKeys": true,
  "LockKeyboardOther": true,
  "LockMouseLeftButton": true,
  "LockMouseMiddleButton": true,
  "LockMouseRightButton": true,
  "LockMouseSideButtons": true,
  "LockMouseWheel": true,
  "AllowMouseMovement": true,
  "EnableExperimentalFeatures": false,
  "ExcludedKeys": [],
  "CountdownSeconds": 5,
  "NotificationDurationSeconds": 3
}
```

`ExcludedKeys` 保存虚拟键码，建议通过设置页的“添加按键”维护。旧版只包含 `Hotkey`、`LockKeyboard`、`LockMouse`、`AllowMouseMovement`、`CountdownSeconds`、`NotificationDurationSeconds` 的配置会按原行为升级：锁定全部键盘、鼠标按键与滚轮，并保留原来的鼠标移动设置。`EnableExperimentalFeatures` 默认为 `false`，即升级后先按旧版行为锁定整个键盘，排除按键不生效。

## 自动化

在 ClassIsland 的自动化设置中添加：

- 行动：`锁定键鼠`、`解锁键鼠`、`切换键鼠锁定`。
- 触发器：`键鼠锁定状态变化`，设置“已锁定”或“已解锁”。

`锁定键鼠` 会遵循插件设置中的倒计时；`解锁键鼠` 立即生效。

## 规则

在自动化工作流中开启“启用规则集”后，可以在规则集里选择以下插件提供的规则：

- `键鼠锁定状态是`：设置“已锁定”或“已解锁”。
  - 只有真正拦截输入时才判定为“已锁定”，锁定倒计时期间视为“未锁定”。
  - 输入钩子不可用时无法拦截输入，此时不会判定为“已锁定”。
- `锁定的范围包含`：设置键盘、鼠标、鼠标移动，或鼠标左键/中键/右键/侧键/滚轮。
  - 判定的是插件设置中的锁定范围，而不是某一时刻是否正在拦截，因此未锁定时同样可以成立。
  - 鼠标相关范围要求“锁定鼠标”主开关开启；“鼠标移动”等价于开启“锁定鼠标”并关闭“允许鼠标移动”。
  - 键盘范围只看“锁定键盘”主开关，不受【实验性功能】与键盘分区影响。
  - 需要判断“没有包含某范围”时，使用规则条目自带的“反转判断”。

锁定状态、输入钩子可用性或锁定范围设置发生变化时，插件会通知 ClassIsland 重新判定规则集，因此规则条件和依赖规则集的“规则集不再满足时恢复行动”都会及时响应。

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
