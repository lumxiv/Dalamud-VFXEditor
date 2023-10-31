using FFXIVClientStructs.Havok;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using VfxEditor.FileBrowser;
using VfxEditor.FileManager;
using VfxEditor.Interop;
using VfxEditor.Interop.Havok;
using VfxEditor.Interop.Structs.Animation;
using VfxEditor.Ui.Components;
using static FFXIVClientStructs.Havok.hkBaseObject;

namespace VfxEditor.SklbFormat.Mapping {
    public class SklbMappingDropdown : Dropdown<SklbMapping> {
        private readonly SklbFile File;

        public SklbMappingDropdown( SklbFile file, List<SklbMapping> items ) : base( "Mappings", items, true, true ) {
            File = file;
        }

        protected override string GetText( SklbMapping item, int idx ) => $"Mapping {idx}" + ( string.IsNullOrEmpty( item.Name.Value ) ? "" : $" ({item.Name.Value})" );

        protected override unsafe void OnNew() {
            FileBrowserManager.OpenFileDialog( "Select a Skeleton", "Skeleton{.hkx,.sklb},.*", ( ok, res ) => {
                if( !ok ) return;

                var hkxPath = res;
                if( res.EndsWith( ".sklb" ) ) {
                    SimpleSklb.LoadFromLocal( res ).SaveHavokData( SklbMapping.TempMappingHkx );
                    hkxPath = SklbMapping.TempMappingHkx;
                }

                var havokData = new HavokBones( hkxPath, true );

                var mapper = ( SkeletonMapper* )Marshal.AllocHGlobal( Marshal.SizeOf( typeof( SkeletonMapper ) ) );
                File.Handles.Add( ( nint )mapper );
                mapper->hkReferencedObject.MemSizeAndRefCount = 0;
                mapper->hkReferencedObject.hkBaseObject.vfptr = ( hkBaseObjectVtbl* )ResourceLoader.HavokMapperVtbl;
                mapper->Mapping.SkeletonA = new() {
                    ptr = havokData.Skeleton
                };
                mapper->Mapping.SkeletonB = new() {
                    ptr = File.Bones.Skeleton
                };
                mapper->Mapping.PartitionMap = HavokData.CreateArray<short>( File.Handles, null );
                mapper->Mapping.SimpleMappingPartitionRanges = HavokData.CreateArray<PartitionMappingRange>( File.Handles, null );
                mapper->Mapping.ChainMappingPartitionRanges = HavokData.CreateArray<PartitionMappingRange>( File.Handles, null );
                mapper->Mapping.SimpleMappings = HavokData.CreateArray( File.Handles, new List<SimpleMapping>() );
                mapper->Mapping.ChainMappings = HavokData.CreateArray<ChainMapping>( File.Handles, null );
                mapper->Mapping.UnmappedBones = HavokData.CreateArray<short>( File.Handles, null );

                mapper->Mapping.ExtractedMotionMapping = new hkQsTransformf() {
                    Translation = new() {
                        X = 0,
                        Y = 0,
                        Z = 0
                    },
                    Rotation = new() {
                        X = 0,
                        Y = 0,
                        Z = 0,
                        W = 1
                    },
                    Scale = new() {
                        X = 1,
                        Y = 1,
                        Z = 1,
                    }
                };

                mapper->Mapping.KeepUnmappedLocal = 1;
                mapper->Mapping.Type = new hkEnum<MappingType, int>() {
                    Storage = 1
                };

                CommandManager.Add( new GenericAddCommand<SklbMapping>( Items, new SklbMapping( File.Bones, mapper, "hkaSkeletonMapper" ) ) );
            } );
        }

        protected override void OnDelete( SklbMapping item ) => CommandManager.Add( new GenericRemoveCommand<SklbMapping>( Items, item ) );

        protected override void DrawSelected() => Selected.Draw( Items.IndexOf( Selected ) );
    }
}
