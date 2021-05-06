using System.Drawing;

namespace Themer
{


    public record Swatch(Color Rgb, int Population, SwatchType SwatchType = SwatchType.Undefined)
    {
        private Hsl? hsl;
        private int? yiq = null;



        internal Hsl Hsl
        {
            get
            {
                if (this.hsl is null)
                    this.hsl = Hsl.RgbToHsl(this.Rgb);
                return this.hsl.Value;
            }
        }



        public Color TitleTextColor
        {
            get
            {
                this.EnsureTextColors();
                if (this.yiq < 200)
                    return Color.White;
                return Color.Black;
            }
        }


        public Color BodyTextColor
        {
            get
            {
                this.EnsureTextColors();
                if (this.yiq < 150)
                    return Color.White;
                return Color.Black;
            }
        }

        private void EnsureTextColors()
        {
            if (this.yiq is null)
                this.yiq = (this.Rgb.R * 299 + this.Rgb.G * 587 + this.Rgb.B * 114) / 1000;

        }
    }
}
