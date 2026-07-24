#pragma once
#include <cmath>
#include <string>
using std::string;

using namespace System::Runtime::Serialization;

namespace ThreeNH {
	namespace ColorAlgorithm
	{
#define clralgo_PI       3.14159265358979323846		// PI
#define clralgo_1_PI     0.318309886183790671538	// 1/PI

		// 数组的大小
#define clralgo_ARRAY_SIZE(arr)     (sizeof(arr)/sizeof(arr[0]))
// 将角度转换为弧度
#define RADIANS(degrees)		((float)((degrees) / 180.0 * clralgo_PI))
// 将弧度转换为角度
#define DEGREES(radians)		((float)((radians) * clralgo_1_PI * 180.0))
// 判断浮点数是否接近0
#define IS_NEAR_ZERO(value)		(fabs(value) < 0.000001f)


// 将小数约分为整数
//int round(double x);
// 将小数保留precision位小数点
		static double round_to(double x, int precision)
		{
			double result = (int)x;
			double mult = (float)pow(10.0, precision);
			double decimal = (x - result) * mult;
			double t = decimal - (int)decimal;

			if (t > 0.5)
				decimal += 0.5;
			else if (t < -0.5)
				decimal -= 0.5;
			return result + ((int)decimal) / mult;
		}

		// 求两个角度的平均值
		static float average_degree(float a, float b)
		{
			if (fabs(a - b) > 180)
			{
				return (a + b < 360) ? (a + b) / 2 + 180 : (a + b) / 2 - 180;
			}
			return (a + b) / 2;
		}


		// 根据y和x的值反算相应的弧度，与标准库不同的地方是会判断y或x接近零的情况
		static float my_atan2(float y, float x)
		{
			if (IS_NEAR_ZERO(y)) {
				if (IS_NEAR_ZERO(x) || x > 0.0f)
					return 0.0f;
				return (float)clralgo_PI;
			}
			if (IS_NEAR_ZERO(x)) {
				if (y > 0.0f)
					return (float)(clralgo_PI / 2.0);
				return (float)(-clralgo_PI / 2.0);
			}
			return atan2f(y, x);
		}
		ref class CIELCh;
		ref class CIEYxy;

		[DataContract]
		public ref class Lab
		{
		public:
			Lab() : L_(0.0), a_(0.0), b_(0.0) { }

			Lab(double L, double a, double b)
				: L_(L), a_(a), b_(b) { }

			property double L
			{
				double get() { return L_; }
				void set(double value) { L_ = value; }
			}

			property double a
			{
				double get() { return a_; }
				void set(double value) { a_ = value; }
			}

			property double b
			{
				double get() { return b_; }
				void set(double value) { b_ = value; }
			}

		private:
			[DataMember(Name = "L")]
			double L_;
			[DataMember(Name = "a")]
			double a_;
			[DataMember(Name = "b")]
			double b_;
		};

		[DataContract]
		public ref class CIELAB sealed : public Lab
		{
		public:
			CIELAB() { }

			CIELAB(double L, double a, double b)
				: Lab(L, a, b) { }

			property double C
			{
				double get()
				{
					return sqrt(a * a + b * b);
				}
			}

			property double h
			{
				double get();
			}

			CIELCh^ ToLCh();

		internal:
			CIELAB(const clralgo_CIELab& lab)
				: Lab(lab.L, lab.a, lab.b) { }
				
			operator clralgo_CIELab ()
			{
				clralgo_CIELab lab = { static_cast<float>(L), static_cast<float>(a), static_cast<float>(b) };
				return  lab;
			}

			operator clralgo_CIELCh ()
			{
				clralgo_CIELCh lch = { static_cast<float>(L), static_cast<float>(C), static_cast<float>(h) };
				return lch;
			}
		};

		[DataContract]
		public ref class CIELCh sealed
		{
		public:
			CIELCh() : L_(0.0), C_(0.0), h_(0.0) {}

			CIELCh(double L, double C, double h)
				: L_(L), C_(C), h_(h) {}

			property double L
			{
				double get() { return L_; }
				void set(double value) { L_ = value; }
			}

			property double C
			{
				double get() { return C_; }
				void set(double value) { C_ = value; }
			}

			property double h
			{
				double get() { return h_; }
				void set(double value) { h_ = value; }
			}

			property double a
			{
				double get();
			}

			property double b
			{
				double get();
			}

			CIELAB^ ToLab();

		private:
			[DataMember(Name = "L")]
			double L_;
			[DataMember(Name = "C")]
			double C_;
			[DataMember(Name = "h")]
			double h_;
		};

