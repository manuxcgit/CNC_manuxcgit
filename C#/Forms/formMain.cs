using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Threading;


namespace CNC
{
    public delegate void d_majParam();
    public delegate void d_dessinerVisu();
    public delegate void d_chargerPointsVisu();

    public partial class formMain : Form
    {
        #region declarations
        // toutes les coordonnees usinage sont en 1/1000000 de mm pour precision si vis mere n'est pas multiple de mm
        // les coord iso sont au 1/1000
        #region Classes
        public class c_ParamAxe
        {
            public int nbrPasParTour;
            public int µmParTour;
            public int compensation;
            public Boolean inverserSens;

            public c_ParamAxe(int pas, int µm, int comp, bool inv)
            {
                nbrPasParTour = pas; µmParTour = µm; compensation = comp; inverserSens = inv;
            }

            public c_ParamAxe(c_ParamAxe param)
            {
                nbrPasParTour = param.nbrPasParTour; µmParTour = param.µmParTour; compensation = param.compensation; inverserSens = param.inverserSens;
            }
        }
        public class c_ParamGeneraux
        {
            public int v_precisonEllipse;
            public int v_vitesseAvance;
            public int v_nbrChiffresApresVirgule;
            public int v_threadUsinerPriorityInt;
            public int v_freqMaxi;
            public bool v_arreterSurZ;
            public bool v_ignorerX0Y0;
            public enum_choixDll v_dll;
            public bool v_demiPas;
            public bool v_vitesseMaxiSiZ;
            public double v_compensationVitesseUsinage; //pour definir le temps mort 
            public int v_Step, v_nbrLignesDansBuffer;//0 si full, sinon 1/2,1/4 , ou 1/8

            public c_ParamGeneraux(int precision, int vitesse, int nbrChiffresApresVirgule, bool arretSurZ, int threadPriority,
                                   bool ignorerX0Y0, enum_choixDll choixDll, bool demiPas, bool vitesseMax, int freqMaxi, double compens,
                                    int step, int nbrLignes)
            {
                v_precisonEllipse = precision;
                v_vitesseAvance = vitesse;
                v_nbrChiffresApresVirgule = nbrChiffresApresVirgule;
                v_arreterSurZ = arretSurZ;
                v_threadUsinerPriorityInt = threadPriority;
                v_ignorerX0Y0 = ignorerX0Y0;
                v_dll = choixDll;
                v_demiPas = demiPas;
                v_vitesseMaxiSiZ = vitesseMax;
                v_freqMaxi = freqMaxi;
                v_compensationVitesseUsinage = compens;
                v_Step = step;
                v_nbrLignesDansBuffer = nbrLignes;
            }
        }
        public class c_ParamPortParallele
        {
            public int v_adresseportParallele;
            public bool v_enableEtatBas, v_verifierNbrPas;
            public int v_acceleration, v_pourCentAng, v_vitesseMini, v_dureeImpulsion;

            public c_ParamPortParallele(int adress, bool enable, int acceleration, int pourCentAng, 
                int vitMini, bool verifierNbrPas, int dureeImpuls)
            {
                v_adresseportParallele = adress;
                v_enableEtatBas = enable;
                v_acceleration = acceleration;
                v_pourCentAng = pourCentAng;
                v_vitesseMini = vitMini;
                v_verifierNbrPas = verifierNbrPas;
                v_dureeImpulsion = dureeImpuls;
            }
        }
        #endregion
        #region Threads
        Thread threadUsiner;
        Thread threadDessiner;
        #endregion
        public enum enum_choixDll { Delphi, CPlusPlus, IOWKitInterne, PicUsb, Port_Parallele };
        string v_repertoireExe, v_nomFichier, v_lArretSurZ;
        double v_echelleVisu;
        int v_vitesseUsinage = 0;
        Point v_decalageVisu, v_lastCoordDessinee, v_posSouris, v_decalageVisuPBox, v_lastDebutLigne;
        bool v_usinageEnCours, v_stop, v_mouseOverPBox = false, v_dessinNextEnCours;
        Dictionary<char, c_ParamAxe> v_listParamAxe;
        c_ParamGeneraux v_paramGeneraux;
        Cursor v_cursor;
        ThreadPriority v_threadUsinerPriority;
        // List<float> v_listElapsedTime;

        public delegate void d_delegateInterThread();
        public static event d_majParam eventMajParam;
        public static event d_dessinerVisu eventDessinerVisu;
        public static event d_chargerPointsVisu eventChargerPointsVisu;
        public static s_InfosUsinage vs_infoUsinage;
        public static c_ParamPortParallele v_paramPortParallele;
        public static c_ControlleurParallele Controlleur = new c_ControlleurParallele();

        public ListBox p_lBoxFichier { get { return lBoxFichier; } set { lBoxFichier = value; } }
        public ListBox p_lBoxCodeTraduit { get { return lBoxCodeTraduit; } }
        public WindowsControlLibrary1.GaugeBar p_pBar { get { return pBar; } set { pBar = value; } }
        public Point p_decalageVisu { get { return v_decalageVisu; } }
        public Double p_echelleVisu { get { return v_echelleVisu; } }
        public PictureBox p_pBox { get { return pBox; } set { pBox = value; } }

        #endregion

        #region Methodes
        public formMain()
        {
            InitializeComponent();
            v_repertoireExe = new FileInfo(Application.ExecutablePath).DirectoryName + '\\';
            v_echelleVisu = 1;
            for (int i = 0; i < 5; i++) { comboBoxPrioriteThread.Items.Add(((ThreadPriority)i).ToString()); }
            m_chargerIni();

            #region initialise CONTROLLEUR
            m_initialiserControlleur();
            if (!Controlleur.m_IsConnected())
            {
                MessageBox.Show("Pas d'interface", v_paramGeneraux.v_dll.ToString());
                tSSLInfo.Text = "Pas de Controlleur initialisé";
            }
            else
            {
                tSSLInfo.Text = "Controlleur initialisé FIRMWARE : " + Controlleur.Firmware;
                /*    if (v_Controlleur is c_ControlleurPicUsb)
                    {
                        lFirmWare.Text = (v_Controlleur as c_ControlleurPicUsb).Firmware;
                        cmdLireRCPUsb.Enabled = true;
                        cmdEcrireRCPUsb.Enabled = true;
                    } */
                Controlleur.m_Enable(v_paramPortParallele.v_enableEtatBas);
                Controlleur.m_ActiveRelai(true);
            }

            #endregion

            this.MouseWheel += new MouseEventHandler(e_formMain_MouseWheel);
            pBox.MouseEnter += new EventHandler(e_pBox_MouseEnter);
            pBox.MouseLeave += new EventHandler(e_pBox_MouseLeave);
            lBoxFichier.SelectedIndexChanged += new EventHandler(m_centrerlBoxFichier);
            var info = from ligne in CNC.Properties.Resources.AProposDe.Split("\r\n".ToCharArray())
                       where (ligne != "")
                       select ligne;
            rTBAProposDe.Lines = info.ToArray();
           /* info = from ligne in CNC.Properties.Resources.fimwarePic.Split("\r\n".ToCharArray())
                   where ligne != ""
                   select ligne;
            rTBPic.Lines = info.ToArray();*/
            c_usiner.EventUsinage += new d_eventUsinage(e_usiner_EventUsinage);
            threadDessiner = new Thread(new ThreadStart(new c_dessinerVisu(this).m_run));
            threadDessiner.Start();
            lArretSurZ.Enabled = false;
            lArretSurZ.Text = "";
        }

