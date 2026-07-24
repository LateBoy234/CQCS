#pragma once
#include "../DensityAlgo/DensityAlgo.h"
using namespace System;
using namespace System::Runtime::InteropServices;
using namespace ThreeNH::ColorAlgorithm;
using namespace ThreeNH::Optical;

namespace Tnh
{
	namespace Density
	{
		public enum class DensityColor
		{
			Auto = ::DENSITY_AUTO,
			C = ::DENSITY_C,
			M = ::DENSITY_M,
			Y = ::DENSITY_Y,
			K = ::DENSITY_K,
			Spot = ::DENSITY_SPOT,
			// 专色
		};

		public enum class DensityStatus
		{
			ISO_T = DENSITY_ISO_STATUS_T,
			ISO_E = DENSITY_ISO_STATUS_E,
			ISO_I = DENSITY_ISO_STATUS_I,
			ISO_A = DENSITY_ISO_STATUS_A
		};

		public ref class Formula
		{
		public:
			/**
			 * 计算密度值
			 * @param spectrum 光谱数据
			 * @param status 要计算的密度类型
			 * @return 反回相应的密度
			 */
			static double CalculateDensity(Spectrum^ spectrum,
			                               DensityStatus status, DensityColor color);

			/**
			 * 计算密度值
			 * @param spectrum 光谱数据
			 * @param status 密度状态
			 * @param color 密度颜色，如果为Auto，在retColor中返回自动选择的颜色；Color不能为Spot
			 * @param retColor 存储返回的密度的颜色(C,M,Y,K之一)
			 * @return 反回相应的密度
			 */
			static double CalculateDensity(Spectrum^ spectrum,
			                               DensityStatus status, DensityColor color,
			                               [Out] DensityColor% retColor);

			/**
			 * 计算密度值
			 * @param spectrum 光谱数据
			 * @param status 要计算的密度类型
			 * @param color 密度颜色，如果为Auto，在retColor中返回自动选择的颜色；Color不能为Spot
			 * @param retColor 存储返回的密度的颜色(C,M,Y,K之一)
			 * @param cmyk 返回C、M、Y、K的密度
			 * @return 反回选定的密度
			 */
			static double CalculateDensity(Spectrum^ spectrum,
			                               DensityStatus status, DensityColor color,
			                               [Out] DensityColor% retColor, [Out] array<double>^% cmyk);

			/**
			 *计算密度值
			 * @param spectrum 光谱数据
			 * @param status 要计算的密度类型
			 * @param color 密度颜色，如果为Auto，在retColor中返回自动选择的颜色；Color不能为Spot
			 * @param retColor 存储返回的密度的颜色(C,M,Y,K之一)
			 * @param cmyk 返回C、M、Y、K的密度
			 * @param grayness 返回灰度
			 * @param hue_error 返回色调差
			 */
			static double CalculateDensity(Spectrum^ spectrum,
			                               DensityStatus status, DensityColor color,
			                               [Out] DensityColor% retColor, [Out] array<double>^% cmyk,
			                               [Out] double% grayness, [Out] double% hueError);
			/**
			 * 计算CMYK密度
			 * @return 数组分别返回CMYK的值
			 */
			static array<double>^ CalculateCmyk(Spectrum^ spectrum, DensityStatus status);
			/**
			 * 计算色调偏差
			 */
			static double CalculateHueError(array<double>^ cmy);

			/**
			 * 计算灰度值
			 */
			static double CalculateGrayness(array<double>^ cmy);

			/**
			 * 计算专色密度
			 * @param spectrum 光谱数据
			 * @param wavelength 返回专色密度的所在波长
			 */
			static double CalculateSpotDensity(Spectrum^ spectrum, [Out] int% wavelength);

			/**
			 * 计算指定波长位置的专色密度
			 */
			static double CalculateSpecificSpotDensity(Spectrum^ spectrum, int wavelength);

			/**
			 * 使用默里·戴维斯公式计算网点面积
			 * @param paperDensity 纸张密度
			 * @param solidDensity 实地色块密度
			 * @param tintDensity 浅色样品的密度
			 * @return 网点面积
			 */
			static double
			CalculateToneValueByMurrayDavies(double paperDensity, double solidDensity, double tintDensity);

			/**
			 * 使用尤尔–尼尔森公式计算网点面积
			 * @param paperDensity 纸张密度
			 * @param solidDensity 实地色块密度
			 * @param tintDensity 浅色样品的密度
			 * @param n 系数，必须大于0
			 * @return 网点面积
			 */
			static double CalculateToneValueByYuleNielsen(double paperDensity, double solidDensity, double tintDensity,
			                                              double n);

			/**
			 * 通过SCTV（ISO 20654）方式计算网点面积
			 * @param paper 纸张的XYZ值
			 * @param solid 实地色块的XYZ值
			 * @param tint 当前测量色块的XYZ值
			 * @param illuminant XYZ值使用的标准光源，通常是D50
			 * @param observer XYZ值使用的标准观察者角度，通常是2度
			 */
			static double CalculateToneValueBySctv(CIEXYZ^ paper, CIEXYZ^ solid, CIEXYZ^ tint,
			                                       StandardIlluminant illuminant, StandardObserver observer);

			/**
			 * 通过SCTV（ISO 20654）方式计算网点面积
			 * @param paper 纸张的反射率
			 * @param solid 实地色块的反射率
			 * @param tint 当前测量色块的反射率
			 * @param illuminant 使用的标准光源，通常是D50
			 * @param observer 使用的标准观察者角度，通常是2度
			 */
			static double CalculateToneValueBySctv(Spectrum^ paper, Spectrum^ solid, Spectrum^ tint,
			                                       StandardIlluminant illuminant, StandardObserver observer);

			/**
			 * 计算网点增大
			 * @param filmToneValue 胶片的网点面积（即目标百分比）
			 * @param printedToneValue 打印的网点面积（实际计算的百分比）
			 * @return 增大面积
			 */
			static double CalculateToneValueIncrease(double filmToneValue, double printedToneValue);

			/**
			 * 自动匹配胶版的网点面积
			 * @Param toneValue 打印的网点面积
			 * @Param filmToneValues 胶片的网点面积
			 * @Return 返回相应的胶片的网点面积；返回100表示匹配实地密度
			 */
			static int MatchFilmToneValues(array<int>^ filmToneValues, double toneValue);

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
			static double CalculateTrappingByPreucil(double paper, double d1, double d2, double dop);

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
			static double CalculateTrappingByBrunner(double paper, double d1, double d2, double dop);


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
			static double CalculateTrappingByRitz(double paper, double d1, double d2, double dop);

			/**
			 * 计算印刷反差
			 * @Param solid 实地密度
			 * @Param tint 色调色块密度
			 * @Return 印刷反差的结果 
			 */
			static double CalculateContrast(double solid, double tint);
		};
	}
}
