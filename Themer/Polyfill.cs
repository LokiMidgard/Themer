#if !NET5_0_OR_GREATER
using System;
using System.Drawing;

namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}
#endif

#if NETSTANDARD2_0
namespace Themer
{
    internal static class Math
    {
        internal static double Sqrt(double d)
        {
            return System.Math.Sqrt(d);
        }

        internal static double Cbrt(double x)
        {
            if (x == 0) return 0;
            if (x < 0) return -Math.Cbrt(-x);

            var r = x;
            var ex = 0;

            while (r < 0.125) { r *= 8; ex--; }
            while (r > 1.0) { r *= 0.125; ex++; }

            r = (-0.46946116 * r + 1.072302) * r + 0.3812513;

            while (ex < 0) { r *= 0.5; ex++; }
            while (ex > 0) { r *= 2; ex--; }

            r = (2.0 / 3.0) * r + (1.0 / 3.0) * x / (r * r);
            r = (2.0 / 3.0) * r + (1.0 / 3.0) * x / (r * r);
            r = (2.0 / 3.0) * r + (1.0 / 3.0) * x / (r * r);
            r = (2.0 / 3.0) * r + (1.0 / 3.0) * x / (r * r);

            return r;
        }

        internal static float Min(float val1, float val2)
        {
            return System.Math.Min(val1, val2);
        }

        internal static float Max(float val1, float val2)
        {
            return System.Math.Max(val1, val2);
        }
        internal static double Min(double val1, double val2)
        {
            return System.Math.Min(val1, val2);
        }

        internal static double Max(double val1, double val2)
        {
            return System.Math.Max(val1, val2);
        }

        internal static int Clamp(int value, int minValue, int maxValue)
        {
            return System.Math.Min(maxValue, System.Math.Max(minValue, value));
        }

        internal static int Abs(int value)
        {
            return System.Math.Abs(value);
        }
        internal static float Abs(float value)
        {
            return System.Math.Abs(value);
        }

        internal static double Pow(double v1, double v2)
        {
            return System.Math.Pow(v1, v2);
        }
    }
}
#endif