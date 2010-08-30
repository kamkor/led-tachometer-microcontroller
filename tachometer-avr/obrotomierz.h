#ifndef OBROTOMIERZ
#define OBROTOMIERZ

void change_mode(void);
void light_up_leds(unsigned char number_of_leds);
void USART_Transmit(unsigned char data);
unsigned char USART_Receive(void);
void USART_Init(unsigned int baud);
void test_leds(void);
void setup_leds(int mode);

#endif
