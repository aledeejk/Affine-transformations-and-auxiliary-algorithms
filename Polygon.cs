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
        //Сидорчик
        // Центр полигона (центроид — среднее арифметическое вершин)
        public PointF GetCenter()
        {
            if (Points.Count == 0) return PointF.Empty;

            float sumX = 0, sumY = 0;
            foreach (var p in Points)
            {
                sumX += p.X;
                sumY += p.Y;
            }
            return new PointF(sumX / Points.Count, sumY / Points.Count);
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

        // ============================================================
        // 8: Пересечения ребер
        // Проверяет, пересекаются ли два отрезка (a1,a2) и (b1,b2).
        // Если точка пересечения найдена, возвращает её.
        public static PointF? SegmentIntersection(PointF a1, PointF a2, PointF b1, PointF b2)
        {
            double d1x = a2.X - a1.X, d1y = a2.Y - a1.Y;
            double d2x = b2.X - b1.X, d2y = b2.Y - b1.Y;

            double denom = d1x * d2y - d1y * d2x;
            if (Math.Abs(denom) < 1e-9) return null; // параллельны

            double t = ((b1.X - a1.X) * d2y - (b1.Y - a1.Y) * d2x) / denom;
            double u = ((b1.X - a1.X) * d1y - (b1.Y - a1.Y) * d1x) / denom;

            if (t < 0 || t > 1 || u < 0 || u > 1) return null; // вне отрезков

            return new PointF((float)(a1.X + t * d1x), (float)(a1.Y + t * d1y));
        }

        // 9: Выпуклость полигона
        public bool IsConvex()
        {
            if (Kind != PolygonKind.Polygon || !Closed || Points.Count < 3)
                return false;

            int n = Points.Count;
            int sign = 0;

            for (int i = 0; i < n; i++)
            {
                PointF a = Points[i];
                PointF b = Points[(i + 1) % n];
                PointF c = Points[(i + 2) % n];

                double cross = (b.X - a.X) * (c.Y - b.Y) - (b.Y - a.Y) * (c.X - b.X);

                if (Math.Abs(cross) < 1e-6) continue; // коллинеарные

                int s = cross > 0 ? 1 : -1;
                if (sign == 0) sign = s;
                else if (sign != s) return false; // знак меняется → невыпуклый
            }
            return true;
        }

        // 10: Классификация точки относительно ребра
        // Возвращает:
        //   > 0 — точка СЛЕВА от направленного ребра a→b
        //   < 0 — точка СПРАВА
        //   = 0 — на прямой (в пределах eps)
        public static double SideOfEdge(PointF a, PointF b, PointF p)
        {
            // Векторное произведение (b - a) × (p - a)
            return (b.X - a.X) * (p.Y - a.Y) - (b.Y - a.Y) * (p.X - a.X);
        }

        // Строковое представление стороны
        public static string ClassifySide(PointF a, PointF b, PointF p, double eps = 1e-6)
        {
            double s = SideOfEdge(a, b, p);
            if (Math.Abs(s) < eps) return "на прямой";
            return s > 0 ? "СЛЕВА" : "СПРАВА";
        }

        // Минимальное расстояние от точки до контура полигона
        public float DistanceToPolygon(PointF p)
        {
            if (Points.Count == 0) return float.MaxValue;

            // Для точки — просто расстояние до неё
            if (Kind == PolygonKind.Point)
                return Distance(p, Points[0]);

            float min = float.MaxValue;

            // Расстояние до каждого ребра
            for (int i = 0; i < Points.Count - 1; i++)
            {
                float d = DistanceToSegment(p, Points[i], Points[i + 1]);
                if (d < min) min = d;
            }

            // Замыкающее ребро
            if (Closed && Points.Count > 2)
            {
                float d = DistanceToSegment(p, Points[^1], Points[0]);
                if (d < min) min = d;
            }

            // Если полигон — одна точка (на всякий случай)
            if (Points.Count == 1)
                min = Distance(p, Points[0]);

            return min;
        }
        // ============================================================
    }
}