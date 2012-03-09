/*********************************************************************
 *
 *                Microchip USB C18 Firmware Version 1.0
 *
 *********************************************************************
 * FileName:        user.h
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
 * Rawin Rojvanit       11/19/04     Original.
 ********************************************************************/

#ifndef PICDEM_FS_DEMO_H
#define PICDEM_FS_DEMO_H

/** I N C L U D E S **********************************************************/
#include "system\typedefs.h"

/** D E F I N I T I O N S ****************************************************/
/* PICDEM FS USB Demo Version */
#define MINOR_VERSION   0x00    //Demo Version 1.00
#define MAJOR_VERSION   0x01

/** S T R U C T U R E S ******************************************************/
typedef union DATA_PACKET
{
    byte _byte[USBGEN_EP_SIZE];  //For byte access
    word _word[USBGEN_EP_SIZE/2];//For word access(USBGEN_EP_SIZE msut be even)
    struct
    {
        enum
        {
            READ_VERSION    = 0x00,
            TEST_MANU		= 0x30,
            UPDATE_LED      = 0x32,
			INCREMENTVAL 	= 0x33,
			ACTIVE_RELAI    = 0x41,
			ENABLE			= 0x45,
			POSITION		= 0x50, //'P'
			INITIALISE_X_Y	= 0x51, //'Q'
			SET_PARAMS      = 0x52, //'R'
			SET_SENS		= 0x53, //'S'
			TEST			= 0x54, //'T'
            USINAGE			= 0x55, //'U'
            RESET           = 0xFF
        }CMD;
        byte len;
    };
    struct
    {
        unsigned :8;
        byte ID;
    };
    struct
    {
        unsigned :8;
        byte led_num;
        byte led_status;
    };
    struct
    {
        unsigned :8;
        word word_data;
    };
} DATA_PACKET;

/** P U B L I C  P R O T O T Y P E S *****************************************/
void UserInit(void);
void ProcessIO(void);
void m_TestPortA(void);

#endif //PICDEM_FS_DEMO_H
