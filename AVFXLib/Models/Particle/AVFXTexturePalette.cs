using AVFXLib.AVFX;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AVFXLib.Models
{
    public class AVFXTexturePalette : Base
    {
        public LiteralBool Enabled = new LiteralBool("bEna");
        public LiteralEnum<TextureFilterType> TextureFilter = new LiteralEnum<TextureFilterType>("TFT");
        public LiteralEnum<TextureBorderType> TextureBorder = new LiteralEnum<TextureBorderType>("TBT");
        public LiteralInt TextureIdx = new LiteralInt("TxNo");

        List<Base> Attributes;

        public AVFXTexturePalette() : base("TP")
        {
            Attributes = new List<Base>(new Base[]{
                Enabled,
                TextureFilter,
                TextureBorder,
                TextureIdx
            });
        }

        public override void read(AVFXNode node)
        {
            Assigned = true;
            ReadAVFX(Attributes, node);
        }

        public override void toDefault()
        {
            Assigned = true;
            SetDefault(Attributes);
            TextureIdx.GiveValue( -1 );
        }

        public override AVFXNode toAVFX()
        {
            AVFXNode dataAvfx = new AVFXNode("TP");
            PutAVFX(dataAvfx, Attributes);
            return dataAvfx;
        }
    }
}
