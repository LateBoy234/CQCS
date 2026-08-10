using System;
using System.Collections.Generic;
using System.ComponentModel;
using ThreeNH.Helper;

namespace ThreeNH.DataModel
{
    // 色品字段
    public enum ChromaticityField
    {
        // ----- 样品字段 --------
        [Description("L*")]
        L = 0,  // L*
        [Description("a*")]
        a,  // a*
        [Description("b*")]
        b,  // b*
        [Description("C*")]
        C,  // C*
        [Description("h\u00B0")]
        h,  // h°
        [Description("u*")]
        u,  // u*
        [Description("v*")]
        v,  // v*
        [Description("u'")]
        uq,
        [Description("v'")]
        vq,
        [Description("X")]
        X,  // X
        [Description("Y")]
        Y,  // Y
        [Description("Z")]
        Z,  // Z
        [Description("x")]
        x,  // x
        [Description("y")]
        y,  // y
        [Description("z")]
        z,  // z
        [Description("R")]
        R,  // R
        [Description("G")]
        G,  // G
        [Description("B")]
        B,  // B
        [Description("L")]
        HL, // Hunter L
        [Description("a")]
        Ha, // Hunter a
        [Description("b")]
        Hb, // Hunter b
        [Description("sRGB-R")]
        sR, // sRGB的R
        [Description("sRGB-G")]
        sG, // sRGB的G
        [Description("sRGB-B")]
        sB, // sRGB的B
        [Description("L99")]
        L99,    // DIN99 L
        [Description("a99")]
        a99,    // DIN99 a
        [Description("b99")]
        b99,    // DIN99 b
        [Description("C99")]
        C99,    // DIN99 C
        [Description("h99")]
        h99,    // DIN99 h
        [Description("β")]
        LuminanceFactor,        // 亮度因子
        [Description("Munsell-H")]
        MH,
        [Description("Munsell-V")]
        MV,
        [Description("Munsell-C")]
        MC,
        //-------- 色差字段 --------------
        [Description("\u0394L*")]
        dL = 100,  // ΔL*
        [Description("\u0394a*")]
        da,  // Δa*
        [Description("\u0394b*")]
        db,  // Δb*
        //[Description("\u0394ab*")]
        //dab,  // Δab*
        [Description("\u0394C*")]
        dC,  // ΔC*
        //[Description("\u0394h\u00B0")]
        //dh,  // Δh°
        [Description("\u0394H*")]
        dH,  // ΔH*
        [Description("\u0394u*")]
        du,  // Δu*
        [Description("\u0394v*")]
        dv,  // Δv*
        [Description("\u0394u'")]
        duq,  // Δu*
        [Description("\u0394v'")]
        dvq,  // Δv*
        [Description("\u0394X")]
        dX,  // ΔX
        [Description("\u0394Y")]
        dY,  // ΔY
        [Description("\u0394Z")]
        dZ,  // ΔZ
        [Description("\u0394x")]
        dx,  // Δx
        [Description("\u0394y")]
        dy,  // Δy
        //[Description("\u0394z")]
        //dz,  // Δz
        [Description("\u0394R")]
        dR,  // ΔR
        [Description("\u0394G")]
        dG,  // ΔG
        [Description("\u0394B")]
        dB,  // ΔB
        [Description("\u0394L")]
        dHL, // Hunter ΔL
        [Description("\u0394a")]
        dHa, // Hunter Δa
        [Description("\u0394b")]
        dHb, // Hunter Δb
        [Description("\u0394E*ab")]
        dEab,        // ΔE*ab
        [Description("\u0394E*94")]
        dE94,        // ΔE*94
        [Description("\u0394L'94")]
        dL94,        // 计算ΔE*94的ΔL'
        [Description("\u0394C'94")]
        dC94,        // 计算ΔE*94的ΔC'
        [Description("\u0394H'94")]
        dH94,        // 计算ΔE*94的ΔH'
        [Description("\u0394E*00")]
        dE00,        // ΔE*00
        [Description("\u0394L'00")]
        dL00,        // 计算ΔE*00的 ΔL'
        [Description("\u0394C'00")]
        dC00,        // 计算ΔE*00的 ΔC'
        [Description("\u0394H'00")]
        dH00,        // 计算ΔE*00的 ΔH'
        [Description("CMC")]
        dEcmc,       // ΔEcmc(l:c)
        [Description("\u0394L'cmc")]
        dLcmc,       // 计算ΔEcmc的ΔL'
        [Description("\u0394C'cmc")]
        dCcmc,       // ΔEcmc的ΔC'
        [Description("\u0394H'cmc")]
        dHcmc,       // ΔEcmc的ΔH'
        [Description("\u0394E*uv")]
        dEuv,        // ΔE*uv
        [Description("\u0394E")]
        dHE,        // Hunter 的 ΔE
        [Description("\u0394L99")]
        dL99,       // DIN99 ΔL
        [Description("\u0394a99")]
        da99,       // DIN99 ΔL
        [Description("\u0394b99")]
        db99,       // DIN99 ΔL
        [Description("\u0394C99")]
        dC99,       // DIN99 ΔL
        [Description("\u0394H99")]
        dH99,       // DIN99 ΔL
        [Description("\u0394E99")]
        dE99,       // DIN99 ΔL
        [Description("\u0394β")]
        dLuminanceFactor,   // 亮度因子差
        [Description("\u0394Munsell-H")]
        dMH,
        [Description("\u0394Munsell-V")]
        dMV,
        [Description("\u0394Munsell-C")]
        dMC,
        [Description("CMC(1:1)")]
        dEcmc11,    // CMC(1:1)
        [Description("\u0394L'cmc(1:1)")]
        dLcmc11,       // 计算CMC(1:1)的ΔL'
        [Description("\u0394C'cmc(1:1)")]
        dCcmc11,       // CMC(1:1)的ΔC'
        [Description("\u0394H'cmc(1:1)")]
        dHcmc11,       // CMC(1:1)的ΔH'
        [Description("CMC(2:1)")]
        dEcmc21,    // CMC(2:1)
        [Description("\u0394L'cmc(2:1)")]
        dLcmc21,       // 计算CMC(1:1)的ΔL'
        [Description("\u0394C'cmc(2:1)")]
        dCcmc21,       // CMC(1:1)的ΔC'
        [Description("\u0394H'cmc(2:1)")]
        dHcmc21,       // CMC(1:1)的ΔH'
        //------颜色指数字段-------------------
        //[Description("WI(ASTM E313-2005)")]
        WI_E313,     // WI(E313)
        //[Description("T(ASTM E313-2005)")]
        T_E313,      // T(E313)
        WI_CIE_ISO,  // WI(CIE/ISO)
        T_CIE_ISO,   // T(CIE/ISO)
        //[Description("WI(Hunter 1942)")]
        WI_Hunter1942,  // WI(Hunter 1942)
        //[Description("WI(Hunter 1960)")]
        WI_Hunter1960,  // WI(Hunter 1960)
        //[Description("WI(Tappl)")]
        WI_Tapple,      // WI(Tappl)
        //[Description("WI(Taube)")]
        WI_Taube,       // WI(Taube)
        WI_R457,        // R457白度
        WI_Berger,       //伯格白度
        WI_Granz,       //甘茨白度
        WI_GOST_16873,  //GOST 16873白度
        //[Description("YI(ASTM D1925-1970)")]
        YI_D1925,       // YI(ASTM D1925-1970)
        //[Description("YI(ASTM E313-2005)")]
        YI_E313,        // YI(ASTM E313-2005)
        //[Description("MI")]
        MI1,             // 同色异谱指数
        MI2,             // 同色异谱指数
        //[Description("SSR")]
        StainingFastness_dEgs,            // 沾色牢度
        //[Description("SSR Level")]
        StainingFastness_SSRLevel,       // 沾色牢度等级
        //[Description("GSR")]
        ColorFastness_dEf,            // 变色牢度
        //[Description("GSR Level")]
        ColorFastness_GSRLevel,       // 变色牢度等级
        //[Description("% Strength SUM")]
        AverageStrength,    // % Strength SUM
        //[Description("% Strength SWL")]
        PrecentStrengthSWL, // % Strength SWL
        WeightedStrengthSWL,//加权力份
        Opacity,            // 遮盖度
        ShadeBy555,   // 555分类
        GardnerIndex,       // Gardner指数，仅适用于透射数据
        IronCobaltIndex,       // 铁钴指数，仅适用于透射数据
        PtCoIndex, // Pt-CO指数（钴铂指数）
        ICUMSA,     //糖度（糖色值）
        HazeC2,       // 雾度指数(C/2)
        HazeA2,     // 雾度指数（A/2）
        HazeD65,    // 雾度指数（D65/2）
        Gloss8,     //8度光泽度
        MY_Carbon,         //炭黑
        MC_Carbon,         //黑度
        dM_Carbon,         //底色
        [Description("ITA°")]
        ITA,
        [Description("T(C/2)")]
        TransmittanceC2,//总透过率
        [Description("T(A/2)")]
        TransmittanceA2,//总透过率
        [Description("T(D65/2)")]
        TransmittanceD65,//总透过率
        Clarity,//清晰度
        Klett,//柯莱特单位
        Sayblot,
        [Description("ASTM D1500")]
        AstmD1500,
        //------CMYK值------------
        [Description("C(CMYK_T)")]
        C_T,
        [Description("M(CMYK_T)")]
        M_T,
        [Description("Y(CMYK_T)")]
        Y_T,
        [Description("K(CMYK_T)")]
        K_T,
        [Description("C(CMYK_E)")]
        C_E,
        [Description("M(CMYK_E)")]
        M_E,
        [Description("Y(CMYK_E)")]
        Y_E,
        [Description("K(CMYK_E)")]
        K_E,
        [Description("C(CMYK_I)")]
        C_I,
        [Description("M(CMYK_I)")]
        M_I,
        [Description("Y(CMYK_I)")]
        Y_I,
        [Description("K(CMYK_I)")]
        K_I,
        [Description("C(CMYK_A)")]
        C_A,
        [Description("M(CMYK_A)")]
        M_A,
        [Description("Y(CMYK_A)")]
        Y_A,
        [Description("K(CMYK_A)")]
        K_A,
        #region 颜色指数差字段

