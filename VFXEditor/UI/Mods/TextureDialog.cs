using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dalamud.Interface;
using Dalamud.Plugin;
using ImGuiNET;
using VFXEditor.UI.VFX;

namespace VFXEditor.UI {
    public class TextureDialog : GenericDialog {
        public TextureDialog( Plugin plugin ) : base( plugin, "Imported Textures" ) {
            Size = new Vector2( 500, 400 );
        }

        public override void OnDraw() {
            var id = "##ImportTex";

            ImGui.BeginChild( id + "/Child", new Vector2( 0, 0 ), true );
            ImGui.Columns( 3, id + "/Columns", true );
            ImGui.SetColumnWidth( 1, 100 );
            ImGui.SetColumnWidth( 2, 100 );

            foreach(var path in _plugin.Manager.TexManager.GamePathReplace.Keys ) {
                ImGui.Text( path );
                if(ImGui.IsItemHovered() && Configuration.Config.PreviewTextures && _plugin.Manager.TexManager.PathToTex.ContainsKey( path ) ) {
                    var t = _plugin.Manager.TexManager.PathToTex[path];
                    ImGui.Image( t.Wrap.ImGuiHandle, new Vector2( t.Width, t.Height ) );
                }
            }

            ImGui.NextColumn();
            foreach( var item in _plugin.Manager.TexManager.GamePathReplace.Values ) {
                ImGui.Text( $"({item.Format})" );
            }

            int idx = 0;
            ImGui.NextColumn();
            foreach( KeyValuePair<string, TexReplace> entry in _plugin.Manager.TexManager.GamePathReplace ) {
                if(UIUtils.RemoveButton("Remove" + id + idx, small: true ) ) {
                    _plugin.Manager.TexManager.RemoveReplace( entry.Key );
                    _plugin.Manager.TexManager.RefreshPreview( entry.Key );
                }
                idx++;
            }

            ImGui.Columns( 1 );
            ImGui.EndChild();
        }
    }
}
