using System;
using Windows.UI;

namespace MiitsuColorController.Helper
{
    internal class ColorHelper
    {
        public static Color ConvertHSV2RGB(float h, float s, float v)
        {
            byte r = 0;
            byte g = 0;
            byte b = 0;

            h /= 60;
            int i = (int)Math.Floor(h);
            float f = h - i;
            float p = v * (1 - s);
            float q = v * (1 - s * f);
            float t = v * (1 - s * (1 - f));

            switch (i)
            {
                case 0:
                    r = (byte)(255 * v);
                    g = (byte)(255 * t);
                    b = (byte)(255 * p);
                    break;
                case 1:
                    r = (byte)(255 * q);
                    g = (byte)(255 * v);
                    b = (byte)(255 * p);
                    break;
                case 2:
                    r = (byte)(255 * p);
                    g = (byte)(255 * v);
                    b = (byte)(255 * t);
                    break;
                case 3:
                    r = (byte)(255 * p);
                    g = (byte)(255 * q);
                    b = (byte)(255 * v);
                    break;
                case 4:
                    r = (byte)(255 * t);
                    g = (byte)(255 * p);
                    b = (byte)(255 * v);
                    break;
                default:
                    r = (byte)(255 * v);
                    g = (byte)(255 * p);
                    b = (byte)(255 * q);
                    break;
            }
            return Color.FromArgb(255, r, g, b);
        }
    }
}
