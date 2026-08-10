#include "Stdafx.h"
#include "clralgo.h"
#include "ph_cp_std_table.h"
#include "ph_eu_std_table.h"
#include "ph_usa_std_table.h"

#include "ColorSpaces.h"
#include "Spectrum.h"
#include "Formula.h"
#include <limits>
#include <tristimulus_weighting_factors.h>

#define FLOAT_NAN      (std::numeric_limits<float>::quiet_NaN())
#define DOUBLE_NAN     (std::numeric_limits<double>::quiet_NaN())

using namespace ThreeNH::Optical;

namespace ThreeNH {
	namespace ColorAlgorithm
	{
		namespace
		{
			clralgo_Spectra* ToSpectra(Spectrum^ spectrum)
			{
				clralgo_Spectra* spectra = reinterpret_cast<clralgo_Spectra*>(
					malloc(sizeof(clralgo_Spectra)));
				spectra->range.start = spectrum->Range.Min;
				spectra->range.interval = spectrum->Range.Step;
				spectra->range.count = spectrum->Range.Count;
				spectra->data = reinterpret_cast<float*>(
					malloc(sizeof(float) * spectra->range.count));
				for (size_t i = 0; i < spectra->range.count; i++)
				{
					spectra->data[i] = static_cast<float>(spectrum->Data[i]);
				}

				return spectra;
			}

			void FreeSpectra(clralgo_Spectra* spectra)
			{
				if (spectra == NULL)
					return;

				if (spectra->data)
				{
					free(spectra->data);
					spectra->data = NULL;
				}
				free(spectra);
			}

			class SpectralAgent
			{
			public:
				explicit SpectralAgent(Spectrum^ spectrum)
				{
					spectral_data_.range.start = spectrum->Range.Min;
					spectral_data_.range.interval = spectrum->Range.Step;
					spectral_data_.range.count = spectrum->Range.Count;
					spectral_data_.data = new float[spectral_data_.range.count];
					for (int i = 0; i < spectral_data_.range.count; ++i)
					{
						int wavelength = spectral_data_.range.start + i * spectral_data_.range.interval;
						spectral_data_.data[i] = static_cast<float>(spectrum[wavelength]);
					}
				}
				~SpectralAgent()
				{
					delete[] spectral_data_.data;
					spectral_data_.data = nullptr;
				}

				operator clralgo_Spectra* () { return &spectral_data_; }
				operator const clralgo_Spectra* () const { return &spectral_data_; }

				int start() const { return spectral_data_.range.start; }
				int interval() const { return spectral_data_.range.interval; }
				int count() const { return spectral_data_.range.count; }
				float* data() { return spectral_data_.data; }
				const float* data() const { return spectral_data_.data; }
			private:
				clralgo_Spectra spectral_data_;
			};
		}

		Formula::Formula()
			: illuminant_(convert(ThreeNH::Optical::StandardIlluminant::D65))
			, observer_(convert(ThreeNH::Optical::StandardObserver::CIE1964))
		{
		}

		Formula::Formula(StandardIlluminant illuminant, StandardObserver observer)
			: illuminant_(convert(illuminant)), observer_(convert(observer))
		{

		}

		CIEXYZ^ Formula::CalculateXyz(Spectrum^ spectrum)
		{
			clralgo_Spectra* spectral = ToSpectra(spectrum);
			clralgo_CIEXYZ xyz;

			clralgo_calculate_xyz(spectral, illuminant_, observer_, &xyz);

			FreeSpectra(spectral);

			return gcnew CIEXYZ(xyz);
		}

		CIELAB^ Formula::CalculateLab(Spectrum^ spectrum)
		{
			return XyzToLab(CalculateXyz(spectrum));
		}

		CIELAB^ Formula::XyzToLab(CIEXYZ^ XYZ)
		{
			clralgo_CIEXYZ _xyz = *XYZ;
			clralgo_CIELab _lab;

			clralgo_xyz_to_lab(illuminant_, observer_, &_xyz, &_lab);

			return gcnew CIELAB(_lab);
		}

		CIEXYZ^ Formula::LabToXyz(CIELAB^ lab)
		{
			clralgo_CIEXYZ _xyz;
			clralgo_CIELab _lab = *lab;

			clralgo_lab_to_xyz(illuminant_, observer_, &_lab, &_xyz);

			return gcnew CIEXYZ(_xyz);
		}

		CIELUV^ Formula::XyzToLuv(CIEXYZ^ xyz)
		{
			clralgo_CIEXYZ _xyz = *xyz;
			clralgo_CIELuv _luv;

			clralgo_xyz_to_luv(illuminant_, observer_, &_xyz, &_luv);

			return gcnew CIELUV(_luv);
		}

		CIEXYZ^ Formula::LuvToXyz(CIELUV^ luv)
		{
			clralgo_CIEXYZ _xyz;
			//clralgo_CIELuv _luv = *luv;

			//clralgo_luv_to_xyz(illuminant_, observer_, &_luv, &_xyz);

			return gcnew CIEXYZ(_xyz);

		}

