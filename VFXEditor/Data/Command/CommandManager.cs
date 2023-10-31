using ImGuiNET;
using OtterGui.Raii;
using System.Collections.Generic;
using VfxEditor.FileManager;
using VfxEditor.Utils.Stacks;

namespace VfxEditor {
    public class CommandManager {
        public static CommandManager Current => Stack.Count == 0 ? null : Stack.Peek();
        private static readonly Stack<CommandManager> Stack = new();

        public static void Push( CommandManager current ) => Stack.Push( current );

        public static void Pop() => Stack.Pop();

        public static void Add( ICommand command ) => Current?.AddAndExecute( command );

        public static void Draw() {
            if( Current == null ) {
                using var disabled = ImRaii.Disabled();
                ImGui.MenuItem( "Undo" );
                ImGui.MenuItem( "Redo" );
                return;
            }

            Current.DrawMenu();
        }

        public static void Undo() => Current?.UndoInternal();

        public static void Redo() => Current?.RedoInternal();

        // ======================

        private readonly UndoRedoStack<ICommand> Commands = new( 25 );

        private readonly FileManagerFile File;

        public CommandManager( FileManagerFile file ) {
            File = file;
        }

        public void AddAndExecute( ICommand command ) {
            command.Execute();
            Commands.Add( command );

            File.SetUnsaved();
            File.OnChange();
        }

        protected bool CanUndo => Commands.CanUndo;

        protected bool CanRedo => Commands.CanRedo;

        protected void UndoInternal() {
            if( !Commands.Undo( out var item ) ) return;
            item.Undo();

            File.SetUnsaved();
            File.OnChange();
        }

        protected void RedoInternal() {
            if( !Commands.Redo( out var item ) ) return;
            item.Redo();

            File.SetUnsaved();
            File.OnChange();
        }

        protected unsafe void DrawMenu() {
            using( var dimUndo = ImRaii.PushColor( ImGuiCol.Text, *ImGui.GetStyleColorVec4( ImGuiCol.TextDisabled ), !CanUndo ) ) {
                if( ImGui.MenuItem( "Undo" ) ) UndoInternal();
            }

            using var dimRedo = ImRaii.PushColor( ImGuiCol.Text, *ImGui.GetStyleColorVec4( ImGuiCol.TextDisabled ), !CanRedo );
            if( ImGui.MenuItem( "Redo" ) ) RedoInternal();
        }

        public void Dispose() => Commands.Clear();
    }
}
