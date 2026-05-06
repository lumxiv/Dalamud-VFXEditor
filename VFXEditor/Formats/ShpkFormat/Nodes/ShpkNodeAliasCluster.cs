using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using VfxEditor.Parsing;
using VfxEditor.Ui.Interfaces;

namespace VfxEditor.Formats.ShpkFormat.Nodes {
    public class ShpkNodeAliasCluster : IUiItem {
        private uint SubView2;
        private uint SubView1;
        private uint Unk141E;

        private readonly List<ShpkNodeAliasSubCluster> SubClusters = [];

        public ShpkNodeAliasCluster( BinaryReader reader ) {
            SubView2 = reader.ReadUInt32();
            SubView1 = reader.ReadUInt32();
            var count = reader.ReadUInt32();
            Unk141E = reader.ReadUInt32();

            for( var i = 0; i < count; i++ ) {
                SubClusters.Add( new( reader ) );
            }
        }

        public void Write( BinaryWriter writer ) {
            writer.Write( SubView2 );
            writer.Write( SubView1 );
            writer.Write( SubClusters.Count );
            writer.Write( Unk141E );
            SubClusters.ForEach( x => x.Write( writer ) );
        }

        public void Draw() {

        }
    }
}
