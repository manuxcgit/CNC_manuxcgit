using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.IO.Ports;

namespace CNC
{
    abstract public class c_ControlleurBase
    {
        protected double v_Result;
        protected Stopwatch sW = new Stopwatch();

        //protected static int v_ParamSens;

        public bool IsBusy = false;

        public double m_Write(string AxeAUsiner)
        {
            return m_WriteInterne(AxeAUsiner);
        }

        public bool m_IsConnected()
        {
            return (m_IsConnectedInterne());
        }


        public void m_DisConnect()
        {
            m_DisconnectInterne();
        }

   /*     public void m_SetSens(int v_param)
        {
            m_SetSensInterne(v_param);
        } */

        protected double m_delay(double vl_delay)
        {
            double v_result = 0;
            sW.Start();
            while (sW.Elapsed.TotalMilliseconds < vl_delay)
            {
               // System.Windows.Forms.Application.DoEvents();
            }
            v_result = sW.Elapsed.TotalMilliseconds;
            sW.Reset();
            return v_result;
        }

        protected abstract double m_WriteInterne(string AxeAUsiner);

        protected abstract bool m_IsConnectedInterne();

        protected abstract void m_DisconnectInterne();

      /*  protected abstract void m_SetSensInterne(int v_param);  */
    }
  
    public class c_ControlleurPicUsb : c_ControlleurBase
    {
        #region declarations

        double[] v_listePeriodeVitesse = { 24.8, 11.8, 7.8, 5.9, 4.9, 3.9, 2.9 };
        double v_periode = 0;

        public string Firmware;
        // 2.0 : reglage full ou 1/x step

        #region CONST
        const byte MPUSB_FAIL = 0;
        const byte MPUSB_SUCCESS = 1;

        const byte MP_WRITE = 0;
        const byte MP_READ = 1;

        const byte SEND_DIGITAL_byte = 0x10;
        const byte SEND_DIGITAL_BIT = 0x11;
        const byte RECEIVE_DIGITAL_byte = 0x12;
        const byte RECEIVE_DIGITAL_BIT = 0x13;
        const byte SEND_ANALOGICA = 0x14;
        const byte RECEIVE_ANALOGICA = 0x15;


        // MAX_NUM_MPUSB_DEV is an abstract limitation.
        // It is very unlikely that a computer system will have more
        // then 127 USB devices attached to it. (single or multiple USB hosts)
        const int MAX_NUM_MPUSB_DEV = 127;
        const int INVALID_HANDLE_VALUE = -1;
        const int ERROR_INVALID_HANDLE = 6;

        //VARIABLES GLOBALES A NIVEL DE ARCHIVO
        const string vid_pid = @"vid_04d8&pid_000c";    // Default Demo Application Firmware
        const string out_pipe = @"\MCHP_EP1";
        const string in_pipe = @"\MCHP_EP1";
        #endregion
        byte[] send_buf = new byte[64];
        byte[] receive_buf = new byte[64];


        int myOutPipe = INVALID_HANDLE_VALUE;
        int myInPipe = INVALID_HANDLE_VALUE;

        int RecvLength = 0;
        protected int v_nbrpasX, v_nbrpasY;


        [DllImport("mpusbapi.dll")]
        static extern UInt32 _MPUSBGetDLLVersion();

        [DllImport("mpusbapi.dll")]
        static extern int _MPUSBGetDeviceCount(string pVID_PID);

        [DllImport("mpusbapi.dll")]
        static extern int _MPUSBOpen(int iInstance, string pVID_PID, string pEP, int dwDir, int dwReserved);
        //typedef HANDLE (WINAPI *MPUSBOPEN)(
        //                 DWORD instance,     // Input
        //                 PCHAR pVID_PID,				// Input
        //                 PCHAR pEP,						// Input
        //                 DWORD dwDir,					// Input
        //                 DWORD dwReserved);				// Input <Future Use>

        [DllImport("mpusbapi.dll")]
        static extern int _MPUSBRead(int iHandle, byte[] pData, int dwLen, ref int pLength, int dwMilliseconds);
        //typedef DWORD (WINAPI *MPUSBREAD)(
        //                HANDLE handle,       // Input
        //                PVOID pData,					// Output
        //                DWORD dwLen,					// Input
        //                PDWORD pLength,					// Output
        //                DWORD dwMilliseconds);			// Input

        [DllImport("mpusbapi.dll")]
        static extern int _MPUSBWrite(int iHandle, byte[] pData, int dwLen, ref int pLength, int dwMilliseconds);
        //typedef DWORD (WINAPI *MPUSBWRITE)(
        //                 HANDLE handle,      // Input
        //                 PVOID pData,					// Input
        //                 DWORD dwLen,					// Input
        //                 PDWORD pLength,				// Output
        //                 DWORD dwMilliseconds);			// Input

