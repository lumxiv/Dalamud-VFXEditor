using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace VfxEditor.Ui.NodeGraphViewer {
    [Flags]
    public enum NodeInteractionFlags {
        None = 0,
        Handle = 1,
        Internal = 2,
        LockSelection = 4,      // requesting canvas to stop capture ALL nodes select/deselect/remove
        Edge = 8,
        RequestingEdgeConn = 16,
        UnrequestingEdgeConn = 32,
        RequestEdgeRemoval = 64,
        RequestSelectAllChild = 128
    }

    [Flags]
    public enum CanvasDrawFlags {
        None = 0,
        NoInteract = 1,
        NoCanvasInteraction = 2,
        NoNodeInteraction = 4,
        NoNodeDrag = 8,
        NoNodeSnap = 16,
        StateNodeDrag = 32,
        StateCanvasDrag = 64,
        NoCanvasDrag = 128,
        NoCanvasZooming = 256
    }

    public class NodeUtils {
        public static float DefaultFontScale { get; } = 1;

        public static void PushFontScale( float scale ) {
            ImGui.SetWindowFontScale( ImGui.GetFont().Scale * scale );
        }

        public static void PopFontScale() {
            ImGui.SetWindowFontScale( DefaultFontScale );
        }

        // https://stackoverflow.com/questions/5953552/how-to-get-the-closest-number-from-a-listint-with-linq
        public static float? GetClosestItem( float itemToCompare, List<float> items )
            => items.Count == 0
               ? null
               : items.Aggregate( ( x, y ) => Math.Abs( x - itemToCompare ) < Math.Abs( y - itemToCompare ) ? x : y );
        public static void AlignRight( float pTargetItemWidth, bool pConsiderScrollbar = false, bool pConsiderImguiPaddings = true ) {
            var tStyle = ImGui.GetStyle();
            var tPadding = ( pConsiderImguiPaddings ? tStyle.WindowPadding.X + tStyle.FramePadding.X : 0 )
                             + ( pConsiderScrollbar ? tStyle.ScrollbarSize : 0 );
            ImGui.SetCursorPosX( ImGui.GetWindowWidth() - pTargetItemWidth - tPadding );
        }

        public static float GetGreaterVal( float v1, float v2 ) => v1 > v2 ? v1 : v2;

        public static float GetSmallerVal( float v1, float v2 ) => v1 < v2 ? v1 : v2;

        public static bool IsLineIntersectRect( Vector2 a, Vector2 b, Area area ) {
            if( Math.Min( a.X, b.X ) > area.End.X ) return false;
            if( Math.Max( a.X, b.X ) < area.Start.X ) return false;
            if( Math.Min( a.Y, b.Y ) > area.End.Y ) return false;
            if( Math.Max( a.Y, b.Y ) < area.Start.Y ) return false;
            if( area.CheckPosIsWithin( a ) ) return true;
            if( area.CheckPosIsWithin( a ) ) return true;
            return true;
        }

        public static Vector4 AdjustTransparency( Vector4 pColor, float pTransparency ) => new( pColor.X, pColor.Y, pColor.Z, pTransparency );

        public static Vector4 RgbatoVec4( int R, int G, int B, int A ) => new( ( float )R / 255, ( float )G / 255, ( float )B / 255, ( float )A / 255 );

        internal class Colors {
            public static readonly Vector4 GenObj_BlueAction = new( 0.61f, 0.79f, 0.92f, 0.4f );
            public static readonly Vector4 NormalBar_Grey = RgbatoVec4( 165, 165, 165, 80 );
            public static readonly Vector4 NodeBg = RgbatoVec4( 49, 48, 49, 255 );
            public static readonly Vector4 NodeFg = RgbatoVec4( 148, 121, 74, 255 );
            public static readonly Vector4 NodeText = RgbatoVec4( 223, 211, 185, 255 );
            public static readonly Vector4 NodePack = RgbatoVec4( 157, 189, 99, 255 );
            public static readonly Vector4 NodeEdgeHighlightNeg = RgbatoVec4( 246, 132, 118, 255 );
            public static readonly Vector4 NodeGraphViewer_BackdropGrey = RgbatoVec4( 165, 165, 165, 255 );
            public static readonly Vector4 NodeGraphViewer_SnaplineGold = RgbatoVec4( 148, 121, 74, 255 );
            public static readonly Vector4 NodeNotifInfo = RgbatoVec4( 223, 211, 185, 255 );
            public static readonly Vector4 NodeNotifWarning = RgbatoVec4( 240, 160, 19, 255 );
            public static readonly Vector4 NodeNotifError = RgbatoVec4( 216, 148, 148, 255 );
        }
    }
}
