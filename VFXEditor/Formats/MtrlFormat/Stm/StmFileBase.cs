using Lumina.Data;
using System.Collections.Generic;
using System.IO;
using VfxEditor.Formats.MtrlFormat.Stm.Entry;

namespace VfxEditor.Formats.MtrlFormat.Stm {
    //  https://github.com/Ottermandias/Penumbra.GameData/blob/7cd96c18d06fa41f3a990021f01714a00e1f2843/Files/StmFile.cs#L29

    public abstract class StmFileBase<T> : FileResource where T : StmEntryBase {
        public bool IsLegacy { get; private set; } = false;

        public readonly Dictionary<int, T> Entries = [];

        public int[] Templates { get; private set; }

        public override void LoadFile() {
            Reader.BaseStream.Position = 0;

            Reader.ReadUInt16(); // magic
            var version = Reader.ReadUInt16();
            var numEntries = Reader.ReadUInt16();
            Reader.ReadByte(); // numColors
            Reader.ReadByte(); // numScalars

            switch( version ) {
                case 0x0101:
                    IsLegacy = true;
                    break;
            }

            var keys = new List<uint>();
            var offsets = new List<uint>();

            for( var i = 0; i < numEntries; i++ ) keys.Add( Reader.ReadUInt32() );
            for( var i = 0; i < numEntries; i++ ) offsets.Add( Reader.ReadUInt32() );

            var startPos = Reader.BaseStream.Position;
            var templates = new List<int> { 0 };
            for( var i = 0; i < numEntries; i++ ) {
                Entries[(int)keys[i]] = ParseEntry( Reader, ( offsets[i] * 2 ) + startPos );
                templates.Add( ( int )keys[i] );
            }
            Templates = [.. templates];
        }

        protected abstract T ParseEntry( BinaryReader reader, long offset );

        public abstract StmDyeData GetDye( int template, int idx );
    }
}
