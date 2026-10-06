using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace Lab4Graph
{
    //=======================================
    //Деева
    //=======================================
    public partial class MainForm : Form
    {
        // Сцена
        private readonly List<Polygon> polygons = new List<Polygon>();

        // Режим создания
        private PolygonKind currentKind = PolygonKind.Polygon;
        private Polygon? currentPolygon = null;

        // Режим выделения (включается галочкой)
        private bool selectMode = false;

        // Холст
        private Bitmap? canvasBitmap;

        // Информация о последнем преобразовании
        private string lastTransformInfo = "";

        // История для отмены
        private const int MaxUndoSteps = 50;
        private readonly Stack<List<Polygon>> undoStack = new Stack<List<Polygon>>();

        // ============================================================
        // Пункты 8–10: режимы проверок
        private enum CheckMode { None, Intersect, PointInPoly, SideOfEdge }
        private CheckMode checkMode = CheckMode.None;

        // Для пункта 8: первое ребро задаётся кликами
        private PointF? intersectFirstPoint = null;
        private PointF? intersectSecondPoint = null;

        // Для пункта 10: ребро (два клика) и проверяемая точка (третий клик)
        private PointF? sideFirstPoint = null;
        private PointF? sideSecondPoint = null;

        // Сохранённые результаты для отрисовки (чтобы не терялись при Redraw)
        private readonly List<(PointF pt, string text)> checkHints =
            new List<(PointF, string)>();
        private readonly List<(PointF a, PointF b, PointF p, string text)> sideResults = new List<(PointF, PointF, PointF, string)>();
        
        // ============================================================

        public MainForm()
        {
            InitializeComponent();
            SetupCanvas();
            SetupEvents();
        }

        // ============================================================
        // Инициализация
        // ============================================================
        private void SetupCanvas()
        {
            if (canvas.Width <= 0 || canvas.Height <= 0) return;

            canvasBitmap = new Bitmap(canvas.Width, canvas.Height);
            using var g = Graphics.FromImage(canvasBitmap);
            g.Clear(Color.White);
            canvas.Image = canvasBitmap;
        }

        private void SetupEvents()
        {
            canvas.MouseDown += Canvas_MouseDown;
            canvas.MouseMove += Canvas_MouseMove;
            canvas.SizeChanged += Canvas_SizeChanged;

            btnClear.Click += BtnClear_Click;
            btnUndo.Click += BtnUndo_Click;

            chkSelectMode.CheckedChanged += ChkSelectMode_CheckedChanged;

            btnPointMode.Click += (s, e) => SetMode(PolygonKind.Point);
            btnEdgeMode.Click += (s, e) => SetMode(PolygonKind.Edge);
            btnPolygonMode.Click += (s, e) => SetMode(PolygonKind.Polygon);

            btnTranslate.Click += BtnTranslate_Click;
            btnRotate.Click += BtnRotate_Click;
            btnScale.Click += BtnScale_Click;
            btnReflectY.Click += BtnReflectY_Click;

            // ============================================================
            // Пункты 8–10
            btnIntersectMode.Click += (s, e) => SetCheckMode(CheckMode.Intersect);
            btnPointInPolyMode.Click += (s, e) => SetCheckMode(CheckMode.PointInPoly);
            btnSideMode.Click += (s, e) => SetCheckMode(CheckMode.SideOfEdge);
            btnClearHints.Click += (s, e) =>
            {
                checkHints.Clear();
                sideResults.Clear();
                intersectFirstPoint = intersectSecondPoint = null;
                sideFirstPoint = sideSecondPoint = null;
                Redraw();
                statusLabel.Text = "Подсказки очищены.";
            };
            // ============================================================
        }

        // ============================================================
        // Галочка выделения
        // ============================================================
        private void ChkSelectMode_CheckedChanged(object? sender, EventArgs e)
        {
            selectMode = chkSelectMode.Checked;

            if (selectMode)
            {
                currentPolygon = null;
                statusLabel.Text = "Режим ВЫДЕЛЕНИЯ. Кликните по полигонам. Повторный клик — снять.";
            }
            else
            {
                DeselectAll();
                statusLabel.Text = "Режим выделения выключен. Преобразования — ко ВСЕМ полигонам.";
            }

            Redraw();
        }

        private void SetMode(PolygonKind kind)
        {
            currentKind = kind;
            currentPolygon = null;
            statusLabel.Text = $"Режим: {kind}. Кликните для добавления вершин.";
            Redraw();
        }

        // ============================================================
        // История отмены
        // ============================================================
        private void SaveUndoState()
        {
            var snapshot = new List<Polygon>();
            foreach (var p in polygons)
                snapshot.Add(p.Clone());

            undoStack.Push(snapshot);

            if (undoStack.Count > MaxUndoSteps)
            {
                var temp = new Stack<List<Polygon>>();
                int keep = MaxUndoSteps;
                while (undoStack.Count > 0 && keep-- > 0)
                    temp.Push(undoStack.Pop());
                undoStack.Clear();
                while (temp.Count > 0)
                    undoStack.Push(temp.Pop());
            }
        }

        private void BtnUndo_Click(object? sender, EventArgs e)
        {
            if (undoStack.Count == 0)
            {
                MessageBox.Show("Нечего отменять.");
                return;
            }

            var previous = undoStack.Pop();

            polygons.Clear();
            polygons.AddRange(previous);

            lastTransformInfo = "Отменено последнее действие";
            Redraw();
            statusLabel.Text = $"Отменено. Осталось шагов: {undoStack.Count}";
        }

        // ============================================================
        // Пункты 8–10: установка режима проверки
        private void SetCheckMode(CheckMode mode)
        {
            checkMode = mode;
            currentPolygon = null;

            // сброс промежуточных состояний
            intersectFirstPoint = intersectSecondPoint = null;
            sideFirstPoint = sideSecondPoint = null;

            switch (mode)
            {
                case CheckMode.Intersect:
                    statusLabel.Text =
                        "Поиск пересечения с другими рёбрами. ЛКМ - выбрать начало ребра, повторный клик - конец. ПКМ - выход из режима проверки.";
                    break;
                case CheckMode.PointInPoly:
                    statusLabel.Text =
                        "Проверка принадлежности точки всем полигонам. ЛКМ - выбрать точку. ПКМ - выход из режима проверки.";
                    break;
                case CheckMode.SideOfEdge:
                    statusLabel.Text =
                        "ЛКМ выбрать 2 точки (ребро), затем 3-ю — проверяемую точку. ПКМ - выход из режима проверки.";
                    break;
                default:
                    statusLabel.Text = "Режим проверок выключен.";
                    break;
            }
            Redraw();
        }
        // ============================================================

        // ============================================================
        // ВЫДЕЛЕНИЕ — можно несколько
        // ============================================================

        // Найти полигон под курсором (верхний — проверяется первым)
        private Polygon? FindPolygonAt(PointF pt, float tolerance)
        {
            for (int i = polygons.Count - 1; i >= 0; i--)
            {
                if (polygons[i].ContainsPoint(pt, tolerance))
                    return polygons[i];
            }
            return null;
        }

        // Список ВСЕХ выделенных полигонов
        private List<Polygon> GetSelected()
        {
            return polygons.Where(p => p.Selected).ToList();
        }

        // ПЕРЕКЛЮЧИТЬ выделение одного полигона (toggle)
        // НЕ трогает остальные!
        private void ToggleSelection(Polygon poly)
        {
            poly.Selected = !poly.Selected;

            int count = GetSelected().Count;
            if (count == 0)
                statusLabel.Text = "Ничего не выделено. Преобразования — ко ВСЕМ.";
            else
                statusLabel.Text = $"Выделено полигонов: {count}. Преобразования — к ним.";

            Redraw();
        }

        // Снять выделение со всех
        private void DeselectAll()
        {
            foreach (var p in polygons)
                p.Selected = false;

            Redraw();
        }

        // ============================================================
        // Мышь
        // ============================================================
        private void Canvas_MouseDown(object? sender, MouseEventArgs e)
        {
            var pt = new PointF(e.X, e.Y);

            if (e.Button == MouseButtons.Right)
            {
                // ПКМ — выйти из режима проверки
                if (checkMode != CheckMode.None)
                {
                    SetCheckMode(CheckMode.None);
                    return;
                }
                HandleRightClick();
                return;
            }

            if (e.Button != MouseButtons.Left) return;

            // === Режимы проверок 8–10 ===
            if (checkMode == CheckMode.Intersect) { HandleIntersectClick(pt); return; }
            if (checkMode == CheckMode.PointInPoly) { HandlePointInPolyClick(pt); return; }
            if (checkMode == CheckMode.SideOfEdge) { HandleSideClick(pt); return; }

            // === Старое поведение ===
            if (selectMode)
            {
                var hit = FindPolygonAt(pt, 8f);
                if (hit != null) ToggleSelection(hit);
                else { DeselectAll(); statusLabel.Text = "Выделение снято. Преобразования — ко ВСЕМ."; }
                return;
            }

            HandleLeftClick(pt);
        }

        private void HandleLeftClick(PointF pt)
        {
            switch (currentKind)
            {
                case PolygonKind.Point:
                    SaveUndoState();
                    var p = new Polygon(PolygonKind.Point, Color.Blue);
                    p.Points.Add(pt);
                    polygons.Add(p);
                    statusLabel.Text = $"Точка добавлена: ({pt.X:0}, {pt.Y:0})";
                    break;

                case PolygonKind.Edge:
                    if (currentPolygon == null)
                    {
                        SaveUndoState();
                        currentPolygon = new Polygon(PolygonKind.Edge, Color.Green);
                        currentPolygon.Points.Add(pt);
                        statusLabel.Text = "Первая вершина ребра задана. Кликните вторую.";
                    }
                    else
                    {
                        currentPolygon.Points.Add(pt);
                        polygons.Add(currentPolygon);
                        currentPolygon = null;
                        statusLabel.Text = "Ребро добавлено.";
                    }
                    break;

                case PolygonKind.Polygon:
                    if (currentPolygon == null)
                    {
                        SaveUndoState();
                        currentPolygon = new Polygon(PolygonKind.Polygon, Color.Black);
                        currentPolygon.Points.Add(pt);
                        statusLabel.Text = "Первая вершина полигона. ЛКМ — добавить, ПКМ — замкнуть.";
                    }
                    else
                    {
                        currentPolygon.Points.Add(pt);
                        statusLabel.Text = $"Вершин: {currentPolygon.Points.Count}. ЛКМ — добавить, ПКМ — замкнуть.";
                    }
                    break;
            }

            Redraw();
        }

        private void HandleRightClick()
        {
            if (currentPolygon != null && currentPolygon.Kind == PolygonKind.Polygon)
            {
                if (currentPolygon.Points.Count >= 3)
                {
                    SaveUndoState();
                    currentPolygon.Closed = true;
                    polygons.Add(currentPolygon);
                    statusLabel.Text = $"Полигон замкнут ({currentPolygon.Points.Count} вершин).";
                }
                else
                {
                    statusLabel.Text = "Нужно минимум 3 вершины для замыкания полигона.";
                }

                currentPolygon = null;
                Redraw();
            }
        }

        private void Canvas_MouseMove(object? sender, MouseEventArgs e)
        {
            coordsLabel.Text = $"({e.X}, {e.Y})";

            // Перерисовка для превью линии (поставили первую точку, а вторую нет, отрисовываем линию динамически, чтобы было представление, как она будет выглядеть)
            if (checkMode == CheckMode.Intersect && intersectFirstPoint != null)
                Redraw();
            else if (checkMode == CheckMode.SideOfEdge &&
                     sideFirstPoint != null && sideSecondPoint == null)
                Redraw();
        }

        private void Canvas_SizeChanged(object? sender, EventArgs e)
        {
            if (canvas.Width <= 0 || canvas.Height <= 0) return;

            canvasBitmap?.Dispose();
            canvasBitmap = new Bitmap(canvas.Width, canvas.Height);
            canvas.Image = canvasBitmap;
            Redraw();
        }

        // ============================================================
        // Отрисовка
        // ============================================================
        private void Redraw()
        {
            if (canvasBitmap == null) return;

            using (var g = Graphics.FromImage(canvasBitmap))
            {
                g.Clear(Color.White);
                g.SmoothingMode = SmoothingMode.AntiAlias;

                foreach (var poly in polygons)
                    poly.Draw(g);

                currentPolygon?.Draw(g);

                // === Пункт 8: рисуем вводимое ребро ===
                if (checkMode == CheckMode.Intersect && intersectFirstPoint != null)
                {
                    using var pen = new Pen(Color.Purple, 2f) { DashStyle = DashStyle.Dash };
                    var cur = canvas.PointToClient(Cursor.Position);
                    g.DrawLine(pen, intersectFirstPoint.Value, cur);
                }

                // === Пункт 10: ребро и точка ===
                if (checkMode == CheckMode.SideOfEdge)
                {
                    using var pen = new Pen(Color.DarkOrange, 2f) { DashStyle = DashStyle.Dash };

                    if (sideFirstPoint != null && sideSecondPoint == null)
                    {
                        var cur = canvas.PointToClient(Cursor.Position);
                        g.DrawLine(pen, sideFirstPoint.Value, cur);
                    }
                    else if (sideFirstPoint != null && sideSecondPoint != null)
                    {
                        g.DrawLine(pen, sideFirstPoint.Value, sideSecondPoint.Value);
                    }

                    foreach (var r in sideResults)
                    {
                        g.DrawLine(pen, r.a, r.b);
                        using var br = new SolidBrush(Color.DarkOrange);
                        g.FillEllipse(br, r.p.X - 5, r.p.Y - 5, 10, 10);

                        using var font = new Font("Segoe UI", 10, FontStyle.Bold);
                        g.DrawString(r.text, font, Brushes.DarkRed, r.p.X + 8, r.p.Y + 8);
                    }
                }

                // === Подсказки пунктов 8 и 9 ===
                using (var font = new Font("Segoe UI", 10, FontStyle.Bold))
                using (var brush = new SolidBrush(Color.DarkRed))
                {
                    foreach (var h in checkHints)
                    {
                        using var marker = new SolidBrush(Color.Red);
                        g.FillEllipse(marker, h.pt.X - 5, h.pt.Y - 5, 10, 10);

                        var size = g.MeasureString(h.text, font);
                        using var bg = new SolidBrush(Color.FromArgb(220, 255, 255, 200));
                        g.FillRectangle(bg, h.pt.X + 8, h.pt.Y + 8, size.Width + 6, size.Height + 4);
                        g.DrawString(h.text, font, brush, h.pt.X + 11, h.pt.Y + 10);
                    }
                }

                // === Информация о последнем преобразовании ===
                if (!string.IsNullOrEmpty(lastTransformInfo))
                {
                    using var font = new Font("Segoe UI", 12, FontStyle.Bold);
                    using var brush = new SolidBrush(Color.DarkRed);

                    var size = g.MeasureString(lastTransformInfo, font);
                    using var bg = new SolidBrush(Color.FromArgb(220, 255, 255, 255));
                    g.FillRectangle(bg, 5, 5, size.Width + 10, size.Height + 4);
                    g.DrawString(lastTransformInfo, font, brush, 10, 8);
                }
            }

            canvas.Invalidate();
        }

        // ============================================================
        // Кнопки
        // ============================================================
        private void BtnClear_Click(object? sender, EventArgs e)
        {
            if (polygons.Count == 0) return;

            SaveUndoState();

            polygons.Clear();
            currentPolygon = null;
            lastTransformInfo = "";
            Redraw();
            statusLabel.Text = "Сцена очищена. Можно отменить.";
        }

        // ============================================================
        // ПРИМЕНЕНИЕ ПРЕОБРАЗОВАНИЯ
        // ============================================================
        // Если есть выделенные — только к ним.
        // Если нет — ко всем.
        // matrixFunc принимает полигон и возвращает ЕГО матрицу
        // (для отражения — у каждого свой центр, поэтому параметр нужен).
        private void ApplyTransform(Func<Polygon, AffineMatrix> matrixFunc, string info)
        {
            var selected = GetSelected();
            var targets = selected.Count > 0 ? selected : polygons;

            if (targets.Count == 0)
            {
                MessageBox.Show("Сначала создайте полигон.");
                return;
            }

            SaveUndoState();

            foreach (var p in targets)
            {
                p.SaveSnapshot();
                p.ApplyTransform(matrixFunc(p));
            }

            string suffix = selected.Count > 0
                ? $"к ВЫДЕЛЕННЫМ ({selected.Count})"
                : $"ко ВСЕМ ({polygons.Count})";

            lastTransformInfo = info + " — " + suffix;
            statusLabel.Text = lastTransformInfo;
            Redraw();
        }

        private void BtnTranslate_Click(object? sender, EventArgs e)
        {
            if (polygons.Count == 0)
            {
                MessageBox.Show("Сначала создайте полигон.");
                return;
            }

            double dx = ParseDouble(txtDx.Text);
            double dy = ParseDouble(txtDy.Text);

            ApplyTransform(p => AffineMatrix.Translation(dx, dy),
                          $"Смещение: dx = {dx}, dy = {dy}");
        }

        private void BtnRotate_Click(object? sender, EventArgs e)
        {
            if (polygons.Count == 0)
            {
                MessageBox.Show("Сначала создайте полигон.");
                return;
            }

            double a = ParseDouble(txtAx.Text);
            double b = ParseDouble(txtAy.Text);
            double deg = ParseDouble(txtPhi.Text);
            double phi = deg * Math.PI / 180.0;

            ApplyTransform(p => AffineMatrix.RotationAroundPoint(a, b, phi),
                          $"Поворот: {deg}° вокруг ({a}, {b})");
        }

        private void BtnScale_Click(object? sender, EventArgs e)
        {
            if (polygons.Count == 0)
            {
                MessageBox.Show("Сначала создайте полигон.");
                return;
            }

            double a = ParseDouble(txtAx.Text);
            double b = ParseDouble(txtAy.Text);
            double alpha = ParseDouble(txtAlpha.Text);
            double delta = ParseDouble(txtDelta.Text);

            if (Math.Abs(alpha) < 0.0001 || Math.Abs(delta) < 0.0001)
            {
                MessageBox.Show("Коэффициенты α и δ не могут быть равны 0.");
                return;
            }

            ApplyTransform(p => AffineMatrix.ScaleAroundPoint(a, b, alpha, delta),
                          $"Растяжение: α = {alpha}, δ = {delta} от ({a}, {b})");
        }

        // Отражение — каждый полигон относительно СВОЕГО центра по X
        private void BtnReflectY_Click(object? sender, EventArgs e)
        {
            if (polygons.Count == 0)
            {
                MessageBox.Show("Сначала создайте полигон.");
                return;
            }

            ApplyTransform(p =>
            {
                double sumX = 0;
                foreach (var pt in p.Points)
                    sumX += pt.X;
                double axisX = p.Points.Count > 0 ? sumX / p.Points.Count : 0;
                return AffineMatrix.ReflectionAroundVerticalLine(axisX);
            },
            "Отражение: каждый от своего центра");
        }

        // ============================================================
        // Парсинг
        // ============================================================
        private static double ParseDouble(string s)
        {
            if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v))
                return v;
            if (double.TryParse(s, NumberStyles.Any, CultureInfo.CurrentCulture, out v))
                return v;
            return 0;
        }

        // ============================================================
        // 8. Пересечение рёбер
        // ============================================================
        private void HandleIntersectClick(PointF pt)
        {
            if (intersectFirstPoint == null)
            {
                intersectFirstPoint = pt;
                statusLabel.Text = "Первая точка ребра задана. Выберите вторую.";
                Redraw();
                return;
            }

            intersectSecondPoint = pt;
            var a1 = intersectFirstPoint.Value;
            var a2 = intersectSecondPoint.Value;

            int found = 0;
            checkHints.Clear();

            // перебираем все рёбра всех полигонов
            foreach (var poly in polygons)
            {
                for (int i = 0; i < poly.Points.Count - 1; i++)
                {
                    var ip = Polygon.SegmentIntersection(a1, a2,
                                                         poly.Points[i], poly.Points[i + 1]);
                    if (ip.HasValue)
                    {
                        checkHints.Add((ip.Value, $"∩ ({ip.Value.X:0},{ip.Value.Y:0})"));
                        found++;
                    }
                }
                // замыкающее ребро
                if (poly.Closed && poly.Points.Count > 2)
                {
                    var ip = Polygon.SegmentIntersection(a1, a2,
                                                         poly.Points[^1], poly.Points[0]);
                    if (ip.HasValue)
                    {
                        checkHints.Add((ip.Value, $"∩ ({ip.Value.X:0},{ip.Value.Y:0})"));
                        found++;
                    }
                }
            }

            statusLabel.Text = found > 0
                ? $"Найдено пересечений: {found}."
                : "Пересечения не найдены.";

            // Сбрасываем ввод, чтобы можно было сразу рисовать следующее ребро
            intersectFirstPoint = null;
            intersectSecondPoint = null;

            Redraw();
        }

        // ============================================================
        // 9. Принадлежность точки полигону (только БЛИЖАЙШЕМУ)
        // ============================================================
        private void HandlePointInPolyClick(PointF pt)
        {
            checkHints.Clear();

            // Ищем ближайший полигон с >= 3 вершинами
            Polygon? nearest = null;
            float bestDist = float.MaxValue;

            foreach (var poly in polygons)
            {
                if (poly.Kind != PolygonKind.Polygon || !poly.Closed || poly.Points.Count < 3)
                    continue;

                float d = poly.DistanceToPolygon(pt);
                if (d < bestDist)
                {
                    bestDist = d;
                    nearest = poly;
                }
            }

            if (nearest == null)
            {
                statusLabel.Text = "Нет замкнутых полигонов для проверки.";
                Redraw();
                return;
            }

            // Проверяем только ближайший полигон
            bool inside = IsPointInside(nearest.Points, pt);
            bool convex = nearest.IsConvex();

            string type = convex ? "выпуклый" : "невыпуклый";
            string res = inside ? "Внутри" : "Снаружи";

            // Индекс ближайшего полигона
            int idx = polygons.IndexOf(nearest);

            checkHints.Add((pt, $"[{idx}] {type}: {res}"));

            statusLabel.Text = $"Точка {res.ToLower()} полигона [{idx}] ({type}).";

            Redraw();
        }

        // Ray casting — точка внутри полигона
        private static bool IsPointInside(List<PointF> poly, PointF p)
        {
            bool inside = false;
            int n = poly.Count;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                if (((poly[i].Y > p.Y) != (poly[j].Y > p.Y)) &&
                    (p.X < (poly[j].X - poly[i].X) * (p.Y - poly[i].Y) /
                           (poly[j].Y - poly[i].Y) + poly[i].X))
                {
                    inside = !inside;
                }
            }
            return inside;
        }

        // ============================================================
        // ПУНКТ 10: Справа/слева от ребра
        // ============================================================
        private void HandleSideClick(PointF pt)
        {
            if (sideFirstPoint == null)
            {
                sideFirstPoint = pt;
                statusLabel.Text = "Первая точка ребра задана. Выберите вторую.";
                Redraw();
                return;
            }

            if (sideSecondPoint == null)
            {
                sideSecondPoint = pt;
                statusLabel.Text = "Ребро задано. Выберите точку для классификации.";
                Redraw();
                return;
            }

            // третий клик — проверяемая точка
            var a = sideFirstPoint.Value;
            var b = sideSecondPoint.Value;

            string side = Polygon.ClassifySide(a, b, pt);

            // сохраняем результат для отрисовки
            sideResults.Add((a, b, pt, side));

            statusLabel.Text = $"Точка {side} от ребра.";

            // сбрасываем — можно сразу проверять следующую точку относительно ТОГО ЖЕ ребра
            // (чтобы не перерисовывать ребро)
            // sideFirstPoint и sideSecondPoint НЕ сбрасываем — удобно для серии проверок.
            // Если нужно новое ребро — ПКМ.

            Redraw();
        }
    }
}