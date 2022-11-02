using VfxEditor.AVFXLib;
using VfxEditor.AVFXLib.Emitter;

namespace VfxEditor.AvfxFormat.Vfx {
    public class UiEmitterDataConeModel : UiData {
        public readonly UiParameters Parameters;

        public UiEmitterDataConeModel( AVFXEmitterDataConeModel data ) {
            Tabs.Add( Parameters = new UiParameters( "Parameters" ) );
            Parameters.Add( new UiCombo<RotationOrder>( "Rotation Order", data.RotationOrderType ) );
            Parameters.Add( new UiCombo<GenerateMethod>( "Generate Method", data.GenerateMethodType ) );
            Parameters.Add( new UiInt( "Divide X", data.DivideX ) );
            Parameters.Add( new UiInt( "Divide Y", data.DivideY ) );
            Tabs.Add( new UiCurve( data.AX, "Angle X" ) );
            Tabs.Add( new UiCurve( data.AY, "Angle Y" ) );
            Tabs.Add( new UiCurve( data.Radius, "Radius" ) );
            Tabs.Add( new UiCurve( data.InjectionSpeed, "Injection Speed" ) );
            Tabs.Add( new UiCurve( data.InjectionSpeedRandom, "Injection Speed Random" ) );
            Tabs.Add( new UiCurve( data.InjectionAngle, "Injection Angle" ) );
        }
    }
}