        void m_actualisertBarVitesse()
        {
            int v_actuel = tBarVitesse.Value;
            tBarVitesse.Maximum = ((v_listParamAxe['X'].µmParTour / v_listParamAxe['X'].nbrPasParTour) * (v_paramGeneraux.v_freqMaxi)) / 500000;
            /*     if (v_paramGeneraux.v_demiPas)
                 { tBarVitesse.Maximum = tBarVitesse.Maximum / 2; } */
            if (v_actuel > 1)
            {
                if (v_actuel < tBarVitesse.Maximum)
                { tBarVitesse.Value = v_actuel; }
                else
                { tBarVitesse.Value = tBarVitesse.Maximum; }
                e_tBarVitesse_Scroll(null, null);
            }
        }

        void m_afficherCoordonnes()
        {
            try
            {
                double v_X = (double)c_usiner.AxeX.Actuel / 1000000, v_Y = (double)c_usiner.AxeY.Actuel / 1000000, v_Z = (double)c_usiner.AxeZ.Actuel / 1000;
                int v_x = (int)(v_X * 10), v_y = (int)(v_Y * 10);
                tBoxCoordX.Text = string.Format("{0:0.000}", v_X);
                tBoxCoordY.Text = string.Format("{0:0.000}", v_Y);
                tBoxCoordZ.Text = string.Format("{0:0.000}", v_Z);
                tBoxResteX.Text = string.Format("{0:0.000}", (double)c_usiner.vs_ResteAUsiner.X / 1000);
                tBoxResteY.Text = string.Format("{0:0.000}", (double)c_usiner.vs_ResteAUsiner.Y / 1000);
                m_afficherTempsEstime();
                try { pBar.Value = (float)c_usiner.vs_distanceUsinee; }
                catch { }
                if (lInfoAction.Text != "Arrété")
                { lInfoAction.Text = string.Format("{0:0} Hz", 1000 / c_usiner.vs_elapsedTime); }
                vs_infoUsinage.v_moduloTimer++;
                if ((vs_infoUsinage.v_moduloTimer % 3 == 0) & (!v_dessinNextEnCours))
                {
                    #region dessine outil
                    Bitmap bmp = new Bitmap((Bitmap)pBox.Image);
                    Graphics v_grap = Graphics.FromImage(bmp);
                    if (c_usiner.AxeZ.Actuel >= 0)
                    {
                        Point v_temp = new Point(v_x, v_y).Mult(v_echelleVisu).Div().Plus(new Point(bmp.Width / 2, bmp.Height / 2)).Plus(v_decalageVisu).InvY(bmp.Height);
                        Point v_old = v_lastCoordDessinee.Mult(v_echelleVisu).Div().Plus(new Point(bmp.Width / 2, bmp.Height / 2)).Plus(v_decalageVisu).InvY(bmp.Height);
                        v_grap.DrawLine(new Pen(Color.Red),
                         v_old,
                          v_temp);
                    }
                    else
                    {
                        v_grap.DrawArc(new Pen(Color.White), new Rectangle(
                            (int)((v_lastCoordDessinee.X - c_usiner.vs_diametreOutil) * v_echelleVisu / 10) + (bmp.Width / 2) + v_decalageVisu.X,
                            (int)((bmp.Height / 2) - (((v_lastCoordDessinee.Y + c_usiner.vs_diametreOutil) * v_echelleVisu / 10) + v_decalageVisu.Y)),
                            (int)(c_usiner.vs_diametreOutil * v_echelleVisu / 5), (int)(c_usiner.vs_diametreOutil * v_echelleVisu / 5)), 0, 360);
                        v_lastCoordDessinee = new Point(v_x, v_y);
                        v_grap.DrawArc(new Pen(Color.Red), new Rectangle(
                            (int)((v_lastCoordDessinee.X - c_usiner.vs_diametreOutil) * v_echelleVisu / 10) + (bmp.Width / 2) + v_decalageVisu.X,
                            (int)((bmp.Height / 2) - (((v_lastCoordDessinee.Y + c_usiner.vs_diametreOutil) * v_echelleVisu / 10) + v_decalageVisu.Y)),
                            (int)(c_usiner.vs_diametreOutil * v_echelleVisu / 5), (int)(c_usiner.vs_diametreOutil * v_echelleVisu / 5)), 0, 360);
                    }
                    v_lastCoordDessinee = new Point(v_x, v_y);
                    pBox.Image = bmp;
                    #endregion
                    gBarCodeEnCours.Value = ((float)c_usiner.vs_pourCentCodeEnCours / 100) * gBarCodeEnCours.Maximum;
                    if (lArretSurZ.Text == v_lArretSurZ)
                    { lArretSurZ.Text = ""; }
                    else
                    { lArretSurZ.Text = v_lArretSurZ; }
                    if (!lArretSurZ.Focused) { lArretSurZ.Focus(); }
                }
                tSSLNbrErreurs.Text = string.Format("{0} erreurs d'usinage", c_usiner.vs_nbrErreurs);
            }
            catch (Exception erreur) { }
        }

        void m_afficherTempsEstime()
        {
            try
            {
                int TempEstime = (vs_infoUsinage.v_distanceTotale - (int)c_usiner.vs_distanceUsinee) / (v_paramGeneraux.v_vitesseAvance * 100);
                tBoxTempsEstime.Text = string.Format("{0:00}:{1:00}:{2:00}", TempEstime / 3600, (TempEstime / 60) % 60, TempEstime % 60);
            }
            catch { }
        }

        void m_appelerDelegateInterThread(d_delegateInterThread v_delegate)
        {
            if (this.InvokeRequired)
            {
                var v_d = new d_delegateInterThread(v_delegate);
                try { Invoke(v_d); }
                catch { }
            }
            else
            {
                v_delegate();
            }
        }

        void m_centrerlBoxFichier(object sender, EventArgs e)
        {
            //calculer nbr lignes visibles
            int v_nLV = lBoxFichier.Height / lBoxFichier.ItemHeight;
            if (lBoxFichier.SelectedIndex > (v_nLV / 2))
            { lBoxFichier.TopIndex = lBoxFichier.SelectedIndex - (v_nLV / 2); }
        }

