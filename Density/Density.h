#pragma once
#include "../DensityAlgo/DensityAlgo.h"

using namespace System;
using namespace System::Runtime::InteropServices;
using namespace Tnh::Color;

namespace Tnh
{
	namespace Density
	{
		public enum class DensityType
		{
			Auto = DENSITY_AUTO,
			C = DENSITY_C,
			M = DENSITY_M,
			Y = DENSITY_Y,
			K = DENSITY_K,

			// ISO状态密度类型
			ISO_STATUS_T = DENSITY_ISO_STATUS_T,
			ISO_STATUS_E = DENSITY_ISO_STATUS_E,
			ISO_STATUS_I = DENSITY_ISO_STATUS_I,
			ISO_STATUS_A = DENSITY_ISO_STATUS_A,

			// T密度与密度项的组合
			ISO_STATUS_T_C = DENSITY_ISO_STATUS_T_C,
			ISO_STATUS_T_M = DENSITY_ISO_STATUS_T_M,
			ISO_STATUS_T_Y = DENSITY_ISO_STATUS_T_Y,

			// E密度与密度项的组合
			ISO_STATUS_E_C = DENSITY_ISO_STATUS_E_C,
			ISO_STATUS_E_M = DENSITY_ISO_STATUS_E_M,
			ISO_STATUS_E_Y = DENSITY_ISO_STATUS_E_Y,

			// I密度与密度项的组合
			ISO_STATUS_I_C = DENSITY_ISO_STATUS_I_C,
			ISO_STATUS_I_M = DENSITY_ISO_STATUS_I_M,
			ISO_STATUS_I_Y = DENSITY_ISO_STATUS_I_Y,

			// A密度与密度项的组合
			ISO_STATUS_A_C = DENSITY_ISO_STATUS_A_C,
			ISO_STATUS_A_M = DENSITY_ISO_STATUS_A_M,
			ISO_STATUS_A_Y = DENSITY_ISO_STATUS_A_Y,
		};

		public ref class Formula
		{
		public:
			/**
			 * 计算密度值
			 * @param spectrum 光谱数据
			 * @param densityType 要计算的密度类型
			 * @return 反回相应的密度
			 */
			static double CalculateDensity(ISpectralData^ spectrum, DensityType densityType);

			/**
			 * 计算密度值
			 * @param spectrum 光谱数据
			 * @param densityType 要计算的密度类型
			 * @param retDensityType 如果没有指定C、M、Y、K，返回自动密度类型(C,M,Y,K之一)，否则返回指定的密度类型
			 * @return 反回相应的密度
			 */
			static double CalculateDensity(ISpectralData^ spectrum, DensityType densityType,
			                              [Out] DensityType% retDensityType);

			/**
			 * 计算密度值
			 * @param spectrum 光谱数据
			 * @param densityType 要计算的密度类型
			 * @param retDensityType 如果没有指定C、M、Y、K，返回自动密度类型(C,M,Y,K之一)，否则返回指定的密度类型
			 * @param cmyk 返回C、M、Y、K的密度
			 * @return 反回选定的密度
			 */
			static double CalculateDensity(ISpectralData^ spectrum, DensityType densityType,
			                              [Out] DensityType% retDensityType, [Out] array<double>^% cmyk);

			/**
			 *计算密度值
			 * @param spectrum 光谱数据
			 * @param densityType 要计算的密度类型
			 * @param retDensityType 如果没有指定C、M、Y、K，返回自动密度类型(C,M,Y,K之一)，否则返回指定的密度类型
			 * @param cmyk 返回C、M、Y、K的密度
			 * @param grayness 返回灰度
			 * @param hue_error 返回色调差
			 */
			static double CalculateDensity(ISpectralData^ spectrum, DensityType densityType,
				[Out] DensityType% retDensityType, [Out] array<double>^% cmyk, 
				[Out] double% grayness, [Out] double% hueError);

		};
	}
}
