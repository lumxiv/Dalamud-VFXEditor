using ImGuiNET;
using NAudio.Wave;
using System;
using Dalamud.Logging;
using Dalamud.Interface;
using ImGuiFileDialog;
using System.IO;
using System.Numerics;

namespace VfxEditor.ScdFormat {
    public class AudioPlayer {
        private readonly ScdSoundEntry Entry;
        private PlaybackState State => CurrentOutput == null ? PlaybackState.Stopped : CurrentOutput.PlaybackState;

        private WaveStream CurrentStream;
        private WaveChannel32 CurrentChannel;
        private WasapiOut CurrentOutput;

        private double TotalTime => CurrentStream == null ? 0 : CurrentStream.TotalTime.TotalSeconds - 0.1;
        private double CurrentTime => CurrentStream == null ? 0 : CurrentStream.CurrentTime.TotalSeconds;

        public AudioPlayer( ScdSoundEntry entry ) {
            Entry = entry;
        }

        public void Draw( string id, int idx ) {
            // Controls
            ImGui.PushFont( UiBuilder.IconFont );
            if( State == PlaybackState.Stopped ) {
                if( ImGui.Button( $"{( char )FontAwesomeIcon.Play}" + id ) ) {
                    Reset();
                    try {
                        var stream = Entry.Data.GetStream();
                        CurrentStream = stream.WaveFormat.Encoding switch {
                            WaveFormatEncoding.Pcm => WaveFormatConversionStream.CreatePcmStream( stream ),
                            WaveFormatEncoding.Adpcm => WaveFormatConversionStream.CreatePcmStream( stream ),
                            _ => stream
                        };

                        CurrentChannel = new WaveChannel32( CurrentStream ) {
                            Volume = 1f,
                            PadWithZeroes = false,
                        };
                        CurrentOutput = new WasapiOut();

                        CurrentOutput.Init( CurrentChannel );
                        CurrentOutput.Play();
                    }
                    catch( Exception e ) {
                        PluginLog.LogError( e, "Error playing sound" );
                    }
                }
            }
            else if( State == PlaybackState.Playing ) {
                if( ImGui.Button( $"{( char )FontAwesomeIcon.Pause}" + id ) ) CurrentOutput.Pause();
            }
            else if( State == PlaybackState.Paused ) {
                if( ImGui.Button( $"{( char )FontAwesomeIcon.Play}" + id ) ) CurrentOutput.Play();
            }

            ImGui.PopFont();

            if( State == PlaybackState.Stopped ) ImGui.PushStyleVar( ImGuiStyleVar.Alpha, 0.5f );
            var selectedTime = ( float )CurrentTime;
            ImGui.SameLine( 30f );
            ImGui.SetNextItemWidth( 150f );
            if( ImGui.SliderFloat( $"{id}-Drag", ref selectedTime, 0, ( float )TotalTime) ) {
                if( State != PlaybackState.Stopped && selectedTime > 0 && selectedTime < TotalTime ) {
                    CurrentOutput.Pause();
                    CurrentStream.CurrentTime = TimeSpan.FromSeconds( selectedTime );
                }
            }

            if( State == PlaybackState.Stopped ) ImGui.PopStyleVar();

            // Save
            ImGui.SameLine();
            ImGui.PushFont( UiBuilder.IconFont );
            if( ImGui.Button( $"{( char )FontAwesomeIcon.Download}" + id ) ) {
                if( IsVorbis ) ImGui.OpenPopup( "SavePopup" + id );
                else SaveWaveDialog();
            }
            ImGui.PopFont();

            if( ImGui.BeginPopup( "SavePopup" + id ) ) {
                if( ImGui.Selectable( ".wav" ) ) SaveWaveDialog();
                if( ImGui.Selectable( ".ogg" ) ) SaveOggDialog();
                ImGui.EndPopup();
            }

            // Import
            ImGui.SameLine();
            ImGui.PushFont( UiBuilder.IconFont );
            if( ImGui.Button( $"{( char )FontAwesomeIcon.Upload}" + id ) ) ImportDialog();
            ImGui.PopFont();

            ImGui.Indent( 30f );
            var loopStartEnd = new Vector2( Convert( Entry.LoopStart ), Convert( Entry.LoopEnd ) );
            ImGui.SetNextItemWidth( 150f );
            if( ImGui.InputFloat2( $"Loop{id}", ref loopStartEnd ) ) {
                Entry.LoopStart = ( int )Math.Clamp( loopStartEnd.X, 0, Convert( Entry.DataLength ) ) * Entry.SampleRate;
                Entry.LoopEnd = ( int )Math.Clamp( loopStartEnd.Y, 0, Convert( Entry.DataLength ) ) * Entry.SampleRate;
            }

            var totalTimeSpan = TimeSpan.FromSeconds( Entry.DataLength / Entry.SampleRate ).ToString( "c" );

            ImGui.Text( $"Index {idx}" );
            ImGui.SameLine();
            ImGui.TextDisabled( $"{Entry.Format} / {Entry.NumChannels} Ch [{totalTimeSpan}]" );

            ImGui.Unindent( 30f );

            ImGui.SetCursorPosY( ImGui.GetCursorPosY() + 5 );
        }

        private bool IsVorbis => Entry.Format == SscfWaveFormat.Vorbis;

        private float Convert( int byteValue ) => Entry.SampleRate == 0 ? 0 : byteValue / Entry.SampleRate;

        private void ImportDialog() {
            var text = IsVorbis ? "Audio files{.ogg,.wav},.*" : "Audio files{.wav},.*";
            FileDialogManager.OpenFileDialog( "Import File", text, ( bool ok, string res ) => {
                if( ok ) ScdFile.Import( res, Entry );
            } );
        }

        private void SaveWaveDialog() {
            FileDialogManager.SaveFileDialog( "Select a Save Location", ".wav", "ExportedSound", "wav", ( bool ok, string res ) => {
                if( ok ) {
                    using var stream = Entry.Data.GetStream();
                    WaveFileWriter.CreateWaveFile( res, stream );
                }
            } );
        }

        private void SaveOggDialog() {
            FileDialogManager.SaveFileDialog( "Select a Save Location", ".ogg", "ExportedSound", "ogg", ( bool ok, string res ) => {
                if( ok ) {
                    var data = ( ScdVorbis )Entry.Data;
                    File.WriteAllBytes( res, data.DecodedData );
                }
            } );
        }

        public void Reset() {
            CurrentOutput?.Dispose();
            CurrentChannel?.Dispose();
            CurrentStream?.Dispose();
        }

        public void Dispose() {
            CurrentOutput?.Stop();
            Reset();
        }
    }
}