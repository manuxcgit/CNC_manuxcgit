using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;

namespace CNC
{
    class c_traduireIso : IDisposable
    {
        
            string v_nomFichier;
            formMain v_formMain;
            bool v_isDisposed = false, v_ignorerX0Y0;
            int v_nbrchiffresApresVirgule, v_precisionEllipse, v_deltaMiniX, v_deltaMiniY;

            // on lit le fichier ISO, gere les sous prog et cycles percages puis traduit en fichier Commandes

            public c_traduireIso(string fichier, formMain formMain, bool IgnorerX0Y0, int NbrCAV, int PreElip, int deltaMiniX, int deltaMiniY)
            {
                v_nomFichier = fichier;
                v_formMain = formMain;
                v_ignorerX0Y0 = IgnorerX0Y0;
                v_nbrchiffresApresVirgule=NbrCAV;
                v_precisionEllipse = PreElip;
                v_deltaMiniX = deltaMiniX;
                v_deltaMiniY = deltaMiniY;
            }

            ~c_traduireIso()
            {
                Dispose(false);
            }

            public void Dispose()
            {
                //Pass true in dispose method to clean managed resources too and say GC to skip finalize                             in next line.
                Dispose(true);
                //If dispose is called already then say GC to skip finalize on this instance.
                GC.SuppressFinalize(this);
            }

            protected virtual void Dispose(bool disposedStatus)
            {
                if (!v_isDisposed)
                {
                    v_isDisposed = true;
                    // Released unmanaged Resources
                    if (disposedStatus)
                    {
                        // Released managed Resources
                    }
                }
            }