        void m_chargerIni()
        {
            v_listParamAxe = new Dictionary<char, c_ParamAxe>();
            //IniFile v_iniFile = new IniFile(v_repertoireExe + "CNC.ini");
            IniReg.c_IniRegBase v_iniFile = new IniReg.c_Ini("CNC.ini");
            c_ParamAxe ParamGeneraux = new c_ParamAxe(0, 1, 2, true);
            #region Paramatres Axes
            var listeC = from Control C in tabPageParams.Controls
                         where C is GroupBox && C.Name.StartsWith("gBoxAxe")
                         from Control C1 in C.Controls
                         where C1 is TextBox || C1 is CheckBox
                         select C1;
            foreach (char c in "XYZ".ToCharArray())
            {
                try
                {
                    //   c_ParamAxe v_paramTemp = v_iniFile.m_Read(new c_ParamAxe(400, 2000000, 20, false),"Param" + c);
                    v_listParamAxe.Add(c, v_iniFile.m_Read(new c_ParamAxe(400, 2000000, 20, false), "Param" + c));// new c_ParamAxe((c_ParamAxe)v_iniFile.m_Read(GetValue("Param" + c, new c_ParamAxe(400, 2000000, 20, false))));
                }
                catch
                { v_listParamAxe.Add(c, new c_ParamAxe(400, 2000000, 0, false)); }
                listeC.First(x => x.Name == "tBoxPas" + c).Text = v_listParamAxe[c].nbrPasParTour.ToString();
                listeC.First(x => x.Name == "tBoxµmParTour" + c).Text = v_listParamAxe[c].µmParTour.ToString();
                listeC.First(x => x.Name == "tBoxCompens" + c).Text = v_listParamAxe[c].compensation.ToString();
                CheckBox cB = new CheckBox();
                cB = (CheckBox)listeC.First(x => x.Name == "cBoxInverser" + c);
                cB.Checked = (v_listParamAxe[c].inverserSens == true);
            }
            #endregion
            v_paramGeneraux = v_iniFile.m_Read(new c_ParamGeneraux(4, 10, 2, true, 2, true, enum_choixDll.IOWKitInterne, false, true, 400, 0, 0, 20), "General");// (c_ParamGeneraux)v_iniFile.GetValue("General", new c_ParamGeneraux(4, 10, 2, true, 2, true, enum_choixDll.IOWKitInterne, false, true, 400, 0));
            v_paramPortParallele = v_iniFile.m_Read(new c_ParamPortParallele(888, true, 1, 80, 10, true, 50), "PortParallele");
            tBFreqMaxi.Text = v_paramGeneraux.v_freqMaxi.ToString();
            m_actualisertBarVitesse();
            cBVitesseMaxiZ.Checked = v_paramGeneraux.v_vitesseMaxiSiZ;
            try { tBarVitesse.Value = v_paramGeneraux.v_vitesseAvance / 5; }
            catch { tBarVitesse.Value = tBarVitesse.Maximum; v_paramGeneraux.v_vitesseAvance = tBarVitesse.Maximum; }
            m_actualisertBarVitesse();
            //cBDemiPas.Checked = v_paramGeneraux.v_demiPas;
            e_tBarVitesse_Scroll(null, null);
            tBarEllipse.Value = v_paramGeneraux.v_precisonEllipse;
            e_tBarEllipse_Scroll(null, null);
            tBNbrLignesDansBuffer.Text = v_paramGeneraux.v_nbrLignesDansBuffer.ToString();
            vs_infoUsinage.v_nbrLignesBuffer = v_paramGeneraux.v_nbrLignesDansBuffer;
            gBarBufferUsinage.Maximum = v_paramGeneraux.v_nbrLignesDansBuffer;

            if (v_paramGeneraux.v_nbrChiffresApresVirgule == 2) { rButton100.Select(); } else { rButton1000.Select(); }
            cBoxArreterSurZ.Checked = v_paramGeneraux.v_arreterSurZ;
            v_threadUsinerPriority = (ThreadPriority)v_paramGeneraux.v_threadUsinerPriorityInt;
            comboBoxPrioriteThread.SelectedIndex = (int)v_threadUsinerPriority;
            cBoxIgnorerX0Y0.Checked = v_paramGeneraux.v_ignorerX0Y0;
            switch (v_paramGeneraux.v_dll)
            {
                case enum_choixDll.PicUsb:
                    rBPicUsb.Checked = true;
                    break;
                case enum_choixDll.Port_Parallele:
                    rBParallele.Checked = true;
                    break;
            }
            //port // dans initialise Controlleur

            cBStep.SelectedIndex = v_paramGeneraux.v_Step;
        }

        void m_dessinerVisu()
        {
            if (c_dessinerVisu.vs_IsWorking) { return; }
            if ((threadDessiner != null) && (threadDessiner.IsAlive))
            {
                d_dessinerVisu dessinerVisu = eventDessinerVisu;
                if (dessinerVisu != null) { dessinerVisu(); }
            }
            pBox.Enabled = true;
            lDiametreOutil.BackColor = Color.Black;
        }

        void m_initialiserControlleur()
        {
           // tabPageUSB.Dispose();
            tBAdressePortParallele.Text = string.Format("0x{0:X}", v_paramPortParallele.v_adresseportParallele);
            cBEnableEtatBas.Checked = v_paramPortParallele.v_enableEtatBas;
            tBAcceleration.Text = v_paramPortParallele.v_acceleration.ToString();
            tBPourCentVitAng.Text = v_paramPortParallele.v_pourCentAng.ToString();
            c_usiner.vs_PasAcceleration = v_paramPortParallele.v_acceleration;
            c_usiner.vs_PourCentAng = v_paramPortParallele.v_pourCentAng;
            c_ControlleurParallele.v_adressePort = v_paramPortParallele.v_adresseportParallele;
            tBVitesseMini.Text = v_paramPortParallele.v_vitesseMini.ToString();
            cbNbrPasEnvoyes.Checked = v_paramPortParallele.v_verifierNbrPas;
            tBDureeImpulsion.Text = v_paramPortParallele.v_dureeImpulsion.ToString();
        }


        void m_majParam()
        {
            if ((threadUsiner != null) && (threadUsiner.IsAlive))
            {
                d_majParam majParam = eventMajParam;
                if (majParam != null) { majParam(); }
            }
        }

        void m_remplirlBoxCodeTraduit(List<string> vl_liste)
        {
            lBoxCodeTraduit.Items.Clear();
            lBoxCodeTraduit.Items.AddRange(vl_liste);
            v_decalageVisu = new Point(0, 0);
            v_lastCoordDessinee = new Point(0, 0);
            v_echelleVisu = 1;
            if ((threadDessiner != null) && (threadDessiner.IsAlive))
            {
                d_chargerPointsVisu chargerPoints = eventChargerPointsVisu;
                if (chargerPoints != null) { chargerPoints(); }
            }
            m_dessinerVisu();
            m_afficherTempsEstime();
            lNbrLignes.Text = "Nbr Lignes : " + vs_infoUsinage.v_nbrLignesProgramme.ToString();
            bDemarrer.Enabled = true;
            bStop.Enabled = true;
        }

