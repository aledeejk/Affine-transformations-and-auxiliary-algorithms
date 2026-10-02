using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Lab4Graph
{
    //=======================================
    //Деева
    //=======================================
    public enum PolygonKind
    {
        Point,
        Edge,
        Polygon
    }

    public class Polygon
    {
        public List<PointF> Points { get; } = new List<PointF>();
        public List<PointF>? Snapshot { get; set; }
        public PolygonKind Kind { get; set; } = PolygonKind.Polygon;
        public bool Closed { get; set; } = false;
        public bool Selected { get; set; } = false;
        public Color Color { get; set; } = Color.Black;

        public Polygon() { }

        public Polygon(PolygonKind kind, Color color)
        {
            Kind = kind;
            Color = color;
        }

        // Глубокая копия (для истории отмены)
        public Polygon Clone()
        {
            var copy = new Polygon
            {
                Kind = this.Kind,
                Closed = this.Closed,
                Color = this.Color,
                Selected = false
            };
            copy.Points.AddRange(this.Points);
            if (this.Snapshot != null)
                copy.Snapshot = new List<PointF>(this.Snapshot);
            return copy;
        }

        public void SaveSnapshot()
        {
            Snapshot = new List<PointF>(Points);
        }

        public void ClearSnapshot()
        {
            Snapshot = null;
        }

        public void ApplyTransform(AffineMatrix matrix)
        {
            for (int i = 0; i < Points.Count; i++)
                Points[i] = matrix.Apply(Points[i]);
        }

        // Ограничивающий прямоугольник
        public RectangleF GetBounds()
        {
            if (Points.Count == 0) return RectangleF.Empty;
            float minX = Points[0].X, maxX = Points[0].X;
            float minY = Points[0].Y, maxY = Points[0].Y;
            foreach (var p in Points)
            {
                if (p.X < minX) minX = p.X;
                if (p.X > maxX) maxX = p.X;
                if (p.Y < minY) minY = p.Y;
                if (p.Y > maxY) maxY = p.Y;
            }
            return new RectangleF(minX, minY, maxX - minX, maxY - minY);
        }

        // Попадает ли точка в полигон (для выделения кликом)
        public bool ContainsPoint(PointF point, float tolerance)
        {
            if (Points.Count == 0) return false;

            if (Kind == PolygonKind.Point)
            {
                float dx = Points[0].X - point.X;
                float dy = Points[0].Y - point.Y;
                return dx * dx + dy * dy <= tolerance * tolerance;
            }

            if (Kind == PolygonKind.Edge && Points.Count >= 2)
                return DistanceToSegment(point, Points[0], Points[1]) <= tolerance;

            // Полигон — проверяем близость к рёбрам
            for (int i = 0; i < Points.Count - 1; i++)
                if (DistanceToSegment(point, Points[i], Points[i + 1]) <= tolerance)
                    return true;

            if (Closed && Points.Count > 2)
                if (DistanceToSegment(point, Points[^1], Points[0]) <= tolerance)
                    return true;

            // Проверка "точка внутри" — через ray casting
            if (Closed && Points.Count >= 3)
                return IsPointInsidePolygon(point, Points);

            return false;
        }

        private static float DistanceToSegment(PointF p, PointF a, PointF b)
        {
            float dx = b.X - a.X;
            float dy = b.Y - a.Y;
            float lengthSq = dx * dx + dy * dy;

            if (lengthSq < 0.0001f)
                return Distance(p, a);

            float t = ((p.X - a.X) * dx + (p.Y - a.Y) * dy) / lengthSq;
            t = Math.Max(0, Math.Min(1, t));

            float projX = a.X + t * dx;
            float projY = a.Y + t * dy;

            return Distance(p, new PointF(projX, projY));
        }

        private static float Distance(PointF a, PointF b)
        {
            float dx = a.X - b.X;
            float dy = a.Y - b.Y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        private static bool IsPointInsidePolygon(PointF p, List<PointF> polygon)
        {
            bool inside = false;
            int n = polygon.Count;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                if (((polygon[i].Y > p.Y) != (polygon[j].Y > p.Y)) &&
                    (p.X < (polygon[j].X - polygon[i].X) * (p.Y - polygon[i].Y) /
                           (polygon[j].Y - polygon[i].Y) + polygon[i].X))
                {
                    inside = !inside;
                }
            }
            return inside;
        }

        public void Draw(Graphics g)
        {
            if (Points.Count == 0) return;

            // 1) Пунктир — положение «до»
            if (Snapshot != null && Snapshot.Count > 0)
            {
                using var ghostPen = new Pen(Color.LightGray, 1f) { DashStyle = DashStyle.Dash };
                for (int i = 0; i < Snapshot.Count - 1; i++)
                    g.DrawLine(ghostPen, Snapshot[i], Snapshot[i + 1]);
                if (Closed && Snapshot.Count > 2)
                    g.DrawLine(ghostPen, Snapshot[^1], Snapshot[0]);
                foreach (var p in Snapshot)
                    g.DrawEllipse(ghostPen, p.X - 2, p.Y - 2, 4, 4);
            }

            // 2) Основной полигон
            using var pen = new Pen(Color, 2f);
            using var brush = new SolidBrush(Color);

            if (Kind == PolygonKind.Point)
            {
                var p = Points[0];
                g.FillEllipse(brush, p.X - 4, p.Y - 4, 8, 8);
            }
            else
            {
                for (int i = 0; i < Points.Count - 1; i++)
                    g.DrawLine(pen, Points[i], Points[i + 1]);
                if (Closed && Points.Count > 2)
                    g.DrawLine(pen, Points[^1], Points[0]);
                foreach (var p in Points)
                    g.FillRectangle(brush, p.X - 2, p.Y - 2, 4, 4);
            }

            // 3) Выделение — оранжевая рамка
            if (Selected)
            {
                var bounds = GetBounds();
                using var selectPen = new Pen(Color.OrangeRed, 2f) { DashStyle = DashStyle.Dot };
                g.DrawRectangle(selectPen, bounds.X - 4, bounds.Y - 4,
                                bounds.Width + 8, bounds.Height + 8);
            }

            // 4) Координаты вершин
            using var font = new Font("Segoe UI", 8);
            using var textBrush = new SolidBrush(Color.DarkBlue);
            foreach (var p in Points)
                g.DrawString($"({p.X:0},{p.Y:0})", font, textBrush, p.X + 6, p.Y + 6);
        }
    }
}