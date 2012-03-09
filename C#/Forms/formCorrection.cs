using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace CNC
{
    public partial class formCorrection : Form
    {
        public formCorrection( int posX, int posY)
        {
            InitializeComponent();
            tBX.Text = posX.ToString();
            tBY.Text = posY.ToString();
        }

        private void e_cmdEnvoyer_Click(object sender, EventArgs e)
        {
            try
            {
                int X = int.Parse(tBX.Text) * 1000;
                int Y = int.Parse(tBY.Text) * 1000;
                c_ExecuteCode.m_corriger(X, Y);
            }
            catch { }
            this.Close();
        }

        private void e_cmdCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            formCorrection fC = new formCorrection(c_usiner.AxeX.Actuel / 1000, c_usiner.AxeY.Actuel / 1000);
            fC.ShowDialog();
        }

    }
}