        void m_sauverIni()
        {
            IniReg.c_IniRegBase v_iniFile = new IniReg.c_Ini("CNC.ini");
            foreach (char c in "XYZ".ToCharArray())
            {
                v_iniFile.m_Write(v_listParamAxe[c], "Param" + c);
            }
            v_iniFile.m_Write(v_paramGeneraux, "General");
            v_iniFile.m_Write(v_paramPortParallele, "PortParallele");
        }

        void m_traiterBoutonVisu(object sender, EventArgs e)
        {
            Button v_button = new Button();
            try
            {
                v_button = (Button)sender;
                switch (v_button.Name)
                {
                    case "bVisuGauche":
                        v_decalageVisu = v_decalageVisu.Plus(-50, 0);
                        break;
                    case "bVisuDroite":
                        v_decalageVisu = v_decalageVisu.Plus(50, 0);
                        break;
                    case "bVisuHaut":
                        v_decalageVisu = v_decalageVisu.Plus(0, 50);
                        break;
                    case "bVisuBas":
                        v_decalageVisu = v_decalageVisu.Plus(0, -50);
                        break;
                    case "bVisuLoupePlus":
                        v_echelleVisu++;
                        break;
                    case "bVisuLoupeMoins":
                        if (v_echelleVisu > 1) { v_echelleVisu--; }
                        break;
                }
                lEchelle.Text = v_echelleVisu.ToString();
                m_dessinerVisu();
            }
            catch { }
        }

        #endregion

        #region Events

        void e_bCharger_Click(object sender, EventArgs e)
        {
            var oFD = new OpenFileDialog();
            oFD.Filter = "fichiers ISO|*.iso";
            if (oFD.ShowDialog() == DialogResult.OK)
            {
                using (c_traduireIso v_tI = new c_traduireIso(oFD.FileName, this, v_paramGeneraux.v_ignorerX0Y0,
                                                v_paramGeneraux.v_nbrChiffresApresVirgule, v_paramGeneraux.v_precisonEllipse,
                                                v_listParamAxe['X'].µmParTour / (v_listParamAxe['X'].nbrPasParTour * 1000), v_listParamAxe['Y'].µmParTour / (v_listParamAxe['Y'].nbrPasParTour * 1000)))
                {
                    v_nomFichier = oFD.FileName;
                    m_remplirlBoxCodeTraduit(v_tI.m_convertirFichier());
                    bDemarrer.Enabled = true;
                    bPasAPas.Enabled = true;
                    bStop.Enabled = true;
                    bRafraichir.Enabled = true;
                }
            }
        }

        void e_bDemarrer_Click(object sender, EventArgs e)
        {
            if (bDemarrer.Text == "Reprendre")
            {
                if (lArretSurZ.Enabled == true)
                { return; }
                bDemarrer.Text = "Demarrer";
                bDemarrer.Enabled = false;
                c_usiner.vs_PauseUsinage = false;
                v_stop = false;
                this.KeyPress += new KeyPressEventHandler(e_formMain_KeyPress);
                lInfoAction.Text = "";
                return;
            }
            if ((v_nomFichier == null) || (v_usinageEnCours))
            { return; }
            if (MessageBox.Show("Confirmez vous l'usinage ?", "DEBUT", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
            { return; }
            m_dessinerVisu();
            pBar.Maximum = vs_infoUsinage.v_distanceTotale;
            threadUsiner = new Thread(new ThreadStart(new c_usiner(lBoxCodeTraduit.Items.ToListString(), v_listParamAxe['X'], v_listParamAxe['Y'],
                v_paramGeneraux.v_dll, v_paramGeneraux.v_arreterSurZ, v_paramGeneraux.v_vitesseMaxiSiZ,
                v_paramGeneraux.v_compensationVitesseUsinage).m_run));
            try { threadUsiner.Priority = v_threadUsinerPriority; }
            catch { MessageBox.Show("Impossible de regler la priorité de l'usinage !!", "ERREUR"); }
            threadUsiner.Start();
            Application.DoEvents();
            //    e_tBarVitesse_Scroll(null, null);
            while (!threadUsiner.IsAlive)
            { Application.DoEvents(); }
            timerAffichage.Enabled = true;
            v_usinageEnCours = true;
            bDemarrer.Enabled = false;
            bChargerFichier.Enabled = false;
            bRafraichir.Enabled = false;
            this.KeyPress += new KeyPressEventHandler(e_formMain_KeyPress);
            lInfoAction.Text = "";
            lArretSurZ.Text = "";
        }

        void e_formMain_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!lArretSurZ.Enabled) { e_bSTop_Click(null, null); }
            else
            {
                if (e.KeyChar == 13) { e_lArretSurZ_Click(null, null); }
            }
        }

        void e_bExecuterListe_Click(object sender, EventArgs e)
        {
            lBoxFichier.Items.Clear();
            lBoxFichier.Items.AddRange(lBoxSaisieDirecte.Items);
            v_nomFichier = "";
            using (c_traduireIso v_tI = new c_traduireIso("", this, v_paramGeneraux.v_ignorerX0Y0,
                                                v_paramGeneraux.v_nbrChiffresApresVirgule, v_paramGeneraux.v_precisonEllipse,
                                                v_listParamAxe['X'].µmParTour / (v_listParamAxe['X'].nbrPasParTour * 1000), v_listParamAxe['Y'].µmParTour / (v_listParamAxe['Y'].nbrPasParTour * 1000)))
            {
                m_remplirlBoxCodeTraduit(v_tI.m_convertirFichier());
            }
            tabControlParams.SelectedIndex = 0;
        }

        void e_bLogUsinage_Click(object sender, EventArgs e)
        {
            if (c_usiner.vs_listElapsedTime != null)
            {
                StreamWriter sW = new StreamWriter(v_repertoireExe + "Log.txt");
                int modulo = 0;
                StringBuilder result = new StringBuilder();
                foreach (float eT in c_usiner.vs_listElapsedTime)
                {
                    result.Append(string.Format("{0,6}", string.Format("{0:0.00} ", eT)));
                    modulo++;
                    if (modulo % 20 == 0)
                    {
                        sW.WriteLine(result.ToString());
                        result.Remove(0, result.Length);
                    }
                }
                sW.Flush();
                sW.Close();
            }
            System.Diagnostics.Process p = new System.Diagnostics.Process();
            p.StartInfo.FileName = v_repertoireExe + "Log.txt";
            p.Start();
        }

