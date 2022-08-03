using MiitsuColorController.Models;
using System;
using Windows.UI;

namespace MiitsuColorController.Helper
{
    internal class ColorHelper
    {
        //360, 1, 1 to 255, 255, 255,
        public static byte[] ConvertHSV2RGB(float h, float s, float v)
        {
            byte r;
            byte g;
            byte b;

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
            return new byte[] { r, g, b };
        }

        public static Color ConvertHSV2RGBColor(float h, float s, float v)
        {
            byte[] rgb = ConvertHSV2RGB(h, s, v);
            return Color.FromArgb(255, rgb[0], rgb[1], rgb[2]);
        }

        public static void ConvertHSV2RGBColorTint(float h, float s, float v, ref ArtMeshColorTint result)
        {
            byte[] rgb = ConvertHSV2RGB(h, s, v);
            result.colorB = rgb[2];
            result.colorG = rgb[1];
            result.colorR = rgb[0];
        }

        public static void RBGToAdjustedColorTint(float[] color, float sRatio, float minS, float vRatio, float minV, ref ArtMeshColorTint result)
        {
            float[] hsv = { 0, 0, 0 };
            ConvertRGB2HSV(color, ref hsv);
            hsv[1] = hsv[1] * sRatio + minS / 100;
            hsv[2] = hsv[2] * vRatio + minV / 100;
            ConvertHSV2RGBColorTint(hsv[0], hsv[1], hsv[2], ref result);
        }

        //255, 255, 255 to 360, 1, 1
        private static void ConvertRGB2HSV(float[] color, ref float[] result)
        {
            float R = color[0];
            float G = color[1];
            float B = color[2];

            float Min = Math.Min(Math.Min(R, G), B);
            float Max = Math.Max(Math.Max(R, G), B);
            float Delta = Max - Min;

            result[2] = Max;

            if (result[2] == 0)
            {
                result[1] = 0;
            }
            else
            {
                result[1] = Delta / Max;
            }

            if (result[1] == 0)
            {
                result[0] = 0;
            }
            else
            {
                if (R == Max)
                {
                    result[0] = (G - B) / Delta;
                }
                else if (G == Max)
                {
                    result[0] = 2f + (B - R) / Delta;
                }
                else if (B == Max)
                {
                    result[0] = 4f + (R - G) / Delta;
                }
                result[0] *= 60;
                if (result[0] < 0.0)
                {
                    result[0] = result[0] + 360;
                }
            }

            result[2] /= 255;
        }
    }
}