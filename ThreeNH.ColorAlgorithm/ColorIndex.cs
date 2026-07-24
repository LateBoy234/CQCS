using System.ComponentModel;

namespace ThreeNH.ColorAlgorithm
{
    /// <summary>
    /// 顏色指數
    /// </summary>
    public enum ColorIndex
    {
        /// <summary>
        /// 无
        /// </summary>
        None,  
        /// <summary>
        /// 黄度指数
        /// </summary>
        Yellowness,   
        /// <summary>
        /// 白度指数
        /// </summary>
        Whiteness,  
        /// <summary>
        /// 力份
        /// </summary>
        Strength,           
        /// <summary>
        /// 同色异谱指数
        /// </summary>
        [Description("MI")]
        MetamerismIndiex,  
        /// <summary>
        /// 遮盖度
        /// </summary>
        Opacity,          
        /// <summary>
        /// 555色调分类
        /// </summary>
        [Description("ShadeBy555")]
        Shade555,          
        /// <summary>
        /// 沾色牢度
        /// </summary>
        [Description("沾色牢度")]
        StainingFastness,   
        /// <summary>
        /// 透射Garder指数
        /// </summary>
        GardnerIndex,       
        /// <summary>
        /// 变色牢度
        /// </summary>
        [Description("变色牢度")]
        ColorFastness,      
        /// <summary>
        /// 钴铂指数
        /// </summary>
        PtCoIndex,     
        /// <summary>
        /// 8 度光泽度
        /// </summary>
        Gloss8, 
        /// <summary>
        /// 雾度指标
        /// </summary>
        Haze,              
    }
}
