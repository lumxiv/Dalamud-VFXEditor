using System.IO;
using VfxEditor.Interop;

namespace VfxEditor.ScdFormat {
    public static class ScdUtils {
        public static string VorbisHeader => Path.Combine( Plugin.RootLocation, "Files", "vorbis_header.bin" );

        public static void ConvertToOgg( string wavPath ) {
            Cleanup();
            InteropUtils.Run( "oggenc2.exe", $"-s 0  --resample 44100 -o \"{ScdManager.ConvertOgg}\" \"{wavPath}\"", false, out var _ );
        }

        public static void ConvertToAdpcm( string wavPath ) {
            Cleanup();
            InteropUtils.Run( "adpcmencode3.exe", $"-b 256 \"{wavPath}\" \"{ScdManager.ConvertWav}\"", false, out var _ );
        }

        public static void Cleanup() {
            if( File.Exists( ScdManager.ConvertWav ) ) File.Delete( ScdManager.ConvertWav );
            if( File.Exists( ScdManager.ConvertOgg ) ) File.Delete( ScdManager.ConvertOgg );
        }

        public static void XorDecode( byte[] vorbisHeader, byte encodeByte ) {
            for( var i = 0; i < vorbisHeader.Length; i++ ) {
                vorbisHeader[i] ^= encodeByte;
            }
        }

        public static void XorDecodeFromTable( byte[] dataFile, int dataLength ) {
            var byte1 = dataLength & 0xFF & 0x7F;
            var byte2 = byte1 & 0x3F;
            for( var i = 0; i < dataFile.Length; i++ ) {
                var xorByte = XORTABLE[( byte2 + i ) & 0xFF];
                xorByte &= 0xFF;
                xorByte ^= dataFile[i] & 0xFF;
                xorByte ^= byte1;
                dataFile[i] = ( byte )xorByte;
            }
        }

        public static readonly int[] XORTABLE = [
            0x003A,
            0x0032,
            0x0032,
            0x0032,
            0x0003,
            0x007E,
            0x0012,
            0x00F7,
            0x00B2,
            0x00E2,
            0x00A2,
            0x0067,
            0x0032,
            0x0032,
            0x0022,
            0x0032,
            0x0032,
            0x0052,
            0x0016,
            0x001B,
            0x003C,
            0x00A1,
            0x0054,
            0x007B,
            0x001B,
            0x0097,
            0x00A6,
            0x0093,
            0x001A,
            0x004B,
            0x00AA,
            0x00A6,
            0x007A,
            0x007B,
            0x001B,
            0x0097,
            0x00A6,
            0x00F7,
            0x0002,
            0x00BB,
            0x00AA,
            0x00A6,
            0x00BB,
            0x00F7,
            0x002A,
            0x0051,
            0x00BE,
            0x0003,
            0x00F4,
            0x002A,
            0x0051,
            0x00BE,
            0x0003,
            0x00F4,
            0x002A,
            0x0051,
            0x00BE,
            0x0012,
            0x0006,
            0x0056,
            0x0027,
            0x0032,
            0x0032,
            0x0036,
            0x0032,
            0x00B2,
            0x001A,
            0x003B,
            0x00BC,
            0x0091,
            0x00D4,
            0x007B,
            0x0058,
            0x00FC,
            0x000B,
            0x0055,
            0x002A,
            0x0015,
            0x00BC,
            0x0040,
            0x0092,
            0x000B,
            0x005B,
            0x007C,
            0x000A,
            0x0095,
            0x0012,
            0x0035,
            0x00B8,
            0x0063,
            0x00D2,
            0x000B,
            0x003B,
            0x00F0,
            0x00C7,
            0x0014,
            0x0051,
            0x005C,
            0x0094,
            0x0086,
            0x0094,
            0x0059,
            0x005C,
            0x00FC,
            0x001B,
            0x0017,
            0x003A,
            0x003F,
            0x006B,
            0x0037,
            0x0032,
            0x0032,
            0x0030,
            0x0032,
            0x0072,
            0x007A,
            0x0013,
            0x00B7,
            0x0026,
            0x0060,
            0x007A,
            0x0013,
            0x00B7,
            0x0026,
            0x0050,
            0x00BA,
            0x0013,
            0x00B4,
            0x002A,
            0x0050,
            0x00BA,
            0x0013,
            0x00B5,
            0x002E,
            0x0040,
            0x00FA,
            0x0013,
            0x0095,
            0x00AE,
            0x0040,
            0x0038,
            0x0018,
            0x009A,
            0x0092,
            0x00B0,
            0x0038,
            0x0000,
            0x00FA,
            0x0012,
            0x00B1,
            0x007E,
            0x0000,
            0x00DB,
            0x0096,
            0x00A1,
            0x007C,
            0x0008,
            0x00DB,
            0x009A,
            0x0091,
            0x00BC,
            0x0008,
            0x00D8,
            0x001A,
            0x0086,
            0x00E2,
            0x0070,
            0x0039,
            0x001F,
            0x0086,
            0x00E0,
            0x0078,
            0x007E,
            0x0003,
            0x00E7,
            0x0064,
            0x0051,
            0x009C,
            0x008F,
            0x0034,
            0x006F,
            0x004E,
            0x0041,
            0x00FC,
            0x000B,
            0x00D5,
            0x00AE,
            0x0041,
            0x00FC,
            0x000B,
            0x00D5,
            0x00AE,
            0x0041,
            0x00FC,
            0x003B,
            0x0070,
            0x0071,
            0x0064,
            0x0033,
            0x0032,
            0x0012,
            0x0032,
            0x0032,
            0x0036,
            0x0070,
            0x0034,
            0x002B,
            0x0056,
            0x0022,
            0x0070,
            0x003A,
            0x0013,
            0x00B7,
            0x0026,
            0x0060,
            0x00BA,
            0x001B,
            0x0094,
            0x00AA,
            0x0040,
            0x0038,
            0x0000,
            0x00FA,
            0x00B2,
            0x00E2,
            0x00A2,
            0x0067,
            0x0032,
            0x0032,
            0x0012,
            0x0032,
            0x00B2,
            0x0032,
            0x0032,
            0x0032,
            0x0032,
            0x0075,
            0x00A3,
            0x0026,
            0x007B,
            0x0083,
            0x0026,
            0x00F9,
            0x0083,
            0x002E,
            0x00FF,
            0x00E3,
            0x0016,
            0x007D,
            0x00C0,
            0x001E,
            0x0063,
            0x0021,
            0x0007,
            0x00E3,
            0x0001
        ];
    }
}
