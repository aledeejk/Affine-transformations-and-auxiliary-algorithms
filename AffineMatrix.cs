using System;
using System.Drawing;

namespace Lab4Graph
{
    public class AffineMatrix
    {
        public double[,] M { get; } = new double[3, 3];

        public AffineMatrix()
        {
            M[0, 0] = 1; M[1, 1] = 1; M[2, 2] = 1;
        }

        public AffineMatrix(double[,] values)
        {
            Array.Copy(values, M, 9);
        }

        // Умножение матриц: result = A × B
        public static AffineMatrix Multiply(AffineMatrix a, AffineMatrix b)
        {
            var r = new AffineMatrix();
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                {
                    double sum = 0;
                    for (int k = 0; k < 3; k++)
                        sum += a.M[i, k] * b.M[k, j];
                    r.M[i, j] = sum;
                }
            return r;
        }

        // Применить матрицу к точке (x, y, 1)
        public PointF Apply(PointF p)
        {
            double x = p.X * M[0, 0] + p.Y * M[1, 0] + 1.0 * M[2, 0];
            double y = p.X * M[0, 1] + p.Y * M[1, 1] + 1.0 * M[2, 1];
            return new PointF((float)x, (float)y);
        }

        // Перенос
        public static AffineMatrix Translation(double dx, double dy)
        {
            var m = new AffineMatrix();
            m.M[2, 0] = dx;
            m.M[2, 1] = dy;
            return m;
        }

        // Поворот вокруг начала координат
        public static AffineMatrix Rotation(double phi)
        {
            var m = new AffineMatrix();
            double c = Math.Cos(phi);
            double s = Math.Sin(phi);
            m.M[0, 0] = c;  m.M[0, 1] = s;
            m.M[1, 0] = -s; m.M[1, 1] = c;
            return m;
        }

        // Растяжение
        public static AffineMatrix Scale(double alpha, double delta)
        {
            var m = new AffineMatrix();
            m.M[0, 0] = alpha;
            m.M[1, 1] = delta;
            return m;
        }

        // Отражение относительно вертикальной линии x = a
        public static AffineMatrix ReflectionAroundVerticalLine(double a)
        {
            var m = new AffineMatrix();
            m.M[0, 0] = -1;
            m.M[1, 1] = 1;
            m.M[2, 0] = 2 * a;
            return m;
        }

        // Отражение относительно горизонтальной линии y = b
        public static AffineMatrix ReflectionAroundHorizontalLine(double b)
        {
            var m = new AffineMatrix();
            m.M[0, 0] = 1;
            m.M[1, 1] = -1;
            m.M[2, 1] = 2 * b;
            return m;
        }

        // Поворот вокруг точки A(a, b)
        public static AffineMatrix RotationAroundPoint(double a, double b, double phi)
        {
            var tBack = Translation(-a, -b);
            var r = Rotation(phi);
            var tFwd = Translation(a, b);
            return Multiply(Multiply(tBack, r), tFwd);
        }

        // Растяжение с центром A(a, b)
        public static AffineMatrix ScaleAroundPoint(double a, double b, double alpha, double delta)
        {
            var tBack = Translation(-a, -b);
            var s = Scale(alpha, delta);
            var tFwd = Translation(a, b);
            return Multiply(Multiply(tBack, s), tFwd);
        }
    }
}