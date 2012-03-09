using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

namespace CNC
{
    class c_dessinerVisu
    {

        public static bool vs_IsWorking; //indique si le thread est "actif" 
        public static bool vs_ArreterThread;
        Bitmap v_bmp;
        formMain v_formMain;
        List<Point> v_listePoints;

        public c_dessinerVisu(formMain vl_formMain)
        {
            v_formMain = vl_formMain;
            formMain.eventDessinerVisu += m_dessinerVisu;
            formMain.eventChargerPointsVisu += new d_chargerPointsVisu(m_eventChargerPointsVisu);
        }

        public void m_run()
        {
            while (!vs_ArreterThread)
            {
                Application.DoEvents();
            }
        }

        void m_eventChargerPointsVisu()
        {
            v_listePoints = (from string v_ligne in v_formMain.p_lBoxCodeTraduit.Items
                             where (v_ligne.StartsWith("dX"))
                             select new Point(int.Parse(v_ligne.Substring(12, 8).Trim()) / 100, int.Parse(v_ligne.Substring(32, 8).Trim()) / 100)).
                            ToList();
        }

        void m_dessinerVisu()
        {
            vs_IsWorking = true;
            try
            {
                v_bmp = new Bitmap(v_formMain.p_pBox.Width, v_formMain.p_pBox.Height);
                Graphics v_graphic = Graphics.FromImage(v_bmp);
                double vl_echelleVisu = v_formMain.p_echelleVisu;
                Point vl_decalageVisu = v_formMain.p_decalageVisu;
                //trace fond noir
                v_graphic.FillRectangle(Brushes.Black, new Rectangle(0, 0, v_bmp.Width, v_bmp.Height));
                Point v_lastPoint = new Point(0, 0);
                Point v_center = new Point(v_bmp.Width / 2, v_bmp.Height / 2);
                foreach (Point v_Point in v_listePoints)
                {
                    v_graphic.DrawLine(new Pen(Color.White), v_lastPoint.Mult(vl_echelleVisu).Div().Plus(v_center).Plus(vl_decalageVisu).InvY(v_bmp.Height)
                                        , v_Point.Mult(vl_echelleVisu).Div().Plus(v_center).Plus(vl_decalageVisu).InvY(v_bmp.Height));
                    v_lastPoint = v_Point;
                }
                //trace croix bleue 0,0
                Point v_decalage = v_formMain.p_decalageVisu;
                v_graphic.DrawLine(new Pen(Color.Pink), new Point(0, v_center.Y - v_decalage.Y), new Point(v_bmp.Width, v_center.Y - v_decalage.Y));
                v_graphic.DrawLine(new Pen(Color.Pink), new Point(v_center.X + v_decalage.X, 0), new Point(v_center.X + v_decalage.X, v_bmp.Height));
                v_formMain.p_pBox.Image = v_bmp;
                Application.DoEvents();
            }
            catch { }
            vs_IsWorking = false;
        }
    }
}
