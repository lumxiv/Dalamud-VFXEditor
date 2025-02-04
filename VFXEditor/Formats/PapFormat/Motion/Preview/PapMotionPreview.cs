using VfxEditor.FileBrowser;
using VfxEditor.PapFormat;
using VfxEditor.PapFormat.Motion;

namespace VfxEditor.Formats.PapFormat.Motion.Preview {
    public abstract class PapMotionPreview {
        public readonly PapMotion Motion;

        public PapMotionPreview( PapMotion motion ) {
            Motion = motion;
        }

        public abstract void Draw( int idx );

        // ======== IMPORT EXPORT =========

        protected void ExportDialog( string animationName ) {
            FileBrowserManager.SaveFileDialog( "Select a Save Location", ".gltf", "motion", "gltf", ( bool ok, string res ) => {
                if( !ok ) return;
                Plugin.AddModal( new PapGltfExportModal( Motion, animationName, res ) );
            } );
        }

        protected void ImportDialog( int idx ) {
            FileBrowserManager.OpenFileDialog( "Select a File", "Motion{.hkx,.gltf,.glb},.*", ( bool ok, string res ) => {
                if( !ok ) return;
                if( res.Contains( ".hkx" ) ) {
                    Plugin.AddModal( new PapReplaceModal( Motion, idx, res ) );
                }
                else {
                    var modal = new PapGltfImportModal( Motion, idx, res );
                    if( modal.AnimationCount <= 0 )
                    {
                        Dalamud.ErrorNotification( "No animations found in this file." );
                    } else
                    {
                        Plugin.AddModal( modal );
                    }
                }
            } );
        }
    }
}