            public List<string> m_convertirFichier()
            {
                List<string> v_fichier = new List<string>();
                List<string> v_listeCommandes = new List<string>();
                Point v_lastPoint;
                if (v_nomFichier != "")
                {
                    v_fichier.AddRange(File.ReadAllLines(v_nomFichier));
                }
                else
                { v_fichier = v_formMain.p_lBoxFichier.Items.ToListString(); }
                formMain.vs_infoUsinage.v_nbrLignesProgramme = 0;
                formMain.vs_infoUsinage.v_distanceTotale = 0;
                formMain.vs_infoUsinage.XCalcul = 0;
                formMain.vs_infoUsinage.YCalcul = 0;
                formMain.vs_infoUsinage.ZCalcul = 0;
                v_formMain.p_pBar.Maximum = v_fichier.Count;
                #region cherche les sous-programmes
                List<string> v_liste = new List<string>();
                for (int i = 1; i <= v_fichier.Count; i++)
                {
                    string v_line = v_fichier[i - 1];
                    if (!v_line.StartsWith("M98"))
                    { v_liste.Add(v_line); }
                    else
                    {
                        // chercher le sousprogramme correspondant
                        try
                        {
                            int v_numSousProg = int.Parse(v_line.Substring(5, v_line.Length - 6));
                            int i1 = i;
                            string v_newLine = v_fichier[i1 - 1];
                            string v_aChercher = string.Format("O{0};", v_numSousProg);
                            while ((i1 < v_fichier.Count) && (v_newLine != v_aChercher))
                            {
                                i1++;
                                v_newLine = v_fichier[i1 - 1];
                            }
                            i1++;
                            if (i1 < v_fichier.Count)
                            {
                                v_newLine = v_fichier[i1 - 1];
                                while ((i1 < v_fichier.Count) && (v_newLine != "M99;"))
                                {
                                    v_liste.Add(v_newLine);
                                    i1++;
                                    v_newLine = v_fichier[i1 - 1];
                                }
                            }
                        }
                        catch { }
                    }
                }
                v_fichier.Clear();
                v_fichier.AddRange(v_liste);
                #endregion
                #region convertit les instructions de perçage en G01
                v_liste = new List<string>();
                for (int i = 1; i <= v_fichier.Count; i++)
                {
                    string v_line = v_fichier[i - 1];
                    if (!v_line.StartsWith("G8"))
                    { v_liste.Add(v_line); }
                    else
                    {
                        int v_Z = -1;
                        v_liste.Add("G01" + v_line.Substring(3));
                        i++;
                        v_line = v_fichier[i - 1];
                        while ((i < v_fichier.Count) && (v_line != "G80;"))
                        {
                            v_liste.Add("G01 " + v_line.Replace(';', ' ') + " Z" + v_Z.ToString() + ".;");
                            i++;
                            v_Z--;
                            v_line = v_fichier[i - 1];
                        }
                        //convertit G80
                        v_liste.Add("M5;");
                    }
                }
                v_fichier.Clear();
                v_fichier.AddRange(v_liste);
                #endregion
                bool v_finTrouvee = false;
                int v_distLigne;
                foreach (string v_line in v_fichier)
                {
                    if (!v_finTrouvee)
                    {
                        #region convertir code
                        try
                        {
                            string v_commande = v_line.Substring(0, 3).ToUpper();
                            switch (v_commande)
                            {
                                case "G00":
                                case "G01":
                                    if ((v_ignorerX0Y0) && (v_line.Contains("X0. Y0.")))
                                    {
                                        v_listeCommandes.Add("NOP");
                                        break;
                                    }
                                    v_lastPoint = new Point(formMain.vs_infoUsinage.XCalcul, formMain.vs_infoUsinage.YCalcul);
                                    string v_ligneTraduite = m_analyseLigne("XYZ".ToCharArray(), v_line);
                                    v_distLigne = (int)Math.Sqrt(Math.Pow(formMain.vs_infoUsinage.XCalcul - v_lastPoint.X, 2) + Math.Pow(formMain.vs_infoUsinage.YCalcul - v_lastPoint.Y, 2));
                                    formMain.vs_infoUsinage.v_distanceTotale += v_distLigne;
                                    formMain.vs_infoUsinage.v_nbrLignesProgramme++;
                                    v_listeCommandes.Add("Debut Bloc Lineaire" + v_distLigne.ToString());
                                    v_listeCommandes.Add(v_ligneTraduite);
                                    v_listeCommandes.Add("Fin de Bloc");
                                    break;

                                case "G02":
                                case "G03":
                                case "G04": //pour voir angulaire qui cause erreur 18.01.2012
                                    if (v_commande == "G04")
                                    { }
                                    int v_oldDist = formMain.vs_infoUsinage.v_distanceTotale;
                                    List<string> v_listeTraduite = m_analyseLigne(v_line, (v_commande == "G02"));
                                    v_listeCommandes.Add("Debut Bloc Angulair" + (formMain.vs_infoUsinage.v_distanceTotale - v_oldDist).ToString());
                                    v_listeCommandes.AddRange(v_listeTraduite);
                                    v_listeCommandes.Add("Fin de Bloc");
                                    break;

                                case "M5;":
                                    v_listeCommandes.Add("STOP");
                                    break;

                                case "M02":
                                    v_listeCommandes.Add("STOP");
                                    v_finTrouvee = true;
                                    break;

                                case "S01":
                                    v_listeCommandes.Add("VITESSE:" + v_line.Substring(3).Replace(";", "").Replace(" ", ""));
                                    break;

                                default:
                                    if (v_line.StartsWith("T"))
                                    {
                                        try
                                        {
                                            string v_test = v_line.Substring(v_line.IndexOf("00)") - 5, 7).Trim().Replace('.', ',');
                                            double v_diam = (double.Parse(v_test));
                                            v_listeCommandes.Add("Diametre" + v_diam.ToString());
                                        }
                                        catch { v_listeCommandes.Add("NOP"); }
                                    }
                                    else
                                    { v_listeCommandes.Add("NOP"); }
                                    break;
                            }
                        }
                        catch { v_listeCommandes.Add("NOP"); }
                        #endregion
                        v_formMain.p_pBar.Value++;
                    }
                }
                try { v_formMain.Text = "CNC ... " + new FileInfo(v_nomFichier).Name; }
                catch { v_formMain.Text = "CNC ... Saisie Directe"; }
                v_formMain.p_lBoxFichier.Items.Clear();
                v_formMain.p_lBoxFichier.Items.AddRange(v_fichier);
                v_formMain.p_pBar.Value = 0;
                c_usiner.vs_distanceUsinee = 0;
                return v_listeCommandes;
            }