        [DllImport("mpusbapi.dll")]
        static extern int _MPUSBReadInt(int iHandle, byte[] pData, int dwLen, ref int pLength, int dwMilliseconds);
        //typedef DWORD (WINAPI *MPUSBREADINT)(
        //                   HANDLE handle,    // Input
        //                   PVOID pData,					// Output
        //                   DWORD dwLen,					// Input
        //                   PDWORD pLength,				// Output
        //                   DWORD dwMilliseconds);		// Input

        [DllImport("mpusbapi.dll")]
        static extern Boolean _MPUSBClose(int iHandle);
        // typedef BOOL (WINAPI *MPUSBCLOSE)(HANDLE handle);
        #endregion

        public c_ControlleurPicUsb()
        {
            m_OpenUsb();
        }
        //*************************************************************************
        bool m_OpenUsb()
        {
            // Always open one device only 
            myOutPipe = myInPipe = INVALID_HANDLE_VALUE;
            int count = _MPUSBGetDeviceCount(vid_pid);
            if (count > 0)
            {
                myOutPipe = _MPUSBOpen(0, vid_pid, out_pipe, (int)MP_WRITE, 0);
                myInPipe = _MPUSBOpen(0, vid_pid, in_pipe, (int)MP_READ, 0);
                if (myOutPipe == INVALID_HANDLE_VALUE || myInPipe == INVALID_HANDLE_VALUE)
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
            return true;
        }
        //*************************************************************************
        void m_CloseUsb()
        {
            _MPUSBClose(myOutPipe);
            _MPUSBClose(myInPipe);
            myOutPipe = myInPipe = INVALID_HANDLE_VALUE;
        }
        //*************************************************************************
        int m_GetDeviceCount()
        {
            return (_MPUSBGetDeviceCount(vid_pid));
            // USB\VID_04D8&PID_000A\5&2CC3F34&0&2   vid_04d8&pid_000c

        }
        //*************************************************************************
        int m_SendByte(byte value)
        {
            send_buf[0] = 0x55;// (byte)char.Parse("U");// 0x30;	//Comando
            send_buf[1] = value;	//Dato
            RecvLength = 2;
            SendReceivePacket(send_buf, 2, receive_buf, ref RecvLength, 100, 100, false);
            return receive_buf[1];
        }
        //*************************************************************************
        int SendReceivePacket(byte[] SendData, int SendLength, byte[] ReceiveData,
                    ref int ReceiveLength, int SendDelay, int ReceiveDelay, bool attendRetour)
        {
            int SentDataLength = 0;
            int ExpectedReceiveLength = ReceiveLength;

            if (myOutPipe != INVALID_HANDLE_VALUE && myInPipe != INVALID_HANDLE_VALUE)
            {
               // System.Windows.Forms.Application.DoEvents();
                var v_test = _MPUSBWrite(myOutPipe, SendData, SendLength, ref SentDataLength, SendDelay);
               // System.Windows.Forms.Application.DoEvents();
                if ((v_test > 0) && (!attendRetour))
                { return (1); }
                if ((v_test > 0) && (attendRetour))
                {
                    for (int i = 0; i < 5; i++)
                    {
                        ReceiveData[i] = 0;
                    }
                    var v_result = _MPUSBRead(myInPipe, ReceiveData, ExpectedReceiveLength, ref ReceiveLength, ReceiveDelay);
                    if (v_result > 0)
                    {
                        if (ReceiveLength == ExpectedReceiveLength)
                        {
                            return (1);   //Correct receive length
                        }
                        else if (ReceiveLength < ExpectedReceiveLength)
                        {
                            return (2);   //Partially failed, incorrect receive length
                        }
                    }
                }

            }
            return (0);
        }
        //*************************************************************************
        protected override double m_WriteInterne(string AxeAUsiner)
        {
            byte v_usb = 0;
            if (AxeAUsiner.Contains('X')) { v_usb = 1; }
            if (AxeAUsiner.Contains('Y')) { v_usb += 2; }
            sW.Reset();
            sW.Start();
            int result = m_SendByte(v_usb);
            double temp = sW.Elapsed.TotalMilliseconds;
            while (sW.Elapsed.TotalMilliseconds < v_periode)
            { }
            sW.Stop();
            System.Windows.Forms.Application.DoEvents();
            return sW.Elapsed.TotalMilliseconds;
        }
        //*************************************************************************
        protected override bool m_IsConnectedInterne()
        {
            send_buf[0] = 0x54;// (byte)char.Parse("T");// 0x30;	//Comando
            send_buf[1] = 4;
            RecvLength = 7;
            if (SendReceivePacket(send_buf, 2, receive_buf, ref RecvLength, 100, 100, true) == 1)
            {
                string v_recu = new string(new char[] { (char)receive_buf[1], (char)receive_buf[2], (char)receive_buf[3],
                                                        (char)receive_buf[4], (char)receive_buf[5], (char)receive_buf[6]});
                Firmware = v_recu.Substring(3);
                return (v_recu.StartsWith("CNC"));
            }
            else
            {
                return false;
            }
        }
        //*************************************************************************
        protected override void m_DisconnectInterne()
        {
            m_CloseUsb();
        }
        //*************************************************************************
        public int[] m_testePositionPortB()
        {
            send_buf[0] = 0x50;// (byte)char.Parse("P") nbr pas X, pas Y
            RecvLength = 5;
            SendReceivePacket(send_buf, 2, receive_buf, ref RecvLength, 1000, 1000, true);
            int[] result =  {receive_buf[1] + (receive_buf[2] * 256),
                             receive_buf[3] + ( receive_buf[4] * 256)};
            return result;
        }
        //*************************************************************************
        public void m_initialise_X_Y()
        {
            send_buf[0] = 0x51;//'Q' 
            v_nbrpasX = 0;
            v_nbrpasY = 0;
            SendReceivePacket(send_buf, 2, receive_buf, ref RecvLength, 100, 100, true);
        }
        //*************************************************************************
        protected void m_setParams(int vl_dureeImpulsion, int vl_vitesse)
        {
            //pour regler tempo cycles à vide, ..., ...
            int v_vitesse = 0;
            if (vl_vitesse > 5) { v_vitesse = 1; }
            if (vl_vitesse > 10) { v_vitesse = 2; }
            if (vl_vitesse > 15) { v_vitesse = 3; }
            if (vl_vitesse > 20) { v_vitesse = 4; }
            if (vl_vitesse > 25) { v_vitesse = 5; }
            if (vl_vitesse >30) { v_vitesse = 6; }
            send_buf[0] = 0x52;//'R' 
            send_buf[1] = (byte)vl_dureeImpulsion;
            send_buf[2] = (byte)v_vitesse;
            SendReceivePacket(send_buf, 3, receive_buf, ref RecvLength, 1000, 1000, true);
            v_periode = v_listePeriodeVitesse[v_vitesse];
        }
        //*************************************************************************
        public bool m_SetSens(bool sensX, bool sensY)
        {
            //sert pour regler sens rotation et nbr pas
            //sensX, sensY, V3, V2, V1, V0, Step1, Step0           
            send_buf[0] = 0x53;//'S' 
            if (sensX) { send_buf[1] = 1; } else { send_buf[1] = 0; }
            if (sensY) { send_buf[2] = 1; } else { send_buf[2] = 0; }
            RecvLength = 3;
            SendReceivePacket(send_buf, 3, receive_buf, ref RecvLength, 1000, 1000, true);
            return ((receive_buf[1] == send_buf[1]) & (receive_buf[2] == send_buf[2]));
        }

        public bool m_ActiveRelai(bool Active)
        {
            send_buf[0] = 0x41;//'A' 
            if (Active) { send_buf[1] = 1; } else { send_buf[1] = 0; }
            RecvLength = 1;
            return (SendReceivePacket(send_buf, 2, receive_buf, ref RecvLength, 1000, 1000, true) == 1);
        }

        public bool m_Enable(bool Active)
        {
            send_buf[0] = 0x45;//'E' 
            if (Active) { send_buf[1] = 1; } else { send_buf[1] = 0; }
            RecvLength = 1;
            return (SendReceivePacket(send_buf, 2, receive_buf, ref RecvLength, 1000, 1000, true) == 1);
        }


    }

