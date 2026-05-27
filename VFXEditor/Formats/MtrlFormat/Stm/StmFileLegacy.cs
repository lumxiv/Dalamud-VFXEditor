using System;
using System.IO;
using VfxEditor.Formats.MtrlFormat.Stm.Entry;

namespace VfxEditor.Formats.MtrlFormat.Stm {
    public class StmFileLegacy : StmFileBase<StmEntryLegacy> {
        protected override StmEntryLegacy ParseEntry( BinaryReader reader, long offset ) => new( reader, offset );

        public override StmDyeData GetDye( int template, int idx ) {
            if( !Entries.TryGetValue( ( ushort )template, out var entry ) ) return null;
            if( idx <= 0 || idx > StmEntry.MAX ) return null;
            idx--;

            var diffuse = entry.Diffuse[idx];
            var specular = entry.Specular[idx];
            var emissive = entry.Emissive[idx];
            var gloss = entry.Gloss[idx];
            var power = entry.SpecularMask[idx];

            return new() {
                Diffuse = new( ( float )diffuse.R, ( float )diffuse.G, ( float )diffuse.B ),
                Specular = new( ( float )specular.R, ( float )specular.G, ( float )specular.B ),
                Emissive = new( ( float )emissive.R, ( float )emissive.G, ( float )emissive.B ),
                Gloss = ( float )gloss,
                Power = ( float )power
            };
        }
    }
}