		CIERGB^ Formula::XyzToCieRgb(CIEXYZ^ xyz)
		{
			clralgo_CIEXYZ _xyz = *xyz;
			clralgo_CIERGB _rgb;

			clralgo_xyz_to_ciergb(&_xyz, &_rgb);

			return gcnew CIERGB(_rgb);
		}

		CIEXYZ^ Formula::CieRgbToXyz(CIERGB^ rgb)
		{
			clralgo_CIEXYZ _xyz;
			clralgo_CIERGB _rgb = *rgb;

			clralgo_ciergb_to_xyz(&_rgb, &_xyz);

			return gcnew CIEXYZ(_xyz);
		}

		HunterLab^ Formula::XyzToHunterLab(CIEXYZ^ xyz)
		{
			clralgo_CIEXYZ _xyz = *xyz;
			clralgo_HunterLab _lab;

			clralgo_xyz_to_hunterlab(illuminant_, observer_, &_xyz, &_lab);

			return gcnew HunterLab(_lab);
		}

		CIEXYZ^ Formula::HunterLabToXyz(HunterLab^ lab)
		{
			clralgo_CIEXYZ _xyz;
			clralgo_HunterLab _lab = *lab;

			clralgo_hunterlab_to_xyz(illuminant_, observer_, &_lab, &_xyz);

			return gcnew CIEXYZ(_xyz);
		}

		sRGB Formula::XyzToSrgb(CIEXYZ^ XYZ)
		{
			clralgo_CIEXYZ _xyz = *XYZ;
			clralgo_sRGB _rgb;

			clralgo_xyz_to_srgb(illuminant_, observer_, &_xyz, &_rgb);

			return sRGB(_rgb);
		}

		sRGB Formula::LabToSrgb(CIELAB^ lab)
		{
			clralgo_CIELab _lab = *lab;
			clralgo_sRGB _rgb;

			clralgo_lab_to_srgb(&_lab, &_rgb);

			return sRGB(_rgb);
		}

		DIN99Lab^ Formula::LabToDin99(CIELAB^ lab)
		{
			clralgo_CIELab _lab = *lab;
			clralgo_DIN99Lab _din99;

			clralgo_lab_to_din99(&_lab, &_din99);

			return gcnew DIN99Lab(_din99);
		}

		CIEXYZ^ Formula::CalculateXyz(Spectrum^ spectral_data, StandardIlluminant illuminant,
			StandardObserver observer)
		{
			clralgo_CIEXYZ xyz;
			clralgo_calculate_xyz(SpectralAgent(spectral_data), convert(illuminant), convert(observer), &xyz);
			return gcnew  CIEXYZ(xyz);
		}

		CIELAB^ Formula::CalculateLab(Spectrum^ spectral_data, StandardIlluminant illuminant,
			StandardObserver observer)
		{
			clralgo_CIELab lab;
			clralgo_calculate_lab(SpectralAgent(spectral_data), convert(illuminant), convert(observer), &lab);
			return gcnew CIELAB(lab);
		}

		CIELAB^ Formula::XyzToLab(CIEXYZ^ xyz, StandardIlluminant illuminant, StandardObserver observer)
		{
			clralgo_CIEXYZ _xyz = *xyz;
			clralgo_CIELab _lab;

			clralgo_xyz_to_lab(convert(illuminant), convert(observer), &_xyz, &_lab);

			return gcnew CIELAB(_lab);
		}

		CIEXYZ^ Formula::LabToXyz(CIELAB^ lab, StandardIlluminant illuminant, StandardObserver observer)
		{
			clralgo_CIEXYZ _xyz;
			clralgo_CIELab _lab = *lab;

			clralgo_lab_to_xyz(convert(illuminant), convert(observer), &_lab, &_xyz);

			return gcnew CIEXYZ(_xyz);
		}

		CIELUV^ Formula::XyzToLuv(CIEXYZ^ xyz, StandardIlluminant illuminant, StandardObserver observer)
		{
			clralgo_CIEXYZ _xyz = *xyz;
			clralgo_CIELuv _luv;

			clralgo_xyz_to_luv(convert(illuminant), convert(observer), &_xyz, &_luv);

			return gcnew CIELUV(_luv);
		}

		HunterLab^ Formula::XyzToHunterLab(CIEXYZ^ xyz, StandardIlluminant illuminant, StandardObserver observer)
		{
			clralgo_CIEXYZ _xyz = *xyz;
			clralgo_HunterLab _lab;

			clralgo_xyz_to_hunterlab(convert(illuminant), convert(observer), &_xyz, &_lab);

			return gcnew HunterLab(_lab);
		}

		DIN99Lab^ Formula::XyzToDin99Lab(CIELAB^ lab)
		{
			clralgo_CIELab _lab = *lab;
			clralgo_DIN99Lab _lab99;

			clralgo_lab_to_din99(&_lab, &_lab99);

			return gcnew DIN99Lab(_lab99);
		}

		MunsellHVC^ Formula::XyzToMunsell(CIEXYZ^ xyz)
		{
			clralgo_CIEXYZ _xyz = *xyz;
			clralgo_Munsell munsell;

			clralgo_xyz_to_munsell(&_xyz, &munsell);

			return gcnew MunsellHVC(munsell);
		}