    public class c_ControlleurParallele : c_ControlleurPicUsb
    {
        #region declarations

        [DllImport("inpout32.dll", EntryPoint = "Out32")]
        public static extern void m_DLL_Output(int adress, int value);

        [DllImport("inpout32.dll", EntryPoint = "Inp32")]
        public static extern void m_DLL_Input(int adress);


        public static int v_adressePort;
        int v_vitesseSelectionnee;
        Stopwatch sW1 = new Stopwatch();

        #endregion

        protected override void m_DisconnectInterne()
        {
            base.m_DisConnect();
        }

        protected override bool m_IsConnectedInterne()
        {
            return (base.m_IsConnectedInterne());
        }

        public void m_SetVitesseEtPas(int vitesse, int dureeImpulsion)
        {
            //vitesse 0:0.5, 1:1, 2:1.5, 3:2, 4:2.5, 5:3, 6:4mm/s
            IsBusy = true;
            v_vitesseSelectionnee=vitesse;
            m_setParams(dureeImpulsion, vitesse);
            IsBusy = false;
        }

        public void m_corrigePas(char Axe, int nbrPas)
        {
            IsBusy = true;
            for (int i = 1; i <= nbrPas; i++)
            {
                base.m_WriteInterne(Axe.ToString());
                c_usiner.vs_nbrErreurs++;
            }
            IsBusy = false;
        }

        protected override double m_WriteInterne(string AxesAUsiner)
        {
            IsBusy = true;
            v_Result =  base.m_WriteInterne(AxesAUsiner);
            IsBusy = false;
            return (v_Result);
        }
    }
}
