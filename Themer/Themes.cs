using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

namespace Themer
{

    /// <summary>
    /// This is a list of forground and background color pars generated from an image.
    /// </summary>
    public class Themes : IEnumerable<Swatch>
    {

        const float TARGET_DARK_LUMA = 0.26f;
        const float MAX_DARK_LUMA = 0.45f;
        const float MIN_LIGHT_LUMA = 0.55f;
        const float TARGET_LIGHT_LUMA = 0.74f;

        const float MIN_NORMAL_LUMA = 0.3f;
        const float TARGET_NORMAL_LUMA = 0.5f;
        const float MAX_NORMAL_LUMA = 0.7f;

        const float TARGET_MUTED_SATURATION = 0.3f;
        const float MAX_MUTED_SATURATION = 0.4f;

        const float TARGET_VIBRANT_SATURATION = 1f;
        const float MIN_VIBRANT_SATURATION = 0.35f;

        const int WEIGHT_SATURATION = 3;
        const int WEIGHT_LUMA = 6;
        const int WEIGHT_POPULATION = 1;

        private Swatch[] _swatches = Array.Empty<Swatch>();
        private readonly Dictionary<SwatchType, Swatch> lookup;
        public Swatch? VibrantSwatch { get; private set; }
        public Swatch? MutedSwatch { get; private set; }
        public Swatch? DarkVibrantSwatch { get; private set; }
        public Swatch? DarkMutedSwatch { get; private set; }
        public Swatch? LightVibrantSwatch { get; private set; }
        public Swatch? LightMutedSwatch { get; private set; }

        public Swatch this[int index]
        {
            get { return this._swatches[index]; }
        }
        public int Count => this._swatches.Length;

        public Swatch? this[SwatchType index]
        {
            get
            {
                if (this.lookup.TryGetValue(index, out var result))
                    return result;
                return null;
            }

        }

        private readonly int highestPopulation = 0;


        /// <summary>
        /// Calculates color pairs for the image
        /// </summary>
        /// <param name="sourceImage">The image to analize.</param>
        /// <param name="colorCount">The number of colors the image will be reduced.</param>
        /// <param name="quality">The quality. One will analize every pixel, 3 will analizer every thrid pixel.</param>
        /// <returns>A Themes object that is a list of color pairs.</returns>
        public static async Task<Themes> Calculate(Image sourceImage, int colorCount = 64, int quality = 5)
        {
            if (quality < 1)
                throw new ArgumentOutOfRangeException(nameof(quality), quality, "Minimum (and best) quality is 1.");

            using var bitmap = new Bitmap(sourceImage);
            return await Task.Run(() =>
            {
                var counter = Quantizer.QuantizeImage(bitmap, colorCount, quality);
                return new Themes(counter);
            });
        }

