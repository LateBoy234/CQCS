#ifndef DENSITY_ALGO_H_
#define DENSITY_ALGO_H_
#include <cstdint>

#ifdef __cplusplus
extern "C" {
#endif

// 自动密度选择K值时的最大密度度减最小密度的阈值
#define VISUAL_DENSITY_THRESH 0.08f

enum clralgo_StandardIlluminant;
enum clralgo_StandardObserver;

/// 密度颜色
typedef enum DensityColor
{
	DENSITY_AUTO,
	DENSITY_C,
	DENSITY_M,
	DENSITY_Y,
	DENSITY_K,
	DENSITY_SPOT,
} DensityColor;

/// 状态
typedef enum DensityStatus
{
	DENSITY_ISO_STATUS_T,
	// ISO Status T
	DENSITY_ISO_STATUS_E,
	// ISO Status E
	DENSITY_ISO_STATUS_I,
	// ISO Status I
	DENSITY_ISO_STATUS_A,
	// ISO Status A
} DensityStatus;

typedef struct SpectralData
{
	uint16_t start; // 起始波长
	uint16_t interval; // 波长间隔
	uint16_t count; // 波长个数
	float* data; // 光谱数据
} SpectralData;


#define MAKE_DENSITY_TYPE(density_status, density_color)	(((density_status) << 4) | ((density_color) & 0x0F))

/**
 * 计算单项密度
 * @Param spectral_data 反射率
 * @Param status 使用的密度状态
 * @Param color 要计算的密度颜色，如果为DENSITY_AUTO，则自动选择；否则计算相应的颜色的密度
 * （注意；不能为DENSITY_SPOT，如果要计算专色密度请使用density_calculate_spot）
 *	如果ret_color不为空，则存储返回的密度使用的颜色
 * @Param ret_color 如果不为空，存储返回的密度使用的颜色；
 * @Param cmyk 如果不为空（长度必须为4），存储计算的C、M、Y、K密度
 * @Param grayness 如果不为空存储相灰度值
 * @Param hue_error 如果不为空存储色调差
 * @return 相应的密度
 */
float density_calculate(const SpectralData* spectral_data, DensityStatus status, DensityColor color,
                        DensityColor* ret_color, float* cmyk, float* grayness, float* hue_error);


/**
 * 计算CMYK密度
 * @Param spectral_data 反射率
 * @Param iso_status DENSITY_ISO_STATUS_T、DENSITY_ISO_STATUS_E、DENSITY_ISO_STATUS_I、DENSITY_ISO_STATUS_A之一
 * @Param cmyk 存储计算结果，必须为长度为4的数组
 * @return 无
 */
void density_calculate_cmyk(const struct SpectralData* spectral_data, DensityStatus status, float* cmyk);

/**
 * 获取自动密度
 * @Param cmyk C,M,Y,K的密度值，不能为空
 * @return 自动密度的类型(DENSITY_C, DENSITY_M, DENSITY_Y, DENSITY_COLOR_K之一）
 */
DensityColor density_get_auto_density(const float* cmyk);

/**
 * 由CMY密度获取灰度值
 * @Param cmy C,M,Y的密度值，不能为空
 * @return 灰度的百分比
 */
float density_calculate_grayness(const float* cmy);

/**
 * 计算色调差
 * @Param cmy C,M,Y的密度值，不能为空
 * @return 色调差
 */
float density_calculate_hue_error(const float* cmy);

/**
 * 计算专色密度
 * @Param spectral_data 反射率
 * @Param wavelength 返回专色的波长
 * @return 返回相应专色密度
 */
float density_calculate_spot(const struct SpectralData* spectral_data, int* wavelength);

/**
 * 计算指定波长位置的专色密度
 */
float density_calculate_specific_spot(const struct SpectralData* spectral_data, int wavelength);

/**
 * 计算密度光谱，即计算所有反射率的专色密度
 * @Param spectral_data 反射率
 * @Param density_spectrum 输出密度光谱，数组大小必须与输入反射率的数据个数一样
 * @return void
 */
void density_calculate_density_spectrum(const struct SpectralData* spectral_data, float density_spectrum[]);

/**
 * 使用默里·戴维斯方式计算色调值（网点面积）
 * @Param paper_density 纸张密度
 * @Param solid_density 实地密度
 * @Param tint_density 当前测试的样品密度
 * @return 色调值
 * 
 * @Note 密度颜要依实地密度颜色
 */
float density_calculate_tone_value_by_murray_davies(float paper_density, float solid_density, float tint_density);

/**
 * 使用尤尔尼尔森公式方式计算色调值（网点面积）
 * @Param paper_density 纸张密度
 * @Param solid_density 实地密度
 * @Param tint_density 当前测试的样品密度
 * @Param n 系数（当系数为1时等于默里·戴维斯公式），不能小于或等于0
 * @return 色调值
 * 
 * @Note 密度颜要依实地密度颜色
 */
float density_calculate_tone_value_by_yule_nielsen(float paper_density, float solid_density, float tint_density,
                                                   float n);


/**
 * 通过SCTV（ISO 20654）方式计算色调值（网点面积）
 * @Param paper_xyz 纸张的XYZ值
 * @Param solid_xyz 实地色块的XYZ值
 * @Param tint_xyz 当前测量色块的XYZ值
 * @Param illuminant 使用的标准光源，通常是D50
 * @Param observer 使用的标准观察者角度，通常是2度
 * 
 * @Note 密度颜要依实地密度颜色
 */
float density_calculate_tone_value_by_sctv(float paper_xyz[], float solid_xyz[], float tint_xyz[],
                                           clralgo_StandardIlluminant illuminant, clralgo_StandardObserver observer);

/**
 * 通过SCTV（ISO 20654）方式计算色调值（网点面积），使用Lab
 * @Param paper_lab 纸张的XYZ值
 * @Param solid_lab 实地色块的XYZ值
 * @Param tint_lab 当前测量色块的XYZ值
 * @Param illuminant 使用的标准光源，通常是D50
 * @Param observer 使用的标准观察者角度，通常是2度
 * 
 * @Note 密度颜要依实地密度颜色
 */
float density_calculate_tone_value_by_sctv2(float paper_lab[], float solid_lab[], float tint_lab[],
	clralgo_StandardIlluminant illuminant, clralgo_StandardObserver observer);

/**
 * 计算色调值增加（网点增大面积）
 * @Param film_tone_value 胶片色调值
 * @Param printed_tone_value 打印的色调值
 */
inline float density_calculate_tone_value_increase(float film_tone_value, float printed_tone_value)
{
	const float dot_gain = printed_tone_value - film_tone_value;
	return dot_gain >= 0.0f ? dot_gain : 0.0f;
}

/**
 * 通过Preucil(GATF)叠印公式计算叠印
 * @Param paper 纸张密度
 * @Param d1 第一印油墨的实地密度（不需要减去纸张）
 * @Param d2 第二印油墨的实地密度（不需要减去纸张）
 * @Param dop 叠印油墨密度（不需要减去纸张）
 * @return 叠印率
 * 
 * @Note 密度颜要依dop的密度颜色
 */
float density_calculate_trapping_by_preucil(float paper, float d1, float d2, float dop);

/**
* 通过Brunner叠印公式计算叠印
* @Param paper 纸张密度
* @Param d1 第一印油墨的实地密度（不需要减去纸张）
* @Param d2 第二印油墨的实地密度（不需要减去纸张）
* @Param dop 叠印油墨密度（不需要减去纸张）
* @return 叠印率
* 
* @Note 密度颜要依dop的密度颜色
*/
float density_calculate_trapping_by_brunner(float paper, float d1, float d2, float dop);


/**
* 通过Ritz叠印公式计算叠印
* @Param paper 纸张密度
* @Param d1 第一印油墨的实地密度（不需要减去纸张）
* @Param d2 第二印油墨的实地密度（不需要减去纸张）
* @Param dop 叠印油墨密度（不需要减去纸张）
* @return 叠印率
* 
* @Note 密度颜要依dop的密度颜色
*/
float density_calculate_trapping_by_ritz(float paper, float d1, float d2, float dop);

/**
 * 计算印刷反差
 * @Param solid 实地密度
 * @Param tint 色调色块密度
 * @Return 印刷反差的结果 
 */
float density_calculate_contrast(float solid, float tint);


#ifdef __cplusplus
}
#endif

#endif