        void e_bMoteursAuRepos_Click(object sender, EventArgs e)
        {
            if (v_usinageEnCours)
            {
                if (MessageBox.Show("Voulez vous arreter les moteurs ?", "USINAGE EN COURS", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                { return; }
            }
            Controlleur.m_Enable(v_paramPortParallele.v_enableEtatBas);
        }

        void e_bPasAPas_Click(object sender, EventArgs e)
        {
            if (bPasAPas.Text == "&Pas à Pas")
            {
                c_usiner.vs_PasAPas = true;
                bPasAPas.Text = "En Continu";
            }
            else
            {
                c_usiner.vs_PasAPas = false;
                bPasAPas.Text = "&Pas à Pas";
            }
        }

        void e_bRafraichir_Click(object sender, EventArgs e)
        {
            if ((!v_usinageEnCours) && (v_nomFichier != null))
            {
                using (c_traduireIso v_tI = new c_traduireIso(v_nomFichier, this, v_paramGeneraux.v_ignorerX0Y0,
                                                v_paramGeneraux.v_nbrChiffresApresVirgule, v_paramGeneraux.v_precisonEllipse,
                                                v_listParamAxe['X'].µmParTour / (v_listParamAxe['X'].nbrPasParTour * 1000), v_listParamAxe['Y'].µmParTour / (v_listParamAxe['Y'].nbrPasParTour * 1000)))
                {
                    m_remplirlBoxCodeTraduit(v_tI.m_convertirFichier());
                }
            }
        }

        void e_bRAZSaisieDirecte_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("RAZ Liste ?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            { lBoxSaisieDirecte.Items.Clear(); }
        }

        void e_bRemplirListe_Click(object sender, EventArgs e)
        {
            lBoxSaisieDirecte.Items.Add(tBSaisieDirecte.Text.ToUpper());
        }

        void e_bSTop_Click(object sender, EventArgs e)
        {
            if (!v_usinageEnCours) { return; }
            if (!v_stop)
            {
                v_stop = true;
                c_usiner.vs_PauseUsinage = true;
                Application.DoEvents();
                bDemarrer.Text = "Reprendre";
                bDemarrer.Enabled = true;
                this.KeyPress -= new KeyPressEventHandler(e_formMain_KeyPress);
                lInfoAction.Text = "Arrété";
                bChargerFichier.Focus();
            }
            else
            {
                if (MessageBox.Show("Voulez vous arreter DEFINITIVEMENT ?", "USINAGE", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                { return; }
                c_usiner.vs_ArreterUsinage = true;
                c_usiner.vs_PauseUsinage = false;
                Application.DoEvents();
                //Thread.Sleep(1000);
                //Application.DoEvents();
                System.Diagnostics.Stopwatch sW = new System.Diagnostics.Stopwatch();
                sW.Start();
                while (sW.ElapsedMilliseconds < 1000) { Application.DoEvents(); }
                sW.Reset();
                while (c_usiner.vs_isRunning)// (threadUsiner.IsAlive)
                {
                    //MessageBox.Show("Cliquez sur le message clignotant");
                    threadUsiner.Abort();
                    Application.DoEvents();
                }
                threadUsiner.Abort();
                //e_threadUsinerTermine();
                v_stop = false;
                c_usiner.vs_ArreterUsinage = false;
                c_usiner.vs_isRunning = false;
                lArretSurZ.Text = "";
                v_lArretSurZ = "";
                bDemarrer.Text = "&Demarrer";
                lInfoAction.Text = "Fini";
            }
        }

        void e_bTestVitesse_Click(object sender, EventArgs e)
        {
            try
            {
                if ((threadUsiner != null) && (threadUsiner.IsAlive))
                {
                    MessageBox.Show("Test impossible", "Usinage en cours");
                    return;
                }
                lBoxFichier.Items.Clear();
                lBoxFichier.Items.Add("G00 Z-1.;");
                lBoxFichier.Items.Add("G00 X40.;");
                v_nomFichier = "";
                using (c_traduireIso v_tI = new c_traduireIso("", this, v_paramGeneraux.v_ignorerX0Y0,
                                                v_paramGeneraux.v_nbrChiffresApresVirgule, v_paramGeneraux.v_precisonEllipse,
                                                v_listParamAxe['X'].µmParTour / (v_listParamAxe['X'].nbrPasParTour * 1000), v_listParamAxe['Y'].µmParTour / (v_listParamAxe['Y'].nbrPasParTour * 1000)))
                {
                    m_remplirlBoxCodeTraduit(v_tI.m_convertirFichier());
                }
                tabControlParams.SelectedIndex = 0;
                c_usiner.vs_enregistrerLOG = true;
                bool v_arretZ = v_paramGeneraux.v_arreterSurZ;
                cBoxArreterSurZ.Checked = false;
                tBarVitesse.Value = tBarVitesse.Maximum / 2;
                e_tBarVitesse_Scroll(null, null);
                Application.DoEvents();
                double v_periodeTheorique = ((double)(v_listParamAxe['X'].µmParTour / v_listParamAxe['X'].nbrPasParTour) / (double)v_paramGeneraux.v_vitesseAvance) / 100;
                Application.DoEvents();
                m_majParam();
                e_bDemarrer_Click(null, null);
                while (threadUsiner.IsAlive) { Application.DoEvents(); }
                cBoxArreterSurZ.Checked = v_arretZ;
                c_usiner.vs_enregistrerLOG = false;
                int v_maxi = (int)(v_periodeTheorique * 4);
                double v_moyenne = (2 * c_usiner.vs_elapsedTime) / c_usiner.vs_listElapsedTime.Count;//car remplit 2 fois par pas
                var v_compens = v_moyenne - v_periodeTheorique;
                if (MessageBox.Show(string.Format("Voulez vous compenser à raison de : {0:0.0000} ms ?", v_compens + v_paramGeneraux.v_compensationVitesseUsinage), "Etalonnage", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    v_paramGeneraux.v_compensationVitesseUsinage += v_compens;
                    c_usiner.vs_compensationVitesseUsinage = v_paramGeneraux.v_compensationVitesseUsinage;
                }
            }
            catch { }
        }

        void e_buttonVisu_Click(object sender, EventArgs e)
        {
            // bouton deplacement image
            m_traiterBoutonVisu(sender, e);
        }

        void e_cBLOG_CheckedChanged(object sender, EventArgs e)
        {
            c_usiner.vs_enregistrerLOG = cBLOG.Checked;
        }

        void e_cBoxArreterSurZ_CheckedChanged(object sender, EventArgs e)
        {
            v_paramGeneraux.v_arreterSurZ = cBoxArreterSurZ.Checked;
            c_usiner.vs_ArretSurZ = v_paramGeneraux.v_arreterSurZ;
            //            m_majParam();
        }

        void e_cBoxIgnorerX0Y0_CheckedChanged(object sender, EventArgs e)
        {
            v_paramGeneraux.v_ignorerX0Y0 = cBoxIgnorerX0Y0.Checked;
        }

        void e_cBPrioriteThread_SelectedIndexChanged(object sender, EventArgs e)
        {
            v_threadUsinerPriority = (ThreadPriority)comboBoxPrioriteThread.SelectedIndex;
        }

        void e_cBSimuler_CheckedChanged(object sender, EventArgs e)
        {
            if ((v_usinageEnCours) && (cBSimuler.Checked == true))
            {
                if (MessageBox.Show("Voulez vous simuler ?", "Usinage en cours", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                { return; }
            }
            c_usiner.vs_SimulerUsinage = cBSimuler.Checked;
            if (c_usiner.vs_SimulerUsinage)
            { tBoxCoordX.ForeColor = Color.Red; tBoxCoordY.ForeColor = Color.Red; tBoxCoordZ.ForeColor = Color.Red; }
            else
            { tBoxCoordX.ForeColor = Color.LimeGreen; ; tBoxCoordY.ForeColor = Color.LimeGreen; tBoxCoordZ.ForeColor = Color.LimeGreen; }
            m_majParam();
        }

        void e_cBVitesseMaxiZ_CheckedChanged(object sender, EventArgs e)
        {
            v_paramGeneraux.v_vitesseMaxiSiZ = cBVitesseMaxiZ.Checked;
            c_usiner.vs_VitMaxiSurZ = v_paramGeneraux.v_vitesseMaxiSiZ;
        }

        void e_changeParamAxe(object sender, EventArgs e)
        {
            try
            {
                if (sender is TextBox)
                {
                    TextBox v_tB = new TextBox();
                    v_tB = (TextBox)sender;
                    char v_Axe = v_tB.Name.LastChar();
                    if (v_tB.Name.StartsWith("tBoxPas"))
                    { v_listParamAxe[v_Axe].nbrPasParTour = int.Parse(v_tB.Text); }
                    if (v_tB.Name.StartsWith("tBoxµm"))
                    { v_listParamAxe[v_Axe].µmParTour = int.Parse(v_tB.Text); }
                    if (v_tB.Name.StartsWith("tBoxCompens"))
                    { v_listParamAxe[v_Axe].compensation = int.Parse(v_tB.Text); }
                    if (v_Axe == 'X') { m_actualisertBarVitesse(); Application.DoEvents(); }
                }
                if (sender is CheckBox)
                {
                    CheckBox v_cB = new CheckBox();
                    v_cB = (CheckBox)sender;
                    v_listParamAxe[v_cB.Name.LastChar()].inverserSens = v_cB.Checked;
                }
            }
            catch { }
            m_majParam();
        }

        void e_formMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (v_usinageEnCours)
            { MessageBox.Show("Usinage en cours"); e.Cancel = true; return; }
            e.Cancel = (MessageBox.Show("Voulez vous quitter ?", "CNC", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No);
            if (!e.Cancel)
            {
                m_sauverIni();
                if (threadDessiner != null)
                {
                    c_dessinerVisu.vs_ArreterThread = true;
                    while (threadDessiner.IsAlive) { Application.DoEvents(); }
                }
                Controlleur.m_ActiveRelai(false);
            }
        }

        void e_formMain_MouseWheel(object sender, MouseEventArgs e)
        {
            if (v_mouseOverPBox)
            {
                Button B = new Button();
                if (e.Delta > 0) { B.Name = "bVisuLoupePlus"; }
                else { B.Name = "bVisuLoupeMoins"; }
                m_traiterBoutonVisu(B, null);
            }
        }

        void e_pBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) { return; }
            v_cursor = this.Cursor;
            pBox.Cursor = Cursors.SizeAll;
            v_posSouris = new Point(e.X, e.Y);
            v_decalageVisuPBox = v_decalageVisu;
        }

        void e_pBox_MouseEnter(object sender, EventArgs e)
        {
            v_mouseOverPBox = true;
            pBox.Focus();
        }

        void e_pBox_MouseLeave(object sender, EventArgs e)
        {
            v_mouseOverPBox = false;
        }

        void e_pBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (!(pBox.Cursor == Cursors.SizeAll)) { return; }
            v_decalageVisu = v_decalageVisuPBox.Plus(e.X - v_posSouris.X, -(e.Y - v_posSouris.Y));
            m_dessinerVisu();
        }

        void e_pBox_MouseUp(object sender, MouseEventArgs e)
        {
            pBox.Cursor = v_cursor;
        }

        void e_rBChoixDllChanged(object sender, EventArgs e)
        {
            RadioButton v_rB;
            if ((v_rB = sender as RadioButton) != null)
            {
                switch (v_rB.Name.ToLower())
                {
                    case "rbdelphi": v_paramGeneraux.v_dll = enum_choixDll.Delphi; break;
                    case "rbcplusplus": v_paramGeneraux.v_dll = enum_choixDll.CPlusPlus; break;
                    case "rbiowkitdll": v_paramGeneraux.v_dll = enum_choixDll.IOWKitInterne; break;
                    case "rbpicusb": v_paramGeneraux.v_dll = enum_choixDll.PicUsb; break;
                    case "rbparallele": v_paramGeneraux.v_dll = enum_choixDll.Port_Parallele; break;
                }
            }
        }

        void e_rButton100_CheckedChanged(object sender, EventArgs e)
        {
            v_paramGeneraux.v_nbrChiffresApresVirgule = 2;
        }

        void e_rButton1000_CheckedChanged(object sender, EventArgs e)
        {
            v_paramGeneraux.v_nbrChiffresApresVirgule = 3;
        }

        void e_tabControlParams_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControlParams.SelectedIndex == 2)
            {
                bExecuterListe.Enabled = !v_usinageEnCours;
                bRemplirListe.Enabled = !v_usinageEnCours;
            }
        }

        void e_tBarEllipse_Scroll(object sender, EventArgs e)
        {
            v_paramGeneraux.v_precisonEllipse = tBarEllipse.Value;
            lPrecisionEllipse.Text = v_paramGeneraux.v_precisonEllipse.ToString() + " °";
        }

        void e_tBarVitesse_Scroll(object sender, EventArgs e)
        {
            //pour eviter vitesse 3.5 mm/s
            if (tBarVitesse.Value == 7)
            {
                if (v_paramGeneraux.v_vitesseAvance == 40) { tBarVitesse.Value = 6; }
                else { tBarVitesse.Value = 8; }
            }
            v_paramGeneraux.v_vitesseAvance = tBarVitesse.Value * 5;
            c_usiner.vs_vitesseAvance = v_paramGeneraux.v_vitesseAvance;// tBarVitesse.Value;
            if (sender != null)
            { v_vitesseUsinage = tBarVitesse.Value * 5; }
            lVitesseAvance.Text = string.Format("{0:0.0} mm/s", (double)v_paramGeneraux.v_vitesseAvance / 10);
            try
            {
                lFreqTheorique.Text = string.Format("Freq théorique : {0:0}", ((v_paramGeneraux.v_vitesseAvance * v_listParamAxe['X'].nbrPasParTour * 100000) / v_listParamAxe['X'].µmParTour));
            }
            catch { }
            m_afficherTempsEstime();
            m_majParam();
        }

        void e_tBFreqMaxi_TextChanged(object sender, EventArgs e)
        {
            try
            {
                v_paramGeneraux.v_freqMaxi = int.Parse(tBFreqMaxi.Text);
                m_actualisertBarVitesse();
            }
            catch { }
        }

        void e_threadUsinerTermine()
        {
            MessageBox.Show("Usinage terminé");
            bChargerFichier.Enabled = true;
            bDemarrer.Enabled = true;
            bRafraichir.Enabled = true;
            v_usinageEnCours = false;
            timerAffichage.Enabled = false;
        }

        void e_timerAffichage_Tick(object sender, EventArgs e)
        {
            m_afficherCoordonnes();
        }

        void e_toolStripMenuItemInserer_Click(object sender, EventArgs e)
        {
            ListBox v_lB = new ListBox();
            int i = 0;
            foreach (string v_s in lBoxSaisieDirecte.Items)
            {
                if (i == lBoxSaisieDirecte.SelectedIndex) { v_lB.Items.Add(tBSaisieDirecte.Text.ToUpper()); }
                v_lB.Items.Add(v_s);
                i++;
            }
            lBoxSaisieDirecte.Items.Clear();
            lBoxSaisieDirecte.Items.AddRange(v_lB.Items);
        }

        void e_toolStripMenuItemSupprimer_Click(object sender, EventArgs e)
        {
            if (lBoxSaisieDirecte.SelectedIndex != -1) { lBoxSaisieDirecte.Items.Remove(lBoxSaisieDirecte.SelectedItem); }
        }

        //traite les events venant de l'usinage
        void e_usiner_EventUsinage(c_usiner.c_eventArgUsiner e)
        {
            switch (e.Event)
            {
                case c_usiner.c_eventArgUsiner.enumEventsUsinage.incrementerLBoxFichier:
                    m_appelerDelegateInterThread(delegate { lBoxFichier.SelectedIndex = c_usiner.vs_indexLigne; });
                    break;
                case c_usiner.c_eventArgUsiner.enumEventsUsinage.modifDiamOutil:
                    m_appelerDelegateInterThread(delegate
                    {
                        lDiametreOutil.Text = string.Format("DIamètre Outil : {0:0.00} mm", c_usiner.vs_diametreOutil / 5);
                    });
                    break;
                case c_usiner.c_eventArgUsiner.enumEventsUsinage.vitesseAvanceMaxi:
                    m_appelerDelegateInterThread(delegate
                    {
                        v_vitesseUsinage = tBarVitesse.Value * 5;
                        tBarVitesse.Value = tBarVitesse.Maximum * 5;
                        e_tBarVitesse_Scroll(null, null);
                    });
                    break;
                case c_usiner.c_eventArgUsiner.enumEventsUsinage.vitesseAvanceNormale:
                    m_appelerDelegateInterThread(delegate
                    {
                        tBarVitesse.Value = v_vitesseUsinage;
                        e_tBarVitesse_Scroll(null, null);
                    });
                    break;
                case c_usiner.c_eventArgUsiner.enumEventsUsinage.vitesseAvanceModifie:
                    m_appelerDelegateInterThread(delegate
                    {
                        tBarVitesse.Value = (tBarVitesse.Maximum * vs_infoUsinage.v_vitesseAvancePercent) / 20;
                        e_tBarVitesse_Scroll(null, null);
                    });
                    break;
                case c_usiner.c_eventArgUsiner.enumEventsUsinage.threadTermine:
                    m_appelerDelegateInterThread(delegate { e_threadUsinerTermine(); });
                    break;
                case c_usiner.c_eventArgUsiner.enumEventsUsinage.mettreAJourCoordNext:
                    m_appelerDelegateInterThread(delegate
                    {
                        tBoxNextX.Text = string.Format("{0:0.000}", (double)c_usiner.AxeX.Next / 1000000);
                        tBoxNextY.Text = string.Format("{0:0.000}", (double)c_usiner.AxeY.Next / 1000000);
                        tBoxNextZ.Text = string.Format("{0:0.000}", (double)c_usiner.AxeZ.Next / 1000000);
                    });
                    break;
                case c_usiner.c_eventArgUsiner.enumEventsUsinage.gBarBuffer:
                    m_appelerDelegateInterThread(delegate
                    {
                        gBarBufferUsinage.Value = c_ExecuteCode.vs_listCode.Count;
                    });
                    break;
                case c_usiner.c_eventArgUsiner.enumEventsUsinage.lArretSurZ:
                    m_appelerDelegateInterThread(delegate
                    {
                        lArretSurZ.Text = c_usiner.vs_lArretSurZ;
                        v_lArretSurZ = c_usiner.vs_lArretSurZ;
                        lArretSurZ.Enabled = true;
                        c_usiner.vs_PauseUsinage = true;
                    });
                    break;

                default:
                    break;
            }
        }

        void e_cmdLireRCPUsb_Click(object sender, EventArgs e)
        {
            if ((v_usinageEnCours) || (v_paramGeneraux.v_dll != enum_choixDll.PicUsb))
            { return; }
            /*    c_ControlleurBase v_Controlleur = null;
                m_initialiserControlleur(ref v_Controlleur);
                if (v_Controlleur.m_IsConnected())
                {
                    int[] v_infos = (v_Controlleur as c_ControlleurPicUsb).m_setPulses(0, 0, 0);
                    tBPulsesLow.Text = v_infos[1].ToString();
                    tBPulsesHigh.Text = v_infos[0].ToString();
                    tBPulsesIdle.Text = v_infos[2].ToString();
                    v_Controlleur.m_DisConnect();
                } */
        }

        void e_cmdEcrireRCPUsb_Click(object sender, EventArgs e)
        {
            if ((v_usinageEnCours) || (v_paramGeneraux.v_dll != enum_choixDll.PicUsb))
            { return; }
        /*    int v_pHigh = 0, v_pLow = 0, v_pIdle = 0;
            try
            {
                v_pHigh = int.Parse(tBPulsesLow.Text);
                v_pLow = int.Parse(tBPulsesHigh.Text);
                v_pIdle = int.Parse(tBPulsesIdle.Text);
            }
            catch { return; }
            if ((v_pHigh < 0) | (v_pHigh > 255) | (v_pLow < 0) | (v_pLow > 255) |
                ((v_pIdle < 0) | (v_pIdle > 255)))
            {
                MessageBox.Show("Verifiez les valeurs");
                return;
            }
            /*      c_ControlleurBase v_Controlleur = null;
                  m_initialiserControlleur(ref v_Controlleur);
                  if (v_Controlleur.m_IsConnected())
                  {
                      (v_Controlleur as c_ControlleurPicUsb).m_setPulses(v_pHigh, v_pLow, v_pIdle);
                      v_Controlleur.m_DisConnect();
                  }
                  MessageBox.Show("Transfert effectué");
                  e_cmdLireRCPUsb_Click(null, null);  */
        }

        void e_cBStep_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cBStep.SelectedIndex >= 0)
            {
                v_paramGeneraux.v_Step = cBStep.SelectedIndex;
                c_usiner.vs_Step = cBStep.SelectedIndex;
                e_tBarVitesse_Scroll(null, null);//pour actualiser freq theorique
            }
        }