        private Themes(Dictionary<Color, int> counter)
        {
            this._swatches = counter.Select(pair => new Swatch(pair.Key, pair.Value)).ToArray();

            this.highestPopulation = this.maxfindMaxPopulation();
            this.GenerateVariationColors();
            this.GenerateEmptySwatches();

            this._swatches = new[] {
            this.VibrantSwatch!,
            this.DarkVibrantSwatch!,
            this.LightVibrantSwatch!,
            this.MutedSwatch!,
            this.DarkMutedSwatch!,
            this.LightMutedSwatch!,
            }.Where(x => x is not null).OrderByDescending(x => x.Population).ToArray();

            this.lookup = this._swatches.ToDictionary(x => x.SwatchType);
        }
        private static double GetContrast(Swatch x1, Swatch x2)
        {
            return GetContrast(x1.Rgb, x2.Rgb);
        }
        private static double GetContrast(Color x1, Color x2)
        {
            double luminance(System.Drawing.Color c)
            {
                return (t(c.R) * 0.2126 + t(c.G) * 0.7152 + t(c.B) * 0.0722);
            }
            double t(byte b)
            {
                var f = b / 255.0;
                return f < 0.03928
                    ? f / 12.92
                    : Math.Pow((f + 0.055) / 1.055, 2.4);
            }

            var min = Math.Min(luminance(x1), luminance(x2));
            var max = Math.Max(luminance(x1), luminance(x2));
            return (max + 0.05) / (min + 0.05);
        }
        /// <summary>
        /// Returs a list of color pairs. beginning with the most prominent color.
        /// </summary>
        /// <param name="minimumContrast">The minimum contrast text forground needs to background</param>
        /// <returns></returns>
        public (System.Drawing.Color forground, System.Drawing.Color Background, SwatchType ForgroundType, SwatchType BackgroundType, int populus, double contrast)[] GetColorPairs(double minimumContrast = 3)
        {
            var list = new List<(System.Drawing.Color forground, System.Drawing.Color Background, SwatchType ForgroundType, SwatchType BackgroundType, int populus, double contrast)>();

            for (int j = 0; j < this.Count; j++)
            {
                var current = this[j];

                if (current.SwatchType == SwatchType.Vibrant || current.SwatchType == SwatchType.DarkVibrant || current.SwatchType == SwatchType.LightVibrant)
                {

                    for (int i = 0; i < this.Count; i++)
                    {
                        var other = this[i];
                        if (other.SwatchType == SwatchType.Vibrant || other.SwatchType == SwatchType.DarkVibrant || other.SwatchType == SwatchType.LightVibrant)
                        {
                            var contrast = GetContrast(current, other);
                            if (contrast >= minimumContrast)
                            {
                                list.Add((other.Rgb, current.Rgb, other.SwatchType, current.SwatchType, other.Population + current.Population * 2, contrast));
                            }
                        }
                    }

                }
                else if (current.SwatchType == SwatchType.Muted || current.SwatchType == SwatchType.DarkMuted || current.SwatchType == SwatchType.LightMuted)
                {
                    for (int i = 0; i < this.Count; i++)
                    {
                        var other = this[i];
                        if (other.SwatchType == SwatchType.Muted || other.SwatchType == SwatchType.DarkMuted || other.SwatchType == SwatchType.LightMuted)
                        {
                            var contrast = GetContrast(current, other);
                            if (contrast >= minimumContrast)
                            {
                                list.Add((other.Rgb, current.Rgb, other.SwatchType, current.SwatchType, other.Population + current.Population * 2, contrast));
                            }
                        }
                    }

                }

            }

            for (int j = 0; j < this.Count; j++)
            {
                var current = this[j];

                list.Add((current.BodyTextColor, current.Rgb, SwatchType.Undefined, current.SwatchType, current.Population , GetContrast(current.BodyTextColor, current.Rgb)));
            }
            return list.OrderByDescending(x => x.populus).ThenByDescending(x => x.contrast).ToArray();
        }

        private void GenerateVariationColors()
        {
            this.@VibrantSwatch = this.FindColorVariation(@TARGET_NORMAL_LUMA, @MIN_NORMAL_LUMA, @MAX_NORMAL_LUMA,
                @TARGET_VIBRANT_SATURATION, @MIN_VIBRANT_SATURATION, 1, SwatchType.Vibrant);

            this.@LightVibrantSwatch = this.FindColorVariation(@TARGET_LIGHT_LUMA, @MIN_LIGHT_LUMA, 1,
              @TARGET_VIBRANT_SATURATION, @MIN_VIBRANT_SATURATION, 1, SwatchType.LightVibrant);

            this.@DarkVibrantSwatch = this.FindColorVariation(@TARGET_DARK_LUMA, 0, @MAX_DARK_LUMA,
              @TARGET_VIBRANT_SATURATION, @MIN_VIBRANT_SATURATION, 1, SwatchType.DarkVibrant);

            this.@MutedSwatch = this.FindColorVariation(@TARGET_NORMAL_LUMA, @MIN_NORMAL_LUMA, @MAX_NORMAL_LUMA,
              @TARGET_MUTED_SATURATION, 0, @MAX_MUTED_SATURATION, SwatchType.Muted);

            this.@LightMutedSwatch = this.FindColorVariation(@TARGET_LIGHT_LUMA, @MIN_LIGHT_LUMA, 1,
              @TARGET_MUTED_SATURATION, 0, @MAX_MUTED_SATURATION, SwatchType.LightMuted);

            this.@DarkMutedSwatch = this.FindColorVariation(@TARGET_DARK_LUMA, 0, @MAX_DARK_LUMA,
              @TARGET_MUTED_SATURATION, 0, @MAX_MUTED_SATURATION, SwatchType.DarkMuted);
        }

