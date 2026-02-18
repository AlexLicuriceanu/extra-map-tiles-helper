using System;
using System.IO;
using System.Text;
using CodeWalker.GameFiles; // Make sure this reference is here

namespace ExtraMapTilesHelper
{
    public static class DDSExtractor
    {
        public static byte[] GetDDS(Texture tex)
        {
            if (tex == null) return null;

            // 1. Get the raw texture data from the CodeWalker object
            // Get the raw bytes of the first (biggest) mipmap level
            byte[] textureData = tex.Data.FullData;

            // If the texture is empty or weird, return null
            if (textureData == null || textureData.Length == 0) return null;

            int width = tex.Width;
            int height = tex.Height;
            int mips = tex.Levels;
            string format = tex.Format.ToString(); // e.g., DXT1, DXT5

            // 2. Build the DDS Header
            // DDS files always start with a 128-byte header.
            byte[] header = new byte[128];
            using (MemoryStream ms = new MemoryStream(header))
            using (BinaryWriter bw = new BinaryWriter(ms))
            {
                // Magic "DDS "
                bw.Write(0x20534444);

                // Size of header structure (must be 124)
                bw.Write(124);

                // Flags (Caps | Height | Width | PixelFormat | MipMapCount | LinearSize)
                // 0x1 = CAPS, 0x2 = HEIGHT, 0x4 = WIDTH, 0x1000 = PIXELFORMAT, 0x20000 = MIPMAPCOUNT, 0x80000 = LINEARSIZE
                bw.Write(0x1 | 0x2 | 0x4 | 0x1000 | 0x20000 | 0x80000);

                bw.Write(height);
                bw.Write(width);

                // Pitch/Linear Size (Approximation for DXT formats)
                int blockSize = (format.Contains("DXT1") || format.Contains("BC1")) ? 8 : 16;
                int pitch = Math.Max(1, ((width + 3) / 4)) * blockSize;
                bw.Write(pitch * height);

                bw.Write(0); // Depth
                bw.Write(mips); // Mipmap count

                // Reserved[11]
                for (int i = 0; i < 11; i++) bw.Write(0);

                // --- PIXEL FORMAT STRUCTURE ---
                bw.Write(32); // Size of PixelFormat structure
                bw.Write(0x4); // Flags (DDPF_FOURCC)

                // FourCC Code (The most important part!)
                // Tells Windows what compression is used.
                string fourCC = "DXT1"; // Default
                if (format.Contains("DXT3") || format.Contains("BC2")) fourCC = "DXT3";
                if (format.Contains("DXT5") || format.Contains("BC3")) fourCC = "DXT5";
                if (format.Contains("ATI1") || format.Contains("BC4")) fourCC = "ATI1";
                if (format.Contains("ATI2") || format.Contains("BC5")) fourCC = "ATI2";

                // Write FourCC as 4 bytes
                byte[] fourCCBytes = Encoding.ASCII.GetBytes(fourCC);
                bw.Write(fourCCBytes);

                // RGBBitCount, RBitMask, G, B, A (Ignored for DXT/FourCC formats)
                bw.Write(0);
                bw.Write(0);
                bw.Write(0);
                bw.Write(0);
                bw.Write(0);

                // --- CAPS ---
                bw.Write(0x1000 | 0x400000); // DDSCAPS_TEXTURE | DDSCAPS_MIPMAP
                bw.Write(0); // Caps2
                bw.Write(0); // Caps3
                bw.Write(0); // Caps4
                bw.Write(0); // Reserved2
            }

            // 3. Combine Header + Raw Data
            byte[] combined = new byte[header.Length + textureData.Length];
            Array.Copy(header, 0, combined, 0, header.Length);
            Array.Copy(textureData, 0, combined, header.Length, textureData.Length);

            return combined;
        }
    }
}