        #region Events Tab Parallele
        private void e_tBAdressePortParallele_Leave(object sender, EventArgs e)
        {
            try
            {
                if (tBAdressePortParallele.Text.ToLower().StartsWith("0x"))
                { v_paramPortParallele.v_adresseportParallele = int.Parse(tBAdressePortParallele.Text.Substring(2), System.Globalization.NumberStyles.AllowHexSpecifier); }
                else
                { v_paramPortParallele.v_adresseportParallele = int.Parse(tBAdressePortParallele.Text); }
                c_ControlleurParallele.v_adressePort = v_paramPortParallele.v_adresseportParallele;
            }
            catch { MessageBox.Show("Probleme de Convertion !"); }
        }

        private void e_cmdTesterPortParallele_Click(object sender, EventArgs e)
        {
            c_ControlleurParallele v_controlleur = new c_ControlleurParallele();
            if (v_controlleur.m_IsConnected())
            { MessageBox.Show("Connection réussie"); }
            else { MessageBox.Show("Pas de connection"); }
        }

        private void e_cBEnableEtatBas_CheckedChanged(object sender, EventArgs e)
        {
            v_paramPortParallele.v_enableEtatBas = cBEnableEtatBas.Checked;
        }

        private void e_tBAcceleration_Leave(object sender, EventArgs e)
        {
            try
            {
                int v_temp = int.Parse(tBAcceleration.Text);
                if (v_temp < 0) { MessageBox.Show("Valeur d'acceleration incorrecte"); }
                else
                {
                    v_paramPortParallele.v_acceleration = v_temp;
                    c_usiner.vs_PasAcceleration = v_paramPortParallele.v_acceleration;
                }
            }
            finally { tBAcceleration.Text = v_paramPortParallele.v_acceleration.ToString(); }
        }

