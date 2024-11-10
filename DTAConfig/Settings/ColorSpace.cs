using System;
using Microsoft.Xna.Framework;

namespace DTAConfig.Settings
{
    public class ColorSpace
    {
        public static Color RGB2HSI(Color rgb)
        {
            double h, s, i;
            double r = rgb.R / 255.0;
            double g = rgb.G / 255.0;
            double b = rgb.B / 255.0;

            double theta = Math.Acos(0.5 * ((r - g) + (r - b))/Math.Sqrt((r - g) * (r - g) + (r - b) * (g - b))) / (2 * Math.PI);

            h = (b <= g) ? theta : (1 - theta);
            s = (1 - 3 * Math.Min(Math.Min(r, g), b) / (r + g + b));
            i = (r + g + b) / 3.0;
            return new Color((byte)(h * 255.0 + .5), (byte)(s * 255.0 + .5), (byte)(i * 255.0 + .5));
        }

        public static Color HSI2RGB(Color hsi)
        {
            double r, g, b;

            double h = hsi.R;
            double s = hsi.G;
            double i = hsi.B;

            h = h * 2 * Math.PI;
            if(h > 0 && h < (2 *Math.PI / 3))
            {
                b = i * (1 - s);
                r = i * (1 + s * Math.Cos(h) / Math.Cos(Math.PI / 3 - h));
                g = 3 * i - (r + b);
            }
            else if( h >= 2 * Math.PI / 3 && h < 4 * Math.PI/ 3)
            {
                r = i * (1 - s);
                g = i * (1 + s * Math.Cos(h - 2 * Math.PI / 3) / Math.Cos(Math.PI - h));
                b = 3 * i - (r + g);
            }
            else
            {
                g = i * (1 - s);
                b = i * (1 + s * Math.Cos(h - 4 * Math.PI / 3) / Math.Cos(5 * Math.PI / 3 - h));
                r = 3 * i - (g + b);
            }

            return new Color((byte)(r * 255.0 + .5), (byte)(g * 255.0 + .5), (byte)(b * 255.0 + .5));
        }
    }
}
