using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.System.Scheduler.Base;
using System;

namespace VfxEditor.Interop {
    public unsafe partial class ResourceLoader {
        // ====== PLAY ACTION =======

        public delegate ulong PlayActionPrototype( SchedulerTimeline* timeline );

        [Signature( Constants.PlayActionSig, DetourName = nameof( PlayActionDetour ) )]
        public readonly Hook<PlayActionPrototype> PlayActionHook = null;

        private ulong PlayActionDetour( SchedulerTimeline* timeline ) {
            var ret = PlayActionHook.Original( timeline );
            Plugin.TrackerManager?.Tmb.AddAction( timeline );
            return ret;
        }
    }
}
