using Dalamud;
using FFXIVClientStructs.FFXIV.Client.System.Resource.Handle;
using Penumbra.String;
using Penumbra.String.Functions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using VfxEditor.Structs;

namespace VfxEditor.Interop {
    public static unsafe class InteropUtils {
        private const char Prefix = ( char )( ( byte )'P' | ( ( '?' & 0x00FF ) << 8 ) );

        public const int Size = 28;

        public static void Run( string exePath, string arguments, bool captureOutput, out string output ) {
            output = "";

            // Use ProcessStartInfo class
            var startInfo = new ProcessStartInfo {
                CreateNoWindow = true,
                UseShellExecute = false,
                FileName = Path.Combine( Plugin.RootLocation, "Files", exePath ),
                WindowStyle = ProcessWindowStyle.Hidden,
                Arguments = arguments,
                RedirectStandardOutput = true
            };

            try {
                var process = new Process {
                    StartInfo = startInfo
                };

                process.Start();
                if( captureOutput ) output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
            }
            catch( Exception e ) {
                Dalamud.Error( e, "Error executing" );
            }
        }

        // https://github.com/xivdev/Penumbra/blob/7710d9249675e6550f9db2eaaf94e1c570929c23/Penumbra/Interop/Hooks/ResourceLoading/ResourceLoader.cs#L269

        public static int ComputeHash( CiByteString path, GetResourceParameters* resParams ) {
            if( resParams == null || !resParams->IsPartialRead )
                return path.Crc32;

            // When the game requests file only partially, crc32 includes that information, in format of:
            // path/to/file.ext.hex_offset.hex_size
            // ex) music/ex4/BGM_EX4_System_Title.scd.381adc.30000
            return CiByteString.Join(
                ( byte )'.',
                path,
                CiByteString.FromString( resParams->SegmentOffset.ToString( "x" ), out var s1, MetaDataComputation.None ) ? s1 : CiByteString.Empty,
                CiByteString.FromString( resParams->SegmentLength.ToString( "x" ), out var s2, MetaDataComputation.None ) ? s2 : CiByteString.Empty
            ).Crc32;
        }

        public static byte[] GetBgCategory( string expansion, string zone ) {
            var ret = BitConverter.GetBytes( 2u );
            if( expansion == "ffxiv" ) return ret;
            // ex1/03_abr_a2/fld/a2f1/level/a2f1 -> [02 00 03 01]
            // expansion = ex1
            // zone = 03_abr_a2
            var expansionTrimmed = expansion.Replace( "ex", "" );
            var zoneTrimmed = zone.Split( '_' )[0];
            ret[2] = byte.Parse( zoneTrimmed );
            ret[3] = byte.Parse( expansionTrimmed );
            return ret;
        }

        public static byte[] GetDatCategory( uint prefix, string expansion ) {
            var ret = BitConverter.GetBytes( prefix );
            if( expansion == "ffxiv" ) return ret;
            // music/ex4/BGM_EX4_Field_Ult_Day03.scd
            // 04 00 00 0C
            var expansionTrimmed = expansion.Replace( "ex", "" );
            ret[3] = byte.Parse( expansionTrimmed );
            return ret;
        }

        public static void PrepPap( ResourceHandle* resource, List<string> ids, List<short> types ) {
            if( ids == null || types == null ) return;
            Marshal.WriteByte( (nint)resource + Constants.PrepPapOffset, Constants.PrepPapValue );
        }

        public static void WritePapIds( ResourceHandle* resource, List<string> ids, List<short> types ) {
            if( ids == null ) return;
            var data = Marshal.ReadIntPtr( (nint)resource + Constants.PapIdsOffset );
            for( var i = 0; i < ids.Count; i++ ) {
                SafeMemory.WriteString( data + ( i * 40 ), ids[i], Encoding.ASCII );
                Marshal.WriteInt16( data + ( i * 40 ) + 32, types[i] );
                Marshal.WriteByte( data + ( i * 40 ) + 34, ( byte )i );
            }
        }

        public static void WritePtr( char* buffer, byte* address, int length ) {
            // Set the prefix, which is not valid for any actual path.
            buffer[0] = Prefix;

            var ptr = ( byte* )buffer;
            var v = ( ulong )address;
            var l = ( uint )length;

            // Since the game calls wstrcpy without a length, we need to ensure
            // that there is no wchar_t (i.e. 2 bytes) of 0-values before the end.
            // Fill everything with 0xFF and use every second byte.
            MemoryUtility.MemSet( ptr + 2, 0xFF, 23 );

            // Write the byte pointer.
            ptr[2] = ( byte )( v >> 0 );
            ptr[4] = ( byte )( v >> 8 );
            ptr[6] = ( byte )( v >> 16 );
            ptr[8] = ( byte )( v >> 24 );
            ptr[10] = ( byte )( v >> 32 );
            ptr[12] = ( byte )( v >> 40 );
            ptr[14] = ( byte )( v >> 48 );
            ptr[16] = ( byte )( v >> 56 );

            // Write the length.
            ptr[18] = ( byte )( l >> 0 );
            ptr[20] = ( byte )( l >> 8 );
            ptr[22] = ( byte )( l >> 16 );
            ptr[24] = ( byte )( l >> 24 );

            ptr[Size - 2] = 0;
            ptr[Size - 1] = 0;
        }
    }
}
