#include "DensityAlgo.h"
#include "DensityDataTables.h"
#include <assert.h>
#include <math.h>
#include <string.h>

#include "clralgo.h"
#include "tristimulus_weighting_factors.h"

#define IS_SPECTRAL_DATA_VALID(spectral_data) \
	((spectral_data) && (spectral_data)->data && (spectral_data)->count > 0 && (spectral_data)->interval)

#define CALCULATE_GRAYNESS(cmy, minimum, maximum) \
	(cmy[minimum] / cmy[maximum] * 100.0f)

#define CALCULATE_HUEERROR(cmy, minimum, maximum, middle) \
	((cmy[middle] - cmy[minimum]) / (cmy[maximum] - cmy[minimum]) * 100.0f);

#define GET_AUTO_DENSITY_COLOR(cmyk, minimum, maximum) \
	((cmyk[maximum] - cmyk[minimum] <= VISUAL_DENSITY_THRESH) ? DENSITY_K : (enum DensityColor)((maximum) + 1))


static float calculate_density(const struct SpectralData* spectral_data,
                               const DensityWeightingFactors* factors)
{
	/* 反射密度计算公式
	 * D = -log10(Σ(Wλ*Rλ/100))
	 * 其中W为权重因子，R为反射率
	 */

	int i;
	int wavelength_min = spectral_data->start;
	int wavelength_max = spectral_data->start + (spectral_data->count - 1) * spectral_data->interval;
	float sum = 0.0;

	assert(spectral_data != NULL);
	assert(spectral_data->interval == factors->interval);
	assert(spectral_data->count != 0);

	for (i = 0; i < factors->count; i++)
	{
		int wavelength = factors->start + i * factors->interval;
		if (wavelength < wavelength_min)
		{
			//sum += factors->data[i] * spectral_data->data[0];
		}
		else if (wavelength > wavelength_max)
		{
			//sum += factors->data[i] * spectral_data->data[spectral_data->count - 1];
		}
		else
		{
			int j = (wavelength - wavelength_min) / spectral_data->interval;
			sum += factors->data[i] * spectral_data->data[j];
		}
	}

	return -log10f(sum / 100.0f);
}

// 获取三元组的最大、最小、和中间值的索引
static void get_min_max_middle(const float triple[], int* min_index, int* max_index, int* middle_index)
{
	if (triple[0] < triple[1] && triple[0] < triple[2])
	{
		*min_index = 0;
		if (triple[1] < triple[2])
		{
			*max_index = 2;
			*middle_index = 1;
		}
		else
		{
			*max_index = 1;
			*middle_index = 2;
		}
	}
	else if (triple[1] < triple[0] && triple[1] < triple[2])
	{
		*min_index = 1;
		if (triple[0] < triple[2])
		{
			*max_index = 2;
			*middle_index = 0;
		}
		else
		{
			*max_index = 0;
			*middle_index = 2;
		}
	}
	else //if (triple[2] < triple[0] && triple[2] < triple[1])
	{
		*min_index = 2;
		if (triple[0] < triple[1])
		{
			*max_index = 1;
			*middle_index = 0;
		}
		else
		{
			*max_index = 0;
			*middle_index = 1;
		}
	}
}

float density_calculate(const SpectralData* spectral_data, DensityStatus status, DensityColor color,
                        DensityColor* ret_color, float* cmyk, float* grayness, float* hue_error)
{
	assert(IS_SPECTRAL_DATA_VALID(spectral_data));
	assert(color <= DENSITY_K);

	// 自动选择、获取灰度或CMYK，都需要计算所有类型密度
	if (color == DENSITY_AUTO || grayness || cmyk)
	{
		float _cmyk[4];
		DensityColor _ret_color = color;
		int minimum, maximum, middle;
		density_calculate_cmyk(spectral_data, status, _cmyk);

		get_min_max_middle(_cmyk, &minimum, &maximum, &middle);

		if (grayness)
		{
			*grayness = CALCULATE_GRAYNESS(_cmyk, minimum, maximum);
		}
		if (hue_error)
		{
			*hue_error = CALCULATE_HUEERROR(_cmyk, minimum, maximum, middle);
		}
		if (cmyk)
		{
			memcpy(cmyk, _cmyk, sizeof(_cmyk));
		}

		if (color == DENSITY_AUTO)
		{
			_ret_color = GET_AUTO_DENSITY_COLOR(_cmyk, minimum, maximum);
		}

		if (ret_color)
		{
			*ret_color = _ret_color;
		}

		return _cmyk[_ret_color - 1];
	}

	if (ret_color != NULL)
	{
		*ret_color = color;
	}
	return calculate_density(spectral_data, density_get_weighting_factors(status, color));
}