		double Formula::GetTAstmE313(CIEXYZ^ xyz)
		{
			clralgo_CIEYxy _yxy = *xyz;
			float T;
			clralgo_calculate_whiteness_astm_e313_2005(
				illuminant_, observer_, &_yxy, &T);

			return T;
		}

		double Formula::GetWhitenessAstmE313(CIEXYZ^ xyz)
		{
			clralgo_CIEYxy _yxy = *xyz;
			return clralgo_calculate_whiteness_astm_e313_2005(
				illuminant_, observer_, &_yxy, NULL);
		}

		double Formula::GetT_CIEISO(CIEXYZ^ xyz)
		{
			clralgo_CIEYxy _yxy = *xyz;
			float T;
			clralgo_calculate_whiteness_cie_iso(
				illuminant_, observer_, &_yxy, &T
			);
			return T;
		}

		double Formula::GetWhiteness_CIEISO(CIEXYZ^ xyz)
		{
			clralgo_CIEYxy _yxy = *xyz;
			return clralgo_calculate_whiteness_cie_iso(
				illuminant_, observer_, &_yxy, NULL
			);
		}

		double Formula::GetWhitenessTappl(CIEXYZ^ xyz)
		{
			clralgo_CIEXYZ _xyz = *xyz;
			return clralgo_calculate_whiteness_tappl(
				illuminant_, observer_, &_xyz);
		}

		double Formula::GetWhitenessTaube(CIEXYZ^ xyz)
		{
			clralgo_CIEXYZ _xyz = *xyz;
			return clralgo_calculate_whiteness_taube(
				illuminant_, observer_, &_xyz);
		}

		double Formula::GetWhitnessHunter1942(HunterLab^ lab)
		{
			clralgo_HunterLab _lab = *lab;
			return clralgo_calculate_whiteness_hunter1942(illuminant_, observer_, &_lab);
		}

		double Formula::GetWhitnessHunter1964(HunterLab^ lab)
		{
			clralgo_HunterLab _lab = *lab;
			return clralgo_calculate_whiteness_hunter1960(illuminant_, observer_, &_lab);
		}

		double Formula::GetWhitnessR457(Spectrum^ spectrum)
		{
			clralgo_Spectra* spectra = ToSpectra(spectrum);
			double wi = clralgo_calculate_whiteness_r457(illuminant_, spectra);
			FreeSpectra(spectra);
			return wi;
		}

		double Formula::GetWhitenessBerger(CIEXYZ^ xyz)
		{
			clralgo_CIEXYZ _xyz = *xyz;
			double wi = clralgo_calculate_whiteness_berger59(illuminant_, observer_, &_xyz);
			return wi;
		}


		double Formula::GetWhitenessGanz(CIEXYZ^ xyz)
		{
			if (illuminant_ == clralgo_D65 && observer_ == clralgo_DEGREE_10) {
				clralgo_CIEXYZ _xyz = *xyz;
				return clralgo_calculate_whiteness_ganz_and_grieser(&_xyz);
			}
			return NAN;
		}

		double Formula::GetTintGanzGriesser(CIEXYZ^ xyz)
		{
			if (illuminant_ == clralgo_D65 && observer_ == clralgo_DEGREE_2) {
				clralgo_CIEXYZ _xyz = *xyz;
				return clralgo_calculate_tint_ganz_and_grieser(&_xyz);
			}
			return NAN;
		}


		double Formula::GetWhitenessGost16873(CIELAB^ lab)
		{
			clralgo_CIELab _lab = *lab;
			return	clralgo_calculate_whiteness_gost_16873(&_lab);

		}

		double Formula::GetYellownessAstmD1925(CIEXYZ^ xyz)
		{
			clralgo_CIEXYZ _xyz = *xyz;
			return clralgo_calculate_yellowness_astm_d1925_1970(illuminant_, observer_, &_xyz);
		}

		double Formula::GetYellownessAstmE313(CIEXYZ^ xyz)
		{
			clralgo_CIEXYZ _xyz = *xyz;
			return clralgo_calculate_yellowness_astm_e313_2005(illuminant_, observer_, &_xyz);
		}

		double Formula::GetMetamerismIndiex(Spectrum^ standard,  // 标样的光谱
			Spectrum^ test,  // 试样的光谱
			StandardIlluminant testIlluminant,   // 测试光源
			StandardObserver testObserver)
		{
			clralgo_Spectra* s1 = ToSpectra(standard);
			clralgo_Spectra* s2 = ToSpectra(test);

			double mi = clralgo_calculate_metamerism_index(s1, s2, illuminant_, observer_,
				convert(testIlluminant), convert(testObserver));

			FreeSpectra(s1);
			FreeSpectra(s2);

			return mi;
		}

		double Formula::GetMetamerismIndiex(Spectrum^ standard, Spectrum^ test, Formula^ referenceFormula)
		{
			clralgo_Spectra* s1 = ToSpectra(standard);
			clralgo_Spectra* s2 = ToSpectra(test);

			double mi = clralgo_calculate_metamerism_index(s1, s2, illuminant_, observer_,
				referenceFormula->illuminant_, referenceFormula->observer_);

			FreeSpectra(s1);
			FreeSpectra(s2);

			return mi;
		}

