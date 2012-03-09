using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Threading;

namespace CNC
{
    public delegate void d_eventUsinage(c_usiner.c_eventArgUsiner e);

    public class c_usiner : IDisposable
    {
        #region definitions
        #region Structs et Class
        public struct s_Axe
        {
            public int AvanceParPas, Compensation,  Sens, ActuelCalcul, Actuel, NextCalcul, Next;

            public void m_Initialiser()
            {
                Actuel = 0; Sens = 0; 
                ActuelCalcul = 0;
            }
        }

        public class c_eventArgUsiner : EventArgs
        {
            public enum enumEventsUsinage
            {
                incrementerLBoxFichier,
                modifDiamOutil,
                vitesseAvanceMaxi,
                vitesseAvanceNormale,
                vitesseAvanceModifie,
                threadTermine,
                mettreAJourCoordNext,
                gBarBuffer,
                lArretSurZ
            }
            public enumEventsUsinage Event;

        }
        #endregion
        #region Variables
        formMain.c_ParamAxe v_paramX, v_paramY;
        bool v_connected, v_peutChangerParam = true, v_debutBloc, v_finBloc;
        List<string> v_listCommandes;
        byte[] positionX = new byte[] { 10, 9, 5, 6 };//1010,1001,0101,0110
        byte[] positionY = new byte[] { 160, 144, 80, 96 };//10100000,10010000,01010000,01100000

        c_ControlleurParallele Controlleur = formMain.Controlleur;

        //  Thread threadPic;
        Thread t_executeCode;
        #endregion
        #region Static
        // variables pour controler usinage
        public static event d_eventUsinage EventUsinage;
        public static double vs_compensationVitesseUsinage;
        public static bool vs_ArretSurZ, vs_VitMaxiSurZ, vs_SimulerUsinage,
                    vs_PasAPas, vs_PauseUsinage, vs_ArreterUsinage, vs_enregistrerLOG,
                    vs_inverserX, vs_inverserY;
        public static Point vs_ResteAUsiner;
        ///////////////////////////////////
        //variables pour infos à l'exterieur
        public static double vs_diametreOutil, vs_elapsedTime, vs_distanceUsinee;
        public static int vs_pourCentCodeEnCours = 0;
        public static s_Axe AxeX, AxeY, AxeZ;
        public static int vs_indexLigne, vs_vitesseAvance, vs_nbrErreurs, vs_Step, vs_PasAcceleration, vs_PourCentAng, vs_indexLigneCodeTraduit;
        public static double DISTANCE_X, DISTANCE_XY;
        public static List<double> vs_listElapsedTime = new List<double>();//pour fichier LOG
        public static bool vs_isRunning;
        public static string vs_lArretSurZ = "";
        ////////////////////////////////////
        #endregion
        #endregion

        public c_usiner(List<string> vl_ListCommandes, formMain.c_ParamAxe ParamX,
            formMain.c_ParamAxe ParamY, formMain.enum_choixDll TypeDll,
            bool ArretSurZ, bool VitMaxSurZ, double CompensVitesse)
        {
            v_listCommandes = vl_ListCommandes;
            v_paramX = ParamX;
            v_paramY = ParamY;
            m_initialiserUsinage();
            formMain.eventMajParam += new d_majParam(m_chargerParam);
            v_connected = true;
            if (!Controlleur.m_IsConnected())
            {
                MessageBox.Show("Probleme de connection !!");


                /////////////////// a remettre apres test !!!!!!!


                //v_connected = false;

            }
            vs_compensationVitesseUsinage = CompensVitesse;
            vs_ArretSurZ = ArretSurZ;
            vs_VitMaxiSurZ = VitMaxSurZ;
        }

