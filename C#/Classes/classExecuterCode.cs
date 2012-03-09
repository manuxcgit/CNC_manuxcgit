using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace CNC
{
    public class c_ExecuteCode
    {
        #region Declarations
        public enum Ident { usiner, changeSens, arretSurZ, PasAPas, NOP, STOP, FinBloc, DiamOutil, ModifVitesse, SansAvanceLigneTraduite }
        public class c_InfoCode
        {
            public Ident v_ident;
            public int v_nbrPasX, v_nbrPasY, v_nextX, v_nextY, v_posZ;
            public int v_nbrPas, v_sensX, v_sensY, v_modifVitesse, v_distanceBloc;
            public char v_Axe;
            public double v_diamOutil;
            public bool v_avancerLigneTraduite, v_acceleration, v_deceleration, v_lineaire;
            public List<byte> v_liste;
            //v_ident : 0 : usine, 1 : change sens (v_nbrPas -> sens), 2 : arret sur Z, 3 : Pas à Pas

            public c_InfoCode(Ident ident, int pasX, int pasY, int nextX, int nextY, bool accel, bool deccel, int posZ)
            {//pour usiner ligne sur seult X ou Y
                v_ident = ident;
                v_nbrPasX = pasX; v_nbrPasY = pasY; v_nextX = nextX; v_nextY = nextY; v_acceleration = accel;
                v_deceleration = deccel; v_posZ = posZ;
            }

            public c_InfoCode(Ident ident, List<byte> liste, int nextX, int nextY, bool accel, bool deccel, int posZ)
            {//pour usiner ligne en X et Y
                v_ident = ident;
                v_liste = liste;
                v_nextX = nextX; v_nextY = nextY; v_acceleration = accel;
                v_deceleration = deccel; v_posZ = posZ;
            }

            public c_InfoCode(Ident ident, int posZ, bool AvancerLigneTraduite)
            {//arret sur Z
                v_ident = ident;
                v_posZ = posZ;
                v_avancerLigneTraduite = AvancerLigneTraduite;
            }

            public c_InfoCode(Ident ident)
            {//pour stop, fin bloc, Sans avance ligne traduite
                v_ident = ident;
            }

            public c_InfoCode(Ident ident, bool Lineaire, int distanceBloc)
            {//pour debut de bloc
                v_ident = ident;
                v_lineaire = Lineaire;
                v_distanceBloc = distanceBloc;
            }

            public c_InfoCode(Ident ident, char Axe, int pas, int sensX, int sensY)
            {//pour chgt sens
                v_ident = ident; v_Axe = Axe; v_nbrPas = pas; //v_paramPic = paramPic;
                v_sensX = sensX; v_sensY = sensY;
            }

            public c_InfoCode(Ident ident, double diam)
            {//pour diamètre Outil
                v_ident = ident;
                v_diamOutil = diam;
            }

            public c_InfoCode(Ident ident, int vitesse)
            {
                v_ident = ident;
                v_modifVitesse = vitesse;
            }
        }
        #region Static
        // IN et OUT
        public static Queue<c_InfoCode> vs_listCode = new Queue<c_InfoCode>();
        static int v_sensX, v_sensY;
        #endregion

        System.Diagnostics.Stopwatch sW = new System.Diagnostics.Stopwatch();
        int v_paramDecel, v_lastDemarrage, v_lastDistUsinee, v_distBloc, v_accel = 0, i;
        //byte v_paramPic, v_usbOut;
        bool v_doitContinuer = true, v_finBlocCirculaire;
        c_ControlleurParallele Controlleur = formMain.Controlleur;
        #endregion

        public c_ExecuteCode(int sensX, int sensY)
        {//initialise Thread
            v_sensX = sensX;
            v_sensY = sensY;
        }

        public void m_run()
        {
            while ((!c_usiner.vs_ArreterUsinage) & (v_doitContinuer))
            {
                if ((vs_listCode.Count > 0) & (v_doitContinuer))
                {
                    m_traiteLigne(vs_listCode.Dequeue());
                }
                Application.DoEvents();
            }
        }

        void m_traiteLigne(c_InfoCode v_Info)
        {
            switch (v_Info.v_ident)
            {
                case Ident.STOP:
                    m_lArretSurZ("STOP");
                    c_usiner.vs_indexLigne++;
                    break;
                case Ident.NOP:
                    c_usiner.vs_indexLigne++;
                    break;
                case Ident.PasAPas://debut de bloc 
                    if (c_usiner.vs_PasAPas)
                    { m_lArretSurZ("Pas à Pas"); };
                    v_distBloc = v_Info.v_distanceBloc;
                    break;
                case Ident.arretSurZ:
                    if (c_usiner.vs_ArretSurZ)
                    { m_lArretSurZ("Arret sur Z"); };
                    c_usiner.AxeZ.Actuel = v_Info.v_posZ;
                    if (v_Info.v_avancerLigneTraduite)
                    { c_usiner.vs_indexLigneCodeTraduit++; }
                    break;
                case Ident.changeSens:
                    m_setSens(v_Info.v_Axe, v_Info.v_nbrPas, v_Info.v_sensX, v_Info.v_sensY);
                    //pour eviter decalage
                    c_usiner.vs_indexLigneCodeTraduit--;
                    break;
                case Ident.FinBloc:
                    c_usiner.vs_indexLigne++;
                    break;
                case Ident.DiamOutil:
                    c_usiner.vs_diametreOutil = v_Info.v_diamOutil;
                    c_usiner.m_appelerDelegate(c_usiner.c_eventArgUsiner.enumEventsUsinage.modifDiamOutil);
                    c_usiner.vs_indexLigne++;
                    break;
                case Ident.ModifVitesse:
                    formMain.vs_infoUsinage.v_vitesseAvancePercent = v_Info.v_modifVitesse;
                    c_usiner.m_appelerDelegate(c_usiner.c_eventArgUsiner.enumEventsUsinage.vitesseAvanceModifie);
                    c_usiner.vs_indexLigne++;
                    break;
                case Ident.usiner:
                    m_usiner(v_Info);
                    break;
                case Ident.SansAvanceLigneTraduite:
                    break;
            }
            if (v_Info.v_ident != Ident.arretSurZ)
            { c_usiner.vs_indexLigneCodeTraduit++; }
            c_usiner.m_appelerDelegate(c_usiner.c_eventArgUsiner.enumEventsUsinage.gBarBuffer);
            c_usiner.m_appelerDelegate(c_usiner.c_eventArgUsiner.enumEventsUsinage.incrementerLBoxFichier);
            //Application.DoEvents();
        }

        void m_lArretSurZ(string vl_Texte)
        {
            c_usiner.vs_lArretSurZ = vl_Texte;
            c_usiner.m_appelerDelegate(c_usiner.c_eventArgUsiner.enumEventsUsinage.lArretSurZ);
            Application.DoEvents();
            while (c_usiner.vs_PauseUsinage)
            { Application.DoEvents(); }
        }

        void m_setSens(char Axe, int value, int sensX, int sensY)
        {
            //test si bon retour chgt sens
            while (!Controlleur.m_SetSens((sensX == 1) ^ c_usiner.vs_inverserX, (sensY == 1) ^ c_usiner.vs_inverserY))
            { MessageBox.Show("Pb d''inversion de sens !!"); }
            while (Controlleur.IsBusy)
            { Application.DoEvents(); }
            Application.DoEvents();
            for (int i = 1; i <= value; i++)
            {
                m_envoieCommande(Axe.ToString(), 15);
            }
            v_sensX = sensX;
            v_sensY = sensY;
            //if (Axe == 'X') { m_testerPas(value, 0); } else { m_testerPas(0, value); }
        }

        void m_testerPas(int vl_nbrPasX, int vl_nbrPasY)
        {
            if ((!formMain.v_paramPortParallele.v_verifierNbrPas) ||
                (c_usiner.vs_SimulerUsinage))
            { return; }
            int[] v_result = Controlleur.m_testePositionPortB();
            vl_nbrPasX *= (int)Math.Pow(2, (double)c_usiner.vs_Step);
            vl_nbrPasY *= (int)Math.Pow(2, (double)c_usiner.vs_Step);
            vl_nbrPasX &= 0xffff;
            vl_nbrPasY &= 0xffff;
            if ((v_result[0] != vl_nbrPasX) | (v_result[1] != vl_nbrPasY))
            {
                Controlleur.m_corrigePas('X', (vl_nbrPasX - v_result[0]) / 2);
                Controlleur.m_corrigePas('Y', (vl_nbrPasY - v_result[1]) / 2);
            }
        }

        bool m_usiner(c_InfoCode v_info) //return true si doit continer
        {
            c_usiner.AxeX.Next = v_info.v_nextX;
            c_usiner.AxeY.Next = v_info.v_nextY;
            c_usiner.vs_pourCentCodeEnCours = 0;
            c_usiner.m_appelerDelegate(c_usiner.c_eventArgUsiner.enumEventsUsinage.mettreAJourCoordNext);

            while (c_usiner.vs_PauseUsinage)
            { Application.DoEvents(); }
            if (c_usiner.vs_ArreterUsinage) { return false; }

            Controlleur.m_initialise_X_Y();

            double v_deltaX = v_info.v_nbrPasX * c_usiner.AxeX.AvanceParPas,
                    v_deltaY = v_info.v_nbrPasY * c_usiner.AxeY.AvanceParPas,
                    v_avanceMini = 0;

            v_paramDecel = (formMain.v_paramPortParallele.v_acceleration * (c_usiner.vs_vitesseAvance - formMain.v_paramPortParallele.v_vitesseMini)) / 5;
            if (v_info.v_acceleration) //|| (v_info.v_deceleration) || (v_finBlocCirculaire))
            { v_accel = c_usiner.vs_vitesseAvance - formMain.v_paramPortParallele.v_vitesseMini; }

            if (v_info.v_liste == null)
            #region 1 seul AXE
            {
                if (v_info.v_nbrPasX > 0)
                {
                    for (int i = 0; i < v_info.v_nbrPasX; i++)
                    {
                        while (c_usiner.vs_PauseUsinage)
                        { Application.DoEvents(); }
                        if (c_usiner.vs_ArreterUsinage) { return false; }

                        v_accel = m_accel_decel(ref v_info, v_info.v_nbrPasX, v_accel, i);

                        m_envoieCommande("X", v_accel);
                        c_usiner.AxeX.Actuel += c_usiner.AxeX.AvanceParPas * v_sensX;
                        c_usiner.vs_distanceUsinee += c_usiner.DISTANCE_X;
                        //v_nbrInstructions++;
                        v_deltaX -= c_usiner.AxeX.AvanceParPas;
                        c_usiner.vs_ResteAUsiner = new System.Drawing.Point((c_usiner.AxeX.Next - c_usiner.AxeX.Actuel) / 1000, (c_usiner.AxeY.Next - c_usiner.AxeY.Actuel) / 1000);
                        try { c_usiner.vs_pourCentCodeEnCours = ((int)(c_usiner.vs_distanceUsinee - v_lastDistUsinee) * 100) / v_distBloc; }
                        catch { }
                        //Application.DoEvents();
                    }
                }
                else
                {
                    for (int i = 0; i < v_info.v_nbrPasY; i++)
                    {
                        while (c_usiner.vs_PauseUsinage)
                        { Application.DoEvents(); }
                        if (c_usiner.vs_ArreterUsinage) { return false; }

                        v_accel = m_accel_decel(ref v_info, v_info.v_nbrPasY, v_accel, i);

                        m_envoieCommande("Y", v_accel);
                        c_usiner.AxeY.Actuel += c_usiner.AxeY.AvanceParPas * v_sensY;
                        c_usiner.vs_distanceUsinee += c_usiner.DISTANCE_X;
                        //v_nbrInstructions++;
                        v_deltaY -= c_usiner.AxeY.AvanceParPas;
                        c_usiner.vs_ResteAUsiner = new System.Drawing.Point((c_usiner.AxeX.Next - c_usiner.AxeX.Actuel) / 1000, (c_usiner.AxeY.Next - c_usiner.AxeY.Actuel) / 1000);
                        try { c_usiner.vs_pourCentCodeEnCours = ((int)(c_usiner.vs_distanceUsinee - v_lastDistUsinee) * 100) / v_distBloc; }
                        catch { }
                        //Application.DoEvents();
                    }
                }
                m_testerPas(v_info.v_nbrPasX, v_info.v_nbrPasY);
            }
            #endregion
            else
            #region 2 AXES
            {
                string v_axe = "";
                int v_index = 0;
                foreach (byte v_byte in v_info.v_liste)
                {
                    while (v_index < 4)
                    {
                        while (c_usiner.vs_PauseUsinage)
                        { Application.DoEvents(); }
                        if (c_usiner.vs_ArreterUsinage) { return false; }

                        v_axe = v_byte.m_trouverXY(v_index);
                        //usiner v_axe
                        v_accel = m_accel_decel(ref v_info, v_info.v_liste.Count * 4, v_accel, i);
                        m_envoieCommande(v_axe, v_accel);
                        //MAJ coordonnées
                        if (v_axe.Contains('X')) { c_usiner.AxeX.Actuel += (c_usiner.AxeX.AvanceParPas * v_sensX); }
                        if (v_axe.Contains('Y')) { c_usiner.AxeY.Actuel += (c_usiner.AxeY.AvanceParPas * v_sensY); }
                        if (v_axe == "XY") { c_usiner.vs_distanceUsinee += c_usiner.DISTANCE_XY; } else { c_usiner.vs_distanceUsinee += c_usiner.DISTANCE_X; }
                        c_usiner.vs_ResteAUsiner = new System.Drawing.Point((c_usiner.AxeX.Next - c_usiner.AxeX.Actuel) / 1000, (c_usiner.AxeY.Next - c_usiner.AxeY.Actuel) / 1000);
                        c_usiner.vs_pourCentCodeEnCours = ((int)(c_usiner.vs_distanceUsinee - v_lastDistUsinee) * 100) / v_distBloc;
                        Application.DoEvents();

                        v_index++;
                        i++;
                    }
                    v_index = 0;
                }

                /*       double v_delta = 0;
                       int i = 0;
                       if (v_info.v_nbrPasX > v_info.v_nbrPasY)
                       {
                           #region deltaX>deltaY
                           v_delta = (double)c_usiner.AxeX.AvanceParPas * v_info.v_rapportXY;
                           while (v_deltaX > 0)
                           {
                               while (c_usiner.vs_PauseUsinage)
                               { Application.DoEvents(); }
                               if (c_usiner.vs_ArreterUsinage) { return false; }

                               v_accel = m_accel_decel(ref v_info, v_info.v_nbrPasX, v_accel, i);
                               if (v_avanceMini < c_usiner.AxeY.AvanceParPas)
                               {
                                   //avancer X seul
                                   m_envoieCommande("X", v_accel);
                                   v_avanceMini += v_delta;
                                   c_usiner.vs_distanceUsinee += c_usiner.DISTANCE_X;
                               }
                               else
                               {
                                   //avance X et Y
                                   m_envoieCommande("XY", v_accel);
                                   v_deltaY -= c_usiner.AxeY.AvanceParPas;
                                   v_avanceMini += v_delta - (double)c_usiner.AxeX.AvanceParPas;
                                   c_usiner.AxeY.Actuel += (c_usiner.AxeY.AvanceParPas * v_sensY);
                                   c_usiner.vs_distanceUsinee += c_usiner.DISTANCE_XY;
                               }
                               v_deltaX -= c_usiner.AxeX.AvanceParPas;
                               c_usiner.AxeX.Actuel += (c_usiner.AxeX.AvanceParPas * v_sensX);
                               c_usiner.vs_ResteAUsiner = new System.Drawing.Point((c_usiner.AxeX.Next - c_usiner.AxeX.Actuel) / 1000, (c_usiner.AxeY.Next - c_usiner.AxeY.Actuel) / 1000);
                               c_usiner.vs_pourCentCodeEnCours = ((int)(c_usiner.vs_distanceUsinee - v_lastDistUsinee) * 100) / v_distBloc;
                               Application.DoEvents();
                               i++;
                           }
                           while (v_deltaY > 0)
                           {
                               m_envoieCommande("Y", v_accel);
                               v_deltaY -= c_usiner.AxeY.AvanceParPas;
                               c_usiner.AxeY.Actuel += (c_usiner.AxeY.AvanceParPas * v_sensY);
                           }
                           c_usiner.vs_ResteAUsiner = new System.Drawing.Point((c_usiner.AxeX.Next - c_usiner.AxeX.Actuel) / 1000, (c_usiner.AxeY.Next - c_usiner.AxeY.Actuel) / 1000);
                           c_usiner.vs_pourCentCodeEnCours = ((int)(c_usiner.vs_distanceUsinee - v_lastDistUsinee) * 100) / v_distBloc;
                           Application.DoEvents();
                           #endregion
                       }
                       else
                       {
                           #region deltaX<deltaY
                           v_delta = (double)c_usiner.AxeY.AvanceParPas * v_info.v_rapportXY;
                           while (v_deltaY > 0)
                           {
                               while (c_usiner.vs_PauseUsinage)
                               { Application.DoEvents(); }
                               if (c_usiner.vs_ArreterUsinage) { return false; }

                               v_accel = m_accel_decel(ref v_info, v_info.v_nbrPasY, v_accel, i);

                               if (v_avanceMini < c_usiner.AxeY.AvanceParPas)
                               {
                                   //avancer X seul
                                   m_envoieCommande("Y", v_accel);
                                   v_avanceMini += v_delta;
                                   c_usiner.vs_distanceUsinee += c_usiner.DISTANCE_X;
                               }
                               else
                               {
                                   //avance X et Y
                                   m_envoieCommande("XY", v_accel);
                                   v_deltaX -= c_usiner.AxeY.AvanceParPas;
                                   v_avanceMini += v_delta - (double)c_usiner.AxeY.AvanceParPas;
                                   c_usiner.AxeX.Actuel += (c_usiner.AxeX.AvanceParPas * v_sensX);
                                   c_usiner.vs_distanceUsinee += c_usiner.DISTANCE_XY;
                               }
                               v_deltaY -= c_usiner.AxeY.AvanceParPas;
                               c_usiner.AxeY.Actuel += (c_usiner.AxeY.AvanceParPas * v_sensY);
                               c_usiner.vs_ResteAUsiner = new System.Drawing.Point((c_usiner.AxeX.Next - c_usiner.AxeX.Actuel) / 1000, (c_usiner.AxeY.Next - c_usiner.AxeY.Actuel) / 1000);
                               //v_nbrInstructions++;
                               c_usiner.vs_pourCentCodeEnCours = ((int)(c_usiner.vs_distanceUsinee - v_lastDistUsinee) * 100) / v_distBloc;
                               //Application.DoEvents();
                               i++;
                           }
                           while (v_deltaX > 0)
                           {
                               m_envoieCommande("X", v_accel);
                               v_deltaX -= c_usiner.AxeX.AvanceParPas;
                               c_usiner.AxeX.Actuel += (c_usiner.AxeX.AvanceParPas * v_sensX);
                           }
                           c_usiner.vs_ResteAUsiner = new System.Drawing.Point((c_usiner.AxeX.Next - c_usiner.AxeX.Actuel) / 1000, (c_usiner.AxeY.Next - c_usiner.AxeY.Actuel) / 1000);
                           c_usiner.vs_pourCentCodeEnCours = ((int)(c_usiner.vs_distanceUsinee - v_lastDistUsinee) * 100) / v_distBloc;
                           Application.DoEvents();
                           #endregion
                       }
                       m_testerPas(v_info.v_nbrPasX, v_info.v_nbrPasY);
                 * */
            }
            #endregion
            if (v_info.v_deceleration) // pour cumul dans usinage circulaire
            {
                v_lastDistUsinee = (int)c_usiner.vs_distanceUsinee;
                v_finBlocCirculaire = false;
                i = 0;
            }
            else
            { v_finBlocCirculaire = ((v_distBloc - (c_usiner.vs_distanceUsinee - v_lastDistUsinee)) < v_paramDecel * c_usiner.DISTANCE_XY); }
            return true;
        }

        int m_accel_decel(ref c_InfoCode v_info, int nbrPas, int v_accel, int i)
        {
            //gere accel et decel
            if ((v_info.v_acceleration) | (!v_info.v_acceleration & !v_info.v_deceleration & !v_finBlocCirculaire & v_accel != 0))
            {
                if (i % formMain.v_paramPortParallele.v_acceleration == formMain.v_paramPortParallele.v_acceleration - 1)
                { v_accel -= 5; }
                v_info.v_acceleration = (v_accel > 0);
            }
            if (((v_info.v_deceleration) && (nbrPas - i < v_paramDecel)) || (v_finBlocCirculaire))
            {
                if (i % formMain.v_paramPortParallele.v_acceleration == formMain.v_paramPortParallele.v_acceleration - 1)
                {
                    v_accel += 5;
                    if (v_accel > c_usiner.vs_vitesseAvance - formMain.v_paramPortParallele.v_vitesseMini)
                    { v_accel = c_usiner.vs_vitesseAvance - formMain.v_paramPortParallele.v_vitesseMini; }
                }
            }
            return v_accel;
        }

        double m_envoieCommande(string liste, int correctifDemarrage)
        {
            double v_result = 0;
            try
            {
                if (c_usiner.vs_SimulerUsinage)
                {
                    System.Diagnostics.Stopwatch sW = new System.Diagnostics.Stopwatch();
                    sW.Start();
                    while (sW.Elapsed.TotalMilliseconds < 0.1)
                    { Application.DoEvents(); }
                    sW.Reset();
                    return 5;
                }
                else
                {
                    if (v_lastDemarrage != correctifDemarrage)
                    {
                        int v_vitesse = c_usiner.vs_vitesseAvance - correctifDemarrage;
                        if (v_vitesse < formMain.v_paramPortParallele.v_vitesseMini) { v_vitesse = formMain.v_paramPortParallele.v_vitesseMini; }
                        Controlleur.m_SetVitesseEtPas(v_vitesse, formMain.v_paramPortParallele.v_dureeImpulsion);
                        v_lastDemarrage = correctifDemarrage;
                    }
                    //pour eviter arret lors chgt parametres
                    while (Controlleur.IsBusy)
                    { Application.DoEvents(); }
                    v_result = Controlleur.m_Write(liste);
                    while (Controlleur.IsBusy)
                    { Application.DoEvents(); }
                    Application.DoEvents();
                    c_usiner.vs_elapsedTime = v_result;
                }
            }
            catch { }
            if (c_usiner.vs_enregistrerLOG)
            { c_usiner.vs_listElapsedTime.Add(v_result); }
            return v_result;
        }

        public static void m_corriger(int X, int Y)
        {
            int v_deltaX = X - c_usiner.AxeX.Actuel;
            int v_deltaY = Y - c_usiner.AxeY.Actuel;
            int v_oldSensX = v_sensX;
            int v_oldSensY = v_sensY;
            if (v_deltaX > 0)
            {
                v_sensX = -1;
            }
            else { v_sensX = 1; v_deltaX = Math.Abs(v_deltaX); }
            if (v_deltaY > 0)
            {
                v_sensY = -1;
            }
            else { v_sensY = 1; v_deltaY = Math.Abs(v_deltaY); }
            formMain.Controlleur.m_SetSens((v_sensX == 1) ^ c_usiner.vs_inverserX, (v_sensY == 1) ^ c_usiner.vs_inverserY);
            for (int i = 0; i < v_deltaX; i += c_usiner.AxeX.AvanceParPas)
            {
                formMain.Controlleur.m_Write("X");
            }
            for (int i = 0; i < v_deltaY; i += c_usiner.AxeY.AvanceParPas)
            {
                formMain.Controlleur.m_Write("Y");
            }
            v_sensX = v_oldSensX;
            v_sensY = v_oldSensY;
            formMain.Controlleur.m_SetSens((v_sensX == 1) ^ c_usiner.vs_inverserX, (v_sensY == 1) ^ c_usiner.vs_inverserY);
        }

    }
}