		[DataContract]
		public ref class CIEXYZ sealed
		{
		public:
			CIEXYZ() : X_(0.0), Y_(0.0), Z_(0.0) { }

			CIEXYZ(double X, double Y, double Z)
				: X_(X), Y_(Y), Z_(Z) { }

			property double X
			{
				double get() { return X_; }
				void set(double value) { X_ = value; }
			}
			property double Y
			{
				double get() { return Y_; }
				void set(double value) { Y_ = value; }
			}
			property double Z
			{
				double get() { return Z_; }
				void set(double value) { Z_ = value; }
			}

			property double x
			{
				double get()
				{
					double tmp = X + Y + Z;
					return tmp != 0.0 ? X / tmp : 0.0;
				}
			}
			property double y
			{
				double get()
				{
					double tmp = X + Y + Z;
					return tmp != 0.0 ? Y / tmp : 0.0;
				}
			}
			property double z
			{
				double get()
				{
					double tmp = X + Y + Z;
					return tmp != 0.0 ? Z / tmp : 0.0;
				}
			}

			property CIEYxy^ Yxy
			{
				CIEYxy^ get();
				void set(CIEYxy^ value);
			}

		internal:
			CIEXYZ(const clralgo_CIEXYZ& xyz)
				: X_(xyz.X), Y_(xyz.Y), Z_(xyz.Z) { }

			operator clralgo_CIEXYZ()
			{
				clralgo_CIEXYZ xyz = { static_cast<float>(X_), static_cast<float>(Y_), static_cast<float>(Z_) };
				return xyz;
			}

			operator clralgo_CIEYxy()
			{
				clralgo_CIEYxy yxy = { static_cast<float>(Y), static_cast<float>(x), static_cast<float>(y) };
				return yxy;
			}

		private:
			[DataMember(Name = "X")]
			double X_;
			[DataMember(Name = "Y")]
			double Y_;
			[DataMember(Name = "Z")]
			double Z_;
		};

		[DataContract]
		public ref class CIEYxy sealed
		{
		public:
			CIEYxy() { }
			CIEYxy(double Y, double x, double y)
				: Y_(Y), x_(x), y_(y) { }

			property double Y
			{
				double get() { return Y_; }
				void set(double value) { Y_ = value; }
			}

			property double x
			{
				double get() { return x_; }
				void set(double value) { x_ = value; }
			}

			property double y
			{
				double get() { return y_; }
				void set(double value) { y_ = value; }
			}

			property double X
			{
				double get()
				{
					if (y != 0 && Y != 0)
						return Y / y * x;
					return 0.0;
				}
			}
			property double Z
			{
				double get()
				{
					if (y != 0 && Y != 0)
						return Y / y * z;
					return 0.0;
				}
			}
			property double z
			{
				double get()
				{
					return 1.0 - x - y;
				}
			}

			property CIEXYZ^ XYZ
			{
				CIEXYZ^ get();
				void set(CIEXYZ^ XYZ);
			}
		private:
			[DataMember(Name = "Y")]
			double Y_;
			[DataMember(Name = "x")]
			double x_;
			[DataMember(Name = "y")]
			double y_;
		};

		[DataContract]
		public ref class CIELUV sealed
		{
		public:
			CIELUV() { }
			CIELUV(double L, double u, double v)
			{
				L = L;
				u = u;
				v = v;
			}

			[DataMember(Name = "L")]
			property double L;
			[DataMember(Name = "u")]
			property double u;
			[DataMember(Name = "b")]
			property double v;
			[DataMember(Name = "uq")]
			property double uq;
			[DataMember(Name = "vq")]
			property double vq;
			property double s
			{
				double get()
				{
					return (L != 0.0) ? (C / L) : 0.0;
				}
			}
			property double C
			{
				double get() { return sqrt(u * u + v * v); }
			}
			property double h
			{
				double get();
			}

		internal:
			CIELUV(const clralgo_CIELuv& luv)
			{
				L = luv.L;
				u = luv.u;
				v = luv.v;
				//uq = luv.uq;
				//vq = luv.vq;
			}

			/*operator clralgo_CIELuv()
			{
				clralgo_CIELuv luv = { static_cast<float>(L), static_cast<float>(u), static_cast<float>(v),static_cast<float>(uq),static_cast<float>(vq) };
				return luv;
			}*/
		};

		[DataContract]
		public ref class HunterLab sealed : public Lab
		{
		public:
			HunterLab() { }

			HunterLab(double L, double a, double b)
				: Lab(L, a, b) { }

		internal:
			HunterLab(const clralgo_HunterLab& lab)
				: Lab(lab.L, lab.a, lab.b) { }

			operator clralgo_HunterLab()
			{
				clralgo_HunterLab lab = {
					static_cast<float>(L),
					static_cast<float>(a),
					static_cast<float>(b),
				};
				return lab;
			}

		};