        [Description("\u03948°Gloss")]
        DeltaGloss8,       // CMC(1:1)的ΔH'
        [Description("\u0394H(C/2)")]
        DeltaHazeC2,
        [Description("\u0394H(A/2)")]
        DeltaHazeA2,
        [Description("\u0394H(D65/2)")]
        DeltaHazeD65,
        [Description("\u0394T(C/2)")]
        DeltaTransmittanceC2,
        [Description("\u0394T(A/2)")]
        DeltaTransmittanceA2,
        [Description("\u0394T(D65/2)")]
        DeltaTransmittanceD65,
        [Description("\u0394Clarity")]
        DeltaClarity,
        #endregion

        //WavelengthOfPrecentStrengthSWL,
        ////--------其它字段------------------------
        [Description("SampleID")]
        ID,           // 索引
        Name,               // 名称
        PseudoColor,    // 仿真色
        DateTime,           // 日期时间
        Material,           // 料号
        SpecularComponent,  // 测光方式
        MeasurementCaliber, // 测量口径
        IlluminantAndObserver,  // 光源和视场角
        UV,                 // UV设置
        TransmissionFlag,   // 透射标志
        InstrumentName,     // 仪器名称
        ColorOffset,     // 颜色偏向
        Judgement,          // 判定结果
        // 反射率
        R360,
        R370,
        R380,
        R390,
        R400,
        R410,
        R420,
        R430,
        R440,
        R450,
        R460,
        R470,
        R480,
        R490,
        R500,
        R510,
        R520,
        R530,
        R540,
        R550,
        R560,
        R570,
        R580,
        R590,
        R600,
        R610,
        R620,
        R630,
        R640,
        R650,
        R660,
        R670,
        R680,
        R690,
        R700,
        R710,
        R720,
        R730,
        R740,
        R750,
        R760,
        R770,
        R780,
        //------反射率差值------------
        dR360,
        dR370,
        dR380,
        dR390,
        dR400,
        dR410,
        dR420,
        dR430,
        dR440,
        dR450,
        dR460,
        dR470,
        dR480,
        dR490,
        dR500,
        dR510,
        dR520,
        dR530,
        dR540,
        dR550,
        dR560,
        dR570,
        dR580,
        dR590,
        dR600,
        dR610,
        dR620,
        dR630,
        dR640,
        dR650,
        dR660,
        dR670,
        dR680,
        dR690,
        dR700,
        dR710,
        dR720,
        dR730,
        dR740,
        dR750,
        dR760,
        dR770,
        dR780,
        //吸收散色比
        MaxCount,
        [Description("K/S(360)")]
        KS360,
        [Description("K/S(370)")]
        KS370,
        [Description("K/S(380)")]
        KS380,
        [Description("K/S(390)")]
        KS390,
        [Description("K/S(400)")]
        KS400,
        [Description("K/S(410)")]
        KS410,
        [Description("K/S(420)")]
        KS420,
        [Description("K/S(430)")]
        KS430,
        [Description("K/S(440)")]
        KS440,
        [Description("K/S(450)")]
        KS450,
        [Description("K/S(460)")]
        KS460,
        [Description("K/S(470)")]
        KS470,
        [Description("K/S(480)")]
        KS480,
        [Description("K/S(490)")]
        KS490,
        [Description("K/S(500)")]
        KS500,
        [Description("K/S(510)")]
        KS510,
        [Description("K/S(520)")]
        KS520,
        [Description("K/S(530)")]
        KS530,
        [Description("K/S(540)")]
        KS540,
        [Description("K/S(550)")]
        KS550,
        [Description("K/S(560)")]
        KS560,
        [Description("K/S(570)")]
        KS570,
        [Description("K/S(580)")]
        KS580,
        [Description("K/S(590)")]
        KS590,
        [Description("K/S(600)")]
        KS600,
        [Description("K/S(610)")]
        KS610,
        [Description("K/S(620)")]
        KS620,
        [Description("K/S(630)")]
        KS630,
        [Description("K/S(640)")]
        KS640,
        [Description("K/S(650)")]
        KS650,
        [Description("K/S(660)")]
        KS660,
        [Description("K/S(670)")]
        KS670,
        [Description("K/S(680)")]
        KS680,
        [Description("K/S(690)")]
        KS690,
        [Description("K/S(700)")]
        KS700,
        [Description("K/S(710)")]
        KS710,
        [Description("K/S(720)")]
        KS720,
        [Description("K/S(730)")]
        KS730,
        [Description("K/S(740)")]
        KS740,
        [Description("K/S(750)")]
        KS750,
        [Description("K/S(760)")]
        KS760,
        [Description("K/S(770)")]
        KS770,
        [Description("K/S(780)")]
        KS780,
        //吸光度
        [Description("A(360)")]
        Absorbance360,
        [Description("A(370)")]
        Absorbance370,
        [Description("A(380)")]
        Absorbance380,
        [Description("A(390)")]
        Absorbance390,
        [Description("A(400)")]
        Absorbance400,
        [Description("A(410)")]
        Absorbance410,
        [Description("A(420)")]
        Absorbance420,
        [Description("A(430)")]
        Absorbance430,
        [Description("A(440)")]
        Absorbance440,
        [Description("A(450)")]
        Absorbance450,
        [Description("A(460)")]
        Absorbance460,
        [Description("A(470)")]
        Absorbance470,
        [Description("A(480)")]
        Absorbance480,
        [Description("A(490)")]
        Absorbance490,
        [Description("A(500)")]
        Absorbance500,
        [Description("A(510)")]
        Absorbance510,
        [Description("A(520)")]
        Absorbance520,
        [Description("A(530)")]
        Absorbance530,
        [Description("A(540)")]
        Absorbance540,
        [Description("A(550)")]
        Absorbance550,
        [Description("A(560)")]
        Absorbance560,
        [Description("A(570)")]
        Absorbance570,
        [Description("A(580)")]
        Absorbance580,
        [Description("A(590)")]
        Absorbance590,
        [Description("A(600)")]
        Absorbance600,
        [Description("A(610)")]
        Absorbance610,
        [Description("A(620)")]
        Absorbance620,
        [Description("A(630)")]
        Absorbance630,
        [Description("A(640)")]
        Absorbance640,
        [Description("A(650)")]
        Absorbance650,
        [Description("A(660)")]
        Absorbance660,
        [Description("A(670)")]
        Absorbance670,
        [Description("A(680)")]
        Absorbance680,
        [Description("A(690)")]
        Absorbance690,
        [Description("A(700)")]
        Absorbance700,
        [Description("A(710)")]
        Absorbance710,
        [Description("A(720)")]
        Absorbance720,
        [Description("A(730)")]
        Absorbance730,
        [Description("A(740)")]
        Absorbance740,
        [Description("A(750)")]
        Absorbance750,
        [Description("A(760)")]
        Absorbance760,
        [Description("A(770)")]
        Absorbance770,
        [Description("A(780)")]
        Absorbance780,

