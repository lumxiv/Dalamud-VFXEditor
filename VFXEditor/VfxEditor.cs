using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.Command;
using Dalamud.Logging;
using Dalamud.Plugin;
using ImGuiFileDialog;
using ImGuiNET;
using ImPlotNET;
using VFXEditor.AVFX;
using VFXEditor.Data;
using VFXEditor.Dialogs;
using VFXEditor.DirectX;
using VFXEditor.Interop;
using VFXEditor.PapFormat;
using VFXEditor.Penumbra;
using VFXEditor.Select;
using VFXEditor.TexTools;
using VFXEditor.Texture;
using VFXEditor.TmbFormat;
using VFXEditor.Tracker;

namespace VFXEditor {
    public partial class VfxEditor : IDalamudPlugin {
        public static DalamudPluginInterface PluginInterface { get; private set; }
        public static ClientState ClientState { get; private set; }
        public static Framework Framework { get; private set; }
        public static Condition Condition { get; private set; }
        public static CommandManager CommandManager { get; private set; }
        public static ObjectTable Objects { get; private set; }
        public static SigScanner SigScanner { get; private set; }
        public static DataManager DataManager { get; private set; }
        public static TargetManager TargetManager { get; private set; }
        public static KeyState KeyState { get; private set; }

        public static ResourceLoader ResourceLoader { get; private set; }
        public static DirectXManager DirectXManager { get; private set; }
        public static AvfxManager AvfxManager { get; private set; }
        public static TextureManager TextureManager { get; private set; }
        public static TmbManager TmbManager { get; private set; }
        public static PapManager PapManager { get; private set; }
        public static Configuration Configuration { get; private set; }
        public static VfxTracker VfxTracker { get; private set; }
        public static ToolsDialog ToolsDialog { get; private set; }
        public static TexToolsDialog TexToolsDialog { get; private set; }
        public static PenumbraDialog PenumbraDialog { get; private set; }

        public string Name => "VFXEditor";
        public static string RootLocation { get; private set; }
        private const string CommandName = "/vfxedit";

        private static bool ClearKeyState = false;

        public VfxEditor(
                DalamudPluginInterface pluginInterface,
                ClientState clientState,
                CommandManager commandManager,
                Framework framework,
                Condition condition,
                ObjectTable objects,
                SigScanner sigScanner,
                DataManager dataManager,
                TargetManager targetManager,
                KeyState keyState
            ) {
            PluginInterface = pluginInterface;
            ClientState = clientState;
            Condition = condition;
            CommandManager = commandManager;
            Objects = objects;
            SigScanner = sigScanner;
            DataManager = dataManager;
            Framework = framework;
            TargetManager = targetManager;
            KeyState = keyState;

            CommandManager.AddHandler( CommandName, new CommandInfo( OnCommand ) { HelpMessage = "toggle ui" } );

            RootLocation = PluginInterface.AssemblyLocation.DirectoryName;

            ImPlot.SetImGuiContext( ImGui.GetCurrentContext() );
            ImPlot.SetCurrentContext( ImPlot.CreateContext() );

            SheetManager.Initialize();

            Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            Configuration.Setup();

            TextureManager.Setup();
            TextureManager = new TextureManager();

            TmbManager.Setup();
            TmbManager = new TmbManager();

            AvfxManager.Setup();
            AvfxManager = new AvfxManager();

            PapManager.Setup();
            PapManager = new PapManager();

            ToolsDialog = new ToolsDialog();
            PenumbraDialog = new PenumbraDialog();
            TexToolsDialog = new TexToolsDialog();
            ResourceLoader = new ResourceLoader();
            DirectXManager = new DirectXManager();
            VfxTracker = new VfxTracker();

            FileDialogManager.Initialize( PluginInterface );

            ResourceLoader.Init();
            ResourceLoader.Enable();

            Framework.Update += FrameworkOnUpdate;
            PluginInterface.UiBuilder.Draw += Draw;
            PluginInterface.UiBuilder.Draw += FileDialogManager.Draw;
            PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUi;

            //Data.Scd.ScdFile.Test();
        }

        public static void CheckClearKeyState() {
            if( ImGui.IsWindowFocused( ImGuiFocusedFlags.RootAndChildWindows ) && Configuration.BlockGameInputsWhenFocused ) ClearKeyState = true;
        }

        private void FrameworkOnUpdate( Framework framework ) {
            KeybindConfiguration.UpdateState();
            if( ClearKeyState ) KeyState.ClearAll();
            ClearKeyState = false;
        }

        private void DrawConfigUi() => AvfxManager.Show();

        private void OnCommand( string command, string rawArgs ) => AvfxManager.Toggle();

        public void Dispose() {
            Framework.Update -= FrameworkOnUpdate;
            PluginInterface.UiBuilder.Draw -= FileDialogManager.Draw;
            PluginInterface.UiBuilder.Draw -= Draw;
            PluginInterface.UiBuilder.OpenConfigUi -= DrawConfigUi;

            ImPlot.DestroyContext();

            CommandManager.RemoveHandler( CommandName );

            ResourceLoader.Dispose();
            ResourceLoader = null;

            TmbManager.Dispose();
            TmbManager = null;

            PapManager.Dispose();
            PapManager = null;

            AvfxManager.Dispose();
            AvfxManager = null;

            TextureManager.BreakDown();
            TextureManager.Dispose();
            TextureManager = null;

            DirectXManager.Dispose();
            DirectXManager = null;

            RemoveSpawn();

            FileDialogManager.Dispose();
            CopyManager.Dispose();
        }
    }
}