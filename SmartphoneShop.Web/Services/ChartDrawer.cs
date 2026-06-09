using SkiaSharp;

namespace SmartphoneShop.Web.Services;

public static class ChartDrawer
{
    private static readonly SKColor Background = new(0x1A, 0x1A, 0x1A);
    private static readonly SKColor TextColor = new(0xF0, 0xF0, 0xF0);
    private static readonly SKColor Accent = new(0xD4, 0x41, 0x77);
    private static readonly SKColor GridColor = new(0x33, 0x33, 0x33);

    private static SKTypeface GetFont(bool bold = false)
    {
        var weight = bold ? 700 : 400;
        return SKTypeface.FromFamilyName("Arial", new SKFontStyle(weight, 5, SKFontStyleSlant.Upright));
    }

    private static readonly SKColor[] PieColors =
    [
        new(0xD4, 0x41, 0x77), new(0xE8, 0x6C, 0x00), new(0xF0, 0xC0, 0x00),
        new(0x00, 0xB8, 0x8E), new(0x00, 0x7B, 0xE0), new(0x7B, 0x4F, 0xD4),
        new(0xE0, 0x40, 0x60), new(0x40, 0x80, 0x60), new(0xA0, 0x70, 0x40),
        new(0x60, 0x70, 0x80), new(0x90, 0x50, 0x60),
    ];

    public static byte[] DrawPieChart(string title, string[] labels, int[] values, int width = 600, int height = 400)
    {
        using var surface = SKSurface.Create(new SKImageInfo(width, height));
        var canvas = surface.Canvas;
        canvas.Clear(Background);

        using var titlePaint = new SKPaint { Color = TextColor, TextSize = 18, IsAntialias = true, Typeface = GetFont(true) };
        canvas.DrawText(title, 20, 30, titlePaint);

        var total = values.Sum();
        if (total == 0) return surface.Snapshot().Encode(SKEncodedImageFormat.Png, 100).ToArray();

        var cx = 220f;
        var cy = 240f;
        var radius = 140f;
        var rect = new SKRect(cx - radius, cy - radius, cx + radius, cy + radius);

        float startAngle = -90;
        for (int i = 0; i < values.Length; i++)
        {
            float sweepAngle = (float)values[i] / total * 360;
            using var path = new SKPath();
            path.MoveTo(cx, cy);
            path.ArcTo(rect, startAngle, sweepAngle, false);
            path.Close();

            using var fillPaint = new SKPaint { Color = PieColors[i % PieColors.Length], IsAntialias = true, Style = SKPaintStyle.Fill };
            canvas.DrawPath(path, fillPaint);

            if (sweepAngle > 5)
            {
                var midAngle = (startAngle + sweepAngle / 2) * Math.PI / 180;
                var labelRadius = radius * 0.65f;
                var lx = cx + (float)Math.Cos(midAngle) * labelRadius;
                var ly = cy + (float)Math.Sin(midAngle) * labelRadius;
                var pct = (float)values[i] / total * 100;
                using var valPaint = new SKPaint { Color = SKColors.White, TextSize = 13, IsAntialias = true, Typeface = GetFont(true) };
                var text = $"{pct:F0}%";
                var tw = valPaint.MeasureText(text);
                canvas.DrawText(text, lx - tw / 2, ly + 4, valPaint);
            }
            startAngle += sweepAngle;
        }

        using var linePaint = new SKPaint { Color = new SKColor(0x44, 0x44, 0x44), StrokeWidth = 1, Style = SKPaintStyle.Stroke, IsAntialias = true };
        canvas.DrawCircle(cx, cy, radius, linePaint);

        var legY = 60;
        for (int i = 0; i < labels.Length; i++)
        {
            using var boxPaint = new SKPaint { Color = PieColors[i % PieColors.Length], Style = SKPaintStyle.Fill };
            canvas.DrawRect(new SKRect(420, legY, 436, legY + 14), boxPaint);

            using var labelPaint = new SKPaint { Color = TextColor, TextSize = 12, IsAntialias = true, Typeface = GetFont() };
            canvas.DrawText($"{labels[i]} ({values[i]})", 442, legY + 12, labelPaint);
            legY += 22;
        }

        return surface.Snapshot().Encode(SKEncodedImageFormat.Png, 100).ToArray();
    }

