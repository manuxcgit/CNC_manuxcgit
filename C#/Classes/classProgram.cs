using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace CNC
{
    static class classProgram
    {
        /// <summary>
        /// Point d'entrée principal de l'application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            try { Application.Run(new formMain()); }
            catch (Exception e) { MessageBox.Show(e.StackTrace, e.Message); }
        }
    }
}
