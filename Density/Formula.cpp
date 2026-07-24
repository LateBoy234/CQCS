#include "stdafx.h"

#include "Formula.h"
#include "../DensityAlgo/DensityAlgo.h"
#include "clralgo.h"

namespace Tnh
{
	namespace Density
	{
		namespace
		{
			class SpectralDataAgent
			{
			public:
				SpectralDataAgent(Spectrum^ spectrum)
				{
					spectral_data_.start = spectrum->Range.Min;
					spectral_data_.interval = spectrum->Range.Step;
					spectral_data_.count = spectrum->Range.Count;
					spectral_data_.data = new float[spectral_data_.count];
					for (int i = 0; i < spectral_data_.count; ++i)
					{
						int wavelength = spectral_data_.start + i * spectral_data_.interval;
						spectral_data_.data[i] = static_cast<float>(spectrum[wavelength]);
					}
				}

				~SpectralDataAgent()
				{
					delete[] spectral_data_.data;
					spectral_data_.data = nullptr;
				}

				operator SpectralData*() { return &spectral_data_; }
				operator const SpectralData*() const { return &spectral_data_; }

				int start() const { return spectral_data_.start; }
				int interval() const { return spectral_data_.interval; }
				int count() const { return spectral_data_.count; }
				float* data() { return spectral_data_.data; }
				const float* data() const { return spectral_data_.data; }
			private:
				SpectralData spectral_data_;
			};
		}

		double Formula::CalculateDensity(Spectrum^ spectrum, DensityStatus status, DensityColor color)
		{
			SpectralDataAgent spectral_data(spectrum);
			return density_calculate(spectral_data, static_cast<::DensityStatus>(status),
			                         static_cast<::DensityColor>(color), nullptr, nullptr, nullptr, nullptr);
		}

		double Formula::CalculateDensity(Spectrum^ spectrum, DensityStatus status, DensityColor color,
		                                 DensityColor% retColor)
		{
			SpectralDataAgent spectral_data(spectrum);
			::DensityColor _ret_color;
			float density = density_calculate(spectral_data, static_cast<::DensityStatus>(status),
			                                  static_cast<::DensityColor>(color), &_ret_color, nullptr, nullptr,
			                                  nullptr);
			retColor = static_cast<DensityColor>(_ret_color);
			return density;
		}

		double Formula::CalculateDensity(Spectrum^ spectrum, DensityStatus status, DensityColor color,
		                                 DensityColor% retColor,
		                                 array<double>^% cmyk)
		{
			SpectralDataAgent spectral_data(spectrum);
			::DensityColor _ret_color;
			float _ret_cmyk[4];
			float density = density_calculate(spectral_data, static_cast<::DensityStatus>(status),
			                                  static_cast<::DensityColor>(color), &_ret_color, _ret_cmyk, nullptr,
			                                  nullptr);
			retColor = static_cast<DensityColor>(_ret_color);
			cmyk = gcnew array<double>{
				_ret_cmyk[0], _ret_cmyk[1], _ret_cmyk[2], _ret_cmyk[3]
			};

			return density;
		}

		double Formula::CalculateDensity(Spectrum^ spectrum, DensityStatus status, DensityColor color,
		                                 DensityColor% retColor, array<double>^% cmyk, double% grayness,
		                                 double% hueError)
		{
			::DensityColor _ret_color;
			float _ret_cmyk[4];
			float _grayness, _hue_error;
			float _density = density_calculate(SpectralDataAgent(spectrum),
			                                   static_cast<::DensityStatus>(status), static_cast<::DensityColor>(color),
			                                   &_ret_color, _ret_cmyk,
			                                   &_grayness, &_hue_error);

			retColor = static_cast<DensityColor>(_ret_color);
			cmyk = gcnew array<double>{
				_ret_cmyk[0], _ret_cmyk[1], _ret_cmyk[2], _ret_cmyk[3]
			};
			grayness = _grayness;
			hueError = _hue_error;

			return _density;
		}
		array<double>^ Formula::CalculateCmyk(Spectrum^ spectrum, DensityStatus status)
		{
			float _cmyk[4];
			density_calculate_cmyk(SpectralDataAgent(spectrum), static_cast<::DensityStatus>(status), _cmyk);
			return gcnew array<double>{_cmyk[0], _cmyk[1], _cmyk[2], _cmyk[3]};
		}

		double Formula::CalculateHueError(array<double>^ cmy)
		{
			float _cmy[3] = {static_cast<float>(cmy[0]), static_cast<float>(cmy[1]), static_cast<float>(cmy[2])};
			return ::density_calculate_hue_error(_cmy);
		}

		double Formula::CalculateGrayness(array<double>^ cmy)
		{
			float _cmy[3] = {static_cast<float>(cmy[0]), static_cast<float>(cmy[1]), static_cast<float>(cmy[2])};
			return ::density_calculate_grayness(_cmy);
		}