        #region 中国药典
        [Description("ChpGY")]
        ChpGY,
        [Description("ChpYG")]
        ChpYG,
        [Description("ChpY")]
        ChpY,
        [Description("ChpOY")]
        ChpOY,
        [Description("ChpOR")]
        ChpOR,
        [Description("ChpBR")]
        ChpBR,
        #endregion

        #region 欧洲药典
        [Description("EUpB")]
        EU_B,
        [Description("EUpBY")]
        EU_BY,
        [Description("EUpY")]
        EU_Y,
        [Description("EUpGY")]
        EU_GY,
        [Description("EUpR")]
        EU_R,
        #endregion

        #region 美国药典
        [Description("USP")]
        USP,
        #endregion

    }

    public static class ChromaticityFieldExtension
    {
        public static string Name(this ChromaticityField field)
        {
            return field.Description();
        }

        public static ChromaticityField ToChromaticityField(this string fieldName)
        {
            foreach (var value in Enum.GetValues(typeof(ChromaticityField)))
            {
                ChromaticityField field = (ChromaticityField)value;
                if (field.Name() == fieldName)
                    return field;
            }
            throw new ArgumentException(string.Format("Unknown field name: {0}", fieldName));
        }

        public static bool IsReflectance(this ChromaticityField field)
        {
            return field >= ChromaticityField.R360 && field <= ChromaticityField.R780;
        }

