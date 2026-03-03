using CodeWalker.GameFiles;
using CodeWalker.Utils; // Or your local namespace if you copied DDSIO.cs
using Pfim;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;

namespace ExtraMapTilesHelper.backend
{
    public class CodeWalkerService
    {
        public record TextureInfo(string Name, int Width, int Height, string Preview);
        public record YtdResult(string DictionaryName, List<TextureInfo> Textures);

        public YtdResult ExtractYtd(string filePath)
        {
            var dictName = Path.GetFileNameWithoutExtension(filePath);
            var textures = new List<TextureInfo>();

            byte[] fileData = File.ReadAllBytes(filePath);
            var ytd = new YtdFile();

            try
            {
                // YOUR WORKING LOGIC: This handles headers, compression, and dummy entries automatically
                RpfFile.LoadResourceFile(ytd, fileData, 13);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CodeWalker YTD Load Error: {ex.Message}");
                return new YtdResult(dictName, textures);
            }

            // Extract Textures using your custom GetDDS method
            var items = ytd.TextureDict?.Textures?.data_items;
            if (items == null) return new YtdResult(dictName, textures);

            foreach (var tex in items)
            {
                try
                {
                    byte[] ddsBytes = GetDDS(tex);
                    if (ddsBytes != null)
                    {
                        string preview = ConvertDdsToPngBase64(ddsBytes);
                        textures.Add(new TextureInfo(tex.Name, tex.Width, tex.Height, preview));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Skipping {tex.Name}: {ex.Message}");
                }
            }

            return new YtdResult(dictName, textures);
        }

        // --- YOUR CUSTOM DDS EXTRACTOR ---
        private static byte[] GetDDS(Texture tex)
        {
            if (tex == null) return null;

            // Get the raw bytes of the first (biggest) mipmap level
            byte[] textureData = tex.Data?.FullData;
            if (textureData == null || textureData.Length == 0) return null;

            int width = tex.Width;
            int height = tex.Height;
            int mips = tex.Levels;
            string format = tex.Format.ToString();

            byte[] header = new byte[128];
            using (MemoryStream ms = new MemoryStream(header))
            using (BinaryWriter bw = new BinaryWriter(ms))
            {
                bw.Write(0x20534444); // Magic "DDS "
                bw.Write(124); // Size of header structure
                bw.Write(0x1 | 0x2 | 0x4 | 0x1000 | 0x20000 | 0x80000); // Flags
                bw.Write(height);
                bw.Write(width);

                int blockSize = (format.Contains("DXT1") || format.Contains("BC1")) ? 8 : 16;
                int pitch = Math.Max(1, ((width + 3) / 4)) * blockSize;
                bw.Write(pitch * height);

                bw.Write(0); // Depth
                bw.Write(mips); // Mipmap count
                for (int i = 0; i < 11; i++) bw.Write(0); // Reserved

                // PIXEL FORMAT STRUCTURE
                bw.Write(32);
                bw.Write(0x4);

                string fourCC = "DXT1";
                if (format.Contains("DXT3") || format.Contains("BC2")) fourCC = "DXT3";
                if (format.Contains("DXT5") || format.Contains("BC3")) fourCC = "DXT5";
                if (format.Contains("ATI1") || format.Contains("BC4")) fourCC = "ATI1";
                if (format.Contains("ATI2") || format.Contains("BC5")) fourCC = "ATI2";

                bw.Write(Encoding.ASCII.GetBytes(fourCC));

                bw.Write(0); bw.Write(0); bw.Write(0); bw.Write(0); bw.Write(0);

                // CAPS
                bw.Write(0x1000 | 0x400000);
                bw.Write(0); bw.Write(0); bw.Write(0); bw.Write(0);
            }

            byte[] combined = new byte[header.Length + textureData.Length];
            Array.Copy(header, 0, combined, 0, header.Length);
            Array.Copy(textureData, 0, combined, header.Length, textureData.Length);

            return combined;
        }

        // --- PFIM TO PNG CONVERTER ---
        private static string ConvertDdsToPngBase64(byte[] ddsData)
        {
            using var stream = new MemoryStream(ddsData);
            using var pfimImage = Pfimage.FromStream(stream);

            byte[] bgra = ConvertToBgra32(pfimImage);
            var info = new SKImageInfo(pfimImage.Width, pfimImage.Height, SKColorType.Bgra8888, SKAlphaType.Unpremul);

            using var bitmap = new SKBitmap(info);
            Marshal.Copy(bgra, 0, bitmap.GetPixels(), bgra.Length);

            using var skImage = SKImage.FromBitmap(bitmap);
            using var pngData = skImage.Encode(SKEncodedImageFormat.Png, 90);

            return $"data:image/png;base64,{Convert.ToBase64String(pngData.ToArray())}";
        }

        private static byte[] ConvertToBgra32(IImage image)
        {
            int w = image.Width, h = image.Height;
            var result = new byte[w * h * 4];

            if (image.Format == ImageFormat.Rgba32)
            {
                for (int y = 0; y < h; y++)
                    Buffer.BlockCopy(image.Data, y * image.Stride, result, y * w * 4, w * 4);
            }
            else if (image.Format == ImageFormat.Rgb24)
            {
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        int src = y * image.Stride + x * 3, dst = (y * w + x) * 4;
                        result[dst] = image.Data[src];
                        result[dst + 1] = image.Data[src + 1];
                        result[dst + 2] = image.Data[src + 2];
                        result[dst + 3] = 255;
                    }
                }
            }
            return result;
        }
    }
}
