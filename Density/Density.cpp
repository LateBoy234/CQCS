#include "stdafx.h"

#include "Formula.h"
#include "../DensityAlgo/DensityAlgo.h"
#include <intrin.h>


namespace Tnh
{
	namespace Density
	{


		namespace
		{
			class SpectralDataAgent
			{
			public:
				SpectralDataAgent(ISpectralData^ spectrum)
				{
					spectral_data_.start = spectrum->WavelengthBegin;
					spectral_data_.interval = spectrum->WavelengthInterval;
					spectral_data_.count = spectrum->Count;
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

				operator SpectralData* () {return &spectral_data_;}
				operator const SpectralData* () const {return &spectral_data_;}

				int start() const { return spectral_data_.start; }
				int interval() const {return spectral_data_.interval;}
				int count() const {return spectral_data_.count;}
				float* data() {return spectral_data_.data; }
				const float* data() const {return spectral_data_.data; }
			private:
				SpectralData spectral_data_;
			};

			
		}

		double Formula::CalculateDensity(ISpectralData^ spectrum, DensityType densityType)
		{
			SpectralDataAgent spectral_data(spectrum);
			return density_calculate(spectral_data, static_cast<::DensityType>(densityType),
			                         nullptr, nullptr, nullptr, nullptr);
		}

		double Formula::CalculateDensity(ISpectralData^ spectrum, DensityType densityType, DensityType% retDensityType)
		{
			SpectralDataAgent spectral_data(spectrum);
			::DensityType _ret_density_type;
			float density = density_calculate(spectral_data, static_cast<::DensityType>(densityType),
			                                  &_ret_density_type, nullptr, nullptr, nullptr);
			retDensityType = static_cast<DensityType>(_ret_density_type);
			return density;
		}

		double Formula::CalculateDensity(ISpectralData^ spectrum, DensityType densityType, DensityType% retDensityType,
			array<double>^% cmyk)
		{
			SpectralDataAgent spectral_data(spectrum);
			::DensityType _ret_density_type;
			float _ret_cmyk[4];
			float density = density_calculate(spectral_data, static_cast<::DensityType>(densityType),
			                                  &_ret_density_type, _ret_cmyk, nullptr, nullptr);
			retDensityType = static_cast<DensityType>(_ret_density_type);
			cmyk = gcnew array<double> {
				_ret_cmyk[0], _ret_cmyk[1], _ret_cmyk[2], _ret_cmyk[3]
			};

			return density;
		}

		double Formula::CalculateDensity(ISpectralData^ spectrum, DensityType densityType, DensityType% retDensityType,
			array<double>^% cmyk, double% grayness, double% hueError)
		{
			::DensityType _ret_density_type;
			float _ret_cmyk[4];
			float _grayness, _hue_error;
			float _density = density_calculate(SpectralDataAgent(spectrum), 
				static_cast<::DensityType>(densityType), &_ret_density_type, _ret_cmyk,
				&_grayness, &_hue_error);

			retDensityType = static_cast<DensityType>(_ret_density_type);
			cmyk = gcnew array<double> {
				_ret_cmyk[0], _ret_cmyk[1], _ret_cmyk[2], _ret_cmyk[3]
			};
			grayness = _grayness;
			hueError = _hue_error;

			return _density;
		}
	}
}
