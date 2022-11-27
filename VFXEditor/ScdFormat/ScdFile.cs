using Dalamud.Logging;
using ImGuiNET;
using NAudio.Vorbis;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using VfxEditor.FileManager;
using VfxEditor.Utils;

namespace VfxEditor.ScdFormat {
    public class ScdFile : FileManagerFile {
        private readonly ScdHeader Header;
        private readonly ScdOffsetsHeader OffsetsHeader;
        private readonly byte[] PreSoundData;

        public List<ScdSoundEntry> Music = new();

        public ScdFile( BinaryReader reader, bool checkOriginal = true ) {
            var original = checkOriginal ? FileUtils.GetOriginal( reader ) : null;

            Header = new( reader );
            OffsetsHeader = new( reader );

            var savePos = reader.BaseStream.Position;

            reader.BaseStream.Seek( OffsetsHeader.OffsetSound, SeekOrigin.Begin );
            List<int> soundOffsets = new();
            for( var i = 0; i < OffsetsHeader.CountSound; i++ ) {
                var offset = reader.ReadInt32();
                if( offset == 0 ) continue;
                soundOffsets.Add( offset );
                Music.Add( new ScdSoundEntry( reader, offset ) );
            }

            reader.BaseStream.Seek(savePos, SeekOrigin.Begin );
            PreSoundData = reader.ReadBytes( ( int )( soundOffsets[0] - savePos ) );

            PluginLog.Log( $"Diff: {reader.BaseStream.Length - Header.FileSize:X8}" );

            if( checkOriginal ) Verified = FileUtils.CompareFiles( original, ToBytes(), out var _ );
        }

        public override void Draw( string id ) {
            if( ImGui.BeginTabBar( $"{id}-MainTabs", ImGuiTabBarFlags.NoCloseWithMiddleMouseButton ) ) {
                if( ImGui.BeginTabItem( $"Sounds{id}" ) ) {
                    DrawSounds( id );
                    ImGui.EndTabItem();
                }
                ImGui.EndTabBar();
            }
        }

        private void DrawSounds( string id ) {
            if( ImGui.Checkbox( $"Loop Music{id}", ref Plugin.Configuration.LoopMusic ) ) Plugin.Configuration.Save();
            if( ImGui.Checkbox( $"Loop Sound Effects{id}", ref Plugin.Configuration.LoopSoundEffects ) ) Plugin.Configuration.Save();
            ImGui.Separator();
            ImGui.BeginChild( $"{id}-Child" );
            ImGui.SetCursorPosY( ImGui.GetCursorPosY() + 3 );
            for( var idx = 0; idx < Music.Count; idx++ ) {
                Music[idx].Draw( id + idx, idx );
            }
            ImGui.EndChild();
        }

        public override void Write( BinaryWriter writer ) {
            Header.Write( writer );
            OffsetsHeader.Write( writer );
            writer.Write( PreSoundData );

            List<int> musicPositions = new();
            long paddingSubtract = 0;
            foreach( var  music in Music ) {
                musicPositions.Add( ( int )writer.BaseStream.Position );
                music.Write( writer, out var padding );
                paddingSubtract += padding;
            }

            writer.BaseStream.Seek( OffsetsHeader.OffsetSound, SeekOrigin.Begin );
            foreach( var position in musicPositions ) {
                writer.Write( position );
            }

            PluginLog.Log( $"Padding: {paddingSubtract:X8}" );
            var paddingMod = paddingSubtract % 16;
            if( paddingMod > 0 ) paddingSubtract -= paddingMod;

            ScdHeader.UpdateFileSize( writer, paddingSubtract ); // end with this
        }

        public void Replace( ScdSoundEntry old, ScdSoundEntry newEntry ) {
            var index = Music.IndexOf( old );
            if( index == -1 ) return;
            Music.Remove( old );
            Music.Insert( index, newEntry );
        }

        public override void Dispose() => Music.ForEach( x => x.Dispose() );

        public async static void Import( string path, ScdSoundEntry music ) {
            await Task.Run( () => {
                if( music.Format == SscfWaveFormat.Vorbis ) {
                    var ext = Path.GetExtension( path );
                    if( ext == ".wav" ) ScdVorbis.ImportWav( path, music );
                    else ScdVorbis.ImportOgg( path, music );
                }
                else ScdAdpcm.Import( path, music );
            } );
        }
    }
}
