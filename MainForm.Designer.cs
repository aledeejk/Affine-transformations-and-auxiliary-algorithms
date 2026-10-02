namespace Lab4Graph
{
    //=======================================
    //Деева
    //=======================================
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.PictureBox canvas;

        private System.Windows.Forms.Panel toolPanelScroll;
        private System.Windows.Forms.Panel toolPanel;

        // Галочка выделения
        private System.Windows.Forms.CheckBox chkSelectMode;

        // Кнопки
        private System.Windows.Forms.Button btnPointMode;
        private System.Windows.Forms.Button btnEdgeMode;
        private System.Windows.Forms.Button btnPolygonMode;
        private System.Windows.Forms.Button btnClear;
        private System.Windows.Forms.Button btnUndo;
        private System.Windows.Forms.Button btnTranslate;
        private System.Windows.Forms.Button btnRotate;
        private System.Windows.Forms.Button btnScale;
        private System.Windows.Forms.Button btnReflectY;

        // Поля
        private System.Windows.Forms.TextBox txtDx;
        private System.Windows.Forms.TextBox txtDy;
        private System.Windows.Forms.TextBox txtAx;
        private System.Windows.Forms.TextBox txtAy;
        private System.Windows.Forms.TextBox txtPhi;
        private System.Windows.Forms.TextBox txtAlpha;
        private System.Windows.Forms.TextBox txtDelta;

        // Метки
        private System.Windows.Forms.Label lblDx;
        private System.Windows.Forms.Label lblDy;
        private System.Windows.Forms.Label lblAx;
        private System.Windows.Forms.Label lblAy;
        private System.Windows.Forms.Label lblPhi;
        private System.Windows.Forms.Label lblAlpha;
        private System.Windows.Forms.Label lblDelta;

        // Статус-бар
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel statusLabel;
        private System.Windows.Forms.ToolStripStatusLabel coordsLabel;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();

            // Создание контролов
            this.canvas = new System.Windows.Forms.PictureBox();
            this.toolPanelScroll = new System.Windows.Forms.Panel();
            this.toolPanel = new System.Windows.Forms.Panel();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.statusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.coordsLabel = new System.Windows.Forms.ToolStripStatusLabel();

            // Галочка
            this.chkSelectMode = new System.Windows.Forms.CheckBox();

            // Кнопки
            this.btnPointMode = new System.Windows.Forms.Button();
            this.btnEdgeMode = new System.Windows.Forms.Button();
            this.btnPolygonMode = new System.Windows.Forms.Button();
            this.btnClear = new System.Windows.Forms.Button();
            this.btnUndo = new System.Windows.Forms.Button();
            this.btnTranslate = new System.Windows.Forms.Button();
            this.btnRotate = new System.Windows.Forms.Button();
            this.btnScale = new System.Windows.Forms.Button();
            this.btnReflectY = new System.Windows.Forms.Button();

            // Поля
            this.txtDx = new System.Windows.Forms.TextBox();
            this.txtDy = new System.Windows.Forms.TextBox();
            this.txtAx = new System.Windows.Forms.TextBox();
            this.txtAy = new System.Windows.Forms.TextBox();
            this.txtPhi = new System.Windows.Forms.TextBox();
            this.txtAlpha = new System.Windows.Forms.TextBox();
            this.txtDelta = new System.Windows.Forms.TextBox();

            // Метки
            this.lblDx = new System.Windows.Forms.Label();
            this.lblDy = new System.Windows.Forms.Label();
            this.lblAx = new System.Windows.Forms.Label();
            this.lblAy = new System.Windows.Forms.Label();
            this.lblPhi = new System.Windows.Forms.Label();
            this.lblAlpha = new System.Windows.Forms.Label();
            this.lblDelta = new System.Windows.Forms.Label();

            // Компоновка
            ((System.ComponentModel.ISupportInitialize)this.canvas).BeginInit();
            this.toolPanelScroll.SuspendLayout();
            this.toolPanel.SuspendLayout();
            this.statusStrip.SuspendLayout();
            this.SuspendLayout();

            // Canvas
            this.canvas.Dock = System.Windows.Forms.DockStyle.Fill;
            this.canvas.BackColor = System.Drawing.Color.White;
            this.canvas.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.canvas.Name = "canvas";

            // Scroll Panel
            this.toolPanelScroll.Dock = System.Windows.Forms.DockStyle.Left;
            this.toolPanelScroll.Width = 260;
            this.toolPanelScroll.AutoScroll = true;
            this.toolPanelScroll.BackColor = System.Drawing.SystemColors.ControlLight;
            this.toolPanelScroll.Name = "toolPanelScroll";

            // Tool Panel
            this.toolPanel.AutoSize = true;
            this.toolPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.toolPanel.Location = new System.Drawing.Point(0, 0);
            this.toolPanel.Padding = new System.Windows.Forms.Padding(8);
            this.toolPanel.Name = "toolPanel";

            // Размещение
            int y = 10;

            // ===== ГАЛОЧКА ВЫДЕЛЕНИЯ =====
            AddCheckBox(this.toolPanel, this.chkSelectMode,
                "Режим выделения", 8, ref y);

            y += 6;

            // Режимы создания
            AddButton(this.toolPanel, this.btnPointMode, "Точка", 8, ref y);
            AddButton(this.toolPanel, this.btnEdgeMode, "Ребро", 8, ref y);
            AddButton(this.toolPanel, this.btnPolygonMode, "Полигон", 8, ref y);
            AddButton(this.toolPanel, this.btnClear, "Очистить", 8, ref y);
            AddButton(this.toolPanel, this.btnUndo, "Отменить", 8, ref y);

            y += 14;

            // Смещение
            AddLabel(this.toolPanel, this.lblDx, "dx (смещение по X):", 8, ref y);
            AddTextBox(this.toolPanel, this.txtDx, "0", 8, ref y);
            AddLabel(this.toolPanel, this.lblDy, "dy (смещение по Y):", 8, ref y);
            AddTextBox(this.toolPanel, this.txtDy, "0", 8, ref y);
            AddButton(this.toolPanel, this.btnTranslate, "Сместить", 8, ref y);

            y += 14;

            // Поворот
            AddLabel(this.toolPanel, this.lblAx, "a (центр):", 8, ref y);
            AddTextBox(this.toolPanel, this.txtAx, "450", 8, ref y);
            AddLabel(this.toolPanel, this.lblAy, "b (центр):", 8, ref y);
            AddTextBox(this.toolPanel, this.txtAy, "350", 8, ref y);
            AddLabel(this.toolPanel, this.lblPhi, "φ (градусы):", 8, ref y);
            AddTextBox(this.toolPanel, this.txtPhi, "30", 8, ref y);
            AddButton(this.toolPanel, this.btnRotate, "Повернуть", 8, ref y);

            y += 14;

            // Растяжение
            AddLabel(this.toolPanel, this.lblAlpha, "α (по X):", 8, ref y);
            AddTextBox(this.toolPanel, this.txtAlpha, "1", 8, ref y);
            AddLabel(this.toolPanel, this.lblDelta, "δ (по Y):", 8, ref y);
            AddTextBox(this.toolPanel, this.txtDelta, "1", 8, ref y);
            AddButton(this.toolPanel, this.btnScale, "Растянуть", 8, ref y);
            AddButton(this.toolPanel, this.btnReflectY, "Отражение (Y)", 8, ref y);

            // Собираем
            this.toolPanelScroll.Controls.Add(this.toolPanel);

            // Status Strip
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[]
            {
                this.statusLabel, this.coordsLabel
            });
            this.statusLabel.Text = "Готово.";
            this.coordsLabel.Spring = true;
            this.coordsLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;

            // Form
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1200, 750);
            this.Controls.Add(this.canvas);
            this.Controls.Add(this.toolPanelScroll);
            this.Controls.Add(this.statusStrip);
            this.Name = "MainForm";
            this.Text = "Лабораторная работа №4 — Аффинные преобразования";

            ((System.ComponentModel.ISupportInitialize)this.canvas).EndInit();
            this.toolPanelScroll.ResumeLayout(false);
            this.toolPanel.ResumeLayout(false);
            this.toolPanel.PerformLayout();
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        // Вспомогательные методы разметки

        private static void AddCheckBox(System.Windows.Forms.Panel p, System.Windows.Forms.CheckBox c,
                                        string text, int x, ref int y)
        {
            if (c == null)
                throw new System.ArgumentNullException(nameof(c),
                    $"Галочка '{text}' не создана через new!");

            c.Text = text;
            c.Left = x;
            c.Top = y;
            c.Width = 220;
            c.Height = 30;
            c.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            c.ForeColor = System.Drawing.Color.DarkBlue;
            p.Controls.Add(c);
            y += 40;
        }

        private static void AddButton(System.Windows.Forms.Panel p, System.Windows.Forms.Button b,
                                      string text, int x, ref int y)
        {
            if (b == null)
                throw new System.ArgumentNullException(nameof(b),
                    $"Кнопка '{text}' не создана через new!");

            b.Text = text;
            b.Left = x;
            b.Top = y;
            b.Width = 220;
            b.Height = 34;
            b.Font = new System.Drawing.Font("Segoe UI", 11F);
            p.Controls.Add(b);
            y += 42;
        }

        private static void AddLabel(System.Windows.Forms.Panel p, System.Windows.Forms.Label l,
                                     string text, int x, ref int y)
        {
            if (l == null)
                throw new System.ArgumentNullException(nameof(l),
                    $"Метка '{text}' не создана через new!");

            l.Text = text;
            l.Left = x;
            l.Top = y;
            l.Width = 220;
            l.Height = 24;
            l.Font = new System.Drawing.Font("Segoe UI", 11F);
            p.Controls.Add(l);
            y += 26;
        }

        private static void AddTextBox(System.Windows.Forms.Panel p, System.Windows.Forms.TextBox t,
                                       string text, int x, ref int y)
        {
            if (t == null)
                throw new System.ArgumentNullException(nameof(t),
                    $"Поле '{text}' не создано через new!");

            t.Text = text;
            t.Left = x;
            t.Top = y;
            t.Width = 220;
            t.Height = 36;
            t.Font = new System.Drawing.Font("Segoe UI", 14F);
            t.Multiline = true;
            t.TextAlign = System.Windows.Forms.HorizontalAlignment.Left;
            p.Controls.Add(t);
            y += 44;
        }
    }
}