		double Formula::GetMetamerismIndiex(CIEXYZ^ r1, CIEXYZ^ t1, CIEXYZ^ r2, CIEXYZ^ t2, Formula^ formula2)
		{
			clralgo_CIEXYZ _r1 = *r1;
			clralgo_CIEXYZ _t1 = *t1;
			clralgo_CIEXYZ _r2 = *r2;
			clralgo_CIEXYZ _t2 = *t2;

			return clralgo_calculate_metamerism_index_by_xyz(illuminant_, observer_, &_r1, &_t1,
				formula2->illuminant_, formula2->observer_, &_r2, &_t2);
		}

		double Formula::GetStainingFastness_dEgs(CIELAB^ standard, CIELAB^ test)
		{
			float dEgs;
			clralgo_CIELab _test = *test;
			clralgo_CIELab _standard = *standard;
			if (clralgo_calculate_ssr(illuminant_, observer_, &_test, &_standard, NULL, &dEgs)
				!= clralgo_COLOR_FASTNESS_UNDEFINED)
			{
				return dEgs;
			}
			return DOUBLE_NAN;
		}

		double Formula::GetStainingFastness_SSR(CIELAB^ standard, CIELAB^ test)
		{
			float ssr;
			clralgo_CIELab _test = *test;
			clralgo_CIELab _standard = *standard;
			if (clralgo_calculate_ssr(illuminant_, observer_, &_test, &_standard, &ssr, NULL)
				!= clralgo_COLOR_FASTNESS_UNDEFINED)
			{
				return ssr;
			}
			return DOUBLE_NAN;
		}

		ColourFastnessLevel Formula::GetReportedSSR(CIELAB^ standard, CIELAB^ test)
		{
			clralgo_CIELab _test = *test;
			clralgo_CIELab _standard = *standard;

			auto grade = clralgo_calculate_ssr(illuminant_, observer_, &_test, &_standard, NULL, NULL);
			return static_cast<ColourFastnessLevel>(grade);
		}

		double Formula::GetColorFastness_GSR(CIELAB^ standard, CIELAB^ test)
		{
			float gsr;
			clralgo_CIELab _test = *test;
			clralgo_CIELab _standard = *standard;

			auto grade = clralgo_calculate_gsr(illuminant_, observer_, &_test, &_standard, &gsr, NULL);
			return grade == clralgo_COLOR_FASTNESS_UNDEFINED ? DOUBLE_NAN : gsr;
		}

		ColourFastnessLevel Formula::GetReportedGSR(CIELAB^ standard, CIELAB^ test)
		{
			clralgo_CIELab _test = *test;
			clralgo_CIELab _standard = *standard;

			auto grade = clralgo_calculate_gsr(illuminant_, observer_, &_test, &_standard, NULL, NULL);
			return static_cast<ColourFastnessLevel>(grade);
		}

		double Formula::GetColorFastness_dEf(CIELAB^ standard, CIELAB^ test)
		{
			float dEf;
			clralgo_CIELab _test = *test;
			clralgo_CIELab _standard = *standard;

			auto grade = clralgo_calculate_gsr(illuminant_, observer_, &_test, &_standard, NULL, &dEf);
			return grade == clralgo_COLOR_FASTNESS_UNDEFINED ? DOUBLE_NAN : dEf;
		}

		double Formula::GetColorValueSum(Spectrum^ reflectances)
		{
			clralgo_Spectra* s = ToSpectra(reflectances);
			float sum = clralgo_calculate_color_value_sum(s, false);
			FreeSpectra(s);
			return sum;
		}

		double Formula::GetColorValueSWL(Spectrum^ reflectances, int% wavelengthOfMinimum, bool isTransmittance)
		{
			int waveMin;
			clralgo_Spectra* s = ToSpectra(reflectances);
			int isTrans = isTransmittance == true ? 1 : 0;
			float swl = clralgo_calculate_color_value_swl(s, isTrans, &waveMin);
			FreeSpectra(s);
			wavelengthOfMinimum = waveMin;
			return swl;
		}

		double Formula::GetColorValueSWLAt(Spectrum^ reflectances, int wavelength, bool isTransmittance)
		{
			clralgo_Spectra* s = ToSpectra(reflectances);
			return clralgo_calculate_color_value_swl_at(s, wavelength, isTransmittance);
		}
		double Formula::GetWeightedStrength(Spectrum^ standard, Spectrum^ test, bool isTransmittance)
		{
			clralgo_Spectra* _standard = ToSpectra(standard);
			clralgo_Spectra* _test = ToSpectra(test);
			return clralgo_calculate_weighted_strength(illuminant_, observer_, _test, _standard, isTransmittance ? 1 : 0);
		}



		double Formula::GetAverageStrength(Spectrum^ standard, Spectrum^ test, bool isTransmittance)
		{
			clralgo_Spectra* _standard = ToSpectra(standard);
			clralgo_Spectra* _test = ToSpectra(test);
			int isTrans = isTransmittance == true ? 1 : 0;
			float strength = clralgo_calculate_average_strength(_test, _standard, isTrans);
			FreeSpectra(_standard);
			FreeSpectra(_test);
			return strength;
		}

