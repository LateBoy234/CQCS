//////////////////////////////////////////////////////////////////////////
// @File		: clralgo.h
// @Author		: Zhong Jianfeng
// @Date		: 2017/9/26
// @Description	: 色彩空间转换、色差公式、色度指数等接口头文件
//////////////////////////////////////////////////////////////////////////
#ifndef TNH_CLRALGO_H_
#define TNH_CLRALGO_H_
#include <stdint.h>
#include <math.h>
#include "clralgo_dll_defines.h"


#ifdef __cplusplus
extern "C" {
#endif 

//////////////////////////////////////////////////////////////////////////
// 辅助函数

// 判断浮点数是否为NaN

#ifdef NAN
#	define clralgo_FLOAT_NAN       NAN // (*((const float *)(&_clralgo_FLOAT_NAN_)))
#	define clralgo_DOUBLE_NAN      NAN // (*((const double *)(&_clralgo_DOUBLE_NAN_)))
#else
	static const uint32_t _clralgo_FLOAT_NAN_ = -1;
	static const uint64_t _clralgo_DOUBLE_NAN_ = -1;
#	define clralgo_FLOAT_NAN       (*((const float *)(&_clralgo_FLOAT_NAN_)))
#	define clralgo_DOUBLE_NAN      (*((const double *)(&_clralgo_DOUBLE_NAN_)))
#endif

// 判断值是否为NaN
#define clralgo_IS_NAN(val)		((val) != (val))

//////////////////////////////////////////////////////////////////////
// 类型定义


//////////////////////////////////////////////////////////////////////////
// 枚举常量定义

// 标准光源定义
enum clralgo_StandardIlluminant
{
    clralgo_D65,
    clralgo_D50,
    clralgo_A,
    clralgo_C,
    clralgo_D55,
    clralgo_D75,
    clralgo_F1,
    clralgo_F2,
    clralgo_F3,
    clralgo_F4,
    clralgo_F5,
    clralgo_F6,
    clralgo_F7,
    clralgo_F8,
    clralgo_F9,
    clralgo_F10,
    clralgo_F11,
    clralgo_F12,
   // clralgo_H,
    clralgo_CWF,
    clralgo_DLF,
    clralgo_TL83,
    clralgo_TL84,
    clralgo_TPL5,
    clralgo_U30,
    clralgo_B,
    clralgo_U35,
    clralgo_NBF,
    clralgo_ID50,
    clralgo_ID65,
    clralgo_LED_B1,
    clralgo_LED_B2,
    clralgo_LED_B3,
    clralgo_LED_B4,
    clralgo_LED_B5,
    clralgo_LED_BH1,
    clralgo_LED_RGB1,
    clralgo_LED_V1,
    clralgo_LED_V2,
    clralgo_LED_C2,
    clralgo_LED_C3,
    clralgo_LED_C5,
    clralgo_LEDT8G,
    clralgo_U3500,
    clralgo_STANDARD_ILLUMINANT_COUNT,
   
};

// 标准观察者角度定义
enum clralgo_StandardObserver
{
    clralgo_DEGREE_2,		// 2度观察者角
    clralgo_DEGREE_10,		// 10度观察者角
    clralgo_STANDARD_OBSERVER_COUNT
};

// 色牢度等级
enum clralgo_ColorFastnessGrade {
    clralgo_COLOR_FASTNESS_UNDEFINED,	// 色牢度指数在指定条件下未定义
    clralgo_GRADE_1,		// 1
    clralgo_GRADE_1_2,		// 1-2
    clralgo_GRADE_2,		// 2
    clralgo_GRADE_2_3,		// 2-3
    clralgo_GRADE_3,		// 3
    clralgo_GRADE_3_4,		// 3-4
    clralgo_GRADE_4,		// 4
    clralgo_GRADE_4_5,		// 4-5
    clralgo_GRADE_5,		// 5
};


// 药典规定色调
typedef enum PharmacopoeiaTone_t {
    // 绿黄色
    PHARMACOPOEIA_TONE_GY,
    // 黄绿色
    PHARMACOPOEIA_TONE_YG,
    // 黄色
    PHARMACOPOEIA_TONE_Y,
    // 橙黄色
    PHARMACOPOEIA_TONE_OY,
    // 橙红色
    PHARMACOPOEIA_TONE_OR,
    // 棕红色
    PHARMACOPOEIA_TONE_BR,
    // 色调个数
    PHARMACOPOEIA_TONE_COUNT,
} PharmacopoeiaTone_t;



typedef enum {
    Pathlength_30 = 30,
    Pathlength_50 = 50,
    Pathlength_100 = 100
} PathLengthEnum;

//////////////////////////////////////////////////////////////////////////
// 使用的数据结构定义

// 反射率或透射率波长范围
struct clralgo_WavelengthRange
{
    uint16_t start;		// 起始波长
    uint16_t interval;	// 波长间隔
    uint16_t count;		// 返射率个数
};

// 反射率台透射率数据
struct clralgo_Spectra
{
    struct clralgo_WavelengthRange range;
    float *data;
};

// 如果range1和range2相等返回非零，否则返回零
int clralgo_WavelengthRange_is_equal(struct clralgo_WavelengthRange range1, struct clralgo_WavelengthRange range2);
// 获取波长范围的最大波长
int clralgo_WavelengthRange_end(struct clralgo_WavelengthRange range);
// 获取给定波长对应的光源数据的索引
int clralgo_Spectra_index_of(const struct clralgo_Spectra *spectra, int wavelength);
// 获取给定波长处的反射/透射率
float clralgo_Spectra_get(const struct clralgo_Spectra *spectra, int wavelength);
// 设置给定波长处的反射/透射率
void clralgo_Spectra_set(struct clralgo_Spectra *spectra, int wavelength, float value);

#define clralgo_WavelengthRange_is_equal(range1, range2) \
    ((range1).start == (range2).start && (range1).interval == (range2).interval && (range1).count == (range2).count)
#define clralgo_WavelengthRange_end(range)  ((range).start + ((range).count - 1) * (range.interval))
#define clralgo_Spectra_index_of(spectra, wavelength) \
    (((wavelength) - (spectra)->range.start) / (spectra)->range.interval)
#define clralgo_Spectra_get(spectra, wavelength)    \
    ((spectra)->data[clralgo_Spectra_index_of(spectra, wavelength)])
#define clralgo_Spectra_set(spectra, wavelength, value) \
    ((spectra)->data[clralgo_Spectra_index_of(spectra, wavelength)] = value)

//////////////////////////////////////////////////////////////////////////
// 颜色空间相关结构定义

// CIE XYZ颜色空间
struct clralgo_CIEXYZ
{
    float X;
    float Y;
    float Z;
};

// CIE Yxy颜色空间
struct clralgo_CIEYxy
{
    float Y;
    float x;
    float y;
};

// CIE Lab颜色空间
struct clralgo_CIELab
{
    float L;
    float a;
    float b;
};

// CIE LCh 颜色空间
struct clralgo_CIELCh
{
    float L;
    float C;
    float h;
};

// Hunter Lab颜色空间
struct clralgo_HunterLab
{
    float L;
    float a;
    float b;
};

// CIE Luv 颜色空间
struct clralgo_CIELuv
{
    float L;
    float u;
    float v;
    float uq;
    float vq;
};

// CIE RGB 颜色空间
struct clralgo_CIERGB
{
    float R;
    float G;
    float B;
};

// sRGB 颜色空间的值
// 注意：RGB的值会被归一化为0到1之间，如果要转换为0到255请乘以255
struct clralgo_sRGB
{
    float R;
    float G;
    float B;
};

// DIN Lab99
struct clralgo_DIN99Lab
{
	float L;
	float a;
	float b;
};

// CIE βxy颜色空间
typedef struct clralgo_BETAxy
{
    float BETA;
    float x;
    float y;
}_BETAxy;

typedef struct clralgo_Munsell
{
    float H;
    float V;
    float C;
    char  H_str[2];

}_Munsell;


typedef enum clralgo_StandardIlluminant     clralgo_StandardIlluminant;
typedef enum clralgo_StandardObserver       clralgo_StandardObserver;
typedef enum clralgo_ColorFastnessGrade     clralgo_ColorFastnessGrade;
typedef struct clralgo_WavelengthRange      clralgo_WavelengthRange;
typedef struct clralgo_Spectra              clralgo_Spectra;
typedef struct clralgo_CIEXYZ               clralgo_CIEXYZ;
typedef struct clralgo_CIEYxy               clralgo_CIEYxy;
typedef struct clralgo_CIELab               clralgo_CIELab;
typedef struct clralgo_CIELCh               clralgo_CIELCh;
typedef struct clralgo_CIELuv               clralgo_CIELuv;
typedef struct clralgo_CIERGB               clralgo_CIERGB;
typedef struct clralgo_sRGB                 clralgo_sRGB;
typedef struct clralgo_HunterLab            clralgo_HunterLab;
typedef struct clralgo_DIN99Lab			    clralgo_DIN99Lab;
typedef struct clralgo_BETAxy				clralgo_BETAxy;


//////////////////////////////////////////////////////////////////////////
// 颜色空间转换函数


// 获取CIE标准光源的三刺激值
    // 参数：
    // [in] observer:   要获取的三刺激值所在的观察者角度
    // [in] illuminant: 要获取其三刺激值的标准光源
    // 返回：标准光源illuminant的三刺激值
//CLRALGO_API struct clralgo_CIEXYZ clralgo_get_white_point_of_illuminant(enum clralgo_StandardObserver observer,
//    enum clralgo_StandardIlluminant illuminant);

// 将XYZ转换为Yxy
// 参数：
// [in]  xyz:	要转换的XYZ值
// [out] yxy:	返回转换的Yxy值
CLRALGO_API void clralgo_xyz_to_yxy(const struct clralgo_CIEXYZ *xyz, struct clralgo_CIEYxy *yxy);

// 将Yxy值转换为XYZ值
// 参数：
// [in]  yxy: 要转换的Yxy值
// [out] xyz: 存储转换后的结果
CLRALGO_API void clralgo_yxy_to_xyz(const struct clralgo_CIEYxy *yxy, struct clralgo_CIEXYZ *xyz);

	// 颜色空间转换函数
// 将XYZ转换为BETAxy
// 参数：
// [in]  xyz:	要转换的XYZ值
// [out] BETAxy:	返回转换BETAxy
CLRALGO_API void clralgo_xyz_to_BETAxy(const struct clralgo_CIEXYZ *xyz, struct clralgo_BETAxy *BETAxy);

// 将BETAxy值转换为XYZ值
// 参数：
// [in]  BETAxy: 要转换的BETAxy值
// [out] xyz: 存储转换后的结果
CLRALGO_API void clralgo_BETAxy_to_xyz(const struct clralgo_BETAxy *BETAxy, struct clralgo_CIEXYZ *xyz);

// 将Lab值转换为LCh值
// 参数：
// [in]  lab: 要转换的Lab值
// [out] lch: 存储转换的结果
CLRALGO_API void clralgo_lab_to_lch(const struct clralgo_CIELab *lab, struct clralgo_CIELCh *lch);

// 将LCh值转换为lab值
// [in]  lch: 要转换的LCh值
// [out] lab: 存储转换后的结果
CLRALGO_API void clralgo_lch_to_lab(const struct clralgo_CIELCh *lch, struct clralgo_CIELab *lab);


// 通过光谱计算XYZ值
// 参数：
// [in]	 spectra:		反射率或透射率数据
// [in]  illuminant:	要计算的XYZ所在光源
// [in]  observer:		要计算的XYZ的所在观察者角度
// [out] xyz:			存储计算结果
CLRALGO_API void clralgo_calculate_xyz(const struct clralgo_Spectra *spectra,
                               enum clralgo_StandardIlluminant illuminant,
                               enum clralgo_StandardObserver observer,
                               struct clralgo_CIEXYZ *xyz);

// 通过光谱计算Lab值
// 参数：
// [in]	 spectra:		反射率或透射率数据
// [in]  illuminant:	要计算的XYZ所在光源
// [in]  observer:		要计算的XYZ的所在观察者角度
// [out] lab:			存储计算结果
CLRALGO_API void clralgo_calculate_lab(const struct clralgo_Spectra *spectra,
                               enum clralgo_StandardIlluminant illuminant,
                               enum clralgo_StandardObserver observer,
                               struct clralgo_CIELab *lab);

// 将XYZ转换为Lab颜色空间
// 参数：
// [in]  illuminant:	XYZ值所在光源
// [in]  observer:		XYZ值所在观察者角度
// [in]  xyz:			要转换的XYZ值
// [out] lab:			存储转换结果
CLRALGO_API void clralgo_xyz_to_lab(enum clralgo_StandardIlluminant illuminant,
                            enum clralgo_StandardObserver observer,
                            const struct clralgo_CIEXYZ *xyz, struct clralgo_CIELab *lab);

// 将Lab转换为XYZ颜色空间
// 参数：
// [in]  illuminant:	Lab值所在光源
// [in]  observer:		Lab值所在观察者角度
// [in]  lab:			要转换的Lab值
// [out] xyz:			存储转换结果
CLRALGO_API void clralgo_lab_to_xyz(enum clralgo_StandardIlluminant illuminant,
                            enum clralgo_StandardObserver observer,
                            const struct clralgo_CIELab *lab, struct clralgo_CIEXYZ *xyz);

// 将XYZ转换为Hunter Lab颜色空间
// 参数：
// [in]  illuminant:	XYZ值所在光源
// [in]  observer:		XYZ值所在观察者角度
// [in]  xyz:			要转换的XYZ值
// [out] lab:			存储转换结果
CLRALGO_API void clralgo_xyz_to_hunterlab(enum clralgo_StandardIlluminant illuminant,
                                  enum clralgo_StandardObserver observer,
                                  const struct clralgo_CIEXYZ *xyz, struct clralgo_HunterLab *lab);

// 将Hunter Lab转换为XYZ颜色空间
// 参数：
// [in]  illuminant:	Hunter Lab值所在光源
// [in]  observer:		Hunter Lab值所在观察者角度
// [in]  lab:			要转换的Hunter Lab值
// [out] xyz:			存储转换结果
CLRALGO_API void clralgo_hunterlab_to_xyz(enum clralgo_StandardIlluminant illuminant,
                                  enum clralgo_StandardObserver observer,
                                  const struct clralgo_HunterLab *lab,
                                  struct clralgo_CIEXYZ *xyz);

// 将XYZ转换为Luv颜色空间
// 参数：
// [in]  illuminant:	XYZ值所在光源
// [in]  observer:		XYZ值所在观察者角度
// [in]  xyz:			要转换的XYZ值
// [out] luv:			存储转换结果
CLRALGO_API void clralgo_xyz_to_luv(enum clralgo_StandardIlluminant illuminant,
                            enum clralgo_StandardObserver observer,
                            const struct clralgo_CIEXYZ *xyz,
                            struct clralgo_CIELuv *luv);

// 将Luv转换为XYZ颜色空间
// 参数：
// [in]  illuminant:	Luv值所在光源
// [in]  observer:		Luv值所在观察者角度
// [in]  luv:			要转换的Luv值
// [out] xyz:			存储转换结果
CLRALGO_API void clralgo_luv_to_xyz(enum clralgo_StandardIlluminant illuminant,
                            enum clralgo_StandardObserver observer,
                            const struct clralgo_CIELuv *luv,
                            struct clralgo_CIEXYZ *xyz);

// 将XYZ转换为CIE RGB颜色空间
// 参数：
// [in]  xyz:			要转换的XYZ值
// [out] rgb:			存储转换结果
CLRALGO_API void clralgo_xyz_to_ciergb(const struct clralgo_CIEXYZ *xyz,
                               struct clralgo_CIERGB *rgb);

// 将CIE RGB转换为XYZ颜色空间
// 参数：
// [in]  rgb:			要转换的CIE RGB值
// [out] xyz:			存储转换结果
CLRALGO_API void clralgo_ciergb_to_xyz(const struct clralgo_CIERGB *rgb,
                               struct clralgo_CIEXYZ *xyz);

// 将XYZ转换为sRGB颜色空间
// 参数：
// [in]  illuminant:	XYZ值所在光源
// [in]  observer:		XYZ值所在观察者角度
// [in]  xyz:			要转换的XYZ值
// [out] srgb:			存储转换结果
CLRALGO_API void clralgo_xyz_to_srgb(enum clralgo_StandardIlluminant illuminant,
                             enum clralgo_StandardObserver observer,
                             const struct clralgo_CIEXYZ *xyz,
                             struct clralgo_sRGB *rgb);

// 将CIELab转换为sRGB颜色空间
// 参数：
// [in]  lab:			要转换的lab值
// [out] srgb:			存储转换结果
CLRALGO_API void clralgo_lab_to_srgb(const struct clralgo_CIELab *lab,
                             struct clralgo_sRGB *rgb);



// 将CIELab转换成DIN99，根据DIN 6176
// 参数：
// [in] lab:		要转换的Lab值
// [out] din99:		存储转换后的结果
CLRALGO_API void clralgo_lab_to_din99(
	const clralgo_CIELab *lab,
	clralgo_DIN99Lab *din99);
//////////////////////////////////////////////////////////////////////////
// 色差公式相关函数

// 计算lch1 - lch2的ΔL*、ΔC*、ΔH*
// 参数：
// [in] lch1: 样品1的LCh值
// [in] lch2: 样品2的LCh值
// [out] dL, dC, dH: 用于存储 ΔL*、ΔC*、ΔH*
CLRALGO_API void clralgo_calculate_dLCH(const struct clralgo_CIELCh *lch1,
                                const struct clralgo_CIELCh *lch2,
                                float *dL, float *dC, float *dH);

// 使用ΔE*ab计算lab1 - lab2的色差
// 参数：
// [in] lab1: 样品1的Lab值
// [in] lab2: 样品2的Lab值
// 返回：ΔE*ab
CLRALGO_API float clralgo_calculate_dEab(const struct clralgo_CIELab *lab1,
                                 const struct clralgo_CIELab *lab2);

// 使用CIE 1994色差公式计算lab1 - lab2的色差
// 参数：
// [in] lab1: 样品1的Lab值
// [in] lab2: 样品2的Lab值
// [in] KL, KC, KH: 参数因子，一盘使用默认的1:1:1
// [out, opt] dL : 如果不为NULL存储ΔL'94
// [out, opt] dC : 如果不为NULL存储ΔC'94
// [out, opt] dH : 如果不为NULL存储ΔH'94
// 返回：ΔE*94
CLRALGO_API float clralgo_calculate_CIEDE94(const struct clralgo_CIELab *lab1,
                                    const struct clralgo_CIELab *lab2,
                                    float KL, float KC, float KH,
                                    float *dL, float *dC, float *dH);

// 使用CIE 2000色差公式计算lab1 - lab2的色差
// 参数：
// [in] lab1: 样品1的Lab值
// [in] lab2: 样品2的Lab值
// [in] KL, KC, KH: 参数因子，一盘使用默认的1:1:1
// [out, opt] dL : 如果不为NULL存储ΔL'00
// [out, opt] dC : 如果不为NULL存储ΔC'00
// [out, opt] dH : 如果不为NULL存储ΔH'00
// 返回：ΔE*00
CLRALGO_API float clralgo_calculate_CIEDE00(const struct clralgo_CIELab *lab1,
                                    const struct clralgo_CIELab *lab2,
                                    float KL, float KC, float KH,
                                    float *dL, float *dC, float *dH);

// 使用CMC(l:c)色差公式计算lab1 - lab2的色差
// 参数：
// [in] lab1: 样品1的Lab值
// [in] lab2: 样品2的Lab值
// [in] l, c: 参数因子，一盘使用默认的2:1
// [out, opt] dL : 如果不为NULL存储ΔL'cmc(l:c)
// [out, opt] dC : 如果不为NULL存储ΔC'cmc(l:c)
// [out, opt] dH : 如果不为NULL存储ΔH'cmc(l:c)
// 返回：CMC(KL:KC)
CLRALGO_API float clralgo_calculate_CMC(const struct clralgo_CIELab *lab1,
                                const struct clralgo_CIELab *lab2,
                                float l, float c,
                                float *dL, float *dC, float *dH);

// 计算Hunter Lab的色差ΔE
// 参数：
// [in] lab1: 样品1的Hunter Lab值
// [in] lab2: 样品2的Hunter Lab值
// 返回：ΔE
CLRALGO_API float clralgo_calculate_hunter_dE(const struct clralgo_HunterLab *lab1,
                               const struct clralgo_HunterLab *lab2);

// 计算Luv的色差ΔE*uv
// 参数：
// [in] luv1: 样品1的CIELuv值
// [in] luv2: 样品2的CIELuv值
// 返回：ΔE*uv
CLRALGO_API float clralgo_calcuate_dEuv(const struct clralgo_CIELuv *luv1,
                                const struct clralgo_CIELuv *luv2);


//////////////////////////////////////////////////////////////////////////
// 颜色指数相关函数

// 计算ASTM E313-2005白度指数（仅对D65、D50、C光源有效）
// 参数：
// [in] illuminant:		xyz所在光源
// [in] observer:		xyz所在观察者角度
// [in] xyz:			要计算其白度指数的XYZ的值
// [out, opt] T:     如果不为空，存储Tint Index
// 返回：如果光源和观察者角度有效返回白度指数，否则返回NaN
CLRALGO_API float clralgo_calculate_whiteness_astm_e313_2005(
        enum clralgo_StandardIlluminant illuminant,
        enum clralgo_StandardObserver observer,
        const struct clralgo_CIEYxy *yxy, float *T);

// 计算WI(CIE/ISO)：符合标准：ISO 11475，CIE 86，甘茨白度公式，AATCC 110
// 注意：该公式仅在D65、10度下有效
// [in] illuminant:		xyz所在光源
// [in] observer:		xyz所在观察者角度
// [in] xyz:			要计算其白度指数的XYZ的值
// [out, opt] T:     如果不为空，存储Tint Index
// 返回：如果光源和观察者角度有效返回白度指数，否则返回NaN
CLRALGO_API float clralgo_calculate_whiteness_cie_iso(
        enum clralgo_StandardIlluminant illuminant,
        enum clralgo_StandardObserver observer,
        const struct clralgo_CIEYxy *yxy, float *T);

// 计算Tappl（坦伯）白度指数（仅对A、D55、D65、D75、C光源有效）
// 参数：
// [in] illuminant:		xyz所在光源
// [in] observer:		xyz所在观察者角度
// [in] xyz:			要计算其白度指数的XYZ的值
// 返回：如果光源和观察者角度有效返回白度指数，否则返回NaN
CLRALGO_API float clralgo_calculate_whiteness_tappl(
        enum clralgo_StandardIlluminant illuminant,
        enum clralgo_StandardObserver observer,
        const struct clralgo_CIEXYZ *xyz);

// 计算Taube（陶贝）白度指数（仅对A、D55、D65、D75、C光源有效）
// 参数：
// [in] illuminant:		xyz所在光源
// [in] observer:		xyz所在观察者角度
// [in] xyz:			要计算其白度指数的XYZ的值
// 返回：如果光源和观察者角度有效返回白度指数，否则返回NaN
CLRALGO_API float clralgo_calculate_whiteness_taube(
        enum clralgo_StandardIlluminant illuminant,
        enum clralgo_StandardObserver observer,
        const struct clralgo_CIEXYZ *xyz);

// 计算Hunter 1942白度指数（仅对2度观察者C光源和10度观察者D65光源有效）
// 参数：
// [in] illuminant:		lab所在光源
// [in] observer:		lab所在观察者角度
// [in] lab:			要计算其白度指数的Hunter Lab值
// 返回：如果光源和观察者角度有效返回白度指数，否则返回NaN
CLRALGO_API float clralgo_calculate_whiteness_hunter1942(
        enum clralgo_StandardIlluminant illuminant,
        enum clralgo_StandardObserver observer,
        const struct clralgo_HunterLab *lab);

// 计算Hunter 1960白度指数（仅对2度观察者C光源和10度观察者D65光源有效）
// 参数：
// [in] illuminant:		lab所在光源
// [in] observer:		lab所在观察者角度
// [in] lab:			要计算其白度指数的Hunter Lab值
// 返回：如果光源和观察者角度有效返回白度指数，否则返回NaN
CLRALGO_API float clralgo_calculate_whiteness_hunter1960(
        enum clralgo_StandardIlluminant illuminant,
        enum clralgo_StandardObserver observer,
        const struct clralgo_HunterLab *lab);

// 计算R457白度；注意：R457仅在A光源下有效
// 参数：
// [in] illuminant: 当前光源
// [in] spectra:	光谱数据
// 返回：返回白度指数；如果光源不是A光源返回NaN
CLRALGO_API float clralgo_calculate_whiteness_r457(
        enum clralgo_StandardIlluminant illuminant,
        const struct clralgo_Spectra *spectra);

// 计算Berger（59）白度
// 注：Berger定义在C/2条件下，但在其他光源/观察者下也OK。
// [in] illuminant: 当前光源
// [in] observer: 当前观察者角度
// [in] xyz: XYZ值
CLRALGO_API float clralgo_calculate_whiteness_berger59(
	enum clralgo_StandardIlluminant illuminant,
	enum clralgo_StandardObserver observer,
	const struct clralgo_CIEXYZ *xyz
);

/**
 * @brief 计算苷茨白度（仅限D60/10度，其他光源计算出来的不准确）
 * 
 * @param xyz 
 * @return CLRALGO_API 
 */
CLRALGO_API float clralgo_calculate_whiteness_ganz_and_grieser(
	const struct clralgo_CIEXYZ *xyz
);


/**
 * @brief 计算Ganz Grieser 色调（仅限D60/10度，其他光源计算出来的不准确）
 * 
 * @param xyz 
 * @return CLRALGO_API 
 */
CLRALGO_API float clralgo_calculate_tint_ganz_and_grieser(
	const struct clralgo_CIEXYZ *xyz
);

// 计算ASTM D1925-1970黄度指数（仅在2度观察角C光源下有效）
// 参数：
// [in] illuminant:		xyz所在光源
// [in] observer:		xyz所在观察者角度
// [in] xyz:			要计算其黄度指数的XYZ的值
// 返回：如果光源和观察者角度有效返回黄度指数，否则返回NaN
CLRALGO_API float clralgo_calculate_yellowness_astm_d1925_1970(
        enum clralgo_StandardIlluminant illuminant,
        enum clralgo_StandardObserver observer,
        const struct clralgo_CIEXYZ *xyz);

// 计算ASTM E313-2005黄度指数（仅对C光源和D65光源有效）
// 参数：
// [in] illuminant:		xyz所在光源
// [in] observer:		xyz所在观察者角度
// [in] xyz:			要计算其黄度指数的XYZ的值
// 返回：如果光源和观察者角度有效返回黄度指数，否则返回NaN
CLRALGO_API float clralgo_calculate_yellowness_astm_e313_2005(
        enum clralgo_StandardIlluminant illuminant,
        enum clralgo_StandardObserver observer,
        const struct clralgo_CIEXYZ *xyz);


// 通过XYZ值计算同色异谱指数
// 参数：
// [in] spectra1:	样品1的光谱
// [in] spectra2:	样品2的光谱
// [in]	illuminant: 测试光源
// [in] observer:	测试观察者角度
// [in] ref_illuminant:	参考光源
// [in] ref_observer:	参考观察者角度
// 返回：样品1和样品2的同色异谱指数
CLRALGO_API float clralgo_calculate_metamerism_index(
        const struct clralgo_Spectra *spectra1,
        const struct clralgo_Spectra *spectra2,
        enum clralgo_StandardIlluminant illuminant,
        enum clralgo_StandardObserver observer,
        enum clralgo_StandardIlluminant ref_illuminant,
        enum clralgo_StandardObserver ref_observer);


// 通过XYZ值计算同色异谱指数
// 参数：
// [in]	illuminant: xyz1和xyz2的测试光源和观察者角度
// [in] observer:	xyz1和xyz2的测试光源和观察者角度
// [in] xyz1:		样品1的XYZ值
// [in] xyz2:		样品2的XYZ值
// [in] ref_illuminant:	参考光源
// [in] ref_observer:	参考观察者角度
// [in] xyz1_ref:	样品1在参考条件下测得的XYZ值
// [in] xyz2_ref:	样品2在参考条件下测得的XYZ值
// 返回：样品1和样品2的同色异谱指数
CLRALGO_API float clralgo_calculate_metamerism_index_by_xyz(
        enum clralgo_StandardIlluminant illuminant,
        enum clralgo_StandardObserver observer,
        const struct clralgo_CIEXYZ *xyz1,
        const struct clralgo_CIEXYZ *xyz2,
        enum clralgo_StandardIlluminant ref_illuminant,
        enum clralgo_StandardObserver ref_observer,
        const struct clralgo_CIEXYZ *xyz1_ref,
        const struct clralgo_CIEXYZ *xyz2_ref);

// 计算沾色牢度（仅在D65/10度和C/2度有效）
// 参数：
// [in] illuminant:		standard和test使用的标准光源
// [in] observer:		standard和test使用的标准观察者角度
// [in] test:			试样的Lab值
// [in] standard:		标样的Lab值
// [out, opt] ssr:		如果不为空存储连续的色牢度等级
// [out, opt] dEgs:		如果不为空存储ΔEgs
// 返回：沾色牢度；如果指定条件无效返回clralgo_COLOR_FASTNESS_UNDEFINED
CLRALGO_API enum clralgo_ColorFastnessGrade clralgo_calculate_ssr(
        enum clralgo_StandardIlluminant illuminant,
        enum clralgo_StandardObserver observer,
        const struct clralgo_CIELab *test,
        const struct clralgo_CIELab *standard,
        float *ssr, float *dEgs);

// 计算变色牢度（仅在D65和C光源有效）
// 参数：
// [in] illuminant:		standard和test使用的标准光源
// [in] observer:		standard和test使用的标准观察者角度
// [in] test:			试样的Lab值
// [in] standard:		标样的Lab值
// [out, opt] gsr:		如果不为空存储连续的色牢度等级
// [out, opt] dEf:		如果不为空存储ΔEf
// 返回：变色牢度；如果指定条件无效返回clralgo_COLOR_FASTNESS_UNDEFINED
CLRALGO_API enum clralgo_ColorFastnessGrade clralgo_calculate_gsr(
        enum clralgo_StandardIlluminant illuminant,
        enum clralgo_StandardObserver observer,
        const struct clralgo_CIELab *test,
        const struct clralgo_CIELab *standard,
        float *gsr, float *dEf);

// 根据样品反射率计算 Color Value SUM。具体算法参考HunterLab的《Strength Calculations》
// 参数：
// [in] spectra: 反射率光谱
// 返回：Color Value SUM
CLRALGO_API float clralgo_calculate_color_value_sum(const struct clralgo_Spectra *spectra,int isTransmittance);

// 计算 Color Value SWL，即最小反射率的K/S；具体算法参考HunterLab的《Strength Calculations》
// 参数：
// [in] spectra:					 反射率光谱
// [out, opt] wavelength_of_minimum: 如果不为NULL存储取小反射率所在波长
// 返回：Color Value SWL，即最小反射率的K/S
CLRALGO_API float clralgo_calculate_color_value_swl(
        const struct clralgo_Spectra *spectra, int isTransmittance, int *wavelength_of_minimum);

// 计算 Weighted Strength 具体算法参考HunterLab的《Strength Calculations》和《Color iQC and Color iMatch》
    // 参数：
    // [in] illuminant: 光源
    // [in] observer: 观察者
    // [in] trialSpectra:	试样光谱
    // [in] stdSpectra: 标样光谱
    // [in] isTransmittance: 非零表示是透射光谱，0表示是反射光谱
    // 返回：Weighted Strength
float clralgo_calculate_weighted_strength(enum clralgo_StandardIlluminant illuminant,
    enum clralgo_StandardObserver observer,
    const struct clralgo_Spectra* trialSpectra,
    const struct clralgo_Spectra* stdSpectra,
    int isTransmittance);



// 计算Average Strength（也叫做 % Strength SUM）；具体算法参考HunterLab的《Strength Calculations》
// 参数：
// [in] test:		试样光谱数据
// [in] standard:	标样光谱数据
// 返回：Average Strength（也叫做 % Strength SUM）
CLRALGO_API float clralgo_calculate_average_strength(
        const struct clralgo_Spectra *test, const struct clralgo_Spectra *standard, int isTransmittance);

// 计算 %Strength-SWL，算法参考《Strength Calculations》
// 参数：
// [in] test:		试样光谱数据
// [in] standard:	标样光谱数据
// [out, opt] wavelength_of_minimum: 如果不为空存储标样最小反射率所在波长
// 返回：%Strength-SWL
CLRALGO_API float clralgo_calculate_precent_strength_swl(
        const struct clralgo_Spectra *test,
        const struct clralgo_Spectra *standard,
        int *wavelength_of_minimum);
// 计算%Strength。算法参考《Strength Calculations》
// 参数：
// [in] test:			试样光谱数据，波长范围必须与标样相同
// [in] standard:		标样光谱数据
// [out] strengths:		数组，其大小与标样的反射率个数相同，用于存储计算得到的
//						各个波长点的反射率的%Strength
// 注意：标样和试样的反射率波长范围必须相同
CLRALGO_API void clralgo_calculate_precent_strength(
        const struct clralgo_Spectra *test,
        const struct clralgo_Spectra *standard,
        float *strengths);

// 计算遮盖度
// 参数：
// [in] Ywhite:	白底样品的Y值
// [in] Yblack:	黑底样品的Y值
// 返回：遮盖度
CLRALGO_API float clralgo_calculate_opacity(float Ywhite, float Yblack);


//////////////////////////////////////////////////////////////////////////////////////
// 其他函数

// 使用三参样条曲线插值算法插值
// 参数：
// [in] in_x_range: 输入的点X方向上的坐标，固定间隔
// [in] in_y_data:  输入的点Y方向上的坐标
// [in] out_x_range: 输出点的X方向坐标的范围
// [out] out_y_data: 返回插值结果
CLRALGO_API void clralgo_interpolate(const struct clralgo_WavelengthRange *in_x_range, const float *in_y_data,
                                     const struct clralgo_WavelengthRange *out_x_range, float *out_y_data);

// 使用三参样条曲线插值算法插值
// 参数：
// [in] in_x_data: 输入的点X方向上的坐标
// [in] in_y_data:  输入的点Y方向上的坐标
// [in] out_x_range: 输出点的X方向坐标的范围
// [out] out_y_data: 返回插值结果
CLRALGO_API void clralgo_interpolate2(uint32_t count, const float *in_x_data, const float *in_y_data,
                                      const struct clralgo_WavelengthRange *out_x_range, float *out_y_data);

// 计算色调555分类
// 参数：
// [in] test: 试样
// [in] standard: 标样
// [in] tolL, tolA, tolB: L、a、b的对称容差
// 返回：555色调分类，零表示参数有误
CLRALGO_API int clralgo_get_555_shade(
	const struct clralgo_CIELab *test,
	const struct clralgo_CIELab *standard,
	float tolL, float tolA, float tolB);


// 计算透射Gardner指数
// 参数：
// [in] xyz: 要计算其Gardner指数的透射样品的XYZ值（C光源2度角）
// 返回：成功返回相应的Gardner指数，失败返回负数
CLRALGO_API float clralgo_calculate_gardner_index(const struct clralgo_CIEXYZ* xyz);


/**
 * @brief 计算铂-钴指数
 * 
 * @param xyz 透射样品C/2下的XYZ值
 * @return 如果结果无效返回NaN，否则返回指数
 */
CLRALGO_API float clralgo_calculate_YI_Optical_path_conversion(int Pathlength, float YI);
// 计算雾度指数
// [in] T1: 入射光（对于便捷算法可设为100
// [in] T2: 样品总穿透光
// [in] T3: 仪器散射的光（对于便捷算法可直接设为0）
// [in] T4: 仪器和样品散射的光
// [out] haze: 雾度
// [out] Tt: 样品透光率
// [out] Td: 样品散透光率
// 返回：数据有效返回非零值，无效返回零
CLRALGO_API int clralgo_calculate_haze(float T1, float T2, float T3, float T4,
	float *haze, float *Tt, float *Td);
/// <summary>
/// 计算8度光泽度
/// </summary>
/// <param name="ssci">样品反射率SCI</param>
/// <param name="ssce">样品反射率SCE</param>
/// <param name="wsci">白板反射率SCI</param>
/// <param name="wsce">白板反射率SCE</param>
/// <returns></returns>
CLRALGO_API unsigned short clralgo_8_gloss_data(clralgo_Spectra* ssci, clralgo_Spectra* ssce, clralgo_Spectra* wsci, clralgo_Spectra* wsce);

CLRALGO_API float clralgo_calculate_klett_index(const struct clralgo_CIEXYZ* xyz);
/**
 * @brief 药典比色
 * 
 * @note 1. 该函数适用于色差计检测法（采样YS6060的透射功能），对药品只适用于测定澄清液体的颜色，
 *          浑浊液体，黏性液体，带有荧光的液体不适合采用该方法。
 *       2.溶液颜色会随着被测定液体液厚改变而不同，测量需要用光程10mm的石英比色皿。表面无划痕。
 *         可以用清洗液清洗。
 * @param tone 要对比的色调
 * @param sample 测试样品在D65/10度下的XYZ值
 * @return 返回样品在相应色调下的色号
 */
CLRALGO_API float clralgo_pharmacopoeia_colorimetric(PharmacopoeiaTone_t tone, const clralgo_CIEXYZ *sample);

/**
 * @brief 计算Saybolt指数
 * 
 * @param xyz 透射样品C/2条件下的XYZ值
 * @return 返回Saybolt指数；如果结果无效返回NaN 
 */
CLRALGO_API float clralgo_calculate_sayblot(const clralgo_CIEXYZ* xyz);


/**
 * @brief 计算ASTM D1500指数
 * 
 * @param xyz C/2 下的透射样品的XYZ值
 * @return 如果结果有效返回相应结果，否则返回NaN
 */
CLRALGO_API float clralgo_calculate_astm_d1500(const clralgo_CIEXYZ* xyz);

CLRALGO_API void clralgo_xyz_to_munsell(struct clralgo_CIEXYZ* xyz, struct clralgo_Munsell* munsell);

// 计算 Color Value SWL （指定波长点）；具体算法参考HunterLab的《Strength Calculations》
// 参数：
// [in] spectra:					 反射率光谱
// [in] wavelength: 指定波长点
// [in] isTransmittance: 非零表示是透射光谱，0表示是反射光谱
// 返回：Color Value SWL，即最小反射率的K/S
CLRALGO_API float clralgo_calculate_color_value_swl_at(
    const struct clralgo_Spectra* spectra, int wavelength, int isTransmittance);

// 将400-700的反射率计算得到360-780的反射率
// 参数：
// [in]  source:	400-700的反射率
// [out] target:	返回转换计算到的360-780反射率值
CLRALGO_API float*  clralgo_reflect_400_700_to_reflect_360_780(clralgo_Spectra* source,float* reflect);


/// <summary>
/// 计算WI(gost 16873)
/// </summary>
/// <param name="lab">CIELab</param>
/// <returns></returns>
CLRALGO_API float clralgo_calculate_whiteness_gost_16873(const struct  clralgo_CIELab* lab);


#ifdef __cplusplus
}		// end extern "C"
#endif

#endif // TNH_CLRALGO_H_
