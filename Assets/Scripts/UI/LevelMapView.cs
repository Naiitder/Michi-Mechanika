using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// Draws the chapter map background: sketchy decorations plus the dashed route through the level nodes.
public class LevelMapView : VisualElement
{
    private static readonly Color DecorationColor = new Color32(0x64, 0x5c, 0x50, 0xff);
    private static readonly Color RouteColor = new Color32(0x2e, 0x2b, 0x25, 0xff);

    private readonly List<Vector2> route = new List<Vector2>();

    public LevelMapView()
    {
        pickingMode = PickingMode.Ignore;
        style.position = Position.Absolute;
        style.left = 0;
        style.top = 0;
        style.right = 0;
        style.bottom = 0;
        generateVisualContent += Draw;
    }

    public void SetRoute(IEnumerable<Vector2> points)
    {
        route.Clear();
        route.AddRange(points);
        MarkDirtyRepaint();
    }

    private void Draw(MeshGenerationContext ctx)
    {
        Painter2D p = ctx.painter2D;
        p.lineCap = LineCap.Round;
        p.lineJoin = LineJoin.Round;

        p.strokeColor = DecorationColor;
        p.lineWidth = 3;
        DrawDecorations(p);

        p.strokeColor = RouteColor;
        p.lineWidth = 4;
        DashedPolyline(p, route, 14, 12);
    }

    private static void DrawDecorations(Painter2D p)
    {
        // Big gear outline, bottom right.
        DashedCircle(p, new Vector2(470, 410), 46, 10, 9);
        p.BeginPath();
        p.Arc(new Vector2(470, 410), 18, Angle.Degrees(0), Angle.Degrees(360));
        p.Stroke();

        // Steam waves.
        foreach (float y in new[] { 60f, 86f })
        {
            p.BeginPath();
            p.MoveTo(new Vector2(530, y));
            p.QuadraticCurveTo(new Vector2(544, y - 14), new Vector2(558, y));
            p.QuadraticCurveTo(new Vector2(572, y + 14), new Vector2(586, y));
            p.Stroke();
        }

        // Pipe.
        p.BeginPath();
        p.MoveTo(new Vector2(300, 310));
        p.LineTo(new Vector2(300, 250));
        p.LineTo(new Vector2(370, 250));
        p.LineTo(new Vector2(370, 210));
        p.MoveTo(new Vector2(290, 310));
        p.LineTo(new Vector2(310, 310));
        p.MoveTo(new Vector2(360, 210));
        p.LineTo(new Vector2(380, 210));
        p.Stroke();

        // Mountains.
        p.BeginPath();
        p.MoveTo(new Vector2(40, 130));
        p.LineTo(new Vector2(64, 100));
        p.LineTo(new Vector2(84, 122));
        p.LineTo(new Vector2(106, 86));
        p.LineTo(new Vector2(136, 130));
        p.Stroke();
    }

    private static void DashedPolyline(Painter2D p, IReadOnlyList<Vector2> points, float dash, float gap)
    {
        if (points.Count < 2) return;

        p.BeginPath();
        float period = dash + gap;
        float phase = 0;
        for (int i = 1; i < points.Count; i++)
        {
            Vector2 a = points[i - 1], b = points[i];
            float length = Vector2.Distance(a, b);
            Vector2 dir = (b - a) / length;
            float t = 0;
            while (t < length)
            {
                float step = Mathf.Min(length - t, (phase < dash ? dash : period) - phase);
                if (phase < dash)
                {
                    p.MoveTo(a + dir * t);
                    p.LineTo(a + dir * (t + step));
                }
                t += step;
                phase = (phase + step) % period;
            }
        }
        p.Stroke();
    }

    private static void DashedCircle(Painter2D p, Vector2 center, float radius, float dash, float gap)
    {
        float dashDeg = dash / radius * Mathf.Rad2Deg;
        float periodDeg = (dash + gap) / radius * Mathf.Rad2Deg;
        p.BeginPath();
        for (float a = 0; a < 360; a += periodDeg)
        {
            Vector2 start = center + new Vector2(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad)) * radius;
            p.MoveTo(start);
            p.Arc(center, radius, Angle.Degrees(a), Angle.Degrees(Mathf.Min(a + dashDeg, 360)));
        }
        p.Stroke();
    }
}
