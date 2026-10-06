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

        //Сидорчик
        // Режим ожидания клика мышью для задания точки (центра)
        private bool waitingForPointPick = false;
        // Что делать после того, как точка выбрана: "rotate" или "scale"
        private string pendingAction = "";
        // Коэффициенты для отложенного масштаба
        private double pendingAlpha = 1;
        private double pendingDelta = 1;
        // Угол для отложенного поворота
        private double pendingPhi = 0;

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
            //Сидорчик
            btnPickPoint.Click += BtnPickPoint_Click;
            btnRotateCenter.Click += BtnRotateCenter_Click;
            btnScaleCenter.Click += BtnScaleCenter_Click;
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
            //Сидорчик(начало)
            //  режим выбора точки мышью 
            if (waitingForPointPick && e.Button == MouseButtons.Left)
            {
                waitingForPointPick = false;
                Cursor = Cursors.Default;

                var selected = GetSelected();
                var targets = selected.Count > 0 ? selected : polygons;

                if (targets.Count == 0) return;

                SaveUndoState();

                if (pendingAction == "rotate")
                {
                    foreach (var p in targets)
                    {
                        p.SaveSnapshot();
                        p.ApplyTransform(AffineMatrix.RotationAroundPoint(pt.X, pt.Y, pendingPhi));
                    }

                    double deg = pendingPhi * 180.0 / Math.PI;
                    lastTransformInfo = $"Поворот на {deg:0.##}° вокруг ({pt.X:0}, {pt.Y:0})";
                }
                else if (pendingAction == "scale")
                {
                    foreach (var p in targets)
                    {
                        p.SaveSnapshot();
                        p.ApplyTransform(AffineMatrix.ScaleAroundPoint(pt.X, pt.Y, pendingAlpha, pendingDelta));
                    }

                    lastTransformInfo = $"Масштаб α={pendingAlpha}, δ={pendingDelta} от ({pt.X:0}, {pt.Y:0})";
                }

                statusLabel.Text = lastTransformInfo;
                pendingAction = "";
                Redraw();
                return;
            }
            //Сидорчик (конец)
            if (e.Button == MouseButtons.Left)
            {
                if (selectMode)
                {
                    var hit = FindPolygonAt(pt, 8f);

                    if (hit != null)
                    {
                        // Toggle — добавить/убрать из выделения
                        ToggleSelection(hit);
                    }
                    else
                    {
                        // Клик по пустому месту — снять всё
                        DeselectAll();
                        statusLabel.Text = "Выделение снято. Преобразования — ко ВСЕМ.";
                    }
                    return;
                }

                // Иначе — создание
                HandleLeftClick(pt);
            }
            else if (e.Button == MouseButtons.Right)
            {
                HandleRightClick();
            }
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
        //Сидорчик (начало)
        //  ПОВОРОТ ВОКРУГ СВОЕГО ЦЕНТРА 
        private void BtnRotateCenter_Click(object? sender, EventArgs e)
        {
            if (polygons.Count == 0)
            {
                MessageBox.Show("Сначала создайте полигон.");
                return;
            }

            double deg = ParseDouble(txtPhi.Text);
            double phi = deg * Math.PI / 180.0;

            ApplyTransform(p =>
            {
                var c = p.GetCenter();
                return AffineMatrix.RotationAroundPoint(c.X, c.Y, phi);
            }, $"Поворот на {deg}° вокруг СВОЕГО центра");
        }

        // МАСШТАБ ВОКРУГ СВОЕГО ЦЕНТРА 
        private void BtnScaleCenter_Click(object? sender, EventArgs e)
        {
            if (polygons.Count == 0)
            {
                MessageBox.Show("Сначала создайте полигон.");
                return;
            }

            double alpha = ParseDouble(txtAlpha.Text);
            double delta = ParseDouble(txtDelta.Text);

            if (Math.Abs(alpha) < 0.0001 || Math.Abs(delta) < 0.0001)
            {
                MessageBox.Show("Коэффициенты α и δ не могут быть равны 0.");
                return;
            }

            ApplyTransform(p =>
            {
                var c = p.GetCenter();
                return AffineMatrix.ScaleAroundPoint(c.X, c.Y, alpha, delta);
            }, $"Масштаб α={alpha}, δ={delta} вокруг СВОЕГО центра");
        }

        // ЗАДАТЬ ТОЧКУ МЫШЬЮ 
        private void BtnPickPoint_Click(object? sender, EventArgs e)
        {
            if (polygons.Count == 0)
            {
                MessageBox.Show("Сначала создайте полигон.");
                return;
            }

            // Спрашиваем у пользователя, какое действие выполнить
            var result = MessageBox.Show(
                "Выберите действие после указания точки:\n\n" +
                "ДА — Поворот вокруг указанной точки\n" +
                "НЕТ — Масштаб относительно указанной точки\n" +
                "ОТМЕНА — отмена",
                "Выбор действия",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question);

            if (result == DialogResult.Cancel) return;

            if (result == DialogResult.Yes)
            {
                pendingAction = "rotate";
                double deg = ParseDouble(txtPhi.Text);
                pendingPhi = deg * Math.PI / 180.0;
                statusLabel.Text = "Кликните мышью по холсту, чтобы задать центр поворота...";
            }
            else
            {
                pendingAction = "scale";
                pendingAlpha = ParseDouble(txtAlpha.Text);
                pendingDelta = ParseDouble(txtDelta.Text);

                if (Math.Abs(pendingAlpha) < 0.0001 || Math.Abs(pendingDelta) < 0.0001)
                {
                    MessageBox.Show("Коэффициенты α и δ не могут быть равны 0.");
                    return;
                }
                statusLabel.Text = "Кликните мышью по холсту, чтобы задать центр масштабирования...";
            }

            waitingForPointPick = true;
            Cursor = Cursors.Cross; // Меняем курсор, чтобы было видно
        }
        //Сидорчик(конец)

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
    }
}