void density_calculate_cmyk(const SpectralData* spectral_data, DensityStatus status, float* cmyk)
{
	const DensityWeightingFactors* k_factors = density_get_weighting_factors(status, DENSITY_K);
	const DensityWeightingFactorsTable* table = density_get_weighting_factors_table(status);

	assert(IS_SPECTRAL_DATA_VALID(spectral_data));
	assert(cmyk != NULL);

	cmyk[0] = calculate_density(spectral_data, table->c);
	cmyk[1] = calculate_density(spectral_data, table->m);
	cmyk[2] = calculate_density(spectral_data, table->y);
	cmyk[3] = calculate_density(spectral_data, k_factors);
}

float density_calculate_spot(const struct SpectralData* spectral_data, int* wavelength)
{
	int index = 0;
	float spot = 0.0;
	assert(IS_SPECTRAL_DATA_VALID(spectral_data));

	for (int i = 0; i < spectral_data->count; ++i)
	{
		float tmp = -log10f(spectral_data->data[i]);
		if (spot < tmp)
		{
			spot = tmp;
			index = i;
		}
	}

	if (wavelength)
	{
		*wavelength = spectral_data->start + index * spectral_data->interval;
	}
	return spot;
}

void density_calculate_density_spectrum(const SpectralData* spectral_data, float density_spectrum[])
{
	int i;
	assert(IS_SPECTRAL_DATA_VALID(spectral_data));
	assert(density_spectrum);

	for (i = 0; i < spectral_data->count; ++i)
	{
		density_spectrum[i] = -log10f(spectral_data->data[i]);
	}
}


float density_calculate_specific_spot(const struct SpectralData* spectral_data, int wavelength)
{
	assert(IS_SPECTRAL_DATA_VALID(spectral_data));
	assert(wavelength >= spectral_data->start &&
		(wavelength - spectral_data->start)/spectral_data->interval < spectral_data->count);

	return -log10f(spectral_data->data[(wavelength - spectral_data->start) / spectral_data->interval]);
}


float density_calculate_grayness(const float* cmy)
{
	int min_index, max_index, middle_index;

	assert(cmy != NULL);
	get_min_max_middle(cmy, &min_index, &max_index, &middle_index);
	return CALCULATE_GRAYNESS(cmy, min_index, max_index);
}

float density_calculate_hue_error(const float* cmy)
{
	int min_index, max_index, middle_index;

	assert(cmy != NULL);
	get_min_max_middle(cmy, &min_index, &max_index, &middle_index);
	return CALCULATE_HUEERROR(cmy, min_index, max_index, middle_index);
}

DensityColor density_get_auto_density(const float* cmyk)
{
	int min_index, max_index, middle_index;

	assert(cmyk != NULL);
	get_min_max_middle(cmyk, &min_index, &max_index, &middle_index);

	return GET_AUTO_DENSITY_COLOR(cmyk, min_index, max_index);
}

float density_calculate_tone_value_by_murray_davies(float paper_density, float solid_density, float tint_density)
{
	return (1.0f - powf(10.0f, -(tint_density - paper_density)))
		/ (1.0f - powf(10.0f, -(solid_density - paper_density)))
		* 100.0f;
}

float density_calculate_tone_value_by_yule_nielsen(float paper_density, float solid_density, float tint_density,
                                                   float n)
{
	assert(n > 0.0);
	return (1.0f - powf(10.0f, -(tint_density - paper_density) / n))
		/ (1.0f - powf(10.0f, -(solid_density - paper_density) / n))
		* 100.0f;
}

