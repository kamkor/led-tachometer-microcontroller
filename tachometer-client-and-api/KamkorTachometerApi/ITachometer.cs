using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KamkorTachometerApi
{
    interface ITachometer
    {
        void openConnection();
        void closeConnection();
        void sendData(Byte cmd);
        void sendData(Commands cmd);        
    }
}