        private void e_tBPourCentVitAng_Leave(object sender, EventArgs e)
        {
            try
            {
                int v_temp = int.Parse(tBPourCentVitAng.Text);
                if ((v_temp < 1) | (v_temp > 200)) { MessageBox.Show("Valeur de pourcentage angulaire incorrecte"); }
                else { v_paramPortParallele.v_pourCentAng = v_temp; }
            }
            finally
            {
                tBPourCentVitAng.Text = v_paramPortParallele.v_pourCentAng.ToString();
                c_usiner.vs_PourCentAng = v_paramPortParallele.v_pourCentAng;
            }
        }
        #endregion

        private void e_lBoxFichier_SelectedIndexChanged(object sender, EventArgs e)
        {
            #region Dessine le prochain deplacement en bleu
            try
            {
                v_dessinNextEnCours = true;
                tBLigneEnCours.Text = lBoxFichier.Items[lBoxFichier.SelectedIndex].ToString();
                //pour dessiner en couleur la prochaine ligne à usiner
                int v_index = c_usiner.vs_indexLigneCodeTraduit;
                string v_ligne = lBoxCodeTraduit.Items[v_index].ToString();
                if (v_ligne.StartsWith("Debut"))
                {
                    v_index++;
                    Bitmap bmp = new Bitmap((Bitmap)pBox.Image);
                    Graphics v_grap = Graphics.FromImage(bmp);
                    Point v_nextPoint;
                    Point v_center = new Point(bmp.Width / 2, bmp.Height / 2);
                    while (!lBoxCodeTraduit.Items[v_index].ToString().StartsWith("Fin"))
                    {
                        v_ligne = lBoxCodeTraduit.Items[v_index].ToString();
                        if (v_ligne.StartsWith("dX"))
                        {
                            v_nextPoint = new Point(int.Parse(v_ligne.Substring(12, 8).Trim()) / 100, int.Parse(v_ligne.Substring(32, 8).Trim()) / 100);

                            v_grap.DrawLine(new Pen(Color.Blue), v_lastDebutLigne.Mult(v_echelleVisu).Div().Plus(v_center).Plus(v_decalageVisu).InvY(bmp.Height)
                                                , v_nextPoint.Mult(v_echelleVisu).Div().Plus(v_center).Plus(v_decalageVisu).InvY(bmp.Height));
                            v_lastDebutLigne = v_nextPoint;
                        }
                        v_index++;
                    }
                    pBox.Image = bmp;
                    Application.DoEvents();
                }
            }
            finally { v_dessinNextEnCours = false; }
            #endregion
        }

