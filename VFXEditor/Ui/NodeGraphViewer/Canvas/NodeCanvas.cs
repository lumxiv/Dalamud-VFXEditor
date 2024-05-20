using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using VfxEditor.Ui.NodeGraphViewer.Nodes;
using VfxEditor.Ui.NodeGraphViewer.Utils;

namespace VfxEditor.Ui.NodeGraphViewer.Canvas {
    public enum Direction {
        N = 0,
        NE = 1,
        E = 2,
        SE = 3,
        S = 4,
        SW = 5,
        W = 6,
        NW = 7,
        None = 8
    }

    public enum EdgeEndpointOption {
        None = 0,
        Source = 1,
        Target = 2,
        Either = 3
    }

    public class NodeCanvas {
        public const float MinScale = 0.1f;
        public const float MaxScale = 2f;
        public const float StepScale = 0.1f;

        public int Id;
        public string Name;
        private readonly NodeMap Map = new();
        private readonly OccupiedRegion Region;

        public readonly List<Node> Nodes = [];

        private NodeCanvasConfig Config { get; set; } = new();

        private bool NodeBeingDragged = false;
        private readonly HashSet<Node> SelectedNodes = [];
        private readonly LinkedList<Node> NodeRenderZOrder = new();

        private Node SnappingNode = null;
        private Vector2? LastSnapDelta = null;
        private FirstClickType FirstClickInDrag = FirstClickType.None;
        private bool IsFirstFrameAfterLmbDown = true;      // specifically for Draw()
        private Vector2? SelectAreaPosition = null;
        private Slot PendingConnection;

        public NodeCanvas( int pId, string pName = null ) {
            Id = pId;
            Name = pName ?? $"Canvas {Id}";
            Region = new();
        }

        public float GetScaling() => Config.Scaling;

        public void SetScaling( float pScale ) => Config.Scaling = pScale;

        public Vector2 GetBaseOffset() => Map.GetBaseOffset();

        private void AddNode( Node node, Vector2 pDrawRelaPos ) {
            // add node
            try {
                if( !Region.IsUpdatedOnce() ) Region.Update( Nodes, Map );
                NodeRenderZOrder.AddLast( node );
                Map.AddNode( node, pDrawRelaPos );
                Nodes.Add( node );
            }
            catch( Exception e ) { Dalamud.Error( e.Message ); }

            Region.Update( Nodes, Map );
        }

        public void AddNodeWithinView( Node node, Vector2 pViewerSize ) {
            var tOffset = Map.GetBaseOffset();
            Area pRelaAreaToScanForAvailableRegion = new(
                    -tOffset - pViewerSize * 0.5f,
                    pViewerSize * 0.95f              // only get up until 0.8 of the screen to avoid the new node going out of viewer
                );
            AddNode( node, Region.GetAvailableRelaPos( pRelaAreaToScanForAvailableRegion ) );
        }

        public Node GetNode( int id ) => Nodes.FirstOrDefault( x => x.Id == id );

        public IEnumerable<Node> GetChildren( Node node ) => Nodes.Where( x => x.ChildOf( node ) );

        public void AddNodeAdjacent( Node node, Node parent, Vector2? pOffset = null ) {
            Vector2 relativePosition;
            float? chosenY = null;
            Node chosenNode = null;

            foreach( var child in GetChildren( parent ) ) {
                var childPos = Map.GetNodeRelaPos( child );
                if( !childPos.HasValue ) continue;
                if( !chosenY.HasValue || childPos.Value.Y > chosenY ) {
                    chosenY = childPos.Value.Y;
                    chosenNode = child ?? chosenNode;
                }
            }

            // Calc final draw pos
            if( chosenNode == null ) relativePosition = ( Map.GetNodeRelaPos( parent ) ?? Vector2.One ) + new Vector2( parent.Style.GetSize().X, 0 ) + ( pOffset ?? Vector2.One );
            else {
                relativePosition = new(
                        ( ( Map.GetNodeRelaPos( parent ) ?? Vector2.One ) + new Vector2( parent.Style.GetSize().X, 0 ) + ( pOffset ?? Vector2.One ) ).X,
                        ( Map.GetNodeRelaPos( chosenNode ) ?? Map.GetNodeRelaPos( parent ) ?? Vector2.One ).Y + chosenNode.Style.GetSize().Y + ( pOffset ?? Vector2.One ).Y
                    );
            }

            AddNode( node, relativePosition );
        }

