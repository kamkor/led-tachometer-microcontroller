using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KamkorTachometerApi
{
    public enum Commands : byte
    {
        ZeroLeds, OneLed, TwoLeds, ThreeLeds, FourLeds, FiveLeds, SixLeds, SevenLeds, EightLeds, NineLeds, TenLeds, TwelveLeds,
        FirstMode = (byte) '0', 
        SecondMode = (byte) '1', 
        ThirdMode = (byte) '2'
    };
}
