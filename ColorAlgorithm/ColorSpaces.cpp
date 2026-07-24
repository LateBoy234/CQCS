#include "pch.h"
#include "Stdafx.h"
#include "clralgo.h"
#include "ColorSpaces.h"

namespace ThreeNH {

	namespace ColorAlgorithm
	{

		/************************************************
		 * CIELAB
		 ************************************************/
		double CIELAB::h::get()
		{
			double t = DEGREES(my_atan2(static_cast<float>(b), static_cast<float>(a)));
			if (t < 0)
				t += 360.0;
			return t;
		}

		CIELCh^ CIELAB::ToLCh()
		{
			return gcnew CIELCh(L, C, h);
		}


		/************************************************
		* CIELCh
		************************************************/
		double CIELCh::a::get()
		{
			return C * cos(RADIANS(h));
		}

		double CIELCh::b::get()
		{
			return C *  sin(RADIANS(h));
		}

		CIELAB^ CIELCh::ToLab()
		{
			return gcnew CIELAB(L, a, b);
		}

		/****************************************************
		 * CIEXYZ
		 ***************************************************/
		CIEYxy^ CIEXYZ::Yxy::get()
		{
			return gcnew CIEYxy(Y, x, y);
		}

		void CIEXYZ::Yxy::set(CIEYxy^ value)
		{
			X_ = value->X;
			Y_ = value->Y;
			Z_ = value->Z;
		}

		/****************************************************
		* CIEYxy
		***************************************************/
		CIEXYZ^ CIEYxy::XYZ::get()
		{
			return gcnew CIEXYZ(X, Y, Z);
		}

		void CIEYxy::XYZ::set(CIEXYZ^ value)
		{
			Y_ = value->Y;
			x_ = value->x;
			y_ = value->y;
		}

		/*******************************************************
		 * CIELUV
		 ******************************************************/
		double CIELUV::h::get()
		{
			double t = DEGREES(my_atan2(static_cast<float>(v), static_cast<float>(u)));
			if (t < 0)
				t += 360.0;
			return t;
		}

		/*******************************************************
		 * DIN99Lab
		 *******************************************************/
		double DIN99Lab::h::get()
		{
			double t = DEGREES(my_atan2(static_cast<float>(b), static_cast<float>(a)));
			if (t < 0)
				t += 360.0;
			return t;
		}
	}
}