        public void RemoveNode( Node node ) {
            node.Dispose();

            Map.RemoveNode( node );
            Nodes.Remove( node );
            SelectedNodes.Remove( node );
            NodeRenderZOrder.Remove( node );
            Region.Update( Nodes, Map );
            Nodes.ForEach( x => x.Slots.Where( y => y.Connected == node ).ToList().ForEach( z => z.Clear() ) );
        }

        public bool FocusOnNode( Node node, Vector2? pExtraOfs = null ) => Map.FocusOnNode( node, ( pExtraOfs ?? new Vector2( -90, -90 ) ) * GetScaling() );

        public void MoveCanvas( Vector2 pDelta ) => Map.AddBaseOffset( pDelta );

        public void MarkUnneedInitOfs() => Map.MarkUnneedInitOfs();

        public NodeInputProcessResult ProcessInputOnNode( Node node, Vector2 nodePosition, InputPayload pInputPayload, bool pReadClicks ) {
            var tIsNodeHandleClicked = false;
            var tIsNodeClicked = false;
            var tIsCursorWithin = node.Style.CheckPosWithin( nodePosition, Config.Scaling, pInputPayload.MousePos );
            var tIsCursorWithinHandle = node.Style.CheckWithinHandle( nodePosition, Config.Scaling, pInputPayload.MousePos );
            var tIsMarkedForSelect = false;
            var tIsMarkedForDeselect = false;
            var tIsReqqingClearSelect = false;
            var tIsEscapingMultiselect = false;
            var tFirstClick = FirstClickType.None;
            var tCDFRes = CanvasDrawFlags.None;

            // Process node select (on lmb release)
            if( pReadClicks && !tIsNodeHandleClicked && pInputPayload.IsMouseLmb ) {
                if( tIsCursorWithinHandle ) {
                    tIsNodeClicked = true;
                    tIsNodeHandleClicked = true;
                    // single-selecting a node and deselect other node (while in multiselecting)
                    if( !pInputPayload.IsKeyCtrl && !pInputPayload.IsALmbDragRelease && SelectedNodes.Count > 1 ) {
                        tIsEscapingMultiselect = true;
                        //pReadClicks = false;
                        tIsReqqingClearSelect = true;
                        tIsMarkedForSelect = true;
                    }
                }
                else if( tIsCursorWithin ) {
                    tIsNodeClicked = true;
                }
            }
            // Process node holding and dragging, except for when multiselecting
            if( pInputPayload.IsMouseLmbDown )          // if mouse is hold, and the holding's first pos is within a selected node
            {                                           // then mark state as being dragged
                                                        // as long as the mouse is hold, even if mouse then moving out of node zone
                                                        // First click in drag
                if( !NodeBeingDragged && IsFirstFrameAfterLmbDown ) {
                    if( tIsCursorWithin ) {
                        if( tIsCursorWithinHandle )
                            tFirstClick = FirstClickType.Handle;
                        else
                            tFirstClick = FirstClickType.Body;
                    }
                }

                if( !NodeBeingDragged
                    && tFirstClick != FirstClickType.None
                    && !tIsNodeHandleClicked ) {
                    if( tFirstClick == FirstClickType.Handle ) {
                        tIsNodeHandleClicked = true;
                        // multi-selecting
                        if( pInputPayload.IsKeyCtrl ) {
                            // select (should be true, regardless of node's select status)
                            tIsMarkedForSelect = true;
                            // remove (process selecting first, then deselecting the node)
                            if( SelectedNodes.Contains( node ) )
                                tIsMarkedForDeselect = true;
                        }
                        // single-selecting new node
                        else if( !pInputPayload.IsKeyCtrl )     // don't check if node is alrady selected here
                        {
                            SnappingNode = node;
                            if( !SelectedNodes.Contains( node ) ) tIsReqqingClearSelect = true;
                            tIsMarkedForSelect = true;
                        }
                    }
                    else if( tFirstClick == FirstClickType.Body ) {
                        tIsNodeClicked = true;
                    }
                }

                // determine node drag
                if( !NodeBeingDragged
                    && FirstClickInDrag == FirstClickType.Handle
                    && !pInputPayload.IsKeyCtrl
                    && !pInputPayload.IsKeyShift ) {
                    if( pInputPayload.LmbDragDelta != null ) {
                        NodeBeingDragged = true;
                    }
                }
            }
            else {
                NodeBeingDragged = false;
                SnappingNode = null;
            }

            if( tIsCursorWithin ) tCDFRes |= CanvasDrawFlags.NoCanvasZooming;

            NodeInputProcessResult tRes = new() {
                IsNodeHandleClicked = tIsNodeHandleClicked,
                ReadClicks = pReadClicks,
                IsNodeClicked = tIsNodeClicked,
                FirstClick = tFirstClick,
                CDFRes = tCDFRes,
                IsWithin = tIsCursorWithin,
                IsWithinHandle = tIsCursorWithinHandle,
                IsMarkedForSelect = tIsMarkedForSelect,
                IsMarkedForDeselect = tIsMarkedForDeselect,
                IsReqqingClearSelect = tIsReqqingClearSelect,
                IsEscapingMultiselect = tIsEscapingMultiselect
            };

            return tRes;
        }
        public CanvasDrawFlags ProcessInputOnCanvas( InputPayload pInputPayload, CanvasDrawFlags pCanvasDrawFlagIn ) {
            var pCanvasDrawFlags = CanvasDrawFlags.None;
            // Mouse drag
            if( pInputPayload.LmbDragDelta.HasValue ) {
                Map.AddBaseOffset( pInputPayload.LmbDragDelta.Value / Config.Scaling );
                pCanvasDrawFlags |= CanvasDrawFlags.StateCanvasDrag;
            }
            // Mouse wheel zooming
            if( !pCanvasDrawFlagIn.HasFlag( CanvasDrawFlags.NoCanvasZooming ) ) {
                switch( pInputPayload.MouseWheelValue ) {
                    case 1:
                        Config.Scaling += NodeCanvas.StepScale;
                        pCanvasDrawFlags |= CanvasDrawFlags.StateCanvasDrag;
                        break;
                    case -1:
                        Config.Scaling -= NodeCanvas.StepScale;
                        pCanvasDrawFlags |= CanvasDrawFlags.StateCanvasDrag;
                        break;
                };
            }
            return pCanvasDrawFlags;
        }