		[DataContract]
		public ref class DIN99Lab sealed : public Lab
		{
		public:
			DIN99Lab() { }
			DIN99Lab(double L, double a, double b)
				: Lab(L, a, b) { }

			property double C
			{
				double get()
				{
					return sqrt(a * a + b * b);
				}
			}

			property double h
			{
				double get();
			}

		internal:
			DIN99Lab(const clralgo_DIN99Lab& lab)
				: Lab(lab.L, lab.a, lab.b) { }
			operator clralgo_DIN99Lab()
			{
				clralgo_DIN99Lab lab = {
					static_cast<float>(L),
					static_cast<float>(a),
					static_cast<float>(b)
				};
				return lab;
			}
		};

		[DataContract]
		public ref class CIERGB
		{
		public:
			CIERGB()
			{
				this->R = 0;
				this->G = 0;
				this->B = 0;
			}

			CIERGB(double R, double G, double B)
			{
				this->R = R;
				this->G = G;
				this->B = B;
			}

			[DataMember(Name = "R")]
			property double R;
			[DataMember(Name = "G")]
			property double G;
			[DataMember(Name = "B")]
			property double B;

			property double r
			{
				double get()
				{
					if (R + G + B != 0.0)
						return R / (R + G + B);
					return 0.0;
				}
			}

			property double g
			{
				double get()
				{
					if (R + G + B != 0.0)
						return G / (R + G + B);
					return 0.0;
				}
			}

			property double b
			{
				double get()
				{
					if (R + G + B != 0.0)
						return B / (R + G + B);
					return 0.0;
				}
			}

		internal:
			CIERGB(const clralgo_CIERGB& rgb)
			{
				this->R = rgb.R;
				this->G = rgb.G;
				this->B = rgb.B;
			}

			operator clralgo_CIERGB()
			{
				clralgo_CIERGB rgb = {
					static_cast<float>(R),
					static_cast<float>(G),
					static_cast<float>(B),
				};
				return rgb;
			}
		};

		[DataContract]
		public value class sRGB sealed
		{
		public:
			sRGB(unsigned int rgb) : rgb_(rgb) { }

			sRGB(unsigned char R, unsigned char G, unsigned char B)
			{
				rgb_ = ((R & 0xFF) << 16) | ((G & 0xFF) << 8) | (B & 0xFF);
			}

			property unsigned char R
			{
				unsigned char get()
				{
					return static_cast<unsigned char>(rgb_ >> 16);
				}
				void set(unsigned char value)
				{
					rgb_ = (rgb_ & 0xFF00FFFF) | (static_cast<int>(value) << 16);
				}
			}

			property unsigned char G
			{
				unsigned char get()
				{
					return static_cast<unsigned char>(rgb_ >> 8);
				}
				void set(unsigned char value)
				{
					rgb_ = (rgb_ & 0xFFFF00FF) | (static_cast<int>(value) << 8);
				}
			}

			property unsigned char B
			{
				unsigned char get()
				{
					return static_cast<unsigned char>(rgb_ & 0xFF);
				}
				void set(unsigned char value)
				{
					rgb_ = (rgb_ & 0xFFFFFF00) | static_cast<int>(value & 0xFF);
				}
			}

			unsigned int ToInteger()
			{
				return rgb_;
			}

		internal:
			sRGB(const clralgo_sRGB& srgb)
			{
				R = static_cast<unsigned char>(round_to(srgb.R * 255, 0));
				G = static_cast<unsigned char>(round_to(srgb.G * 255, 0));
				B = static_cast<unsigned char>(round_to(srgb.B * 255, 0));
			}


		private:
			[DataMember(Name = "value")]
			unsigned int rgb_;
		};

		[DataContract]
		public ref class MunsellHVC
		{
		public:
			MunsellHVC() {}

			MunsellHVC(double H, double V, double C, const char Hstr[2])
			{
				H_ = H;
				V_ = V;
				C_ = C;
				Hstr_ = gcnew System::String(Hstr,0,2);
			}

			property double H
			{
				double get() { return H_; }
				void set(double value) { H_ = value; }
			}

			property double V
			{
				double get() { return V_; }
				void set(double value) { V_ = value; }
			}

			property double C
			{
				double get() { return C_; }
				void set(double value) { C_ = value; }
			}

			property System::String^ Hstr
			{
				System::String^ get() { return Hstr_; }
				void set(System::String^ value) { Hstr_ = value; }
			}

		internal:

			MunsellHVC(const clralgo_Munsell& munsell)
				: MunsellHVC(munsell.H, munsell.V, munsell.C, munsell.H_str) { }
			operator clralgo_Munsell()
			{
				clralgo_Munsell munsell = {
					static_cast<float>(H_),
					static_cast<float>(V_),
					static_cast<float>(C_),
				};
				return munsell;
			}
			[DataMember(Name = "H")]
			double H_;
			[DataMember(Name = "V")]
			double V_;
			[DataMember(Name = "C")]
			double C_;
			[DataMember(Name = "Hstr")]
			System::String ^Hstr_;
		};

	}
}