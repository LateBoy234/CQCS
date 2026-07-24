#include "pch.h"
#include "Stdafx.h"
#include "clralgo.h"
#include "ColorSpaces.h"
#include "ColorDifferenceFormula.h"

namespace ThreeNH 
{
	namespace ColorAlgorithm
	{

		CIEDE1976::CIEDE1976(CIELAB^ trial, CIELAB^ standard)
			: DEBase(trial, standard)
		{
			dL_ = trial->L - standard->L;
			dC_ = trial->C - standard->C;
			double dh = trial->h - standard->h;
			if (dh > 180.0)
			{
				dh -= 360.0;
			}
			else if (dh < -180.0)
			{
				dh += 360.0;
			}
			dH_ = 2.0 * sqrt(trial->C * standard->C) * sin(RADIANS(dh / 2.0));
			dE_ = sqrt(dL*dL + da*da + db*db); //sqrt(dL*dL + dC*dC + dH*dH);
		}
		void CIEDE1994::init(CIELAB^ trial, CIELAB^ standard, double KL, double KC, double KH)
		{
			float dL, dC, dH;
			clralgo_CIELab lab1 =
			{
				static_cast<float>(trial->L),
				static_cast<float>(trial->a),
				static_cast<float>(trial->b)
			};
			clralgo_CIELab lab2 =
			{
				static_cast<float>(standard->L),
				static_cast<float>(standard->a),
				static_cast<float>(standard->b)
			};

			dE_ = clralgo_calculate_CIEDE94(&lab1, &lab2, static_cast<float>(KL),
				static_cast<float>(KC), static_cast<float>(KH), &dL, &dC, &dH);
			dL_ = dL;
			dC_ = dC;
			dH_ = dH;
		}

		void CIEDE2000::init(CIELAB^ trial, CIELAB^ standard, double KL, double KC, double KH)
		{
			float dL, dC, dH;
			clralgo_CIELab lab1 =
			{
				static_cast<float>(trial->L),
				static_cast<float>(trial->a),
				static_cast<float>(trial->b)
			};
			clralgo_CIELab lab2 =
			{
				static_cast<float>(standard->L),
				static_cast<float>(standard->a),
				static_cast<float>(standard->b)
			};

			dE_ = clralgo_calculate_CIEDE00(&lab1, &lab2, static_cast<float>(KL),
				static_cast<float>(KC), static_cast<float>(KH), &dL, &dC, &dH);
			dL_ = dL;
			dC_ = dC;
			dH_ = dH;
		}

		void CMCDE::init(CIELAB^ trial, CIELAB^ standard, double KL, double KC)
		{
			float dL, dC, dH;
			clralgo_CIELab lab1 =
			{
				static_cast<float>(trial->L),
				static_cast<float>(trial->a),
				static_cast<float>(trial->b)
			};
			clralgo_CIELab lab2 =
			{
				static_cast<float>(standard->L),
				static_cast<float>(standard->a),
				static_cast<float>(standard->b)
			};

			dE_ = clralgo_calculate_CMC(&lab1, &lab2, static_cast<float>(KL),
				static_cast<float>(KC), &dL, &dC, &dH);
			dL_ = dL;
			dC_ = dC;
			dH_ = dH;
		}

		DIN99::DIN99(DIN99Lab^ trial, DIN99Lab^ standard)
			: DEBase(trial, standard)
		{
			dL_ = trial->L - standard->L;
			dC_ = trial->C - standard->C;
			double dh = trial->h - standard->h;
			if (dh > 180.0)
			{
				dh -= 360.0;
			}
			else if (dh < -180.0)
			{
				dh += 360.0;
			}
			dH_ = 2.0 * sqrt(trial->C * standard->C) * sin(RADIANS(dh / 2.0));
			dE_ = sqrt(dL*dL + da*da + db*db);
		}
	}

}