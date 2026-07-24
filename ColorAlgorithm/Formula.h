#pragma once

using namespace ThreeNH::Optical;

namespace ThreeNH
{
	namespace ColorAlgorithm
	{
		// 色牢度等级
		public enum class ColourFastnessLevel
		{
			INVALID,
			// 无效
			LEVEL_1,
			// 1
			LEVEL_1_2,
			// 1-2
			LEVEL_2,
			// 2
			LEVEL_2_3,
			// 2-3
			LEVEL_3,
			// 3
			LEVEL_3_4,
			// 3-4
			LEVEL_4,
			// 4
			LEVEL_4_5,
			// 4-5
			LEVEL_5,
			// 5
		};

		public ref class Formula
		{
		public:
			Formula();

			Formula(ThreeNH::Optical::StandardIlluminant illuminant, ThreeNH::Optical::StandardObserver observer);

#pragma region 属性

			property ThreeNH::Optical::StandardIlluminant Illuminant
			{
				ThreeNH::Optical::StandardIlluminant get(void)
				{
					return convert(illuminant_);
				}
				void set(ThreeNH::Optical::StandardIlluminant illuminant)
				{
					illuminant_ = convert(illuminant);
				}
			}

			property ThreeNH::Optical::StandardObserver Observer
			{
				ThreeNH::Optical::StandardObserver get(void)
				{
					return convert(observer_);
				}
				void set(ThreeNH::Optical::StandardObserver observer)
				{
					observer_ = convert(observer);
				}
			}

#pragma endregion


#pragma region 颜色空间转换相关函数

			CIEXYZ^ CalculateXyz(Spectrum^ spectrum);

			CIELAB^ CalculateLab(Spectrum^ spectrum);

			CIELAB^ XyzToLab(CIEXYZ^ XYZ);

			CIEXYZ^ LabToXyz(CIELAB^ XYZ);

			CIELUV^ XyzToLuv(CIEXYZ^ xyz);

			CIEXYZ^ LuvToXyz(CIELUV^ luv);

			static CIERGB^ XyzToCieRgb(CIEXYZ^ xyz);

			static CIEXYZ^ CieRgbToXyz(CIERGB^ rgb);

			HunterLab^ XyzToHunterLab(CIEXYZ^ xyz);

			CIEXYZ^ HunterLabToXyz(HunterLab^ lab);

			sRGB XyzToSrgb(CIEXYZ^ XYZ);

			static sRGB LabToSrgb(CIELAB^ lab);

			DIN99Lab^ LabToDin99(CIELAB^ lab);

			static CIEXYZ^ CalculateXyz(Spectrum^ spectral_data, StandardIlluminant illuminant, StandardObserver observer);

			static CIELAB^ CalculateLab(Spectrum^ spectral_data, StandardIlluminant illuminant, StandardObserver observer);

			static CIELAB^ XyzToLab(CIEXYZ^ xyz, StandardIlluminant illuminant, StandardObserver observer);

			static CIEXYZ^ LabToXyz(CIELAB^ lab, StandardIlluminant illuminant, StandardObserver observer);

			static CIELUV^ XyzToLuv(CIEXYZ^ xyz, StandardIlluminant illuminant, StandardObserver observer);

			static HunterLab^ XyzToHunterLab(CIEXYZ^ xyz, StandardIlluminant illuminant, StandardObserver observer);

			static DIN99Lab^ XyzToDin99Lab(CIELAB^ lab);

			static MunsellHVC^ XyzToMunsell(CIEXYZ^ xyz);

#pragma endregion


#pragma region 颜色指标计算函数

			double GetTAstmE313(CIEXYZ^ xyz);

			double GetWhitenessAstmE313(CIEXYZ^ xyz);

			double GetT_CIEISO(CIEXYZ^ xyz);

			double GetWhiteness_CIEISO(CIEXYZ^ xyz);

			double GetWhitenessTappl(CIEXYZ^ xyz);

			double GetWhitenessTaube(CIEXYZ^ xyz);

			double GetWhitnessHunter1942(HunterLab^ lab);

			double GetWhitnessHunter1964(HunterLab^ lab);

			double GetWhitnessR457(Spectrum^ spectrum);

			double GetWhitenessBerger(CIEXYZ^ xyz);

			double GetWhitenessGanz(CIEXYZ^ xyz);

			double GetTintGanzGriesser(CIEXYZ^ xyz);

			double GetWhitenessGost16873(CIELAB^ lab);

			double GetYellownessAstmD1925(CIEXYZ^ xyz);

			double GetYellownessAstmE313(CIEXYZ^ xyz);

			double GetMetamerismIndiex(Spectrum^ standard, // 标样的光谱
				Spectrum^ test, // 试样的光谱
				ThreeNH::Optical::StandardIlluminant testIlluminant, // 测试光源
				ThreeNH::Optical::StandardObserver testObserver);

			double GetMetamerismIndiex(Spectrum^ standard, Spectrum^ test, Formula^ referenceFormula);

			double GetMetamerismIndiex(CIEXYZ^ r1, CIEXYZ^ t1, CIEXYZ^ r2, CIEXYZ^ t2, Formula^ formula2);

			double GetStainingFastness_dEgs(CIELAB^ standard, CIELAB^ test);

			double GetStainingFastness_SSR(CIELAB^ standard, CIELAB^ test);

			ColourFastnessLevel GetReportedSSR(CIELAB^ standard, CIELAB^ test);

			double GetColorFastness_GSR(CIELAB^ standard, CIELAB^ test);

			ColourFastnessLevel GetReportedGSR(CIELAB^ standard, CIELAB^ test);

			double GetColorFastness_dEf(CIELAB^ standard, CIELAB^ test);

			double GetColorValueSum(Spectrum^ reflectances);

			double GetColorValueSWL(Spectrum^ reflectances, int% wavelengthOfMinimum, bool isTransmittance);

			static double GetColorValueSWLAt(Spectrum^ reflectances, int wavelength, bool isTransmittance);

			double GetWeightedStrength(Spectrum^ standard, Spectrum^ test, bool isTransmittance);
			double GetAverageStrength(Spectrum^ standard, Spectrum^ test, bool isTransmittance);

			double GetPrecentStrengthSWL(Spectrum^ standard, Spectrum^ test, int% wavelengthOfMinimum);

			array<double>^ GetPrecentStrength(Spectrum^ standard, Spectrum^ test);

			double GetOpacity(CIEXYZ^ whiteBaseSample, CIEXYZ^ blackBaseSample);

			static int Get555Shade(CIELAB^ standard, CIELAB^ test,
				double tolL, double tolA, double tolB);

			/**
			 * 计算Gardner指数
			 * @Param xyz: C/2条件下的透射XYZ值
			 * @Return：成功返回Gardner指数，返回负数表示无效
			 */
			double CalculateGardnerIndex(CIEXYZ^ xyz);
			/**
			 * 获取Pt-CO指数(钴铂指数）
			 * @Param spectraData: 光谱数据
			 * @Return: 返回正数表示有效，NaN表示无效
			 */
			static double CalculatePtCoIndex(Spectrum^ spectralData, int Pathlength);

			static	float clralgo_calculate_pt_co_index(const struct clralgo_CIEXYZ* xyz, int Pathlength);

			// /**
			//  * 根据ASTM D1003标准计算雾度
			//  * @Param T1: 仪器入射光（反射位置垫白板，透射样品位置空）
			//  * @Param T2: 总穿透光（反射位置垫白板，透射位置放样品）
			//  * @Param T3: 仪器散射光（反射位置放光阱，透射位置空）
			//  * @Param T4：样品和仪器的总散光（反射位置放光阱，透射位置放样品）
			//  * @Param haze: 输出样品雾度
			//  * @Param Tt: 样品穿透率
			//  * @Param Td：样品散透率
			//  * @Return：如果数据有效返回真，否则返回假
			//  * 注意：输入和输出都是百分比。
			//  */
			//static bool CalculateHaze(double T1, double T2, double T3, double T4,
				//double %haze, double %Tt, double %Td);

			/**
			 * 根据ASTM D1003标准计算雾度
			 * @Param T1: 仪器入射光（反射位置垫白板，透射样品位置空）
			 * @Param T2: 总穿透光（反射位置垫白板，透射位置放样品）
			 * @Param T3: 仪器散射光（反射位置放光阱，透射位置空）
			 * @Param T4：样品和仪器的总散光（反射位置放光阱，透射位置放样品）
			 * @Return: 如果数据有效返回雾度值，否则返回NaN
			 */
			static double CalculateHaze(double T1, double T2, double T3, double T4);

			/// <summary>
			/// 计算塞伯特指数
			/// </summary>
			/// <param name="xyz">C/2条件下的透射XYZ值</param>
			/// <returns>塞伯特指数</returns>
			double CalculateSayblot(CIEXYZ^ xyz);

			/// <summary>
			/// 计算糖度指数
			/// </summary>
			/// <param name="spectralData">光谱数据</param>
			/// <returns></returns>
			static double CalculateICUMSA(Spectrum^ spectralData);

			/// <summary>
			/// 计算糖度指数
			/// </summary>
			/// <param name="spectralData">光谱数据</param>
			/// <returns></returns>
			static double clralgo_calculate_ICUMSA(Spectrum^ spectralData, double icumsa_b, double icumsa_c);
			/// <summary>
			/// Astm d 1500
			/// </summary>
			/// <param name="xyz">C/2条件下的透射XYZ值</param>
			/// <returns></returns>
			double CalculateAdtmD1500(CIEXYZ^ xyz);

			float CalculateColorScale(int hue, Spectrum^ spectrum);

			/// <summary>
			/// 美国药典颜色标度计算
			/// </summary>
			/// <param name="hue"></param>
			/// <param name="spectrum"></param>
			/// <returns></returns>
			int CalculateUSAColorScale(Spectrum^ spectrum);

			/// <summary>
			/// 欧洲药典颜色标度计算
			/// </summary>
			/// <param name="hue"></param>
			/// <param name="spectrum"></param>
			/// <returns></returns>
			float CalculateEUColorScale(int hue, Spectrum^ spectrum);

			/// <summary>
			/// 计算klett值
			/// </summary>
			float Calculateklett(CIEXYZ^ xyz);
			/// <summary>
			/// 计算8度光泽度
			/// </summary>
			/// <param name="ssci">样品反射率SCI</param>
			/// <param name="ssce">样品反射率SCE</param>
			/// <param name="wsci">白板反射率SCI</param>
			/// <param name="wsce">白板反射率SCE</param>
			/// <returns></returns>
			static unsigned short CalculateEightDegreesGloss(Spectrum^ ssci, Spectrum^ ssce, Spectrum^ wsci, Spectrum^ wsce);

			static Spectrum^ CalculateSpectrum(Spectrum^ source);

			static Spectrum^ Interpolate(Spectrum^ source, const int begin, const int end, const int interval);

			CIEXYZ^ GetWhitePoint();
			static float CalculateColorValueSWL(const struct clralgo_Spectra* spectra, int isTransmittance, int* wavelength_of_maximum_absorption);

#pragma endregion


		private:
			static clralgo_StandardIlluminant convert(ThreeNH::Optical::StandardIlluminant illuminant)
			{
				//return static_cast<clralgo_StandardIlluminant>(illuminant);
				switch (illuminant)
				{
				case Optical::StandardIlluminant::D65: return clralgo_D65;
				case Optical::StandardIlluminant::D50: return clralgo_D50;
				case Optical::StandardIlluminant::A: return clralgo_A;
				case Optical::StandardIlluminant::C: return clralgo_C;
				case Optical::StandardIlluminant::D55: return clralgo_D55;
				case Optical::StandardIlluminant::D75: return clralgo_D75;
				case Optical::StandardIlluminant::F1: return clralgo_F1;
				case Optical::StandardIlluminant::F2: return clralgo_F2;
				case Optical::StandardIlluminant::F3: return clralgo_F3;
				case Optical::StandardIlluminant::F4: return clralgo_F4;
				case Optical::StandardIlluminant::F5: return clralgo_F5;
				case Optical::StandardIlluminant::F6: return clralgo_F6;
				case Optical::StandardIlluminant::F7: return clralgo_F7;
				case Optical::StandardIlluminant::F8: return clralgo_F8;
				case Optical::StandardIlluminant::F9: return clralgo_F9;
				case Optical::StandardIlluminant::F10: return clralgo_F10;
				case Optical::StandardIlluminant::F11: return clralgo_F11;
				case Optical::StandardIlluminant::F12: return clralgo_F12;
				case Optical::StandardIlluminant::CWF: return clralgo_CWF;
				case Optical::StandardIlluminant::DLF: return clralgo_DLF;
				case Optical::StandardIlluminant::TL83: return clralgo_TL83;
				case Optical::StandardIlluminant::TL84: return clralgo_TL84;
				case Optical::StandardIlluminant::TPL5: return clralgo_TPL5;
				case Optical::StandardIlluminant::U30: return clralgo_U30;
				case Optical::StandardIlluminant::B: return clralgo_B;
				case Optical::StandardIlluminant::U35: return clralgo_U35;
				case Optical::StandardIlluminant::NBF: return clralgo_NBF;
				case Optical::StandardIlluminant::ID50: return clralgo_ID50;
				case Optical::StandardIlluminant::ID65: return clralgo_ID65;
				case Optical::StandardIlluminant::LED_B1: return clralgo_LED_B1;
				case Optical::StandardIlluminant::LED_B2: return clralgo_LED_B2;
				case Optical::StandardIlluminant::LED_B3: return clralgo_LED_B3;
				case Optical::StandardIlluminant::LED_B4: return clralgo_LED_B4;
				case Optical::StandardIlluminant::LED_B5: return clralgo_LED_B5;
				case Optical::StandardIlluminant::LED_BH1: return clralgo_LED_BH1;
				case Optical::StandardIlluminant::LED_RGB1: return clralgo_LED_RGB1;
				case Optical::StandardIlluminant::LED_V1: return clralgo_LED_V1;
				case Optical::StandardIlluminant::LED_V2: return clralgo_LED_V2;
				case Optical::StandardIlluminant::LED_C2: return clralgo_LED_C2;
				case Optical::StandardIlluminant::LED_C3: return clralgo_LED_C3;
				case Optical::StandardIlluminant::LED_C5: return clralgo_LED_C5;
				case Optical::StandardIlluminant::LEDT8G: return clralgo_LEDT8G;
				case Optical::StandardIlluminant::U3500: return clralgo_U3500;
				//case Optical::StandardIlluminant::H: return clralgo_H;
				case Optical::StandardIlluminant::Unknown:
				default:
					return clralgo_STANDARD_ILLUMINANT_COUNT;
				}
			}

			static  ThreeNH::Optical::StandardIlluminant convert(clralgo_StandardIlluminant illuminant)
			{
				switch (illuminant)
				{
				case clralgo_D65: return Optical::StandardIlluminant::D65;
				case clralgo_D50: return Optical::StandardIlluminant::D50;
				case clralgo_A: return Optical::StandardIlluminant::A;
				case clralgo_C: return Optical::StandardIlluminant::C;
				case clralgo_D55: return Optical::StandardIlluminant::D55;
				case clralgo_D75: return Optical::StandardIlluminant::D75;
				case clralgo_F1: return Optical::StandardIlluminant::F1;
				case clralgo_F2: return Optical::StandardIlluminant::F2;
				case clralgo_F3: return Optical::StandardIlluminant::F3;
				case clralgo_F4: return Optical::StandardIlluminant::F4;
				case clralgo_F5: return Optical::StandardIlluminant::F5;
				case clralgo_F6: return Optical::StandardIlluminant::F6;
				case clralgo_F7: return Optical::StandardIlluminant::F7;
				case clralgo_F8: return Optical::StandardIlluminant::F8;
				case clralgo_F9: return Optical::StandardIlluminant::F9;
				case clralgo_F10: return Optical::StandardIlluminant::F10;
				case clralgo_F11: return Optical::StandardIlluminant::F11;
				case clralgo_F12: return Optical::StandardIlluminant::F12;
				case clralgo_CWF:return Optical::StandardIlluminant::CWF;
				case clralgo_DLF:return Optical::StandardIlluminant::DLF;
				case clralgo_TL83:return Optical::StandardIlluminant::TL83;
				case clralgo_TL84:return Optical::StandardIlluminant::TL84;
				case clralgo_TPL5:return Optical::StandardIlluminant::TPL5;
				case clralgo_U30:return Optical::StandardIlluminant::U30;
				case clralgo_B:return Optical::StandardIlluminant::B;
				case clralgo_U35:return Optical::StandardIlluminant::U35;
				case clralgo_NBF:return Optical::StandardIlluminant::NBF;
				case clralgo_ID50:return Optical::StandardIlluminant::ID50;
				case clralgo_ID65:return Optical::StandardIlluminant::ID65;
				case clralgo_LED_B1:return Optical::StandardIlluminant::LED_B1;
				case clralgo_LED_B2:return Optical::StandardIlluminant::LED_B2;
				case clralgo_LED_B3:return Optical::StandardIlluminant::LED_B3;
				case clralgo_LED_B4:return Optical::StandardIlluminant::LED_B4;
				case clralgo_LED_B5:return Optical::StandardIlluminant::LED_B5;
				case clralgo_LED_BH1:return Optical::StandardIlluminant::LED_BH1;
				case clralgo_LED_RGB1:return Optical::StandardIlluminant::LED_RGB1;
				case clralgo_LED_V1:return Optical::StandardIlluminant::LED_V1;
				case clralgo_LED_V2:return Optical::StandardIlluminant::LED_V2;
				case clralgo_LED_C2:return Optical::StandardIlluminant::LED_C2;
				case clralgo_LED_C3:return Optical::StandardIlluminant::LED_C3;
				case clralgo_LED_C5:return Optical::StandardIlluminant::LED_C5;
				case clralgo_LEDT8G: return Optical::StandardIlluminant::LEDT8G;
				case clralgo_U3500: return Optical::StandardIlluminant::U3500;
				//case clralgo_H: return Optical::StandardIlluminant::H;
				case clralgo_STANDARD_ILLUMINANT_COUNT:
				default: return Optical::StandardIlluminant::Unknown;
				}
			}

			static  clralgo_StandardObserver convert(ThreeNH::Optical::StandardObserver observer)
			{
				return static_cast<clralgo_StandardObserver>(static_cast<int>(observer));
			}

			static ThreeNH::Optical::StandardObserver convert(clralgo_StandardObserver observer)
			{
				return static_cast<ThreeNH::Optical::StandardObserver>(static_cast<int>(observer));
			}

			clralgo_StandardIlluminant illuminant_;
			clralgo_StandardObserver observer_;
		};
	}
}
