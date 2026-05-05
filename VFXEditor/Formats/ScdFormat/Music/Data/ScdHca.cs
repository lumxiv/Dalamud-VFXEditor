using DereTore.Exchange.Audio.HCA;
using NAudio.Wave;
using System.IO;
using System.Numerics;
using VfxEditor.Formats.ScdFormat.Utils;

namespace VfxEditor.ScdFormat.Music.Data {
    public class ScdHca : ScdAudioData {
        private byte[] StreamData;

        private byte[] Data;

        // TODO: CRC right before the end of the header + data
        /*
         * if you crc the whole header
                it'll give you 0
                (because they just crc everything up until the last 2 bytes, then whatever it returns, they write the last 2 bytes as it)
                (that's what that f8 2c is for)
         */

        public ScdHca( BinaryReader reader, ScdAudioEntry entry ) : base( entry ) {
            reader.ReadInt16(); // TODO
            var headerSize = reader.ReadInt16();
            var blockSize = reader.ReadInt16();
            reader.ReadBytes( 7 ); // TODO
            var plainText = reader.ReadBoolean();
            reader.ReadBytes( 10 );

            var ms = new MemoryStream();
            using var writer = new BinaryWriter( ms );

            writer.Write( reader.ReadBytes( headerSize ) ); // HCA header
            Data = reader.ReadBytes( entry.DataLength );
            writer.Write( ScdUtils.XorDecodeFromTableHca( Data, blockSize, entry.DataLength, headerSize ) );
            writer.BaseStream.Position = 0;

            StreamData = ms.ToArray();
        }

        public override WaveStream GetStream() => new WaveFileReader( new HcaAudioStream( new MemoryStream( StreamData ), DecodeParams.Default ) );

        public override void Write( BinaryWriter writer ) {
           // TODO
        }

        public override int SamplesToBytes( int samples ) => 0;

        public override int TimeToBytes( float time ) => 0;

        public float BytesToTime( int bytes ) => 0f;

        public override Vector2 GetLoopTime() => new();

        public override int GetSubInfoSize() => 0;
    }
}