		double Formula::GetPrecentStrengthSWL(Spectrum^ standard, Spectrum^ test, int% wavelengthOfMinimum)
		{
			clralgo_Spectra* _standard = ToSpectra(standard);
			clralgo_Spectra* _test = ToSpectra(test);
			int waveMin;
			float strength = clralgo_calculate_precent_strength_swl(_test, _standard, &waveMin);
			FreeSpectra(_standard);
			FreeSpectra(_test);
			wavelengthOfMinimum = waveMin;
			return strength;
		}
		float  Formula::CalculateColorValueSWL(const clralgo_Spectra* spectra, int isTransmittance, int* wavelength_of_maximum_absorption)
		{
			return  clralgo_calculate_color_value_swl(spectra, isTransmittance, wavelength_of_maximum_absorption);
		}
		array<double>^ Formula::GetPrecentStrength(Spectrum^ standard, Spectrum^ test)
		{
			clralgo_Spectra* _standard = ToSpectra(standard);
			clralgo_Spectra* _test = ToSpectra(test);

			float* strengths = new float[_standard->range.count];

			clralgo_calculate_precent_strength(_test, _standard, strengths);

			array<double>^ result = gcnew array<double>(_standard->range.count);
			for (int i = 0; i < _standard->range.count; i++)
			{
				result[i] = strengths[i];
			}
			delete[] strengths;

			FreeSpectra(_standard);
			FreeSpectra(_test);
			return result;
		}

		double Formula::GetOpacity(CIEXYZ^ whiteBaseSample, CIEXYZ^ blackBaseSample)
		{
			return clralgo_calculate_opacity(static_cast<float>(whiteBaseSample->Y),
				static_cast<float>(blackBaseSample->Y));
		}

		int Formula::Get555Shade(CIELAB^ standard, CIELAB^ test, double tolL, double tolA, double tolB)
		{
			clralgo_CIELab _std = *standard;
			clralgo_CIELab _test = *test;
			return clralgo_get_555_shade(&_test, &_std,
				static_cast<float>(tolL), static_cast<float>(tolA), static_cast<float>(tolB));
		}

		double Formula::CalculateGardnerIndex(CIEXYZ^ xyz)
		{
			if (Illuminant != StandardIlluminant::C || Observer != StandardObserver::CIE1931)
			{
				return -1.0;
			}
			clralgo_CIEXYZ _xyz = *xyz;
			return clralgo_calculate_gardner_index(&_xyz);
		}

		double Formula::CalculatePtCoIndex(Spectrum^ spectralData, int Pathlength)
		{
			clralgo_Spectra* _spectra = ToSpectra(spectralData);
			clralgo_CIEXYZ xyz;
			clralgo_calculate_xyz(_spectra, clralgo_C, clralgo_DEGREE_2, &xyz);
			pow(1, 2);

			double index = clralgo_calculate_pt_co_index(&xyz, Pathlength);
			FreeSpectra(_spectra);
			return index >= 0.0f ? index : DOUBLE_NAN;
		}


