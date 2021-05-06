using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Themer;

namespace Sample
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            this.loading.IsIndeterminate = true;
            this.loading.Visibility = Visibility.Visible;
            try
            {
                var dialog = new OpenFileDialog()
                {
                    Filter = "Images|*.png;*.jpg;*.jepg|All|*.*"
                };
                var result = dialog.ShowDialog();
                if (result ?? false)
                {
                    var file = dialog.FileName;
                    using var image = System.Drawing.Image.FromFile(file);
                    this.image.Source = this.From(image);

                    
                    var vibrant = await Themer.Themes.Calculate(image);

                    this.VibrantR.Fill = ToBrush(vibrant.VibrantSwatch?.Rgb);
                    this.DarkVibrant.Fill = ToBrush(vibrant.DarkVibrantSwatch?.Rgb);
                    this.LightVibrant.Fill = ToBrush(vibrant.LightVibrantSwatch?.Rgb);
                    this.Muted.Fill = ToBrush(vibrant.MutedSwatch?.Rgb);
                    this.DarkMuted.Fill = ToBrush(vibrant.DarkMutedSwatch?.Rgb);
                    this.LightMuted.Fill = ToBrush(vibrant.LightMutedSwatch?.Rgb);



                    var list = vibrant.GetColorPairs();

                    var (forground, background, forgroundType, backgroundType, populus, contrast) = list.First();

                    this.List.ItemsSource = list.Select(x => new
                    {
                        Forground = ToBrush(x.forground),
                        Background = ToBrush(x.Background),
                        Populus = x.populus,
                        Contrast = x.contrast,
                        ForgroundType = x.ForgroundType,
                        BackgroundType = x.BackgroundType,
                    }).ToArray();
                    this.List.SelectedIndex = 0;
                }

            }
            finally
            {
                this.loading.IsIndeterminate = false;
                this.loading.Visibility = Visibility.Collapsed;
            }
        }



        ImageSource From(System.Drawing.Image image)
        {
            using (var ms = new MemoryStream())
            {
                image.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
                ms.Seek(0, SeekOrigin.Begin);

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = ms;
                bitmapImage.EndInit();

                return bitmapImage;
            }
        }

        private static SolidColorBrush? ToBrush(System.Drawing.Color? rgb)
        {
            if (!rgb.HasValue)
                return null;
            var solidColorBrush = new SolidColorBrush(Color.FromRgb(rgb.Value.R, rgb.Value.G, rgb.Value.B));
            solidColorBrush.Freeze();
            return solidColorBrush;
        }
    }
}
