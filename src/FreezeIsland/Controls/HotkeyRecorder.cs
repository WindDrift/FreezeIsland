using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using FreezeIsland.Models;

namespace FreezeIsland.Controls;

/// <summary>
/// 点击后录制一个组合键。
/// </summary>
public sealed class HotkeyRecorder : Button
{
    private readonly TextBlock _label = new();
    private HotkeyGesture? _gesture;
    private bool _recording;

    public HotkeyRecorder()
    {
        Focusable = true;
        HorizontalAlignment = HorizontalAlignment.Left;
        HorizontalContentAlignment = HorizontalAlignment.Center;
        MinWidth = 190;
        Padding = new Thickness(14, 7);
        Content = _label;

        Click += (_, _) => BeginRecording();
        KeyDown += OnKeyDown;
        LostFocus += (_, _) => CancelRecording();

        UpdateLabel();
    }

    public HotkeyGesture? Gesture => _gesture;

    public event EventHandler? GestureChanged;

    public void SetGesture(HotkeyGesture? gesture)
    {
        _gesture = gesture;
        _recording = false;
        UpdateLabel();
    }

    private void BeginRecording()
    {
        _recording = true;
        _label.Text = "按下组合键…（Esc 取消）";
        Focus();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (!_recording)
        {
            return;
        }

        e.Handled = true;

        if (e.Key == Key.Escape)
        {
            CancelRecording();
            return;
        }

        if (e.Key == Key.None || VirtualKeyMap.IsModifierKey(e.Key))
        {
            _label.Text = "请按下主键…（Esc 取消）";
            return;
        }

        if (!VirtualKeyMap.TryFromAvaloniaKey(e.Key, out var virtualKey))
        {
            _label.Text = "不支持该按键，请换一个";
            return;
        }

        var gesture = new HotkeyGesture(
            virtualKey,
            VirtualKeyMap.ToHotkeyModifiers(e.KeyModifiers));

        if (!HotkeyParser.IsValid(gesture, out var error))
        {
            _label.Text = error ?? "无效组合键";
            return;
        }

        _gesture = gesture;
        _recording = false;
        UpdateLabel();
        GestureChanged?.Invoke(this, EventArgs.Empty);
    }

    private void CancelRecording()
    {
        if (!_recording)
        {
            return;
        }

        _recording = false;
        UpdateLabel();
    }

    private void UpdateLabel()
    {
        _label.Text = _gesture?.ToString() ?? "未设置";
    }
}