            string m_analyseLigne(char[] vl_AxesAAnalyser, string vl_ligne)
            {
                //pour commande G00 et G01
                string v_ligneFinale = "";
                foreach (char v_c in vl_AxesAAnalyser)
                {
                    int v_next = m_trouverCoordonnee(v_c, vl_ligne);
                    //apres arrondi, 9999999 -> 100000000
                    if (v_next != 100000000)
                    {
                        int v_delta = 0;
                        switch (v_c)
                        {
                            case 'X': v_delta = v_next - formMain.vs_infoUsinage.XCalcul; formMain.vs_infoUsinage.XCalcul = v_next; break;
                            case 'Y': v_delta = v_next - formMain.vs_infoUsinage.YCalcul; formMain.vs_infoUsinage.YCalcul = v_next; break;
                            case 'Z': v_delta = v_next - formMain.vs_infoUsinage.ZCalcul; formMain.vs_infoUsinage.ZCalcul = v_next; break;
                        }
                        //             int v_delta = v_next - v_infoUsinage.Coordonnees_Calcul[v_c];
                        v_ligneFinale = v_ligneFinale + string.Format("d{0}{1,8:0} {0}{2,8:0}", v_c, v_delta, v_next);
                        //             v_infoUsinage.Coordonnees_Calcul[v_c] = v_next;
                    }
                    else
                    {
                        int v_temp = 0;
                        switch (v_c)
                        {
                            case 'X': v_temp = formMain.vs_infoUsinage.XCalcul; break;
                            case 'Y': v_temp = formMain.vs_infoUsinage.YCalcul; break;
                            case 'Z': v_temp = formMain.vs_infoUsinage.ZCalcul; break;
                        }
                        v_ligneFinale = v_ligneFinale + string.Format("d{0}{1,8:0} {0}{2,8:0}", v_c, 0, v_temp);
                    }
                }
                return v_ligneFinale;
            }  //retourne une seule commande

            List<string> m_analyseLigne(string vl_ligne, bool vl_horaire)
            {
                int v_xi = m_trouverCoordonnee('X', vl_ligne);
                int v_yi = m_trouverCoordonnee('Y', vl_ligne);
                int v_i1 = m_trouverCoordonnee('I', vl_ligne);
                int v_j1 = m_trouverCoordonnee('J', vl_ligne);
                List<string> v_result = m_ellipse(formMain.vs_infoUsinage.XCalcul, formMain.vs_infoUsinage.XCalcul + v_i1, v_xi, formMain.vs_infoUsinage.YCalcul, formMain.vs_infoUsinage.YCalcul + v_j1, v_yi, vl_horaire);
                if (v_result == null)
                { 
                    System.Windows.Forms.MessageBox.Show("La ligne " + vl_ligne + " n'est pas traduite", "ERREUR DE CODE .ISO");
                    v_result = new List<string>();
                    string v_newLine = vl_ligne.Replace("G02", "G01").Replace("G03", "G01");
                    v_result.Add(m_analyseLigne("XYZ".ToCharArray(), v_newLine));
                }
                return v_result;
                //verifier si Point final correspond
            }  //retourne liste de commandes

            int m_trouverCoordonnee(char vl_c, string vl_ligne)
            {
                int v_coord = 0;
                int v_debut = vl_ligne.IndexOf(vl_c);
                if (v_debut == -1)
                { v_coord = 100000000; }
                else
                {
                    int v_pos = v_debut + 1;
                    string v_valeur = "";
                    while ((v_pos < vl_ligne.Length - 1) && (vl_ligne[v_pos] != ' ')) //-1 pour eviter lire ';' à la fin
                    {
                        string v_essai = vl_ligne[v_pos].ToString();
                        v_valeur = v_valeur + v_essai;
                        v_pos++;
                    }
                    try
                    { v_coord = (int)(double.Parse(v_valeur.Replace('.', ',')) * 1000); }
                    catch { v_coord = 100000000; }
                }
                return v_coord.Arrondir(v_nbrchiffresApresVirgule);
            }