    public static byte[] DrawColumnChart(string title, string[] labels, int[] values, int width = 600, int height = 400)
    {
        return DrawBarChartInternal(title, labels, values, width, height, true);
    }

    public static byte[] DrawBarChart(string title, string[] labels, int[] values, int width = 600, int height = 400)
    {
        return DrawBarChartInternal(title, labels, values, width, height, false);
    }

    private static byte[] DrawBarChartInternal(string title, string[] labels, int[] values, int width, int height, bool vertical)
    {
        using var surface = SKSurface.Create(new SKImageInfo(width, height));
        var canvas = surface.Canvas;
        canvas.Clear(Background);

        using var titlePaint = new SKPaint { Color = TextColor, TextSize = 18, IsAntialias = true, Typeface = GetFont(true) };
        canvas.DrawText(title, 20, 30, titlePaint);

        var maxVal = values.Length > 0 ? values.Max() : 1;
        var marginLeft = vertical ? 50f : 120f;
        var marginRight = 20f;
        var marginTop = 50f;
        var marginBottom = vertical ? 80f : 40f;

        var chartW = width - marginLeft - marginRight;
        var chartH = height - marginTop - marginBottom;

        using var axisPaint = new SKPaint { Color = new SKColor(0x55, 0x55, 0x55), StrokeWidth = 1, Style = SKPaintStyle.Stroke };
        canvas.DrawLine(marginLeft, marginTop, marginLeft, marginTop + chartH, axisPaint);
        canvas.DrawLine(marginLeft, marginTop + chartH, marginLeft + chartW, marginTop + chartH, axisPaint);

        var gridCount = 4;
        for (int i = 0; i <= gridCount; i++)
        {
            var y = marginTop + chartH - (chartH * i / gridCount);
            using var gridPaint = new SKPaint { Color = GridColor, StrokeWidth = 0.5f, Style = SKPaintStyle.Stroke };
            canvas.DrawLine(marginLeft, y, marginLeft + chartW, y, gridPaint);

            var val = maxVal * i / gridCount;
            using var valPaint = new SKPaint { Color = TextColor, TextSize = 11, IsAntialias = true, Typeface = GetFont() };
            canvas.DrawText(val.ToString("N0"), 5, y + 4, valPaint);
        }

        var count = labels.Length;
        using var barPaint = new SKPaint { Color = Accent, Style = SKPaintStyle.Fill };
        using var labelPaint = new SKPaint { Color = TextColor, TextSize = vertical ? 10 : 11, IsAntialias = true, Typeface = GetFont() };

        if (vertical)
        {
            var barW = Math.Min(chartW / count * 0.6f, 40f);
            var gap = (chartW - barW * count) / (count + 1);
            for (int i = 0; i < count; i++)
            {
                var barH = (float)values[i] / maxVal * chartH;
                var x = marginLeft + gap + i * (barW + gap);
                var y = marginTop + chartH - barH;
                canvas.DrawRect(new SKRect(x, y, x + barW, marginTop + chartH), barPaint);

                var label = labels[i].Length > 10 ? labels[i][..10] + ".." : labels[i];
                var tw = labelPaint.MeasureText(label);
                canvas.Save();
                canvas.RotateDegrees(45, x + barW / 2, marginTop + chartH + 5);
                canvas.DrawText(label, x + barW / 2 - tw / 2, marginTop + chartH + 16, labelPaint);
                canvas.Restore();
            }
        }
        else
        {
            var barH = Math.Min(chartH / count * 0.6f, 25f);
            var gap = (chartH - barH * count) / (count + 1);
            for (int i = 0; i < count; i++)
            {
                var barW = (float)values[i] / maxVal * chartW;
                var y = marginTop + gap + i * (barH + gap);
                canvas.DrawRect(new SKRect(marginLeft, y, marginLeft + barW, y + barH), barPaint);

                var label = labels[i].Length > 18 ? labels[i][..18] + ".." : labels[i];
                canvas.DrawText(label, 5, y + barH / 2 + 4, labelPaint);
            }
        }

        return surface.Snapshot().Encode(SKEncodedImageFormat.Png, 100).ToArray();
    }
}