		// 计算透射铂钴指数
// 参数：
// [in] xyz:			要计算其白度指数的XYZ的值
		float Formula::clralgo_calculate_pt_co_index(const struct clralgo_CIEXYZ* xyz, int Pathlength)
		{
			/**
			 * 输入C2条件下的XYZ值，输出Pt_Co/APHA，正常为0~500(颜色越浅Pt_Co指数越小，颜色越深数值越大,最大到1000)；
			 * state说明：普通计算情况下state=1,不是太合理
			 */

			 // 黄度、Pt-Co指数映射表
			 //static const float Pt_CoIndex[128] = {
			 //	0, 0.5, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 25, 30, 35, 40, 45, 50, 55, 60, 65, 70, 75, 80, 
				 //	85, 90, 95, 100, 110, 120, 130, 140, 150, 160, 170, 180, 190, 200, 210, 220, 230, 240, 250, 260, 270, 280, 290, 300, 310, 320, 330, 340, 
				 //	350, 360, 370, 380, 390, 400, 410, 420, 430, 440, 450, 460, 470, 480, 490, 500, 510, 520, 530, 540, 550, 560, 570, 580, 590, 600, 610, 620,
				 //	630, 640, 650, 660, 670, 680, 690, 700, 710, 720, 730, 740, 750, 760, 770, 780, 790, 800, 810, 820, 830, 840, 850, 860, 870, 880, 890, 900, 910, 
				 //	920, 930, 940, 950, 960, 970, 980, 990, 1000};

			static const float Pt_CoIndex[128] = { 0,0.54585, 1.0917, 2.1834, 3.2751, 4.3668, 5.4585, 6.5502, 7.6419, 8.7336, 9.8253, 10.917, 12.0087, 13.1004, 14.1921, 15.2838,
				  16.3755, 17.4672, 18.5589, 19.6506, 20.7423, 21.834, 27.2925, 32.751, 38.2095, 43.668, 49.1265, 54.585, 60.0435, 65.502, 70.9605, 76.419, 81.8775, 87.336, 92.7945,
				  98.253, 103.7115, 109.17, 120.087, 131.004, 141.921, 152.838, 163.755, 174.672, 185.589, 196.506, 207.423, 218.34, 229.257, 240.174, 251.091, 262.008, 272.925,
				  283.842, 294.759, 305.676, 316.593, 327.51, 338.427, 349.344, 360.261, 371.178, 382.095, 393.012, 403.929, 414.846, 425.763, 436.68, 447.597, 458.514, 469.431,
				  480.348, 491.265, 502.182, 513.099, 524.016, 534.933, 545.85, 556.767, 567.684, 578.601, 589.518, 600.435, 611.352, 622.269, 633.186, 644.103, 655.02, 665.937,
				  676.854, 687.771, 698.688, 709.605, 720.522, 731.439, 742.356, 753.273, 764.19, 775.107, 786.024, 796.941, 807.858, 818.775, 829.692, 840.609, 851.526, 862.443,
				  873.36, 884.277, 895.194, 906.111, 917.028, 927.945, 938.862, 949.779, 960.696, 971.613, 982.53, 993.447, 1004.364, 1015.281, 1026.198, 1037.115, 1048.032, 1058.949,
				  1069.866, 1080.783, 1091.7 };

			static const double Pt_CoIndex_YI[128] = { 0.0014746,0.031722646,0.061963502,0.122423655,0.182855076,0.243257782,0.303631791,0.363977121,0.424293788,0.48458181,0.544841205,0.60507199,0.665274181,0.725447797,0.785592855,0.845709371,0.905797364,0.965856849,1.025887845,1.085890369,1.145864438,1.205810068,1.505112245,1.803706025,2.101593526,2.398776851,2.695258094,2.991039336,3.286122646,3.580510081,3.874203687,4.167205497,4.459517535,4.751141813,5.042080331,5.33233508,5.621908037,5.910801172,6.486555795,7.059614486,7.629992622,8.197705419,8.762767938,9.325195087,9.885001627,10.44220217,10.99681119,11.54884303,12.09831187,12.64523178,13.1896167,13.73148043,14.27083665,14.80769891,15.34208066,15.8739952,16.40345576,16.93047541,17.45506713,17.9772438,18.49701818,19.01440293,19.5294106,20.04205364,20.55234441,21.06029518,21.56591809,22.06922523,22.57022855,23.06893996,23.56537125,24.05953411,24.55144018,25.04110099,25.52852799,26.01373256,26.49672599,26.97751947,27.45612416,27.9325511,28.40681127,28.87891558,29.34887485,29.81669985,30.28240126,30.7459897,31.20747571,31.66686978,32.12418231,32.57942364,33.03260405,33.48373376,33.93282292,34.37988161,34.82491985,35.2679476,35.70897477,36.1480112,36.58506667,37.02015089,37.45327354,37.88444423,38.31367249,38.74096784,39.16633971,39.58979747,40.01135048,40.43100799,40.84877925,41.26467341,41.6786996,42.0908669,42.50118431,42.90966081,43.31630531,43.7211267,44.12413378,44.52533533,44.92474008,45.32235671,45.71819384,46.11226006,46.50456391,46.89511389,47.28391844,47.67098597,48.05632484,48.43994336 };

			static const int num_Pt_CoIndex = 128;

			/**
			 * 当样品YI数值小于0，无效
			 */
			float YI = clralgo_calculate_yellowness_astm_e313_2005(clralgo_C, clralgo_DEGREE_2, xyz);
			YI = clralgo_calculate_YI_Optical_path_conversion(Pathlength, YI);	//光程转换
			if (clralgo_IS_NAN(YI) || YI < -0.5)
			{
				//return 0;
			}

			/**
			* 判断样品是否接迎参考白
			*/
			clralgo_CIELab lab;
			clralgo_xyz_to_lab(clralgo_C, clralgo_DEGREE_2, xyz, &lab);
			clralgo_CIEXYZ white_point_xyz = clralgo_get_white_point_of_illuminant(clralgo_DEGREE_2, clralgo_C);
			clralgo_CIELab white_point_lab;
			clralgo_xyz_to_lab(clralgo_C, clralgo_DEGREE_2, &white_point_xyz, &white_point_lab);
			if (clralgo_calculate_dEab(&lab, &white_point_lab) < 0.2)
			{
				//return 0;
			}

			/**
			 * 根据YI查找Pt-Co
			 */
			int G_low = 0;
			int G_up = 0;
			for (int i = 0; i < num_Pt_CoIndex; i++) {
				if (YI < Pt_CoIndex_YI[i]) {
					G_up = i;
					break;
				}
				else if (YI == Pt_CoIndex_YI[i]) {
					G_low = i;
					G_up = i;
					break;
				}
				else {
					G_low = i;
				}
			}

			// 找到对应的，直接返回
			if (G_low == G_up) {
				return Pt_CoIndex[G_low];
			}
			// 超出上限
			if (G_low == num_Pt_CoIndex - 1 && G_up == 0) {
				return 1000;
			}
			// 在两个值中间，进行线性插值
			return (float)(Pt_CoIndex[G_low] + (YI - Pt_CoIndex_YI[G_low]) / (Pt_CoIndex_YI[G_up] - Pt_CoIndex_YI[G_low]) * (Pt_CoIndex[G_up] - Pt_CoIndex[G_low]));
		}