            List<string> m_ellipse(int x1, int x2, int x3, int y1, int y2, int y3, bool vl_horaire)
            {
                int R, Xa, Ya, DeltaX, DeltaY, xp, yp;
                double angletotal, anglehor, step, i;
                List<string> v_result = new List<string>();
                xp = yp = 0;

                angletotal = m_calculAngle(x1, x2, x3, y1, y2, y3);
                //cherche angle / axe des x
                anglehor = m_calculAngle(x1, x2, x1, y1, y2, y2);
                if (((angletotal == 0) && (anglehor == 0)) || (angletotal>359.7))
                { return null; }
                Xa = x1;
                Ya = y1;
                step = v_precisionEllipse;
                if ((x1 == x2) && (y1 <= y2)) { anglehor = 90; }
                if ((x1 == x2) && (y1 > y2)) { anglehor = 270; }
                R = (int)Math.Sqrt(Math.Pow(x1 - x2, 2) + Math.Pow(y1 - y2, 2));
                //pour les tous petits angles
                if (step > angletotal) { step = angletotal; }
                if ((360 - angletotal) < step) { step = 360 - angletotal; }
                if (step == 0)
                {
                    return null;
                }

                if (vl_horaire)
                {
                    i = step;
                    if (x1 < x2) { anglehor = anglehor + 180; }
                    //v_result.Add(string.Format("            Horaire {0:0.00} °", angletotal));
                    while (i <= 360 - angletotal)
                    {
                        if (i > 360 - angletotal - step) { i = 360 - angletotal; }
                        xp = (int)(x2 + (Math.Cos(2 * Math.PI * (-anglehor - i) / 360) * R));
                        yp = (int)(y2 + (Math.Sin(2 * Math.PI * (-anglehor - i) / 360) * R));
                        xp = xp.Arrondir(v_nbrchiffresApresVirgule);
                        yp = yp.Arrondir(v_nbrchiffresApresVirgule);
                        DeltaX = (xp - Xa);
                        DeltaY = (yp - Ya);
                        Xa = xp;
                        Ya = yp;
                        v_result.Add(string.Format("dX{0,8:0} X{1,8:0}dY{2,8:0} Y{3,8:0}", DeltaX, Xa, DeltaY, Ya));//  'A'+FloatToStr(DeltaX)+' B'+FloatToStr(DeltaY)+' C0     X'+FloatToStr(trunc(xa))+' Y'+FloatToStr(trunc(ya)));
                        i = i + step;
                        formMain.vs_infoUsinage.v_distanceTotale += m_estimerDeplacement(DeltaX, DeltaY);
                        formMain.vs_infoUsinage.v_nbrLignesProgramme++;
                    }
                }
                else
                {
                    i = step;
                    if (x1 < x2) { anglehor = anglehor + 180; }
                    //v_result.Add(string.Format("           Anti - Horaire {0:0.00} °", angletotal));
                    while (i <= angletotal)
                    {
                        if (i > angletotal - step) { i = angletotal; }
                        xp = (int)(x2 + (Math.Cos(2 * Math.PI * (-anglehor + i) / 360) * R));
                        yp = (int)(y2 + (Math.Sin(2 * Math.PI * (-anglehor + i) / 360) * R));
                        xp = xp.Arrondir(v_nbrchiffresApresVirgule);
                        yp = yp.Arrondir(v_nbrchiffresApresVirgule);
                        DeltaX = (xp - Xa);
                        DeltaY = (yp - Ya);
                        Xa = xp;
                        Ya = yp;
                        v_result.Add(string.Format("dX{0,8:0} X{1,8:0}dY{2,8:0} Y{3,8:0}", DeltaX, Xa, DeltaY, Ya));//  'A'+FloatToStr(DeltaX)+' B'+FloatToStr(DeltaY)+' C0     X'+FloatToStr(trunc(xa))+' Y'+FloatToStr(trunc(ya)));
                        i = i + step;
                        formMain.vs_infoUsinage.v_distanceTotale += m_estimerDeplacement(DeltaX, DeltaY);
                        formMain.vs_infoUsinage.v_nbrLignesProgramme++;
                    }
                }
                if ((Xa != x3) || (Ya != y3))
                {
                    //decalage à la fin
                    DeltaX = (x3 - Xa);
                    DeltaY = (y3 - Ya);
                    if ((DeltaX > v_deltaMiniX) | (DeltaY > v_deltaMiniY))
                    {
                        v_result.Add(string.Format("dX{0,8:0} X{1,8:0}dY{2,8:0} Y{3,8:0}", DeltaX, x3, DeltaY, y3));
                        formMain.vs_infoUsinage.v_nbrLignesProgramme++;
                    }
                }
                formMain.vs_infoUsinage.XCalcul = Xa;
                formMain.vs_infoUsinage.YCalcul = Ya;
                return v_result;
            }