        public CanvasDrawFlags Draw(
            Vector2 pBaseOSP,               // Base isn't necessarily Viewer. In this case, Base is a point in the center of Viewer.
            Vector2 pViewerOSP,         // Viewer OSP.
            Vector2 pViewerSize,
            Vector2 pInitBaseOffset,
            float pGridSnapProximity,
            InputPayload pInputPayload,
            ImDrawListPtr pDrawList,
            GridSnapData pSnapData = null,
            CanvasDrawFlags pCanvasDrawFlag = CanvasDrawFlags.None ) {
            var tIsAnyNodeHandleClicked = false;
            var tIsReadingClicksOnNode = true;
            var tIsAnyNodeClicked = false;
            var tIsAnySelectedNodeInteracted = false;
            Area tSelectScreenArea = null;
            Vector2? tSnapDelta = null;

            // Get this canvas' origin' screenPos   (only scaling for zooming)
            if( Map.CheckNeedInitOfs() ) {
                Map.AddBaseOffset( pInitBaseOffset );
                Map.MarkUnneedInitOfs();
            }
            var tCanvasOSP = pBaseOSP + Map.GetBaseOffset() * Config.Scaling;

            if( pCanvasDrawFlag.HasFlag( CanvasDrawFlags.NoInteract ) )     // clean up stuff in case viewer is involuntarily lose focus, to avoid potential accidents.
            {
                LastSnapDelta = null;
                SnappingNode = null;
                NodeBeingDragged = false;
            }

            // Capture selectArea
            if( !pCanvasDrawFlag.HasFlag( CanvasDrawFlags.NoInteract ) ) {
                // Capture selectAreaOSP
                if( !NodeBeingDragged && pInputPayload.IsKeyShift && pInputPayload.IsMouseLmbDown ) {
                    if( !SelectAreaPosition.HasValue ) SelectAreaPosition = pInputPayload.MousePos;
                }
                else SelectAreaPosition = null;

                // Capture selectArea
                if( SelectAreaPosition != null ) {
                    tSelectScreenArea = new( SelectAreaPosition.Value, pInputPayload.MousePos, true );
                }
            }

            // Populate snap data
            if( !pCanvasDrawFlag.HasFlag( CanvasDrawFlags.NoInteract ) ) {
                foreach( var node in Nodes ) {
                    var tNodeOSP = Map.GetNodeScreenPos( node, tCanvasOSP, Config.Scaling );
                    if( tNodeOSP == null ) continue;
                    if( SnappingNode != null && node != SnappingNode && !SelectedNodes.Contains( node ) )  // avoid snapping itself & selected nodess
                    {
                        pSnapData?.AddUsingPos( tNodeOSP.Value );
                    }
                }
                // Get snap delta
                if( SnappingNode != null ) {
                    var tNodeOSP = Map.GetNodeScreenPos( SnappingNode, tCanvasOSP, Config.Scaling );
                    Vector2? tSnapOSP = null;
                    if( tNodeOSP.HasValue )
                        tSnapOSP = pSnapData?.GetClosestSnapPos( tNodeOSP.Value, pGridSnapProximity );
                    if( tSnapOSP.HasValue )
                        tSnapDelta = tSnapOSP.Value - tNodeOSP;
                    LastSnapDelta = tSnapDelta;
                }
            }

            // =====================
            // Draw
            // =====================
            var tFirstClickScanRes = FirstClickType.None;

            // Draw edges

            foreach( var node in Nodes ) {
                var nodePosition = Map.GetNodeScreenPos( node, tCanvasOSP, Config.Scaling );
                if( !nodePosition.HasValue ) continue;

                foreach( var (slot, idx) in node.Slots.WithIndex() ) {
                    if( slot.Connected == null ) continue;
                    var parentPosition = Map.GetNodeScreenPos( slot.Connected, tCanvasOSP, Config.Scaling );
                    if( !parentPosition.HasValue ) continue;

                    if( !NodeUtils.IsLineIntersectRect( nodePosition.Value, parentPosition.Value, new( pViewerOSP, pViewerSize ) ) ) continue;

                    node.DrawEdge(
                        pDrawList,
                        Node.GetInputPosition( nodePosition.Value, idx ),
                        slot.Connected.GetOutputPosition( parentPosition.Value ),
                        slot.Connected,
                        idx,
                        SelectedNodes.Contains( slot.Connected ),
                        SelectedNodes.Contains( node )
                    );
                }
            }

            // Draw nodes
            Stack<LinkedListNode<Node>> tNodeToFocus = new();
            Stack<Node> tNodeToSelect = new();
            List<Node> tNodeToDeselect = [];
            HashSet<Node> tNodesReqqingClearSelect = [];
            var tIsEscapingMultiselect = false;
            for( var znode = NodeRenderZOrder.First; znode != null; znode = znode?.Next ) {
                if( znode == null ) break;
                var node = znode.Value;
                // Get NodeOSP
                var nodePosition = Map.GetNodeScreenPos( node, tCanvasOSP, Config.Scaling );
                if( nodePosition == null ) continue;

                // Skip rendering if node is out of view
                if( ( ( nodePosition.Value.X + node.Style.GetSizeScaled( GetScaling() ).X ) < pViewerOSP.X || nodePosition.Value.X > pViewerOSP.X + pViewerSize.X )
                    || ( ( nodePosition.Value.Y + node.Style.GetSizeScaled( GetScaling() ).Y ) < pViewerOSP.Y || nodePosition.Value.Y > pViewerOSP.Y + pViewerSize.Y ) ) {
                    continue;
                }

                // Process input on node
                // We record the inputs of each individual node.
                // Then, we evaluate those recorded inputs in context of z-order, determining which one we need and which we don't.
                if( !pCanvasDrawFlag.HasFlag( CanvasDrawFlags.NoInteract ) ) {
                    // Process input on node    (tIsNodeHandleClicked, pReadClicks, tIsNodeClicked, tFirstClick)
                    var t = ProcessInputOnNode( node, nodePosition.Value, pInputPayload, tIsReadingClicksOnNode );
                    {
                        if( t.FirstClick != FirstClickType.None ) {
                            tFirstClickScanRes = ( t.FirstClick == FirstClickType.Body && SelectedNodes.Contains( node ) )
                                                 ? FirstClickType.BodySelected
                                                 : t.FirstClick;
                        }
                        if( t.IsEscapingMultiselect ) tIsEscapingMultiselect = true;
                        if( t.IsNodeHandleClicked ) {
                            tIsAnyNodeHandleClicked = t.IsNodeHandleClicked;
                        }
                        if( t.IsNodeHandleClicked && pInputPayload.IsMouseLmbDown ) {
                            // Queue the focus nodes
                            if( znode != null ) {
                                tNodeToFocus.Push( znode );
                            }
                        }
                        else if( pInputPayload.IsMouseLmbDown && t.IsWithin && !t.IsWithinHandle )     // if an upper node's body covers the previously chosen nodes, discard the focus/selection queue.
                        {
                            tNodeToFocus.Clear();
                            tNodeToSelect.Clear();
                            tFirstClickScanRes = tFirstClickScanRes == FirstClickType.BodySelected
                                                 ? FirstClickType.BodySelected
                                                 : FirstClickType.Body;
                        }
                        tIsReadingClicksOnNode = t.ReadClicks;
                        if( t.IsNodeClicked ) {
                            tIsAnyNodeClicked = true;
                            if( SelectedNodes.Contains( node ) ) tIsAnySelectedNodeInteracted = true;
                        }
                        pCanvasDrawFlag |= t.CDFRes;

                        if( t.IsMarkedForSelect )                                       // Process node adding
                                                                                        // prevent marking multiple handles with a single lmbDown. Get the uppest node.
                        {
                            if( pInputPayload.IsMouseLmbDown )      // this one is for lmbDown. General use.
                            {
                                if( pInputPayload.IsKeyCtrl || tNodeToSelect.Count == 0 ) tNodeToSelect.Push( node );
                                else if( tNodeToSelect.Count != 0 ) {
                                    tNodeToSelect.Pop();
                                    tNodeToSelect.Push( node );
                                }
                            }
                            else if( pInputPayload.IsMouseLmb && t.IsEscapingMultiselect )     // this one is for lmbClick. Used for when the node is marked at lmb is lift up.
                            {
                                tNodeToSelect.TryPop( out var _ );
                                tNodeToSelect.Push( node );
                            }
                        }
                        if( t.IsMarkedForDeselect ) {
                            tNodeToDeselect.Add( node );
                        }
                        if( t.IsReqqingClearSelect ) tNodesReqqingClearSelect.Add( node );
                    }

                    if( node.Style.CheckPosWithin( nodePosition.Value, GetScaling(), pInputPayload.MousePos )
                        && pInputPayload.MouseWheelValue != 0
                        && SelectedNodes.Contains( node ) ) {
                        pCanvasDrawFlag |= CanvasDrawFlags.NoCanvasZooming;
                    }
                    // Select using selectArea
                    if( tSelectScreenArea != null && !NodeBeingDragged && FirstClickInDrag != FirstClickType.Handle ) {
                        if( node.Style.CheckAreaIntersect( nodePosition.Value, Config.Scaling, tSelectScreenArea ) ) {
                            if( SelectedNodes.Add( node ) && znode != null )
                                tNodeToFocus.Push( znode );
                        }
                    }
                }

                // Draw using NodeOSP
                node.Draw(
                    tSnapDelta != null && SelectedNodes.Contains( node ) ? nodePosition.Value + tSnapDelta.Value : nodePosition.Value,
                    Config.Scaling,
                    SelectedNodes.Contains( node ),
                    PendingConnection,
                    out PendingConnection );

                var tNodeRelaPos = Map.GetNodeRelaPos( node );
                if( NodeBeingDragged && SelectedNodes.Contains( node ) && tNodeRelaPos.HasValue )
                    ImGui.GetWindowDrawList().AddText(
                        nodePosition.Value + new Vector2( 0, -30 ) * GetScaling()
                        , ImGui.ColorConvertFloat4ToU32( NodeUtils.Colors.NodeText ),
                        $"({tNodeRelaPos.Value.X / 10:F1}, {tNodeRelaPos.Value.Y / 2:F1})" );

                // Draw conn tether to cursor
                if( PendingConnection?.Node == node ) {
                    var startPos = PendingConnection.IsInput ? Node.GetInputPosition( nodePosition.Value, PendingConnection.Index ) : node.GetOutputPosition( nodePosition.Value );
                    var endPos = pInputPayload.MousePos;
                    var midPos = startPos + ( endPos - startPos ) / 2f;

                    pDrawList.AddBezierCubic( startPos, new( midPos.X, startPos.Y ), new( midPos.X, endPos.Y ), endPos, ImGui.ColorConvertFloat4ToU32( NodeUtils.Colors.NodeFg ), 1f );
                }


            }
            // Node interaction z-order process (order of op: clearing > selecting > deselecting)
            if( tNodeToSelect.Count != 0 ) pCanvasDrawFlag |= CanvasDrawFlags.NoCanvasDrag;
            if( tNodeToFocus.TryPeek( out var topF ) && tNodesReqqingClearSelect.Contains( topF.Value )
                || tIsEscapingMultiselect )      // only accept a clear-select-req from a node that is on top of the focus queue
            {
                SelectedNodes.Clear();
            }
            foreach( var tId in tNodeToSelect ) SelectedNodes.Add( tId );
            foreach( var tId in tNodeToDeselect ) SelectedNodes.Remove( tId );

            // Bring to focus (only get the top node)
            if( tNodeToFocus.Count != 0 ) {
                var zFocusNode = tNodeToFocus.Pop();
                if( zFocusNode != null ) {
                    NodeRenderZOrder.Remove( zFocusNode );
                    NodeRenderZOrder.AddLast( zFocusNode );
                }
            }
            if( pInputPayload.IsMouseRmb ) PendingConnection = null;

            // Capture drag's first click. State Body or Handle can only be accessed from state None.
            if( pInputPayload.IsMouseLmb ) FirstClickInDrag = FirstClickType.None;
            else if( pInputPayload.IsMouseLmbDown && FirstClickInDrag == FirstClickType.None && tFirstClickScanRes != FirstClickType.None )
                FirstClickInDrag = tFirstClickScanRes;

            if( !pCanvasDrawFlag.HasFlag( CanvasDrawFlags.NoInteract )
                && pInputPayload.IsMouseLmb
                && ( !tIsAnyNodeHandleClicked && ( pInputPayload.LmbDragDelta == null ) )
                && !pInputPayload.IsALmbDragRelease
                && !tIsAnySelectedNodeInteracted ) {
                SelectedNodes.Clear();
            }


            // Draw selectArea
            if( !pCanvasDrawFlag.HasFlag( CanvasDrawFlags.NoInteract ) && tSelectScreenArea != null ) {
                ImGui.GetForegroundDrawList().AddRectFilled( tSelectScreenArea.Start, tSelectScreenArea.End, ImGui.ColorConvertFloat4ToU32( NodeUtils.AdjustTransparency( NodeUtils.Colors.NodeFg, 0.5f ) ) );
            }

            if( !pCanvasDrawFlag.HasFlag( CanvasDrawFlags.NoInteract ) ) {
                // Drag selected node
                if( NodeBeingDragged
                    && pInputPayload.LmbDragDelta.HasValue
                    && !pCanvasDrawFlag.HasFlag( CanvasDrawFlags.NoNodeDrag ) ) {
                    foreach( var id in SelectedNodes ) {
                        Map.MoveNodeRelaPos(
                            id,
                            pInputPayload.LmbDragDelta.Value,
                            Config.Scaling );
                    }
                    pCanvasDrawFlag |= CanvasDrawFlags.StateNodeDrag;
                }
                // Snap if available
                else if( !NodeBeingDragged
                         && LastSnapDelta != null
                         && ( !pCanvasDrawFlag.HasFlag( CanvasDrawFlags.NoNodeDrag ) || !pCanvasDrawFlag.HasFlag( CanvasDrawFlags.NoNodeSnap ) ) ) {
                    foreach( var id in SelectedNodes ) {
                        Map.MoveNodeRelaPos(
                            id,
                            LastSnapDelta.Value,
                            Config.Scaling );
                    }
                    LastSnapDelta = null;
                    pCanvasDrawFlag |= CanvasDrawFlags.StateNodeDrag;
                }
                // Process input on canvas
                if( !pCanvasDrawFlag.HasFlag( CanvasDrawFlags.NoCanvasInteraction )
                    && !pCanvasDrawFlag.HasFlag( CanvasDrawFlags.NoCanvasDrag )
                    && ( !NodeBeingDragged || ( NodeBeingDragged && FirstClickInDrag != FirstClickType.Handle ) )
                    && !tIsAnyNodeClicked
                    && ( FirstClickInDrag == FirstClickType.None || FirstClickInDrag == FirstClickType.Body )
                    && SelectAreaPosition == null ) {
                    pCanvasDrawFlag |= ProcessInputOnCanvas( pInputPayload, pCanvasDrawFlag );
                }
            }

            // Mass delete nodes
            if( pInputPayload.IsKeyDel ) {
                foreach( var id in SelectedNodes ) {
                    RemoveNode( id );
                }
            }

            // First frame after lmb down. Leave this at the bottom (end of frame drawing).
            if( pInputPayload.IsMouseLmb ) IsFirstFrameAfterLmbDown = true;
            else if( pInputPayload.IsMouseLmbDown ) IsFirstFrameAfterLmbDown = false;

            return pCanvasDrawFlag;
        }

        public void Dispose() {
            foreach( var node in Nodes ) node.Dispose();
        }

        public enum FirstClickType {
            None = 0,
            Handle = 1,
            Body = 2,
            BodySelected = 3
        }

        public struct NodeInputProcessResult {
            public bool IsNodeHandleClicked = false;
            public bool IsNodeClicked = false;
            public bool IsWithin = false;
            public bool IsWithinHandle = false;
            public bool IsMarkedForSelect = false;
            public bool IsMarkedForDeselect = false;
            public bool IsReqqingClearSelect = false;
            public bool IsEscapingMultiselect = false;
            public FirstClickType FirstClick = FirstClickType.None;
            public CanvasDrawFlags CDFRes = CanvasDrawFlags.None;
            public bool ReadClicks = false;
            public NodeInputProcessResult() { }
        }
    }
}
