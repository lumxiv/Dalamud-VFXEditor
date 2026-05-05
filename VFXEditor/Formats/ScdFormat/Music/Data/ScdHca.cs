using DereTore.Exchange.Audio.HCA;
using NAudio.Wave;
using System.IO;
using System.Numerics;
using VfxEditor.Formats.ScdFormat.Utils;
using VfxEditor.Utils;

namespace VfxEditor.ScdFormat.Music.Data {
    public class ScdHca : ScdAudioData {
        private byte[] StreamData; // Decoded
        private byte[] RawData; // What will be written, can be the same if there's no encryption

        // TODO: how is looping handled?

        // TODO: CRC right before the end of the header + data
        /*
         * if you crc the whole header
                it'll give you 0
                (because they just crc everything up until the last 2 bytes, then whatever it returns, they write the last 2 bytes as it)
                (that's what that f8 2c is for)
         */

        private readonly short HeaderSize;
        private readonly int BlockSize;
        private readonly bool PlainText;

        public ScdHca( BinaryReader reader, ScdAudioEntry entry ) : base( entry ) {
            reader.ReadInt16(); // TODO
            HeaderSize = reader.ReadInt16();
            BlockSize = reader.ReadInt16();
            reader.ReadBytes( 7 ); // TODO
            PlainText = reader.ReadBoolean();
            reader.ReadBytes( 10 ); // TODO

            using var streamMs = new MemoryStream();
            using var streamWriter = new BinaryWriter( streamMs );

            using var rawMs = new MemoryStream();
            using var rawWriter = new BinaryWriter( rawMs );

            var header = reader.ReadBytes( HeaderSize );
            var data = reader.ReadBytes( entry.DataLength );

            if( !PlainText ) {
                streamWriter.Write( header );
                streamWriter.Write( ScdUtils.XorDecodeFromTableHca( data, BlockSize, entry.DataLength, HeaderSize ) );
                streamWriter.BaseStream.Position = 0;
            }

            rawWriter.Write( header );
            rawWriter.Write( data );

            RawData = rawMs.ToArray();
            StreamData = PlainText ? RawData : streamMs.ToArray();
        }

        public override WaveStream GetStream() => new WaveFileReader( new HcaAudioStream( new MemoryStream( StreamData ), DecodeParams.Default ) );

        public override void Write( BinaryWriter writer ) {
            FileUtils.Pad( writer, 0x18 ); // TODO
            writer.Write( RawData );
        }

        public override int SamplesToBytes( int samples ) => 0; // TODO

        public override int TimeToBytes( float time ) => 0; // TODO

        public float BytesToTime( int bytes ) => 0f; // TODO

        public override Vector2 GetLoopTime() => new(); // TODO

        public override int GetSubInfoSize() => HeaderSize + 0x18;

        // ====================

        // TODO: importing
    }
}
