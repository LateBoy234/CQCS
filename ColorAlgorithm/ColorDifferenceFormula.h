#pragma once

namespace ThreeNH {
	namespace ColorAlgorithm
	{

		ref class CIELAB;
		ref class CIELCh;

		public ref class DEBase abstract
		{
		public:
			DEBase(Lab^ trial, Lab^ standard)
				: dL_(trial->L - standard->L)
				, da_(trial->a - standard->a)
				, db_(trial->b - standard->b)
			{

			}

			property double dL { double get() { return dL_; }}
			property double da { double get() { return da_; }}
			property double db { double get() { return db_; }}
			property double dC { double get() { return dC_; }}
			property double dH { double get() { return dH_; }}
			property double dE { double get() { return dE_; }}

		private protected:
			double dL_;
			double da_;
			double db_;
			double dC_;
			double dH_;
			double dE_;
		};

		public ref class CIEDE1976 sealed : public DEBase
		{
		public:
			CIEDE1976(CIELAB^ trial, CIELAB^ standard);
		};

		public ref class CIEDE1994 sealed : public DEBase
		{
		public:
			CIEDE1994(CIELAB^ trial, CIELAB^ standard)
				: DEBase(trial, standard) {
				init(trial, standard, 1.0, 1.0, 1.0);
			}
			CIEDE1994(CIELAB^ trial, CIELAB^ standard, double KL, double KC, double KH)
				: DEBase(trial, standard) {
				init(trial, standard, KL, KC, KH);
			}

		private:
			void init(CIELAB^ trial, CIELAB^ standard, double KL, double KC, double KH);
		};

		public ref class CIEDE2000 sealed : public DEBase
		{
		public:
			CIEDE2000(CIELAB^ trial, CIELAB^ standard)
				: DEBase(trial, standard) {
				init(trial, standard, 1.0, 1.0, 1.0);
			}
			CIEDE2000(CIELAB^ trial, CIELAB^ standard, double KL, double KC, double KH)
				: DEBase(trial, standard) {
				init(trial, standard, KL, KC, KH);
			}

		private:
			void init(CIELAB^ trial, CIELAB^ standard, double KL, double KC, double KH);
		};

		public ref class CMCDE sealed : public DEBase
		{
		public:
			CMCDE(CIELAB^ trial, CIELAB^ standard)
				: DEBase(trial, standard) {
				init(trial, standard, 2.0, 1.0);
			}
			CMCDE(CIELAB^ trial, CIELAB^ standard, double KL, double KC)
				: DEBase(trial, standard) {
				init(trial, standard, KL, KC);
			}

		private:
			void init(CIELAB^ trial, CIELAB^ standard, double KL, double KC);
		};

		public ref class DeltaXYZ sealed
		{
		public:
			DeltaXYZ(CIEXYZ^ trial, CIEXYZ^ standard)
			{
				dX_ = trial->X - standard->X;
				dY_ = trial->Y - standard->Y;
				dZ_ = trial->Z - standard->Z;
				dx_ = trial->x - standard->x;
				dy_ = trial->y - standard->y;
				dz_ = trial->z - standard->z;
			}

			property double dX { double get() { return dX_; } void set(double value) { dX_ = value; }}
			property double dY { double get() { return dY_; } void set(double value) { dY_ = value; }}
			property double dZ { double get() { return dZ_; } void set(double value) { dZ_ = value; }}
			property double dx { double get() { return dx_; } void set(double value) { dx_ = value; }}
			property double dy { double get() { return dy_; } void set(double value) { dy_ = value; }}
			property double dz { double get() { return dz_; } void set(double value) { dz_ = value; }}

		private:
			double dX_;
			double dY_;
			double dZ_;
			double dx_;
			double dy_;
			double dz_;
		};

		public ref class DeltaHunterLab sealed
		{
		public:
			DeltaHunterLab(HunterLab^ trial, HunterLab^ standard)
			{
				dL_ = trial->L - standard->L;
				da_ = trial->a - standard->a;
				db_ = trial->b - standard->b;
				dE_ = sqrt(dL_ * dL_ + da_ * da_ + db_ * db_);
			}

			property double dL { double get() { return dL_; }}
			property double da { double get() { return da_; }}
			property double db { double get() { return db_; }}
			property double dE { double get() { return dE_; }}

		private protected:
			double dL_;
			double da_;
			double db_;
			double dE_;
		};

		public ref class DeltaLuv sealed
		{
		public:
			DeltaLuv(CIELUV^ trial, CIELUV^ standard)
			{
				dL_ = trial->L - standard->L;
				du_ = trial->u - standard->u;
				dv_ = trial->v - standard->v;
				dC_ = trial->C - standard->C;
				dh_ = trial->h - standard->h;
				duq_ = trial->uq - standard->uq;
				dvq_ = trial->vq - standard->vq;
				if (dh_ > 180.0)
					dh_ -= 360.0;
				else if (dh_ < -180.0)
					dh_ += 360.0;
				dH_ = 2.0 * sqrt(trial->C * standard->C)
					* sin(RADIANS(dh_ / 2.0));
				dE_ = sqrt(dL_ * dL_ + du_ * du_ + dv_ * dv_);
			}

			property double dL { double get() { return dL_; }}
			property double du { double get() { return du_; }}
			property double dv { double get() { return dv_; }}
			property double dC { double get() { return dC_; }}
			property double dh { double get() { return dh_; }}
			property double dH { double get() { return dH_; }}
			property double dE { double get() { return dE_; }}
			property double duq {double get() { return duq_; }}
			property double dvq {double get() { return dvq_; }}
		private:
			double dL_;
			double du_;
			double dv_;
			double dC_;
			double dh_;
			double dH_;
			double dE_;
			double duq_;
			double dvq_;
		};

		public ref class DeltaRGB sealed
		{
		public:
			DeltaRGB(CIERGB^ trial, CIERGB^ standard)
			{
				dR_ = trial->R - standard->R;
				dG_ = trial->G - standard->G;
				dB_ = trial->B - standard->B;
				dr_ = trial->r - standard->r;
				dg_ = trial->g - standard->g;
				db_ = trial->b - standard->b;
			}

			property double dR {double get() { return dR_; }}
			property double dG {double get() { return dG_; }}
			property double dB {double get() { return dB_; }}
			property double dr {double get() { return dr_; }}
			property double dg {double get() { return dg_; }}
			property double db {double get() { return db_; }}

		private:
			double dR_;
			double dG_;
			double dB_;
			double dr_;
			double dg_;
			double db_;
		};

		public ref class DIN99 sealed : public DEBase
		{
		public:
			DIN99(DIN99Lab^ trial, DIN99Lab^ standard);
		};

		public ref class DeltaMunsellHVC sealed
		{
		public:
			DeltaMunsellHVC(MunsellHVC^ trial, MunsellHVC^ standard)
			{
				dMH_ = trial->H - standard->H;
				dMV_ = trial->V - standard->V;
				dMC_ = trial->C - standard->C;
				Hstr_ = trial->Hstr;
			}

			property double dMH {double get() { return dMH_; }}

			property double dMV {double get() { return dMV_; }}

			property double dMC {double get() { return dMC_; }}

			property System::String^ Hstr
			{
				System::String^ get() { return Hstr_; }
				void set(System::String^ value) { Hstr_ = value; }
			}
		private:
			double dMH_;
			double dMV_;
			double dMC_;
			System::String^ Hstr_;
		};
	}
}