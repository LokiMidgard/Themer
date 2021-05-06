using System;
using System.Drawing;

namespace Themer
{
    internal struct Hsl
    {
        public Hsl(float h, float s, float l) : this()
        {
            this.Hue = h;
            this.Saturation = s;
            this.Luminance = l;
        }

        public float Hue { get; set; }
        public float Saturation { get; set; }
        public float Luminance { get; set; }

        internal static Hsl RgbToHsl(Color c)
        {
            var r = c.R / 255f;
            var g = c.G / 255f;
            var b = c.B / 255f;

            var max = Math.Max(r, Math.Max(g, b));
            var min = Math.Min(r, Math.Min(g, b));

            float h;
            float s;
            var l = (max + min) / 2;
            if (max == min)
                h = s = 0;
            else
            {
                var d = max - min;
                s = l > 0.5
                    ? d / (2 - max - min)
                    : d / (max + min);
                h = max == r
                    ? (g - b) / d + (g < b ? 6 : 0)
                    : max == g
                        ? (b - r) / d + 2
                        : h = (r - g) / d + 4;
                h /= 6;
            }
            return new Hsl(h, s, l);
        }

        internal static Color HslToRgb(Hsl hsl)
        {
            float r;
            float g;
            float b;

            var h = hsl.Hue;
            var s = hsl.Saturation;
            var l = hsl.Luminance;

            static float Hue2rgb(float p, float q, float t)
            {
                if (t < 0)
                    t += 1;
                if (t > 1)
                    t -= 1;
                if (t < 1 / 6)
                    return p + (q - p) * 6 * t;
                if (t < 1 / 2)
                    return q;
                if (t < 2 / 3)
                    return p + (q - p) * (2 / 3 - t) * 6;
                return p;
            }
            if (s == 0)
            {
                r = g = b = 1;
            }
            else
            {
                var q = l < 0.5
                    ? l * (1 + s)
                    : l + s - (l * s);
                var p = 2 * l - q;
                r = Hue2rgb(p, q, h + (1 / 3f));
                g = Hue2rgb(p, q, h);
                b = Hue2rgb(p, q, h - (1 / 3f));
            }
            return Color.FromArgb((int)(r * 255), (int)(g * 255), (int)(b * 255));
        }


    }


    //struct VBox
    //{
    //    public VBox(byte r1, byte r2, byte g1, byte g2, byte b1, byte b2, int[] histo)
    //    {
    //        this.R1 = r1;
    //        this.R2 = r2;
    //        this.G1 = g1;
    //        this.G2 = g2;
    //        this.B1 = b1;
    //        this.B2 = b2;
    //        this.Histo = histo;
    //    }

    //    public byte R1 { get; }
    //    public byte R2 { get; }
    //    public byte G1 { get; }
    //    public byte G2 { get; }
    //    public byte B1 { get; }
    //    public byte B2 { get; }
    //    public int[] Histo { get; }

    //    public int Volume
    //    {
    //        get
    //        {
    //            return ((this.R2 - this.R1 + 1) * (this.G2 - this.G1 + 1) * (this.B2 - this.B1 + 1));
    //        }
    //    }

    //    public int Count
    //    {
    //        get
    //        {
    //            var vbox = this;
    //            var histo = vbox.Histo;
    //            //if (!vbox._count_set)
    //            {
    //                var npix = 0;

    //                for (var i = vbox.R1; i <= vbox.R2; i++)
    //                {
    //                    for (var j = vbox.G1; j <= vbox.G2; j++)
    //                    {
    //                        for (var k = vbox.B1; k <= vbox.B2; k++)
    //                        {
    //                            var index = getColorIndex(i, j, k);
    //                            npix += (histo[index] || 0);
    //                        }
    //                    }
    //                }
    //                vbox._count = npix;
    //                vbox._count_set = true;
    //            }
    //            return vbox._count;
    //        }
    //    }

    //}

    //class CMap
    //{
    //    internal void Push(VBox vboc)
    //    }
    //public static class Quantize
    //{
    //    internal static CMap For(List<Color> allPixels, int colorCount)
    //    {
    //        throw new NotImplementedException();
    //    }
    //}
}
