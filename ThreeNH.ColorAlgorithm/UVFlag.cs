using System.ComponentModel;

namespace ThreeNH.ColorAlgorithm
{
    public enum UVFlag : byte
    {
        [Description("UV Off")]
        None,
        [Description("UV On")]
        On,
        [Description("UV_NoCut")]
        NoCut,          // 无截止
        [Description("UV_Cut400nm")]
        Cut400nm,       // 400nm 截止
        [Description("UV_Cut420nm")]
        Cut420nm,       // 420nm 截止
        [Description("UV_Cut460nm")]
        Cut460nm,
    }
}