static float f(float u)
{
	if (u > powf(6.0f / 29.0f, 3))
		return powf(u, 1.0f / 3.0f);
	return 941.0f / 108.0f * u + 4.0f / 29.0f;
}

static float V(float x)
{
	return f(x) * 116.0f - 16.0f;
}

float density_calculate_tone_value_by_sctv(float paper_xyz[], float solid_xyz[], float tint_xyz[],
                                           clralgo_StandardIlluminant illuminant, clralgo_StandardObserver observer)
{
	clralgo_CIEXYZ white_point = clralgo_get_white_point_of_illuminant(observer, illuminant);

	// 纸张
	float Vxp = V(paper_xyz[0] / white_point.X);
	float Vyp = V(paper_xyz[1] / white_point.Y);
	float Vzp = V(paper_xyz[2] / white_point.Z);

	// 实地
	float Vxs = V(solid_xyz[0] / white_point.X);
	float Vys = V(solid_xyz[1] / white_point.Y);
	float Vzs = V(solid_xyz[2] / white_point.Z);

	// 当前
	float Vxt = V(tint_xyz[0] / white_point.X);
	float Vyt = V(tint_xyz[1] / white_point.Y);
	float Vzt = V(tint_xyz[2] / white_point.Z);

	return 100.0f * sqrtf((powf(Vxt - Vxp, 2.0f) + powf(Vyt - Vyp, 2.0f) +
			powf(Vzt - Vzp, 2.0f)) /
		(powf(Vxs - Vxp, 2.0f) + powf(Vys - Vyp, 2.0f)
			+ powf(Vzs - Vzp, 2.0f)));
}


float density_calculate_tone_value_by_sctv2(float paper_lab[], float solid_lab[], float tint_lab[],
	clralgo_StandardIlluminant illuminant, clralgo_StandardObserver observer)
{
	clralgo_CIEXYZ white_point = clralgo_get_white_point_of_illuminant(observer, illuminant);

	
	// 纸张
	float Vxp = paper_lab[0] + (116.0f * paper_lab[1])/500.f;
	float Vyp = paper_lab[0];
	float Vzp = paper_lab[0] - (116.0f * paper_lab[2])/200.f;

	// 实地
	float Vxs = solid_lab[0] + (116.0f * solid_lab[1])/500.f;
	float Vys = solid_lab[0];
	float Vzs = solid_lab[0] - (116.0f * solid_lab[2])/200.f;

	// 当前
	float Vxt = tint_lab[0] + (116.0f * tint_lab[1])/500.f;
	float Vyt = tint_lab[0];
	float Vzt = tint_lab[0] - (116.0f * tint_lab[2])/200.f;

	return 100.0f * sqrtf((powf(Vxt - Vxp, 2.0f) + powf(Vyt - Vyp, 2.0f) +
			powf(Vzt - Vzp, 2.0f)) /
		(powf(Vxs - Vxp, 2.0f) + powf(Vys - Vyp, 2.0f)
			+ powf(Vzs - Vzp, 2.0f)));
}


float density_calculate_trapping_by_preucil(float paper, float d1, float d2, float dop)
{
	float _d1 = d1 - paper;
	float _d2 = d2 - paper;
	float _dop = dop - paper;

	return (_dop - _d1) / _d2 * 100.0f;
}

float density_calculate_trapping_by_brunner(float paper, float d1, float d2, float dop)
{
	float _d1 = d1 - paper;
	float _d2 = d2 - paper;
	float _dop = dop - paper;

	return (1.0f - powf(10.0f, -_dop)) / (1.0f - powf(10.0f, -(_d1 + _d2))) * 100.0f;
}

float density_calculate_trapping_by_ritz(float paper, float d1, float d2, float dop)
{
	float _d1 = d1 - paper;
	float _d2 = d2 - paper;
	float _dop = dop - paper;

	return (1.0f - powf(10.0f, -(_dop - _d1))) / (1.0f - powf(10.0f, -_d2)) * 100.0f;
}

float density_calculate_contrast(float solid, float tint)
{
	return (solid - tint) / solid * 100.0f;
}
