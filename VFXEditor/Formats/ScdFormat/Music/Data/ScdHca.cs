using DereTore.Exchange.Audio.HCA;
using NAudio.Wave;
using System;
using System.IO;
using System.Numerics;
using VfxEditor.Formats.ScdFormat.Utils;
using VfxEditor.Utils;

namespace VfxEditor.ScdFormat.Music.Data {
    public class ScdHca : ScdAudioData {
        // TODO: how is looping handled?

        // TODO: CRC right before the end of the header + data
        /*
            if you crc the whole header
            it'll give you 0
            (because they just crc everything up until the last 2 bytes, then whatever it returns, they write the last 2 bytes as it)
            (that's what that f8 2c is for)
         */

        private readonly byte[] StreamData; // Decoded
        private readonly byte[] RawData; // What will be written, can be the same if there's no encryption

        private readonly short HeaderSize;
        private readonly int BlockSize;
        private readonly bool PlainText;

        private readonly DecodeParams DecodeParams = DecodeParams.Default;
        private readonly HcaInfo HcaInfo;
        private uint SamplesPerBlock => HcaInfo.ChannelCount * 0x80 * 8;

        private byte[] Unk1 = [ 0x20, 0x18 ];
        private byte[] Unk2 = [ 0x00, 0x00, 0x00, 0x00, 0x80, 0x00, 0x00 ];
        private byte[] Unk3 = [0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 ];

        public ScdHca( BinaryReader reader, ScdAudioEntry entry ) : base( entry ) {
            Unk1 = reader.ReadBytes(2); // TODO
            HeaderSize = reader.ReadInt16();
            BlockSize = reader.ReadInt16();
            Unk2 = reader.ReadBytes( 7 ); // TODO
            PlainText = reader.ReadBoolean();
            Unk3 = reader.ReadBytes( 10 ); // TODO

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

            using var hcaMs = new MemoryStream( StreamData );
            using var decoder = new HcaDecoder( hcaMs, DecodeParams );
            HcaInfo = decoder.HcaInfo;
        }

        public override WaveStream GetStream() => new WaveFileReader( new HcaAudioStream( new MemoryStream( StreamData ), DecodeParams ) );

        public override void Write( BinaryWriter writer ) {
            writer.Write( Unk1 );
            writer.Write( HeaderSize );
            writer.Write( (short) BlockSize );
            writer.Write( Unk2 );
            writer.Write( PlainText );
            writer.Write( Unk3 );

            writer.Write( RawData );
        }

        public override int SamplesToBytes( int samples ) {
            var targetBlock = (int) Math.Round( (float) samples / SamplesPerBlock );
            return ( int )( targetBlock * HcaInfo.BlockSize );
        }

        public override int TimeToBytes( float time ) {
            var samples = time * HcaInfo.SamplingRate;
            return SamplesToBytes( ( int )samples  );
        }

        public float BytesToTime( int bytes ) {
            var blockCount = (float) bytes / HcaInfo.BlockSize;
            var samples = blockCount * SamplesPerBlock;
            return samples / HcaInfo.SamplingRate;
        }

        public override Vector2 GetLoopTime() => new( BytesToTime( Entry.LoopStart ), BytesToTime( Entry.LoopEnd ) );

        public override int GetSubInfoSize() => HeaderSize + 0x18;

        // ====================

        // TODO: importing
    }
}
