using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace CNC
{
    public partial class formTest : Form
    {

        [DllImport("inpout32.dll", EntryPoint = "Out32")]
        public static extern void m_DLL_Output(int adress, int value);

        double v_tempo = 0.9;
        Stopwatch sW = new Stopwatch();
        c_ControlleurParallele Controlleur = formMain.Controlleur;

        public formTest()
        {
            InitializeComponent();
            if (Controlleur.m_IsConnected())
            {
                MessageBox.Show("Controlleur OK");
            }
            else { MessageBox.Show("Pas de controlleur"); }
        }

        private void e_cmdEnvoyerSignal_Click(object sender, EventArgs e)
        {
            if (cmdEnvoyerSignal.Text == "Envoyer signal")
            {
                try
                {
                    v_tempo = (1000 / double.Parse(tBFreq.Text)) - 0.1;
                    Application.DoEvents();
                }
                catch { }
                cmdEnvoyerSignal.Text = "Arreter";
                while (cmdEnvoyerSignal.Text == "Arreter")
                {
                    m_DLL_Output(888, 255);
                    sW.Start();
                    while (sW.Elapsed.TotalMilliseconds < 0.1) { }
                    m_DLL_Output(888, 0);
                    sW.Reset();
                    sW.Start();
                    while (sW.Elapsed.TotalMilliseconds < v_tempo) { }
                    sW.Reset();
                    Application.DoEvents();
                }
            }
            else
            {
                cmdEnvoyerSignal.Text = "Envoyer signal";
                sW.Reset();
            }
        }

        private void e_cmdActiverRelai_Click(object sender, EventArgs e)
        {
            if (cmdActiverRelai.Text == "Activer Relai")
            {
                cmdActiverRelai.Text = "Desactiver";
            }
            else
            {
                cmdActiverRelai.Text = "Activer Relai";
            }
            Controlleur.m_ActiveRelai(cmdActiverRelai.Text == "Desactiver");
        }

        private void e_cmdEnable_Click(object sender, EventArgs e)
        {
            if (cmdEnable.Text == "Enable")
            {
                cmdEnable.Text = "Desable";
            }
            else
            {
                cmdEnable.Text = "Enable";
            }
            Controlleur.m_Enable((cmdEnable.Text == "Desable") ^ formMain.v_paramPortParallele.v_enableEtatBas);
        }

        private void e_cmdSens_Click(object sender, EventArgs e)
        {
            Controlleur.m_SetSens(cBAxeX.Checked, cBAxeY.Checked);
        }

        private void e_cmdNbrPas_Click(object sender, EventArgs e)
        {
            int[] v_result = Controlleur.m_testePositionPortB();
            tBNbrPas.Text = string.Format("X:{0:0} Y:{1:0}", v_result[0], v_result[1]);
        }

    }
}