		/*	 bool Formula::CalculateHaze(double T1, double T2, double T3, double T4, double% haze, double% Tt, double% Td)
			 {
				float _tt = 0.0f, _td = 0.0f, _haze = 0.0f;
				auto ret = clralgo_calculate_haze(static_cast<float>(T1), static_cast<float>(T2),
					static_cast<float>(T3), static_cast<float>(T4), &_haze, &_tt, &_td);
				haze = _haze;
				Tt = _tt;
				Td = _td;
				return ret != 0;
			 }*/
			 // 比色皿厚度（cm）与浓度（g/mL）可由外部传入，默认 1 cm 、1 g/mL
		static float icumsa_b = 1.0f;
		static float icumsa_c = 1.0f;

			 /// 计算 ICUMSA 色值（420 nm）
		 /// @param spectra  透射光谱（380-780 nm，步长 10 nm，共 41 点）
		 /// @return         糖度（IU）；若数据无效返回 -1
		double Formula::CalculateICUMSA(Spectrum^ spectralData)
		{


			clralgo_Spectra* spectra = ToSpectra(spectralData);

			if (spectra == nullptr) return -1.0f;

			const int targetWave = 420;          // nm
			const int index = (targetWave - 400) / 10;   // 0-based
			if (index < 0 || index >= spectra->range.count) return -1.0f;

			float* a = spectra->data;

			for (size_t i = 0; i < spectra->range.count; i++)
			{

			}
			double T420 = (double)spectra->data[index] / 100.0; // 透射率 0-1
			if (T420 <= 0.0 || T420 > 1.0)        // 边界保护
				return -1.0f;

			double A420 = 2.0 - log10(T420);      // A = 2 - log10(T)
			double IU = (A420 / (icumsa_b + icumsa_c)) * 1000.0;

			return (float)IU - 2000;
		}

		double Formula::CalculateHaze(double T1, double T2, double T3, double T4)
		{
			float _tt = 0.0f, _td = 0.0f, _haze = 0.0f;
			auto ret = clralgo_calculate_haze(static_cast<float>(T1), static_cast<float>(T2),
				static_cast<float>(T3), static_cast<float>(T4), &_haze, &_tt, &_td);
			if (ret != 0)
			{
				return _haze;

			}
			return DOUBLE_NAN;
		}

		double Formula::CalculateSayblot(CIEXYZ^ xyz)
		{
			//根据王工要求,不管什么光源和观察者,固定显示
			//if (Illuminant != StandardIlluminant::C || Observer != StandardObserver::CIE1931)
			//{
			//	return -1.0;
			//}
			clralgo_CIEXYZ _xyz = *xyz;
			return clralgo_calculate_sayblot(&_xyz);
		}
		double Formula::CalculateAdtmD1500(CIEXYZ^ xyz)
		{
			//根据王工要求,不管什么光源和观察者,固定显示
			//if (Illuminant != StandardIlluminant::C || Observer != StandardObserver::CIE1931)
			//{
			//	return -1.0;
			//}
			clralgo_CIEXYZ _xyz = *xyz;
			return clralgo_calculate_astm_d1500(&_xyz);
		}
		unsigned unsigned short Formula::CalculateEightDegreesGloss(Spectrum^ ssci, Spectrum^ ssce, Spectrum^ wsci, Spectrum^ wsce)
		{
			clralgo_Spectra* _ssci = ToSpectra(ssci);
			clralgo_Spectra* _ssce = ToSpectra(ssce);
			clralgo_Spectra* _wsci = ToSpectra(wsci);
			clralgo_Spectra* _wsce = ToSpectra(wsce);
			unsigned short tmp = clralgo_8_gloss_data(_ssci, _ssce, _wsci, _wsce);

			FreeSpectra(_ssci);
			FreeSpectra(_ssce);
			FreeSpectra(_wsci);
			FreeSpectra(_wsce);

			return tmp;
		}

		Spectrum^ Formula::CalculateSpectrum(Spectrum^ source)
		{
			const int min = 360;
			const int max = 780;
			const int count = (780 - 360) / 10 + 1;
			double data[43];
			float reflect[43];
			clralgo_Spectra* _source = ToSpectra(source);
			Spectrum^ target = gcnew Spectrum(WavelengthRange(min, max, 10), gcnew array<double>(count));
			clralgo_reflect_400_700_to_reflect_360_780(_source, reflect);
			for (int i = 0; i < count; i++)
			{
				target->Data[i] = reflect[i];
			}
			FreeSpectra(_source);
			return target;
		}

		Spectrum^ Formula::Interpolate(Spectrum^ source, const int begin, const int end, const int interval)
		{
			clralgo_Spectra* _source = ToSpectra(source);
			auto count = (end - begin) / interval + 1;
			float* reflect = new float[count];
			array<double>^ datas = gcnew array<double>(count);
			clralgo_WavelengthRange range = { begin, interval, count };
			clralgo_interpolate(&_source->range, _source->data, &range, reflect);
			for (int i = 0; i < count; i++)
			{
				datas[i] = reflect[i];
			}
			return gcnew Spectrum(WavelengthRange(begin, end, interval), datas);
		}