        #endregion

        private void e_tBNbrLignesDansBuffer_Leave(object sender, EventArgs e)
        {
            try
            {
                v_paramGeneraux.v_nbrLignesDansBuffer = int.Parse(tBNbrLignesDansBuffer.Text);
                gBarBufferUsinage.Maximum = v_paramGeneraux.v_nbrLignesDansBuffer;
                vs_infoUsinage.v_nbrLignesBuffer = v_paramGeneraux.v_nbrLignesDansBuffer;
            }
            catch { }
        }

        private void e_cmdFenetreTests_Click(object sender, EventArgs e)
        {
            formTest fT = new formTest();
            fT.ShowDialog();
        }

        private void e_lArretSurZ_Click(object sender, EventArgs e)
        {

            c_usiner.vs_PauseUsinage = lInfoAction.Text == "Arrété";
            lArretSurZ.Text = "";
            v_lArretSurZ = "";
            lArretSurZ.Enabled = false;
            Application.DoEvents();
        }

        private void e_tBVitesseMini_Leave(object sender, EventArgs e)
        {
            try
            {
                int v_temp = int.Parse(tBVitesseMini.Text);
                if ((v_temp < 1) | (v_temp > 200)) { MessageBox.Show("Valeur de vitesse mini incorrecte"); }
                else { v_paramPortParallele.v_vitesseMini = v_temp; }
            }
            finally
            {
                tBVitesseMini.Text = v_paramPortParallele.v_vitesseMini.ToString();
            }
        }

        private void e_cbNbrPasEnvoyes_CheckedChanged(object sender, EventArgs e)
        {
            v_paramPortParallele.v_verifierNbrPas = cbNbrPasEnvoyes.Checked;
        }

        private void e_tBDureeImpulsion_Leave(object sender, EventArgs e)
        {
            try
            { v_paramPortParallele.v_dureeImpulsion = int.Parse(tBDureeImpulsion.Text); }
            catch { }
        }

        private void e_cmdRetourOrigine_Click(object sender, EventArgs e)
        {
            //retour à l'origine
            double v_X = (double)c_usiner.AxeX.Actuel / 1000000, v_Y = (double)c_usiner.AxeY.Actuel / 1000000, v_Z = (double)c_usiner.AxeZ.Actuel / 1000;
            int v_x = (int)(v_X * 10), v_y = (int)(v_Y * 10);
            lBoxSaisieDirecte.Items.Clear();
            string v_line = string.Format("G01 X{0} | Y{1};", -(v_X), -(v_Y)).Replace(',', '.').Replace('|', ',');
            lBoxSaisieDirecte.Items.Add(v_line);
            e_bExecuterListe_Click(null, null);
        }
    }
}