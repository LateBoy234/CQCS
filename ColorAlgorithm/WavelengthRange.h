#pragma once

using namespace System::Runtime::Serialization;

namespace ThreeNH {
	namespace ColorAlgorithm
	{
		[DataContract]
		public value class WavelengthRange

		{
		public:
			// WavelengthRange()
			// 	: min_(380), max_(740), step_(10) { }
			WavelengthRange(int min, int max)
				: min_(min), max_(max), step_(10) { }
			WavelengthRange(int min, int max, int step)
				: min_(min), max_(max), step_(step)
			{
			}

			static const WavelengthRange InvalidRange = WavelengthRange(0, 0, 0);

			property int Min {
				int get() { return min_; }
				void set(int min) { min_ = min; }
			}

			property int Max {
				int get() { return max_; }
				void set(int max) { max_ = max; }
			}

			property int Step {
				int get() { return step_; }
				void set(int step) { step_ = step; }
			}

			property bool IsValid
			{
				bool get()
				{
					return Min > 0 && Max > 0 && (Min < Max) && (Max - Min) % Step == 0;
				}
			}

			property int Count
			{
				int get()
				{
					return IsValid ? (Max - Min) / Step + 1 : 0;
				}
			}


			property array<int>^ Wavelengths {
				array<int>^ get()
				{
					array<int>^ wavelengths = gcnew array<int>(Count);
					for (int wavelength = min_, i = 0; wavelength <= max_; wavelength+= step_, i++)
					{
						wavelengths[i] = wavelength;
					}
					return wavelengths;
				}
			}

			property int default[int]{
				int get(int index)
				{
					return min_ + index * step_;
				}
			}

				bool Contains(int wavelength)
			{
				return (IsValid && wavelength >= Min &&
					wavelength <= Max && (wavelength - Min) % Step == 0);
			}

			bool Contains(WavelengthRange range)
			{
				return (Min <= range.Min) && (Max >= range.Max)
					&& (range.Step % Step == 0) && ((range.Min - Min) % Step == 0);
			}

			int IndexOf(int wavelength)
			{
				if (Contains(wavelength))
				{
					return (wavelength - Min) / Step;
				}
				return -1;
			}

			static bool operator==(WavelengthRange left, WavelengthRange right)
			{
				return left.Min == right.Min && left.Max == right.Max && left.Step == right.Step;
			}

			static bool operator!=(WavelengthRange left, WavelengthRange right)
			{
				return left.Min != right.Min || left.Max != right.Max || left.Step != right.Step;
			}

//			bool operator==(WavelengthRange^ other)
//			{
//				return Min == other->Min && Max == other->Max && Step == other->Step;
//			}
//
//			bool operator!=(WavelengthRange^ other)
//			{
//				return Min != other->Min || Max != other->Max || Step != other->Step;
//			}


		private:
			[DataMember(Name="Min", Order=0)]
			int min_;
			[DataMember(Name="Max", Order=1)]
			int max_;
			[DataMember(Name="Step", Order=2)]
			int step_;
		};
	}
}