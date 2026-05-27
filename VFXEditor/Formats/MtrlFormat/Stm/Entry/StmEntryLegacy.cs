using System;
using System.Collections.Generic;
using System.IO;

namespace VfxEditor.Formats.MtrlFormat.Stm.Entry {
    public class StmEntryLegacy : StmEntryBase {
        public readonly IReadOnlyList<Triple> Diffuse;
        public readonly IReadOnlyList<Triple> Specular;
        public readonly IReadOnlyList<Triple> Emissive;
        public readonly IReadOnlyList<Half> Gloss;
        public readonly IReadOnlyList<Half> SpecularMask;

        public StmEntryLegacy( BinaryReader reader, long offset ) : base( reader, offset ) {
            var diffuseEnd = reader.ReadUInt16() * 2;
            var specularEnd = reader.ReadUInt16() * 2;
            var emissiveEnd = reader.ReadUInt16() * 2;

            var glossEnd = reader.ReadUInt16() * 2;
            var powerEnd = reader.ReadUInt16() * 2;

            var startPos = reader.BaseStream.Position;

            Diffuse = Read( reader, startPos, diffuseEnd, ReadTriple );
            Specular = Read( reader, startPos + diffuseEnd, specularEnd - diffuseEnd, ReadTriple );
            Emissive = Read( reader, startPos + specularEnd, emissiveEnd - specularEnd, ReadTriple );

            Gloss = Read( reader, startPos + emissiveEnd, glossEnd - emissiveEnd, ReadSingle );
            SpecularMask = Read( reader, startPos + glossEnd, powerEnd - glossEnd, ReadSingle );
        }
    }
}
