using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;

namespace KamkorTachometerApi
{
    /// <summary>
    /// Tachometer class can connect to kamkor led tachometer and send commands to it 
    /// </summary>
    public class Tachometer : ITachometer
    {
        private string comPort;
        private SerialPort port;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="comPort">ie "COM1"</param>
        public Tachometer(string comPort)
        {
            this.comPort = comPort;
            openConnection();
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="comPort">ie "COM1"</param>
        /// <param name="mode">mode - left to right, right to left or both</param>
        public Tachometer(string comPort, Commands mode)
            : this (comPort)
        {                       
            sendData(mode);
        }

        public Tachometer(string comPort, char mode)
            : this (comPort)
        {
            sendData((byte)mode);
        }

        public void openConnection()
        {
            if (port == null) {
                try
                {
                    port = new SerialPort(
                        comPort, 4800, Parity.None, 8, StopBits.One);
                }
                catch (System.IO.IOException ex)
                {
                    throw ex;
                }
            }
            if (!port.IsOpen)
            {
                port.Open();
            }            
        }

        public void closeConnection()
        {
            port.Close();
        }

        public void sendData(Byte cmd)
        {
            if (port.IsOpen)
            {
                port.Write(new byte[] { cmd }, 0, 1);
            }
        }

        public void sendData(Commands cmd)
        {
            if (port.IsOpen)
            {
                port.Write(new byte[] { (byte)cmd }, 0, 1);
            }
        }

        ~Tachometer()
        {
            if (port.IsOpen)
            {
                port.Close();
            }
        }
    }
}