        #region Methodes
        public void m_run()
        {
            if (v_connected)
            {
                m_initialiserUsinage();
                Controlleur.m_Enable(!formMain.v_paramPortParallele.v_enableEtatBas);
                m_chargerParam();
                t_executeCode = new Thread(new ThreadStart(new c_ExecuteCode(AxeX.Sens, AxeY.Sens).m_run));
                t_executeCode.Start();

                for (int i = 0; i < v_listCommandes.Count; i++)
                {
                    #region traite ligne
                    string v_line = v_listCommandes[i];
                    string v_test = v_line;
                    if (v_line.Length > 19)
                    { v_test = v_line.Substring(0, 19); }
                    try
                    {
                        if (vs_ArreterUsinage)
                        { break; }
                        while (vs_PauseUsinage) { Application.DoEvents(); }

                        while ((c_ExecuteCode.vs_listCode.Count >= formMain.vs_infoUsinage.v_nbrLignesBuffer) & (!vs_ArreterUsinage))
                        { Application.DoEvents(); }

                        switch (v_test)
                        {
                            #region switch v_line
                            case "NOP":
                                //vs_indexLigne++;
                                c_ExecuteCode.vs_listCode.Enqueue(new c_ExecuteCode.c_InfoCode(c_ExecuteCode.Ident.NOP));
                                break;
                            case "STOP":
                                //MessageBox.Show("STOP");
                                c_ExecuteCode.vs_listCode.Enqueue(new c_ExecuteCode.c_InfoCode(c_ExecuteCode.Ident.STOP));
                                //vs_indexLigne++;
                                break;
                            case "Debut Bloc Lineaire":
                                //v_indexer = false;
                                /*if (vs_PasAPas)
                                { MessageBox.Show("Pas à Pas"); }*/
                                c_ExecuteCode.vs_listCode.Enqueue(new c_ExecuteCode.c_InfoCode(c_ExecuteCode.Ident.PasAPas, true, int.Parse(v_line.Substring(19))));
                                v_debutBloc = true;
                                //v_Angulaire = false;
                                break;
                            case "Debut Bloc Angulair":
                                //v_indexer = false;
                                /*if (vs_PasAPas)
                                { MessageBox.Show("Pas à Pas"); } */
                                c_ExecuteCode.vs_listCode.Enqueue(new c_ExecuteCode.c_InfoCode(c_ExecuteCode.Ident.PasAPas, false, int.Parse(v_line.Substring(19))));
                                v_debutBloc = true;
                                //v_Angulaire = true;
                                break;
                            case "Fin de Bloc":
                                // v_indexer = true;
                                c_ExecuteCode.vs_listCode.Enqueue(new c_ExecuteCode.c_InfoCode(c_ExecuteCode.Ident.FinBloc));
                                //vs_indexLigne++;
                                break;
                            default:
                                break;
                            #endregion
                        }
                        switch (v_line.Substring(0, 2))
                        {
                            #region Debut Ligne
                            case "Di": //Diametre
                                double v_temp = 1;
                                try
                                {
                                    v_temp = double.Parse(v_line.Substring(8)) * 5;
                                    //m_appelerDelegate(c_eventArgUsiner.enumEventsUsinage.modifDiamOutil);
                                }
                                catch { }
                                c_ExecuteCode.vs_listCode.Enqueue(new c_ExecuteCode.c_InfoCode(c_ExecuteCode.Ident.DiamOutil, v_temp));
                                //vs_indexLigne++;
                                break;
                            case "dX":
                                #region Usine Ligne
                                try
                                {
                                    if (v_listCommandes[i + 1] == "Fin de Bloc")
                                    { v_finBloc = true; }
                                    m_traiterLigne(v_line);
                                    v_debutBloc = false;
                                    v_finBloc = false;
                                }
                                catch { }
                                break;
                                #endregion
                            case "VI":
                                int v_percent = 50;
                                try
                                {
                                    v_percent = int.Parse(v_line.Substring(8));
                                }
                                catch { }
                                c_ExecuteCode.vs_listCode.Enqueue(new c_ExecuteCode.c_InfoCode(c_ExecuteCode.Ident.ModifVitesse, v_percent));
                                //vs_indexLigne++;
                                break;
                            default:
                                break;
                            #endregion
                        }
                        m_appelerDelegate(c_usiner.c_eventArgUsiner.enumEventsUsinage.gBarBuffer);
                    }
                    catch (Exception e) { MessageBox.Show("Probleme de conversion de " + v_line); }
                    #endregion
                }
                //terminé !!

                while ((t_executeCode.IsAlive) && (!vs_ArreterUsinage) && (c_ExecuteCode.vs_listCode.Count > 0))
                { Application.DoEvents(); }

                Controlleur.m_Enable(formMain.v_paramPortParallele.v_enableEtatBas);
                vs_isRunning = false;

                Application.DoEvents();
                try { t_executeCode.Abort(); }
                catch { }
                Application.DoEvents();
                { m_appelerDelegate(c_eventArgUsiner.enumEventsUsinage.threadTermine); }
            }
        }

