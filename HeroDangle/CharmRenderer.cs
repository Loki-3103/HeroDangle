using System.IO;
using SkiaSharp;

namespace HeroDangle;

public static class CharmRenderer
{
    public static void Draw(
        SKCanvas canvas,
        ReadOnlySpan<Vec2> points,
        CharmDefinition charm,
        float dpiScale,
        float presence,
        float swayAngle)
    {
        if (points.Length < 2 || presence <= 0.01f)
            return;

        canvas.Clear(SKColors.Transparent);
        DrawThread(canvas, points, dpiScale);
        DrawNail(canvas, points[0], dpiScale);
        DrawCharm(canvas, points[^1], charm, dpiScale, swayAngle);
    }

    public static void AddCatmullRom(SKPath path, ReadOnlySpan<Vec2> points)
    {
        if (points.Length < 2)
            return;

        path.MoveTo(points[0].X, points[0].Y);
        for (int i = 0; i < points.Length - 1; i++)
        {
            Vec2 p0 = points[Math.Max(i - 1, 0)];
            Vec2 p1 = points[i];
            Vec2 p2 = points[i + 1];
            Vec2 p3 = points[Math.Min(i + 2, points.Length - 1)];

            // Catmull-Rom to cubic Bezier: tangents scaled by 1/6.
            float c1x = p1.X + (p2.X - p0.X) / 6f;
            float c1y = p1.Y + (p2.Y - p0.Y) / 6f;
            float c2x = p2.X - (p3.X - p1.X) / 6f;
            float c2y = p2.Y - (p3.Y - p1.Y) / 6f;
            path.CubicTo(c1x, c1y, c2x, c2y, p2.X, p2.Y);
        }
    }

    private static void DrawThread(SKCanvas canvas, ReadOnlySpan<Vec2> points, float dpi)
    {
        using var path = new SKPath();
        AddCatmullRom(path, points);

        using var shadow = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2.4f * dpi,
            Color = new SKColor(0, 0, 0, 50),
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round
        };
        canvas.Save();
        canvas.Translate(1.2f * dpi, 2.4f * dpi);
        canvas.DrawPath(path, shadow);
        canvas.Restore();

