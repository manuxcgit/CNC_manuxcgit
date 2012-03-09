/*********************************************************************
 *
 *                Microchip USB C18 Firmware Version 1.0
 *
 *********************************************************************
 * FileName:        user.c
 * Dependencies:    See INCLUDES section below
 * Processor:       PIC18
 * Compiler:        C18 2.30.01+
 * Company:         Microchip Technology, Inc.
 *
 * Software License Agreement
 *
 * The software supplied herewith by Microchip Technology Incorporated
 * (the “Company”) for its PICmicro® Microcontroller is intended and
 * supplied to you, the Company’s customer, for use solely and
 * exclusively on Microchip PICmicro Microcontroller products. The
 * software is owned by the Company and/or its supplier, and is
 * protected under applicable copyright laws. All rights are reserved.
 * Any use in violation of the foregoing restrictions may subject the
 * user to criminal sanctions under applicable laws, as well as to
 * civil liability for the breach of the terms and conditions of this
 * license.
 *
 * THIS SOFTWARE IS PROVIDED IN AN “AS IS” CONDITION. NO WARRANTIES,
 * WHETHER EXPRESS, IMPLIED OR STATUTORY, INCLUDING, BUT NOT LIMITED
 * TO, IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A
 * PARTICULAR PURPOSE APPLY TO THIS SOFTWARE. THE COMPANY SHALL NOT,
 * IN ANY CIRCUMSTANCES, BE LIABLE FOR SPECIAL, INCIDENTAL OR
 * CONSEQUENTIAL DAMAGES, FOR ANY REASON WHATSOEVER.
 *
 * Author               Date        Comment
 *~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
 * Rawin Rojvanit       11/19/04    Original.
 ********************************************************************
 
 
 *FirmWare 1.5 du 10.06.09
 ajout renvoi n° firmWare à l'init

 *FirmWare 1.6 du 23.06.09
 reutilise controlleur gs D200s

 *Firmware 2.1 du 24.01.2010
 USB pour traiter sens X, sens Y, retour nbr de Pas
 transmet // > sorties Pic
 
 *Firmware 2.2 du 30.01.2010
 retransmet bits par USB
 
 *Firmware 2.3 du 03.03.2012
 confirme chgt sens par retour lecture port

/** I N C L U D E S **********************************************************/
#include <p18cxxx.h>
#include <usart.h>
#include "system\typedefs.h"

#include "system\usb\usb.h"

#include "io_cfg.h"             // I/O pin mapping
#include "user\user.h"

/** V A R I A B L E S ********************************************************/
#pragma udata

byte counter;
byte trf_state;
byte v_vitesse, v_dureeImpuls, v_usbOut;
word v_periode, v_idle;
word v_listePeriode[] = {1000,500,320,230,190,160,90};
word v_pasX = 0, v_pasY = 0;
byte v_usined = 1;

DATA_PACKET dataPacket;

/** P R I V A T E  P R O T O T Y P E S ***************************************/

void BlinkUSBStatus(void);
void ServiceRequests(void);
void Usinage(byte Code);
void WriteEEPROM(unsigned float data, byte memory_location);
byte ReadEEPROM(byte memory_location);
void Delay250Us(void);


/** D E C L A R A T I O N S **************************************************/
#pragma code
void UserInit(void)
{
    mInitAllLEDs();
relai = 1; 
}//end UserInit


/******************************************************************************
 * Function:        void ProcessIO(void)
 *
 * PreCondition:    None
 *
 * Input:           None
 *
 * Output:          None
 *
 * Side Effects:    None
 *
 * Overview:        This function is a place holder for other user routines.
 *                  It is a mixture of both USB and non-USB tasks.
 *
 * Note:            None
 *****************************************************************************/
void ProcessIO(void)
{   
    /*if (PORTAbits.RA4)
{
				dataPacket._byte[1]=v_pasX;
				dataPacket._byte[2]=(v_pasX & 0xff00)/0xff;
				dataPacket._byte[3]=v_pasY;
				dataPacket._byte[4]=(v_pasY & 0xff00)/0xff;
}*/
    BlinkUSBStatus();
    // User Application USB tasks
    if((usb_device_state < CONFIGURED_STATE)||(UCONbits.SUSPND==1)) return;
    
    ServiceRequests();
}//end ProcessIO


void ServiceRequests(void)
{
    byte index;
    
    if(USBGenRead((byte*)&dataPacket,sizeof(dataPacket)))
    {
        counter = 0;
        switch(dataPacket.CMD)
        {	        	
	        case TEST:
        		dataPacket._byte[1]='C';
            	dataPacket._byte[2]='N';
            	dataPacket._byte[3]='C';
            	dataPacket._byte[4]='2';
				dataPacket._byte[5]='.';
				dataPacket._byte[6]='3';
            	counter = 0x07;	
				break;

			case ACTIVE_RELAI:
				LATBbits.LATB4 = dataPacket._byte[1];
				mLED_2_Toggle();
				counter = 1;
				break;

			case ENABLE:
				LATBbits.LATB6 = dataPacket._byte[1];
				mLED_2_Toggle();
				counter = 1;
				break;

			case SET_SENS:
				LATBbits.LATB1 = dataPacket._byte[1];
				LATBbits.LATB3 = dataPacket._byte[2];
				mLED_2_Toggle();
				counter = 254;
				while (counter--) ;
				dataPacket._byte[1]=PORTBbits.RB1;
            	dataPacket._byte[2]=PORTBbits.RB3;
				counter = 3;
				break;
				
				
			case POSITION:
				//renvoie la valeur de portB et nbrImpulsions
				dataPacket._byte[1]=v_pasX;
				dataPacket._byte[2]=(v_pasX & 0xff00)/0xff;
				dataPacket._byte[3]=v_pasY;
				dataPacket._byte[4]=(v_pasY & 0xff00)/0xff;
				counter=0x05;
				break;
				
			case INITIALISE_X_Y:
				v_pasX=0;
				v_pasY=0;
				//mLED_2_Toggle();
				counter = 1;
				break;
				
			case USINAGE:
				v_usbOut = dataPacket._byte[1];
				v_idle = v_periode;
				Usinage(v_usbOut);
				v_usined = 0;
				dataPacket._byte[1]=v_usbOut*2;
				counter=0;
				break;
				
			case SET_PARAMS:
				v_vitesse = dataPacket._byte[2];
				v_dureeImpuls = dataPacket._byte[1];
				counter = 1;
				v_periode = v_listePeriode[v_vitesse];
				//mLED_2_Toggle();	
				break;
	        	
                	                   
            case RESET:
                Reset();
                break;
                
            default:
                break;
        }//end switch()
        if(counter != 0)
        {
            if(!mUSBGenTxIsBusy())
            {
                USBGenWrite((byte*)&dataPacket,counter);
             // 	mLED_2_Toggle();
            }    	        	
        }//end if
    }//end if

}//end ServiceRequests