        void m_initialiserUsinage()
        {
            vs_isRunning = true;
            vs_indexLigne = 0;
            vs_indexLigneCodeTraduit = 0;
            AxeX.m_Initialiser();
            AxeY.m_Initialiser();
            AxeZ.m_Initialiser();
            c_ExecuteCode.vs_listCode.Clear();
            vs_distanceUsinee = 0;
            vs_ArreterUsinage = false;
            vs_PauseUsinage = false;
            vs_nbrErreurs = 0;
            vs_listElapsedTime.Clear();
            vs_lArretSurZ = "";
            Controlleur.IsBusy = false;
        }

        public void Dispose() { }

        public static void m_appelerDelegate(c_eventArgUsiner.enumEventsUsinage vl_event)
        {
            d_eventUsinage v_newEvent = EventUsinage;
            if (v_newEvent != null)
            {
                v_newEvent(new c_eventArgUsiner { Event = vl_event });
            }
        }

        void m_traiterLigne(string ligne)
        {
            int v_deltaX, v_deltaY;
            int v_dZ = 0;
            try
            {
                AxeX.NextCalcul = int.Parse(ligne.Substring(12, 8).Trim()) * 1000;
                AxeY.NextCalcul = int.Parse(ligne.Substring(32, 8).Trim()) * 1000;
                v_deltaX = AxeX.NextCalcul - AxeX.ActuelCalcul;// int.Parse(ligne.Substring(2, 8).Trim()) * 1000;
                v_deltaY = AxeY.NextCalcul - AxeY.ActuelCalcul;// int.Parse(ligne.Substring(22, 8).Trim()) * 1000;
                if (ligne.Length > 41)
                {
                    #region deltaZ
                    v_dZ = int.Parse(ligne.Substring(42, 8));
                    int v_Z = int.Parse(ligne.Substring(52, 8));
                    if (v_dZ != 0)
                    {
                        {
                            c_ExecuteCode.vs_listCode.Enqueue(new c_ExecuteCode.c_InfoCode(c_ExecuteCode.Ident.arretSurZ, v_Z,
                                                                ((Math.Abs(v_deltaX) < AxeX.AvanceParPas) & (Math.Abs(v_deltaY) < AxeY.AvanceParPas))));
                            AxeZ.ActuelCalcul = v_Z;
                        }
                    }
                    #endregion
                }
                if ((Math.Abs(v_deltaX) < AxeX.AvanceParPas) & (Math.Abs(v_deltaY) < AxeY.AvanceParPas))
                {
                    if (v_dZ == 0)
                    { c_ExecuteCode.vs_listCode.Enqueue(new c_ExecuteCode.c_InfoCode(c_ExecuteCode.Ident.SansAvanceLigneTraduite)); }
                    return;
                }
                #region Compensation par chgt de sens
                if ((v_deltaX > 0) && (AxeX.Sens < 1))
                {
                    AxeX.Sens = 1;
                    m_compenser('X', AxeX.Compensation);
                }
                if ((v_deltaX < 0) && (AxeX.Sens > -1))
                {
                    AxeX.Sens = -1;
                    m_compenser('X', AxeX.Compensation);
                }
                if ((v_deltaY > 0) && (AxeY.Sens < 1))
                {
                    AxeY.Sens = 1;
                    m_compenser('Y', AxeY.Compensation);
                }
                if ((v_deltaY < 0) && (AxeY.Sens > -1))
                {
                    AxeY.Sens = -1;
                    m_compenser('Y', AxeY.Compensation);
                }
                #endregion
                v_deltaX = Math.Abs(v_deltaX);// *1000;
                v_deltaY = Math.Abs(v_deltaY);// *1000;
                double rapportXY;
                double v_avanceMini = 0;
                int v_nbrPasPrevus = 0;//, v_nbrPasX = 0, v_nbrPasY = 0;
                List<byte> v_liste = new List<byte>();
                byte v_byte = 0;
                int v_index = 0;
              
                if ((v_deltaX / AxeX.AvanceParPas) >= (v_deltaY / AxeY.AvanceParPas))
                {
                    #region deltaX>deltaY
                    if (v_deltaY < c_usiner.AxeY.AvanceParPas)
                    {
                        int v_nbrPasX = (int)((double)v_deltaX / AxeX.AvanceParPas);
                        AxeX.ActuelCalcul += v_nbrPasX * AxeX.AvanceParPas * AxeX.Sens;
                        c_ExecuteCode.vs_listCode.Enqueue(new c_ExecuteCode.c_InfoCode(c_ExecuteCode.Ident.usiner,
                                        v_nbrPasX, 0, AxeX.NextCalcul, AxeY.NextCalcul, v_debutBloc, v_finBloc, AxeZ.Actuel));
                    }
                    else
                    {
                        double rapportX = (double)(v_deltaX / AxeX.AvanceParPas);
                        double rapportY = (double)(v_deltaY / AxeY.AvanceParPas);
                        rapportXY = rapportY / rapportX;
                        v_nbrPasPrevus = (int)rapportX;
                        while (v_deltaX >= AxeX.AvanceParPas)
                        {

                            if (v_avanceMini < AxeY.AvanceParPas)
                            {
                                //v_nbrPasX++;
                                v_byte = v_byte.m_placerXY(v_index, "X");
                                v_avanceMini += ((double)AxeX.AvanceParPas * rapportXY);
                            }
                            else
                            {
                                v_deltaY -= AxeY.AvanceParPas;
                                v_avanceMini += (((double)AxeX.AvanceParPas * rapportXY) - (double)AxeX.AvanceParPas);
                                AxeY.ActuelCalcul += (AxeY.AvanceParPas * AxeY.Sens);
                                //v_nbrPasX++;
                                //v_nbrPasY++;
                                v_byte = v_byte.m_placerXY(v_index, "XY");
                            }
                            v_deltaX -= AxeX.AvanceParPas;
                            AxeX.ActuelCalcul += (AxeX.AvanceParPas * AxeX.Sens);
                            m_remplirListeByte(v_liste, ref v_byte, ref v_index);
                        }
                        while (v_deltaY >= AxeY.AvanceParPas)
                        {
                            //v_nbrPasY++;
                            v_byte = v_byte.m_placerXY(v_index, "Y");
                            v_deltaY -= AxeY.AvanceParPas;
                            AxeY.ActuelCalcul += (AxeY.AvanceParPas * AxeY.Sens);
                            m_remplirListeByte(v_liste, ref v_byte, ref v_index);
                        }
                        //ajoute le byte en cours
                        if (v_index > 0)
                        { v_liste.Add(v_byte); }
                        c_ExecuteCode.vs_listCode.Enqueue(new c_ExecuteCode.c_InfoCode(c_ExecuteCode.Ident.usiner,
                                v_liste, AxeX.NextCalcul, AxeY.NextCalcul, v_debutBloc, v_finBloc, AxeZ.Actuel));
                    }
                    #endregion
                }
                else
                {
                    #region deltaX<deltaY
                    rapportXY = ((double)(v_deltaX / AxeX.AvanceParPas) / (double)(v_deltaY / AxeY.AvanceParPas));
                    v_nbrPasPrevus = (int)((double)(v_deltaY / AxeY.AvanceParPas));
                    if (v_deltaX < c_usiner.AxeX.AvanceParPas)
                    {
                        int v_nbrPasY = v_nbrPasPrevus;
                        AxeY.ActuelCalcul += v_nbrPasPrevus * AxeY.AvanceParPas * AxeY.Sens;
                        c_ExecuteCode.vs_listCode.Enqueue(new c_ExecuteCode.c_InfoCode(c_ExecuteCode.Ident.usiner,
                                        0, v_nbrPasY, AxeX.NextCalcul, AxeY.NextCalcul, v_debutBloc, v_finBloc, AxeZ.Actuel));
                    }
                    else
                    {
                        while (v_deltaY >= AxeY.AvanceParPas)
                        {
                           
                            if (v_avanceMini < AxeX.AvanceParPas)
                            {
                                //avancer Y seul
                                //v_nbrPasY++;
                                v_byte = v_byte.m_placerXY(v_index, "Y");
                                v_avanceMini += (AxeY.AvanceParPas * rapportXY);
                            }
                            else
                            {
                                //avance X et Y
                                //v_nbrPasX++;
                                //v_nbrPasY++;
                                v_byte = v_byte.m_placerXY(v_index, "XY");
                                v_deltaX -= AxeX.AvanceParPas;
                                v_avanceMini += (((double)AxeY.AvanceParPas * rapportXY) - (double)AxeY.AvanceParPas);
                                AxeX.ActuelCalcul += (AxeX.AvanceParPas * AxeX.Sens);
                            }
                            v_deltaY -= AxeY.AvanceParPas;
                            AxeY.ActuelCalcul += (AxeY.AvanceParPas * AxeY.Sens);
                            m_remplirListeByte(v_liste, ref v_byte, ref v_index);
                        }
                        while (v_deltaX >= AxeX.AvanceParPas)
                        {
                            //v_nbrPasX++;
                            v_byte = v_byte.m_placerXY(v_index, "X");
                            v_deltaX -= AxeX.AvanceParPas;
                            AxeX.ActuelCalcul += (AxeX.AvanceParPas * AxeX.Sens);
                            m_remplirListeByte(v_liste, ref v_byte, ref v_index);
                        }
                        //ajoute le byte en cours
                        if (v_index > 0)
                        { v_liste.Add(v_byte); }
                        c_ExecuteCode.vs_listCode.Enqueue(new c_ExecuteCode.c_InfoCode(c_ExecuteCode.Ident.usiner,
                                v_liste, AxeX.NextCalcul, AxeY.NextCalcul, v_debutBloc, v_finBloc, AxeZ.Actuel));
                    }
                    #endregion
                }
            }
            catch { MessageBox.Show("Pb de Traduction à la ligne " + ligne); }
            Application.DoEvents();
        }

