﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ActiveTextureManagement
{
    class TextureConverter
    {
        const int MAX_IMAGE_SIZE = 4048 * 4048 * 5;
        static byte[] imageBuffer = null;

        public static void InitImageBuffer()
        {
            if (imageBuffer == null)
            {
                imageBuffer = new byte[MAX_IMAGE_SIZE];
            }
        }

        public static void DestroyImageBuffer()
        {
            imageBuffer = null;
        }

        public static void Resize(GameDatabase.TextureInfo texture, int width, int height, bool mipmaps, bool convertToNormalFormat)
        {
            ActiveTextureManagement.DBGLog("Resizing...");
            Texture2D tex = texture.texture;
            TextureFormat format = tex.format;
            if (texture.isNormalMap)
            {
                format = TextureFormat.ARGB32;
            }
            else if (format == TextureFormat.DXT1 || format == TextureFormat.RGB24)
            {
                format = TextureFormat.RGB24;
            }
            else
            {
                format = TextureFormat.RGBA32;
            }

            Color32[] pixels = tex.GetPixels32();
            if (convertToNormalFormat)
            {
                ConvertToUnityNormalMap(pixels);
            }

            Color32[] newPixels = ResizePixels(pixels, tex.width, tex.height, width, height);
            tex.Resize(width, height, format, mipmaps);
            tex.SetPixels32(newPixels);
            tex.Apply(mipmaps);
        }

        private static Color32[] ResizePixels(Color32[] pixels, int width, int height, int newWidth, int newHeight)
        {
            Color32[] newPixels = new Color32[newWidth * newHeight];
            int index = 0;
            for (int h = 0; h < newHeight; h++)
            {
                for (int w = 0; w < newWidth; w++)
                {
                    newPixels[index++] = GetPixel(pixels, width, height, ((float)w) / newWidth, ((float)h) / newHeight, newWidth, newHeight);
                }
            }
            return newPixels;
        }

        public static void ConvertToUnityNormalMap(Color32[] colors)
        {
            for(int i = 0; i < colors.Length; i++)
            {
                colors[i].a = colors[i].r;
                colors[i].r = colors[i].g;
                colors[i].b = colors[i].g;
            }
        }

        private static Color32 GetPixel(Color32[] pixels, int width, int height, float w, float h, int newWidth, int newHeight)
        {
            float widthDist = 4.0f - ((4.0f * (float)newWidth) / width);
            float heightDist = 4.0f - ((4.0f * (float)newHeight) / height);
            int[,] posArray = new int[2, 4];
            posArray[0, 0] = (int)Math.Floor((w * width) - widthDist);
            posArray[0, 1] = (int)Math.Floor(w * width);
            posArray[0, 2] = (int)Math.Ceiling((w * width) + widthDist);
            posArray[0, 3] = (int)Math.Ceiling((w * width) + (2.0 * widthDist));
            posArray[1, 0] = (int)Math.Floor((h * height) - heightDist);
            posArray[1, 1] = (int)Math.Floor(h * height);
            posArray[1, 2] = (int)Math.Ceiling((h * height) + heightDist);
            posArray[1, 3] = (int)Math.Ceiling((h * height) + (2.0 * heightDist));

            Color32 cw1 = new Color32(), cw2 = new Color32(), cw3 = new Color32(), cw4 = new Color32(), ch1 = new Color32(), ch2 = new Color32(), ch3 = new Color32(), ch4 = new Color32();
            int w1 = posArray[0, 0];
            int w2 = posArray[0, 1];
            int w3 = posArray[0, 2];
            int w4 = posArray[0, 3];
            int h1 = posArray[1, 0];
            int h2 = posArray[1, 1];
            int h3 = posArray[1, 2];
            int h4 = posArray[1, 3];

            if (h2 >= 0 && h2 < height)
            {
                if (w2 >= 0 && w2 < width)
                {
                    cw2 = pixels[w2+ (h2*width)];
                }
                if (w1 >= 0 && w1 < width)
                {
                    cw1 = pixels[w1 + (h2 * width)];
                }
                else
                {
                    cw1 = cw2;
                }
                if (w3 >= 0 && w3 < width)
                {
                    cw3 = pixels[w3 + (h2 * width)];
                }
                else
                {
                    cw3 = cw2;
                }
                if (w4 >= 0 && w4 < width)
                {
                    cw4 = pixels[w4 + (h2 * width)];
                }
                else
                {
                    cw4 = cw3;
                }

            }
            if (w2 >= 0 && w2 < width)
            {
                if (h2 >= 0 && h2 < height)
                {
                    ch2 = pixels[w2 + (h2 * width)];
                }
                if (h1 >= 0 && h1 < height)
                {
                    ch1 = pixels[w2 + (h1 * width)];
                }
                else
                {
                    ch1 = ch2;
                }
                if (h3 >= 0 && h3 < height)
                {
                    ch3 = pixels[w2 + (h3 * width)];
                }
                else
                {
                    ch3 = ch2;
                }
                if (h4 >= 0 && h4 < height)
                {
                    ch4 = pixels[w2 + (h4 * width)];
                }
                else
                {
                    ch4 = ch3;
                }
            }
            byte cwr = (byte)(((.25f * cw1.r) + (.75f * cw2.r) + (.75f * cw3.r) + (.25f * cw4.r)) / 2.0f);
            byte cwg = (byte)(((.25f * cw1.g) + (.75f * cw2.g) + (.75f * cw3.g) + (.25f * cw4.g)) / 2.0f);
            byte cwb = (byte)(((.25f * cw1.b) + (.75f * cw2.b) + (.75f * cw3.b) + (.25f * cw4.b)) / 2.0f);
            byte cwa = (byte)(((.25f * cw1.a) + (.75f * cw2.a) + (.75f * cw3.a) + (.25f * cw4.a)) / 2.0f);
            byte chr = (byte)(((.25f * ch1.r) + (.75f * ch2.r) + (.75f * ch3.r) + (.25f * ch4.r)) / 2.0f);
            byte chg = (byte)(((.25f * ch1.g) + (.75f * ch2.g) + (.75f * ch3.g) + (.25f * ch4.g)) / 2.0f);
            byte chb = (byte)(((.25f * ch1.b) + (.75f * ch2.b) + (.75f * ch3.b) + (.25f * ch4.b)) / 2.0f);
            byte cha = (byte)(((.25f * ch1.a) + (.75f * ch2.a) + (.75f * ch3.a) + (.25f * ch4.a)) / 2.0f);
            byte R = (byte)((cwr + chr) / 2.0f);
            byte G = (byte)((cwg + chg) / 2.0f);
            byte B = (byte)((cwb + chb) / 2.0f);
            byte A = (byte)((cwa + cha) / 2.0f);

            Color32 color = new Color32(R, G, B, A);
            return color;
        }

        public static void MBMToTexture(TexInfo Texture, bool mipmaps)
        {
            GameDatabase.TextureInfo texture = Texture.texture;
            TextureConverter.InitImageBuffer();
            FileStream mbmStream = new FileStream(Texture.filename, FileMode.Open, FileAccess.Read);
            mbmStream.Position = 4;

            uint width = 0, height = 0;
            for (int b = 0; b < 4; b++)
            {
                width >>= 8;
                width |= (uint)(mbmStream.ReadByte() << 24);
            }
            for (int b = 0; b < 4; b++)
            {
                height >>= 8;
                height |= (uint)(mbmStream.ReadByte() << 24);
            }
            mbmStream.Position = 12;
            bool convertToNormalFormat = false;
            if (mbmStream.ReadByte() == 1)
            {
                texture.isNormalMap = true;
            }
            else
            {
                convertToNormalFormat = texture.isNormalMap ? true : false;
            }

            mbmStream.Position = 16;
            int format = mbmStream.ReadByte();
            mbmStream.Position += 3;

            int imageSize = (int)(width * height * 3);
            TextureFormat texformat = TextureFormat.RGB24;
            bool alpha = false;
            if (format == 32)
            {
                imageSize += (int)(width * height);
                texformat = TextureFormat.ARGB32;
                alpha = true;
            }
            if (texture.isNormalMap)
            {
                texformat = TextureFormat.ARGB32;
            }

            mbmStream.Read(imageBuffer, 0, MAX_IMAGE_SIZE);
            mbmStream.Close();

            Texture2D tex = texture.texture;
            
            Color32[] colors = new Color32[width * height];
            int n = 0;
            for (int i = 0; i < width * height; i++)
            {
                colors[i].r = imageBuffer[n++];
                colors[i].g = imageBuffer[n++];
                colors[i].b = imageBuffer[n++];
                if (alpha)
                {
                    colors[i].a = imageBuffer[n++];
                }
                else
                {
                    colors[i].a = 255;
                }
                if(convertToNormalFormat)
                {
                    colors[i].a = colors[i].r;
                    colors[i].r = colors[i].g;
                    colors[i].b = colors[i].g;
                }
            }

            if (Texture.loadOriginalFirst)
            {
                Texture.Resize((int)width, (int)height);
            }

            if (Texture.needsResize)
            {
                colors = TextureConverter.ResizePixels(colors, (int)width, (int)height, Texture.resizeWidth, Texture.resizeHeight);
                width = (uint)Texture.resizeWidth;
                height = (uint)Texture.resizeHeight;
            }
            tex.Resize((int)width, (int)height, texformat, mipmaps);
            tex.SetPixels32(colors);
            tex.Apply(mipmaps, false);
        }

        // DDS Texture loader inspired by
        // http://answers.unity3d.com/questions/555984/can-you-load-dds-textures-during-runtime.html#answer-707772
        // http://msdn.microsoft.com/en-us/library/bb943992.aspx
        // http://msdn.microsoft.com/en-us/library/windows/desktop/bb205578(v=vs.85).aspx
        // does not rescale and such atm
        public static void DDSToTexture(TexInfo Texture, bool mipmaps)
        {
            GameDatabase.TextureInfo texture = Texture.texture;
            TextureConverter.InitImageBuffer();
            using (BinaryReader reader = new BinaryReader(File.Open(Texture.filename, FileMode.Open, FileAccess.Read)))
            {
                int dwMagic = (int)reader.ReadUInt32();

                int dwSize = (int)reader.ReadUInt32();

                //this header byte should be 124 for DDS image files
                if (dwSize != 124)
                    throw new Exception("Invalid DDS DXTn texture. Unable to read");

                int dwFlags = (int)reader.ReadUInt32();
                int dwHeight = (int)reader.ReadUInt32();
                int dwWidth = (int)reader.ReadUInt32();
                int dwPitchOrLinearSize = (int)reader.ReadUInt32();
                int dwDepth = (int)reader.ReadUInt32();
                int dwMipMapCount = (int)reader.ReadUInt32();

                // dwReserved1 
                for (int i=0;i<11;i++)
                    reader.ReadUInt32();
                // DDS_PIXELFORMAT 
                int dds_pxlf_dwSize = (int)reader.ReadUInt32();
                int dds_pxlf_dwFlags = (int)reader.ReadUInt32();
                byte[] dds_pxlf_dwFourCC = reader.ReadBytes(4);
                string fourCC = Encoding.ASCII.GetString(dds_pxlf_dwFourCC);
                int dds_pxlf_dwRGBBitCount = (int)reader.ReadUInt32();
                int dds_pxlf_dwRBitMask = (int)reader.ReadUInt32();
                int dds_pxlf_dwGBitMask = (int)reader.ReadUInt32();
                int dds_pxlf_dwBBitMask = (int)reader.ReadUInt32();
                int dds_pxlf_dwABitMask = (int)reader.ReadUInt32();

                int dwCaps = (int)reader.ReadUInt32();
                int dwCaps2 = (int)reader.ReadUInt32();
                int dwCaps3 = (int)reader.ReadUInt32();
                int dwCaps4 = (int)reader.ReadUInt32();
                int dwReserved2 = (int)reader.ReadUInt32();

                long dxtBytesLength = reader.BaseStream.Length - 128;
                TextureFormat textureFormat = TextureFormat.ARGB32;

                // For now do as if there was no mipmap since I don't 
                // know if they are actually loaded with LoadRawTextureData
                if (fourCC == "DXT1")
                {
                    textureFormat = TextureFormat.DXT1;
                }
                else if (fourCC == "DXT5")
                {
                    textureFormat = TextureFormat.DXT5;
                }
                byte[] dxtBytes = reader.ReadBytes((int)dxtBytesLength);

                if (textureFormat == TextureFormat.DXT1 || textureFormat ==TextureFormat.DXT5)
                {
                    texture.texture = new Texture2D(dwWidth, dwHeight, textureFormat, dwMipMapCount > 0);
                    texture.texture.LoadRawTextureData(dxtBytes);
                    texture.texture.Apply();
                }
            }

        }




        public static void IMGToTexture(TexInfo Texture, bool mipmaps, bool isNormalFormat)
        {
            GameDatabase.TextureInfo texture = Texture.texture;
            TextureConverter.InitImageBuffer();
            FileStream imgStream = new FileStream(Texture.filename, FileMode.Open, FileAccess.Read);
            imgStream.Position = 0;
            imgStream.Read(imageBuffer, 0, MAX_IMAGE_SIZE);
            imgStream.Close();

            Texture2D tex = texture.texture;
            tex.LoadImage(imageBuffer);
            bool convertToNormalFormat = texture.isNormalMap && !isNormalFormat ? true : false;
            bool hasMipmaps = tex.mipmapCount == 1 ? false : true;
            if(Texture.loadOriginalFirst)
            {
                Texture.Resize(tex.width, tex.height);
            }
            TextureFormat format = tex.format;
            if(texture.isNormalMap)
            {
                format = TextureFormat.ARGB32;
            }
            if(Texture.needsResize)
            {
                TextureConverter.Resize(texture, Texture.resizeWidth, Texture.resizeHeight, mipmaps, convertToNormalFormat);
            }
            else if (convertToNormalFormat || hasMipmaps != mipmaps || format != tex.format)
            {
                Color32[] pixels = tex.GetPixels32();
                if (convertToNormalFormat)
                {
                    for (int i = 0; i < pixels.Length; i++)
                    {
                        pixels[i].a = pixels[i].r;
                        pixels[i].r = pixels[i].g;
                        pixels[i].b = pixels[i].g;
                    }
                }
                if (tex.format != format || hasMipmaps != mipmaps)
                {
                    tex.Resize(tex.width, tex.height, format, mipmaps);
                }
                tex.SetPixels32(pixels);
                tex.Apply(mipmaps);
            }
            
        }

        public static void TGAToTexture(TexInfo Texture, bool mipmaps)
        {
            GameDatabase.TextureInfo texture = Texture.texture;
            TextureConverter.InitImageBuffer();
            FileStream tgaStream = new FileStream(Texture.filename, FileMode.Open, FileAccess.Read);
            tgaStream.Position = 0;
            tgaStream.Read(imageBuffer, 0, MAX_IMAGE_SIZE);
            tgaStream.Close();

            byte imgType = imageBuffer[2];
            int width = imageBuffer[12] | (imageBuffer[13] << 8);
            int height = imageBuffer[14] | (imageBuffer[15] << 8);
            if (Texture.loadOriginalFirst)
            {
                Texture.Resize(width, height);
            }
            int depth = imageBuffer[16];
            bool alpha = depth == 32 ? true : false;
            TextureFormat texFormat = depth == 32 ? TextureFormat.RGBA32 : TextureFormat.RGB24;
            if(texture.isNormalMap)
            {
                texFormat = TextureFormat.ARGB32;
            }
            bool convertToNormalFormat = texture.isNormalMap ? true : false; 

            Texture2D tex = texture.texture;

            Color32[] colors = new Color32[width * height];
            int n = 18;
            if (imgType == 2)
            {
                for (int i = 0; i < width * height; i++)
                {
                    colors[i].b = imageBuffer[n++];
                    colors[i].g = imageBuffer[n++];
                    colors[i].r = imageBuffer[n++];
                    if (alpha)
                    {
                        colors[i].a = imageBuffer[n++];
                    }
                    else
                    {
                        colors[i].a = 255;
                    }
                    if (convertToNormalFormat)
                    {
                        colors[i].a = colors[i].r;
                        colors[i].r = colors[i].g;
                        colors[i].b = colors[i].g;
                    }
                }
            }
            else if (imgType == 10)
            {
                int i = 0;
                int run = 0;
                while (i < width * height)
                {
                    run = imageBuffer[n++];
                    if ((run & 0x80) != 0)
                    {
                        run = (run ^ 0x80) + 1;
                        colors[i].b = imageBuffer[n++];
                        colors[i].g = imageBuffer[n++];
                        colors[i].r = imageBuffer[n++];
                        if (alpha)
                        {
                            colors[i].a = imageBuffer[n++];
                        }
                        else
                        {
                            colors[i].a = 255;
                        }
                        if (convertToNormalFormat)
                        {
                            colors[i].a = colors[i].r;
                            colors[i].r = colors[i].g;
                            colors[i].b = colors[i].g;
                        }
                        i++;
                        for (int c = 1; c < run; c++, i++)
                        {
                            colors[i] = colors[i - 1];
                        }
                    }
                    else
                    {
                        run += 1;
                        for (int c = 0; c < run; c++, i++)
                        {
                            colors[i].b = imageBuffer[n++];
                            colors[i].g = imageBuffer[n++];
                            colors[i].r = imageBuffer[n++];
                            if (alpha)
                            {
                                colors[i].a = imageBuffer[n++];
                            }
                            else
                            {
                                colors[i].a = 255;
                            }
                            if (convertToNormalFormat)
                            {
                                colors[i].a = colors[i].r;
                                colors[i].r = colors[i].g;
                                colors[i].b = colors[i].g;
                            }
                        }
                    }
                }
            }
            else
            {
                ActiveTextureManagement.DBGLog("TGA format is not supported!");
            }


            if (Texture.needsResize)
            {
                colors = TextureConverter.ResizePixels(colors, width, height, Texture.resizeWidth, Texture.resizeHeight);
                width = Texture.resizeWidth;
                height = Texture.resizeHeight;
            }
            tex.Resize((int)width, (int)height, texFormat, mipmaps);
            tex.SetPixels32(colors);
            tex.Apply(mipmaps, false);
        }

        public static void GetReadable(TexInfo Texture, bool mipmaps)
        {
            String mbmPath = KSPUtil.ApplicationRootPath + "GameData/" + Texture.name + ".mbm";
            String pngPath = KSPUtil.ApplicationRootPath + "GameData/" + Texture.name + ".png";
            String pngPathTruecolor = KSPUtil.ApplicationRootPath + "GameData/" + Texture.name + ".truecolor";
            String jpgPath = KSPUtil.ApplicationRootPath + "GameData/" + Texture.name + ".jpg";
            String tgaPath = KSPUtil.ApplicationRootPath + "GameData/" + Texture.name + ".tga";
            String ddsPath = KSPUtil.ApplicationRootPath + "GameData/" + Texture.name + ".dds";
            if (File.Exists(pngPath) || File.Exists(pngPathTruecolor) || File.Exists(jpgPath) || File.Exists(tgaPath) || File.Exists(mbmPath))
            {
                Texture2D tex = new Texture2D(2, 2);
                String name;
                if (Texture.name.Length > 0)
                {
                    name = Texture.name;
                }
                else
                {
                    name = Texture.name;
                }
                
                GameDatabase.TextureInfo newTexture = new GameDatabase.TextureInfo(tex, Texture.isNormalMap, true, false);
                Texture.texture = newTexture;
                newTexture.name = Texture.name;
                if (File.Exists(pngPath))
                {
                    Texture.filename = pngPath;
                    IMGToTexture(Texture, mipmaps, false);
                }
                else if (File.Exists(pngPathTruecolor))
                {
                    Texture.filename = pngPathTruecolor;
                    IMGToTexture(Texture, mipmaps, false);
                }
                else if (File.Exists(jpgPath))
                {
                    Texture.filename = jpgPath;
                    IMGToTexture(Texture, mipmaps, false);
                }
                else if (File.Exists(tgaPath))
                {
                    Texture.filename = tgaPath;
                    TGAToTexture(Texture, mipmaps);
                }
                else if (File.Exists(mbmPath))
                {
                    Texture.filename = mbmPath;
                    MBMToTexture(Texture, mipmaps);
                }
                else if (File.Exists(ddsPath))
                {
                    Texture.filename = ddsPath;
                    DDSToTexture(Texture, mipmaps);
                }
                tex.name = newTexture.name;
            }
        }

        internal static void WriteTo(Texture2D cacheTexture, string cacheFile)
        {
            String directory = Path.GetDirectoryName(cacheFile + ".none");
            if (File.Exists(directory))
            {
                File.Delete(directory);
            }
            Directory.CreateDirectory(directory);
            FileStream imgStream = new FileStream(cacheFile, FileMode.Create, FileAccess.Write);
            imgStream.Position = 0;
            byte[] png = cacheTexture.EncodeToPNG();
            imgStream.Write(png, 0, png.Length);
            imgStream.Close();
        }

    }
}
