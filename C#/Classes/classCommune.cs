using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Drawing;

namespace CNC
{
    public static class helper
    {
        public static void AddRange(this ListBox.ObjectCollection vl_listBox, List<string> vl_liste)
        {
            //    ListBox.ObjectCollection v_final = new ListBox.ObjectCollection();
            foreach (string v_line in vl_liste)
            {
                vl_listBox.Add(v_line);
            }
        }

        public static int Arrondir(this int v_int, int index)
        {
            if (index == 2)
            {
                double v;
                if (v_int >= 0)
                { v = ((double)v_int + 5) / 10; }
                else
                { v = ((double)v_int - 5) / 10; }
                v = (int)v * 10;
                return (int)v;
            }
            else
            { return v_int; }
        }

        public static Point Div(this Point v_point)
        {
            return new Point(v_point.X / 10, v_point.Y / 10);
        }

        public static Point InvY(this Point v_point, int Y)
        {
            return new Point(v_point.X, Y - v_point.Y);
        }

        public static char LastChar(this string v_s)
        {
            return v_s.Substring(v_s.Length - 1).ToCharArray()[0];
        }

        public static Point Mult(this Point v_pointA, double v_mult)
        {
            return new Point((int)((double)v_pointA.X * v_mult), (int)((double)v_pointA.Y * v_mult));
        }

        public static Point Plus(this Point v_point, int X, int Y)
        {
            return new Point(v_point.X + X, v_point.Y + Y);
        }

        public static Point Plus(this Point v_pointA, Point v_PointB)
        {
            return new Point(v_pointA.X + v_PointB.X, v_pointA.Y + v_PointB.Y);
        }

        public static List<string> ToListString(this ListBox.ObjectCollection vl_listBox)
        {
            List<string> v_result = new List<string>();
            foreach (object v_line in vl_listBox)
            {
                v_result.Add(v_line.ToString());
            }
            return v_result;
        }

        public static string m_trouverXY(this byte v_byte, int v_index)
        {
            // sur un byte, cherche si 0 = X, 1 = Y, 2 = X, ...
            v_index *= 2;
            if ((v_index < 0) | (v_index > 6)) { return ""; }
            string v_result = "";
            if ((v_byte & (byte)Math.Pow(2, 7 - v_index)) == (byte)Math.Pow(2, 7 - v_index)) { v_result = "X"; }
            if ((v_byte & (byte)Math.Pow(2, 6 - v_index)) == (byte)Math.Pow(2, 6 - v_index)) { v_result += "Y"; }
            return v_result;
        }

        public static byte  m_placerXY(this byte v_byte, int v_index, string v_axes)
        {
            v_index *= 2;
            if (v_axes.Contains('X'))
            { v_byte |= (byte)Math.Pow(2, 7 - v_index); }
            if (v_axes.Contains('Y'))
            { v_byte |= (byte)Math.Pow(2, 6 - v_index); }
            return v_byte;
        }

    }
}
