using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using SkiaSharp;
using SkiaSharp.Views.Desktop;

namespace HeroDangle;

public partial class MainWindow : Window
{
    private const float RestLength = 228f;
    private const float CharmHitRadius = 32f;
    private const float ReanchorY = 42f;

    private readonly AppConfig _config;
    private readonly RopeSolver _rope;
    private readonly Vec2[] _renderPoints = new Vec2[RopeSolver.PointCount];
    private readonly HotkeyManager _hotkey;
    private readonly TrayManager _tray;

    private CharmDefinition _charm;
    private bool _summoned;
    private float _presence = 1f;
    private float _presenceVelocity;
    private double _accumulator;
    private double _lastRenderTime = -1;
    private float _simTime;
    private bool _dragging;
    private bool _repositionMode;

    private const float MenuPad = 8f;
    private const float MenuItemHeight = 40f;
    private const float MenuCheckWidth = 22f;
    private const float MenuEmojiWidth = 30f;
    private const float MenuFontSize = 14.5f;
    private const float MenuEmojiSize = 19f;

    private readonly CharmDefinition[] _menuCharms = CharmCatalog.BuiltIn;
    private bool _menuOpen;
    private Vec2 _menuPos;
    private float _menuWidth;
    private float _menuHeight;
    private float _menuDpi = 1f;
    private int _menuHover = -1;

    private SettingsWindow? _settings;

    public MainWindow(AppConfig config)
    {
        _config = config;
        _summoned = config.Summoned;
        _presence = _summoned ? 1f : 0f;
        _charm = CharmCatalog.Resolve(config);

        InitializeComponent();

        Width = SystemParameters.PrimaryScreenWidth;
        Height = SystemParameters.PrimaryScreenHeight;
        Left = 0;
        Top = 0;

        float anchorX = (float)(config.AnchorXNormalized * Width);
        _rope = new RopeSolver(new Vec2(anchorX, LiveAnchorY()), RestLength * Math.Clamp(_presence, 0.04f, 1.2f));

        _hotkey = new HotkeyManager(this, config.HotkeyModifiers, config.HotkeyVirtualKey);
        _hotkey.Pressed += ToggleSummon;

        _tray = new TrayManager(
            () => _charm,
            _hotkey.DisplayText,
            config.LaunchOnStartup,
            OnCharmPicked,
            OnCustomEmoji,
            EnableReposition,
            OpenSettings,
            OnStartupToggled,
            Quit);

        SourceInitialized += (_, _) => OverlayWindow.ApplyChrome(this);
        Loaded += OnLoaded;
        Closed += (_, _) =>
        {
            CompositionTarget.Rendering -= OnRendering;
            _hotkey.Dispose();
            _tray.Dispose();
        };
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        CompositionTarget.Rendering += OnRendering;
        StartupManager.SetEnabled(_config.LaunchOnStartup);
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        double now = (e as RenderingEventArgs)?.RenderingTime.TotalSeconds ?? 0;
        if (_lastRenderTime < 0)
            _lastRenderTime = now;

        double dt = Math.Clamp(now - _lastRenderTime, 0, 0.1);
        _lastRenderTime = now;
        _accumulator += dt;

        StepPresence((float)dt);
        UpdateAnchorFromPresence();

        Vec2 cursor = CursorInWindow();
        bool overCharm = IsOverCharm(cursor);
        bool mouseDown = (NativeMethods.GetAsyncKeyState(NativeMethods.VkLbutton) & 0x8000) != 0;

        if (_dragging && !mouseDown)
            ReleaseDrag(cursor);

        OverlayWindow.SetClickThrough(this, !((_summoned && overCharm) || _dragging || (_menuOpen && InMenu(cursor))));

        const float maxCatchUp = RopeSolver.FixedDt * 8;
        if (_accumulator > maxCatchUp)
            _accumulator = maxCatchUp;

        while (_accumulator >= RopeSolver.FixedDt)
        {
            _simTime += RopeSolver.FixedDt;
            Vec2 wind = new(
                SimplexNoise.Noise(_simTime * 0.28f, 0.13f) * 95f,
                SimplexNoise.Noise(1.7f, _simTime * 0.19f) * 22f);
            _rope.Step(wind);
            _accumulator -= RopeSolver.FixedDt;
        }

        float alpha = (float)(_accumulator / RopeSolver.FixedDt);
        _rope.Interpolate(alpha, _renderPoints);
        Skia.InvalidateVisual();
    }