/******************************************************************************
 * Function:        void BlinkUSBStatus(void)
 *
 * PreCondition:    None
 *
 * Input:           None
 *
 * Output:          None
 *
 * Side Effects:    None
 *
 * Overview:        BlinkUSBStatus turns on and off LEDs corresponding to
 *                  the USB device state.
 *
 * Note:            mLED macros can be found in io_cfg.h
 *                  usb_device_state is declared in usbmmap.c and is modified
 *                  in usbdrv.c, usbctrltrf.c, and usb9.c
 *****************************************************************************/
void BlinkUSBStatus(void)
{
    static word led_count=0;
    
    if(led_count == 0)led_count = 10000U;
    led_count--;

    #define mLED_Both_Off()         {mLED_1_Off();mLED_2_Off();}
    #define mLED_Both_On()          {mLED_1_On();mLED_2_On();}
    #define mLED_Only_1_On()        {mLED_1_On();mLED_2_Off();}
    #define mLED_Only_2_On()        {mLED_1_Off();mLED_2_On();}

    if(UCONbits.SUSPND == 1)
    {
        if(led_count==0)
        {
            mLED_1_Toggle();
            mLED_2 = mLED_1;        // Both blink at the same time
        }//end if
    }
    else
    {
        if(usb_device_state == DETACHED_STATE)
        {
            mLED_Both_Off();
            
        }
        else if(usb_device_state == ATTACHED_STATE)
        {
            mLED_Both_On();
        }
        else if(usb_device_state == POWERED_STATE)
        {
            mLED_Only_1_On();
        }
        else if(usb_device_state == DEFAULT_STATE)
        {
            mLED_Only_2_On();
        }
        else if(usb_device_state == ADDRESS_STATE)
        {
            if(led_count == 0)
            {
                mLED_1_Toggle();
                mLED_2_Off();
            }//end if
        }
        else if(usb_device_state == CONFIGURED_STATE)
        {
            if(led_count==0)
            {
                mLED_1_Toggle();
             //   mLED_2 = !mLED_1;       // Alternate blink                
            }//end if
        }//end if(...)
        
        if (v_idle>1)
        {v_idle--;}
        else
        {
	        if (v_usined == 0)
	        {
        		Usinage(v_usbOut);
        		v_usined = 1;
        	}	
        }
    }//end if(UCONbits.SUSPND...)

}//end BlinkUSBStatus

void Usinage (byte Code)
{
    byte timer;
    timer = v_dureeImpuls;
	if ((Code==1) || (Code==3))
		{ 
			LATBbits.LATB0=1;
			v_pasX++;
		} 
	if ((Code==2) || (Code==3))
		{ 
			LATBbits.LATB2=1;
			v_pasY++;
		}
	while (-- timer) continue;
	LATBbits.LATB0 = 0;
	LATBbits.LATB2 = 0;
}	


void WriteEEPROM(unsigned float data, byte memory_location)
{
	static unsigned char GIE_Status;
	EEADR = memory_location;  //EEPROM memory location
	EEDATA = data;     //Data to be writen 
	EECON1bits.EEPGD=0;    //Enable EEPROM write
	EECON1bits.CFGS=0;    //Enable EEPROM write
	EECON1bits.WREN = 1;   //Enable EEPROM write
	//GIE_Status = INTCONbits.GIE; //Save global interrupt enable bit
	//INTCONbits.GIE=0;    //Disable global interrupts
	EECON2 = 0x55;     //Required sequence to start write cycle
	EECON2 = 0xAA;     //Required sequence to start write cycle
	EECON1bits.WR = 1;    //Required sequence to start write cycle
	//INTCONbits.GIE=GIE_Status;  //Restore the original global interrupt status
	while(EECON1bits.WR);   //Wait for completion of write sequence
	PIR2bits.EEIF = 0;    //Disable EEPROM write
	EECON1bits.WREN = 0;   //Disable EEPROM write
}

byte ReadEEPROM(byte memory_location)//void)
{
	byte memory_data;
	EEADR = memory_location;  //EEPROM memory location
	EECON1bits.EEPGD = 0;   //Enable read sequence
	EECON1bits.CFGS = 0;   //Enable read sequence
	EECON1bits.RD = 1;    //Enable read sequence
	Delay10TCYx(2);    //Delay to ensure read is completed
	memory_data = EEDATA; 
	return memory_data;        
}


void Delay250Us(void)
{
unsigned char _dcnt;
_dcnt = 250/(12/20); //20 = XTAL _ FREQ
while(--_dcnt) continue;
}

/** EOF user.c ***************************************************************/