        public static bool IsAbsoluteStrengthField(this ChromaticityField field)
        {
            return field >= ChromaticityField.KS360 && field <= ChromaticityField.KS780;
        }

        public static bool IsAbsorbanceField(this ChromaticityField field)
        {
            return field >= ChromaticityField.Absorbance360 && field <= ChromaticityField.Absorbance780;
        }

        /// <summary>
        /// 中国药典
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        public static bool IsChPField(this ChromaticityField field)
        {
            return field >= ChromaticityField.ChpGY && field <= ChromaticityField.ChpBR;
        }
        /// <summary>
        /// 欧洲药典
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        public static bool IsEUPField(this ChromaticityField field)
        {
            return field >= ChromaticityField.EU_B && field <= ChromaticityField.EU_R;
        }
        /// <summary>
        /// 美国药典
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        public static bool IsUSAPField(this ChromaticityField field)
        {
            return field == ChromaticityField.USP;
        }
        public static bool IsReflectanceDifference(this ChromaticityField field)
        {
            return field >= ChromaticityField.dR360 && field <= ChromaticityField.dR780;
        }

        public static bool IsMunsellField(this ChromaticityField field)
        {
            return field >= ChromaticityField.MH && field <= ChromaticityField.MC;
        }

        public static int ToWavelength(this ChromaticityField field)
        {
            if (field.IsReflectance())
            {
                return (field - ChromaticityField.R360) * 10 + 360;
            }
            if (field.IsReflectanceDifference())
            {
                return (field - ChromaticityField.dR360) * 10 + 360;
            }
            if (field.IsAbsoluteStrengthField())
            {
                return (field - ChromaticityField.KS360) * 10 + 360;
            }
            if (field.IsAbsorbanceField())
            {
                return (field - ChromaticityField.Absorbance360) * 10 + 360;
            }
            throw new ArgumentException();
        }

