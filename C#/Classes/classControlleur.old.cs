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
        protected int v_Result;
        protected Stopwatch sW = new Stopwatch();

        protected static int v_ParamSens;

        public bool IsBusy = false;

        public double m_Write(int Value)
        {
            return m_WriteInterne(Value);
        }

        public bool m_IsConnected()
        {
            return (m_IsConnectedInterne());
        }

        public void m_Desactiver()
        {
            m_WriteInterne(0);
        }

        public void m_DisConnect()
        {
            m_DisconnectInterne();
        }

        public void m_SetSens(int v_param)
        {
            m_SetSensInterne(v_param);
        }

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

        protected abstract double m_WriteInterne(int Value);

        protected abstract bool m_IsConnectedInterne();

        protected abstract void m_DisconnectInterne();

        protected abstract void m_SetSensInterne(int v_param);
    }
    /*
        public class c_ControlleurDelphi : c_ControlleurBase
        {
            [DllImport("IowKitManu")]
            static extern bool IowKitOpen();
            [DllImport("IowKitManu")]
            static extern ulong IowKitWriteTo(int value);
            [DllImport("IowKitManu")]
            static extern bool IowKitClose();

            bool v_isConnected;

            public c_ControlleurDelphi()
            {
                v_isConnected = IowKitOpen();
                v_sW = new Stopwatch();
            }

            ~c_ControlleurDelphi()
            {
                IowKitClose();
            }

            protected override double m_WriteInterne(int Value, double TempsMort)
            {
                v_Result = 0;
                v_sW.Start();
                double start = v_sW.Elapsed.TotalMilliseconds;
                v_Result = (int)IowKitWriteTo(Value);
                while (v_sW.Elapsed.TotalMilliseconds - start < 0.90) { System.Windows.Forms.Application.DoEvents(); }
                start = v_sW.Elapsed.TotalMilliseconds;
                Value = Value & 250;
                v_Result += (int)IowKitWriteTo(Value);
                while (v_sW.Elapsed.TotalMilliseconds - start < TempsMort) { System.Windows.Forms.Application.DoEvents(); }
                v_sW.Stop();
                return v_Result;
            }

            protected override bool m_IsConnectedInterne()
            {
                return v_isConnected;
            }

            protected override void m_DisconnectInterne()
            {
                IowKitClose();
            }
        }

        public class c_ControlleurDirect : c_ControlleurBase
        {
            [DllImport("iowkit.dll")]
            public static extern int IowKitOpenDevice();
            [DllImport("iowkit.dll")]
            public static extern unsafe int IowKitWrite(int IOWKIT_HANDLE, int numPipe, int adress, int length, bool writeAll);
            [DllImport("iowkit.dll")]
            public static extern int IowKitGetProductId(int IOWKIT_HANDLE);
            [DllImport("iowkit.dll")]
            public static extern void IowKitCloseDevice(int IOWKIT_HANDLE);

            struct s_valueCommande
            {
                public byte ReportID;
                public byte Value;
                public byte Null1;

                public void m_Initialiser()
                { ReportID = 0; Value = 0; Null1 = 0; }
            }
            s_valueCommande v_valueCommande = new s_valueCommande();

            int v_handle = 0;

            public c_ControlleurDirect()
            {
                v_handle = IowKitOpenDevice();
                v_sW = new Stopwatch();
            }

            ~c_ControlleurDirect()
            {
                IowKitCloseDevice(v_handle);
            }

            protected override double m_WriteInterne(int Value, double TempsMort)
            {
                unsafe
                {
                    fixed (byte* adresse = &v_valueCommande.ReportID)
                    {
                        v_Result = 0;
                        v_sW.Start();
                        double start = v_sW.Elapsed.TotalMilliseconds;
                        double test1, test2;
                        v_valueCommande.Value = (byte)Value;
                        v_Result = IowKitWrite(v_handle, 0, (int)adresse, 3, false);
                        while (v_sW.Elapsed.TotalMilliseconds - start < 0.90) { System.Windows.Forms.Application.DoEvents(); }
                        test1 = v_sW.Elapsed.TotalMilliseconds - start;
                        start = v_sW.Elapsed.TotalMilliseconds;
                        v_valueCommande.Value = (byte)(Value & 250);
                        v_Result += IowKitWrite(v_handle, 0, (int)adresse, 3, false);
                        while (v_sW.Elapsed.TotalMilliseconds - start < TempsMort) { System.Windows.Forms.Application.DoEvents(); }
                        test2 = v_sW.Elapsed.TotalMilliseconds - start;
                        v_sW.Stop();
                        return v_Result;
                    }
                }
            }

            protected override bool m_IsConnectedInterne()
            {
                return (v_handle != 0);
            }

            protected override void m_DisconnectInterne()
            {
                IowKitCloseDevice(v_handle);
            }
        }

        public class c_ControlleurCPlusPlus : c_ControlleurBase
        {

            [DllImport("IowCplusPlus.dll")]
            static extern bool IowOpenDeviceCplusplus();
            [DllImport("IowCplusPlus.dll")]
            static extern int IowWriteCplusplus(int value);

            bool v_isConnected;

            public c_ControlleurCPlusPlus()
            {
                v_isConnected = IowOpenDeviceCplusplus();
                v_sW = new Stopwatch();
            }

            protected override double m_WriteInterne(int Value, double TempsMort)
            {
                v_Result = 0;
                v_sW.Start();
                double start = v_sW.Elapsed.TotalMilliseconds;
                v_Result = (int)IowWriteCplusplus(Value);
                while (v_sW.Elapsed.TotalMilliseconds - start < 0.90) { System.Windows.Forms.Application.DoEvents(); }
                start = v_sW.Elapsed.TotalMilliseconds;
                Value = Value & 250;
                v_Result += (int)IowWriteCplusplus(Value);
                while (v_sW.Elapsed.TotalMilliseconds - start < TempsMort) { System.Windows.Forms.Application.DoEvents(); }
                v_sW.Stop();
                return v_Result;
            }

            protected override bool m_IsConnectedInterne()
            {
                return v_isConnected;
            }

            protected override void m_DisconnectInterne()
            {
                throw new NotImplementedException();
            }
        }
    */
    public class c_ControlleurPicUsb : c_ControlleurBase
    {
        #region declarations
        Stopwatch sW = new Stopwatch();

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
        void m_SendByte(byte value)
        {
            send_buf[0] = 0x55;// (byte)char.Parse("U");// 0x30;	//Comando
            send_buf[1] = value;	//Dato
            SendReceivePacket(send_buf, 2, receive_buf, ref RecvLength, 100, 100, false);
        }
        //*************************************************************************
        int SendReceivePacket(byte[] SendData, int SendLength, byte[] ReceiveData,
                    ref int ReceiveLength, int SendDelay, int ReceiveDelay, bool attendRetour)
        {
            int SentDataLength = 0;
            int ExpectedReceiveLength = ReceiveLength;

            if (myOutPipe != INVALID_HANDLE_VALUE && myInPipe != INVALID_HANDLE_VALUE)
            {
                var v_test = _MPUSBWrite(myOutPipe, SendData, SendLength, ref SentDataLength, SendDelay);
                System.Windows.Forms.Application.DoEvents();
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
        protected override double m_WriteInterne(int Value)
        {
            sW.Reset();
            sW.Start();
            m_SendByte((byte)Value);
            sW.Stop();
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
            send_buf[0] = 0x50;// (byte)char.Parse("P") portB, nbrImpulsions, indexX, indexY, Code envoyé
            send_buf[1] = 6;
            RecvLength = 7;
            SendReceivePacket(send_buf, 2, receive_buf, ref RecvLength, 1000, 1000, true);
            int[] result =  {receive_buf[1], receive_buf[2] + (receive_buf[3] * 256),
                                                receive_buf[4], receive_buf[5], receive_buf[6]};
            return result;
        }
        //*************************************************************************
        public void m_initialise_X_Y()
        {
            send_buf[0] = 0x51;//'Q' 
            SendReceivePacket(send_buf, 2, receive_buf, ref RecvLength, 100, 100, false);
        }
        //*************************************************************************
        public int[] m_setPulses(int vl_pulseHIGH, int vl_pulseLOW, int vl_idle)
        {
            //pour regler tempo cycles à vide, ..., ...
            send_buf[0] = 0x52;//'R' 
            send_buf[1] = (byte)vl_pulseHIGH;
            send_buf[2] = (byte)vl_pulseLOW;
            send_buf[3] = (byte)vl_idle;
            RecvLength = 4;
            SendReceivePacket(send_buf, 4, receive_buf, ref RecvLength, 1000, 1000, true);
            int[] result = { receive_buf[1], receive_buf[2], receive_buf[3] };
            return result;
        }
        //*************************************************************************
        protected override void m_SetSensInterne(int vl_param)
        {
            //sert pour regler sens rotation et nbr pas
            //sensX, sensY, V3, V2, V1, V0, Step1, Step0
            send_buf[0] = 0x53;//'S' 
            send_buf[1] = (byte)vl_param;
            RecvLength = 1;
            SendReceivePacket(send_buf, 2, receive_buf, ref RecvLength, 1000, 1000, true);
        }
    }

    public class c_ControlleurParallele : c_ControlleurBase
    {
        #region declarations

        [DllImport("inpout32.dll", EntryPoint = "Out32")]
        public static extern void m_DLL_Output(int adress, int value);

        [DllImport("inpout32.dll", EntryPoint = "Inp32")]
        public static extern void m_DLL_Input(int adress);


        public static int v_adressePort;
        public static int v_nbrStep;
        static int v_usbOUT = 16; //00010000 pour enable relai
        static int v_vitesse;
        static double[] v_listeRalentir = { 2.8125, 1.25, .7292, .4688, .3125, .2083, .1339, .0781, .0347, 0 };
        static double v_pulsesLOW;
        //Stopwatch sW1 = new Stopwatch();

        #endregion

        public static void m_initialiseParallele()
        {
            m_DLL_Output(v_adressePort, v_usbOUT);
        }

        public static void m_inactiveRelai()
        {
            m_DLL_Output(v_adressePort, 0);
        }

        protected override void m_DisconnectInterne()
        {
            //throw new NotImplementedException();
        }

        protected override bool m_IsConnectedInterne()
        {
            //throw new NotImplementedException();
            return true;
        }

        protected override void m_SetSensInterne(int vl_param)
        {
            //sens X, sens Y, vitesse sur 4 bits, niveau step sur 2 bits
            IsBusy = true;
            v_ParamSens = vl_param;
            double v_temp = 0;
            switch (v_ParamSens & 3)
            {
                case 0: v_nbrStep = 1; v_temp = 8; break;
                case 1: v_nbrStep = 2; v_temp = 4; break;
                case 2: v_nbrStep = 4; v_temp = 2; break;
                case 3: v_nbrStep = 8; v_temp = 1; break;
            }
            v_vitesse = ((v_ParamSens & 60) / 4) - 1;
            v_pulsesLOW = (.3125 + v_listeRalentir[v_vitesse]);
            v_pulsesLOW *= v_temp;
            v_pulsesLOW -= .100;
            //envoie info seulement si sens change
            int v_paramOld = v_ParamSens;
            //penser à mettre sens dans v_usbOUT;
            if ((byte)(v_ParamSens & 128) == 128) { v_usbOUT |= 2; } else { v_usbOUT &= 248; }
            if ((byte)(v_ParamSens & 64) == 64) { v_usbOUT |= 8; } else { v_usbOUT &= 242; }
            //enable le controlleur
            if (formMain.v_paramPortParallele.v_enableEtatBas)
            { v_usbOUT &= 191; }
            else { v_usbOUT |= 64; }
            v_usbOUT = v_usbOUT & 250;
            //v_usbOUT |= 64;
            if (v_paramOld != v_ParamSens)
            {
                try
                {
                    m_DLL_Output(v_adressePort, v_usbOUT);
                }
                catch { }
                m_delay(0.02);
            }
            IsBusy = false;
        }

        protected override double m_WriteInterne(int Value)
        {
            IsBusy = true;
            double v_result = 0;
            int v_usbLOW = 0;
            //teste si disable
            if (Value != 0)
            {
                v_usbLOW = Value & 250;
                v_usbOUT = Value;
            }
            else
            {
                if (formMain.v_paramPortParallele.v_enableEtatBas)
                { v_usbOUT |= 64; }
                else { v_usbOUT &= 58; } //111010
                m_DLL_Output(v_adressePort, v_usbOUT);
                return(0);
            }
            for (int i = 1; i <= v_nbrStep; i++)
            {
                m_DLL_Output(v_adressePort, Value);
                sW.Start();
                while (sW.Elapsed.TotalMilliseconds < 0.100)
                { }// System.Windows.Forms.Application.DoEvents(); }
                sW.Reset();
               m_DLL_Output(v_adressePort, v_usbLOW);
                sW.Start();
                while (sW.Elapsed.TotalMilliseconds < v_pulsesLOW)
                { }// System.Windows.Forms.Application.DoEvents(); }
                v_result += sW.Elapsed.TotalMilliseconds;
                sW.Reset();
            }
            IsBusy = false;
            return v_result;
        }
    }
}
