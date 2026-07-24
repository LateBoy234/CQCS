using DocumentFormat.OpenXml.Office2013.Drawing.ChartStyle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ThreeNH.ColorAlgorithm;

namespace ThreeNH.ColorQualityControl.Utils
{
    public static class AlgorithmExtension
    {

        static float[] integral_percent =
        { 0, 0, 0, 0, 0.0004f, 0.0012f, 0.0040f, 0.0116f,
            0.023f, 0.038f, 0.06f, 0.091f, 0.139f, 0.208f, 0.323f, 0.503f,
            0.71f, 0.862f, 0.954f, 0.955f, 0.995f, 0.952f, 0.87f, 0.757f,
            0.631f, 0.503f, 0.381f, 0.265f, 0.175f, 0.107f, 0.061f, 0.032f,
            0.017f, 0.0082f, 0.0041f, 0, 0, 0, 0, 0,
            0, 0, 0,
        };
        private static int standand = 113;

        private static float integral_quantity(Spectrum spectrum)
        {
            float data = 0;

            for (int i = 0; i < integral_percent.Length; i++)
            {
                int wavelength = 360 + i * 10;
                if (wavelength < spectrum.Range.Min || wavelength > spectrum.Range.Max)
                {
                    continue;
                }
                data += (float)spectrum[wavelength] * integral_percent[i];
            }
            return data;
        }
        public static short clralgo_8_gloss_data(Spectrum ssci, Spectrum ssce, Spectrum wsci, Spectrum wsce)
        {
            float sample = 0, sample1 = 0, glass = 0, glass1 = 0, data = 0;
            sample = integral_quantity(ssci);
            if (sample == 0) return 0;
            sample1 = integral_quantity(ssce);
            if (sample1 == 0) return 0;
            sample -= integral_quantity(ssce);

            glass = integral_quantity(wsci);
            glass1 = integral_quantity(wsce);

            glass -= glass1;
            data = Math.Abs(sample) / Math.Abs(glass) * standand;
            //data *= 10;
            var temp = Math.Round(data);
            return (short)temp;
            //var data_8_gloss = (short)(Math.Abs(data) / 10 + ((short)Math.Abs(data) % 10 > 5 ? 1 : 0));
            //return data_8_gloss;
        }

    }
}
