using AVFXLib.Models;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VFXEditor.UI.VFX
{
    public class UIParticleDataPolygon : UIData {
        public AVFXParticleDataPolygon Data;
        public List<UIBase> Attributes = new List<UIBase>();
        //==========================

        public UIParticleDataPolygon(AVFXParticleDataPolygon data)
        {
            Data = data;
            //=======================
            Attributes.Add( new UICurve( Data.Count, "count" ) );
        }

        public override void Draw(string parentId)
        {
            string id = parentId + "/Data";
            DrawList( Attributes, id );
        }
    }
}
