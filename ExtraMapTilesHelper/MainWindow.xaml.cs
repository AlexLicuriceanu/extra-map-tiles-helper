using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using CodeWalker.GameFiles;

namespace ExtraMapTilesHelper
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void ProcessYtdFile(string filePath)
        {
            try
            {
                byte[] data = File.ReadAllBytes(filePath);
                YtdFile ytd = new YtdFile();
                RpfFile.LoadResourceFile(ytd, data, 13);

                TextureDictionary dict = ytd.TextureDict;

                if (dict?.Textures != null)
                {
                    foreach (var tex in dict.Textures.data_items)
                    {
                        byte[] ddsData = DDSExtractor.GetDDS(tex);
                        if (ddsData != null)
                        {
                            BitmapSource bmp = ConvertDdsToBitmap(ddsData);
                            if (bmp != null)
                            {
                                // We pass the filename too so you know which YTD it came from
                                AddImageToGallery(bmp, $"{Path.GetFileName(filePath)} -> {tex.Name}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading {filePath}: {ex.Message}");
            }
        }


        private void BtnLoadYtd_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "YTD Files (*.ytd)|*.ytd";
            openFileDialog.Multiselect = true; // Allow multiple files

            if (openFileDialog.ShowDialog() == true)
            {
                StatusText.Text = $"Loading {openFileDialog.FileNames.Length} files...";

                // Optional: Should we clear the gallery every time? 
                // If you want to append new files to old ones, remove the line below.
                TextureGallery.Children.Clear();

                foreach (string filePath in openFileDialog.FileNames)
                {
                    ProcessYtdFile(filePath);
                }

                StatusText.Text = "All files loaded.";
            }
        }

        private void AddImageToGallery(BitmapSource bmp, string name)
        {
            StackPanel card = new StackPanel { Margin = new Thickness(5) };

            Image img = new Image
            {
                Source = bmp,
                Width = 140,
                Height = 140,
                Stretch = Stretch.Uniform
            };

            TextBlock lbl = new TextBlock
            {
                Text = name,
                Foreground = Brushes.White,
                TextAlignment = TextAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                FontSize = 11,
                MaxWidth = 140
            };

            card.Children.Add(img);
            card.Children.Add(lbl);
            TextureGallery.Children.Add(card);
        }

        private BitmapSource ConvertDdsToBitmap(byte[] ddsData)
        {
            try
            {
                using (MemoryStream stream = new MemoryStream(ddsData))
                {
                    // We use the full path to the library to avoid confusion
                    // Structure: global::<Namespace>.<Class>.<Method>
                    using (var image = global::Pfim.Pfimage.FromStream(stream))
                    {
                        PixelFormat format;

                        // We also assume the format enum is inside the Pfim namespace
                        switch (image.Format)
                        {
                            case global::Pfim.ImageFormat.Rgb24:
                                format = PixelFormats.Bgr24;
                                break;
                            case global::Pfim.ImageFormat.Rgba32:
                                format = PixelFormats.Bgra32;
                                break;
                            case global::Pfim.ImageFormat.R5g6b5:
                                format = PixelFormats.Bgr565;
                                break;
                            default:
                                // Fallback for tricky formats
                                format = PixelFormats.Bgra32;
                                break;
                        }

                        return BitmapSource.Create(
                            image.Width,
                            image.Height,
                            96.0, 96.0,
                            format,
                            null,
                            image.Data,
                            image.Stride);
                    }
                }
            }
            catch (Exception ex)
            {
                // If it fails, print the error to the debug console so we know why
                System.Diagnostics.Debug.WriteLine("PFIM ERROR: " + ex.Message);
                return null;
            }
        }
    }
}