    private void StepPresence(float dt)
    {
        float target = _summoned ? 1f : 0f;
        // Underdamped spring: drop-in overshoot when summoning, snap-up when dismissed.
        float stiffness = _summoned ? 78f : 140f;
        float damping = _summoned ? 9.5f : 16f;
        float accel = (target - _presence) * stiffness - _presenceVelocity * damping;
        _presenceVelocity += accel * dt;
        _presence += _presenceVelocity * dt;
        if (!_summoned && _presence < 0.002f && MathF.Abs(_presenceVelocity) < 0.05f)
        {
            _presence = 0;
            _presenceVelocity = 0;
        }
    }

    private void UpdateAnchorFromPresence()
    {
        _rope.Anchor = new Vec2(_rope.Anchor.X, LiveAnchorY());
        _rope.RestLength = RestLength * Math.Clamp(_presence, 0.03f, 1.25f);
    }

    private float LiveAnchorY()
    {
        float shown = 10f;
        float hidden = -190f;
        return hidden + (_presence * (shown - hidden));
    }

    private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        SKCanvas canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);
        if (_presence <= 0.01f)
            return;

        float dpi = (float)VisualTreeHelper.GetDpi(this).PixelsPerDip;
        float sway = _rope.SwayAngle(_renderPoints);
        CharmRenderer.Draw(canvas, _renderPoints, _charm, dpi, _presence, sway);
        if (_menuOpen)
            DrawMenu(canvas);
    }

    private void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (_menuOpen)
        {
            Vec2 pt = PointToVec(e.GetPosition(this));
            if (InMenu(pt))
            {
                int index = HitMenu(pt);
                if (index >= 0)
                    OnCharmPicked(_menuCharms[index]);
                _menuOpen = false;
                e.Handled = true;
                return;
            }
            _menuOpen = false;
        }

        if (!_summoned)
            return;

        Vec2 cursor = PointToVec(e.GetPosition(this));
        if (!IsOverCharm(cursor) && !_repositionMode)
            return;

        _dragging = true;
        CaptureMouse();
        _rope.BeginDrag(cursor);
        if (_repositionMode || cursor.Y < ReanchorY)
            Reanchor(cursor.X);
        e.Handled = true;
    }

    private void OnPreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (_menuOpen)
            _menuHover = HitMenu(PointToVec(e.GetPosition(this)));

        if (!_dragging)
            return;

        Vec2 cursor = PointToVec(e.GetPosition(this));
        _rope.UpdateDrag(cursor);
        if (_repositionMode || cursor.Y < ReanchorY)
            Reanchor(cursor.X);
    }

    private void OnPreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!_dragging)
            return;

        ReleaseDrag(PointToVec(e.GetPosition(this)));
        e.Handled = true;
    }

    private void OnPreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_summoned)
            return;

        Vec2 cursor = PointToVec(e.GetPosition(this));

        if (_menuOpen)
        {
            _menuOpen = false;
            e.Handled = true;
            return;
        }

        if (!IsOverCharm(cursor))
            return;

        e.Handled = true;
        OpenMenu(cursor);
    }

    private bool InMenu(Vec2 p)
    {
        return _menuOpen &&
               p.X >= _menuPos.X && p.X <= _menuPos.X + _menuWidth &&
               p.Y >= _menuPos.Y && p.Y <= _menuPos.Y + _menuHeight;
    }

    private int HitMenu(Vec2 p)
    {
        if (_menuDpi <= 0f || !InMenu(p))
            return -1;

        float itemH = MenuItemHeight * _menuDpi;
        float innerTop = _menuPos.Y + MenuPad * _menuDpi;
        if (p.Y < innerTop)
            return -1;

        int index = (int)((p.Y - innerTop) / itemH);
        if (index < 0 || index >= _menuCharms.Length || p.Y >= innerTop + (index + 1) * itemH)
            return -1;
        return index;
    }

    private void OpenMenu(Vec2 cursor)
    {
        _menuDpi = (float)VisualTreeHelper.GetDpi(this).PixelsPerDip;

        float maxNameWidth = 0f;
        using (var measurePaint = new SKPaint
        {
            Typeface = SKTypeface.FromFamilyName("Segoe UI"),
            TextSize = MenuFontSize * _menuDpi,
            IsAntialias = true
        })
        {
            foreach (CharmDefinition charm in _menuCharms)
                maxNameWidth = Math.Max(maxNameWidth, measurePaint.MeasureText(charm.Name));
        }

        float itemH = MenuItemHeight * _menuDpi;
        float width = MenuCheckWidth * _menuDpi + MenuEmojiWidth * _menuDpi + maxNameWidth + MenuPad * 5f * _menuDpi;
        float height = _menuCharms.Length * itemH + MenuPad * 2f * _menuDpi;

        float x = cursor.X + 10f;
        if (x + width > Width - 6f)
            x = cursor.X - width - 10f;
        x = Math.Clamp(x, 6f, Math.Max(6f, (float)Width - width - 6f));

        float y = cursor.Y + 10f;
        if (y + height > Height - 6f)
            y = Math.Max(6f, (float)Height - height - 6f);

        _menuPos = new Vec2(x, y);
        _menuWidth = width;
        _menuHeight = height;
        _menuHover = -1;
        _menuOpen = true;
    }

    private void DrawMenu(SKCanvas canvas)
    {
        if (_menuDpi <= 0f)
            return;

        float s = _menuDpi;
        float px = _menuPos.X;
        float py = _menuPos.Y;
        float w = _menuWidth;
        float h = _menuHeight;

        var box = new SKRoundRect(new SKRect(px, py, px + w, py + h), 8f * s, 8f * s);
        using (var fill = new SKPaint { IsAntialias = true, Color = new SKColor(248, 249, 251, 242) })
        {
            canvas.DrawRoundRect(box, fill);
        }
        using (var border = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1f * s, Color = new SKColor(196, 202, 212, 210) })
        {
            canvas.DrawRoundRect(box, border);
        }

        float itemH = MenuItemHeight * s;
        for (int i = 0; i < _menuCharms.Length; i++)
        {
            float rowTop = py + MenuPad * s + i * itemH;
            if (i == _menuHover)
            {
                var row = new SKRoundRect(new SKRect(px + 4f * s, rowTop, px + w - 4f * s, rowTop + itemH), 6f * s, 6f * s);
                using (var highlight = new SKPaint { IsAntialias = true, Color = new SKColor(210, 228, 250, 220) })
                {
                    canvas.DrawRoundRect(row, highlight);
                }
            }

            float centerY = rowTop + itemH / 2f;
            float colX = px + MenuPad * s;

            using (var checkFont = new SKFont(SKTypeface.FromFamilyName("Segoe UI"), 13f * s))
            using (var checkPaint = new SKPaint
            {
                IsAntialias = true,
                Color = _menuCharms[i].Id == _charm.Id ? new SKColor(28, 112, 200) : new SKColor(186, 192, 202)
            })
            {
                canvas.DrawText("✓", colX + 2f * s, centerY + 4.5f * s, checkFont, checkPaint);
            }

            float emojiX = px + (MenuCheckWidth + MenuEmojiWidth / 2f) * s + MenuPad * s;
            using (var emojiFont = new SKFont(SKTypeface.FromFamilyName("Segoe UI Emoji"), MenuEmojiSize * s))
            using (var emojiPaint = new SKPaint { IsAntialias = true, TextAlign = SKTextAlign.Center })
            {
                canvas.DrawText(_menuCharms[i].Emoji, emojiX, centerY + MenuEmojiSize * s * 0.36f, emojiFont, emojiPaint);
            }

            float nameX = px + (MenuCheckWidth + MenuEmojiWidth) * s + MenuPad * s;
            using (var nameFont = new SKFont(SKTypeface.FromFamilyName("Segoe UI"), MenuFontSize * s))
            using (var namePaint = new SKPaint { IsAntialias = true, Color = new SKColor(31, 35, 40, 235) })
            {
                canvas.DrawText(_menuCharms[i].Name, nameX, centerY + MenuFontSize * s * 0.36f, nameFont, namePaint);
            }
        }
    }

    private void ReleaseDrag(Vec2 cursor)
    {
        _rope.UpdateDrag(cursor);
        _rope.EndDrag();
        _dragging = false;
        _repositionMode = false;
        if (IsMouseCaptured)
            ReleaseMouseCapture();
    }

    private void Reanchor(float x)
    {
        float clamped = Math.Clamp(x, 24f, (float)Width - 24f);
        _rope.Anchor = new Vec2(clamped, LiveAnchorY());
        _config.AnchorXNormalized = Width <= 0 ? 0.5 : clamped / Width;
        ConfigService.Save(_config);
    }

    private bool IsOverCharm(Vec2 cursor)
    {
        if (_presence < 0.15f)
            return false;

        Vec2 tip = _renderPoints[^1];
        return Vec2.Distance(cursor, tip) <= CharmHitRadius;
    }

    private Vec2 CursorInWindow()
    {
        NativeMethods.GetCursorPos(out NativeMethods.Point p);
        var relative = PointFromScreen(new Point(p.X, p.Y));
        return PointToVec(relative);
    }

    private static Vec2 PointToVec(Point p) => new((float)p.X, (float)p.Y);

    private void ToggleSummon()
    {
        _summoned = !_summoned;
        if (!_summoned)
            _menuOpen = false;
        _config.Summoned = _summoned;
        ConfigService.Save(_config);
        if (_summoned && _presence < 0.05f)
            _rope.ResetToRest();
    }

    private void OnCharmPicked(CharmDefinition charm)
    {
        _charm = charm;
        _config.CharmId = charm.Id;
        if (charm.Id != "custom")
            ConfigService.Save(_config);
        else
        {
            _config.CustomEmoji = charm.Emoji;
            ConfigService.Save(_config);
        }
    }

    private void OnCustomEmoji()
    {
        var dialog = new EmojiPromptWindow(_config.CustomEmoji);
        if (dialog.ShowDialog() == true)
        {
            _config.CharmId = "custom";
            _config.CustomEmoji = dialog.Emoji;
            _charm = CharmCatalog.Custom(dialog.Emoji);
            ConfigService.Save(_config);
        }
    }

    private void EnableReposition()
    {
        _summoned = true;
        _repositionMode = true;
    }

    private void OpenSettings()
    {
        if (_settings is { IsVisible: true })
        {
            _settings.Activate();
            return;
        }

        _settings = new SettingsWindow(_config, _hotkey, OnCharmPicked);
        _settings.Closed += (_, _) => _settings = null;
        _settings.Show();
    }

    private void OnStartupToggled(bool enabled)
    {
        _config.LaunchOnStartup = enabled;
        StartupManager.SetEnabled(enabled);
        ConfigService.Save(_config);
    }

    private void Quit()
    {
        ConfigService.Save(_config);
        Application.Current.Shutdown();
    }
}
