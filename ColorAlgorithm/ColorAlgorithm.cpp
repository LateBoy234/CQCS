
#include "ColorAlgorithm.h"
#include "stdafx.h"



float  CalculateColorScale(PH_CP_Hue_t hue, const clralgo_CIEXYZ* sample)
{
	return ph_cp_calculate_color_scale(hue, sample);
}
