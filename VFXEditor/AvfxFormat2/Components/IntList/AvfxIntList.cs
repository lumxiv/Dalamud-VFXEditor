using ImGuiNET;
using System;
using System.Collections.Generic;
using System.IO;

namespace VfxEditor.AvfxFormat2 {
    public class AvfxIntList : AvfxDrawable {
        public readonly string Name;

        private int Size;
        private readonly List<int> Value = new() { 0 };

        public AvfxIntList( string name, string avfxName, int size = 1 ) : base( avfxName ) {
            Name = name;
            Size = size;
        }

        public List<int> GetValue() => Value;

        public void SetValue( List<int> value ) {
            SetAssigned( true );
            Value.Clear();
            Value.AddRange( value );
            Size = Value.Count;
        }

        public void SetValue( int value ) {
            SetValue( new List<int> { value } );
        }

        public void SetValue( int value, int idx ) {
            SetAssigned( true );
            Value[idx] = value;
        }

        public void AddItem( int item ) {
            SetAssigned( true );
            Size++;
            Value.Add( item );
        }

        public void RemoveItem( int idx ) {
            SetAssigned( true );
            Size--;
            Value.Remove( idx );
        }

        public override void ReadContents( BinaryReader reader, int size ) {
            Size = size;
            Value.Clear();
            for( var i = 0; i < Size; i++ ) Value.Add( reader.ReadByte() );
        }

        protected override void RecurseChildrenAssigned( bool assigned ) { }

        protected override void WriteContents( BinaryWriter writer ) {
            foreach( var item in Value ) writer.Write( ( byte )item );
        }

        public override void Draw( string id ) {
            // Unassigned
            AssignedCopyPaste( this, Name );
            if( DrawAddButton( this, Name, id ) ) return;

            var value = Value[0];
            if( ImGui.InputInt( Name + id, ref value ) ) {
                CommandManager.Avfx.Add( new AvfxIntListCommand( this, value ) );
            }

            DrawRemoveContextMenu( this, Name, id );
        }
    }
}
