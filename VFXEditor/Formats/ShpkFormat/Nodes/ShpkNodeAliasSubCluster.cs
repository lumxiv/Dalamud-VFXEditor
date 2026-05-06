using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using VfxEditor.Parsing;
using VfxEditor.Ui.Interfaces;

namespace VfxEditor.Formats.ShpkFormat.Nodes {
    public class ShpkNodeAliasSubCluster : IUiItem {
        private const int DataCapacity = 97;

        private ParsedShort OwnIndex = new( "Own Index" );

        private ushort AliasCount; // TODO
        private byte[] Data;

        public ShpkNodeAliasSubCluster( BinaryReader reader ) {
            OwnIndex.Read( reader );
            AliasCount = reader.ReadUInt16();
            Data = reader.ReadBytes( DataCapacity * 4 );
        }

        public void Write( BinaryWriter writer ) {
            OwnIndex.Write( writer );
            writer.Write( AliasCount );
            writer.Write( Data );
        }

        public void Draw() {
            OwnIndex.Draw();
            // TODO
        }
    }
}