            double m_calculAngle(double x1, double x2, double x3, double y1, double y2, double y3)
            {
                double result = 0;
                if (x3 == x2)
                {
                    if (x2 == x1)
                    {
                        if ((y3 >= y2) && (y2 >= y1)) { result = 180; }
                        if ((y3 >= y2) && (y2 < y1)) { result = 360; }
                        if ((y3 < y2) && (y2 >= y1)) { result = 360; }
                        if ((y3 < y2) && (y2 < y1)) { result = 180; }
                    }
                    else
                    {
                        if ((y3 >= y2) && (x2 >= x1)) { result = 270 - ((Math.Atan((y2 - y1) / (x2 - x1)) / Math.PI) * 180); }
                        if ((y3 >= y2) && (x2 < x1)) { result = 90 - ((Math.Atan((y2 - y1) / (x2 - x1)) / Math.PI) * 180); }
                        if ((y3 < y2) && (x2 >= x1)) { result = 90 - ((Math.Atan((y2 - y1) / (x2 - x1)) / Math.PI) * 180); }
                        if ((y3 < y2) && (x2 < x1)) { result = 270 - ((Math.Atan((y2 - y1) / (x2 - x1)) / Math.PI) * 180); }
                    }
                }
                if (x3 != x2)
                {
                    if (x1 == x2)
                    {
                        if ((y2 < y1) && (x3 < x2)) { result = ((Math.Atan((y3 - y2) / (x3 - x2)) / Math.PI) * 180) - 90; }
                        if ((y2 >= y1) && (x3 < x2)) { result = ((Math.Atan((y3 - y2) / (x3 - x2)) / Math.PI) * 180) + 270; }
                        if ((y2 < y1) && (x3 >= x2)) { result = ((Math.Atan((y3 - y2) / (x3 - x2)) / Math.PI) * 180) + 270; }
                        if ((y2 >= y1) && (x3 >= x2)) { result = ((Math.Atan((y3 - y2) / (x3 - x2)) / Math.PI) * 180) + 90; }
                    }
                    if (x1 != x2)
                    {
                        if ((x3 >= x2) && (x2 >= x1)) { result = Math.Atan((y3 - y2) / (x3 - x2)) - Math.Atan((y2 - y1) / (x2 - x1)) + Math.PI; }
                        if ((x3 < x2) && (x2 >= x1))
                        {
                            if (y3 < y2)
                            {
                                result = Math.Atan((y3 - y2) / (x3 - x2)) - Math.Atan((y2 - y1) / (x2 - x1));
                                if (result < 0) { result = (2 * Math.PI) + result; }
                            }
                            if (y3 >= y2) { result = (2 * Math.PI) + Math.Atan((y3 - y2) / (x3 - x2)) - Math.Atan((y2 - y1) / (x2 - x1)); }
                        }
                        if ((x3 >= x2) && (x2 < x1))
                        {
                            if (y3 > y2)
                            {
                                result = Math.Atan((y3 - y2) / (x3 - x2)) - Math.Atan((y2 - y1) / (x2 - x1));
                                if (result < 0) { result = (2 * Math.PI) + result; }
                            }
                            if (y3 <= y2) { result = Math.Atan((y3 - y2) / (x3 - x2)) - Math.Atan((y2 - y1) / (x2 - x1)) + (2 * Math.PI); }
                        }
                        if ((x3 < x2) && (x2 < x1)) { result = Math.Atan((y3 - y2) / (x3 - x2)) - Math.Atan((y2 - y1) / (x2 - x1)) + Math.PI; }

                        result = (result / Math.PI) * 180;
                    }
                    if (result < 0) { result = 180 + result; }
                    if (result > 360) { result = result - 360; }
                    if (result == 360) { result = 0; }
                }
                return result;
            }

            int m_estimerDeplacement(int vl_deltaX, int vl_deltaY)
            {
               /* double v_rapport = Math.Abs((double)(vl_deltaX) / (double)(vl_deltaY));
                if (v_rapport.ToString().ToLower() == "non numérique")
                { return 0; }
                double v_distance;
                if (v_rapport >= 1)
                {
                    v_distance = (((v_rapport - 1) * 5) + 7) * (Math.Abs(vl_deltaX) / (v_rapport * 5));
                }
                else
                {
                    v_rapport = 1 / v_rapport;
                    v_distance = (((v_rapport - 1) * 5) + 7) * (Math.Abs(vl_deltaY) / (v_rapport * 5));
                }*/
                double v_distance = Math.Sqrt((vl_deltaX * vl_deltaX) + (vl_deltaY * vl_deltaY));
                return (int)v_distance;
            }
    }
}
