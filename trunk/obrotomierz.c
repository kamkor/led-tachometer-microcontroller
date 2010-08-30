#include "obrotomierz.h"
#include <avr/io.h>
#include <util/delay.h>
#include <avr/interrupt.h>

#define SHORT_DELAY 50
#define DELAY 100
#define F_CPU 1000000

#define FOSC 1000000
// error 0,2%. Baud can be 9600 with same error if U2X = 1
#define BAUD 4800
//#define MYUBBR FOSC/16/BAUD-1
#define MYUBBR ((FOSC)/((BAUD)*16l)-1)

//volatile byte direction = 0;
unsigned char i;
// mode 0 lights up leds from right to left
// mode 1 lights up leds from left to right
// mode 2 lights up leds from left to right and from right to left
volatile unsigned char  mode = 2;

// external interrupt button, changes techometer mode
SIGNAL(SIG_INTERRUPT0) {	
	change_mode();
}


// serial port receive interrupt
SIGNAL(SIG_UART_RECV) {
	unsigned char c;
	c = UDR;	
	switch (c) {
		case '0':
			mode = 0;
			break;
		case '1':
			mode = 1;
			break;
		case '2': 
			mode = 2;
			break;
	}
	light_up_leds(c);

}


void change_mode(void) { 
	if (mode == 0) 
		mode = 1;
	else if (mode == 1) 
		mode = 2;
	else 
		mode = 0;
}

void light_up_leds(unsigned char number_of_leds) { 
	unsigned char shift_count;
	if (mode == 0 || mode == 1) {
		switch (number_of_leds) {
			case 0:
				PORTA = 0xFF; // 11111111
				PORTC = 0xF0; // 11110000
				break;
			case 1:
				setup_leds(mode);
				break;
			case 2:	case 3:	case 4:		
				shift_count = number_of_leds - 1;
				if (mode == 0) {
					PORTC = 0xE0<<shift_count;	// 11100000<<shift_count
					PORTA = 0xFF;
				} else if (mode == 1) {
					PORTC = 0xF0;				// 11110000
					PORTA = 0xFE<<shift_count;	// 11111110
				} 
				break;		
			case 5: case 6: case 7: case 8:
				shift_count = number_of_leds - 5;
				if (mode == 0) {
					PORTC = 0;
					PORTA = 0x7F>>shift_count; // 01111111 >> shift_count
				} else if (mode == 1) {
					PORTC = 0xF0;
					PORTA = 0xE0<<shift_count; // 11100000 >> shift_count				
				}		
				break;
			case 9: case 10: case 11: case 12:
				shift_count = number_of_leds - 9;
				USART_Transmit(number_of_leds);			
				if (mode == 0) { 
					PORTC = 0;
					PORTA = 7>>shift_count; // 00000111 >> shift_count				
				} else if (mode == 1) {
					PORTC = 0x70>>shift_count; // 01110000 >> shift_count
					PORTA = 0;			
				}
				break;							
		}
	} else { // third mode (leds lightning up to the center)
		switch (number_of_leds) { 
			case 0:
				PORTA = 0xFF; // 11111111
				PORTC = 0xF0; // 11110000
				break;
		 	case 2:
				setup_leds(mode);
				break;
			case 4:	case 6:	case 8:
				shift_count = (number_of_leds / 2) - 1;
				PORTC = 0xE0<<shift_count;	// 11100000<<shift_count
				PORTA = 0xFE<<shift_count;	// 11111110<<shift_count
				break;
			case 10:
				PORTC = 0;
				PORTA = 0x60;
				break;
			case 12:
				PORTC = 0;
				PORTA = 0;
				break;
		}
	}
}	

// serial transmit method
void USART_Transmit(unsigned char data) {
	// wait for empty transmit buffer
	while (!(UCSRA & (1<<UDRE)));
	// put data into buffer, sends the data
	UDR = data;
}

// serial receive method for pooling (not used)
unsigned char USART_Receive(void) {
	// wait for data to be received
	while (!(UCSRA & (1<<RXC)));
	// get and return received data from buffer;
	return UDR;
}

// initializes serial port
void USART_Init(unsigned int baud) {
	// set baud rate
	UBRRH = (unsigned char) (baud>>8);
	UBRRL = (unsigned char) baud;

	// enable receiver and transmitter 
	UCSRB = (1<<RXEN) | (1<<TXEN) | (1 << RXCIE);
	// Set frame format: 8data, 1stop bit 
	UCSRC = (1<<URSEL) | (3<<UCSZ0);
}

int main(void) {
	// init usart
	USART_Init(MYUBBR);
	
	// pull up int0 pin
	PORTD |= (1<<PORTD2);
	// interrupt on rising edge of int0
	MCUCR |= (1<<ISC01);
	// add int0 to interrupts
  	GICR |= (1<<INT0);
	// enable interrupts
  	sei();                

	// port A as output
	DDRA = 0xFF;	
	// Turn off leds
	PORTA = 0xFF;

	// Part of PORT C as output
	DDRC |= (15<<DDC4);
	//turn off leds
	PORTC = (15<<PORTC4);
	// pull up button on portc0
	PORTD |= (1<<PORTD7);

	test_leds();
  	while(1) {		
		if (!(PIND & (1<<PIND7))) {
			test_leds();
		} 		
  	}              
}

void test_leds(void) { 	
	// if 0, leds will light up to the left, if 1, then to the right
	// if 2 then as xxx____xxx (from left and right)
	unsigned char test_mode = mode;

	setup_leds(test_mode);
	delay_ms(SHORT_DELAY);
	while (PORTA != 0 || PORTC != 0) {				
		if (mode != test_mode) {			
			test_mode = mode;
			setup_leds(mode);			
		} else if (mode == 0) {				
			if (PORTC == 0 && PORTA == 0xFF) {
				PORTA = 0x7F;				
			} else if (PORTC == 0) {
				PORTA = PORTA>>1;				
			} else {
				PORTC = PORTC<<1;
			}						
		} else if (mode == 1) {			
			if (PORTA == 0 && PORTC == 0xF0) {
				PORTC = 0x70;
			} else if (PORTA == 0) {
				PORTC = PORTC>>1 & 0xF0;
			} else {
				PORTA = PORTA<<1;
			}			
		} else {
			if (PORTC != 0) { 
				PORTC = PORTC<<1;					
				PORTA = PORTA<<1;
			} else if (PORTA & 0x80) {				
				PORTA = PORTA & 0x7F;
			} else { 
				PORTA = PORTA>>1 & PORTA<<1;
			}							
		}	
		delay_ms(SHORT_DELAY);
	}
		
	// all leds blinking
	for (i = 0; i < 4; i++) {
		delay_ms(DELAY);
		PORTA = 0xFF;
		PORTC = 15<<PORTC4;
		delay_ms(SHORT_DELAY);
		PORTA = 0;
		PORTC = 0;
	}		
		
	// turn off leds after test	
	PORTA = 0xFF;
	PORTC = 15<<PORTC4;
}

void setup_leds(int mode) {
	if (mode == 0) {
		PORTC = 14<<PORTC4; 
		PORTA = 0xFF;
	} else if (mode == 1) {
		PORTC = 15<<PORTC4;		
		PORTA = 0xFE;	
	} else {
		PORTA = 0xFE;
		PORTC = 14<<PORTC4;		
	}
}

void delay_ms(uint16_t ms) {
	while (ms) {
		_delay_ms(1);
		ms--;
	}
}

