using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SoulsFormats
{
    /// <summary>
    /// https://registry.khronos.org/KTX/spec/2.0/ktxspec.v2.html
    /// vkFormat values are sourced from Khronos Vulkan-Headers (vulkan_core.h) and DFD constants
    /// from Khronos khr_df.h.
    /// 
    /// Right now, only ASTC formats are supported.
    /// </summary>
    public class KTX2
    {
        public static readonly byte[] ktxMagic = { 0xAB, 0x4B, 0x54, 0x58, 0x20, 0x32, 0x30, 0xBB, 0x0D, 0x0A, 0x1A, 0x0A };

        public const byte KHR_DF_MODEL_ASTC = 162;
        public const byte KHR_DF_PRIMARIES_BT709 = 1;
        public const byte KHR_DF_TRANSFER_LINEAR = 1;
        public const byte KHR_DF_TRANSFER_SRGB = 2;
        public const ushort KHR_DF_VERSION = 2;
        public enum VkFormat : uint
        {
            UNDEFINED = 0,
            ASTC_4X4_UNORM_BLOCK = 157,
            ASTC_4X4_SRGB_BLOCK = 158,
            ASTC_5X4_UNORM_BLOCK = 159,
            ASTC_5X4_SRGB_BLOCK = 160,
            ASTC_5X5_UNORM_BLOCK = 161,
            ASTC_5X5_SRGB_BLOCK = 162,
            ASTC_6X5_UNORM_BLOCK = 163,
            ASTC_6X5_SRGB_BLOCK = 164,
            ASTC_6X6_UNORM_BLOCK = 165,
            ASTC_6X6_SRGB_BLOCK = 166,
            ASTC_8X5_UNORM_BLOCK = 167,
            ASTC_8X5_SRGB_BLOCK = 168,
            ASTC_8X6_UNORM_BLOCK = 169,
            ASTC_8X6_SRGB_BLOCK = 170,
            ASTC_8X8_UNORM_BLOCK = 171,
            ASTC_8X8_SRGB_BLOCK = 172,
            ASTC_10X5_UNORM_BLOCK = 173,
            ASTC_10X5_SRGB_BLOCK = 174,
            ASTC_10X6_UNORM_BLOCK = 175,
            ASTC_10X6_SRGB_BLOCK = 176,
            ASTC_10X8_UNORM_BLOCK = 177,
            ASTC_10X8_SRGB_BLOCK = 178,
            ASTC_10X10_UNORM_BLOCK = 179,
            ASTC_10X10_SRGB_BLOCK = 180,
            ASTC_12X10_UNORM_BLOCK = 181,
            ASTC_12X10_SRGB_BLOCK = 182,
            ASTC_12X12_UNORM_BLOCK = 183,
            ASTC_12X12_SRGB_BLOCK = 184,
        }

        public static bool Is(byte[] bytes)
        {
            if (bytes == null || bytes.Length < ktxMagic.Length)
                return false;

            for (int i = 0; i < ktxMagic.Length; i++)
                if (bytes[i] != ktxMagic[i])
                    return false;
            return true;
        }

        /// <summary>
        /// Per level supercompression applied to the mip level byte ranges.
        /// BasisLz is a full ETC1S re-encode rather than a byte stream codec
        /// and is not implemented.
        /// </summary>
        public enum SupercompressionScheme : uint
        {
            None = 0,
            Zstd = 1,
            BasisLz = 2,
            Zlib = 3,
        }

        private int zstdCompressionLevel = 3;

#pragma warning disable CS1591 
        public uint vkFormat;
        public uint typeSize;
        public uint pixelWidth;
        public uint pixelHeight;
        public uint pixelDepth;
        public uint layerCount;
        public uint faceCount;
        public uint levelCount;
        public SupercompressionScheme supercompressionScheme;

        public byte colorModel;
        public byte colorPrimaries;
        public byte transferFunction;
        public byte flags;
        public byte texelBlockDimension0;
        public byte texelBlockDimension1;
        public byte texelBlockDimension2;
        public byte texelBlockDimension3;
        public byte bytesPlane0;

        public List<List<byte[]>> Layers = new List<List<byte[]>>();

        public struct LayerInfo
        {
            public long offset;
            public long length;
            public long uncompressedLength;
        }
#pragma warning restore CS1591 
        public KTX2()
        {
            typeSize = 1;
            faceCount = 1;
        }

        public KTX2(byte[] bytes)
        {
            var br = new BinaryReaderEx(false, bytes);
            byte[] id = br.ReadBytes(ktxMagic.Length);

            vkFormat = br.ReadUInt32();
            typeSize = br.ReadUInt32();
            pixelWidth = br.ReadUInt32();
            pixelHeight = br.ReadUInt32();
            pixelDepth = br.ReadUInt32();
            layerCount = br.ReadUInt32();
            faceCount = br.ReadUInt32();
            levelCount = br.ReadUInt32();
            supercompressionScheme = br.ReadEnum32<SupercompressionScheme>();

            if (faceCount != 1)
                throw new NotSupportedException("KTX2 cube maps are not supported.");
            if (pixelDepth > 1)
                throw new NotSupportedException("KTX2 3D textures are not supported.");

            long dfdByteOffset = br.ReadInt32();
            br.ReadInt32(); //dfdByteLength
            br.ReadInt32(); //kvdByteOffset
            br.ReadInt32(); //kvdByteLength
            br.ReadInt64(); //sgdByteOffset
            br.ReadInt64(); //sgdByteLength

            int entryCount = Math.Max(1, (int)levelCount);
            var levelIndex = new LayerInfo[entryCount];
            for (int i = 0; i < entryCount; i++)
            {
                levelIndex[i] = new LayerInfo() 
                { 
                    offset = br.ReadInt64(), 
                    length = br.ReadInt64(), 
                    uncompressedLength = br.ReadInt64() 
                };
            }

            br.Position = dfdByteOffset;
            uint dfdTotalSize = br.ReadUInt32();
            br.ReadUInt32(); //vendorId | descriptorType
            uint versionAndBlockSize = br.ReadUInt32();
            if ((versionAndBlockSize & 0xFFFF) != KHR_DF_VERSION)
                throw new InvalidDataException($"Unexpected DFD version {versionAndBlockSize & 0xFFFF}.");
            colorModel = br.ReadByte();
            colorPrimaries = br.ReadByte();
            transferFunction = br.ReadByte();
            flags = br.ReadByte();
            texelBlockDimension0 = br.ReadByte();
            texelBlockDimension1 = br.ReadByte();
            texelBlockDimension2 = br.ReadByte();
            texelBlockDimension3 = br.ReadByte();
            bytesPlane0 = br.ReadByte();

            int layers = Math.Max(1, (int)layerCount);
            for (int i = 0; i < layers; i++)
                Layers.Add(new List<byte[]>((int)levelCount));

            for (int m = 0; m < levelCount; m++)
            {
                var info = levelIndex[m];
                if (info.offset + info.length > bytes.Length)
                    throw new InvalidDataException($"KTX2 level {m} data is out of bounds.");

                //Each mip level is an independent compressed stream covering all
                //layers/faces of that level 
                br.Position = info.offset;
                byte[] levelData;
                switch (supercompressionScheme)
                {
                    case SupercompressionScheme.None:
                        levelData = br.ReadBytes((int)info.length);
                        break;
                    case SupercompressionScheme.Zstd:
                        levelData = Compression.ZstdHelper.ReadZstd(br, (int)info.length);
                        break;
                    case SupercompressionScheme.Zlib:
                        levelData = Compression.ZlibHelper.ReadZlib(br, (int)info.length);
                        break;
                    case SupercompressionScheme.BasisLz:
                        throw new NotImplementedException("BasisLZ compression not implemented");
                    default:
                        throw new NotImplementedException($"Supercompression scheme {(uint)supercompressionScheme:X} not implemented");
                }

                int layerSize = (int)(levelData.Length / layers);
                for (int l = 0; l < layers; l++)
                {
                    var slice = new byte[layerSize];
                    Array.Copy(levelData, (long)l * layerSize, slice, 0, layerSize);
                    Layers[l].Add(slice);
                }
            }
        }


        /// <summary>
        /// Write a KTX2 texture, optionally supercompressing each mip level.
        /// </summary>
        public static byte[] WriteKtx2(List<Headerizer.Image> images, int width, int height, int depth, int mipCount, int dxgi,
            int footprintW, int footprintH, SupercompressionScheme supercompression = SupercompressionScheme.None, int zstdCompressionLevel = 3)
        {
            if (supercompression == SupercompressionScheme.BasisLz)
                throw new NotSupportedException("BasisLZ supercompression is not supported.");

            //NVIDIA's extended ids pair each footprint as (even UNORM, odd SRGB)
            bool srgb = (dxgi & 1) == 1;

            var ktx = new KTX2
            {
                vkFormat = KTX2.VkFormatForAstcFootprint(footprintW, footprintH, srgb), //We need to remap to KTX2's internal pixelformat
                typeSize = 1,
                pixelWidth = (uint)width,
                pixelHeight = (uint)height,
                pixelDepth = 0,
                layerCount = depth > 1 ? (uint)depth : 0,
                faceCount = 1,
                levelCount = (uint)mipCount,
                supercompressionScheme = supercompression,
                colorModel = KTX2.KHR_DF_MODEL_ASTC,
                colorPrimaries = KTX2.KHR_DF_PRIMARIES_BT709,
                transferFunction = (byte)(srgb ? KTX2.KHR_DF_TRANSFER_SRGB : KTX2.KHR_DF_TRANSFER_LINEAR),
                texelBlockDimension0 = (byte)(footprintW - 1),
                texelBlockDimension1 = (byte)(footprintH - 1),
                texelBlockDimension2 = 0,
                texelBlockDimension3 = 0,
                bytesPlane0 = 16,
                zstdCompressionLevel = zstdCompressionLevel,
            };
            foreach (var image in images)
            {
                ktx.Layers.Add(image.subImages);
            }

            return ktx.Write();
        }

        /// <summary>
        /// Writes the KTX2 file. 
        /// </summary>
        public byte[] Write()
        {
            //Trim to the shortest mip chain so all layers stay consistent
            int levels = Layers.Min(l => l.Count);
            levelCount = (uint)levels;
            //layerCount is 0 for non-array textures, but Layers always holds one entry per plane
            int layerPlanes = Math.Max(1, (int)layerCount);

            //Assemble level blobs, supercompressing each level if requested
            var levelBlobs = new List<byte[]>();
            var uncompressedLengths = new List<long>();
            for (int m = 0; m < levels; m++)
            {
                long size = 0;
                for (int l = 0; l < layerPlanes; l++)
                    size += Layers[l][m].Length;
                var blob = new byte[size];
                long pos = 0;
                for (int l = 0; l < layerPlanes; l++)
                {
                    Array.Copy(Layers[l][m], 0, blob, pos, Layers[l][m].Length);
                    pos += Layers[l][m].Length;
                }

                uncompressedLengths.Add(blob.Length);
                switch (supercompressionScheme)
                {
                    case SupercompressionScheme.None:
                        break;
                    case SupercompressionScheme.Zstd:
                        blob = Compression.ZstdHelper.WriteZstd(blob, zstdCompressionLevel);
                        break;
                    case SupercompressionScheme.Zlib:
                        blob = Compression.ZlibHelper.WriteZlibToBytes(blob, 0x9C);
                        break;
                    case SupercompressionScheme.BasisLz:
                        throw new NotImplementedException("BasisLZ compression not implemented");
                    default:
                        throw new NotImplementedException($"Supercompression scheme {(uint)supercompressionScheme:X} not implemented");
                }

                levelBlobs.Add(blob);
            }

            byte[] dfd = BuildDfd();
            byte[] kvd = BuildKvd();

            //Header
            long headerSize = 80 + Math.Max(1, levels) * 24;
            long dfdOffset = headerSize;
            long kvdOffset = dfdOffset + dfd.Length;
            int levelAlignment = Math.Max(4, (int)bytesPlane0);
            long levelStart = (kvdOffset + kvd.Length + levelAlignment - 1) & ~(levelAlignment - 1);

            var bw = new BinaryWriterEx(false);
            bw.WriteBytes(ktxMagic);
            bw.WriteUInt32(vkFormat);
            bw.WriteUInt32(typeSize);
            bw.WriteUInt32(pixelWidth);
            bw.WriteUInt32(pixelHeight);
            bw.WriteUInt32(pixelDepth);
            bw.WriteUInt32(layerCount);
            bw.WriteUInt32(faceCount);
            bw.WriteUInt32(levelCount);
            bw.WriteUInt32((uint)supercompressionScheme);
            bw.WriteUInt32((uint)dfdOffset);
            bw.WriteUInt32((uint)dfd.Length);
            bw.WriteUInt32((uint)kvdOffset);
            bw.WriteUInt32((uint)kvd.Length);
            bw.WriteInt64(0); //sgdByteOffset
            bw.WriteInt64(0); //sgdByteLength

            //Mip levels are stored smallest first (base level last)
            var levelOffsets = new long[levels];
            long levelOffset = levelStart;
            for (int m = levels - 1; m >= 0; m--)
            {
                levelOffsets[m] = levelOffset;
                levelOffset += levelBlobs[m].Length;
                levelOffset = (levelOffset + levelAlignment - 1) & ~(levelAlignment - 1);
            }
            for (int m = 0; m < levels; m++)
            {
                bw.WriteInt64(levelOffsets[m]);
                bw.WriteInt64(levelBlobs[m].Length);
                bw.WriteInt64(uncompressedLengths[m]); //uncompressedByteLength
            }

            bw.WriteBytes(dfd);
            bw.WriteBytes(kvd);
            while (bw.Length < levelStart)
                bw.WriteByte(0);

            for (int m = levels - 1; m >= 0; m--)
            {
                bw.WriteBytes(levelBlobs[m]);
                while (bw.Length % levelAlignment != 0)
                    bw.WriteByte(0);
            }

            return bw.FinishBytes();
        }

        /// <summary>
        /// The DFD as laid out for ASTC block compressed data
        /// </summary>
        private byte[] BuildDfd()
        {
            const int blockSize = 24 + 16; //basic block header + one sample
            var bw = new BinaryWriterEx(false);
            bw.WriteInt32(4 + blockSize); //dfdTotalSize
            bw.WriteInt32(0); //vendorId (Khronos) | descriptorType (basic)
            bw.WriteUInt16(KHR_DF_VERSION);
            bw.WriteUInt16((ushort)blockSize);
            bw.WriteByte(colorModel);
            bw.WriteByte(colorPrimaries);
            bw.WriteByte(transferFunction);
            bw.WriteByte(flags);
            bw.WriteByte(texelBlockDimension0);
            bw.WriteByte(texelBlockDimension1);
            bw.WriteByte(texelBlockDimension2);
            bw.WriteByte(texelBlockDimension3);
            bw.WriteByte(bytesPlane0);
            bw.WritePattern(7, 0); //bytesPlane1-7
            //Sample: channel R at bit 0, 128 bits long, unorm with full range bounds
            bw.WriteUInt16(0); //bitOffset
            bw.WriteByte(127); //bitLength - 1
            bw.WriteByte(0); //channelType (channel R, no qualifiers)
            bw.WritePattern(4, 0); //samplePosition
            bw.WriteUInt32(0); //sampleLower
            bw.WriteUInt32(0xFFFFFFFF); //sampleUpper
            return bw.FinishBytes();
        }

        /// <summary>
        /// Key/value data
        /// </summary>
        private byte[] BuildKvd()
        {
            var entries = new List<(string key, byte[] value)>
            {
                ("KTXversion", System.Text.Encoding.ASCII.GetBytes("2.0")),
                ("KTXwriter", System.Text.Encoding.ASCII.GetBytes("SoulsFormatsNEXT")),
            };

            var bw = new BinaryWriterEx(false);
            foreach (var (key, value) in entries.OrderBy(e => e.key, StringComparer.Ordinal))
            {
                byte[] keyBytes = System.Text.Encoding.ASCII.GetBytes(key + "\0");
                int length = keyBytes.Length + value.Length;
                bw.WriteInt32(length);
                bw.WriteBytes(keyBytes);
                bw.WriteBytes(value);
                bw.WritePattern((4 - length % 4) % 4, 0); //valuePadding
            }
            return bw.FinishBytes();
        }

        /// <summary>
        /// The images in Headerizer's slice major layout: one Image per layer with its mips.
        /// </summary>
        public List<Headerizer.Image> GetImages()
        {
            var images = new List<Headerizer.Image>();
            foreach (var layer in Layers)
            {
                var image = new Headerizer.Image();
                image.subImages.AddRange(layer);
                images.Add(image);
            }
            return images;
        }

        /// <summary>
        /// Resolve the ASTC block footprint in pixels and whether it's SRGB or not
        /// </summary>
        public bool IsASTCTexture(out int footprintW, out int footprintH, out bool srgb)
        {
            if (TryGetIsASTCTexture(vkFormat, out footprintW, out footprintH, out srgb))
                return true;

            if (vkFormat == 0 && colorModel == KHR_DF_MODEL_ASTC)
            {
                footprintW = texelBlockDimension0 + 1;
                footprintH = texelBlockDimension1 + 1;
                srgb = transferFunction == KHR_DF_TRANSFER_SRGB;
                return true;
            }
            return false;
        }

        /// <summary>
        /// The VkFormat for a standard ASTC footprint.
        /// </summary>
        public static uint VkFormatForAstcFootprint(int footprintW, int footprintH, bool srgb)
        {
            return (footprintW, footprintH, srgb) switch
            {
                (4, 4, false) => (uint)VkFormat.ASTC_4X4_UNORM_BLOCK,
                (4, 4, true) => (uint)VkFormat.ASTC_4X4_SRGB_BLOCK,
                (5, 4, false) => (uint)VkFormat.ASTC_5X4_UNORM_BLOCK,
                (5, 4, true) => (uint)VkFormat.ASTC_5X4_SRGB_BLOCK,
                (5, 5, false) => (uint)VkFormat.ASTC_5X5_UNORM_BLOCK,
                (5, 5, true) => (uint)VkFormat.ASTC_5X5_SRGB_BLOCK,
                (6, 5, false) => (uint)VkFormat.ASTC_6X5_UNORM_BLOCK,
                (6, 5, true) => (uint)VkFormat.ASTC_6X5_SRGB_BLOCK,
                (6, 6, false) => (uint)VkFormat.ASTC_6X6_UNORM_BLOCK,
                (6, 6, true) => (uint)VkFormat.ASTC_6X6_SRGB_BLOCK,
                (8, 5, false) => (uint)VkFormat.ASTC_8X5_UNORM_BLOCK,
                (8, 5, true) => (uint)VkFormat.ASTC_8X5_SRGB_BLOCK,
                (8, 6, false) => (uint)VkFormat.ASTC_8X6_UNORM_BLOCK,
                (8, 6, true) => (uint)VkFormat.ASTC_8X6_SRGB_BLOCK,
                (8, 8, false) => (uint)VkFormat.ASTC_8X8_UNORM_BLOCK,
                (8, 8, true) => (uint)VkFormat.ASTC_8X8_SRGB_BLOCK,
                (10, 5, false) => (uint)VkFormat.ASTC_10X5_UNORM_BLOCK,
                (10, 5, true) => (uint)VkFormat.ASTC_10X5_SRGB_BLOCK,
                (10, 6, false) => (uint)VkFormat.ASTC_10X6_UNORM_BLOCK,
                (10, 6, true) => (uint)VkFormat.ASTC_10X6_SRGB_BLOCK,
                (10, 8, false) => (uint)VkFormat.ASTC_10X8_UNORM_BLOCK,
                (10, 8, true) => (uint)VkFormat.ASTC_10X8_SRGB_BLOCK,
                (10, 10, false) => (uint)VkFormat.ASTC_10X10_UNORM_BLOCK,
                (10, 10, true) => (uint)VkFormat.ASTC_10X10_SRGB_BLOCK,
                (12, 10, false) => (uint)VkFormat.ASTC_12X10_UNORM_BLOCK,
                (12, 10, true) => (uint)VkFormat.ASTC_12X10_SRGB_BLOCK,
                (12, 12, false) => (uint)VkFormat.ASTC_12X12_UNORM_BLOCK,
                (12, 12, true) => (uint)VkFormat.ASTC_12X12_SRGB_BLOCK,
                _ => (uint)VkFormat.UNDEFINED,
            };
        }

        /// <summary>
        /// The ASTC block footprint in pixels and sRGB-ness for a standard ASTC VkFormat.
        /// </summary>
        public static bool TryGetIsASTCTexture(uint format, out int footprintW, out int footprintH, out bool srgb)
        {
            footprintW = 0;
            footprintH = 0;
            srgb = false;
            if (format < (uint)VkFormat.ASTC_4X4_UNORM_BLOCK || format > (uint)VkFormat.ASTC_12X12_SRGB_BLOCK)
                return false;

            int index = (int)(format - (uint)VkFormat.ASTC_4X4_UNORM_BLOCK);
            srgb = (index & 1) == 1;
            switch (index >> 1)
            {
                case 0: footprintW = 4; footprintH = 4; return true;
                case 1: footprintW = 5; footprintH = 4; return true;
                case 2: footprintW = 5; footprintH = 5; return true;
                case 3: footprintW = 6; footprintH = 5; return true;
                case 4: footprintW = 6; footprintH = 6; return true;
                case 5: footprintW = 8; footprintH = 5; return true;
                case 6: footprintW = 8; footprintH = 6; return true;
                case 7: footprintW = 8; footprintH = 8; return true;
                case 8: footprintW = 10; footprintH = 5; return true;
                case 9: footprintW = 10; footprintH = 6; return true;
                case 10: footprintW = 10; footprintH = 8; return true;
                case 11: footprintW = 10; footprintH = 10; return true;
                case 12: footprintW = 12; footprintH = 10; return true;
                case 13: footprintW = 12; footprintH = 12; return true;
                default: return false;
            }
        }
    }
}
