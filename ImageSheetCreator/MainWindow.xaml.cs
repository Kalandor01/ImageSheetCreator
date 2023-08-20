using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Drawing;
using System;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Media.Media3D;

namespace ImageSheetCreator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Private fields
        private string _imagesInColumnString;
        private string _imagesInRowString;
        private string _aspectRatioString;
        private string _imageLimitString;
        private string _targetImagePath;

        private int imagesInColumn;
        private int imagesInRow;
        private double aspectRatio;
        private int imageLimit;
        private Image? targetImage;
        #endregion

        #region Public properies
        public string ImagesInColumn
        {
            get
            {
                return _imagesInColumnString;
            }
            set
            {
                _imagesInColumnString = int.TryParse(value, out int parsedValue) && parsedValue > 0 ? parsedValue.ToString() : _imagesInColumnString;
                imagesInColumn = int.Parse(_imagesInColumnString);
            }
        }

        public string ImagesInRow
        {
            get
            {
                return _imagesInRowString;
            }
            set
            {
                _imagesInRowString = int.TryParse(value, out int parsedValue) && parsedValue > 0 ? parsedValue.ToString() : _imagesInRowString;
                imagesInRow = int.Parse(_imagesInRowString);
            }
        }

        public string AspectRatio
        {
            get
            {
                return _aspectRatioString;
            }
            set
            {
                _aspectRatioString = double.TryParse(value, out double parsedValue) && parsedValue > 0 ? parsedValue.ToString() : _aspectRatioString;
                aspectRatio = double.Parse(_aspectRatioString);
            }
        }

        public bool FlipAspectRatio { get; set; }

        public string ImageLimit
        {
            get
            {
                return _imageLimitString;
            }
            set
            {
                _imageLimitString = string.IsNullOrWhiteSpace(value) || (int.TryParse(value, out int parsedValue) && parsedValue > 0) ? value : _imageLimitString;
                imageLimit = string.IsNullOrWhiteSpace(_imageLimitString) ? -1 : int.Parse(_imageLimitString);
            }
        }

        public string TargetImagePath
        {
            get
            {
                return _targetImagePath;
            }
            set
            {
                try
                {
                    targetImage = Image.FromFile(value);
                }
                catch (Exception)
                {
                    return;
                }
                _targetImagePath = value;
            }
        }
        #endregion

        #region Public constructors
        public MainWindow()
        {
            ImagesInColumn = "1";
            ImagesInRow = "1";
            AspectRatio = (297 / 210.0).ToString();
            ImageLimit = "";
            TargetImagePath = "";
            targetImage = null;

            InitializeComponent();
            DataContext = this;
        }
        #endregion

        #region Public methods
        private void SelectTargetImage(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                InitialDirectory = Directory.GetCurrentDirectory()
            };
            if (openFileDialog.ShowDialog() == true)
            {
                TargetImagePath = openFileDialog.FileName;
            }
            targetImageTextBox.Text = TargetImagePath;
        }

        private void CreateImageSheetCommand(object sender, RoutedEventArgs e)
        {
            if (targetImage is null)
            {
                return;
            }

            if (FlipAspectRatio)
            {
                aspectRatio = 1 / aspectRatio;
            }

            // calculate sizes    
            var imageSize = targetImage.Size;

            var fullWidth = imageSize.Width * imagesInRow;
            var fullHeight = imageSize.Height * imagesInColumn;


            var rawAspectRatio = fullHeight / (fullWidth * 1.0);

            (int width, int height) finalSize = (1, 1);

            if (rawAspectRatio != aspectRatio)
            {
                if (rawAspectRatio > aspectRatio)
                {
                    finalSize = (imageSize.Width, (int)Math.Round(imageSize.Height / (rawAspectRatio / aspectRatio)));
                }
                else
                {
                    finalSize = ((int)Math.Round(imageSize.Width / (aspectRatio / rawAspectRatio)), imageSize.Height);
                }

                fullWidth = finalSize.width * imagesInRow;
                fullHeight = finalSize.height * imagesInColumn;
            }

            // make img
            var destImage = new Bitmap(fullWidth, fullHeight);

            destImage.SetResolution(targetImage.HorizontalResolution, targetImage.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                var imageNum = 0;
                var wrapMode = new ImageAttributes();
                wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                for (int y = 0; y < imagesInColumn; y++)
                {
                    for (int x = 0; x < imagesInRow; x++)
                    {
                        var destRect = new Rectangle(x * finalSize.width, y * finalSize.height, finalSize.width, finalSize.height);
                        graphics.DrawImage(targetImage, destRect, 0, 0, targetImage.Width, targetImage.Height, GraphicsUnit.Pixel, wrapMode);
                        if (imageLimit > 0)
                        {
                            imageNum++;
                            if (imageNum >= imageLimit)
                            {
                                break;
                            }
                        }
                    }
                    if (imageLimit > 0 && imageNum >= imageLimit)
                    {
                        break;
                    }
                }
            }
            destImage.Save("tiled_image.png");
        }
        #endregion
    }
}
