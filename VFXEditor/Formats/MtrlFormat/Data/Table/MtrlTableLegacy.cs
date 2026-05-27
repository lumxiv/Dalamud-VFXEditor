using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using System.Collections.Generic;
using System.IO;
using VfxEditor.Formats.MtrlFormat.Data.Color;

namespace VfxEditor.Formats.MtrlFormat.Data.Table {
    public class MtrlTableLegacy : MtrlTableBase<MtrlColorRowLegacy> {
        public MtrlTableLegacy( MtrlFile file ) : base( file ) { }

        public MtrlTableLegacy( MtrlFile file, BinaryReader reader, long dataEnd ) : base( file, reader, dataEnd ) { }

        protected override DyeTableSize GetDyeTableSize() => DyeTableSize.Legacy;

        protected override MtrlColorRowLegacy GetNew() => new( File, this );

        protected override int GetRowCount() => 16;
    }
}
