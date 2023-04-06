using Lumina.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VfxEditor.FileManager;
using VfxEditor.UldFormat.Headers;
using VfxEditor.UldFormat.Part;
using VfxEditor.UldFormat.Texture;

namespace VfxEditor.UldFormat
{
    public class UldFile : FileManagerFile {
        private readonly UldMainHeader Header;
        private readonly UldAtkHeader OffsetsHeader;
        private readonly UldAtkHeader2 OffsetsHeader2;

        private readonly UldListHeader AssetList;
        private readonly List<UldTexture> Assets = new();

        private readonly UldListHeader PartList;
        private readonly List<UldParts> Parts = new();

        private readonly UldListHeader ComponentList;

        private readonly UldListHeader TimelineList;

        private readonly UldListHeader WidgetList;

        // Plugin.UldManager.GetCopyManager()
        public UldFile( BinaryReader reader, bool checkOriginal = true ) : base( new CommandManager( null ) ) {
            var pos = reader.BaseStream.Position;
            Header = new( reader );

            var offsetsPosition = reader.BaseStream.Position;
            OffsetsHeader = new( reader );

            reader.Seek( offsetsPosition + OffsetsHeader.AssetOffset );
            AssetList = new( reader );
            for( var i = 0; i < AssetList.ElementCount; i++ ) Assets.Add( new( reader, AssetList.Version[3] ) );

            reader.Seek( offsetsPosition + OffsetsHeader.PartOffset );
            PartList = new( reader );
            for( var i = 0; i < PartList.ElementCount; i++ ) Parts.Add( new( reader ) );

            reader.Seek( offsetsPosition + OffsetsHeader.ComponentOffset );
            ComponentList = new( reader );
            // component data

            reader.Seek( offsetsPosition + OffsetsHeader.TimelineOffset );
            TimelineList = new( reader );
            // timeline data

            var offsetsPosition2 = reader.BaseStream.Position;
            reader.Seek( pos + Header.WidgetOffset );
            OffsetsHeader2 = new( reader );

            reader.Seek( offsetsPosition2 + OffsetsHeader2.WidgetOffset );
            WidgetList = new( reader );
            // widget data
        }

        public override void Write( BinaryWriter writer ) {
            var pos = writer.BaseStream.Position;
            Header.Write( writer, out var headerUpdatePosition );

            var offsetsPosition = writer.BaseStream.Position;
            OffsetsHeader.Write( writer, out var offsetsUpatePosition );

            // TODO: some of the Atk offsets can be zero

            var offsetsPosition2 = writer.BaseStream.Position;
            // TODO
            OffsetsHeader.Write( writer, out var offsetsUpdatePosition2 );

            // TODO: update header offsets
        }

        public override void Draw( string id ) {
            
        }
    }
}