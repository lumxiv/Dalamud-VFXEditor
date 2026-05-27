using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace VfxEditor.Formats.MtrlFormat.Stm.Entry {
    public abstract class StmEntryBase {
        public const int MAX = 254;

        public StmEntryBase( BinaryReader reader, long offset ) {
            reader.BaseStream.Position = offset;
        }

        protected static Triple ReadTriple( BinaryReader reader ) => new() {
            R = reader.ReadHalf(),
            G = reader.ReadHalf(),
            B = reader.ReadHalf(),
        };

        protected static Half ReadSingle( BinaryReader reader ) => reader.ReadHalf();

        protected static IReadOnlyList<T> Read<T>( BinaryReader reader, long start, int length, Func<BinaryReader, T> read ) {
            reader.BaseStream.Position = start;

            var entrySize = Marshal.SizeOf<T>();
            var entryCount = length / entrySize;

            return entryCount switch {
                0 => new RepeatingList<T>( default!, MAX ),
                1 => new RepeatingList<T>( read( reader ), MAX ),
                MAX => ReadStandard( reader, read ),
                < MAX => new IndexedList<T>( reader, entryCount - MAX / entrySize, read ),
                _ => null
            };
        }

        protected static List<T> ReadStandard<T>( BinaryReader reader, Func<BinaryReader, T> read ) {
            var ret = new List<T>();
            for( var i = 0; i < MAX; i++ ) ret.Add( read( reader ) );
            return ret;
        }
    }
}
