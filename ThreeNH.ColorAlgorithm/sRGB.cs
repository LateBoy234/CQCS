using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ThreeNH.ColorAlgorithm
{
    public class sRGB
    {
        private int _rgb;

        public sRGB(int rgb)
        {
            _rgb = rgb;
        }

        public sRGB(byte R, byte G, byte B)
        {
            _rgb = (((R & 0xFF) << 16) | ((G & 0xFF) << 8) | (B & 0xFF));
        }

        public byte R
        {
            get { return (byte) ((_rgb >> 16) & 0xFF); }
            set { _rgb |= _rgb << 16; }
        }

        public byte G
        {
            get { return (byte)((_rgb >> 8) & 0xFF); }
            set { _rgb |= _rgb << 8; }
        }

        public byte B
        {
            get { return (byte)(_rgb & 0xFF); }
            set { _rgb |= _rgb; }
        }

        public int ToInteger()
        {
            return _rgb;
        }
    }
}
