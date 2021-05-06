using System;
using System.Buffers;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Themer
{
    internal static class Quantizer
    {
        private const int PropertyTagIndexTransparent = 0x5104;
        private const int PropertyTagTypeByte = 1;
        private class Pnnbin
        {
            internal float ac, rc, gc, bc;
            internal int cnt;
            internal int nn, fw, bk, tm, mtm;
            internal float err;
        }

        private static int GetARGB1555(Color c)
        {
            return (c.A & 0x80) << 8 | (c.R & 0xF8) << 7 | (c.G & 0xF8) << 2 | (c.B >> 3);
        }
        private static int GetARGBIndex(Color c, bool hasSemiTransparency, bool hasTransparency)
        {
            if (hasSemiTransparency)
                return (c.A & 0xF0) << 8 | (c.R & 0xF0) << 4 | (c.G & 0xF0) | (c.B >> 4);
            if (hasTransparency)
                return GetARGB1555(c);
            return (c.R & 0xF8) << 8 | (c.G & 0xFC) << 3 | (c.B >> 3);
        }

        private static double Sqr(double value)
        {
            return value * value;
        }
        private static void Find_nn(Span<Pnnbin> bins, int idx)
        {
            var nn = 0;
            var err = 1e100;

            ref var bin1 = ref bins[idx];
            var n1 = bin1.cnt;
            var wa = bin1.ac;
            var wr = bin1.rc;
            var wg = bin1.gc;
            var wb = bin1.bc;
            for (var i = bin1.fw; i != 0; i = bins[i].fw)
            {
                var nerr = Sqr(bins[i].ac - wa) + Sqr(bins[i].rc - wr) + Sqr(bins[i].gc - wg) + Sqr(bins[i].bc - wb);
                var n2 = bins[i].cnt;
                nerr *= (n1 * n2) / (n1 + n2);
                if (nerr >= err)
                    continue;
                err = nerr;
                nn = i;
            }
            bin1.err = (float)err;
            bin1.nn = nn;
        }
        private static Color[] Pnnquan(Span<Color> pixels, int nMaxColors, short quan_rt, bool hasSemiTransparency, int transparentPixelIndex, Color transparentColor)
        {

            var bins = new Pnnbin[65536];
            var palettes = new Color[nMaxColors];
            /* Build histogram */
            foreach (var c in pixels)
            {
                // !!! Can throw gamma correction in here, but what to do about perceptual
                // !!! nonuniformity then?

                int index = GetARGBIndex(c, hasSemiTransparency, transparentPixelIndex > -1);
                if (bins[index] == null)
                    bins[index] = new Pnnbin();
                bins[index].ac += c.A;
                bins[index].rc += c.R;
                bins[index].gc += c.G;
                bins[index].bc += c.B;
                bins[index].cnt++;
            }

            /* Cluster nonempty bins at one end of array */
            int maxbins = 0;
            for (int i = 0; i < bins.Length; ++i)
            {
                if (bins[i] == null)
                    continue;

                var d = 1.0f / (float)bins[i].cnt;
                bins[i].ac *= d;
                bins[i].rc *= d;
                bins[i].gc *= d;
                bins[i].bc *= d;

                bins[maxbins++] = bins[i];
            }

            if (nMaxColors < 16)
                nMaxColors = -1;
            if (Sqr(nMaxColors) / maxbins < .022)
                quan_rt = 0;

            if (quan_rt > 0)
                bins[0].cnt = (int)Math.Sqrt(bins[0].cnt);
            else if (quan_rt < 0)
                bins[0].cnt = (int)Math.Cbrt(bins[0].cnt);
            for (int i = 0; i < maxbins - 1; ++i)
            {
                bins[i].fw = i + 1;
                bins[i + 1].bk = i;

                if (quan_rt > 0)
                    bins[i + 1].cnt = (int)Math.Sqrt(bins[i + 1].cnt);
                else if (quan_rt < 0)
                    bins[i + 1].cnt = (int)Math.Cbrt(bins[i + 1].cnt);
            }

            int h, l, l2;
            /* Initialize nearest neighbors and build heap of them */
            var heap = new int[bins.Length + 1];
            for (int i = 0; i < maxbins; ++i)
            {
                Find_nn(bins, i);
                /* Push slot on heap */
                double err = bins[i].err;
                for (l = ++heap[0]; l > 1; l = l2)
                {
                    l2 = l >> 1;
                    if (bins[h = heap[l2]].err <= err)
                        break;
                    heap[l] = h;
                }
                heap[l] = i;
            }

            /* Merge bins which increase error the least */
            int extbins = maxbins - nMaxColors;
            for (int i = 0; i < extbins;)
            {
                Pnnbin tb;
                /* Use heap to find which bins to merge */
                for (; ; )
                {
                    int b1 = heap[1];
                    tb = bins[b1]; /* One with least error */
                    /* Is stored error up to date? */
                    if ((tb.tm >= tb.mtm) && (bins[tb.nn].mtm <= tb.tm))
                        break;
                    if (tb.mtm == 0xFFFF) /* Deleted node */
                        b1 = heap[1] = heap[heap[0]--];
                    else /* Too old error value */
                    {
                        Find_nn(bins, b1);
                        tb.tm = i;
                    }
                    /* Push slot down */
                    var err = bins[b1].err;
                    for (l = 1; (l2 = l + l) <= heap[0]; l = l2)
                    {
                        if ((l2 < heap[0]) && (bins[heap[l2]].err > bins[heap[l2 + 1]].err))
                            ++l2;
                        if (err <= bins[h = heap[l2]].err)
                            break;
                        heap[l] = h;
                    }
                    heap[l] = b1;
                }

                /* Do a merge */
                var nb = bins[tb.nn];
                var n1 = tb.cnt;
                var n2 = nb.cnt;
                var d = 1.0f / (n1 + n2);
                tb.ac = d * (n1 * tb.ac + n2 * nb.ac);
                tb.rc = d * (n1 * tb.rc + n2 * nb.rc);
                tb.gc = d * (n1 * tb.gc + n2 * nb.gc);
                tb.bc = d * (n1 * tb.bc + n2 * nb.bc);
                tb.cnt += nb.cnt;
                tb.mtm = ++i;

                /* Unchain deleted bin */
                bins[nb.bk].fw = nb.fw;
                bins[nb.fw].bk = nb.bk;
                nb.mtm = 0xFFFF;
            }
            static void Swap<T>(ref T x, ref T y)
            {
                var t = y;
                y = x;
                x = t;
            }


            /* Fill palette */
            int k = 0;
            for (int i = 0; ; ++k)
            {
                var alpha = Math.Clamp((int)bins[i].ac, byte.MinValue, byte.MaxValue);
                palettes[k] = Color.FromArgb(alpha, Math.Clamp((int)bins[i].rc, byte.MinValue, byte.MaxValue), Math.Clamp((int)bins[i].gc, byte.MinValue, byte.MaxValue), Math.Clamp((int)bins[i].bc, byte.MinValue, byte.MaxValue));
                if (transparentPixelIndex >= 0 && palettes[k] == transparentColor)
                    Swap(ref palettes[0], ref palettes[k]);

                if ((i = bins[i].fw) == 0)
                    break;
            }

            Array.Resize(ref palettes, k);

            return palettes;
        }

        private static bool GrabPixels(Bitmap source, ref Span<Color> pixels, out bool hasSemiTransparency, out int transparentPixelIndex, ref Color transparentColor, int quality)
        {
            if (quality < 1)
                throw new ArgumentOutOfRangeException(nameof(quality), quality, "Quality must at least be *1*. Which is the best quality");
            var bitmapWidth = source.Width;
            var bitmapHeight = source.Height;

            hasSemiTransparency = false;
            transparentPixelIndex = -1;

            var transparentIndex = -1;
            var palettes = source.Palette.Entries;
            foreach (var pPropertyItem in source.PropertyItems)
            {
                if (pPropertyItem.Id == PropertyTagIndexTransparent)
                {
                    transparentIndex = pPropertyItem.Value![0];
                    var c = palettes[transparentIndex];
                    transparentColor = Color.FromArgb(0, c.R, c.G, c.B);
                }
            }

            var pixelIndex = 0;
            var data = source.LockBits(new Rectangle(0, 0, bitmapWidth, bitmapHeight), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            unsafe
            {
                var rgbValues = (byte*)data.Scan0.ToPointer();
                var bytesLength = Math.Abs(data.Stride) * bitmapHeight;

                for (var i = 0; i < bytesLength; i += 4 * quality)
                {
                    ref var pixelBlue = ref rgbValues[i];
                    ref var pixelGreen = ref rgbValues[i + 1];
                    ref var pixelRed = ref rgbValues[i + 2];
                    ref var pixelAlpha = ref rgbValues[i + 3];

                    var argb = Color.FromArgb(pixelAlpha, pixelRed, pixelGreen, pixelBlue);
                    var argb1 = Color.FromArgb(0, pixelRed, pixelGreen, pixelBlue);
                    if (transparentIndex > -1 && transparentColor.ToArgb() == argb1.ToArgb())
                    {
                        pixelAlpha = 0;
                        argb = argb1;
                    }

                    if (pixelAlpha < byte.MaxValue)
                    {

                        if (pixelAlpha == 0)
                        {
                            transparentPixelIndex = pixelIndex;
                            transparentColor = argb;
                        }
                        else
                            hasSemiTransparency = true;
                    }
                    var pixel = argb;
                    if (pixel.A > 125
                        && !(pixel.R > 250 && pixel.G > 250 && pixel.B > 250))
                        pixels[pixelIndex++] = argb;
                }
            }

            pixels = pixels.Slice(0, pixelIndex);

            source.UnlockBits(data);
            return true;
        }

        private static Dictionary<Color, int> Quantize_image(Span<Color> pixels, Color[] palette, int nMaxColors, int transparentPixelIndex)
        {
            var dic = new Dictionary<Color, int>();

            if (transparentPixelIndex >= 0 || nMaxColors < 64)
            {
                var nearestMap = new Dictionary<Color, ushort>();
                for (int i = 0; i < pixels.Length; ++i)
                {

                    var index = NearestColorIndex(palette, pixels[i], nearestMap, 0);
                    var color = palette[index];
                    if (dic.TryGetValue(color, out var count))
                        count++;
                    else
                        count = 1;
                    dic[color] = count;

                }
            }
            else
            {
                var r = new Random();
                var closestMap = new Dictionary<Color, ushort[]>();
                for (int i = 0; i < pixels.Length; ++i)
                {

                    var index = ClosestColorIndex(palette, pixels[i], closestMap, r);
                    var color = palette[index];
                    if (dic.TryGetValue(color, out var count))
                        count++;
                    else
                        count = 1;
                    dic[color] = count;
                }
            }

            return dic;
        }

        private static ushort NearestColorIndex(Color[] palette, Color c, Dictionary<Color, ushort> nearestMap, float alphaThreshold)
        {
            if (nearestMap.TryGetValue(c, out var k))
                return k;


            if (c.A <= alphaThreshold)
                return 0;

            double mindist = 1e100;
            for (int i = 0; i < palette.Length; ++i)
            {
                var c2 = palette[i];
                var curdist = Sqr(c2.A - c.A);
                if (curdist > mindist)
                    continue;

                curdist += Sqr(c2.R - c.R);
                if (curdist > mindist)
                    continue;

                curdist += Sqr(c2.G - c.G);
                if (curdist > mindist)
                    continue;

                curdist += Sqr(c2.B - c.B);
                if (curdist > mindist)
                    continue;

                mindist = curdist;
                k = (ushort)i;
            }
            nearestMap[c] = k;
            return k;
        }
        private static ushort ClosestColorIndex(Color[] palette, Color c, Dictionary<Color, ushort[]> closestMap, Random r)
        {
            ushort k = 0;

            if (!closestMap.TryGetValue(c, out var closest))
            {
                closest = new ushort[5];
                closest[2] = closest[3] = ushort.MaxValue;

                for (; k < palette.Length; ++k)
                {
                    var c2 = palette[k];
                    closest[4] = (ushort)(Math.Abs(c.A - c2.A) + Math.Abs(c.R - c2.R) + Math.Abs(c.G - c2.G) + Math.Abs(c.B - c2.B));
                    if (closest[4] < closest[2])
                    {
                        closest[1] = closest[0];
                        closest[3] = closest[2];
                        closest[0] = (ushort)k;
                        closest[2] = closest[4];
                    }
                    else if (closest[4] < closest[3])
                    {
                        closest[1] = (ushort)k;
                        closest[3] = closest[4];
                    }
                }

                if (closest[3] == ushort.MaxValue)
                    closest[2] = 0;
            }

            if (closest[2] == 0 || (r.Next(short.MaxValue) % (closest[3] + closest[2])) <= closest[3])
                k = closest[0];
            else
                k = closest[1];

            closestMap[c] = closest;
            return k;
        }


        public static Dictionary<Color, int> QuantizeImage(Bitmap source, int nMaxColors, int quality)
        {
            var bitmapWidth = source.Width;
            var bitmapHeight = source.Height;

            var transparentColor = Color.Transparent;

            var rented = ArrayPool<Color>.Shared.Rent(bitmapWidth * bitmapHeight);
            var pixels = rented.AsSpan(0, bitmapWidth * bitmapHeight);
            var result = GrabPixels(source, ref pixels, out var hasSemiTransparency, out var transparentPixelIndex, ref transparentColor, quality);


            ArrayPool<Color>.Shared.Return(rented);

            var pallet = Pnnquan(pixels, nMaxColors, 1, hasSemiTransparency, transparentPixelIndex, transparentColor);
            return
            Quantize_image(pixels, pallet, nMaxColors, transparentPixelIndex);
        }
    }
}
