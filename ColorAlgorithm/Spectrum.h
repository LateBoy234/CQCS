#pragma once
#include "WaveLengthRange.h"

using namespace System::Runtime::Serialization;

namespace ThreeNH {
	namespace ColorAlgorithm
	{
		[DataContract(Name="Spectrum")]
		public ref class Spectrum
		{
		public:
			Spectrum() 
			: range_(WavelengthRange(380, 740, 10))
			, data_(gcnew  array<double>(37)) { }

			Spectrum(WavelengthRange range)
				: range_(range)
				, data_(gcnew array<double>(range.Count)) { }

			Spectrum(WavelengthRange range, array<double>^ data)
				: range_(range), data_(data) { }


			Spectrum(Spectrum^ other) 
			: range_(other->range_)
			, data_(gcnew array<double>(other->data_->Length))
			{
				for (int i = 0; i < other->data_->Length; i++)
				{
					data_[i] = other->data_[i];
				}
			}

			property WavelengthRange Range 
			{
				WavelengthRange get() { return range_; }
				void set(WavelengthRange value) { range_ = value; }
			}

			property array<double>^ Data {
				array<double>^ get() { return data_; }
			}

			property double default[int]{
				double get(int wavelength) { return data_[range_.IndexOf(wavelength)]; }
				void set(int wavelength, double value)
				{
					data_[range_.IndexOf(wavelength)] = value;
				}
			}
            
			Object^ Clone() { return gcnew Spectrum(this); }

			/// <summary>
			/// 将光谱数据插值成指定波长范围
			/// </summary>
			/// <param name="range">新的范長范圍，如果起始波长范围小于当前波长最小值或大于最大值，找抛出异常</param>
			/// <returns>反回插值後的反射率數據</returns>
			/// <exception cref="ArgumentOutOfRangeException">當給定波長的區間超過當前反射率數據的區間</exception>
			Spectrum^ Interpolate(WavelengthRange newRange);


		private:
			[DataMember(Name="_range", Order=0)]
			WavelengthRange range_;
			[DataMember(Name="_data", Order=1)]
			array<double>^ data_;
		};
	}
}