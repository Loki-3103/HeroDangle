using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Hardcodet.Wpf.TaskbarNotification;
using SkiaSharp;

namespace HeroDangle;

public sealed class TrayManager : IDisposable
{
    private readonly TaskbarIcon _icon;
    private readonly Func<CharmDefinition> _currentCharm;
    private readonly MenuItem _startupItem;

    public TrayManager(
        Func<CharmDefinition> currentCharm,
        string hotkeyText,
        bool launchOnStartup,
        Action<CharmDefinition> onCharm,
        Action onCustomEmoji,
        Action onReposition,
        Action onSettings,
        Action<bool> onStartup,
        Action onQuit)
    {
        _currentCharm = currentCharm;

        var charms = new MenuItem { Header = "Charm" };
        foreach (var charm in CharmCatalog.BuiltIn)
        {
            var item = new MenuItem { Header = $"{charm.Emoji}  {charm.Name}", Tag = charm.Id, IsCheckable = true };
            item.Click += (_, _) =>
            {
                onCharm(charm);
                RefreshChecks(charms);
            };
            charms.Items.Add(item);
        }

        var custom = new MenuItem { Header = "Custom emoji…" };
        custom.Click += (_, _) =>
        {
            onCustomEmoji();
            RefreshChecks(charms);
        };
        charms.Items.Add(new Separator());
        charms.Items.Add(custom);

        var reposition = new MenuItem { Header = "Reposition anchor" };
        reposition.Click += (_, _) => onReposition();

        var settings = new MenuItem { Header = $"Hotkey ({hotkeyText})…" };
        settings.Click += (_, _) => onSettings();

        _startupItem = new MenuItem { Header = "Launch on startup", IsCheckable = true, IsChecked = launchOnStartup };
        _startupItem.Click += (_, _) => onStartup(_startupItem.IsChecked);

        var quit = new MenuItem { Header = "Quit" };
        quit.Click += (_, _) => onQuit();

        var menu = new ContextMenu();
        menu.Items.Add(charms);
        menu.Items.Add(reposition);
        menu.Items.Add(settings);
        menu.Items.Add(_startupItem);
        menu.Items.Add(new Separator());
        menu.Items.Add(quit);
        menu.Opened += (_, _) => RefreshChecks(charms);

        _icon = new TaskbarIcon
        {
            ToolTipText = "HeroDangle",
            ContextMenu = menu
        };
        _icon.Icon = CreateNativeIcon();
        _icon.TrayMouseDoubleClick += (_, _) => onSettings();
    }

    private void RefreshChecks(MenuItem charms)
    {
        string id = _currentCharm().Id;
        foreach (var obj in charms.Items)
        {
            if (obj is MenuItem { Tag: string tag } item)
                item.IsChecked = tag == id;
        }
    }

    private static System.Drawing.Icon CreateNativeIcon()
    {
        // Render icon pixels with SkiaSharp
        var info = new SKImageInfo(32, 32, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info);
        SKCanvas canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);
        using var paint = new SKPaint { IsAntialias = true };
        paint.Color = new SKColor(24, 78, 168);
        canvas.DrawCircle(16, 16, 14, paint);
        paint.Color = SKColors.White;
        canvas.DrawCircle(16, 16, 8, paint);
        paint.Color = new SKColor(46, 156, 214);
        canvas.DrawCircle(16, 16, 5, paint);
        paint.Color = new SKColor(18, 22, 30);
        canvas.DrawCircle(16, 16, 2.4f, paint);

        // Convert Skia pixels -> System.Drawing.Bitmap -> System.Drawing.Icon
        using SKImage image = surface.Snapshot();
        using SKData data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = new MemoryStream(data.ToArray());
        using var gdiBmp = new System.Drawing.Bitmap(stream);
        return System.Drawing.Icon.FromHandle(gdiBmp.GetHicon());
    }

    public void Dispose() => _icon.Dispose();
}