		/// <summary>
		/// 计算白砂糖度指数
		/// </summary>
		/// <param name="spectralData"></param>
		/// <param name="icumsa_b"></param>
		/// <param name="icumsa_c"></param>
		/// <returns></returns>
		double Formula::clralgo_calculate_ICUMSA(Spectrum^ spectralData, double icumsa_b, double icumsa_c)
		{
			//b = 1 (比色皿厚度，单位为厘米)
			//c = 1 (溶液浓度g / ml)
			//A420 = 2 - log10(T420)
			//Y_ICUMSA = { A420 / (b + c) } *1000
			clralgo_Spectra* spectra = ToSpectra(spectralData);

			float ICUMSA = 0;
			int wave = (420 - spectra->range.start) / 10;
			double T420 = (double)spectra->data[wave] * 100.0f;
			double A420 = 2 - log10(T420);
			ICUMSA = (A420 / (icumsa_b * icumsa_c)) * 1000;

			return ICUMSA;
		}

		/// <summary>
		/// 计算蔗糖指数
		/// </summary>
		/// <param name="spectralData"></param>
		/// <param name="icumsa_b"></param>
		/// <param name="icumsa_c"></param>
		/// <returns></returns>
		double Formula::clralgo_calculate_ICUMSA2(Spectrum^ spectralData, double icumsa_b, double icumsa_c)
		{
			//b = 1 (比色皿厚度，单位为厘米)
			//c = 1 (溶液浓度g / ml)
			clralgo_Spectra* spectra = ToSpectra(spectralData);

			float ICUMSA = 0;
			int wave = (610 - spectra->range.start) / 10;
			double T610 = (double)spectra->data[wave] * 100.0f;
			double A610 = 2 - log10(T610);
			ICUMSA = (A610 / (icumsa_b * icumsa_c)) * 1000;

			return ICUMSA;
		}

		CIEXYZ^ Formula::GetWhitePoint()
		{
			clralgo_CIEXYZ xyz0 = clralgo_get_white_point_of_illuminant(observer_, illuminant_);
			return gcnew CIEXYZ(xyz0);
		}
		/// <summary>
		/// 中国药典色度计算
		/// </summary>
		/// <param name="index"></param>
		/// <param name="spectrum"></param>
		/// <returns></returns>
		float Formula::CalculateColorScale(int index, Spectrum^ spectrum)
		{
			clralgo_Spectra* spectral = ToSpectra(spectrum);
			clralgo_CIEXYZ xyz;
			PH_CP_Hue_t hue = static_cast<PH_CP_Hue_t>(index);
			clralgo_calculate_xyz(spectral, illuminant_, observer_, &xyz);
			return ph_cp_calculate_color_scale(hue, &xyz);
		}
		/// <summary>
		/// 美国药典色度计算
		/// </summary>
		/// <param name="index"></param>
		/// <param name="spectrum"></param>
		/// <returns></returns>
		int Formula::CalculateUSAColorScale(Spectrum^ spectrum)
		{
			clralgo_Spectra* spectral = ToSpectra(spectrum);
			clralgo_CIEXYZ xyz;
			clralgo_calculate_xyz(spectral, illuminant_, observer_, &xyz);
			return ph_usa_find_hue(&xyz);
		}
		/// <summary>
		/// 欧洲药典色度计算
		/// </summary>
		/// <param name="index"></param>
		/// <param name="spectrum"></param>
		/// <returns></returns>
		float Formula::CalculateEUColorScale(int index, Spectrum^ spectrum)
		{
			clralgo_Spectra* spectral = ToSpectra(spectrum);
			clralgo_CIEXYZ xyz;
			PH_EU_Hue_t hue = static_cast<PH_EU_Hue_t>(index);
			clralgo_calculate_xyz(spectral, illuminant_, observer_, &xyz);
			return ph_eu_calculate_color_scale(hue, &xyz);
		}
		/// <summary>
		/// klett指数值计算
		/// </summary>
		/// <param name="xyz"></param>
		/// <returns></returns>
		float Formula::Calculateklett(CIEXYZ^ xyz)
		{
			clralgo_CIEXYZ _xyz = *xyz;
			return clralgo_calculate_klett_index(&_xyz);
		}

		/// <summary>
		/// 计算MI值----20270728----xdy
		/// </summary>
		/// <param name="spectral_data"></param>
		/// <returns></returns>
		float Formula::CalculateMI(Spectrum^ spectral_data)
		{
			float mi;
			calculate_MI(SpectralAgent(spectral_data), &mi);
			return mi;
		}

		/// <summary>
		/// 计算EI值----20270728----xdy
		/// </summary>
		/// <param name="spectral_data"></param>
		/// <returns></returns>
		float Formula::CalculateEI(Spectrum^ spectral_data)
		{
			float ei;
			calculate_EI(SpectralAgent(spectral_data), &ei);
			return ei;
		}
	}
}