        private static void m_remplirListeByte(List<byte> v_liste, ref byte v_byte, ref int v_index)
        {
            v_index++;
            if (v_index == 4)
            {
                v_liste.Add(v_byte);
                v_index = 0;
                v_byte = 0;
            }
        }

        void m_compenser(char Axe, int value)
        {
            byte v_index ;
            bool v_inverser = false, v_result;
            int v_sens = 0;
            #region regle Sens
            switch (Axe)
            {
                case 'X': v_index = 1; v_sens = AxeX.Sens; v_inverser = vs_inverserX;
                    break;
                case 'Y': v_index = 3; v_sens = AxeY.Sens; v_inverser = vs_inverserY;
                    break;
            }
            v_result = (v_sens > 0) ^ (v_inverser);
            #endregion
            c_ExecuteCode.vs_listCode.Enqueue(new c_ExecuteCode.c_InfoCode(c_ExecuteCode.Ident.changeSens, Axe, value, AxeX.Sens, AxeY.Sens));
        }

        void m_chargerParam()
        {
            if (!v_peutChangerParam)
            { return; }
            bool v_oldEtatPause = vs_PauseUsinage;
            vs_PauseUsinage = true;
            v_peutChangerParam = false;
            while (Controlleur.IsBusy)
            { Application.DoEvents(); }
            Controlleur.IsBusy = true;
            AxeX.AvanceParPas = v_paramX.µmParTour / v_paramX.nbrPasParTour;
            AxeX.Compensation = v_paramX.compensation;
            vs_inverserX = v_paramX.inverserSens;
            AxeY.AvanceParPas = v_paramY.µmParTour / v_paramY.nbrPasParTour;
            AxeY.Compensation = v_paramY.compensation;
            vs_inverserY = v_paramY.inverserSens;
            Application.DoEvents();
            //Controlleur.m_SetSens((AxeX.Sens == 1) ^ c_usiner.vs_inverserX, (AxeY.Sens == 1) ^ c_usiner.vs_inverserY);
            Controlleur.m_SetVitesseEtPas(vs_vitesseAvance, formMain.v_paramPortParallele.v_dureeImpulsion);
            Application.DoEvents();
            DISTANCE_X = (double)AxeX.AvanceParPas / 1000;
            DISTANCE_XY = Math.Sqrt((((double)AxeX.AvanceParPas / 1000) * ((double)AxeX.AvanceParPas / 1000)) +
                (((double)AxeY.AvanceParPas / 1000) * ((double)AxeY.AvanceParPas / 1000)));

            Controlleur.IsBusy = false;
            Application.DoEvents();
            v_peutChangerParam = true;
            vs_PauseUsinage = v_oldEtatPause;
        }
        #endregion
    }

    public struct s_InfosUsinage
    {
        public int v_nbrLignesProgramme, v_distanceTotale, v_moduloTimer, v_nbrLignesBuffer;
        public int XCalcul, YCalcul, ZCalcul;
        public int v_vitesseAvancePercent;
        public Point v_resteAUsiner;
    }
}