        private void GenerateEmptySwatches()
        {
            if (this.VibrantSwatch is null && this.DarkVibrantSwatch is not null)
            {
                var hsl = this.DarkVibrantSwatch.Hsl;
                hsl.Luminance = TARGET_NORMAL_LUMA;
                this.VibrantSwatch = new Swatch(Hsl.HslToRgb(hsl), 0, SwatchType.Vibrant);
            }

            if (this.VibrantSwatch is null && this.LightVibrantSwatch is not null)
            {
                var hsl = this.LightVibrantSwatch.Hsl;
                hsl.Luminance = TARGET_NORMAL_LUMA;
                this.VibrantSwatch = new Swatch(Hsl.HslToRgb(hsl), 0, SwatchType.Vibrant);
            }

            if (this.LightVibrantSwatch is null && this.VibrantSwatch is not null)
            {
                var hsl = this.VibrantSwatch.Hsl;
                hsl.Luminance = TARGET_LIGHT_LUMA;
                this.LightVibrantSwatch = new Swatch(Hsl.HslToRgb(hsl), 0, SwatchType.LightVibrant);
            }

            if (this.DarkVibrantSwatch is null && this.VibrantSwatch is not null)
            {
                var hsl = this.VibrantSwatch.Hsl;
                hsl.Luminance = TARGET_DARK_LUMA;
                this.DarkVibrantSwatch = new Swatch(Hsl.HslToRgb(hsl), 0, SwatchType.DarkVibrant);
            }


            if (this.MutedSwatch is null && this.DarkMutedSwatch is not null)
            {
                var hsl = this.DarkMutedSwatch.Hsl;
                hsl.Luminance = TARGET_NORMAL_LUMA;
                this.MutedSwatch = new Swatch(Hsl.HslToRgb(hsl), 0, SwatchType.Muted);
            }

            if (this.MutedSwatch is null && this.LightMutedSwatch is not null)
            {
                var hsl = this.LightMutedSwatch.Hsl;
                hsl.Luminance = TARGET_NORMAL_LUMA;
                this.MutedSwatch = new Swatch(Hsl.HslToRgb(hsl), 0, SwatchType.Muted);
            }

            if (this.LightMutedSwatch is null && this.MutedSwatch is not null)
            {
                var hsl = this.MutedSwatch.Hsl;
                hsl.Luminance = TARGET_LIGHT_LUMA;
                this.LightMutedSwatch = new Swatch(Hsl.HslToRgb(hsl), 0, SwatchType.LightMuted);
            }

            if (this.DarkMutedSwatch is null && this.MutedSwatch is not null)
            {
                var hsl = this.MutedSwatch.Hsl;
                hsl.Luminance = TARGET_DARK_LUMA;
                this.DarkMutedSwatch = new Swatch(Hsl.HslToRgb(hsl), 0, SwatchType.DarkMuted);
            }

        }

        private int maxfindMaxPopulation()
        {
            return this._swatches.Select(x => x.Population).Max();
        }



        private Swatch? FindColorVariation(float targetLuma, float minLuma, float maxLuma, float targetSaturation, float minSaturation, float maxSaturation, SwatchType swatchType)
        {
            Swatch? max = null;
            float maxValue = 0;

            foreach (var swatch in this._swatches)
            {
                var sat = swatch.Hsl.Saturation;
                var luma = swatch.Hsl.Luminance;

                if (sat >= minSaturation && sat <= maxSaturation
                    && luma >= minLuma && luma <= maxLuma
                    && !this.IsAlreadySelected(swatch))
                {
                    var value = CreateComparisionValue(sat, targetSaturation, luma, targetLuma, swatch.Population, this.highestPopulation);
                    if (max is null || value > maxValue)
                    {
                        max = swatch;
                        maxValue = value;
                    }
                }
            }
            if (max is null)
                return null;
            return max with { SwatchType = swatchType };
        }

        private static float CreateComparisionValue(float saturation, float targetSaturation, float luma, float targetLuma, int population, int maxPopulation)
        {

            return WeightedMean(
                (InvertDiff(saturation, targetSaturation), WEIGHT_SATURATION),
                (InvertDiff(luma, targetLuma), WEIGHT_LUMA),
                (population / maxPopulation, WEIGHT_POPULATION));

            static float InvertDiff(float value, float targetValue) => 1 - Math.Abs(value - targetValue);

            static float WeightedMean(params (float value, int wight)[] values)
            {
                float sum = 0;
                float sumWeight = 0;

                foreach (var (value, weight) in values)
                {
                    sum += value * weight;
                    sumWeight += weight;
                }
                return sum / sumWeight;
            }
        }

        private bool IsAlreadySelected(Swatch swatch)
        {
            return swatch == this.VibrantSwatch
                || swatch == this.DarkVibrantSwatch
                || swatch == this.LightVibrantSwatch
                || swatch == this.MutedSwatch
                || swatch == this.DarkMutedSwatch
                || swatch == this.LightMutedSwatch;
        }





        public IEnumerator<Swatch> GetEnumerator()
        {
            return ((IEnumerable<Swatch>)this._swatches).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this._swatches.GetEnumerator();
        }
    }

}
