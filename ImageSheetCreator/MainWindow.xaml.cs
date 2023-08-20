using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows;

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

        public string ImagePath
        {
            get
            {
                return _targetImagePath;
            }
            set
            {
                try
                {
                    Image.FromFile(value);
                }
                catch (Exception)
                {
                    return;
                }
                _targetImagePath = value;
            }
        }

        public ObservableCollection<ImageData> Images { get; set; }
        public bool IsEnabledAddImageButton { get => !string.IsNullOrEmpty(ImagePath); }
        public bool IsEnabledRemoveImageButton { get => imageList.SelectedIndex != -1; }
        public bool IsEnabledCreateImageSheetButton { get => Images.Any(); }
        #endregion

        #region Public constructors
        public MainWindow()
        {
            ImagesInColumn = "1";
            ImagesInRow = "1";
            AspectRatio = (297 / 210.0).ToString();
            ImageLimit = "";
            ImagePath = "";
            Images = new ObservableCollection<ImageData>();

            InitializeComponent();
            DataContext = this;
        }
        #endregion

        #region Commands

        private void SelectImageCommand(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                InitialDirectory = Directory.GetCurrentDirectory()
            };
            if (openFileDialog.ShowDialog() == true)
            {
                ImagePath = openFileDialog.FileName;
            }
            ImagePathTextBox.Text = ImagePath;
            addImageButton.IsEnabled = IsEnabledAddImageButton;
        }

        private void AddImageCommand(object sender, RoutedEventArgs e)
        {
            try
            {
                Image.FromFile(ImagePath);
            }
            catch (Exception)
            {
                return;
            }

            Images.Add(new ImageData(ImagePath, imageLimit));
            createImageSheetButton.IsEnabled = IsEnabledCreateImageSheetButton;
        }

        private void RemoveImageCommand(object sender, RoutedEventArgs e)
        {
            if (imageList.SelectedIndex == -1)
            {
                return;
            }

            Images.RemoveAt(imageList.SelectedIndex);
            createImageSheetButton.IsEnabled = IsEnabledCreateImageSheetButton;
        }

        private void CreateImageSheetCommand(object sender, RoutedEventArgs e)
        {
            if (!Images.Any())
            {
                return;
            }

            if (FlipAspectRatio)
            {
                aspectRatio = 1 / aspectRatio;
            }

            // correct image aspect ratios
            (int width, int height) biggestSize = (0, 0);

            foreach (var targetImage in Images)
            {
                var image = targetImage.Image;

                var fullWidth = image.Width * imagesInRow;
                var fullHeight = image.Height * imagesInColumn;


                var rawAspectRatio = fullHeight / (fullWidth * 1.0);

                (int width, int height) correctedSize = (0, 0);

                if (rawAspectRatio != aspectRatio)
                {
                    if (rawAspectRatio > aspectRatio)
                    {
                        correctedSize = (image.Width, (int)Math.Round(image.Height / (rawAspectRatio / aspectRatio)));
                    }
                    else
                    {
                        correctedSize = ((int)Math.Round(image.Width / (aspectRatio / rawAspectRatio)), image.Height);
                    }
                }

                if (correctedSize.width > biggestSize.width)
                {
                    biggestSize = correctedSize;
                }
            }

            (int width, int height) = (biggestSize.width * imagesInRow, biggestSize.height * imagesInColumn);

            // make img
            var imageIndex = 0;
            var currentImage = Images[imageIndex];

            var destImage = new Bitmap(width, height);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                var wrapMode = new ImageAttributes();
                wrapMode.SetWrapMode(WrapMode.TileFlipXY);

                var imageNum = 0;

                for (int y = 0; y < imagesInColumn; y++)
                {
                    for (int x = 0; x < imagesInRow; x++)
                    {
                        var destRect = new Rectangle(x * biggestSize.width, y * biggestSize.height, biggestSize.width, biggestSize.height);
                        graphics.DrawImage(currentImage.Image, destRect, 0, 0, currentImage.Image.Width, currentImage.Image.Height, GraphicsUnit.Pixel, wrapMode);
                        if (currentImage.Limit > 0)
                        {
                            imageNum++;
                            if (imageNum >= currentImage.Limit)
                            {
                                imageIndex++;
                                if (imageIndex >= Images.Count)
                                {
                                    break;
                                }
                                else
                                {
                                    imageNum = 0;
                                    currentImage = Images[imageIndex];
                                }
                            }
                        }
                    }
                    if (imageIndex >= Images.Count)
                    {
                        break;
                    }
                }
            }
            destImage.Save("címke kép.png", ImageFormat.Png);
        }
        #endregion

        #region Private methods
        private void imageList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            removeImageButton.IsEnabled = IsEnabledRemoveImageButton;
        }
        #endregion
    }
}
