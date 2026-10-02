using System;
using System.Windows.Forms;

namespace Lab4Graph
{
    //=======================================
    //Деева
    //=======================================
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}