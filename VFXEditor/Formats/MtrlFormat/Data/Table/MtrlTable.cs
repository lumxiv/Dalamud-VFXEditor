using System.IO;
using VfxEditor.Formats.MtrlFormat.Data.Color;

namespace VfxEditor.Formats.MtrlFormat.Data.Table {
    public class MtrlTable : MtrlTableBase<MtrlColorRow> {
        public MtrlTable( MtrlFile file ) : base( file ) { }

        public MtrlTable( MtrlFile file, BinaryReader reader, long dataEnd ) : base( file, reader, dataEnd ) { }

        protected override DyeTableSize GetDyeTableSize() => DyeTableSize.Extended;

        protected override MtrlColorRow GetNew() => new( File, this );

        protected override int GetRowCount() => 32;
    }
}