        /// <summary>
        /// 判断字段是否是色品属性代号
        /// </summary>
        /// <param name="field">要判断的代号</param>
        /// <returns>如果field是色品属性代号返回真</returns>
        public static bool IsColorField(this ChromaticityField field)
        {
            return field < ChromaticityField.dL;
        }
        /// <summary>
        /// 判断代号是否是色差属性
        /// </summary>
        /// <param name="field">要判断的代号</param>
        /// <returns>如果代号是色差属性代号返回真</returns>
        public static bool IsColorDiffField(this ChromaticityField field)
        {
            return (field >= ChromaticityField.dL && field < ChromaticityField.WI_E313) ||
                field.IsReflectanceDifference();
        }

        /// <summary>
        /// 判断代号是否是颜色指数
        /// </summary>
        /// <param name="field">要判断的代号</param>
        /// <returns>如果是颜色指数返回真</returns>
        public static bool IsChromaticityIndex(this ChromaticityField field)
        {
            return field >= ChromaticityField.WI_E313 && field < ChromaticityField.DeltaGloss8;
        }

        public static bool IsChromaticityIndexDiff(this ChromaticityField field)
        {
            return field >= ChromaticityField.DeltaGloss8 && field < ChromaticityField.ID;
        }

        public static bool IsSampleReportField(this ChromaticityField field)
        {
            return !field.IsColorDiffField() && !field.IsChromaticityIndexDiff()
                && !field.IsReflectanceDifference() && !field.IsSpecimenAttribute();
        }