        using var thread = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1.7f * dpi,
            Color = new SKColor(214, 184, 128, 230),
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round
        };
        canvas.DrawPath(path, thread);

        using var highlight = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 0.6f * dpi,
            Color = new SKColor(255, 245, 220, 140),
            StrokeCap = SKStrokeCap.Round
        };
        canvas.Save();
        canvas.Translate(-0.4f * dpi, -0.4f * dpi);
        canvas.DrawPath(path, highlight);
        canvas.Restore();
    }

    private static void DrawNail(SKCanvas canvas, Vec2 p, float dpi)
    {
        using var paint = new SKPaint { IsAntialias = true };
        paint.Color = new SKColor(0, 0, 0, 40);
        canvas.DrawCircle(p.X + 0.8f * dpi, p.Y + 1.6f * dpi, 4.2f * dpi, paint);
        paint.Color = new SKColor(92, 92, 98);
        canvas.DrawCircle(p.X, p.Y, 3.6f * dpi, paint);
        paint.Color = new SKColor(210, 210, 218);
        canvas.DrawCircle(p.X - 0.8f * dpi, p.Y - 0.8f * dpi, 1.3f * dpi, paint);
    }

    private static void DrawCharm(SKCanvas canvas, Vec2 p, CharmDefinition charm, float dpi, float sway)
    {
        float r = 22f * dpi;
        float ox = MathF.Sin(sway) * 3.5f * dpi;
        float oy = 4.5f * dpi + MathF.Abs(MathF.Sin(sway)) * 1.5f * dpi;

        canvas.Save();
        canvas.Translate(p.X, p.Y);
        canvas.RotateDegrees(sway * (180f / MathF.PI) * 0.35f);

        if (charm.Kind != CharmKind.Image)
        {
            using var shadow = new SKPaint
            {
                IsAntialias = true,
                Color = new SKColor(0, 0, 0, 55),
                MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 3.5f * dpi)
            };
            canvas.DrawCircle(ox, oy, r * 0.92f, shadow);
        }

        if (charm.Kind == CharmKind.Emoji)
            DrawEmoji(canvas, charm.Emoji, r, dpi);
        else if (charm.Kind == CharmKind.Image)
            DrawImage(canvas, r, dpi);
        else
            DrawVectorCharm(canvas, charm.Id, r, dpi);

        canvas.Restore();
    }

    private static void DrawEmoji(SKCanvas canvas, string emoji, float r, float dpi)
    {
        using var font = new SKFont(SKTypeface.FromFamilyName("Segoe UI Emoji"), r * 1.55f)
        {
            Edging = SKFontEdging.SubpixelAntialias,
            Hinting = SKFontHinting.Slight
        };
        using var paint = new SKPaint
        {
            IsAntialias = true,
            TextAlign = SKTextAlign.Center
        };

        canvas.DrawText(emoji, 0, r * 0.55f, font, paint);
    }

    private static void DrawVectorCharm(SKCanvas canvas, string id, float r, float dpi)
    {
        using var fill = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill };
        using var stroke = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1.4f * dpi,
            StrokeJoin = SKStrokeJoin.Round,
            StrokeCap = SKStrokeCap.Round
        };

        switch (id)
        {
            case "nazar":
                fill.Color = new SKColor(24, 78, 168);
                canvas.DrawCircle(0, 0, r, fill);
                fill.Color = new SKColor(244, 246, 250);
                canvas.DrawCircle(0, 0, r * 0.68f, fill);
                fill.Color = new SKColor(46, 156, 214);
                canvas.DrawCircle(0, 0, r * 0.42f, fill);
                fill.Color = new SKColor(18, 22, 30);
                canvas.DrawCircle(0, 0, r * 0.18f, fill);
                fill.Color = new SKColor(255, 255, 255, 180);
                canvas.DrawCircle(-r * 0.18f, -r * 0.22f, r * 0.12f, fill);
                break;

            case "lemon":
                canvas.Save();
                canvas.RotateDegrees(-18);
                canvas.Scale(1.15f, 0.82f);
                fill.Color = new SKColor(244, 206, 66);
                canvas.DrawCircle(0, 0, r, fill);
                stroke.Color = new SKColor(214, 164, 32);
                canvas.DrawCircle(0, 0, r, stroke);
                fill.Color = new SKColor(120, 168, 64);
                using (var leaf = new SKPath())
                {
                    leaf.MoveTo(r * 0.15f, -r * 1.05f);
                    leaf.CubicTo(r * 0.55f, -r * 1.35f, r * 0.85f, -r * 0.85f, r * 0.35f, -r * 0.7f);
                    canvas.DrawPath(leaf, fill);
                }
                canvas.Restore();
                break;

            case "chili":
                fill.Color = new SKColor(214, 48, 49);
                using (var chili = new SKPath())
                {
                    chili.MoveTo(-r * 0.15f, -r * 0.55f);
                    chili.CubicTo(r * 0.95f, -r * 0.2f, r * 0.7f, r * 0.95f, -r * 0.05f, r * 0.95f);
                    chili.CubicTo(-r * 0.85f, r * 0.55f, -r * 0.7f, -r * 0.15f, -r * 0.15f, -r * 0.55f);
                    canvas.DrawPath(chili, fill);
                }
                fill.Color = new SKColor(70, 140, 58);
                using (var stem = new SKPath())
                {
                    stem.MoveTo(-r * 0.1f, -r * 0.5f);
                    stem.CubicTo(r * 0.05f, -r * 0.95f, r * 0.45f, -r * 1.05f, r * 0.55f, -r * 0.7f);
                    stroke.Color = fill.Color;
                    stroke.StrokeWidth = 3.2f * dpi;
                    canvas.DrawPath(stem, stroke);
                }
                break;

            case "horseshoe":
                stroke.Color = new SKColor(196, 154, 74);
                stroke.StrokeWidth = 7.2f * dpi;
                using (var shoe = new SKPath())
                {
                    shoe.MoveTo(-r * 0.62f, r * 0.15f);
                    shoe.CubicTo(-r * 0.85f, -r * 0.85f, r * 0.85f, -r * 0.85f, r * 0.62f, r * 0.15f);
                    canvas.DrawPath(shoe, stroke);
                }
                stroke.StrokeWidth = 3.4f * dpi;
                canvas.DrawLine(-r * 0.62f, r * 0.15f, -r * 0.62f, r * 0.55f, stroke);
                canvas.DrawLine(r * 0.62f, r * 0.15f, r * 0.62f, r * 0.55f, stroke);
                break;

            case "clover":
                fill.Color = new SKColor(46, 158, 78);
                DrawLeaf(canvas, fill, -r * 0.32f, -r * 0.18f, r * 0.38f);
                DrawLeaf(canvas, fill, r * 0.32f, -r * 0.18f, r * 0.38f);
                DrawLeaf(canvas, fill, 0, r * 0.32f, r * 0.38f);
                DrawLeaf(canvas, fill, 0, -r * 0.52f, r * 0.34f);
                stroke.Color = new SKColor(36, 120, 58);
                stroke.StrokeWidth = 2.2f * dpi;
                canvas.DrawLine(0, r * 0.2f, 0, r * 0.95f, stroke);
                break;

            case "hamsa":
                fill.Color = new SKColor(232, 214, 176);
                using (var hand = new SKPath())
                {
                    hand.MoveTo(-r * 0.55f, r * 0.15f);
                    hand.CubicTo(-r * 0.7f, -r * 0.1f, -r * 0.35f, -r * 0.95f, -r * 0.18f, -r * 0.95f);
                    hand.LineTo(-r * 0.08f, -r * 0.2f);
                    hand.LineTo(0, -r * 1.05f);
                    hand.LineTo(r * 0.08f, -r * 0.2f);
                    hand.LineTo(r * 0.18f, -r * 0.95f);
                    hand.CubicTo(r * 0.35f, -r * 0.95f, r * 0.7f, -r * 0.1f, r * 0.55f, r * 0.15f);
                    hand.CubicTo(r * 0.4f, r * 0.95f, -r * 0.4f, r * 0.95f, -r * 0.55f, r * 0.15f);
                    canvas.DrawPath(hand, fill);
                }
                fill.Color = new SKColor(46, 120, 186);
                canvas.DrawCircle(0, r * 0.22f, r * 0.22f, fill);
                fill.Color = SKColors.White;
                canvas.DrawCircle(0, r * 0.22f, r * 0.12f, fill);
                fill.Color = new SKColor(20, 24, 32);
                canvas.DrawCircle(0, r * 0.22f, r * 0.05f, fill);
                break;

            default:
                fill.Color = new SKColor(240, 200, 80);
                canvas.DrawCircle(0, 0, r, fill);
                break;
        }
    }

    private static void DrawLeaf(SKCanvas canvas, SKPaint fill, float x, float y, float r)
    {
        canvas.DrawCircle(x, y, r, fill);
    }

    private static SKBitmap? _batman;

    private static SKBitmap? LoadImageCharm()
    {
        if (_batman is not null)
            return _batman;

        try
        {
            string path = Path.Combine(AppContext.BaseDirectory, "Assets", "batman.png");
            if (!File.Exists(path))
                return null;

            _batman = SKBitmap.Decode(path);
        }
        catch
        {
            _batman = null;
        }

        return _batman;
    }

    private static void DrawImage(SKCanvas canvas, float r, float dpi)
    {
        SKBitmap? image = LoadImageCharm();
        if (image is null)
        {
            DrawEmoji(canvas, "🦇", r, dpi);
            return;
        }

        float scale = (r * 2.2f + 9f * dpi) / image.Height;
        float w = image.Width * scale;
        float h = image.Height * scale;
        using var paint = new SKPaint
        {
            ColorFilter = SKColorFilter.CreateBlendMode(new SKColor(60, 60, 60), SKBlendMode.SrcIn)
        };
        canvas.DrawBitmap(image, new SKRect(-w / 2f, -h / 2f, w / 2f, h / 2f), paint);
    }
}
