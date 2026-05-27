using System.IO;
using VfxEditor.Formats.MtrlFormat.Stm.Entry;

namespace VfxEditor.Formats.MtrlFormat.Stm {
    public class StmFile : StmFileBase<StmEntry> {
        protected override StmEntry ParseEntry( BinaryReader reader, long offset ) => new( reader, offset );

        public override StmDyeData GetDye( int template, int idx ) => null; // TODO
    }
}