        /// <summary>
        /// 判断field是否是样品的特性
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        public static bool IsSpecimenAttribute(this ChromaticityField field)
        {
            return field >= ChromaticityField.ID && field < ChromaticityField.R360;
        }

        /// <summary>
        /// 如果需要标样才能计算返回真
        /// </summary>
        /// <param name="field">ChromaticityField</param>
        /// <returns></returns>
        public static bool IsNeedStandard(this ChromaticityField field)
        {

            //色差项和色差公式都需要标样,指数差也需要标样
            if (field.IsColorDiffField() || field.IsChromaticityIndexDiff())
                return true;

            // 判断需要标样的颜色指数
            switch (field)
            {
                case ChromaticityField.MI1:
                case ChromaticityField.MI2:
                case ChromaticityField.StainingFastness_dEgs:
                case ChromaticityField.StainingFastness_SSRLevel:
                case ChromaticityField.ColorFastness_dEf:
                case ChromaticityField.ColorFastness_GSRLevel:
                case ChromaticityField.AverageStrength:
                case ChromaticityField.PrecentStrengthSWL:
                case ChromaticityField.Judgement:
                case ChromaticityField.Opacity:
                case ChromaticityField.ColorOffset:
                case ChromaticityField.ShadeBy555:
                case ChromaticityField.WeightedStrengthSWL:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// 判断代号是不是色差公式
        /// </summary>
        /// <param name="field">要判断的代号</param>
        /// <returns>如果是丮差公式返回真</returns>
        public static bool IsEquation(this ChromaticityField field)
        {
            switch (field)
            {
                case ChromaticityField.dEab:
                case ChromaticityField.dE94:
                case ChromaticityField.dE00:
                case ChromaticityField.dEcmc:
                case ChromaticityField.dHE:
                case ChromaticityField.dEuv:
                case ChromaticityField.dE99:
                case ChromaticityField.dEcmc11:
                case ChromaticityField.dEcmc21:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// 判断是否为色品座标或色品座标之差
        /// </summary>
        /// <param name="field">要判断的代号</param>
        /// <returns>如果是色品座标或色品座标之差返回真</returns>
        public static bool IsChromaticity(this ChromaticityField field)
        {
            return field.IsColorField() || (field.IsColorDiffField() && !field.IsEquation());
        }

        public static bool IsDoubleValue(this ChromaticityField field)
        {
            return field.IsColorField() || field.IsColorDiffField() || field.IsReflectance() || field.IsReflectanceDifference() ||
                (field.IsChromaticityIndex() && field != ChromaticityField.StainingFastness_SSRLevel &&
                 field != ChromaticityField.ColorFastness_GSRLevel && field != ChromaticityField.ShadeBy555);
        }

        /// <summary>
        /// 判断字段是不是百分比
        /// </summary>
        /// <param name="field"></param>
        /// <returns>如果是百分比返回真,否则返回假</returns>
        public static bool IsPercent(this ChromaticityField field)
        {
            switch (field)
            {
                case ChromaticityField.Opacity:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsDateTime(this ChromaticityField field)
        {
            return field == ChromaticityField.DateTime;
        }

        public static bool IsStringValue(this ChromaticityField field)
        {
            switch (field)
            {
                case ChromaticityField.Name:
                case ChromaticityField.Material:
                case ChromaticityField.InstrumentName:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsEnumValue(this ChromaticityField field)
        {
            switch (field)
            {
                case ChromaticityField.Judgement:
                case ChromaticityField.UV:
                case ChromaticityField.MeasurementCaliber:
                case ChromaticityField.StainingFastness_SSRLevel:
                case ChromaticityField.ColorFastness_GSRLevel:
                case ChromaticityField.ColorOffset:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsIntValue(this ChromaticityField field)
        {
            return field == ChromaticityField.ID || field == ChromaticityField.ShadeBy555;
        }

       
        /// <summary>
        /// 确保名称、测光方式等固定字段始终存在于显示列表中。
        /// </summary>
        public static void EnsureFixedDisplayFields(this IList<ChromaticityField> fields)
        {
            InsertFixedFieldIfMissing(fields, ChromaticityField.Name, () =>
            {
                if (fields.Contains(ChromaticityField.PseudoColor))
                    return fields.IndexOf(ChromaticityField.PseudoColor) + 1;
                return 0;
            });

            InsertFixedFieldIfMissing(fields, ChromaticityField.SpecularComponent, () =>
            {
                if (fields.Contains(ChromaticityField.Name))
                    return fields.IndexOf(ChromaticityField.Name) + 1;
                if (fields.Contains(ChromaticityField.PseudoColor))
                    return fields.IndexOf(ChromaticityField.PseudoColor) + 1;
                return 0;
            });
        }

        private static void InsertFixedFieldIfMissing(
            IList<ChromaticityField> fields,
            ChromaticityField field,
            Func<int> getInsertIndex)
        {
            if (fields.Contains(field))
                return;

            int index = getInsertIndex();
            fields.Insert(Math.Min(index, fields.Count), field);
        }

        // 已删除未使用的 WPF Run 上下标辅助方法，避免 Domain 依赖 PresentationFramework
    }
}
