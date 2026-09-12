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

        OverlayWindow.SetClickThrough(this, !((_summoned && overCharm) || _dragging));

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
    }

    private void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
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