		double Formula::CalculateSpotDensity(Spectrum^ spectrum, int% wavelength)
		{
			int _wavelength;
			float _density = ::density_calculate_spot(SpectralDataAgent(spectrum), &_wavelength);
			wavelength = _wavelength;
			return _density;
		}

		double Formula::CalculateSpecificSpotDensity(Spectrum^ spectrum, int wavelength)
		{
			return ::density_calculate_specific_spot(SpectralDataAgent(spectrum), wavelength);
		}

		double Formula::CalculateToneValueByMurrayDavies(double paperDensity, double solidDensity, double tintDensity)
		{
			return density_calculate_tone_value_by_murray_davies(
				static_cast<float>(paperDensity), 
				static_cast<float>(solidDensity), 
				static_cast<float>(tintDensity));
		}

		double Formula::CalculateToneValueByYuleNielsen(double paperDensity, double solidDensity, double tintDensity,
			double n)
		{
			return density_calculate_tone_value_by_yule_nielsen(
				static_cast<float>(paperDensity), 
				static_cast<float>(solidDensity), 
				static_cast<float>(tintDensity),
				static_cast<float>(n));
		}

		double Formula::CalculateToneValueBySctv(CIEXYZ^ paper, CIEXYZ^ solid, CIEXYZ^ tint, StandardIlluminant illuminant,
			StandardObserver observer)
		{
			float paperXyz[3] = {static_cast<float>(paper->X), static_cast<float>(paper->Y), static_cast<float>(paper->Z)};
			float solidXyz[3] = {static_cast<float>(solid->X), static_cast<float>(solid->Y), static_cast<float>(solid->Z)};
			float tintXyz[3] = {static_cast<float>(tint->X), static_cast<float>(tint->Y), static_cast<float>(tint->Z)};
			return density_calculate_tone_value_by_sctv(paperXyz, solidXyz, tintXyz,
				static_cast<clralgo_StandardIlluminant>(illuminant),
				static_cast<clralgo_StandardObserver>(observer));
		}

		double Formula::CalculateToneValueBySctv(Spectrum^ paper, Spectrum^ solid, Spectrum^ tint,
			StandardIlluminant illuminant, StandardObserver observer)
		{
			return CalculateToneValueBySctv(
				ThreeNH::ColorAlgorithm::Formula::CalculateXyz(paper, illuminant, observer),
				ThreeNH::ColorAlgorithm::Formula::CalculateXyz(solid, illuminant, observer),
				ThreeNH::ColorAlgorithm::Formula::CalculateXyz(tint, illuminant, observer),
				illuminant, observer);
		}

		double Formula::CalculateToneValueIncrease(double filmToneValue, double printedToneValue)
		{
			return density_calculate_tone_value_increase(
			static_cast<float>(filmToneValue), static_cast<float>(printedToneValue));
		}

		int Formula::MatchFilmToneValues(array<int>^ filmToneValues, double toneValue)
		{
			
			array<int>^ sortedArray;
			if (array<int>::IndexOf(filmToneValues, 100) < 0) {
				sortedArray = gcnew array<int>(filmToneValues->Length + 1);
				sortedArray[0] = 100;
				filmToneValues->CopyTo(sortedArray, 1);
			}
			else
			{
				sortedArray = gcnew array<int>(filmToneValues->Length);
				filmToneValues->CopyTo(sortedArray, 0);
			}
			array<int>::Sort(sortedArray);

			for (int i = sortedArray->Length - 1; i > 0; i--)
			{
				if (toneValue >= sortedArray[i] || (sortedArray[i] - toneValue) * 5 < (sortedArray[i] - sortedArray[i - 1]))
				{
					return sortedArray[i];
				}
			}
			return sortedArray[0];
		}

		double Formula::CalculateTrappingByPreucil(double paper, double d1, double d2, double dop)
		{
			return ::density_calculate_trapping_by_preucil(
			static_cast<float>(paper), static_cast<float>(d1),
				static_cast<float>(d2), static_cast<float>(dop));
		}

		double Formula::CalculateTrappingByBrunner(double paper, double d1, double d2, double dop)
		{
			return ::density_calculate_trapping_by_brunner(
			static_cast<float>(paper), static_cast<float>(d1),
				static_cast<float>(d2), static_cast<float>(dop));
		}

		double Formula::CalculateTrappingByRitz(double paper, double d1, double d2, double dop)
		{
			return ::density_calculate_trapping_by_ritz(
			static_cast<float>(paper), static_cast<float>(d1),
				static_cast<float>(d2), static_cast<float>(dop));
		}

		double Formula::CalculateContrast(double solid, double tint)
		{
			return ::density_calculate_contrast(
			static_cast<float>(solid), static_cast<float>(tint));
		}
	}
}
