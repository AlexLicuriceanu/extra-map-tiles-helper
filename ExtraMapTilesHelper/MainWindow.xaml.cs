using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input; // Needed for MouseButtonEventArgs
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

        // ==========================================
        // 1. CUSTOM TITLE BAR & WINDOW LOGIC
        // ==========================================

        // 1. DRAG LOGIC (Fixes the crash)
        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                // Allow double-click to maximize
                if (e.ClickCount == 2)
                {
                    if (this.WindowState == WindowState.Maximized)
                        SystemCommands.RestoreWindow(this);
                    else
                        SystemCommands.MaximizeWindow(this);
                }
                else
                {
                    this.DragMove();
                }
            }
        }

        // 2. MINIMIZE (Triggers "Zoom down" animation)
        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            SystemCommands.MinimizeWindow(this);
        }

        // 3. MAXIMIZE (Triggers "Zoom up" animation)
        private void BtnMaximize_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
                SystemCommands.RestoreWindow(this);
            else
                SystemCommands.MaximizeWindow(this);
        }

        // 4. CLOSE
        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            SystemCommands.CloseWindow(this);
        }

        // ==========================================
        // 2. DRAG & DROP LOGIC (The missing link)
        // ==========================================

        // When you drag FROM the TreeView
        private void TextureNode_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (sender is TreeViewItem item && item.Tag is BitmapSource bmp)
                {
                    // Create data package
                    DataObject data = new DataObject();
                    data.SetData("Object", bmp);

                    // Get name from the header textblock
                    string name = "Unknown";
                    if (item.Header is StackPanel panel && panel.Children.Count > 1 && panel.Children[1] is TextBlock tb)
                    {
                        name = tb.Text;
                    }
                    data.SetData("Name", name);

                    // Start Drag
                    DragDrop.DoDragDrop(item, data, DragDropEffects.Copy);
                }
            }
        }

        // When you drop ONTO the Canvas
        private void MapCanvas_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("Object"))
            {
                BitmapSource bmp = e.Data.GetData("Object") as BitmapSource;
                string name = e.Data.GetData("Name") as string;

                // 1. Create the Image Control
                Image newTile = new Image
                {
                    Source = bmp,
                    Width = bmp.PixelWidth,
                    Height = bmp.PixelHeight,
                    Stretch = Stretch.Fill,
                    Tag = name // Store name for properties
                };

                // 2. Position it where mouse was dropped
                Point dropPoint = e.GetPosition(MapCanvas);
                Canvas.SetLeft(newTile, dropPoint.X - (bmp.PixelWidth / 2));
                Canvas.SetTop(newTile, dropPoint.Y - (bmp.PixelHeight / 2));

                // 3. Add to Canvas
                MapCanvas.Children.Add(newTile);

                // 4. Select it immediately
                PropName.Text = name;
                PropX.Text = Canvas.GetLeft(newTile).ToString("F0");
                PropY.Text = Canvas.GetTop(newTile).ToString("F0");
            }
        }

        // ==========================================
        // 3. FILE LOADING LOGIC
        // ==========================================

        private void BtnLoadYtd_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "YTD Files (*.ytd)|*.ytd";
            openFileDialog.Multiselect = true;

            if (openFileDialog.ShowDialog() == true)
            {
                foreach (string file in openFileDialog.FileNames)
                {
                    ProcessYtdFile(file);
                }
            }
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (string file in files)
                {
                    if (Path.GetExtension(file).ToLower() == ".ytd")
                    {
                        ProcessYtdFile(file);
                    }
                }
            }
        }

        private void ProcessYtdFile(string filePath)
        {
            try
            {
                TreeViewItem ytdNode = new TreeViewItem();
                ytdNode.Header = Path.GetFileName(filePath);
                ytdNode.FontWeight = FontWeights.Bold;
                ytdNode.Foreground = Brushes.White;
                ytdNode.IsExpanded = true;

                AssetTree.Items.Add(ytdNode);

                byte[] data = File.ReadAllBytes(filePath);
                YtdFile ytd = new YtdFile();
                RpfFile.LoadResourceFile(ytd, data, 13);
                TextureDictionary dict = ytd.TextureDict;

                if (dict != null && dict.Textures != null)
                {
                    foreach (var tex in dict.Textures.data_items)
                    {
                        byte[] ddsData = DDSExtractor.GetDDS(tex);
                        if (ddsData != null)
                        {
                            BitmapSource bmp = ConvertDdsToBitmap(ddsData);
                            if (bmp != null)
                            {
                                AddTextureToNode(ytdNode, bmp, tex.Name);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading {filePath}:\n{ex.Message}");
            }
        }

        private void AddTextureToNode(TreeViewItem parentNode, BitmapSource bmp, string name)
        {
            StackPanel panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(2) };

            Image icon = new Image
            {
                Source = bmp,
                Width = 20,
                Height = 20,
                Margin = new Thickness(0, 0, 8, 0)
            };

            TextBlock lbl = new TextBlock
            {
                Text = name,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Brushes.LightGray
            };

            panel.Children.Add(icon);
            panel.Children.Add(lbl);

            TreeViewItem textureNode = new TreeViewItem();
            textureNode.Header = panel;
            textureNode.Tag = bmp;

            // CHANGE: Use 'PreviewMouseMove' instead of just 'MouseMove'
            textureNode.PreviewMouseMove += TextureNode_PreviewMouseMove;

            parentNode.Items.Add(textureNode);
        }

        // CHANGE: The event signature now matches PreviewMouseMove
        private void TextureNode_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            // Only drag if the left button is held down
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                // Safety check: make sure sender is a TreeViewItem
                if (sender is TreeViewItem item && item.Tag is BitmapSource bmp)
                {
                    // Create data package
                    DataObject data = new DataObject();
                    data.SetData("Object", bmp);

                    // Get name safely
                    string name = "Unknown";
                    if (item.Header is StackPanel panel && panel.Children.Count > 1 && panel.Children[1] is TextBlock tb)
                    {
                        name = tb.Text;
                    }
                    data.SetData("Name", name);

                    // START DRAG
                    // This enables the "Ghost" cursor effect
                    DragDrop.DoDragDrop(item, data, DragDropEffects.Copy);

                    // Mark event as handled so the TreeView doesn't get confused
                    e.Handled = true;
                }
            }
        }

        private BitmapSource ConvertDdsToBitmap(byte[] ddsData)
        {
            try
            {
                using (MemoryStream stream = new MemoryStream(ddsData))
                {
                    using (var image = global::Pfim.Pfimage.FromStream(stream))
                    {
                        PixelFormat format;
                        switch (image.Format)
                        {
                            case global::Pfim.ImageFormat.Rgb24: format = PixelFormats.Bgr24; break;
                            case global::Pfim.ImageFormat.Rgba32: format = PixelFormats.Bgra32; break;
                            case global::Pfim.ImageFormat.R5g6b5: format = PixelFormats.Bgr565; break;
                            default: format = PixelFormats.Bgra32; break;
                        }

                        return BitmapSource.Create(image.Width, image.Height, 96.0, 96.0, format, null, image.Data, image.Stride);
                    }
                }
            }
            catch { return null; }
        }


        // ==========================================
        // 4. ZOOM LOGIC
        // ==========================================

        private void MapScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            // Only zoom if CTRL key is held down (Standard behavior)
            // If you want it to ALWAYS zoom without CTRL, remove this 'if' check.
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                // Prevent the ScrollViewer from scrolling normally
                e.Handled = true;

                // 1. Get the current mouse position relative to the content
                Point mousePos = e.GetPosition(MapGrid);

                // 2. Calculate new scale
                double zoomFactor = e.Delta > 0 ? 1.1 : 0.9; // Zoom in (1.1x) or out (0.9x)
                double newScaleX = MapScale.ScaleX * zoomFactor;
                double newScaleY = MapScale.ScaleY * zoomFactor;

                // Limit zoom limits (e.g., 0.1x to 5.0x)
                if (newScaleX < 0.1 || newScaleX > 5.0) return;

                // 3. Apply the zoom
                MapScale.ScaleX = newScaleX;
                MapScale.ScaleY = newScaleY;

                // 4. Adjust ScrollViewer to keep mouse over the same point
                // We calculate where the mouse point *would* be after the zoom
                double newMouseX = mousePos.X * zoomFactor;
                double newMouseY = mousePos.Y * zoomFactor;

                // The difference is how much we need to shift the scrollbars
                double offsetX = newMouseX - mousePos.X;
                double offsetY = newMouseY - mousePos.Y;

                MapScrollViewer.ScrollToHorizontalOffset(MapScrollViewer.HorizontalOffset + offsetX);
                MapScrollViewer.ScrollToVerticalOffset(MapScrollViewer.VerticalOffset + offsetY);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // 1. Set Initial Zoom (Optional, 1.0 is default)
            MapScale.ScaleX = 1.0;
            MapScale.ScaleY = 1.0;

            // 2. Calculate Center Position
            // (Map Width / 2) - (Viewport Width / 2)
            double centerX = (MapGrid.Width / 2) - (MapScrollViewer.ActualWidth / 2);
            double centerY = (MapGrid.Height / 2) - (MapScrollViewer.ActualHeight / 2);

            // 3. Scroll to that position
            MapScrollViewer.ScrollToHorizontalOffset(centerX);
            MapScrollViewer.ScrollToVerticalOffset(centerY);
        }
    }
}