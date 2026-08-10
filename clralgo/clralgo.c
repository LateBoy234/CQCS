#include "clralgo.h"
#include "cubicsplinecurve.h"
#include <stdlib.h>
#include<stdio.h>
#include <assert.h>
#include "util.h"
#include "tristimulus_weighting_factors.h"    // 将标准光源颜色匹配权重系数表包含进来

// 将XYZ转换为Yxy
// 参数：
// [in]  xyz:	要转换的XYZ值
// [out] yxy:	返回转换的Yxy值
void clralgo_xyz_to_yxy(const struct clralgo_CIEXYZ* xyz, struct clralgo_CIEYxy* yxy)
{
	float tmp;
	assert(xyz != NULL && yxy != NULL);

	tmp = xyz->X + xyz->Y + xyz->Z;
	if (tmp != 0.0f)
	{
		yxy->Y = xyz->Y;
		yxy->x = xyz->X / tmp;
		yxy->y = xyz->Y / tmp;
	}
	else
	{
		yxy->Y = yxy->x = yxy->y = 0.0f;
	}
}

// 将Yxy值转换为XYZ值
// 参数：
// [in]  yxy: 要转换的Yxy值
// [out] xyz: 存储转换后的结果
void clralgo_yxy_to_xyz(const struct clralgo_CIEYxy* yxy, struct clralgo_CIEXYZ* xyz)
{
	assert(xyz != NULL && yxy != NULL);

	if (yxy->Y != 0.0f && yxy->y != 0.0f)
	{
		float tmp = yxy->Y / yxy->y;
		xyz->X = tmp * yxy->x;
		xyz->Y = yxy->Y;
		xyz->Z = tmp * (1.0f - yxy->x - yxy->y);
	}
	else
	{
		xyz->X = xyz->Y = xyz->Z = 0.0f;
	}
}

// 将XYZ转换为BETAxy
// 参数：
// [in]  xyz:	要转换的XYZ值
// [out] BETAxy:	返回转换BETAxy
void clralgo_xyz_to_BETAxy(const struct clralgo_CIEXYZ* xyz, struct clralgo_BETAxy* BETAxy)
{
	float tmp;
	assert(xyz != NULL && BETAxy != NULL);

	tmp = xyz->X + xyz->Y + xyz->Z;
	if (tmp != 0.0f) {
		BETAxy->BETA = xyz->Y / 100.0f;
		BETAxy->x = xyz->X / tmp;
		BETAxy->y = xyz->Y / tmp;
	}
	else {
		BETAxy->BETA = BETAxy->x = BETAxy->y = 0.0f;
	}
}

// 将BETAxy值转换为XYZ值
// 参数：
// [in]  BETAxy: 要转换的BETAxy值
// [out] xyz: 存储转换后的结果
void clralgo_BETAxy_to_xyz(const struct clralgo_BETAxy* BETAxy, struct clralgo_CIEXYZ* xyz)
{
	assert(xyz != NULL && BETAxy != NULL);

	if (BETAxy->BETA != 0.0f && BETAxy->y != 0.0f) {
		float tmp = BETAxy->BETA / BETAxy->y;
		xyz->X = tmp * BETAxy->x;
		xyz->Y = BETAxy->BETA;
		xyz->Z = tmp * (1.0f - BETAxy->x - BETAxy->y);
	}
	else {
		xyz->X = xyz->Y = xyz->Z = 0.0f;
	}
}

// 将Lab值转换为LCh值
// 参数：
// [in]  lab: 要转换的Lab值
// [out] lch: 存储转换的结果
void clralgo_lab_to_lch(const struct clralgo_CIELab* lab, struct clralgo_CIELCh* lch)
{
	assert(lab != NULL && lch != NULL);

	lch->L = lab->L;
	lch->C = sqrtf(lab->a * lab->a + lab->b * lab->b);
	lch->h = (float)DEGREES(my_atan2(lab->b, lab->a));
	if (lch->h < 0)
		lch->h += 360.0f;
}


// 将LCh值转换为lab值
// [in]  lch: 要转换的LCh值
// [out] lab: 存储转换后的结果
void clralgo_lch_to_lab(const struct clralgo_CIELCh* lch, struct clralgo_CIELab* lab)
{
	assert(lch != NULL && lab != NULL);

	lab->L = lch->L;
	lab->a = lch->C * cosf((float)RADIANS(lch->h));
	lab->b = lch->C * sinf((float)RADIANS(lch->h));
}

// 使用三参样条曲线插值算法插值
// 参数：
// [in] in_x_range: 输入的点X方向上的坐标，固定间隔
// [in] in_y_data:  输入的点Y方向上的坐标
// [in] out_x_range: 输出点的X方向坐标的范围
// [out] out_y_data: 返回插值结果
void clralgo_interpolate(const struct clralgo_WavelengthRange* in_x_range, const float* y,
	const struct clralgo_WavelengthRange* out_x_range, float* out_y_data)
{
	int i;

	if (in_x_range->start == out_x_range->start &&
		in_x_range->interval == out_x_range->interval &&
		in_x_range->count == out_x_range->count)
	{
		for (i = 0; i < out_x_range->count; i++)
			out_y_data[i] = y[i];
	}
	else
	{
		float* x = (float*)malloc(sizeof(float) * in_x_range->count);
		CubicSplineCurve* curve = NULL;

		for (i = 0; i < in_x_range->count; i++)
		{
			x[i] = (float)(in_x_range->start + i * in_x_range->interval);
		}

		curve = CubicSplineCurve_Simulate_f(in_x_range->count, x, y, 2, 0, 0);
		//assert(curve != NULL);
		if (curve == NULL) {
			free(x);
			return;
		}

		for (i = 0; i < out_x_range->count; i++)
		{
			out_y_data[i] = (float)CubicSplineCurve_GetItem_f(curve, (float)(out_x_range->start + i * out_x_range->interval));
		}

		free(x);
		CubicSplineCurve_Destruct(curve);
	}
}

// 使用三参样条曲线插值算法插值
// 参数：
// [in] in_x_data: 输入的点X方向上的坐标
// [in] in_y_data:  输入的点Y方向上的坐标
// [in] out_x_range: 输出点的X方向坐标的范围
// [out] out_y_data: 返回插值结果
void clralgo_interpolate2(uint32_t count, const float* x, const float* y,
	const struct clralgo_WavelengthRange* out_x_range, float* out_y_data)
{
	int i;
	CubicSplineCurve* curve = CubicSplineCurve_Simulate_f(count, x, y, 2, 0, 0);
	assert(curve != NULL);

	for (i = 0; i < out_x_range->count; i++)
	{
		out_y_data[i] = CubicSplineCurve_GetItem_f(curve, (float)(out_x_range->start + i * out_x_range->interval));
	}

	CubicSplineCurve_Destruct(curve);
}

// 计算光谱的三刺激值
static void calculate_xyz(const float factor_table[][3], int interval, const struct clralgo_Spectra* spectra, struct clralgo_CIEXYZ* xyz)
{
	float buffer[85];
	double X, Y, Z;
	int i;
	struct clralgo_WavelengthRange out_range = { 360, interval, (780 - 360) / interval + 1 };

	//assert(spectra->range.start <= 400 && spectra->range.start % 10 == 0);
	//assert(interval == 5 || interval == 10);

	// 如果光谱波长间隔包含光源色匹配权重因子的间隔，直接将其380~780之间的光谱拷贝到buffer中
	// 否则将其插值成间隔为interval的反射率

	if (interval % spectra->range.interval == 0)
	{
		int start_wavelength = spectra->range.start;
		int end_wavelength = clralgo_WavelengthRange_end(spectra->range);

		for (i = 360; i <= 780; i += interval)
		{
			if (i < start_wavelength)
				buffer[(i - 360) / interval] = spectra->data[0];
			else if (i > end_wavelength)
				buffer[(i - 360) / interval] = spectra->data[spectra->range.count - 1];
			else
				buffer[(i - 360) / interval] = clralgo_Spectra_get(spectra, i);
		}
	}
	else
	{
		clralgo_interpolate(&spectra->range, spectra->data, &out_range, buffer);
	}

	X = Y = Z = 0.0;
	for (i = 0; i < out_range.count; i++)
	{
		X += factor_table[i][0] * buffer[i];
		Y += factor_table[i][1] * buffer[i];
		Z += factor_table[i][2] * buffer[i];
	}

	xyz->X = (float)X;
	xyz->Y = (float)Y;
	xyz->Z = (float)Z;
}


// 如果是F光源返回非零值
static int is_Fn(enum clralgo_StandardIlluminant illuminant)
{
	return illuminant >= clralgo_F1 && illuminant <= clralgo_F12;
}

// 通过光谱计算XYZ值
// 参数：
// [in]	 spectra:		反射率或透射率数据
// [in]  illuminant:	要计算的XYZ所在光源
// [in]  observer:		要计算的XYZ的所在观察者角度
// [out] xyz:			存储计算结果
void clralgo_calculate_xyz(const struct clralgo_Spectra* spectra,
	enum clralgo_StandardIlluminant illuminant,
	enum clralgo_StandardObserver observer,
	struct clralgo_CIEXYZ* xyz)
{
	if (5 % spectra->range.interval == 0 || is_Fn(illuminant))
	{
		calculate_xyz(TRISTIMULUS_WEIGHTING_WEIGHTING_FACTORS_5NM_TABLEs[observer][illuminant]->factors,
			5, spectra, xyz);
	}
	else
	{
		calculate_xyz(TRISTIMULUS_WEIGHTING_WEIGHTING_FACTORS_10NM_TABLEs[observer][illuminant]->factors,
			10, spectra, xyz);
	}
}


// 通过光谱计算Lab值
// 参数：
// [in]	 spectra:		反射率或透射率数据
// [in]  illuminant:	要计算的XYZ所在光源
// [in]  observer:		要计算的XYZ的所在观察者角度
// [out] lab:			存储计算结果
void clralgo_calculate_lab(const struct clralgo_Spectra* spectra,
	enum clralgo_StandardIlluminant illuminant,
	enum clralgo_StandardObserver observer,
	struct clralgo_CIELab* lab)
{
	struct clralgo_CIEXYZ xyz;

	clralgo_calculate_xyz(spectra, illuminant, observer, &xyz);
	clralgo_xyz_to_lab(illuminant, observer, &xyz, lab);
}


static float f(float a)
{
	if (a > powf(24.0f / 116.0f, 3))
		return powf(a, 1.0f / 3.0f);
	else
		return 841.0f / 108.0f * a + 16.0f / 116.0f;
}

static float f_inv(float x)
{
	if (x > 24.0f / 116.0f)
		return powf(x, 3);
	else
		return ((x - 16.0f / 116.0f) / (841.0f / 108.0f));
}

// 将XYZ转换为Lab颜色空间
// 参数：
// [in]  illuminant:	XYZ值所在光源
// [in]  observer:		XYZ值所在观察者角度
// [in]  xyz:			要转换的XYZ值
// [out] lab:			存储转换结果
void clralgo_xyz_to_lab(enum clralgo_StandardIlluminant illuminant,
	enum clralgo_StandardObserver observer,
	const struct clralgo_CIEXYZ* xyz,
	struct clralgo_CIELab* lab)
{
	if (xyz->X + xyz->Y + xyz->Z == 0.0f)
	{
		lab->L = lab->a = lab->b = 0.0f;
	}
	else
	{
		struct clralgo_CIEXYZ xyz0 = clralgo_get_white_point_of_illuminant(observer, illuminant);

		float f_Y = f(xyz->Y / xyz0.Y);
		float f_X = f(xyz->X / xyz0.X);
		float f_Z = f(xyz->Z / xyz0.Z);

		lab->L = 116.0f * f_Y - 16.0f;
		lab->a = 500.0f * (f_X - f_Y);
		lab->b = 200.0f * (f_Y - f_Z);
	}
}

// 将Lab转换为XYZ颜色空间
// 参数：
// [in]  illuminant:	Lab值所在光源
// [in]  observer:		Lab值所在观察者角度
// [in]  lab:			要转换的Lab值
// [out] xyz:			存储转换结果
void clralgo_lab_to_xyz(enum clralgo_StandardIlluminant illuminant,
	enum clralgo_StandardObserver observer,
	const struct clralgo_CIELab* lab, struct clralgo_CIEXYZ* xyz)
{
	if (lab->L == 0.0f && lab->a == 0.0f && lab->b == 0.0f)
	{
		xyz->X = xyz->Y = xyz->Z = 0.0f;
	}
	else
	{
		struct clralgo_CIEXYZ xyz0 = clralgo_get_white_point_of_illuminant(observer, illuminant);

		float f_Y_Yn = (lab->L + 16.0f) / 116.0f;  // f(Y/Yn)
		float Y_Yn = f_inv(f_Y_Yn); // Y/Yn
		float X_Xn = f_inv(lab->a / 500.0f + f_Y_Yn); // X/Xn
		float Z_Zn = f_inv(f_Y_Yn - lab->b / 200.0f); // Z/Zn
		xyz->X = xyz0.X * X_Xn;
		xyz->Y = xyz0.Y * Y_Yn;
		xyz->Z = xyz0.Z * Z_Zn;
	}
}

// 获取HunterLab和CIEXYZ互相转换的系数
static void get_KaKb(enum clralgo_StandardIlluminant illuminant,
	enum clralgo_StandardObserver observer,
	float* Ka,
	float* Kb)
{
	if (observer == clralgo_DEGREE_2)
	{
		switch (illuminant)
		{
		case clralgo_A:
			*Ka = 185.20f;
			*Kb = 38.40f;
			break;
		case clralgo_C:
			*Ka = 175.00f;
			*Kb = 70.00f;
			break;
		case clralgo_D50:
			*Ka = 173.51f;
			*Kb = 58.48f;
			break;
		case clralgo_D65:
			*Ka = 172.30f;
			*Kb = 67.20f;
			break;
		case clralgo_D75:
			*Ka = 172.22f;
			*Kb = 71.30f;
			break;
		case clralgo_F2:
			*Ka = 175.00f;
			*Kb = 52.90f;
			break;
		case clralgo_F11:
			*Ka = 178.00f;
			*Kb = 52.30f;
			break;
		case clralgo_F12:
			*Ka = 183.70f;
			*Kb = 37.50f;
			break;
		default:
		{
			struct clralgo_CIEXYZ xyz = clralgo_get_white_point_of_illuminant(observer, illuminant);
			*Ka = 175.0f / 198.04f * (xyz.X + xyz.Y);
			*Kb = 70.0f / 218.11f * (xyz.Y + xyz.Z);
		}
		break;
		}
	}
	else
	{
		switch (illuminant)
		{
		case clralgo_A:
			*Ka = 186.30f;
			*Kb = 38.20f;
			break;
		case clralgo_C:
			*Ka = 174.30f;
			*Kb = 69.40f;
			break;
		case clralgo_D50:
			*Ka = 173.82f;
			*Kb = 58.13f;
			break;
		case clralgo_D65:
			*Ka = 172.10f;
			*Kb = 66.70f;
			break;
		case clralgo_D75:
			*Ka = 171.76f;
			*Kb = 70.76f;
			break;
		case clralgo_F2:
			*Ka = 178.60f;
			*Kb = 53.60f;
			break;
		case clralgo_F11:
			*Ka = 180.10f;
			*Kb = 52.70f;
			break;
		case clralgo_F12:
			*Ka = 186.30f;
			*Kb = 38.20f;
			break;
		default:
		{
			struct clralgo_CIEXYZ xyz = clralgo_get_white_point_of_illuminant(observer, illuminant);
			*Ka = 175.0f / 198.04f * (xyz.X + xyz.Y);
			*Kb = 70.0f / 218.11f * (xyz.Y + xyz.Z);
		}
		break;
		}
	}
}

// 将XYZ转换为Hunter Lab颜色空间
// 参数：
// [in]  illuminant:	XYZ值所在光源
// [in]  observer:		XYZ值所在观察者角度
// [in]  xyz:			要转换的XYZ值
// [out] lab:			存储转换结果
void clralgo_xyz_to_hunterlab(enum clralgo_StandardIlluminant illuminant,
	enum clralgo_StandardObserver observer,
	const struct clralgo_CIEXYZ* xyz,
	struct clralgo_HunterLab* lab)
{
	if (xyz->X + xyz->Y + xyz->Z == 0.0f)
	{
		lab->L = lab->a = lab->b = 0.0f;
	}
	else
	{
		float Ka, Kb, sqrt_Y_Yn;
		const struct clralgo_CIEXYZ xyz0 = clralgo_get_white_point_of_illuminant(observer, illuminant);

		get_KaKb(illuminant, observer, &Ka, &Kb);

		sqrt_Y_Yn = sqrtf(xyz->Y / xyz0.Y);
		lab->L = 100.f * sqrt_Y_Yn;
		lab->a = Ka * ((xyz->X / xyz0.X) / sqrt_Y_Yn - sqrt_Y_Yn);
		lab->b = Kb * (sqrt_Y_Yn - (xyz->Z / xyz0.Z) / sqrt_Y_Yn);
	}
}

// 将Hunter Lab转换为XYZ颜色空间
// 参数：
// [in]  illuminant:	Hunter Lab值所在光源
// [in]  observer:		Hunter Lab值所在观察者角度
// [in]  lab:			要转换的Hunter Lab值
// [out] xyz:			存储转换结果
void clralgo_hunterlab_to_xyz(enum clralgo_StandardIlluminant illuminant,
	enum clralgo_StandardObserver observer,
	const struct clralgo_HunterLab* lab, struct clralgo_CIEXYZ* xyz)
{
	float Ka, Kb;
	const struct clralgo_CIEXYZ xyz0 = clralgo_get_white_point_of_illuminant(observer, illuminant);
	float sqrt_Y_Yn = lab->L / 100.0f; // sqrt(Y/Yn)
	float Y_Yn = sqrt_Y_Yn * sqrt_Y_Yn; // Y/Yn

	get_KaKb(illuminant, observer, &Ka, &Kb);

	xyz->Y = Y_Yn * xyz0.Y;
	xyz->X = (((lab->a / Ka) + sqrt_Y_Yn) * sqrt_Y_Yn) * xyz0.X;
	xyz->Z = ((sqrt_Y_Yn - (lab->b / Kb)) * sqrt_Y_Yn) * xyz0.Z;
}

// 计算u'的公式
static float uq(const struct clralgo_CIEXYZ* xyz)
{
	return 4.0f * xyz->X / (xyz->X + 15.0f * xyz->Y + 3 * xyz->Z);
}

// 计算v'的公式
static float vq(const struct clralgo_CIEXYZ* xyz)
{
	return 9.0f * xyz->Y / (xyz->X + 15.0f * xyz->Y + 3 * xyz->Z);
}

// 将XYZ转换为Luv颜色空间
// 参数：
// [in]  illuminant:	XYZ值所在光源
// [in]  observer:		XYZ值所在观察者角度
// [in]  xyz:			要转换的XYZ值
// [out] luv:			存储转换结果
void clralgo_xyz_to_luv(enum clralgo_StandardIlluminant illuminant,
	enum clralgo_StandardObserver observer,
	const struct clralgo_CIEXYZ* xyz, struct clralgo_CIELuv* luv)
{
	if (xyz->X + xyz->Y + xyz->Z == 0.0f)
	{
		luv->L = luv->u = luv->v = 0.0f;
	}
	else
	{
		const struct clralgo_CIEXYZ xyz0 = clralgo_get_white_point_of_illuminant(observer, illuminant);

		luv->L = 116.0f * f(xyz->Y / xyz0.Y) - 16.0f;
		luv->u = 13.0f * luv->L * (uq(xyz) - uq(&xyz0));
		luv->v = 13.0f * luv->L * (vq(xyz) - vq(&xyz0));
		luv->uq = uq(xyz);
		luv->vq = vq(xyz);
	}
}

// 将Luv转换为XYZ颜色空间
// 参数：
// [in]  illuminant:	Luv值所在光源
// [in]  observer:		Luv值所在观察者角度
// [in]  luv:			要转换的Luv值
// [out] xyz:			存储转换结果
void clralgo_luv_to_xyz(enum clralgo_StandardIlluminant illuminant,
	enum clralgo_StandardObserver observer,
	const struct clralgo_CIELuv* luv, struct clralgo_CIEXYZ* xyz)
{
	if (luv->L == 0.0f)
	{
		xyz->X = xyz->Y = xyz->Z = 0.0f;
	}
	else
	{
		const struct clralgo_CIEXYZ xyz0 = clralgo_get_white_point_of_illuminant(observer, illuminant);
		float Y_Yn = f_inv((luv->L + 16.0f) / 116.0f); // Y/Yn
		float u = luv->u / luv->L / 13.0f + uq(&xyz0); // u'
		float v = luv->v / luv->L / 13.0f + vq(&xyz0); // v'
		float tmp;

		xyz->Y = xyz0.Y * Y_Yn;
		tmp = 9.0f * xyz->Y / v;        // (xyz->X + 15.0 * xyz->Y + 3 * xyz->Z)
		xyz->X = u * tmp / 4.0f;
		xyz->Z = (tmp - xyz->X - 15.0f * xyz->Y) / 3.0f;
	}
}

// 将XYZ转换为CIE RGB颜色空间
// 参数：
// [in]  xyz:			要转换的XYZ值
// [out] rgb:			存储转换结果
void clralgo_xyz_to_ciergb(const struct clralgo_CIEXYZ* xyz, struct clralgo_CIERGB* rgb)
{
	const double mat[3][3] = {
		{0.41845516, -0.15865714, -0.08283193},
		{-0.09116486,  0.25242564,  0.01570531},
		{ 0.00092086, -0.00254975,  0.17859499}
	};

	rgb->R = (float)(mat[0][0] * xyz->X + mat[0][1] * xyz->Y + mat[0][2] * xyz->Z);
	rgb->G = (float)(mat[1][0] * xyz->X + mat[1][1] * xyz->Y + mat[1][2] * xyz->Z);
	rgb->B = (float)(mat[2][0] * xyz->X + mat[2][1] * xyz->Y + mat[2][2] * xyz->Z);
}

// 将CIE RGB转换为XYZ颜色空间
// 参数：
// [in]  rgb:			要转换的CIE RGB值
// [out] xyz:			存储转换结果
void clralgo_ciergb_to_xyz(const struct clralgo_CIERGB* rgb, struct clralgo_CIEXYZ* xyz)
{
	const double mat[3][3] =
	{
		{2.768892, 1.751748, 1.130160},
		{1.000000, 4.590700, 0.060100},
		{0.000000, 0.056508, 5.594292}
	};
	xyz->X = (float)(rgb->R * mat[0][0] + rgb->G * mat[0][1] + rgb->B * mat[0][2]);
	xyz->Y = (float)(rgb->R * mat[1][0] + rgb->G * mat[1][1] + rgb->B * mat[1][2]);
	xyz->Z = (float)(rgb->R * mat[2][0] + rgb->G * mat[2][1] + rgb->B * mat[2][2]);
}

static double srgb_convert(const double CL[], const struct clralgo_CIEXYZ* xyz)
{
	double tmp = CL[0] * xyz->X + CL[1] * xyz->Y + CL[2] * xyz->Z;
	tmp = tmp / 100.0;

	if (tmp * 12.92 <= 0.04045)
		tmp = tmp * 12.92;
	else
		tmp = 1.055 * pow(tmp, 1.0 / 2.4) - 0.055;
	if (tmp < 0.0) tmp = 0.0;
	if (tmp > 1.0) tmp = 1.0;
	return tmp;
}


// 将CIELab转换为sRGB颜色空间
// 参数：
// [in]  lab:			要转换的lab值
// [out] srgb:			存储转换结果
void clralgo_lab_to_srgb(const struct clralgo_CIELab* lab,
	struct clralgo_sRGB* rgb)
{
	// 线性变换矩阵

	const double mat[3][3] = { //2025-10-27
		   {3.2406255, -1.5372080, -0.4986286},
		   {-0.9689307, 1.8757561, 0.0415175},
		   {0.0557101, -0.2040211, 1.0569959}
	};
	struct clralgo_CIEXYZ xyz;

	clralgo_lab_to_xyz(clralgo_D65, clralgo_DEGREE_10, lab, &xyz);

	rgb->R = (float)srgb_convert(mat[0], &xyz);
	rgb->G = (float)srgb_convert(mat[1], &xyz);
	rgb->B = (float)srgb_convert(mat[2], &xyz);
}


// 将XYZ转换为sRGB颜色空间
// 参数：
// [in]  illuminant:	XYZ值所在光源
// [in]  observer:		XYZ值所在观察者角度
// [in]  xyz:			要转换的XYZ值
// [out] srgb:			存储转换结果
void clralgo_xyz_to_srgb(enum clralgo_StandardIlluminant illuminant,
	enum clralgo_StandardObserver observer,
	const struct clralgo_CIEXYZ* xyz, struct clralgo_sRGB* rgb)
{

	struct clralgo_CIELab lab;
	clralgo_xyz_to_lab(illuminant, observer, xyz, &lab);
	clralgo_lab_to_srgb(&lab, rgb);
}

// 计算lch1 - lch2的ΔL*、ΔC*、ΔH*
// 参数：
// [in] lch1: 样品1的LCh值
// [in] lch2: 样品2的LCh值
// [out] dL, dC, dH: 用于存储 ΔL*、ΔC*、ΔH*
void clralgo_calculate_dLCH(const struct clralgo_CIELCh* lch1,
	const struct clralgo_CIELCh* lch2,
	float* dL, float* dC, float* dH)
{
	float dh;

	assert(lch1 != NULL && lch2 != NULL);
	assert(dL != NULL && dC != NULL && dH != NULL);

	*dL = lch1->L - lch2->L;
	*dC = lch1->C - lch2->C;
	dh = lch1->h - lch2->h;
	if (dh > 180.0f)
		dh -= 360.0f;
	else if (dh < -180.0f)
		dh += 360.0f;
	*dH = 2.0f * sqrtf(lch1->C * lch2->C) * sinf(RADIANS(dh / 2.0f));
}

// 使用ΔE*ab计算lab1 - lab2的色差
// 参数：
// [in] lab1: 样品1的Lab值
// [in] lab2: 样品2的Lab值
// 返回：ΔE*ab
float clralgo_calculate_dEab(const struct clralgo_CIELab* lab1,
	const struct clralgo_CIELab* lab2)
{
	float dL = lab1->L - lab2->L;
	float da = lab1->a - lab2->a;
	float db = lab1->b - lab2->b;

	return sqrtf(dL * dL + da * da + db * db);
}

// 使用CIE 1994色差公式计算lab1 - lab2的色差
// 参数：
// [in] lab1: 样品1的Lab值
// [in] lab2: 样品2的Lab值
// [in] KL, KC, KH: 参数因子，一盘使用默认的1:1:1
// [out, opt] dL : 如果不为NULL存储ΔL'94
// [out, opt] dC : 如果不为NULL存储ΔC'94
// [out, opt] dH : 如果不为NULL存储ΔH'94
// 返回：ΔE*94
float clralgo_calculate_CIEDE94(const struct clralgo_CIELab* lab1,
	const struct clralgo_CIELab* lab2,
	float KL, float KC, float KH,
	float* dL, float* dC, float* dH)
{
	struct clralgo_CIELCh lch1, lch2;
	float _dL, _dC, _dH, SL, SC, SH;

	clralgo_lab_to_lch(lab1, &lch1);
	clralgo_lab_to_lch(lab2, &lch2);
	clralgo_calculate_dLCH(&lch1, &lch2, &_dL, &_dC, &_dH);

	SL = 1.0f;
	SC = 1.0f + 0.045f * lch2.C;
	SH = 1.0f + 0.015f * lch2.C;

	_dL = _dL / (KL * SL);
	_dC = _dC / (KC * SC);
	_dH = _dH / (KH * SH);

	if (dL != NULL)
		*dL = _dL;
	if (dC != NULL)
		*dC = _dC;
	if (dH != NULL)
		*dH = _dH;

	return sqrtf(_dL * _dL + _dC * _dC + _dH * _dH);
}

// 使用CIE 2000色差公式计算lab1 - lab2的色差
// 参数：
// [in] lab1: 样品1的Lab值
// [in] lab2: 样品2的Lab值
// [in] KL, KC, KH: 参数因子，一盘使用默认的1:1:1
// [out, opt] dL : 如果不为NULL存储ΔL'00
// [out, opt] dC : 如果不为NULL存储ΔC'00
// [out, opt] dH : 如果不为NULL存储ΔH'00
// 返回：ΔE*00
float clralgo_calculate_CIEDE00(const struct clralgo_CIELab* lab1,
	const struct clralgo_CIELab* lab2,
	float KL, float KC, float KH,
	float* dL, float* dC, float* dH)
{
	struct clralgo_CIELCh lch1, lch2, _lch1, _lch2;
	struct clralgo_CIELab _lab1, _lab2;
	float _dL, _dC, _dH;
	float CC, G, L, C, h;
	float SL, SC, SH, Rt;
	float T, dTheta, Rc;

	clralgo_lab_to_lch(lab1, &lch1);
	clralgo_lab_to_lch(lab2, &lch2);

	CC = (lch1.C + lch2.C) / 2.0f;
	G = 0.5f * (1.0f - sqrtf(powf(CC, 7) /
		(powf(CC, 7) + powf(25.0f, 7))));

	_lab1.L = lab1->L;
	_lab1.a = lab1->a * (1.0f + G);
	_lab1.b = lab1->b;

	_lab2.L = lab2->L;
	_lab2.a = lab2->a * (1.0f + G);
	_lab2.b = lab2->b;

	clralgo_lab_to_lch(&_lab1, &_lch1);
	clralgo_lab_to_lch(&_lab2, &_lch2);
	clralgo_calculate_dLCH(&_lch1, &_lch2, &_dL, &_dC, &_dH);

	L = (_lch1.L + _lch2.L) / 2.0f;
	C = (_lch1.C + _lch2.C) / 2.0f;
	h = average_degree(_lch1.h, _lch2.h);

	SL = 1.0f + 0.015f * powf(L - 50.0f, 2) / sqrtf(20.0f + powf(L - 50.0f, 2));
	SC = 1.0f + 0.045f * C;
	T = 1.0f - 0.17f * cosf(RADIANS(h - 30.0f)) + 0.24f * cosf(RADIANS(2.0f * h))
		+ 0.32f * cosf(RADIANS(3.0f * h + 6.0f)) - 0.20f * cosf(RADIANS(4.0f * h - 63.0f));
	SH = 1.0f + 0.015f * C * T;

	Rc = 2.0f * sqrtf(powf(C, 7) / (powf(C, 7) + powf(25.0f, 7)));
	dTheta = 30.0f * expf(-powf((h - 275.0f) / 25.0f, 2));
	Rt = -sinf(RADIANS(2.0f * dTheta)) * Rc;

	_dL = _dL / (KL * SL);
	_dC = _dC / (KC * SC);
	_dH = _dH / (KH * SH);

	if (dL != NULL)
		*dL = _dL;
	if (dC != NULL)
		*dC = _dC;
	if (dH != NULL)
		*dH = _dH;

	return sqrtf(_dL * _dL + _dC * _dC + _dH * _dH + Rt * _dC * _dH);
}

// 使用CMC(l:c)色差公式计算lab1 - lab2的色差
// 参数：
// [in] lab1: 样品1的Lab值
// [in] lab2: 样品2的Lab值
// [in] l, c: 参数因子，一盘使用默认的2:1
// [out, opt] dL : 如果不为NULL存储ΔL'cmc(l:c)
// [out, opt] dC : 如果不为NULL存储ΔC'cmc(l:c)
// [out, opt] dH : 如果不为NULL存储ΔH'cmc(l:c)
// 返回：CMC(KL:KC)
float clralgo_calculate_CMC(const struct clralgo_CIELab* lab1,
	const struct clralgo_CIELab* lab2,
	float l, float c,
	float* dL, float* dC, float* dH)
{
	struct clralgo_CIELCh lch1, lch2;
	float _dL, _dC, _dH, SL, SC, SH, f, T;

	clralgo_lab_to_lch(lab1, &lch1);
	clralgo_lab_to_lch(lab2, &lch2);
	clralgo_calculate_dLCH(&lch1, &lch2, &_dL, &_dC, &_dH);

	SL = (lch2.L < 16.0f) ?
		0.511f : (0.040975f * lch2.L / (1.0f + 0.01765f * lch2.L));
	SC = 0.0638f * lch2.C / (1.0f + 0.0131f * lch2.C) + 0.638f;
	f = sqrtf(powf(lch2.C, 4) / (powf(lch2.C, 4) + 1900.0f));
	T = (164.0f < lch2.h && lch2.h < 345.0f) ?
		(0.56f + fabsf(0.2f * cosf(RADIANS(lch2.h + 168.0f)))) :
		(0.36f + fabsf(0.4f * cosf(RADIANS(lch2.h + 35.0f))));
	SH = SC * (T * f + 1.0f - f);

	_dL = _dL / (l * SL);
	_dC = _dC / (c * SC);
	_dH = _dH / SH;

	if (dL != NULL)
		*dL = _dL;
	if (dC != NULL)
		*dC = _dC;
	if (dH != NULL)
		*dH = _dH;

	return sqrtf(_dL * _dL + _dC * _dC + _dH * _dH);
}

// 计算Hunter Lab的色差ΔE
// 参数：
// [in] lab1: 样品1的Hunter Lab值
// [in] lab2: 样品2的Hunter Lab值
// 返回：ΔE
float clralgo_calculate_hunter_dE(const struct clralgo_HunterLab* lab1,
	const struct clralgo_HunterLab* lab2)
{
	float dL = lab1->L - lab2->L;
	float da = lab1->a - lab2->a;
	float db = lab1->b - lab2->b;

	return sqrtf(dL * dL + da * da + db * db);
}

// 计算Luv的色差ΔE*uv
// 参数：
// [in] luv1: 样品1的CIELuv值
// [in] luv2: 样品2的CIELuv值
// 返回：ΔE*uv
float clralgo_calcuate_dEuv(const struct clralgo_CIELuv* luv1,
	const struct clralgo_CIELuv* luv2)
{
	float dL = luv1->L - luv2->L;
	float du = luv1->u - luv2->u;
	float dv = luv1->v - luv2->v;

	return sqrtf(dL * dL + du * du + dv * dv);
}

//////////////////////////////////////////////////////////////////////////
// 颜色指数相关函数

// 计算ASTM E313-2005白度指数
// 参数：
// [in] illuminant:		xyz所在光源
// [in] observer:		xyz所在观察者角度
// [in] xyz:			要计算其白度指数的XYZ的值
// [out, opt] T:     如果不为空，存储Tint Index
// 返回：如果光源和观察者角度有效返回白度指数，否则返回NAN
float clralgo_calculate_whiteness_astm_e313_2005(enum clralgo_StandardIlluminant illuminant,
	enum clralgo_StandardObserver observer,
	const struct clralgo_CIEYxy* yxy,
	float* T)
{
	if (illuminant == clralgo_C || illuminant == clralgo_D50 || illuminant == clralgo_D65)
	{
		const float Tx = (observer == clralgo_DEGREE_2) ? 1000.0f : 900.0f;
		const float Ty = 650.0f;
		float WI, _T;
		struct clralgo_CIEXYZ xyz0 = clralgo_get_white_point_of_illuminant(observer, illuminant);
		struct clralgo_CIEYxy yxy0;

		clralgo_xyz_to_yxy(&xyz0, &yxy0);

		WI = yxy->Y + 800.0f * (yxy0.x - yxy->x) + 1700.0f * (yxy0.y - yxy->y);
		_T = Tx * (yxy0.x - yxy->x) - Ty * (yxy0.y - yxy->y);

		if (T != NULL)
		{
			*T = _T;
		}

		///仪器上已放开限制 ASTM E313
		//if (40.0f < WI && WI < (5.0f * yxy->Y - 280.f) && -3.0 < _T && _T < 3.0f)
		//{
		//	return WI;
		//}
		return WI;

	}
	else
	{
		if (T != NULL)
		{
			*T = clralgo_FLOAT_NAN;
		}
	}
	return clralgo_FLOAT_NAN;
}

// 计算WI(CIE/ISO)：符合标准：ISO 11475，CIE 86，甘茨白度公式，AATCC 110
// 注意：该公式仅在D65、10度下有效
// [in] illuminant:		xyz所在光源
// [in] observer:		xyz所在观察者角度
// [in] xyz:			要计算其白度指数的XYZ的值
// [out, opt] T:     如果不为空，存储Tint Index
// 返回：如果光源和观察者角度有效返回白度指数，否则返回NAN
float clralgo_calculate_whiteness_cie_iso(
	enum clralgo_StandardIlluminant illuminant,
	enum clralgo_StandardObserver observer,
	const struct clralgo_CIEYxy* yxy, float* T)
{
	if (illuminant == clralgo_D65 && observer == clralgo_DEGREE_10)
	{
		return clralgo_calculate_whiteness_astm_e313_2005(illuminant, observer, yxy, T);
	}
	if (T != NULL)
		*T = clralgo_FLOAT_NAN;
	return clralgo_FLOAT_NAN;
}

// 获取X、Y、Z与A、G、B之间的变换系数
static int get_AGB(enum clralgo_StandardIlluminant illuminant,
	enum clralgo_StandardObserver observer,
	float* fXA, float* fXB, float* fZB)
{
	const float cie1937[][3] = {
		{ 1.0447f, 0.0539f, 0.3558f },  // A
		{ 0.8061f, 0.1504f, 0.9209f },	 // D55
		{ 0.7701f, 0.1804f, 1.0889f },	 // D65
		{ 0.7446f, 0.2047f, 1.2256f },	 // D75
		{ 0.7832f, 0.1975f, 1.1822f }	 // C
	};
	const float cie1964[][3] = {
		{ 1.0571f, 0.0544f, 0.3520f },  // A
		{ 0.8078f, 0.1502f, 0.9098f },  // D55
		{ 0.7683f, 0.1798f, 1.0733f },  // D65
		{ 0.7405f, 0.2038f, 1.2073f },  // D75
		{ 0.7772f, 0.1957f, 1.1614f }   // C
	};

	int index = 0;
	switch (illuminant) {
	case clralgo_A: index = 0; break;
	case clralgo_D55: index = 1; break;
	case  clralgo_D65: index = 2; break;
	case clralgo_D75: index = 3; break;
	case clralgo_C: index = 4; break;
	default: return 0;
	}

	if (observer == clralgo_DEGREE_2)
	{
		*fXA = cie1937[index][0];
		*fXB = cie1937[index][1];
		*fZB = cie1937[index][2];
	}
	else
	{
		*fXA = cie1964[index][0];
		*fXB = cie1964[index][1];
		*fZB = cie1964[index][2];
	}
	return 1;
}

// 计算Tappl（坦伯）白度指数
// 参数：
// [in] illuminant:		xyz所在光源
// [in] observer:		xyz所在观察者角度
// [in] xyz:			要计算其白度指数的XYZ的值
// 返回：如果光源和观察者角度有效返回白度指数，否则返回NAN
float clralgo_calculate_whiteness_tappl(enum clralgo_StandardIlluminant illuminant,
	enum clralgo_StandardObserver observer,
	const struct clralgo_CIEXYZ* xyz)
{
	float fXA, fXB, fZB;

	if (get_AGB(illuminant, observer, &fXA, &fXB, &fZB))
	{
		return 1.0f / fZB * xyz->Z;
	}

	return clralgo_FLOAT_NAN;
}

// 计算Taube（陶贝）白度指数
// 参数：
// [in] illuminant:		xyz所在光源
// [in] observer:		xyz所在观察者角度
// [in] xyz:			要计算其白度指数的XYZ的值
// 返回：如果光源和观察者角度有效返回白度指数，否则返回NAN
float clralgo_calculate_whiteness_taube(enum clralgo_StandardIlluminant illuminant,
	enum clralgo_StandardObserver observer,
	const struct clralgo_CIEXYZ* xyz)
{
	float fXA, fXB, fZB;

	if (get_AGB(illuminant, observer, &fXA, &fXB, &fZB))
	{
		return 4.0f / fZB * xyz->Z - 3.0f * xyz->Y;
	}

	return clralgo_FLOAT_NAN;
}

// 计算Hunter 1942白度指数
// 参数：
// [in] illuminant:		lab所在光源
// [in] observer:		lab所在观察者角度
// [in] lab:			要计算其白度指数的Hunter Lab值
// 返回：如果光源和观察者角度有效返回白度指数，否则返回NAN
float clralgo_calculate_whiteness_hunter1942(enum clralgo_StandardIlluminant illuminant,
	enum clralgo_StandardObserver observer,
	const struct clralgo_HunterLab* lab)
{
	if ((illuminant == clralgo_C && observer == clralgo_DEGREE_2) ||
		(illuminant == clralgo_D65 && observer == clralgo_DEGREE_10))
	{
		return 100.0f - sqrtf(powf(100.0f - lab->L, 2) + powf(lab->a, 2) + powf(lab->b, 2));
	}
	return clralgo_FLOAT_NAN;
}

// 计算Hunter 1960白度指数
// 参数：
// [in] illuminant:		lab所在光源
// [in] observer:		lab所在观察者角度
// [in] lab:			要计算其白度指数的Hunter Lab值
// 返回：如果光源和观察者角度有效返回白度指数，否则返回NAN
float clralgo_calculate_whiteness_hunter1960(enum clralgo_StandardIlluminant illuminant,
	enum clralgo_StandardObserver observer,
	const struct clralgo_HunterLab* lab)
{
	if ((illuminant == clralgo_C && observer == clralgo_DEGREE_2) ||
		(illuminant == clralgo_D65 && observer == clralgo_DEGREE_10))
	{
		return lab->L - 3.0f * lab->b;
	}
	return clralgo_FLOAT_NAN;
}

// 计算R457白度；注意：R457仅在A光源下有效
// 参数：
// [in] illuminant: 当前光源
// [in] spectra:	光谱数据
// 返回：返回白度指数
float clralgo_calculate_whiteness_r457(enum clralgo_StandardIlluminant illuminant,
	const struct clralgo_Spectra* spectra)
{
	// F(λ) 400~520nm的表
	const float Flambda[] = {/* 0.0, 0.0, */ 1.0f, 6.7f, 18.2f, 34.5f, 57.6f, 82.5f, 100.0f, 88.7f, 53.1f, 20.3f, 5.6f, 0.3f, 0.0f };
	const float Kb = 100.0f / 468.5f;
	float sum = 0.0f;
	int offset = clralgo_Spectra_index_of(spectra, 400);
	int i;
	for (i = 0; i < clralgo_ARRAY_SIZE(Flambda); i++)
	{
		sum += Flambda[i] * spectra->data[offset + i];
	}
	return Kb * sum;
}


float clralgo_calculate_whiteness_berger59(enum clralgo_StandardIlluminant illuminant,
	enum clralgo_StandardObserver observer, const struct clralgo_CIEXYZ* xyz)
{
	const struct clralgo_CIEXYZ white_point = clralgo_get_white_point_of_illuminant(observer, illuminant);
	//float y = white_point.Y / (white_point.Y + white_point.X + white_point.Z);
	float y = 0.3333f;
	float tmp = y * xyz->Y + 125.0f * (xyz->Z / white_point.Z) - 125.0f * (xyz->X / white_point.X);
	return tmp / y; // 归一化
}


float clralgo_calculate_whiteness_ganz_and_grieser(
	const struct clralgo_CIEXYZ* xyz
)
{
	static const float P = -1868.322f;
	static const float Q = -3695.690f;
	static const float C = 1809.441f;

	clralgo_CIEYxy yxy;
	clralgo_xyz_to_yxy(xyz, &yxy);
	return yxy.Y + yxy.x * P + yxy.y * Q + C;
}

float clralgo_calculate_tint_ganz_and_grieser(
	const struct clralgo_CIEXYZ* xyz
)
{
	static const m = -1001.223f;
	static const n = 748.366f;
	static const k = 68.261f;

	clralgo_CIEYxy yxy;
	clralgo_xyz_to_yxy(xyz, &yxy);

	return m * yxy.x + n * yxy.y + k;
}

// 计算ASTM D1925-1970黄度指数（仅在2度观察角C光源有效）
// 参数：
// [in] illuminant:		xyz所在光源
// [in] observer:		xyz所在观察者角度
// [in] xyz:			要计算其黄度指数的XYZ的值
// 返回：如果光源和观察者角度有效返回黄度指数，否则返回NAN
float clralgo_calculate_yellowness_astm_d1925_1970(enum clralgo_StandardIlluminant illuminant,
	enum clralgo_StandardObserver observer,
	const struct clralgo_CIEXYZ* xyz)
{
	if (illuminant == clralgo_C && observer == clralgo_DEGREE_2)
	{
		return 100.0f * (1.28f * xyz->X - 1.06f * xyz->Z) / xyz->Y;
	}
	return clralgo_FLOAT_NAN;
}

// 计算ASTM E313-2005黄度指数
// 参数：
// [in] illuminant:		xyz所在光源
// [in] observer:		xyz所在观察者角度
// [in] xyz:			要计算其黄度指数的XYZ的值
// 返回：如果光源和观察者角度有效返回黄度指数，否则返回NAN
float clralgo_calculate_yellowness_astm_e313_2005(enum clralgo_StandardIlluminant illuminant,
	enum clralgo_StandardObserver observer,
	const struct clralgo_CIEXYZ* xyz)
{
	float Cx, Cz;
	switch (observer)
	{
	case clralgo_DEGREE_2:
		switch (illuminant) {
		case clralgo_C:
			Cx = 1.2769f;
			Cz = 1.0592f;
			break;
		case clralgo_D65:
			Cx = 1.2985f;
			Cz = 1.1335f;
			break;
		default:
			return clralgo_FLOAT_NAN;
		}
		break;
	case clralgo_DEGREE_10:
		switch (illuminant) {
		case clralgo_C:
			Cx = 1.2871f;
			Cz = 1.0781f;
			break;
		case clralgo_D65:
			Cx = 1.3013f;
			Cz = 1.1498f;
			break;
		default:
			return clralgo_FLOAT_NAN;
		}
		break;
	default:
		return clralgo_FLOAT_NAN;
	}

	return (xyz->Y > 0.0f) ? 100.0f * (Cx * xyz->X - Cz * xyz->Z) / xyz->Y : clralgo_FLOAT_NAN;
}


// 通过XYZ值计算同色异谱指数
// 参数：
// [in] spectra1:	样品1的光谱
// [in] spectra2:	样品2的光谱
// [in]	illuminant: 测试光源
// [in] observer:	测试观察者角度
// [in] ref_illuminant:	参考光源
// [in] ref_observer:	参考观察者角度
// 返回：样品1和样品2的同色异谱指数
float clralgo_calculate_metamerism_index(
	const struct clralgo_Spectra* spectra1,
	const struct clralgo_Spectra* spectra2,
	enum clralgo_StandardIlluminant illuminant,
	enum clralgo_StandardObserver observer,
	enum clralgo_StandardIlluminant ref_illuminant,
	enum clralgo_StandardObserver ref_observer)
{
	struct clralgo_CIEXYZ xyz1, xyz2, xyz1_ref, xyz2_ref;

	clralgo_calculate_xyz(spectra1, illuminant, observer, &xyz1);
	clralgo_calculate_xyz(spectra2, illuminant, observer, &xyz2);
	clralgo_calculate_xyz(spectra1, ref_illuminant, ref_observer, &xyz1_ref);
	clralgo_calculate_xyz(spectra2, ref_illuminant, ref_observer, &xyz2_ref);

	return clralgo_calculate_metamerism_index_by_xyz(
		illuminant, observer, &xyz1, &xyz2,
		ref_illuminant, ref_observer, &xyz1_ref, &xyz2_ref);
}


// 通过XYZ值计算同色异谱指数
// 参数：
// [in]	illuminant: xyz1和xyz2的测试光源和观察者角度
// [in] observer:	xyz1和xyz2的测试光源和观察者角度
// [in] xyz1:		样品1的XYZ值
// [in] xyz2:		样品2的XYZ值
// [in] ref_illuminant:	参考光源
// [in] ref_observer:	参考观察者角度
// [in] xyz1_ref:	样品1在参考条件下测得的XYZ值
// [in] xyz2_ref:	样品2在参考条件下测得的XYZ值
// 返回：样品1和样品2的同色异谱指数
float clralgo_calculate_metamerism_index_by_xyz(
	enum clralgo_StandardIlluminant illuminant,
	enum clralgo_StandardObserver observer,
	const struct clralgo_CIEXYZ* xyz1,
	const struct clralgo_CIEXYZ* xyz2,
	enum clralgo_StandardIlluminant ref_illuminant,
	enum clralgo_StandardObserver ref_observer,
	const struct clralgo_CIEXYZ* xyz1_ref,
	const struct clralgo_CIEXYZ* xyz2_ref)
{
	float fx = xyz1->X != 0 ? xyz2->X / xyz1->X : 1.0f;
	float fy = xyz1->Y != 0 ? xyz2->Y / xyz1->Y : 1.0f;
	float fz = xyz1->Z != 0 ? xyz2->Z / xyz1->Z : 1.0f;

	struct clralgo_CIEXYZ tmp = {
		xyz1_ref->X * fx, xyz1_ref->Y * fy, xyz1_ref->Z * fz
	};
	struct clralgo_CIELab lab1, lab2;

	clralgo_xyz_to_lab(ref_illuminant, ref_observer, &tmp, &lab1);
	clralgo_xyz_to_lab(ref_illuminant, ref_observer, xyz2_ref, &lab2);

	return clralgo_calculate_dEab(&lab1, &lab2);
}


static enum clralgo_ColorFastnessGrade float_fastness_grade_to_enum(float fgrade)
{
	int ival;
	float fval;

	if (fgrade < 1.25)
		return clralgo_GRADE_1;
	if (fgrade >= 4.75)
		return clralgo_GRADE_5;

	ival = (int)fgrade;
	fval = fgrade - ival;
	if (fval >= 0.25 && fval < 0.75)
		return (enum clralgo_ColorFastnessGrade)(ival * 2);
	if (fval >= 0.75)
		ival += 1;
	return (enum clralgo_ColorFastnessGrade)(ival * 2 - 1);
}

// 计算沾色牢度
// 参数：
// [in] illuminant:		standard和test使用的标准光源
// [in] observer:		standard和test使用的标准观察者角度
// [in] test:			试样的Lab值
// [in] standard:		标样的Lab值
// [out, opt] ssr:		如果不为空存储连续的色牢度等级
// [out, opt] dEgs:		如果不为空存储ΔEgs
// 返回：沾色牢度；如果指定条件无效返回clralgo_COLOR_FASTNESS_UNDEFINED
enum clralgo_ColorFastnessGrade clralgo_calculate_ssr(
	enum clralgo_StandardIlluminant illuminant,
	enum clralgo_StandardObserver observer,
	const struct clralgo_CIELab* test,
	const struct clralgo_CIELab* standard,
	float* ssr, float* dEgs)
{
	if ((observer == clralgo_DEGREE_2 && illuminant == clralgo_C)
		|| (observer == clralgo_DEGREE_10 && illuminant == clralgo_D65))
	{
		float dL, dE, _dEgs, _ssr;

		dL = test->L - standard->L;
		dE = clralgo_calculate_dEab(test, standard);
		_dEgs = dE - 0.4f * sqrtf(dE * dE - dL * dL);
		_dEgs = (float)round_to(_dEgs, 2);
		_ssr = _dEgs >= 4.26f ? (6.1f - 1.45f * logf(_dEgs)) : (5.0f - 0.23f * _dEgs);
		_ssr = (float)round_to(_ssr, 2);

		if (ssr != NULL)
			*ssr = _ssr;
		if (dEgs != NULL)
			*dEgs = _dEgs;

		return float_fastness_grade_to_enum(_ssr);
	}

	return clralgo_COLOR_FASTNESS_UNDEFINED;
}

// 计算变色牢度
// 参数：
// [in] illuminant:		standard和test使用的标准光源
// [in] observer:		standard和test使用的标准观察者角度
// [in] test:			试样的Lab值
// [in] standard:		标样的Lab值
// [out, opt] gsr:		如果不为空存储连续的色牢度等级
// [out, opt] dEf:		如果不为空存储ΔEf
// 返回：变色牢度；如果指定条件无效返回clralgo_COLOR_FASTNESS_UNDEFINED
enum clralgo_ColorFastnessGrade  clralgo_calculate_gsr(
	enum clralgo_StandardIlluminant illuminant,
	enum clralgo_StandardObserver observer,
	const struct clralgo_CIELab* test,
	const struct clralgo_CIELab* standard,
	float* gsr, float* dEf)
{

	clralgo_CIELCh std_lch, test_lch;
	clralgo_lab_to_lch(standard, &std_lch);
	clralgo_lab_to_lch(test, &test_lch);

	float dL = test->L - standard->L;
	float dCab = test_lch.C - std_lch.C;
	float dHab = (standard->a * test->b - standard->b * test->a) / sqrtf(0.5 * (test_lch.C * std_lch.C + test->a * standard->a + test->b * standard->b));
	float hm;
	if (fabsf(test_lch.h - std_lch.h) <= 180.f)
		hm = (test_lch.h + std_lch.h) / 2.f;
	else if ((fabsf(test_lch.h - std_lch.h) > 180.f) && ((test_lch.h + std_lch.h) < 360.f))
		hm = (test_lch.h + std_lch.h) / 2.f + 180.f;
	else
		hm = (test_lch.h + std_lch.h) / 2.f - 180.f;

	float Cm = (test_lch.C + std_lch.C) / 2.f;
	float x;
	if (fabsf(hm - 280.f) <= 180.f)
		x = powf((hm - 280.f) / 30.f, 2.f);
	else
		x = powf(((360.f - fabsf(hm - 280.f)) / 30.0f), 2.f);
	float D = dCab * Cm * expf(-x) / 100.f;
	float dCk = dCab - D;
	float dHk = dHab - D;
	float dCf = dCk / (1.0f + powf((20.0f * Cm / 1000.0f), 2.0f));
	float dHf = dHk / (1.0f + powf((10.0f * Cm / 1000.0f), 2.0f));
	float _dEf = sqrtf(dL * dL + dCf * dCf + dHf * dHf);

	_dEf = (float)round_to(_dEf, 2);

	float _gsr = _dEf > 3.40f ? (5.0f - log10f(_dEf / 0.85f) / log10f(2.0f)) : (5.0f - _dEf / 1.7f);
	_gsr = (float)round_to(_gsr, 2);

	if (gsr != NULL)
		*gsr = _gsr;
	if (dEf != NULL)
		*dEf = _dEf;

	return float_fastness_grade_to_enum(_gsr);
}

// 计算反射率的K/S值，具體算法參考《The Kubelka-Monk Theory and K/S》
static float K_S(float r)
{
	return (1.0f - r) * (1.0f - r) / (2.0f * r);
}

// 根据透射率计算光学吸收率
// 注意吸收率不能是百分比
static float absorbance(float transmittance)
{
	if (transmittance < 0.000001)
		transmittance = 0.000001;
	if (transmittance > 1)
		transmittance = 1;
	return -log10f(transmittance);
}

// 根据样品反射率计算 Color Value SUM。具体算法参考HunterLab的《Strength Calculations》
// 参数：
// [in] spectra: 反射率光谱
// 返回：Color Value SUM
float clralgo_calculate_color_value_sum(const struct clralgo_Spectra* spectra, int isTransmittance)
{
	int i = 0;
	double sum = 0.0f;
	//for (i = 0; i < spectra->range.count; i++)
	//{
	//	sum += K_S(spectra->data[i]);
	//}
	//对标DataColor,计算400~700的KS和
	/*int wavelength = 0, min = 400, max = 700;
	for (wavelength = min; wavelength <= max; wavelength += spectra->range.interval)
	{
		sum += K_S(clralgo_Spectra_get(spectra, wavelength));
		i++;
	}*/
	float (*f)(float) = isTransmittance ? absorbance : K_S;
	for (i = 0; i < spectra->range.count; i++) {
		sum += f(spectra->data[i]);
	}

	return (float)(sum / spectra->range.count);
	//return (float)(sum / i);
}

// 计算 Color Value SWL，即最小反射率的K/S；具体算法参考HunterLab的《Strength Calculations》
// 参数：
// [in] spectra:					 反射率光谱
// [out, opt] wavelength_of_minimum: 如果不为NULL存储取小反射率所在波长
// 返回：Color Value SWL，即最小反射率的K/S
float clralgo_calculate_color_value_swl(
	const struct clralgo_Spectra* spectra, int isTransmittance, int* wavelength_of_minimum)
{

	const float WL_MIN = 400.0f;
	const float WL_MAX = 700.0f;

	int idxMin = (int)((WL_MIN - spectra->range.start) / spectra->range.interval + 0.5f);
	int idxMax = (int)((WL_MAX - spectra->range.start) / spectra->range.interval + 0.5f);

	/* 钳制到合法下标 */
	if (idxMin < 0) idxMin = 0;
	if (idxMax >= spectra->range.count) idxMax = spectra->range.count - 1;

	int   minI = idxMin;
	float minR = spectra->data[minI];

	for (int i = idxMin + 1; i <= idxMax; ++i)
	{
		if (spectra->data[i] < minR)
		{
			minR = spectra->data[i];
			minI = i;
		}
	}


	if (wavelength_of_minimum != NULL)
		*wavelength_of_minimum = spectra->range.start + minI * spectra->range.interval;
	//return K_S(minR);
	return !isTransmittance ? K_S(minR) : absorbance(minR);
}

// 计算Average Strength（也叫做 % Strength SUM）；具体算法参考HunterLab的《Strength Calculations》
// 参数：
// [in] test:		试样光谱数据
// [in] standard:	标样光谱数据
// 返回：Average Strength（也叫做 % Strength SUM）
float clralgo_calculate_average_strength(
	const struct clralgo_Spectra* test, const struct clralgo_Spectra* standard, int isTransmittance)
{
	return clralgo_calculate_color_value_sum(test, isTransmittance) / clralgo_calculate_color_value_sum(standard, isTransmittance) * 100.0f;
}

//计算加权力份
float clralgo_calculate_weighted_strength(enum clralgo_StandardIlluminant illuminant,
	enum clralgo_StandardObserver observer,
	const struct clralgo_Spectra* trialSpectra,
	const struct clralgo_Spectra* stdSpectra,
	int isTransmittance) {
	const float(*weights)[3];
	int wavelength;
	double trialSum, stdSum;
	float (*f)(float);
	assert((stdSpectra->range.interval == 5 || stdSpectra->range.interval == 10) && trialSpectra->range.interval == stdSpectra->range.interval);

	if (stdSpectra->range.interval == 5)
	{
		weights = TRISTIMULUS_WEIGHTING_WEIGHTING_FACTORS_5NM_TABLEs[observer][illuminant]->factors;
	}
	else if (stdSpectra->range.interval == 10) {
		weights = TRISTIMULUS_WEIGHTING_WEIGHTING_FACTORS_10NM_TABLEs[observer][illuminant]->factors;
	}
	else {
		// 只接受5nm和10nm的
		assert(0);
		return clralgo_FLOAT_NAN;
	}

	// 只计算400nm到700nm之间的
	trialSum = 0.0;
	stdSum = 0.0;
	f = !isTransmittance ? K_S : absorbance;
	for (wavelength = 400; wavelength <= 700; wavelength += stdSpectra->range.interval) {
		int index = (wavelength - 360) / stdSpectra->range.interval;
		float t = (*weights)[index * 3] + (*weights)[index * 3 + 1] + (*weights)[index * 3 + 2];

		trialSum += f(clralgo_Spectra_get(trialSpectra, wavelength)) * t;
		stdSum += f(clralgo_Spectra_get(stdSpectra, wavelength)) * t;
	}

	return (float)(trialSum / stdSum * 100.0);
}




// 计算 %Strength-SWL，算法参考《Strength Calculations》
// 参数：
// [in] test:		试样光谱数据
// [in] standard:	标样光谱数据
// [out, opt] wavelength_of_minimum: 如果不为空存储标样最小反射率所在波长
// 返回：%Strength-SWL
float clralgo_calculate_precent_strength_swl(
	const struct clralgo_Spectra* test,
	const struct clralgo_Spectra* standard,
	int* wavelength_of_minimum)
{
	int wave;
	float SWLstd, SWLtest;

	assert(clralgo_WavelengthRange_is_equal(test->range, standard->range));

	SWLstd = clralgo_calculate_color_value_swl(standard, 0, &wave);
	SWLtest = K_S(clralgo_Spectra_get(test, wave));

	if (wavelength_of_minimum != NULL)
		*wavelength_of_minimum = wave;

	return SWLtest / SWLstd * 100.0f;
}

// 计算%Strength。算法参考《Strength Calculations》
// 参数：
// [in] test:			试样光谱数据，波长范围必须与标样相同
// [in] standard:		标样光谱数据
// [out] strengths:		数组，其大小与标样的反射率个数相同，用于存储计算得到的
//						各个波长点的反射率的%Strength
// 注意：标样和试样的反射率波长范围必须相同
void clralgo_calculate_precent_strength(
	const struct clralgo_Spectra* test,
	const struct clralgo_Spectra* standard,
	float* strengths)
{
	int i;
	assert(clralgo_WavelengthRange_is_equal(test->range, standard->range));
	assert(strengths != NULL);

	for (i = 0; i < standard->range.count; i++)
	{
		strengths[i] = K_S(test->data[i]) / K_S(standard->data[i]);
	}
}

// 计算遮盖度
// 参数：
// [in] Ywhite:	白底样品的Y值
// [in] Yblack:	黑底样品的Y值
// 返回：遮盖度
float clralgo_calculate_opacity(float Ywhite, float Yblack)
{
	return Yblack / Ywhite;
}

// 获取555色调分类的等级
// d: 试样的L, a, b减标样的差值之一
// tol: 容差，必须大于0
static int get_shade_number(float d, float tol)
{
	int gradeL;
	if (tol <= 0)
	{
		return 0;
	}

	if (d >= -tol && d < tol)
		gradeL = 5;
	else if (d >= -2 * tol && d < -tol)
		gradeL = 4;
	else if (d >= -3 * tol && d < -2 * tol)
		gradeL = 3;
	else if (d >= -4 * tol && d < -3 * tol)
		gradeL = 2;
	else if (d < -4 * tol)
		gradeL = 1;
	else if (d > tol && d <= 2 * tol)
		gradeL = 6;
	else if (d > 2 * tol && d <= 3 * tol)
		gradeL = 7;
	else if (d > 3 * tol && d <= 4 * tol)
		gradeL = 8;
	else
		gradeL = 9;

	return gradeL;
}

int clralgo_get_555_shade(const struct clralgo_CIELab* test,
	const struct clralgo_CIELab* standard,
	float tolL, float tolA, float tolB)
{
	if (standard->L < 0 || standard->L > 150 || standard->a < -130
		|| standard->a > 130 || standard->b < -130 || standard->b > 130
		|| test->L < 0 || test->L > 150 || test->a < -130
		|| test->a > 130 || test->b < -130 || test->b > 130
		|| tolL <= 0 || tolA <= 0 || tolB <= 0)
	{
		// 无效参数
		return 0;
	}
	return get_shade_number(test->L - standard->L, tolL) * 100 +
		get_shade_number(test->a - standard->a, tolA) * 10 +
		get_shade_number(test->b - standard->b, tolB);
}


void clralgo_lab_to_din99(
	const clralgo_CIELab* lab,
	clralgo_DIN99Lab* din99)
{
	const float kE = 1;
	//const float kCH = 1;

	const float cos16 = cosf(RADIANS(16));
	const float sin16 = sinf(RADIANS(16));

	float e = lab->a * cos16 + lab->b * sin16;
	float f = -0.7f * lab->a * sin16 + 0.7f * lab->b * cos16;
	float G = sqrtf(e * e + f * f);
	float k = logf(1.0f + 0.045f * G) / 0.045f;

	din99->L = (1.0f / kE) * 105.51f * logf(1.0f + 0.0158f * lab->L);
	din99->a = k * e / G;
	din99->b = k * f / G;
}

float clralgo_calculate_gardner_index(const struct clralgo_CIEXYZ* xyz) {
	/**
	 * 输入C2条件下的XYZ值，输出Gardner，正常为1~18(颜色越浅Gardner指数越小，颜色越深数值越大)；
	 */

	unsigned char G_low = 0, G_up = 0;
	int i;

	// Gardner指数等级表
	static const double GardnerIndex[] = {
		9.9403,
		15.2461,
		24.3695,
		35.5900,
		46.5409,
		61.7951,
		79.6146,
		88.9341,
		96.3011,
		104.3960,
		120.8514,
		133.2676,
		150.8785,
		166.7542,
		181.5017,
		196.0602,
		216.7568,
		234.8300 };

	//const float CX = 1.2769f;
	//const float CZ = 1.0592f;
	float Gardner = 0;
	float YI_E313_00 = //100.0f * (CX * xyz->X - CZ * xyz->Z) / xyz->Y;
		clralgo_calculate_yellowness_astm_e313_2005(clralgo_C, clralgo_DEGREE_2, xyz);
	if (YI_E313_00 < -0.5) {
		return clralgo_FLOAT_NAN;
	}

	/**
	* 判断样品是否接近参考白
	*/
	clralgo_CIELab lab;
	clralgo_xyz_to_lab(clralgo_C, clralgo_DEGREE_2, xyz, &lab);
	clralgo_CIEXYZ white_point_xyz = clralgo_get_white_point_of_illuminant(clralgo_DEGREE_2, clralgo_C);
	clralgo_CIELab white_point_lab;
	clralgo_xyz_to_lab(clralgo_C, clralgo_DEGREE_2, &white_point_xyz, &white_point_lab);
	if (clralgo_calculate_dEab(&lab, &white_point_lab) < 0.2) {
		return 0;
	}

	for (i = 0; i < 18; i++) {
		if (YI_E313_00 < GardnerIndex[i]) {
			G_up = i;
			break;
		}
		if (YI_E313_00 == GardnerIndex[i]) {
			G_low = i;
			G_up = i;
			break;
		}
		G_low = i;
	}

	if ((G_low == G_up) && (G_low != 0)) {
		Gardner = (float)(G_low + 1); // 序号加1
	}
	else if ((G_low == 17) && (G_up == 0)) {
		Gardner = 18;
	}
	else if ((G_low == 0) && (G_up == 0)) {
		Gardner = (float)(YI_E313_00 / GardnerIndex[0]);
	}
	else {
		Gardner = (float)(G_low + 1 + (YI_E313_00 - GardnerIndex[G_low]) / (GardnerIndex[G_up] - GardnerIndex[G_low]));
	}

	return Gardner;
}




//光程转换
///2026.4.3；取消YI值的上限判断
float clralgo_calculate_YI_Optical_path_conversion(int Pathlength, float YI)
{
	float ret = 0;
	switch (Pathlength)
	{
	case 30: {
		//if (YI > 1.0221) {
		//	//y = 0.0021x2 + 0.2709x + 1.0221
		//	YI = (0.0021 * pow(YI, 2)) + (0.2709 * YI) + 1.0221;
		//}
		//else {
			//2025-02-24    y = -2E-08 * X ^ 4 + 8E-06 * X ^ 3 + 0.0002 * X ^ 2 + 0.3468 * X + 0.0142
			//2025-02-26    y = 2E-08*x^4 + 2E-07*x^3 + 0.0001*x^2 + 0.1094*x + 0.0935
			YI = (2E-08) * YI * YI * YI * YI + (2E-07) * YI * YI * YI + 0.0001 * YI * YI + 0.1094 * YI + 0.0935;
		//}
		//YI = (2E-08) * YI * YI * YI * YI + (2E-07) * YI * YI * YI + 0.0001 * YI * YI + 0.1094 * YI + 0.0935;
	}break;
	case 50: {
		//if (YI > 1.1479) {
		//	//y = 0.0018x2 + 0.1398x + 1.1479
		//	YI = (0.0018 * pow(YI, 2)) + (0.1398 * YI) + 1.1479;
		//}
		//else {
			//2025-02-24    y = 1E-08*X^4 + 3E-06*X^3 + 0.0002*X^2 + 0.2133*X - 0.0409
			//2025-02-26    y = 1E-08*x^4 + 4E-06*x^3 + 0.0002*x^2 + 0.2141*x - 0.0356
			YI = (1E-08) * YI * YI * YI * YI + (4E-06) * YI * YI * YI + 0.0002 * YI * YI + 0.2141 * YI - 0.0356;
		//}
		//YI = (1E-08) * YI * YI * YI * YI + (4E-06) * YI * YI * YI + 0.0002 * YI * YI + 0.2141 * YI - 0.0356;
	}break;
	case 100: {
		//if (YI > 1.044) {
		//	//y = 0.0012x2 + 0.0533x + 1.044
		//	YI = (0.0012 * pow(YI, 2)) + (0.0533 * YI) + 1.044;
		//}
		//else {
			//2025-02-24    y = 3E-08*X^4 - 3E-06*X^3 + 0.0004*X^2 + 0.103*X + 0.1197
			//2025-02-26    y = -2E-08*x^4 + 9E-06*x^3 + 0.0002*x^2 + 0.347*x + 0.0201
			YI = (-2E-08) * YI * YI * YI * YI - (9E-06) * YI * YI * YI + 0.0002 * YI * YI + 0.347 * YI + 0.0201;
		//}
		//YI = (-2E-08) * YI * YI * YI * YI - (9E-06) * YI * YI * YI + 0.0002 * YI * YI + 0.347 * YI + 0.0201;
	}break;
	}
	ret = YI;
	return ret;
}


float clralgo_calculate_klett_index(const struct clralgo_CIEXYZ* xyz) {
	/**
	 * 输入C2条件下的XYZ值，输出Pt_Co/APHA，正常为0~500(颜色越浅Pt_Co指数越小，颜色越深数值越大,最大到1000)；
	 * state说明：普通计算情况下state=1,不是太合理
	 */

	float klett = 0;
	static const int num_Pt_CoIndex = 128;
	/**
	 * 当样品YI数值小于0，无效
	 */
	float YI = clralgo_calculate_yellowness_astm_e313_2005(clralgo_C, clralgo_DEGREE_2, xyz);
	if (clralgo_IS_NAN(YI) || YI < -0.5)
	{
		//return 0;
	}

	/**
	* 判断样品是否接迎参考白
	*/
	clralgo_CIELab lab;
	clralgo_xyz_to_lab(clralgo_C, clralgo_DEGREE_2, xyz, &lab);
	clralgo_CIEXYZ white_point_xyz = clralgo_get_white_point_of_illuminant(clralgo_DEGREE_2, clralgo_C);
	clralgo_CIELab white_point_lab;
	clralgo_xyz_to_lab(clralgo_C, clralgo_DEGREE_2, &white_point_xyz, &white_point_lab);
	if (clralgo_calculate_dEab(&lab, &white_point_lab) < 0.2) 
	{
		//return 0;
	}
	//klett = 0.048*YI*YI + 10.477*YI + 1.7607;
	klett = 0.3426*YI + 5.0117;
	if(klett > 625) klett = 625;
	return klett;
}

int clralgo_calculate_haze(float T1, float T2, float T3, float T4, float* haze, float* Tt, float* Td)
{
	if (T1 <= 50.0f || T1 >= 120.0f || T3 >= 100.0f || T3 >= T1)
		return 0;

	*Tt = T2 / T1;
	*Td = (T4 - T3 * (T2 / T1)) / T1;
	*haze = (*Td) / (*Tt);
	if (*haze < 0.0f) {
		*haze = 0.00f;
	}
	else if (*haze > 1.0f) {
		*haze = 1.0;
	}
	return 1;
}

//float get_white_refledt()
// 360~780
const float integral_percent[] =
{ 0, 0, 0, 0, 0.0004, 0.0012, 0.0040, 0.0116,
 0.023, 0.038, 0.06, 0.091, 0.139, 0.208, 0.323, 0.503,
 0.71, 0.862, 0.954, 0.955, 0.995, 0.952, 0.87, 0.757,
 0.631, 0.503, 0.381, 0.265, 0.175, 0.107, 0.061, 0.032,
 0.017, 0.0082, 0.0041, 0, 0, 0, 0, 0,
 0, 0, 0,
};

#define standand 113
float integral_quantity(clralgo_Spectra* reflect)
{
	unsigned char i = 0;
	float data = 0;

	int end_wavelength = reflect->range.start + reflect->range.interval * reflect->range.count;
	for (int i = 0; i < clralgo_ARRAY_SIZE(integral_percent); i++) {
		int wavelength = 360 + i * 10;
		if (wavelength < reflect->range.start || wavelength > end_wavelength) {
			continue;
		}
		data += (float)clralgo_Spectra_get(reflect, wavelength) * integral_percent[i];
	}
	return data;
}
unsigned short clralgo_8_gloss_data(clralgo_Spectra* ssci, clralgo_Spectra* ssce, clralgo_Spectra* wsci, clralgo_Spectra* wsce)
{
	unsigned short data_8_gloss;
	float sample = 0, sample1 = 0, glass = 0, glass1 = 0, data = 0;
	sample = integral_quantity(ssci);
	if (sample == 0)  return 0;
	sample1 = integral_quantity(ssce);
	if (sample1 == 0)  return 0;
	sample -= integral_quantity(ssce);

	glass = integral_quantity(wsci);
	glass1 = integral_quantity(wsce);

	glass -= glass1;
	data = sample / glass * standand;
	data *= 10;
	data_8_gloss = (unsigned short)(data / 10 + ((unsigned short)data % 10 > 4 ? 1 : 0));
	return data_8_gloss;
}



/*新增Munsell空间算法*/

//同一 V 值 个数
int V_COUNT[] = { 121,	175,	212,	257,	297,	379,	449,	493,	525,	529,	492,	450,	367,	249, 249 };

//V值
float V_value[] = { 0.2,0.4 ,0.6 ,0.8 ,1 , 2, 3, 4, 5, 6, 7, 8, 9, 10, 10 };


//不同V值起始地址
int V_COUNT_START[] = { 0,	121,	296,	508,	765,	1062,	1441,	1890,	2383,	2908,	3437,	3929,	4379,	4746, 4746 };


const double Munsell_x[5000] //*1000
= {
713,	449,	262,	-78,	185,	-257,	144,	-235,	117,	-209,	97,	-181,	80,	-137,	68,	-100,	66,	-48,	72,	-16,	85,	15,	97,	39,	111,	63,	121,	74,	133,	88,	63,	147,	109,	87,	165,	133,	115,	104,	96,	86,	192,	171,	159,	152,	148,	145,	142,	139,	137,	136,	227,	213,	206,	202,	200,	198,	196,	195,	193,	192,	250,	241,	236,	232,	231,	229,	228,	227,	226,	275,	269,	266,	264,	263,	263,	262,	288,	283,	280,	277,	276,	275,	300,	296,	293,	291,	290,	315,	313,	312,	310,	310,	337,	341,	342,	342,	370,	381,	388,	393,	404,	424,	435,	451,	488,	510,	501,	556,	543,	626,	592,	696,	679,	843,	837,	1174,	1000,	1290,	1430,	1495,	1434,	542,	423,	735,	358,	323,	312,	128,	277,	41,	258,	5,	239,	-11,	226,	-11,	213,	-4,	196,	8,	180,	31,	173,	51,	-5,	169,	74,	32,	169,	87,	48,	172,	102,	67,	176,	113,	81,	183,	126,	93,	67,	190,	141,	113,	93,	202,	161,	135,	116,	107,	99,	96,	94,	91,	86,	220,	192,	175,	165,	160,	155,	152,	150,	147,	145,	143,	141,	139,	137,	241,	223,	212,	206,	202,	199,	197,	195,	193,	192,	191,	190,	188,	187,	259,	246,	238,	233,	230,	227,	226,	224,	223,	222,	220,	219,	218,	281,	272,	265,	260,	257,	253,	252,	250,	248,	247,	296,	289,	284,	280,	276,	273,	272,	270,	269,	309,	304,	302,	298,	296,	294,	293,	291,	320,	320,	320,	320,	320,	320,	320,	337,	344,	348,	350,	353,	353,	360,	374,	384,	391,	398,	381,	406,	423,	437,	454,	411,	450,	477,	501,	529,	441,	498,	537,	575,	466,	539,	588,	635,	490,	582,	649,	706,	534,	665,	755,	844,	585,	775,	889,	638,	908,	698,	729,	721,	659,	432,	902,	377,	603,	342,	410,	315,	274,	-45,	292,	208,	-128,	281,	175,	-143,	270,	152,	-128,	259,	137,	-102,	247,	124,	-68,	236,	117,	-36,	221,	112,	9,	213,	113,	30,	206,	116,	52,	202,	123,	71,	17,	202,	134,	88,	44,	204,	143,	99,	60,	209,	153,	115,	84,	215,	166,	131,	106,	89,	79,	223,	182,	152,	131,	118,	110,	104,	99,	93,	88,	84,	239,	208,	188,	176,	168,	163,	159,	156,	153,	150,	148,	146,	144,	142,	140,	257,	237,	225,	216,	211,	208,	204,	202,	200,	198,	196,	195,	194,	192,	190,	272,	258,	250,	244,	240,	238,	235,	233,	232,	230,	229,	228,	227,	226,	287,	280,	274,	270,	268,	266,	264,	263,	262,	261,	260,	260,	301,	296,	292,	288,	284,	281,	279,	277,	276,	275,	311,	308,	306,	304,	302,	301,	299,	298,	297,	322,	324,	325,	326,	326,	326,	326,	325,	337,	347,	354,	359,	363,	366,	370,	373,	355,	373,	387,	397,	406,	413,	421,	372,	399,	419,	434,	447,	459,	391,	432,	464,	489,	511,	537,	411,	469,	514,	551,	586,	431,	502,	558,	604,	640,	447,	537,	605,	660,	704,	474,	603,	693,	787,	846,	505,	673,	805,	930,	526,	764,	551,	899,	563,	995,	548,	502,	1044,	397,	761,	363,	556,	336,	400,	425,	314,	305,	237,	-119,	298,	254,	150,	-156,	287,	225,	102,	-213,	280,	205,	82,	-185,	272,	191,	73,	-143,	265,	178,	70,	-100,	253,	163,	70,	-62,	241,	150,	72,	-18,	230,	145,	77,	8,	223,	146,	86,	33,	220,	149,	94,	48,	218,	154,	106,	69,	220,	160,	115,	82,	49,	222,	168,	128,	97,	71,	225,	178,	142,	117,	98,	82,	234,	192,	160,	139,	123,	109,	99,	92,	87,	81,	247,	216,	194,	179,	170,	162,	155,	150,	147,	144,	140,	137,	135,	133,	131,	130,	129,	128,	263,	242,	229,	220,	215,	210,	206,	203,	200,	199,	197,	196,	194,	193,	192,	191,	190,	189,	277,	264,	255,	248,	245,	242,	238,	236,	235,	233,	232,	231,	230,	228,	228,	227,	227,	227,	292,	283,	279,	275,	269,	266,	263,	262,	260,	258,	257,	256,	256,	255,	254,	304,	298,	294,	291,	290,	287,	284,	283,	281,	280,	279,	278,	278,	312,	310,	309,	308,	307,	306,	304,	303,	302,	302,	302,	302,	322,	326,	328,	329,	329,	330,	330,	331,	331,	331,	331,	336,	347,	355,	362,	366,	370,	373,	376,	378,	379,	350,	369,	384,	397,	405,	413,	421,	427,	365,	393,	415,	435,	447,	459,	470,	381,	421,	455,	483,	502,	521,	536,	399,	450,	496,	536,	565,	591,	411,	477,	534,	584,	624,	660,	423,	508,	578,	635,	685,	725,	445,	558,	637,	715,	802,	875,	463,	612,	721,	826,	475,	663,	817,	481,	749,	479,	820,	465,	891,	434,	880,	380.2,	501,	354,	439,	480,	335.9,	376.5,	398,	315.4,	313.3,	290,	235,	300.6,	272.2,	223.2,	110,	-13,	291,	245.4,	171.1,	62,	-22,	283.3,	229,	146.8,	55.9,	-20,	275.8,	215.9,	134.4,	53,	-20,	268.9,	204,	124.9,	51.1,	-18,	260,	188.3,	116.9,	47.6,	-17,	250,	175.3,	109.3,	46,	-14,	243,	170.2,	105.9,	50,	236.2,	165.8,	107.4,	60,	232.2,	164.9,	111.8,	71,	229.1,	166.7,	121.2,	84,	55,	229.1,	171.6,	130.3,	96.8,	71,	230.9,	178.3,	139.2,	107.7,	84,	236,	189.5,	153.9,	127.3,	106,	91,	78,	242.7,	201.2,	167.8,	144.7,	128.5,	117,	112,	106,	104,	100,	98,	96,	95,	94,	93,	92,	91,	90,	89,	88,	88,	87,	254.7,	223.2,	200,	187.2,	180.4,	176.3,	173.8,	172,	170.9,	170.1,	169.6,	169.1,	168.9,	168.6,	168.4,	168.2,	168.2,	168.1,	168,	168,	168,	168,	168,	168,	267.7,	245.9,	229,	219,	212,	207,	203.8,	200.8,	199.1,	197.6,	196.5,	195.2,	194.2,	193.6,	192.8,	193,	193,	193,	193,	193,	192,	192,	192,	192,	280.8,	266.8,	257,	249.6,	244.1,	239.4,	236.1,	233.1,	231.2,	229.5,	227.9,	226.6,	225.1,	225,	224,	224,	223,	222,	222,	222,	222,	293.6,	285.4,	279.4,	274.2,	270.1,	267,	264.5,	262.5,	261.2,	260.1,	259,	258,	258,	257,	256,	256,	303,	299.1,	296,	293.2,	290.5,	288.4,	286.8,	285.2,	284.1,	283.1,	282,	282,	281,	313.2,	313.2,	312.6,	311.4,	310.2,	309.4,	308.4,	307.8,	306.9,	307,	306,	305,	324,	329,	332.1,	334.2,	335.4,	336.1,	336.8,	336.8,	337,	336,	336,	337.8,	350.3,	358.8,	366,	372.7,	377.2,	381.1,	384,	386,	389,	349.8,	370.5,	386.5,	400.5,	413.2,	424,	433,	440,	362.9,	392,	415.1,	435.7,	452.1,	466.8,	479,	488,	376.8,	416.6,	451.5,	481.2,	505.8,	533,	548,	390.8,	442,	488.5,	528.2,	560.4,	595,	402,	466,	523.5,	572.2,	611.1,	654,	412.8,	493.3,	558.4,	617.8,	666.1,	711,	425.8,	531.1,	604.8,	672.1,	727,	778,	437.7,	566,	656,	736,	829,	443,	585,	685,	444.6,	591,	436.2,	581,	423,	565,	404.2,	543,	355.6,	418.8,	482,	342.1,	388.1,	434,	476,	330.9,	358.2,	383.9,	409,	418,	316.5,	324.8,	326,	316,	297,	274,	243,	306.9,	298.6,	285.2,	262.8,	230.7,	190.7,	141,	61,	-30,	297.8,	276.3,	249.3,	219.2,	177.3,	130.7,	82,	32.9,	-20,	291.8,	264,	231.8,	197.9,	156,	112,	68.8,	27.7,	-15,	286.9,	254,	220,	184.2,	144.2,	102.2,	62.9,	27.6,	-10,	282,	244.2,	209.2,	170.5,	132.1,	93.4,	59.9,	28.5,	-7,	276.5,	234.3,	197.1,	155.7,	119,	85.1,	55.5,	29,	-2,	269.7,	223.4,	184.3,	140.5,	105,	76.9,	51,	28,	265.1,	216.2,	174.7,	132.5,	99.1,	72.4,	50,	28,	260.6,	209.6,	166.9,	125.8,	92.9,	68,	50,	257.8,	206,	162.1,	123,	91.1,	67,	255.9,	204.8,	161.7,	124.5,	96.5,	77,	254.5,	206.3,	165.8,	131.3,	105.1,	88,	70,	255.8,	210.2,	171.6,	139.6,	115.7,	99,	83,	259.2,	217.5,	182.5,	154,	133.2,	116.6,	102,	92,	84,	263.8,	226.3,	194.2,	168.5,	150,	136.3,	125.3,	117,	111,	108,	104,	100,	98,	97,	96,	95,	93,	92,	90,	89,	87,	86,	84,	271.2,	242,	218.9,	200.5,	188.2,	181.3,	176.2,	172.8,	170.1,	168.5,	167,	166,	165.3,	164.7,	164,	163.5,	163,	162.8,	162.3,	162,	162,	162,	161,	161,	160,	280.3,	260,	244,	229.4,	220,	213.9,	208.7,	205.2,	202.1,	199.8,	197.8,	196.2,	194.9,	193.7,	192.5,	191.8,	191.1,	190,	190,	190,	189,	189,	189,	188,	188,	289.2,	275.8,	266.1,	257,	250.1,	244.9,	240.6,	237.2,	234.5,	232,	229.8,	227.7,	226,	224.5,	223.1,	222,	221,	220,	219,	219,	218,	217,	298.4,	290.8,	285,	279.1,	274.8,	270.9,	267.6,	265.2,	263.2,	261.2,	259.7,	258.2,	256.9,	255.9,	255,	254,	253,	252,	307.1,	304.8,	302.5,	300,	297.9,	295.6,	293.8,	292.2,	291.2,	290.2,	289,	288.2,	287,	286,	284,	316.1,	318.9,	320.7,	321.9,	323,	323.3,	323.5,	323.5,	323.3,	323.1,	323,	322,	322,	322,	327.9,	338.2,	347,	355.5,	361.7,	366.8,	371.1,	374.8,	377.8,	380.2,	383,	386,	388,	338.3,	355.8,	370.8,	385.8,	397.1,	408,	418,	426.9,	433.8,	442,	448,	345.9,	370.2,	391.8,	413.7,	432.1,	448.1,	462.4,	474.4,	485,	497,	353.2,	385,	413.9,	442.8,	467.8,	491.1,	512.9,	531,	546,	361.4,	402.1,	439,	477.6,	512.2,	543.8,	573.4,	601,	369.2,	418.4,	464.2,	514.3,	555.7,	593,	630.2,	659,	375.1,	433.5,	487.5,	543.3,	595.2,	639.2,	679.1,	714,	381.1,	448.1,	509.5,	571.3,	624.7,	673.2,	716.5,	752,	385.2,	459.8,	528,	599.5,	659,	718,	779,	824,	388,	467.4,	542.6,	620,	684,	388.9,	469,	547.5,	620,	387.2,	467.6,	545,	612,	382.5,	462.7,	538,	604,	375.7,	454.3,	526,	366,	440.1,	510,	351.3,	396.1,	434.5,	470,	341.2,	377.2,	406.9,	432,	455,	331.9,	355.4,	375,	392.4,	404,	413,	318,	327,	333.3,	334.1,	326.6,	315,	300,	308.8,	305.3,	299.2,	288.7,	272.4,	253.1,	228.3,	202,	168,	130,	89,	46,	299.9,	283.6,	264.2,	243.5,	217,	190.2,	162.6,	134.1,	104.9,	72,	39,	9,	293.5,	271.1,	247.1,	222.8,	193.5,	166,	138.2,	112,	88.2,	62,	34,	4,	289,	261.8,	234.6,	208.8,	180,	151.6,	126.2,	102.3,	79.8,	56.8,	33.2,	6,	284.4,	252.5,	224,	197,	168.8,	141.1,	116.1,	92.5,	71.8,	52.8,	33.3,	9,	279.9,	243.7,	213.2,	184.5,	155.2,	128.8,	105.1,	84.3,	64.8,	48.2,	32,	12,	274.2,	234.3,	202,	170.3,	141,	115.8,	94,	73.5,	58,	43,	28,	269.9,	227.2,	192.8,	162,	132.6,	108.6,	87.4,	69.1,	53,	39,	266,	222.1,	186.1,	155.1,	125,	101.8,	79.8,	65,	53,	263.6,	218.3,	182.6,	151.1,	122,	98.9,	80,	65,	261.7,	217.6,	183.5,	152.7,	125.9,	104.2,	86,	71,	261.6,	220,	187.5,	158.3,	134.3,	113.1,	95,	83,	263.1,	224.6,	193.3,	165.8,	143.2,	122.8,	106.5,	95,	84,	266.3,	231.2,	202.2,	178,	157.6,	139.8,	125.1,	113,	102,	95,	88,	270.8,	239.3,	212.2,	190.8,	171.8,	155.7,	143.1,	131.8,	122.8,	117,	112,	107,	104,	102,	99,	97,	95,	94,	92,	91,	90,	89,	88,	87,	86,	277.7,	252,	231.1,	214.9,	200.5,	190.3,	182.4,	176.5,	173,	170.2,	167.7,	165.8,	164.2,	163.2,	162.1,	161.2,	160.8,	159,	158,	158,	157,	157,	157,	156,	156,	284.7,	266,	251.1,	238.7,	227.8,	220.6,	214.2,	209.2,	206,	203,	200.4,	198.2,	196.3,	195,	193.8,	192.6,	191.8,	190,	190,	189,	189,	188,	188,	188,	188,	292.2,	279.2,	269.1,	261.5,	254.8,	249.8,	244.9,	241,	238,	235.4,	232.9,	230.5,	228.6,	226.8,	225.2,	224.2,	223,	222,	221,	220,	220,	219,	218,	299.7,	292.8,	287,	281.9,	277.2,	273.9,	270.7,	268,	265.7,	263.9,	262,	260.2,	259,	257.9,	256.8,	255.7,	255,	254,	253,	252,	251,	308.8,	307.2,	305.7,	303.7,	302,	300.3,	299.2,	298.1,	296.9,	296.1,	295.3,	294.4,	293.8,	293,	292.2,	292,	291,	290,	289,	317,	321.4,	324.3,	326.9,	328.6,	330.1,	330.9,	332,	332.9,	333.2,	334,	334.1,	334.3,	335,	335,	335,	335,	327.2,	340,	350.1,	359.8,	368.1,	375.4,	381.8,	387.6,	392.9,	396.9,	401.8,	405,	408,	414,	417,	337,	358.6,	376.5,	393,	407.3,	419.9,	431.3,	441.8,	450.3,	457.7,	467,	472,	479,	345,	373.9,	399,	423.4,	444.5,	465.4,	483.1,	499.1,	513,	528,	542,	352.6,	388.9,	421.8,	455.2,	485.1,	513.9,	538,	562.8,	584,	602,	359.1,	402.1,	440.9,	482.1,	519.1,	553.6,	582.8,	611.6,	640,	667,	364.5,	414.8,	459.2,	506.4,	550,	588.4,	620.4,	652,	682,	710,	369,	424,	473.8,	525.1,	573,	615.8,	649.2,	681.7,	714,	747,	372.8,	430.8,	485.4,	539.3,	587.1,	632.2,	670.3,	703,	739,	778,	375.7,	436,	495.4,	547.5,	594.1,	637,	674,	708,	377.1,	437.6,	496.6,	545.6,	590,	629,	377.1,	437.8,	493,	539,	581,	374.7,	434.1,	487.2,	530.5,	572,	370.3,	427.7,	478.4,	521,	560,	364.6,	419.1,	467,	509,	358.9,	408.6,	452.6,	492,	347.6,	387.1,	419,	443,	462,	473,	338.2,	370.8,	396.8,	417.4,	433,	443,	331.2,	353.8,	371.8,	386.8,	398.3,	407,	415,	318.5,	328.1,	335.5,	340,	339.5,	334.8,	327,	315,	303,	310.9,	310,	306.9,	300.8,	290.8,	275.8,	259,	242.2,	221,	192,	165,	142,	106,	58,	301.2,	289.1,	273.5,	256.1,	235.5,	212.8,	190.9,	168.2,	144.6,	123,	100.9,	76,	52.8,	28,	-5,	295.9,	278.1,	258.1,	235.9,	211.5,	184.3,	162.7,	140.2,	118.8,	101.8,	84.1,	61.4,	40.7,	20,	-3,	291.9,	270.2,	246.7,	223.2,	198.9,	170.6,	150,	129.3,	108.6,	92.8,	77,	58.1,	39.2,	20,	2,	288,	262.8,	237.4,	212.4,	187.6,	160.2,	139.8,	121.2,	100.6,	85,	70.2,	55.3,	40,	23,	8,	284,	255.2,	227.8,	200.6,	173.8,	149.2,	128.3,	110.2,	91.5,	76.8,	63.6,	51,	38,	25,	279.9,	248,	218.2,	189,	161.8,	137.9,	117,	99.2,	82.8,	67.5,	56,	47,	36,	276.4,	242.9,	211.3,	181.5,	154,	129.8,	109.2,	92.2,	76.8,	65,	54,	45,	274,	238.4,	206.5,	176,	148,	124.8,	103.3,	88.8,	73,	62,	51,	272.7,	236,	204.8,	173.7,	146.3,	124.7,	102.7,	90,	73,	62,	272.3,	236.3,	206,	175.9,	151.2,	129.9,	109.8,	94,	79,	273.3,	238.8,	210.2,	182.1,	160.1,	139.3,	120.4,	102,	91,	275.3,	242.9,	215.7,	189.3,	168.1,	148.7,	131,	115.5,	103,	92,	278.2,	248.7,	223.5,	199.5,	180.5,	163.4,	147.3,	133.6,	121.8,	112,	104,	98,	281.6,	256.2,	232.5,	210.3,	192.5,	177.3,	162.7,	150.4,	139.2,	128.8,	122,	118,	115,	111,	108,	106,	104,	102,	98,	96,	286.1,	265.7,	247.1,	230.4,	215.8,	203.7,	194.1,	186.1,	179.8,	174.2,	171.3,	168.4,	165.9,	164,	162,	160,	158,	157,	155,	153,	291.1,	275.9,	261.8,	249.7,	238.8,	229.8,	222,	217,	212,	207.5,	204.8,	202,	199.4,	197.1,	195.2,	192,	191,	189,	187,	184,	296.2,	285.5,	276.3,	268.5,	261.9,	255.9,	250.9,	246.7,	243,	239.4,	237.1,	234.8,	232.2,	230.2,	228.5,	226.5,	224,	222,	220,	218,	302.2,	295.8,	290.3,	285.5,	281.4,	277.8,	274.7,	271.8,	269.3,	267,	265.2,	263.5,	261.8,	260,	258.8,	257.4,	256,	255,	254,	254,	309.3,	308.4,	307.6,	306.6,	305.6,	304.5,	303.5,	302.8,	301.6,	301,	300.1,	299.3,	298.6,	297.9,	296.9,	296.2,	295,	295,	294,	293,	316.2,	321,	324.8,	328,	330.6,	333.1,	335.1,	337,	338.6,	340,	341.1,	342.1,	342.8,	343.2,	344,	345,	346,	346,	347,	323.1,	334,	344.2,	353.3,	360.8,	368.3,	374.8,	380.7,	386.5,	392.6,	396.7,	401.1,	404.8,	409,	414,	417,	419,	331,	349.1,	367.1,	383.3,	396,	410.4,	422.5,	433.9,	445.5,	457.1,	465.6,	473,	482,	489,	499,	337.1,	361.2,	385,	407.2,	425.9,	445,	462.9,	479.9,	496.5,	513,	523,	536,	549,	341.7,	371.5,	399.9,	428.2,	452.8,	478.9,	502,	523.4,	546.6,	567.4,	582,	598,	346.1,	380.6,	414.1,	447.2,	477.4,	507.2,	536.9,	562,	589.8,	615,	633,	654,	350.8,	391.6,	429.9,	469,	504.3,	538.5,	573.4,	603.9,	632.9,	658,	678,	699,	353.8,	399,	441.5,	485,	523.5,	560.3,	595.9,	626,	653.8,	680.6,	703,	726,	358.2,	407.8,	453.5,	499.5,	541.8,	580.1,	615.4,	640.9,	671,	698,	726,	756,	362.4,	414.1,	461.2,	507.1,	547.5,	580.9,	614,	639,	669,	365.1,	418.7,	465.1,	507,	543.2,	572.9,	603,	366.2,	420.8,	465.5,	503.8,	535.6,	559,	587,	366,	418.9,	461.8,	496.5,	525,	545,	363.3,	413.8,	454.2,	486.5,	512,	529,	359,	406.9,	445.1,	474.5,	496,	512,	354.2,	398.2,	433.1,	459.5,	481,	494,	342.2,	376.2,	407.2,	430.7,	446.8,	459,	469,	335.2,	362.1,	387.9,	408.8,	422.4,	433.3,	440,	328.9,	348.2,	366.3,	381.5,	392.8,	401.1,	407,	410,	318.8,	327.4,	335.4,	341.2,	345.1,	345,	342.9,	341,	339,	333,	311,	311.1,	310.8,	308,	302.8,	294,	283.8,	270.2,	254.9,	237,	221,	202,	179,	154,	125,	97,	303,	294.3,	284.1,	271,	256.5,	238.5,	221.1,	200.5,	178.2,	157.9,	137.7,	118.8,	99.2,	79.4,	55,	34,	297.8,	284.1,	269,	251.1,	232.9,	210.4,	191.2,	169.5,	148.9,	131.8,	114.4,	95.3,	78.4,	60.9,	40,	20,	294.5,	277.5,	259.8,	239.5,	220,	196.4,	177.6,	157.1,	137.2,	121.2,	105,	87.8,	73,	58.5,	41,	21,	291,	271.1,	251.9,	229.7,	209.5,	185.2,	167.1,	146.9,	127.5,	112,	95.8,	81.1,	69,	57.2,	41,	23,	288,	265.9,	244.8,	220.5,	198,	173.5,	155.9,	134.8,	116.5,	100.5,	86.1,	73.8,	64,	53,	42,	27,	284.1,	259.1,	236,	210,	185,	161.4,	144.8,	124.3,	104.6,	90.4,	78.1,	67,	58,	50,	42,	281.2,	255,	229.2,	203,	177.6,	153.7,	136.4,	116.7,	98.2,	83,	71,	62,	55,	279.6,	251.2,	223.4,	197,	171.6,	148.5,	130.8,	110.8,	93,	79,	66,	58,	279.1,	249.2,	221,	194.7,	169.7,	146.1,	128.3,	109,	94,	77,	279.4,	249.3,	221.5,	195.8,	172.9,	150.5,	132,	113.2,	98,	86,	280.3,	251.1,	224.8,	200.7,	179.2,	158.4,	140.4,	123,	109,	98,	282.1,	254.7,	229.9,	206.7,	186,	166.6,	149.2,	132.6,	120.3,	108,	96,	284.7,	260,	236.5,	215.7,	196.8,	179.3,	164.2,	149.5,	136.3,	124,	114,	103,	288.2,	266.2,	244.7,	225.5,	208,	191.8,	177.3,	163.8,	151.8,	142,	134,	124,	116,	109,	104,	97,	291.8,	273.9,	256.3,	241.7,	228.5,	215.7,	204.2,	194.5,	186.2,	179.4,	175,	170,	165,	162,	158,	154,	150,	295.9,	282.1,	268.6,	257.2,	247.8,	238.4,	229.9,	222.4,	217.4,	212.1,	208.2,	204,	200,	197,	193,	189,	186,	183,	300,	289.8,	280.6,	272.8,	266.5,	260.8,	256,	251.5,	247.6,	243.8,	240.2,	237.2,	234.8,	231,	229,	226,	223,	221,	219,	304.5,	298.6,	293.2,	288.5,	284.5,	280.6,	277.5,	274.4,	271.8,	269.4,	267.3,	265.2,	263.5,	261.8,	259,	256,	255,	253,	252,	251,	310.3,	310,	309.3,	308.7,	308,	307.1,	306.8,	306,	305.2,	304.2,	303.8,	303,	302.2,	301.8,	301,	300,	300,	299,	298,	298,	314.8,	319.8,	324.3,	328,	330.8,	333.5,	336,	338.2,	340.1,	342.2,	343.7,	345,	346.8,	347.8,	349,	350,	351,	352,	353,	354,	319.9,	329.8,	339.6,	349,	356,	363.5,	370.3,	376.3,	382.1,	387.3,	392.4,	396.5,	401.1,	405,	409,	413,	416,	421,	325.6,	342.1,	358.5,	374.8,	388,	402.2,	414.2,	426.1,	437.2,	448.4,	458.1,	468.3,	476,	483,	491,	498,	329.6,	351.5,	372.6,	393.2,	410.8,	430.3,	445.4,	461.7,	476.1,	491.5,	504.5,	517,	526,	536,	546,	333.2,	359.4,	385.1,	410.5,	433.2,	457.9,	476.7,	498.6,	518.5,	539.6,	554,	570,	582,	593,	336,	366,	396,	425.2,	453.3,	482,	504.7,	530,	554,	578.4,	593,	607,	620,	631,	339.2,	374,	407.8,	441.3,	474.7,	507.1,	534.1,	563.7,	591.8,	614.2,	634,	648,	662,	674,	342.5,	380.6,	418,	456.3,	492.7,	528,	559,	590.1,	616.1,	638.8,	661,	680,	692,	708,	346.5,	387.9,	429.9,	471.3,	511.3,	548.1,	577.1,	603.7,	629.7,	655,	680,	702,	721,	350.6,	392.5,	436.5,	479.5,	517.5,	548.2,	573.1,	593.3,	617,	636,	353,	396.8,	442,	483,	516.1,	542.2,	564.2,	584,	601,	354,	399.1,	444,	482,	510.8,	533.5,	550.6,	568,	354.6,	399.5,	442.8,	477,	502.5,	521.1,	539,	552,	353.4,	396.8,	438,	468.5,	490.5,	508.2,	523,	350,	391.5,	430.2,	457.9,	477.7,	493.2,	507,	347,	385,	419.9,	445,	463.2,	476.7,	489,	339.8,	367.9,	396,	420.1,	437.2,	448.8,	459.3,	465,	334.2,	357.2,	379.9,	400.6,	415.9,	426.9,	435.4,	439,	328.8,	346.1,	362.2,	377.2,	389.1,	398,	404.2,	409,	413,	319.3,	327.5,	335.1,	341.8,	346.3,	348.8,	349.8,	349.8,	349,	348,	347,	311.2,	312.4,	312.8,	311.6,	308.6,	303.7,	296.2,	287.2,	276.3,	264.8,	253,	238,	225,	208,	188,	171,	303.9,	296.7,	289.2,	279.9,	269,	257.4,	242.6,	227.8,	210.2,	192.2,	173.9,	153.6,	134,	114.5,	92,	69,	47,	298.8,	286.8,	274.8,	261.2,	246.6,	229.3,	213,	196,	178.5,	160.9,	143.2,	125.2,	107.9,	90.8,	71,	53,	31,	295.8,	280.7,	266.2,	251,	235,	217.1,	200.1,	183.2,	165.4,	148.5,	132.5,	115.9,	101,	85.8,	68,	52,	34,	292.9,	274.9,	259.1,	242,	224.7,	206,	189.5,	172.2,	155.1,	138.2,	123,	107,	94.1,	81,	66,	52,	35,	290.2,	270.2,	252.6,	233.2,	214.8,	195.4,	177.9,	160,	142.8,	126.9,	112,	96,	83,	74,	62,	50,	287.2,	264.8,	244.1,	223.6,	203.7,	184.4,	166.2,	149.1,	132.5,	116.8,	101,	86,	75,	68,	60,	284.9,	260.4,	238.4,	217.1,	196.1,	176.2,	158.5,	140.8,	124.8,	108,	92,	78,	68,	283.7,	257.8,	233.5,	211.6,	190.9,	169.8,	151.8,	133.7,	118.1,	101,	86,	72,	283.5,	257.1,	231.2,	208,	187.9,	166,	148,	129.4,	114,	99,	85,	284.2,	257.9,	232,	208.8,	188.3,	168.5,	149.6,	131,	117,	102,	285.4,	260.2,	235.2,	213.2,	193.4,	173.4,	155.6,	137.6,	123,	111,	100,	287.1,	263.7,	239.9,	218.9,	200,	180.3,	162.9,	145.4,	131,	120,	111,	99,	289.7,	268.4,	246.5,	227.4,	209.5,	191.3,	175.4,	159,	148,	137,	127,	117,	292.3,	273.4,	253.3,	236,	219.7,	202.6,	187.3,	171,	162,	151,	143,	135,	126,	295.5,	279.8,	263.8,	250.5,	237.8,	224.1,	211.9,	199,	192,	185,	179,	173,	166,	298.8,	286.3,	274,	263.7,	254,	244,	235.2,	226.5,	221,	214,	208,	204,	199,	194,	301.6,	293.2,	284.2,	277,	270.3,	264.7,	259.3,	254.8,	250.4,	246,	242,	239,	235,	231,	228,	225,	305,	300.1,	295,	290.5,	286.2,	282.9,	279.4,	276.1,	273.1,	270.2,	267,	265,	263,	260,	258,	256,	254,	252,	310.7,	310.7,	310.1,	309.9,	309.2,	309,	308.4,	308,	307.5,	306.9,	306.2,	305.8,	305,	304,	303,	302,	301,	301,	314.6,	318.1,	322.6,	325.9,	329.3,	332.1,	334.9,	337,	338.8,	340.9,	342.6,	344.1,	345.7,	347,	349,	350,	351,	352,	318.8,	327.2,	336.2,	343.7,	350.9,	358.2,	365.2,	371.8,	377.3,	383.3,	387.7,	392.7,	397,	401,	406,	410,	413,	416,	323.2,	337.1,	352,	364.8,	376.9,	390,	402.3,	413.6,	424.5,	436.8,	444.9,	452,	462,	468,	474,	481,	326.1,	343.9,	363.5,	379.1,	396,	412.5,	428.5,	444.8,	458.1,	473.5,	486,	496,	505,	513,	519,	329.2,	350.8,	374,	393,	415,	436,	455.2,	478.1,	496.1,	513,	527,	541,	552,	563,	331.8,	356.6,	383.2,	406.5,	432,	456.8,	479,	504.1,	526.2,	545,	561,	574,	588,	597,	334.3,	362.8,	392.1,	418.7,	448,	476,	502,	529.7,	555.2,	575,	593,	607,	618,	632,	643,	338.1,	369.2,	400,	431.8,	465.5,	496.1,	526.5,	556,	582.9,	603,	625,	639,	652,	667,	679,	341.7,	376.8,	410.3,	444.9,	481.2,	515,	546.8,	574.1,	600.9,	624,	646,	664,	679,	696,	714,	345.3,	380.6,	418,	453.3,	489.1,	521.5,	548.8,	569.8,	587.9,	604,	614,	347.4,	384,	422.9,	459.2,	492.1,	519.9,	542.3,	559.7,	571.5,	585,	348.7,	386,	424.2,	459.6,	490.4,	514.5,	532,	546.8,	555,	564,	349.1,	386.1,	424,	457,	484.3,	505,	520,	531,	539,	348,	384,	420.3,	451.7,	476,	492.8,	506.1,	516,	523,	345.7,	379.4,	414,	442.6,	463.9,	478,	490.5,	499,	343.1,	374.5,	406,	432.1,	451.2,	463.8,	475.4,	482,	336.9,	362.4,	386.4,	409,	428.9,	442,	451.6,	458.2,	462,	332.8,	353.4,	372.8,	391.9,	409.1,	421.3,	430.9,	436.6,	439,	328.4,	343.7,	358.1,	372.2,	385.2,	394.9,	402.7,	407.6,	410,	413,	319,	326.7,	334.1,	340.6,	346.1,	350.2,	353.2,	354.9,	355.5,	356,	356,	356,	311.7,	313.3,	314.2,	314,	312.3,	309.2,	304.7,	298.1,	290.5,	281.6,	272.8,	262,	252,	242,	233,	224,	304.7,	299.2,	293.3,	286.1,	277.5,	267.2,	256.8,	244.8,	232.8,	218.1,	202.9,	187.5,	168.9,	149,	126,	106,	81,	300.1,	290.2,	280.1,	268.7,	255.4,	241.6,	226.2,	211.1,	196.7,	180.5,	165.9,	152.1,	139.7,	123,	105,	88,	69,	297.2,	285,	272.8,	259.5,	244.5,	229.5,	213.9,	198.2,	184.1,	168.8,	153.9,	141.5,	130.3,	115,	99,	84,	69,	294.5,	280.3,	266.2,	251.3,	235.2,	219.5,	203.3,	188.1,	173.4,	158.9,	143.4,	131,	121,	108,	94,	83,	292.7,	276.4,	260.8,	243.9,	226.4,	210.2,	193.2,	178.8,	162.6,	149,	133.4,	122,	111,	99,	88,	79,	289.8,	271.2,	254.3,	235.4,	216.3,	199.7,	183.8,	167.5,	151.5,	138,	122,	110,	101,	92,	83,	287.8,	267.1,	249,	229.2,	209.4,	191.4,	175.1,	158.4,	142.7,	130,	114,	102,	94,	286.9,	264.2,	244.8,	223.5,	203.5,	184.1,	167.1,	148.9,	134,	122,	108,	94,	286.7,	262.9,	241.8,	220.8,	199.4,	179.7,	162.4,	143.5,	130,	117,	104,	287.5,	263.3,	241,	220.4,	198.6,	177.8,	161.5,	145,	128,	114,	288.8,	265.1,	243.6,	222.5,	201.6,	181.8,	165,	149,	132,	121,	290.8,	268.5,	247.8,	227.7,	207.8,	188.3,	172,	156,	141,	128,	293.2,	272.9,	253.8,	235.2,	216.2,	199,	183,	168,	156,	295.2,	277.3,	259.6,	242.7,	225.4,	208,	194,	181,	170,	298.2,	283.3,	268.7,	254.6,	241,	228,	217,	207,	198,	188,	300.5,	288.6,	277.6,	267,	256.3,	246.5,	239,	232,	225,	219,	212,	303.1,	295,	287.3,	279.9,	272.9,	266.4,	261,	256,	252,	248,	244,	240,	305.9,	300.9,	296.1,	291.8,	287.2,	283.3,	280.1,	277,	274,	271,	268,	265,	262,	260,	310.9,	311.1,	311.1,	310.9,	310.8,	310.4,	310.1,	309.9,	309.3,	308,	308,	307,	306,	305,	304,	313.8,	318.1,	322.1,	325.6,	328.8,	331.4,	334.1,	336.8,	339.1,	341,	343,	344,	344,	345,	346,	317,	325.4,	333.8,	341.7,	348.7,	355.5,	362,	368.8,	375.1,	381.1,	385,	389,	394,	398,	402,	320.6,	333.2,	347,	360.3,	371.3,	384.1,	395.8,	407.6,	418.6,	426,	436,	443,	452,	460,	323.2,	338.9,	356.2,	372.2,	387.1,	404,	419.5,	434.6,	448,	460,	471,	481,	491,	325.8,	344.6,	364.8,	385.1,	404,	426,	445.6,	464.8,	483,	498,	511,	526,	328.4,	349.9,	372.8,	396.1,	418.3,	443.5,	466,	488.5,	507,	523,	536,	553,	330.6,	355.2,	380.5,	406.7,	432,	459.5,	484.8,	510,	530,	547,	563,	580,	333.5,	361.1,	388.8,	419.6,	447,	477.7,	505.9,	534.1,	554,	573,	592,	607,	622,	336,	367.1,	398.4,	430.8,	460,	493,	523.4,	551.9,	571,	592,	614,	630,	647,	339.2,	371.5,	405.3,	437.1,	467.1,	500.1,	529.7,	552.2,	569.5,	582.4,	594,	602,	342.1,	375,	409.1,	440.2,	471.1,	500.7,	525.2,	543.7,	556.4,	565.7,	575,	343.7,	377.2,	410.7,	441.5,	470.4,	497,	517.4,	531.9,	541.7,	548,	556,	344.3,	377.8,	410.2,	439.9,	466.7,	490,	507.4,	518.8,	527.6,	532,	343.6,	376.1,	407.3,	435.3,	460.6,	480.6,	495,	504.9,	511,	516,	341.9,	371.8,	400.9,	427.1,	450.9,	467.7,	479.1,	487.5,	493,	498,	339.6,	367.7,	394.3,	418.4,	440,	454.7,	465.2,	472.8,	477,	335.9,	358.1,	380.3,	400.8,	419,	434.1,	445,	452.5,	457,	461,	332.7,	350.4,	369,	385.8,	402.1,	415.4,	426.1,	432.7,	437.1,	440,	443,	328.4,	343.3,	357.3,	369.6,	381.6,	392.4,	401.1,	406.1,	410.4,	412.7,	415,	319.4,	326.6,	333.9,	340.8,	346.3,	351.1,	354.6,	356.9,	358.5,	359.2,	360,	360,	361,	361,	312.1,	314,	315,	314.9,	314,	312.4,	309.1,	304.3,	298.7,	291.8,	284.6,	278.1,	271,	262,	252,	240,	305.3,	300.9,	295.2,	289.6,	282.9,	274.3,	266.1,	256.3,	245.1,	233.9,	222.1,	209.1,	196,	180,	163,	144,	300.9,	292.4,	282.2,	272.3,	261.3,	248.9,	236.8,	224,	210.3,	195.6,	182.1,	168,	151,	134,	115,	96,	298.1,	287.4,	275.4,	263.9,	251.5,	238,	225.4,	212,	198,	184.5,	170,	155,	139,	123,	105,	295.7,	282.8,	269.3,	256.4,	243,	228.2,	214.8,	201.2,	186.6,	173.4,	159,	142,	127,	112,	294,	279.1,	264.7,	250,	235.2,	219.6,	205.7,	191.5,	175.9,	162,	148,	132,	116,	291.9,	275.2,	258.8,	241.9,	226.4,	210.1,	195.8,	181.4,	165,	151,	138,	122,	105,	290,	271.8,	252.5,	235.2,	218.4,	201,	186.8,	172.1,	155,	142,	127,	111,	289.4,	268.6,	248.9,	230.2,	212,	193.7,	178.8,	161,	145,	133,	118,	289.7,	266.8,	246.2,	226.4,	206.6,	187.7,	172,	152,	137,	123,	290.8,	267.1,	245.7,	223.7,	204,	182,	166,	148,	132,	292.2,	268.8,	247.2,	225.2,	205,	184,	168,	151,	293.5,	271.8,	251.2,	229.4,	210,	190,	174,	295.7,	275.8,	256.2,	237,	218,	202,	185,	297.4,	279.8,	261.4,	244,	227,	214,	200,	300.3,	285.6,	270.2,	255,	241,	232,	221,	302.7,	291.1,	279.2,	267.7,	257,	248,	240,	233,	304.8,	296.2,	288.1,	280,	274,	268,	263,	259,	255,	306.5,	301.2,	296.3,	291.4,	287,	283,	280,	278,	276,	273,	270,	310.7,	311.4,	311.4,	311.6,	311.6,	311.7,	311,	311,	311,	311,	311,	311,	311,	313.1,	317.5,	321.3,	325,	328.2,	331.2,	334.2,	337,	339,	341,	344,	346,	348,	315.4,	323.9,	332.7,	340.6,	347.9,	355.2,	362.1,	369,	373,	379,	385,	389,	394,	318,	330.8,	344,	357,	368.5,	381.8,	393,	402,	411,	420,	429,	437,	320,	336,	352.1,	368.2,	383,	400.2,	414,	426,	438,	449,	460,	321.8,	341.2,	360,	380,	398.3,	422,	439,	453,	469,	484,	323.6,	346,	367.1,	390,	412.5,	439,	460,	476,	493,	510,	325.4,	351,	374.3,	400.1,	424.9,	454,	476,	494,	513,	327.7,	356.4,	383,	411.8,	438.8,	470,	492,	514,	536,	553,	330.1,	362.1,	391,	421.2,	449,	481,	507,	529,	551,	569,	588,	607,	625,	333.4,	366.7,	396,	427.5,	455.2,	485.2,	513,	533,	551,	566,	580,	592,	602,	337.3,	369,	398.8,	431,	457.6,	484.9,	508.8,	530,	542,	553,	561,	571,	578,	339.5,	369.9,	400,	430.6,	456.8,	481.6,	502.5,	519.5,	531.6,	539.1,	546,	553,	340.7,	370.1,	399.4,	428,	452.7,	475.3,	494,	507.9,	517.9,	524.5,	530,	340.6,	368.4,	396.9,	423.1,	446.9,	467.8,	484.2,	495.7,	503.3,	509.1,	514,	339.4,	365,	391.3,	415.8,	437.6,	456.2,	469.9,	479.1,	484.7,	490,	493,	337.9,	362.2,	386.2,	408.8,	428.3,	445.5,	457.4,	465.8,	470.9,	475,	334.9,	355.8,	376.1,	395.7,	412,	427.1,	439.3,	447.7,	454,	457,	462,	332.1,	349.9,	367,	383.4,	397.3,	410.8,	421.2,	428.8,	435.4,	438,	442,	328.4,	343.7,	357.2,	369.8,	381,	391.1,	399.3,	405.8,	410.8,	414,	417,	319.8,	327.4,	335.1,	341.4,	347.1,	351.8,	355.1,	358.1,	360.2,	361,	362,	360,	360,	360,	312.4,	314.4,	315.3,	315.7,	315.5,	313.9,	311.5,	307.9,	303.2,	298,	292,	284,	277,	268,	305.8,	301.8,	296.6,	291.2,	285.1,	278.6,	271.1,	263,	253,	242,	230,	219,	206,	192,	301.7,	293.3,	283.2,	273.5,	263.9,	252.8,	242,	229,	214,	200,	185,	170,	157,	143,	298.7,	288.2,	276.3,	265.2,	254.5,	241.9,	229,	216,	201,	187,	174,	160,	296.5,	284,	270.3,	257.4,	245.7,	232.5,	217,	203,	188,	175,	161,	147,	294.7,	280.5,	265.2,	250.9,	238.2,	222,	208,	194,	178,	166,	152,	293,	276.8,	259.9,	243.7,	230.1,	212,	196,	180,	164,	150,	291.1,	272.8,	254.3,	236.1,	221.5,	203,	187,	170,	156,	290.7,	270,	250.1,	232,	214,	196,	180,	162,	147,	290.9,	268,	247,	228,	211,	193,	176,	158,	291.9,	267.5,	246,	228,	210,	191,	175,	157,	293.7,	268.8,	247,	228,	211,	194,	294.9,	271.2,	250,	233,	216,	297.5,	276,	256,	239,	224,	299.1,	281,	262,	247,	232,	301.5,	286,	271,	257,	247,	303.8,	291,	278,	267,	256,	305,	296.3,	288,	280,	273,	306.7,	300.3,	296,	292,	286,	284,	281,	310.7,	311.7,	312,	311,	310,	308,	306,	305,	312.8,	317.6,	321.8,	326,	330,	334,	336,	339,	314.9,	323.4,	332.2,	340,	349,	356,	364,	371,	317.2,	330.1,	343.1,	355,	368,	380,	389,	400,	319,	335,	351.2,	364,	382,	397,	410,	320.5,	340,	359,	376,	394,	414,	431,	322,	344.5,	366.5,	384,	406,	429,	448,	324,	349.5,	373.4,	395,	418,	444,	326.3,	355.1,	381.2,	407,	431,	457,	480,	328.4,	360,	388,	416,	441,	465,	490,	510,	332,	364.1,	392.7,	422,	446,	469,	492,	513,	532,	335.3,	366.8,	394.8,	422,	448,	470,	491,	510,	525,	541,	550,	559,	567,	338,	367.9,	395,	422,	447,	468,	486,	503,	517,	529,	537,	545,	553,	339.2,	367.7,	394.1,	419.9,	444,	463,	480,	495,	506,	516,	524,	532,	339,	365.5,	391,	415.4,	437,	456.9,	473,	486,	494,	501,	509,	515,	337.8,	362.1,	385.8,	408,	427.5,	445.5,	460.2,	471.1,	478.2,	483,	489,	495,	336.5,	359.1,	381.1,	401.9,	420.1,	436.9,	450.3,	459.5,	466.3,	471,	476,	334,	355,	371,	390,	405,	420,	433,	442,	450,	455,	460,	332,	348,	365,	380,	393,	406,	416,	424,	432,	436,	440,	328,	343,	356,	370,	380,	391,	398,	405,	410,	413,	416,	320,	327,	334,	342,	347,	351,	354,	357,	360,	361,	360,	312,	314,	314,	314,	314,	314,	313,	310,	307,	303,	299,	306,	303,	298,	293,	286,	282,	276,	270,	262,	254,	248,	301,	293,	283,	274,	265,	256,	245,	235,	222,	298,	288,	277,	266,	256,	246,	235,	224,	296,	284,	271,	258,	248,	235,	223,	294,	280,	265,	251,	241,	227,	213,	293,	277,	262,	246,	233,	220,	203,	293,	273,	256,	238,	224,	209,	293,	270,	251,	234,	219,	203,	293,	270,	249,	232,	215,	294,	268,	248,	230,	295,	269,	249,	296,	272,	251,	298,	278,	260,	299,	282,	265,	302,	286,	273,	303,	291,	280,	305,	296,	289,	307,	302,	298,	294,	310,	311,	311,	311,	313,	317,	321,	326,	328,	315,	323,	332,	339,	344,	318,	332,	345,	355,	363,	320,	335,	351,	365,	322,	342,	361,	378,	324,	345,	366,	385,	326,	348,	372,	392,	329,	354,	380,	402,	332,	359,	386,	411,	334,	362,	389,	415,	336,	364,	391,	416,	440,	337,	366,	391,	415,	439,	455,	338,	366,	389,	412,	435,	452,	467,	483,	337,	363,	385,	407,	428,	446,	463,	476,	487,	496,	503,	336,	360,	381,	400,	418,	435,	450,	462,	472,	478,	484,	335,	357,	376,	395,	412,	427,	442,	453,	462,	467,	472,

};
const double Munsell_y[5000] =//*1000
{
1414,	1145,	837,	2160,	676,	1233,	584,	891,	516,	719,	458,	575,	397,	425,	332,	325,	261,	223,	226,	181,	195,	148,	177,	128,	164,	114,	157,	109,	149,	102,	74,	143,	94,	67,	136,	90,	62,	45,	32,	18,	130,	87,	61,	45,	34,	25,	18,	12,	6,	1,	126,	88,	64,	49,	40,	31,	24,	18,	12,	6,	127,	90,	67,	52,	43,	35,	28,	22,	16,	129,	93,	72,	56,	47,	39,	32,	131,	94,	74,	58,	49,	40,	134,	97,	75,	60,	51,	137,	100,	78,	64,	53,	143,	106,	84,	69,	152,	115,	93,	77,	164,	125,	103,	183,	142,	119,	204,	159,	224,	178,	246,	198,	290,	245,	375,	355,	480,	740,	970,	1284,	1459,	670,	590,	1820,	528,	1467,	482,	1141,	445,	906,	423,	743,	399,	604,	380,	501,	361,	405,	332,	334,	298,	259,	275,	220,	193,	249,	187,	149,	236,	172,	134,	223,	159,	119,	213,	151,	111,	203,	145,	105,	67,	196,	139,	98,	65,	188,	134,	95,	66,	53,	42,	36,	30,	25,	18,	180,	130,	95,	72,	58,	46,	40,	32,	26,	20,	16,	10,	5,	1,	176,	131,	100,	78,	62,	50,	43,	36,	29,	24,	18,	13,	8,	4,	177,	134,	104,	82,	64,	54,	45,	38,	31,	26,	20,	16,	10,	182,	138,	110,	87,	69,	57,	48,	40,	34,	28,	185,	142,	114,	91,	72,	59,	49,	42,	34,	189,	146,	119,	95,	73,	61,	52,	43,	193,	151,	123,	100,	78,	65,	54,	199,	158,	131,	106,	82,	70,	209,	169,	141,	117,	90,	220,	181,	153,	128,	98,	236,	198,	170,	143,	113,	255,	219,	190,	161,	272,	238,	208,	176,	289,	258,	229,	196,	324,	298,	270,	241,	367,	362,	344,	420,	452,	499,	588,	656,	700,	501,	1223,	468,	1169,	442,	995,	420,	792,	1459,	399,	652,	1125,	388,	561,	889,	376,	493,	682,	363,	440,	541,	349,	389,	425,	334,	341,	334,	311,	284,	243,	295,	254,	211,	276,	221,	176,	260,	202,	157,	111,	245,	187,	145,	100,	235,	178,	136,	96,	227,	172,	128,	91,	221,	165,	122,	90,	68,	55,	215,	160,	118,	88,	69,	57,	48,	40,	32,	24,	18,	208,	155,	117,	92,	74,	62,	53,	45,	37,	30,	25,	20,	14,	8,	4,	204,	157,	124,	98,	82,	70,	58,	50,	42,	35,	29,	24,	18,	12,	6,	205,	160,	129,	104,	88,	75,	60,	53,	44,	38,	32,	27,	21,	14,	207,	166,	136,	110,	94,	80,	65,	56,	46,	41,	34,	30,	211,	170,	141,	115,	98,	83,	67,	56,	49,	42,	214,	174,	145,	119,	101,	87,	69,	60,	51,	218,	179,	151,	125,	106,	92,	73,	62,	226,	189,	159,	135,	114,	99,	80,	68,	236,	200,	170,	146,	124,	106,	87,	247,	211,	182,	158,	135,	114,	260,	227,	200,	176,	154,	126,	274,	246,	221,	197,	176,	290,	264,	240,	214,	192,	305,	284,	261,	235,	214,	332,	322,	303,	282,	266,	367,	371,	362,	347,	397,	441,	444,	587,	491,	734,	526,	531,	1074,	448,	998,	425,	907,	410,	738,	1266,	394,	613,	969,	1632,	381,	537,	791,	1220,	371,	488,	660,	967,	363,	447,	553,	737,	355,	414,	476,	561,	346,	382,	408,	428,	332,	342,	341,	330,	315,	299,	275,	239,	296,	264,	233,	198,	280,	237,	199,	164,	271,	222,	181,	147,	258,	207,	163,	127,	249,	196,	153,	120,	89,	241,	187,	145,	112,	83,	234,	181,	138,	105,	81,	58,	226,	174,	132,	102,	83,	60,	45,	34,	26,	18,	221,	170,	131,	104,	89,	72,	56,	46,	39,	32,	25,	18,	13,	9,	5,	2,	-1,	-4,	219,	170,	137,	112,	97,	81,	68,	57,	50,	43,	36,	30,	26,	21,	18,	14,	10,	8,	220,	174,	144,	120,	105,	90,	76,	64,	57,	50,	44,	38,	34,	28,	26,	22,	19,	15,	224,	179,	151,	127,	112,	95,	80,	69,	62,	56,	49,	44,	39,	35,	30,	228,	184,	156,	132,	118,	101,	84,	72,	66,	60,	54,	49,	44,	232,	189,	162,	137,	123,	106,	89,	78,	71,	65,	59,	53,	236,	195,	168,	144,	129,	111,	93,	84,	77,	70,	65,	243,	206,	179,	154,	139,	120,	105,	94,	86,	81,	251,	217,	190,	165,	149,	132,	115,	104,	261,	230,	203,	177,	160,	143,	127,	272,	245,	219,	195,	177,	159,	143,	286,	261,	237,	214,	197,	180,	297,	276,	255,	234,	216,	199,	309,	296,	280,	259,	240,	218,	333,	330,	320,	306,	288,	273,	361,	376,	376,	372,	386,	430,	450,	411,	534,	439,	640,	457,	790,	460,	936,	421.2,	600,	408.8,	615,	723,	398.2,	594.2,	740,	384,	538,	706,	944,	372,	490.3,	639.2,	883,	1065,	363.4,	448.9,	561.9,	689.6,	776,	356.4,	421.8,	499.6,	571,	620,	348.4,	396.7,	450.5,	494.3,	528,	340.7,	372.4,	401.9,	415.8,	424,	328.9,	340.6,	345.2,	345.8,	344,	314.1,	302.1,	286,	264,	237,	302.3,	276.8,	248.5,	218,	288.2,	249.6,	212.9,	180,	278.1,	232.4,	190.8,	155,	267.7,	216.8,	174.5,	139,	103,	257.9,	204.8,	163.9,	128,	97,	249.1,	197.4,	156.3,	121.8,	94,	242,	191.1,	149.1,	115.7,	90,	68,	56,	236.8,	186.7,	144.7,	112.4,	87,	70,	60,	50,	44,	38,	34,	30,	27,	24,	22,	20,	18,	16,	14,	12,	10,	8,	231,	182.1,	142.2,	114.1,	95,	80.4,	68.8,	58.3,	51.8,	45.4,	40.2,	35.2,	30.9,	27,	23.4,	20.2,	18,	16,	14,	12,	10,	8,	6,	4,	228,	182.8,	147,	122.8,	102.9,	86.9,	74.5,	63.8,	56.4,	49.3,	43.6,	38,	32.6,	28.1,	24,	21,	19,	16,	14,	13,	11,	9,	7,	5,	229.6,	187.4,	155.9,	130.3,	111.2,	94,	81,	69.6,	61.8,	54.2,	47.3,	41.8,	35.5,	31,	26,	22,	20,	18,	15,	13,	11,	233,	192.7,	162.8,	137.5,	117.8,	100.6,	86.3,	74.6,	66.7,	58.6,	50.9,	46,	39,	34,	28,	24,	236.1,	197.4,	168.2,	142.9,	122.9,	105.9,	90.3,	79,	70.6,	62.5,	55,	49,	42,	240.4,	203.2,	173.7,	148.1,	128.2,	111,	95.2,	83.9,	74.8,	66,	58,	51,	245.9,	209.5,	181.1,	155.1,	135.1,	118.1,	102,	90.2,	80,	70,	61,	254.2,	219.6,	192,	166.2,	145.8,	128.3,	113.8,	102,	92,	79,	261.7,	230,	203.6,	179.3,	158,	140,	125,	113,	271,	242.3,	216.9,	192.1,	171,	151.4,	136,	124,	281.6,	256.9,	232.9,	210.3,	190,	169,	156,	292.9,	272.8,	251.5,	229.7,	210,	187,	303.4,	288.8,	269.8,	248.7,	229,	204,	315.4,	306.8,	292.1,	271.3,	249.9,	227,	334.4,	337.1,	327,	305.8,	279,	248,	358,	379.5,	384,	385,	385,	377.5,	422,	442,	398.2,	464,	417.7,	505,	426.5,	543,	428.7,	570,	384.8,	478.9,	576,	380.3,	475.2,	590,	716,	374.3,	465,	574.8,	710,	825,	365,	445.7,	537.9,	650.9,	768,	879,	1010,	358,	424,	497.2,	583.7,	681.4,	779.8,	876,	998,	1110,	350.7,	399.8,	452.2,	504.2,	569.8,	630.8,	686,	735.8,	788,	345,	384.5,	423.1,	458.3,	498.1,	535.8,	569.1,	598.6,	625,	340,	370.5,	398.3,	424.4,	450.5,	475.9,	497.3,	515.3,	532,	334.1,	355.9,	373.9,	391.1,	405.9,	418.3,	427,	432.7,	441,	327.1,	337.8,	345.2,	351.7,	355.1,	357.6,	358.8,	359,	360,	317.5,	315,	311,	303.7,	295.6,	288,	280,	274,	309.8,	298.1,	285.3,	271,	258.2,	247.8,	237,	228,	301,	279,	257,	233.1,	213.3,	194,	181,	294,	264.9,	235.8,	207.6,	182.8,	165,	287.4,	251.8,	216.2,	182.7,	155.8,	136,	279.9,	240,	202.6,	169.2,	142.2,	124,	106,	272.5,	231.3,	193.7,	160.3,	134.6,	115,	97,	267.5,	224.5,	185.7,	153,	127.8,	107.6,	89,	76,	64,	262.4,	219.2,	181.1,	149.1,	124,	104.8,	87.3,	74,	64,	58,	52,	46,	42,	40,	38,	36,	33,	31,	28,	25,	22,	19,	16,	258.2,	214.8,	179,	149.5,	125.8,	109.4,	95.5,	83.9,	74.2,	66.6,	59.4,	53.8,	49.2,	45.1,	40.9,	37.3,	34,	31,	28,	25,	22,	19,	16,	13,	10,	256.7,	216.2,	184,	155.1,	133,	117,	102.6,	91,	80.8,	71.8,	64.3,	57.8,	52,	47.1,	42,	37.9,	34.4,	31,	28,	25,	22,	19,	16,	13,	10,	258.3,	220.8,	192.1,	163.5,	142.2,	124.5,	110,	98,	87.3,	77.9,	69.6,	62.1,	55.5,	49.1,	43.2,	38,	35,	31,	28,	25,	22,	19,	261.2,	226.1,	199.2,	170.7,	150,	132,	116.3,	104.5,	93.5,	83.8,	75,	66.9,	59.4,	52.5,	45,	39,	35,	31,	264.7,	232.1,	205.8,	178.1,	156.9,	139.2,	123.5,	110.6,	99.5,	90.1,	79.9,	71.9,	64,	55,	46,	269.1,	239,	213.2,	186.2,	165.9,	147.7,	131.7,	118.1,	106.3,	96.2,	86.1,	76,	68,	58,	275.4,	249.6,	225.9,	200.3,	180,	161.8,	144.9,	131,	118.8,	108,	96,	86,	76,	282.9,	259.7,	238,	214,	193.9,	176.4,	159.8,	145.4,	134,	121,	110,	289.2,	268.3,	249,	227.6,	208.2,	190.3,	173.7,	159.5,	146,	132,	295.7,	277.8,	260.8,	241.9,	223.7,	206,	188.8,	174,	161,	303.3,	290,	276,	259.3,	242.8,	225.4,	208.3,	192,	311.1,	303.2,	293.4,	280,	263.3,	246.5,	228.7,	212,	318.1,	316.9,	312.3,	302.7,	287.4,	270.4,	252,	234,	327.4,	333,	333.1,	325.9,	312,	293.7,	273.4,	254,	336.5,	350.8,	358.1,	359,	350,	340,	323,	309,	347.6,	373.8,	392.5,	406,	415,	359,	396.4,	427.1,	456,	368.8,	416.8,	458,	493,	378.5,	439.2,	486,	526,	383.9,	457.3,	519,	385.8,	472.3,	547,	378.9,	445.2,	502.6,	555,	376.8,	448.4,	511,	576,	638,	372.9,	442.9,	510.9,	583.2,	650,	718,	364.4,	428.8,	496.7,	570,	644.8,	720,	793,	357.8,	412.3,	471.7,	536.1,	602.6,	670,	742.3,	807,	880,	948,	1014,	1078,	350,	391.5,	434.2,	475.2,	521.1,	564.2,	605.2,	642,	676.6,	712.7,	746.8,	776,	343.9,	378,	410,	438,	468.2,	494.8,	519.7,	541.4,	560.5,	580.2,	601.1,	622,	339.1,	366.7,	390.1,	410.1,	431,	450.5,	466.7,	481.8,	495.4,	508.2,	520.6,	534,	333.7,	353.7,	369.9,	384.1,	397.4,	409.5,	419.2,	427.5,	434,	439.3,	444.4,	450,	327.1,	338.6,	346.8,	353.1,	358,	362,	364.8,	366.7,	368.2,	369.5,	370,	371,	319.2,	320,	318.8,	315.9,	311.8,	307.1,	302.7,	297.9,	294,	291,	286,	312,	304.1,	295.8,	287.2,	278.4,	270.6,	262.7,	255.9,	249,	242,	305,	288.6,	272.2,	257.1,	241.1,	228.1,	215.1,	206,	199,	298.3,	274.8,	253.6,	233.1,	213.2,	196.3,	180,	170,	292.1,	263.2,	237.5,	211.9,	187.9,	168.1,	150,	137,	285.7,	253.6,	225.8,	198.7,	175.6,	154.2,	136,	123,	280.1,	246.7,	217.3,	190.5,	167.5,	146,	128.5,	115,	100,	275.6,	240.5,	210.1,	183.3,	160,	139.5,	121.8,	107,	93,	83,	73,	271.9,	236.1,	205.2,	179.9,	156.2,	135.6,	118.4,	102.4,	89.5,	80,	72,	64,	59,	54,	50,	45,	42,	40,	37,	34,	32,	30,	28,	25,	22,	268.7,	231.9,	201,	176.1,	153.6,	135.3,	118.8,	104.8,	94.8,	86.7,	78.2,	71.1,	65.5,	60.9,	55.6,	51.1,	48,	44,	40,	37,	34,	31,	28,	25,	22,	267,	231.9,	203.1,	178.6,	156.5,	140.7,	125,	111.8,	102,	93,	84.7,	77.2,	70.8,	65,	59.9,	54.2,	50.3,	46,	42,	38,	34,	31,	28,	25,	22,	268,	234.2,	207.2,	184.5,	163.8,	148,	132.5,	119.8,	109.4,	100.3,	91.1,	83.2,	76.5,	69.8,	63.8,	58.7,	54.3,	48,	44,	40,	35,	32,	28,	270,	238.6,	213.5,	191,	170.7,	153.9,	139.7,	127.2,	116.3,	107.4,	97.8,	89.1,	82.2,	75,	69,	63,	56,	51,	46,	41,	36,	274,	244.8,	220.8,	198.1,	179.4,	161.8,	147.5,	135.6,	123.9,	115.1,	105.7,	96.7,	89.2,	81.2,	75,	67,	60,	54,	48,	279,	251.7,	229.3,	207.5,	188.9,	171.5,	157.2,	145.6,	133.2,	124,	114.6,	105.5,	97.8,	88,	81,	72,	63,	286.1,	262.4,	242.5,	223.3,	205.4,	189.8,	175.8,	162.9,	150.6,	141.3,	130.4,	122,	114,	102,	94,	294,	274.2,	256.9,	239.5,	223.5,	208.9,	194.4,	180.9,	169.5,	159.3,	145,	136,	127,	300.1,	285.1,	270.8,	255.6,	241.9,	227.3,	214,	201.1,	189.3,	178,	165,	306.8,	296.9,	286.4,	274.1,	261.8,	248.9,	236.9,	224.1,	212,	200,	313,	307.6,	300.9,	291.8,	281.1,	269.1,	257.9,	245.6,	232,	217,	319,	319,	316.8,	311.4,	302.4,	290.4,	278.9,	266,	251,	234,	324.8,	330.2,	331.6,	329.7,	324,	312.9,	301.2,	287.2,	271,	251,	331.4,	341.2,	346.7,	347.7,	344,	336.1,	324.9,	314,	302,	286,	339.1,	356.3,	369.2,	377.1,	381.8,	381,	379,	374,	347.6,	371.5,	390.8,	404,	414,	423,	354.9,	386.5,	411.6,	430.6,	448,	363,	401.8,	432.6,	455.9,	475,	370,	416.6,	453.1,	482,	505,	374.8,	428.3,	471.1,	509,	377.8,	437.9,	488.9,	535,	373.2,	432.1,	479.5,	515.3,	543,	562,	370.6,	432.9,	485.7,	530,	568,	594,	367.8,	428.4,	485.2,	538.4,	585,	619,	659,	360.4,	415.7,	473.9,	534.8,	591.3,	646.8,	698,	757,	809,	355,	401.8,	455,	509.5,	567.2,	628.2,	685.8,	736,	793,	860,	917,	964,	1028,	1100,	347,	382.1,	421.5,	459.7,	500.6,	542.5,	577.9,	611.1,	643.1,	670.6,	697.5,	725,	750.2,	780,	816,	341.7,	370.4,	399.2,	426.6,	453.2,	480.7,	501.5,	521.4,	540,	554.3,	568.4,	585.7,	601,	618,	632,	337.1,	360.2,	382.2,	402.2,	421.9,	441.9,	456.2,	470.3,	484.2,	494.2,	504,	515.1,	525.8,	536,	546,	332.7,	349.8,	365.5,	379.9,	393.3,	407,	416.8,	424.5,	433,	438.8,	444,	449.2,	454.5,	460,	465,	327,	337.5,	346.3,	354,	360,	364.9,	368.8,	372,	375.4,	377.3,	378.8,	380,	382,	383,	320.8,	323.2,	324,	323.4,	321.9,	319.8,	317,	314.1,	310.8,	307.5,	305,	304,	303,	314.8,	310.8,	305.2,	298.5,	291,	284,	277.4,	271.8,	266.7,	262,	258,	255,	309.1,	298.4,	286.3,	273,	260,	248.4,	237.6,	229.8,	221,	214,	207,	303.8,	287.2,	270.8,	252.4,	235.4,	220.9,	205.7,	197.3,	184,	177,	299.2,	278.2,	257.2,	234.5,	214.8,	196.3,	178.5,	163,	150,	294.7,	270.4,	247,	223.2,	202.8,	183.7,	165.5,	149,	138,	291,	264.8,	240.7,	216,	195.4,	176,	158,	141.6,	128,	117,	287.6,	259.7,	234.3,	209.4,	188.8,	169.8,	151.3,	134.9,	120.8,	108,	96,	89,	284.2,	256,	230,	205,	184.3,	165.9,	147.9,	131.7,	116.7,	102.7,	92,	84,	78,	73,	68,	64,	60,	55,	48,	44,	281.9,	252.8,	226.6,	202.3,	181.1,	162.9,	146.8,	131.6,	118.5,	105.8,	98,	89.9,	82.5,	77,	72,	66,	60,	54,	49,	44,	280.4,	252.2,	226.3,	203.8,	183.7,	165.9,	150.3,	137.3,	125.6,	114,	106.4,	98.5,	90.4,	84,	77.8,	71,	64,	58,	52,	46,	280.7,	253.1,	230,	208.9,	190.3,	173,	158.5,	145.2,	133.2,	122.1,	114.3,	106.2,	97.8,	90.9,	84.7,	77.4,	70,	64,	57,	51,	282.5,	256.5,	234.7,	215,	196.7,	180.8,	166,	152,	140.8,	130,	121.8,	113.2,	105.2,	97.1,	90.7,	83.3,	74,	69,	63,	56,	285.9,	262.2,	241.6,	222.8,	206,	190.5,	175.5,	162.1,	150,	139.6,	130.6,	122.5,	113.5,	106.2,	97.9,	90.6,	82,	75,	69,	62,	290.2,	268.6,	249.3,	231.8,	216.2,	201.4,	187.5,	175.6,	162.6,	150,	142.4,	133.7,	124.8,	117.2,	108,	100,	93,	85,	78,	295.1,	277,	259.5,	243.8,	230.1,	216.2,	203.9,	192.3,	180.2,	167.9,	159.3,	150.4,	142.8,	134,	122,	115,	108,	301,	287.2,	273.3,	260,	248.9,	236.1,	224.9,	213.9,	202.3,	190.6,	182.1,	172,	162,	154,	143,	306.1,	296.3,	285.9,	275,	265.1,	254.1,	243.7,	232.9,	221.7,	210.1,	202,	192,	181,	310.6,	304.2,	297.2,	289,	281.1,	271.7,	262.3,	253,	242.4,	231.9,	224,	214,	315,	312.5,	308.5,	303.1,	296.9,	289.7,	281,	272.4,	262.2,	253,	244,	236,	320,	322.3,	322.6,	320.9,	317.6,	312.9,	305.7,	297.8,	288.1,	278,	270,	260,	323.6,	330,	334,	335.9,	335.1,	332.1,	326.9,	319.2,	310,	298.8,	291,	280,	329.4,	341.2,	350,	355.7,	358,	358.8,	356.8,	353.3,	348,	344,	338,	332,	336.7,	353.9,	367.4,	377.7,	385.6,	391,	396,	400,	404,	344.2,	367.9,	385.9,	399.4,	409.7,	416.9,	423,	350.4,	380.9,	402.9,	420.4,	434.2,	445,	456,	359,	394.8,	421.3,	441.4,	457.3,	468,	365.4,	407.6,	439.1,	462.5,	480,	492,	370.1,	418.8,	455,	481,	503,	516,	372.7,	427.2,	468.8,	499,	523,	538,	364.8,	415.8,	462.1,	496.7,	520.9,	539,	554,	363.6,	414.3,	464.6,	506.8,	536.9,	560.2,	580,	361.2,	409.7,	461.4,	509.3,	548.5,	580.2,	604,	626,	356,	399.4,	448.3,	497.6,	549,	594.9,	633.5,	666,	697,	733,	350.8,	388.1,	430.1,	475.9,	523.7,	575.1,	620.8,	670,	717.9,	767,	808,	852,	898,	949,	1000,	1048,	344.5,	373.5,	404.5,	438,	470.5,	507.1,	541.1,	575.9,	609.5,	639.2,	667.4,	691.8,	715.5,	738.5,	766,	789,	339.2,	362.8,	386,	410.7,	433.1,	457.8,	477.3,	498.1,	517.1,	532.1,	546.3,	562.8,	576.1,	589.8,	605,	622,	335.5,	354.5,	372.4,	391.5,	408.2,	427.1,	441.5,	456.1,	470.5,	481.7,	492.7,	503.9,	513.1,	522.4,	532,	542,	331,	345.5,	358.7,	373,	385.3,	399.2,	408.9,	419.2,	428.8,	436,	442.8,	449.1,	454.2,	459,	466,	474,	327,	336.9,	345.2,	353.7,	360.6,	366.8,	370.8,	375,	378.5,	381.4,	383.2,	385.1,	386,	388,	389,	391,	321,	324.6,	327,	328,	328,	328,	327.5,	326.1,	324.4,	323.1,	321.1,	320,	318,	317,	316,	316.1,	315,	312.5,	308.2,	303.2,	297.6,	293.2,	288,	282.8,	278,	274,	270,	268,	311.1,	304,	295.2,	286,	276,	266.2,	258.2,	248.9,	240,	232,	226,	221,	307.1,	295.4,	282.3,	268.7,	254.9,	240.6,	229.2,	216.6,	206,	194,	303.2,	287.9,	270.1,	251.9,	234.7,	217.2,	202.1,	186.3,	172,	160,	300,	280.8,	261.2,	241.7,	223,	204.2,	187.8,	171.1,	157,	146,	296.6,	275.7,	254.8,	234.4,	214.9,	196.4,	179.7,	163.2,	150.5,	137,	126,	294.2,	272,	248.8,	227.8,	207.8,	189.4,	172.8,	155.9,	141,	128,	116,	104,	292.3,	268.7,	244.9,	223.9,	204.1,	185.8,	168.9,	152.1,	136.5,	123,	113,	100,	89,	79,	70,	62,	290.8,	266.6,	241.7,	220.4,	202,	183,	166.1,	151.1,	136.5,	123.9,	114,	103,	94,	86,	78,	70,	62,	290.5,	265.9,	241.2,	221.1,	203,	185.7,	169.8,	155.5,	144.4,	132.9,	122.5,	113,	103,	95,	86,	78,	71,	63,	291.2,	266.7,	244.4,	224,	207.5,	191.3,	177.4,	164.4,	153.2,	141.9,	131.5,	122.3,	114,	104,	97,	88,	80,	72,	66,	292.8,	269.9,	248.7,	229.6,	213.7,	197.7,	184.7,	171.8,	160.4,	149.9,	139.8,	130.4,	122.4,	113.5,	105,	96,	88,	81,	75,	69,	295.9,	275,	255.5,	237.5,	223,	208,	195.1,	183,	171.1,	160.6,	150,	142.3,	133.1,	125.3,	117,	108,	100,	93,	88,	81,	298.6,	280.7,	263,	246.4,	232.8,	218.7,	206.6,	195.1,	184,	173.5,	164.4,	155.5,	146,	138.8,	130.8,	124,	115,	107,	102,	96,	301.9,	286.9,	271.8,	257,	245.2,	232.5,	221.1,	210.8,	200.7,	190.9,	181.4,	173.8,	165.2,	157,	149,	142,	135,	126,	306.5,	295.4,	284.2,	272.9,	263,	252.3,	242.8,	233.1,	224.2,	215,	206.8,	197.8,	190,	183,	176,	169,	309.8,	302.4,	294.1,	285.2,	277.3,	267.5,	259.6,	250.6,	242.1,	233,	224.8,	217,	210,	204,	197,	313.1,	309,	303.9,	298,	291.8,	284.1,	277.6,	269.5,	262,	253.5,	246,	240,	234,	229,	315.8,	314.8,	313,	310.1,	305.8,	300.2,	295,	288,	280.4,	271.9,	265,	259,	253,	248,	319.2,	322,	323.8,	324,	322.7,	319.4,	315.8,	310.2,	303.8,	297,	290,	284,	279,	274,	322.9,	329.4,	334.8,	338.7,	339.9,	338.9,	337,	333.1,	327.7,	321.6,	317,	312,	307,	302,	327.8,	339.8,	349.9,	357.5,	363,	366,	366.4,	365.7,	364.2,	361,	357,	354,	350,	333.7,	349.4,	364,	375.8,	384.4,	390.9,	395.3,	398.9,	402,	406,	339.5,	361.4,	380.8,	396,	406.4,	414.1,	420.1,	425,	429,	344.5,	371.4,	395.4,	414.1,	427.6,	437.8,	445,	453,	351.4,	384,	412.8,	433.8,	448.9,	460,	468,	477,	357,	395.4,	429.2,	452.4,	468.3,	481.2,	491,	362,	405.7,	443.5,	469.2,	487.6,	501.9,	512,	364,	412,	455.1,	485,	505.7,	520.8,	535,	361.1,	403.3,	445.2,	481.2,	506.8,	523.7,	539.2,	546,	360.7,	403.8,	447,	488.5,	519,	541.4,	559.4,	568,	359.2,	400.8,	443.8,	488,	526.4,	556.4,	578.8,	594,	607,	355,	392.2,	432.1,	476.8,	519.6,	559.6,	598.5,	628.2,	650,	667,	688,	349.6,	382.2,	417.5,	456.3,	494.9,	535.8,	580.2,	619.9,	661.6,	700.4,	738,	782,	820,	863,	905,	939,	343.7,	369.5,	396.3,	423.9,	453,	481.4,	513.3,	543,	573.7,	603.5,	631.8,	660.5,	687.1,	712.2,	738,	764,	789,	338.2,	359.5,	379.5,	399,	418.1,	439,	457.1,	475.1,	492.4,	509.1,	525.2,	540.8,	556,	569.5,	586,	599,	614,	334.4,	352.2,	367.2,	382.9,	397.9,	413.8,	427.8,	441.4,	455.1,	467.7,	479.5,	491,	501.8,	512.7,	524,	535,	546,	330.3,	344.3,	355.8,	367.9,	379.6,	391.4,	401.5,	411.3,	420.8,	429.9,	437.8,	445.8,	452,	458,	465,	471,	480,	326.8,	336.9,	344.8,	352.2,	358.4,	364.5,	369.9,	374.8,	379,	382.9,	386,	389,	392,	394,	397,	398,	321.9,	326.2,	329,	331.1,	332.9,	333.7,	334.3,	334.5,	334.5,	334.4,	332,	332,	331,	330,	330,	317.2,	316.9,	315.5,	313.8,	311,	308.1,	305.2,	301.7,	298.1,	294,	290,	287,	283,	313.2,	307.8,	301.5,	295,	288.1,	280.2,	272.9,	265.1,	258.1,	251,	244,	237,	309.7,	300.8,	289.9,	278.9,	268.2,	256.1,	245.9,	234.8,	226,	216,	205,	306.3,	293.8,	278.9,	263.5,	248.7,	233.9,	219.3,	204.8,	192,	181,	303.7,	288.1,	270.8,	253.7,	237.4,	220.3,	204.3,	187.9,	174,	162,	151,	301.2,	284,	265,	246.8,	229.8,	211.4,	194.7,	177.8,	164,	153,	144,	133,	299.1,	280.4,	259.9,	240.6,	222.5,	203.8,	186.8,	169,	156,	144,	134,	123,	297.8,	277.8,	255.8,	236.5,	218.8,	199.9,	182.2,	164,	153,	140,	129,	118,	107,	296.3,	275.2,	253.1,	234.7,	216.8,	197.5,	179.9,	163,	151,	139,	128,	118,	105,	296.1,	274.7,	253.3,	235.2,	217.6,	199.8,	183.9,	167.1,	156,	143,	132,	123,	111,	101,	296,	275.9,	255,	237.2,	220.4,	205.2,	190.9,	176.8,	165.8,	153,	142,	132,	121,	112,	102,	93,	296.7,	277.8,	258.5,	242.1,	226,	212.1,	197.9,	185.2,	173.8,	162.1,	149,	140,	130,	120,	111,	103,	95,	89,	299.3,	283.1,	265,	250.2,	235,	222.2,	209.5,	197.6,	187,	174.5,	163.8,	154.7,	144,	136,	126,	118,	111,	105,	301.8,	287.1,	271.6,	258.4,	245,	232.9,	220.3,	209.5,	199.5,	188.2,	178.5,	169.8,	160.4,	151,	144,	136,	129,	123,	304.8,	292.9,	279.9,	268.8,	257.8,	246.2,	235.5,	225.1,	215.8,	205.6,	197.8,	189.2,	181,	174,	166,	159,	153,	148,	308.5,	300.1,	290.4,	282,	273.8,	264.6,	255.2,	246.7,	238.2,	228.3,	221.9,	215,	207,	201,	196,	189,	311.3,	305.6,	298.7,	292.9,	286,	278.4,	270.5,	262.2,	254.9,	246.4,	240,	233,	227,	221,	216,	314.1,	311.2,	307.4,	303.8,	298.9,	293.6,	288.1,	281.2,	275.1,	268,	262,	256,	250,	246,	316.6,	316.3,	315.8,	314.4,	311.8,	308.2,	304.1,	298.3,	292.8,	288,	282,	276,	271,	265,	319,	322.1,	324.4,	325.1,	325,	323.4,	321.2,	317.9,	313.8,	309,	304,	299,	295,	289,	283,	322.8,	329.1,	334,	338.3,	341.2,	342.8,	343.1,	342,	339.6,	336,	333,	330,	326,	321,	316,	326.8,	338.1,	347.3,	355,	361.9,	366.7,	369.7,	371.3,	372,	371,	370,	370,	369,	368,	365,	332.1,	346.7,	360,	370.8,	380.6,	388.7,	394.7,	399,	402.1,	403,	406,	337.3,	356.4,	375,	390,	402.2,	411.9,	418.8,	423.9,	427,	431,	342.1,	365.2,	387.6,	406.4,	422,	433.1,	441.2,	447.8,	452,	455,	348.3,	376.7,	403,	424.9,	441.6,	453.6,	462.3,	469,	472,	354,	386.7,	417.6,	442.1,	460.7,	473,	482.9,	489,	494,	358,	395.5,	430.5,	458.8,	479,	492,	503.8,	510,	360.1,	400.4,	440,	471.9,	494.3,	508.7,	522,	528,	356.9,	395.1,	430.5,	464.1,	493.7,	513.1,	527.7,	537.5,	543,	356.9,	395.3,	431.6,	468.4,	503,	527,	545.9,	557.8,	564,	355.9,	392.9,	429.1,	466.9,	505.1,	536.7,	561.5,	578.3,	589,	601,	351.6,	384.8,	419.1,	455.8,	495,	532.8,	570,	600,	624.2,	642,	661,	676,	346.9,	376.4,	405.8,	438.7,	473.2,	509.5,	545.8,	583.5,	618.6,	656.3,	689.3,	727,	758,	790,	819,	851,	341.3,	364.4,	387.3,	412.9,	439.5,	466.7,	493.1,	520.3,	546.7,	574.4,	601.7,	626.5,	654.9,	681,	709,	733,	759,	336.6,	354.8,	372.1,	390.1,	408.7,	426.7,	445,	461.6,	477.1,	493.3,	507.4,	520,	531.2,	546,	560,	573,	587,	333.3,	348.2,	362.2,	376.4,	391.4,	405.8,	419.9,	433,	444.8,	457,	468.3,	477.8,	485.8,	497,	507,	517,	527,	329.7,	341.5,	352.6,	363.5,	374.8,	385.4,	395.6,	404.9,	413.5,	422,	430.6,	437.7,	443,	449,	457,	463,	326.9,	335.4,	343,	350.8,	357.6,	363.6,	369.4,	373.9,	378.8,	382.7,	387,	390,	392,	394,	397,	398,	322.5,	326.9,	330.2,	333.5,	336.1,	337.9,	339,	340.1,	341,	341.2,	340,	340,	340,	340,	339,	318.2,	318.9,	318.6,	317.8,	316.5,	314.8,	312.9,	310.1,	307.6,	305,	302,	300,	298,	314.3,	310.9,	306.9,	301.4,	295.6,	289.2,	283.2,	276.8,	271,	266,	260,	253,	311,	303.8,	296,	287.1,	277.5,	267.2,	258.1,	247.2,	239,	231,	222,	307.8,	297.2,	285.4,	272.9,	257.9,	243,	230.7,	219,	206,	195,	305.8,	292.7,	278.7,	263.1,	246.6,	230.3,	216,	202,	187,	175,	303.9,	288.6,	272.8,	255.9,	238.2,	220.3,	206,	190,	176,	162,	302.5,	284.8,	267.7,	249.8,	230.9,	213,	195,	179,	165,	301.1,	282.8,	264.3,	245.8,	226.7,	208,	191,	175,	161,	300.3,	280.9,	261.2,	241.8,	222.4,	204,	189,	173,	160,	146,	300,	280.1,	261.2,	242.5,	224,	205.8,	191,	178,	166,	152,	140,	300,	281,	263.3,	245.9,	228.9,	212.7,	198,	185,	176,	162,	150,	140,	301,	283.1,	266.3,	250.4,	234.3,	219.7,	206.8,	194,	182,	170,	158,	148,	137,	128,	303.7,	288,	273,	258.4,	244.2,	232,	219.2,	207.4,	196.2,	185,	174,	164,	154,	145,	136,	305.4,	292,	278.6,	265.4,	253.1,	242.3,	230.8,	219.2,	208.8,	198.8,	188.3,	179,	170,	162,	154,	307.6,	297.1,	285.4,	274.5,	264.8,	254.5,	244.8,	234.2,	224.1,	214.3,	206,	200,	192,	184,	178,	310.4,	303.2,	294.9,	286.9,	279.8,	271,	262.8,	254,	245.9,	239,	232,	226,	217,	211,	312.5,	307.9,	302.2,	296.3,	290.6,	283.4,	276.2,	268.9,	261,	255,	249,	243,	236,	314.8,	312.5,	309.8,	306.7,	303,	298,	293.1,	287.8,	283,	279,	274,	267,	317,	317.1,	317,	316,	314.4,	311.9,	308.2,	303.9,	300,	296,	291,	287,	319,	322.2,	324.4,	325.6,	326,	325.2,	323.8,	321,	318,	315,	311,	306,	322,	328.2,	333.6,	338.2,	341.3,	343.5,	345,	345.2,	343,	341,	338,	335,	332,	325.3,	336,	345.2,	353.3,	359.6,	365.9,	370,	372.9,	373,	374,	374,	372,	369,	329.8,	343.9,	357,	367.9,	376.8,	386.1,	393.8,	398.9,	402.4,	404.6,	407,	409,	334.9,	353,	370.1,	384.2,	397.2,	408.1,	416.8,	422.8,	426.7,	429.8,	434,	339.7,	361.3,	382,	399.6,	415.1,	428.2,	438.1,	444.9,	449.2,	453,	456,	345.4,	371.9,	396,	416.4,	433.5,	448,	458.1,	465,	470,	474,	350.7,	380,	407.3,	431.2,	451.6,	466.6,	477.3,	484.3,	488,	492,	354,	388.5,	419.8,	446.2,	469.6,	485.7,	496.5,	504.7,	509,	513,	355.8,	392.5,	426.4,	456.8,	483,	500.5,	512.8,	521.5,	527,	355.2,	388.3,	421.6,	452,	479.1,	502,	518.1,	529.5,	536.6,	542,	355.5,	388.7,	423,	455,	486.9,	513.3,	534.4,	547.5,	555.7,	562,	568,	354.2,	387.2,	421.4,	454.2,	487.9,	519.9,	546.8,	564.1,	578.5,	585.5,	595,	350.2,	380.9,	412.9,	445.2,	479.1,	514.4,	549,	579.8,	606.3,	623.5,	645,	660,	679,	696,	345.9,	372.7,	401.4,	428.4,	460.1,	492.6,	524.7,	557.8,	591.9,	625.5,	656.4,	684,	702,	726,	752,	784,	340.4,	361.4,	385.1,	406.5,	430.1,	455.4,	478,	504.5,	530.9,	556.1,	579.9,	603.3,	622,	647,	670,	692,	335.9,	352.3,	370.2,	386.5,	402.6,	419.1,	434.8,	450,	465.2,	480.6,	494,	507,	521,	537,	553,	564,	332.6,	346.4,	360.8,	373.3,	386.7,	400.2,	412.5,	425.2,	437.2,	449.2,	459,	471,	483,	494,	507,	329.3,	340.3,	351.2,	361.1,	371,	381.1,	390.3,	399.2,	408.6,	416.4,	424,	433,	441,	448,	326.8,	335.1,	342.9,	350,	356.6,	363,	368.1,	373.2,	378.2,	383,	387,	392,	396,	322.8,	327.8,	331.8,	335.2,	338.3,	341.2,	343.2,	345,	346,	348,	349,	351,	352,	318.3,	320,	319.8,	319.8,	319.6,	318.8,	317.9,	316.8,	314,	311,	309,	307,	315.2,	313,	309.9,	306.3,	302.5,	297.8,	293.6,	288,	282,	278,	273,	312.4,	306.7,	300,	292.3,	283.9,	275.2,	266,	256,	245,	235,	309.6,	299.8,	288.8,	276.1,	263,	247,	236,	221,	207,	307.7,	295.6,	282.1,	266.8,	250,	233,	219,	204,	306.2,	291.1,	276,	258.7,	242,	224,	206,	304.7,	287.9,	270.9,	253,	235,	217,	199,	303.9,	286.1,	267,	249,	229,	214,	194,	303.4,	284.6,	264.8,	244,	224,	210,	193,	303.5,	284.8,	264.9,	244.3,	225,	211,	196,	185,	304,	285,	267.1,	248.8,	231,	218,	204,	194,	183,	304.7,	286.8,	270.4,	253.4,	238,	224,	211,	201,	191,	179,	168,	307,	291.5,	278.5,	262.6,	249.7,	237,	224,	214,	204,	194,	184,	173,	162,	308.4,	295.5,	282.9,	270,	258.2,	247,	234.9,	225,	217,	207,	196,	186,	176,	310,	300,	289.8,	279.3,	269.9,	259.4,	249.6,	240,	233,	224,	215,	206,	197,	312,	305.2,	297.8,	290,	282.8,	274.2,	267,	260,	253,	245,	237,	230,	313.6,	309.2,	304.2,	298.3,	293,	285.9,	280,	274,	267,	261,	255,	315.2,	313.5,	311.2,	308.2,	304.9,	300,	297,	293,	289,	284,	316.9,	317.7,	317.5,	317.1,	316,	314,	311,	308,	306,	303,	318.6,	322.4,	324.8,	326.3,	327,	326,	325,	324,	322,	321.1,	327.9,	333.5,	338.5,	341.9,	345,	344,	344,	343,	343,	323.7,	334.9,	344.2,	352.6,	358.9,	365,	369,	371,	374,	374,	375,	375,	375,	327.6,	342.9,	354.7,	366.2,	376.1,	384.7,	391,	396,	399,	402,	403,	404,	405,	333,	351,	366.3,	382,	393.8,	405,	414.5,	421,	425,	427,	431,	433,	435,	337.9,	358.6,	377,	395.2,	410,	423.2,	433.8,	442.4,	448,	451.8,	454,	458,	343.4,	367.4,	389.6,	410.2,	426.8,	441.4,	453,	461.3,	467,	470.9,	474,	348.4,	375.1,	400.9,	423.1,	442.3,	458.9,	471.2,	480,	485.5,	490,	494,	351.8,	382.6,	411.7,	437.8,	460.1,	478.8,	492,	501.2,	506.9,	511,	515,	354,	386.1,	417.5,	446.6,	471.2,	491.7,	506.2,	515.8,	522,	526,	353.7,	385.2,	415.5,	445,	469.4,	492,	510.1,	522.5,	532,	537,	544,	353.9,	386.6,	417.8,	449,	476.1,	502.8,	523.7,	538.3,	550.8,	556,	564,	353.4,	386.1,	417.9,	449.7,	479.1,	508.2,	532.9,	554.1,	569.9,	580,	590,	350,	379.3,	411.1,	441.5,	473.5,	504.2,	533.9,	565.4,	592,	614,	638,	657,	682,	707,	345.4,	371.1,	400.8,	425.9,	455.8,	482.9,	512.9,	544,	574.8,	602,	627,	653,	681,	707,	340,	360.6,	384.6,	405.4,	427.5,	449.1,	472.6,	496.6,	525,	552,	576,	599,	619,	643,	335.7,	351.9,	369.7,	385.4,	400.1,	416,	430,	446,	461,	474,	487,	499,	509,	519,	332.3,	346.1,	360.7,	373.8,	385.5,	398.5,	410,	423,	436,	446,	457,	467,	329.3,	340.2,	351.3,	361.8,	370.2,	379.6,	388,	396,	405,	411,	419,	425,	326.7,	334.9,	343.3,	350.7,	356.8,	364,	370,	376,	382,	386,	391,	323.2,	328.7,	333.8,	337.8,	340.5,	344,	346,	348,	349,	350,	318.8,	320.8,	322,	322.5,	322.6,	323,	323,	323,	322,	315.9,	314,	311.8,	310,	306,	304,	301,	298,	294,	312.5,	307.3,	302,	296,	290,	284,	278,	271,	310.2,	300.5,	290,	281,	272,	260,	251,	241,	308.7,	296.1,	285,	273,	262,	252,	307.6,	292.4,	278,	265,	250,	306.3,	289,	272,	257,	243,	305.7,	287,	268,	252,	235,	305.2,	285,	266,	245,	227,	305.4,	285,	266,	245,	226,	305.1,	286.5,	268,	249,	230,	306,	287,	271,	253,	235,	221,	208,	308.1,	292.8,	278.8,	262,	247,	233,	220,	208,	309.4,	296.6,	284.5,	270,	257,	246,	235,	224,	310.8,	301,	291,	280,	271,	261,	251,	243,	312.6,	306,	298.8,	292,	284,	276,	269,	263,	314.1,	309.9,	305.2,	300,	295,	289,	284,	315.5,	314,	311.8,	310,	306,	302,	298,	316.8,	317.9,	318.3,	318,	316,	314,	311,	318.8,	322.6,	325.6,	326,	326,	325,	321,	328.3,	334.8,	338,	340,	340,	340,	323.3,	334.8,	343.9,	350,	356,	361,	364,	368,	327.3,	342.2,	355,	365,	373,	380,	387,	392,	396,	332.5,	350.9,	365.9,	379,	390,	400,	408,	416,	421,	426,	430,	433,	435,	337.7,	358.5,	376.3,	393,	407,	418,	428,	436,	444,	450,	454,	459,	463,	343,	366.8,	387.7,	406.9,	424,	436,	447,	455,	462,	468,	473,	478,	347.2,	373.8,	397.2,	418.6,	436.9,	452.7,	465,	476,	483,	488,	494,	499,	350.4,	379.9,	407.1,	431.9,	452.9,	471.9,	486.9,	497.7,	504.9,	509.2,	515,	522,	352.7,	383.2,	412.3,	439.2,	462.2,	482.9,	499.3,	510.4,	518.8,	523,	528,	352,	382,	409,	437,	460,	481,	502,	515,	527,	534,	541,	354,	384,	413,	442,	466,	492,	514,	529,	546,	552,	560,	354,	384,	414,	446,	470,	497,	518,	538,	560,	572,	580,	351,	378,	410,	439,	470,	493,	518,	545,	571,	592,	608,	346,	370,	400,	423,	454,	474,	500,	527,	556,	580,	600,	342,	361,	385,	404,	427,	444,	467,	490,	517,	543,	563,	337,	353,	369,	384,	398,	412,	426,	440,	457,	333,	346,	360,	374,	385,	397,	409,	420,	331,	341,	351,	361,	369,	378,	387,	327,	335,	343,	350,	357,	362,	368,	323,	330,	335,	340,	342,	345,	348,	318,	320,	321,	322,	322,	322,	315,	313,	311,	310,	308,	305,	313,	308,	304,	299,	293,	312,	302,	296,	288,	310,	298,	287,	309,	294,	280,	307,	290,	274,	307,	288,	270,	307,	286,	268,	307,	286,	268,	307,	288,	270,	307,	289,	274,	258,	308,	294,	280,	266,	309,	297,	284,	273,	264,	311,	301,	291,	282,	275,	313,	307,	300,	293,	288,	314,	310,	305,	301,	316,	315,	314,	311,	318,	318,	318,	318,	320,	322,	325,	326,	322,	329,	334,	337,	326,	336,	344,	349,	329,	342,	354,	363,	333,	350,	365,	378,	388,	338,	357,	375,	391,	405,	416,	343,	367,	385,	402,	420,	431,	442,	453,	347,	373,	395,	413,	431,	446,	460,	470,	477,	486,	490,	348,	377,	402,	425,	446,	465,	481,	494,	502,	508,	514,	351,	380,	407,	432,	454,	473,	492,	505,	515,	521,	528,

};
const int Munsell_Y[5000] =//*1000    
{
	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	237,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	467,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	699,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	943,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	1210,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	3126,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	6550,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	12000,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	19770,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	30030,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	43060,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	59100,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	78660,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,	102570,

};

const int Munsell_L[5000] =//*100
{
209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	209,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	411,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	616,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	830,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	1041,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	2023,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	3037,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	4074,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	5101,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	6103,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	7086,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	8053,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	9018,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,	10000,
};

const int Munsell_A[5000] =//*100
{
-437,	-540,	-613,	-933,	-649,	-1091,	-673,	-1142,	-692,	-1166,	-705,	-1188,	-715,	-1195,	-712,	-1182,	-668,	-1097,	-607,	-981,	-500,	-807,	-397,	-620,	-279,	-393,	-193,	-277,	-81,	-108,	-119,	43,	164,	292,	213,	456,	802,	1221,	1853,	3410,	455,	903,	1492,	2199,	3067,	4158,	5449,	7258,	11079,	26419,	753,	1321,	2053,	2872,	3581,	4548,	5602,	6926,	8998,	13327,	906,	1557,	2332,	3157,	3856,	4648,	5589,	6688,	8287,	1056,	1754,	2490,	3360,	4016,	4774,	5614,	1117,	1862,	2571,	3408,	4042,	4855,	1154,	1900,	2683,	3466,	4079,	1210,	1972,	2765,	3461,	4191,	1262,	2052,	2828,	3548,	1333,	2140,	2915,	3659,	1360,	2212,	2958,	1361,	2253,	3011,	1353,	2308,	1324,	2327,	1308,	2325,	1248,	2257,	1148,	2134,	1012,	700,	453,	168,	2,	-310,	-477,	-1043,	-547,	-1375,	-603,	-1570,	-648,	-1691,	-670,	-1761,	-690,	-1806,	-698,	-1813,	-706,	-1791,	-706,	-1730,	-681,	-1556,	-636,	-1354,	-1820,	-546,	-1057,	-1385,	-478,	-858,	-1125,	-379,	-613,	-755,	-279,	-420,	-454,	-143,	-202,	-172,	35,	-20,	61,	312,	814,	170,	399,	796,	1404,	1873,	2432,	2898,	3536,	4162,	5379,	437,	897,	1557,	2328,	3029,	3836,	4347,	5290,	6190,	7475,	8641,	11482,	16599,	34331,	703,	1304,	2046,	2860,	3696,	4549,	5184,	5980,	7020,	8013,	9660,	11725,	15223,	21379,	872,	1546,	2323,	3145,	4107,	4793,	5615,	6399,	7433,	8379,	9881,	11284,	14632,	1018,	1789,	2513,	3345,	4260,	5039,	5828,	6695,	7507,	8566,	1120,	1901,	2638,	3463,	4385,	5230,	6100,	6840,	7952,	1183,	1982,	2708,	3541,	4632,	5420,	6174,	7108,	1224,	2045,	2803,	3622,	4684,	5520,	6425,	1288,	2142,	2883,	3752,	4900,	5635,	1341,	2201,	2979,	3801,	5022,	1358,	2250,	3038,	3891,	5236,	1375,	2297,	3096,	3999,	5284,	1353,	2299,	3124,	4081,	1324,	2285,	3125,	4124,	1292,	2270,	3135,	4117,	1207,	2231,	3082,	3998,	1109,	2081,	2777,	973,	1855,	756,	468,	214,	-71,	-320,	-658,	-474,	-1258,	-560,	-1539,	-624,	-1717,	-2737,	-673,	-1790,	-2961,	-694,	-1809,	-3089,	-711,	-1819,	-3161,	-723,	-1811,	-3164,	-739,	-1791,	-3086,	-742,	-1725,	-2945,	-731,	-1586,	-2553,	-700,	-1450,	-2269,	-634,	-1233,	-1854,	-551,	-1006,	-1430,	-2239,	-423,	-715,	-1011,	-1463,	-305,	-480,	-684,	-962,	-162,	-247,	-223,	-156,	-21,	69,	252,	533,	887,	1216,	153,	424,	832,	1345,	1860,	2329,	2797,	3356,	4066,	5163,	6395,	455,	974,	1626,	2295,	2990,	3619,	4211,	4891,	5765,	6766,	7711,	8959,	11200,	15340,	21661,	755,	1395,	2088,	2867,	3525,	4167,	4961,	5646,	6503,	7451,	8490,	9632,	11526,	14493,	20637,	935,	1639,	2346,	3129,	3781,	4471,	5490,	6081,	7057,	7839,	8837,	9880,	11547,	14564,	1089,	1808,	2501,	3321,	3980,	4691,	5673,	6429,	7490,	8134,	9248,	10049,	1191,	1929,	2612,	3407,	4053,	4777,	5801,	6707,	7427,	8300,	1258,	1992,	2689,	3493,	4196,	4881,	6006,	6729,	7616,	1316,	2078,	2769,	3584,	4326,	4997,	6163,	7025,	1351,	2133,	2909,	3669,	4487,	5204,	6347,	7277,	1383,	2195,	3002,	3768,	4618,	5464,	6589,	1387,	2249,	3049,	3813,	4671,	5629,	1382,	2274,	3081,	3864,	4681,	5920,	1372,	2281,	3092,	3893,	4698,	1339,	2271,	3090,	3934,	4703,	1288,	2249,	3078,	3912,	4641,	1194,	2211,	3021,	3884,	4482,	1063,	2087,	2904,	3704,	930,	1910,	704,	1448,	449,	1010,	165,	-96,	-23,	-343,	-795,	-460,	-1341,	-587,	-1600,	-2353,	-669,	-1762,	-2685,	-3844,	-723,	-1852,	-2886,	-4045,	-754,	-1895,	-3014,	-4382,	-763,	-1904,	-3037,	-4494,	-781,	-1894,	-3018,	-4509,	-782,	-1877,	-2952,	-4431,	-796,	-1838,	-2829,	-4264,	-785,	-1747,	-2622,	-3853,	-742,	-1573,	-2372,	-3431,	-671,	-1329,	-2001,	-2844,	-615,	-1128,	-1682,	-2386,	-494,	-863,	-1204,	-1595,	-353,	-598,	-835,	-1084,	-1568,	-216,	-299,	-356,	-417,	-456,	-69,	10,	169,	456,	760,	1358,	191,	420,	766,	1214,	1545,	2390,	3237,	4217,	5295,	6931,	466,	944,	1543,	2160,	2607,	3340,	4328,	5159,	5933,	6931,	8248,	10264,	12527,	15397,	20839,	31916,	-474460,	-120386,	732,	1385,	2038,	2729,	3272,	4009,	4780,	5626,	6261,	7095,	8104,	9235,	10127,	11609,	12733,	14717,	17658,	19777,	910,	1640,	2281,	2955,	3509,	4186,	4950,	5814,	6431,	7135,	7876,	8767,	9465,	10736,	11267,	12478,	13627,	15603,	1042,	1807,	2462,	3165,	3644,	4385,	5202,	5973,	6527,	7068,	7843,	8488,	9266,	9964,	11014,	1129,	1906,	2548,	3247,	3753,	4458,	5349,	6164,	6613,	7144,	7752,	8329,	9016,	1163,	1959,	2600,	3337,	3827,	4537,	5403,	6099,	6608,	7118,	7695,	8356,	1219,	2038,	2702,	3411,	3930,	4685,	5606,	6178,	6668,	7221,	7664,	1271,	2070,	2771,	3544,	4084,	4870,	5612,	6261,	6795,	7159,	1304,	2109,	2855,	3653,	4239,	4945,	5774,	6407,	1315,	2130,	2906,	3753,	4374,	5077,	5833,	1322,	2153,	2978,	3790,	4449,	5185,	5902,	1306,	2167,	3011,	3843,	4505,	5195,	1274,	2178,	3014,	3826,	4541,	5248,	1232,	2148,	2949,	3742,	4480,	5262,	1138,	2086,	2788,	3516,	4358,	5070,	980,	1927,	2624,	3280,	823,	1705,	2387,	636,	1327,	379,	976,	129,	500,	-134,	-146,	-310,	-594,	-462,	-1142,	-1376,	-557,	-1518,	-1967,	-653,	-1758,	-2562,	-3320,	-711,	-1886,	-2850,	-3902,	-4543,	-744,	-1925,	-3060,	-4065,	-4619,	-770,	-1943,	-3110,	-4027,	-4637,	-784,	-1937,	-3088,	-3984,	-4663,	-795,	-1920,	-3030,	-3910,	-4685,	-789,	-1896,	-2900,	-3841,	-4717,	-765,	-1768,	-2696,	-3670,	-4763,	-730,	-1606,	-2490,	-3412,	-661,	-1375,	-2123,	-2925,	-594,	-1162,	-1741,	-2340,	-505,	-887,	-1233,	-1655,	-1985,	-369,	-582,	-769,	-945,	-1058,	-212,	-308,	-360,	-386,	-347,	-21,	42,	196,	445,	716,	1243,	1412,	169,	363,	654,	1081,	1665,	2214,	2724,	3335,	3879,	4435,	4922,	5500,	6043,	6677,	7142,	7671,	8280,	8993,	9841,	10876,	12270,	14000,	453,	878,	1454,	2130,	2805,	3502,	4221,	5049,	5673,	6411,	7130,	7949,	8799,	9715,	10737,	11839,	12748,	13710,	14847,	16230,	17960,	20225,	23407,	28441,	704,	1264,	1899,	2511,	3196,	3918,	4640,	5407,	6062,	6816,	7542,	8385,	9386,	10418,	11563,	12615,	13429,	14892,	16088,	16775,	18336,	20392,	23168,	27269,	869,	1506,	2151,	2846,	3507,	4263,	4983,	5758,	6403,	7152,	7968,	8743,	9823,	10799,	12104,	13449,	14212,	15087,	16746,	18120,	19809,	992,	1677,	2334,	3038,	3725,	4480,	5259,	6043,	6676,	7446,	8323,	8973,	10124,	11102,	12574,	13837,	1068,	1776,	2449,	3176,	3879,	4616,	5458,	6193,	6840,	7573,	8373,	9148,	10205,	1130,	1851,	2553,	3295,	3998,	4741,	5569,	6287,	6958,	7742,	8562,	9413,	1177,	1934,	2641,	3415,	4130,	4853,	5678,	6391,	7120,	7943,	8850,	1213,	2004,	2729,	3523,	4282,	5030,	5758,	6440,	7096,	8108,	1238,	2048,	2803,	3594,	4402,	5193,	5952,	6641,	1245,	2067,	2842,	3670,	4459,	5292,	6049,	6705,	1242,	2077,	2903,	3714,	4496,	5419,	6033,	1230,	2074,	2912,	3739,	4508,	5468,	1201,	2055,	2907,	3742,	4508,	5510,	1149,	2039,	2836,	3690,	4502,	5380,	1035,	1949,	2681,	3513,	4383,	5389,	868,	1707,	2313,	2836,	3412,	702,	1392,	1876,	503,	1036,	241,	622,	43,	227,	-148,	-110,	-306,	-585,	-805,	-443,	-924,	-1428,	-1898,	-531,	-1208,	-1877,	-2538,	-3086,	-628,	-1472,	-2315,	-3263,	-4165,	-4959,	-5810,	-685,	-1632,	-2557,	-3570,	-4662,	-5763,	-6774,	-7982,	-9049,	-732,	-1720,	-2728,	-3710,	-4966,	-6215,	-7275,	-8181,	-9029,	-752,	-1750,	-2755,	-3736,	-4943,	-6192,	-7259,	-8162,	-9012,	-763,	-1757,	-2720,	-3716,	-4865,	-6123,	-7191,	-8074,	-8949,	-762,	-1753,	-2665,	-3699,	-4805,	-6020,	-7024,	-7925,	-8914,	-755,	-1703,	-2577,	-3637,	-4699,	-5842,	-6850,	-7744,	-8789,	-730,	-1599,	-2414,	-3460,	-4485,	-5491,	-6518,	-7485,	-694,	-1494,	-2269,	-3234,	-4194,	-5187,	-6169,	-7236,	-636,	-1328,	-2005,	-2820,	-3702,	-4534,	-5379,	-572,	-1162,	-1744,	-2415,	-3155,	-3977,	-495,	-944,	-1349,	-1785,	-2218,	-2613,	-389,	-671,	-914,	-1172,	-1405,	-1596,	-1929,	-226,	-392,	-520,	-606,	-671,	-664,	-694,	-63,	-63,	11,	136,	320,	528,	833,	1135,	1593,	130,	270,	472,	756,	1132,	1541,	2114,	2695,	3269,	3722,	4188,	4746,	5232,	5502,	5792,	6105,	6586,	6972,	7581,	8385,	9241,	10386,	11703,	361,	739,	1192,	1718,	2361,	2985,	3662,	4383,	5110,	5802,	6569,	7267,	7926,	8590,	9356,	10109,	10889,	11706,	12620,	13689,	14965,	16497,	18320,	20752,	23994,	569,	1099,	1652,	2293,	2973,	3604,	4300,	4996,	5721,	6493,	7244,	8007,	8801,	9568,	10494,	11371,	12223,	13150,	14143,	15289,	16578,	18190,	20183,	22681,	26182,	705,	1312,	1905,	2660,	3357,	4078,	4788,	5488,	6234,	6999,	7790,	8622,	9485,	10473,	11554,	12694,	13437,	14591,	15586,	16788,	18147,	19783,	815,	1477,	2095,	2901,	3617,	4359,	5135,	5824,	6578,	7345,	8164,	9040,	9994,	11035,	12408,	13739,	14777,	15990,	900,	1596,	2255,	3086,	3847,	4588,	5367,	6114,	6868,	7597,	8513,	9353,	10305,	11616,	13220,	968,	1688,	2394,	3249,	4011,	4793,	5592,	6377,	7158,	7924,	8808,	9819,	10780,	12217,	1043,	1777,	2520,	3415,	4219,	5033,	5890,	6698,	7499,	8296,	9314,	10313,	11450,	1067,	1841,	2606,	3514,	4345,	5171,	6042,	6892,	7637,	8592,	9486,	1068,	1883,	2666,	3567,	4434,	5296,	6169,	6989,	7838,	8838,	1061,	1908,	2718,	3612,	4484,	5382,	6316,	7184,	8000,	1048,	1911,	2732,	3651,	4543,	5466,	6413,	7377,	1025,	1883,	2699,	3634,	4546,	5445,	6421,	7324,	990,	1832,	2617,	3486,	4419,	5323,	6258,	7182,	918,	1737,	2494,	3336,	4195,	5109,	6055,	6954,	826,	1586,	2274,	3032,	3793,	4550,	5463,	6181,	689,	1317,	1894,	2484,	2951,	527,	1008,	1459,	1797,	359,	716,	1040,	1277,	157,	377,	644,	843,	-11,	67,	172,	-172,	-264,	-261,	-371,	-638,	-823,	-954,	-524,	-996,	-1341,	-1709,	-2013,	-636,	-1293,	-1840,	-2359,	-2819,	-3256,	-763,	-1607,	-2375,	-3151,	-3951,	-4715,	-5437,	-833,	-1787,	-2702,	-3620,	-4547,	-5444,	-6407,	-7308,	-8403,	-9612,	-10864,	-12007,	-879,	-1920,	-2940,	-3889,	-4964,	-5986,	-7005,	-8050,	-9179,	-10528,	-11765,	-12795,	-905,	-1980,	-2994,	-3928,	-5002,	-6008,	-7050,	-8089,	-9125,	-10380,	-11655,	-12926,	-914,	-2007,	-3006,	-3923,	-4949,	-5994,	-6978,	-7985,	-9041,	-10255,	-11472,	-12804,	-914,	-2007,	-2967,	-3884,	-4864,	-5883,	-6873,	-7909,	-8943,	-10042,	-11189,	-12582,	-888,	-1958,	-2881,	-3784,	-4761,	-5731,	-6700,	-7663,	-8713,	-9782,	-10896,	-12269,	-863,	-1855,	-2708,	-3614,	-4545,	-5453,	-6364,	-7369,	-8275,	-9351,	-10607,	-819,	-1733,	-2544,	-3365,	-4276,	-5148,	-6047,	-6984,	-7975,	-9039,	-767,	-1551,	-2264,	-2989,	-3826,	-4613,	-5531,	-6291,	-7043,	-682,	-1355,	-1956,	-2576,	-3285,	-3979,	-4634,	-5385,	-593,	-1106,	-1527,	-1951,	-2383,	-2834,	-3274,	-3828,	-452,	-801,	-1078,	-1338,	-1590,	-1844,	-2137,	-2342,	-285,	-489,	-639,	-780,	-894,	-997,	-1090,	-1111,	-1006,	-99,	-132,	-125,	-66,	29,	145,	310,	499,	759,	1056,	1423,	103,	221,	356,	529,	779,	1079,	1442,	1896,	2367,	2848,	3322,	3889,	4314,	4880,	5274,	6000,	6420,	6759,	7267,	7944,	8409,	8919,	9480,	10498,	11703,	352,	695,	1088,	1511,	1999,	2553,	3221,	3948,	4596,	5202,	5955,	6690,	7341,	7955,	8751,	9517,	10114,	10883,	11813,	12650,	13512,	14559,	15752,	17048,	18661,	565,	1072,	1604,	2172,	2812,	3384,	4086,	4803,	5441,	6109,	6819,	7559,	8274,	9027,	9766,	10711,	11441,	12290,	13283,	14352,	15649,	16698,	17964,	19425,	21141,	718,	1344,	1962,	2610,	3322,	3965,	4701,	5408,	6079,	6747,	7520,	8267,	8989,	9809,	10643,	11458,	12228,	13530,	14464,	15522,	17139,	18206,	19884,	842,	1551,	2215,	2917,	3659,	4392,	5093,	5803,	6508,	7159,	7952,	8767,	9505,	10377,	11188,	12102,	13356,	14367,	15522,	16862,	18444,	948,	1712,	2434,	3209,	3944,	4735,	5479,	6170,	6933,	7577,	8346,	9169,	9944,	10868,	11667,	12872,	14065,	15241,	16610,	1005,	1838,	2594,	3421,	4207,	5037,	5795,	6491,	7313,	7981,	8748,	9555,	10316,	11424,	12304,	13597,	15125,	1051,	1945,	2750,	3597,	4448,	5265,	6065,	6869,	7707,	8397,	9286,	10022,	10784,	12102,	13067,	1067,	2013,	2864,	3741,	4585,	5401,	6251,	7104,	7875,	8612,	9721,	10470,	11327,	1087,	2033,	2904,	3816,	4653,	5558,	6403,	7250,	8060,	8917,	9917,	1085,	2024,	2901,	3836,	4722,	5629,	6455,	7357,	8213,	9059,	1073,	2009,	2862,	3795,	4694,	5598,	6412,	7280,	8227,	9262,	1044,	1970,	2780,	3670,	4569,	5469,	6274,	7137,	8084,	9136,	1004,	1879,	2671,	3506,	4341,	5223,	5999,	6848,	7798,	8949,	935,	1756,	2518,	3300,	4054,	4846,	5623,	6330,	7122,	8095,	829,	1530,	2202,	2792,	3325,	3889,	4386,	4899,	684,	1259,	1803,	2249,	2652,	2975,	541,	984,	1377,	1694,	1951,	344,	655,	943,	1173,	1416,	136,	307,	498,	659,	836,	-54,	-15,	72,	130,	-211,	-329,	-380,	-424,	-417,	-727,	-923,	-1050,	-1131,	-1215,	-580,	-1079,	-1445,	-1724,	-1970,	-2133,	-686,	-1361,	-1931,	-2410,	-2801,	-3051,	-3357,	-834,	-1708,	-2517,	-3290,	-3997,	-4692,	-5338,	-6079,	-6712,	-905,	-1879,	-2868,	-3806,	-4755,	-5747,	-6663,	-7461,	-8377,	-9523,	-10559,	-11455,	-12913,	-15109,	-975,	-2026,	-3145,	-4201,	-5311,	-6437,	-7440,	-8444,	-9487,	-10470,	-11534,	-12849,	-14289,	-15891,	-17843,	-994,	-2083,	-3171,	-4252,	-5362,	-6573,	-7542,	-8568,	-9596,	-10464,	-11436,	-12849,	-14413,	-16055,	-17779,	-994,	-2090,	-3183,	-4228,	-5298,	-6532,	-7466,	-8450,	-9500,	-10360,	-11296,	-12562,	-14095,	-15825,	-17388,	-996,	-2079,	-3138,	-4177,	-5223,	-6416,	-7353,	-8247,	-9323,	-10213,	-11145,	-12210,	-13506,	-15236,	-16759,	-971,	-2030,	-3048,	-4086,	-5145,	-6186,	-7143,	-8046,	-9079,	-9976,	-10876,	-11853,	-13050,	-14526,	-935,	-1920,	-2880,	-3876,	-4884,	-5859,	-6799,	-7693,	-8617,	-9607,	-10465,	-11244,	-12354,	-885,	-1782,	-2680,	-3602,	-4545,	-5495,	-6415,	-7288,	-8201,	-8988,	-9841,	-10649,	-810,	-1615,	-2381,	-3191,	-4056,	-4887,	-5808,	-6526,	-7440,	-8168,	-9024,	-711,	-1400,	-2029,	-2723,	-3449,	-4112,	-4928,	-5507,	-6368,	-7104,	-602,	-1144,	-1596,	-2090,	-2559,	-3005,	-3521,	-3967,	-4577,	-451,	-839,	-1129,	-1455,	-1706,	-2009,	-2317,	-2762,	-3031,	-291,	-539,	-724,	-900,	-1045,	-1185,	-1331,	-1457,	-1560,	-1736,	-112,	-193,	-225,	-235,	-207,	-154,	-59,	80,	227,	460,	825,	963,	84,	166,	248,	370,	519,	711,	954,	1273,	1649,	2089,	2587,	3112,	3560,	3849,	4263,	4668,	5113,	5786,	6771,	7470,	281,	571,	880,	1250,	1641,	2063,	2561,	3173,	3829,	4610,	5196,	5887,	6613,	7214,	7799,	8611,	9535,	10676,	11678,	12836,	469,	907,	1384,	1884,	2409,	2983,	3577,	4217,	4852,	5598,	6164,	6817,	7586,	8253,	8992,	9838,	10983,	12037,	13256,	14618,	604,	1168,	1711,	2307,	2924,	3591,	4232,	4916,	5621,	6353,	6937,	7611,	8382,	9096,	9810,	10752,	11817,	12805,	14155,	15492,	719,	1357,	1967,	2600,	3280,	3948,	4659,	5421,	6099,	6833,	7448,	8166,	8901,	9725,	10457,	11397,	12772,	13601,	14721,	16290,	813,	1528,	2223,	2923,	3618,	4325,	5090,	5859,	6612,	7344,	8026,	8697,	9525,	10259,	11173,	12077,	13261,	14400,	15449,	16851,	874,	1666,	2429,	3179,	3903,	4654,	5416,	6133,	6977,	7878,	8479,	9211,	10017,	10760,	11761,	12738,	13685,	14846,	16027,	914,	1744,	2590,	3399,	4141,	4942,	5697,	6452,	7288,	8215,	8908,	9679,	10381,	11250,	12534,	13360,	14222,	951,	1814,	2703,	3559,	4280,	5140,	5917,	6704,	7569,	8492,	9203,	10030,	10947,	11724,	12879,	964,	1839,	2726,	3600,	4373,	5214,	6029,	6867,	7749,	8690,	9344,	10203,	11175,	955,	1855,	2719,	3606,	4399,	5278,	6105,	6909,	7823,	8713,	9384,	10215,	944,	1831,	2697,	3567,	4382,	5211,	6088,	6880,	7796,	8641,	9367,	10119,	925,	1810,	2632,	3478,	4261,	5042,	5901,	6701,	7543,	8348,	9001,	9771,	902,	1768,	2559,	3366,	4106,	4848,	5613,	6355,	7111,	7931,	8573,	9360,	857,	1667,	2379,	3109,	3805,	4434,	5064,	5565,	6181,	6707,	7304,	7928,	770,	1483,	2098,	2699,	3210,	3631,	4034,	4319,	4666,	648,	1244,	1740,	2197,	2586,	2911,	3247,	524,	990,	1373,	1690,	1943,	2104,	2321,	318,	650,	924,	1144,	1319,	1441,	112,	284,	438,	578,	695,	762,	-89,	-76,	-20,	48,	45,	96,	-255,	-411,	-482,	-508,	-518,	-531,	-425,	-766,	-1012,	-1161,	-1261,	-1328,	-1382,	-589,	-1088,	-1509,	-1821,	-2046,	-2197,	-2369,	-706,	-1346,	-1965,	-2481,	-2874,	-3178,	-3396,	-3637,	-862,	-1676,	-2492,	-3248,	-3980,	-4640,	-5191,	-5626,	-6021,	-6535,	-956,	-1878,	-2795,	-3737,	-4664,	-5638,	-6488,	-7405,	-8297,	-9229,	-10012,	-10890,	-11901,	-13026,	-14347,	-15733,	-1029,	-2032,	-3042,	-4111,	-5134,	-6275,	-7310,	-8432,	-9588,	-10642,	-11705,	-12724,	-13837,	-15059,	-16800,	-18689,	-1046,	-2087,	-3109,	-4209,	-5242,	-6448,	-7453,	-8587,	-9686,	-10626,	-11619,	-12800,	-13930,	-15244,	-17128,	-19526,	-1048,	-2091,	-3100,	-4204,	-5225,	-6444,	-7423,	-8505,	-9605,	-10530,	-11518,	-12646,	-13706,	-14870,	-16511,	-19021,	-1033,	-2069,	-3043,	-4150,	-5155,	-6378,	-7310,	-8387,	-9479,	-10403,	-11431,	-12451,	-13371,	-14362,	-15953,	-18279,	-1017,	-2017,	-2958,	-4049,	-5078,	-6232,	-7104,	-8213,	-9255,	-10245,	-11209,	-12122,	-12912,	-13911,	-15040,	-16956,	-971,	-1916,	-2806,	-3827,	-4864,	-5932,	-6736,	-7805,	-8948,	-9869,	-10736,	-11620,	-12396,	-13176,	-14047,	-924,	-1789,	-2666,	-3589,	-4559,	-5559,	-6357,	-7371,	-8450,	-9453,	-10349,	-11081,	-11735,	-828,	-1603,	-2391,	-3209,	-4071,	-4951,	-5708,	-6686,	-7689,	-8582,	-9595,	-10270,	-724,	-1414,	-2090,	-2772,	-3499,	-4265,	-4924,	-5760,	-6502,	-7518,	-593,	-1174,	-1672,	-2153,	-2627,	-3158,	-3660,	-4260,	-4782,	-5246,	-463,	-875,	-1230,	-1558,	-1855,	-2172,	-2498,	-2841,	-3141,	-3429,	-293,	-569,	-791,	-1005,	-1178,	-1363,	-1559,	-1756,	-1903,	-2028,	-2331,	-128,	-246,	-299,	-336,	-334,	-338,	-302,	-215,	-138,	-118,	20,	95,	52,	98,	180,	257,	372,	498,	662,	915,	1236,	1614,	1888,	2349,	2873,	3481,	4289,	4866,	222,	451,	762,	1096,	1408,	1826,	2260,	2741,	3358,	4006,	4653,	5473,	6185,	7020,	7900,	8927,	10144,	367,	767,	1250,	1691,	2186,	2713,	3278,	3871,	4436,	5090,	5810,	6515,	7385,	8189,	9178,	10164,	11193,	12596,	478,	1004,	1558,	2162,	2717,	3352,	3969,	4614,	5233,	5936,	6654,	7371,	8105,	9048,	9827,	10928,	12031,	13366,	14478,	570,	1184,	1828,	2487,	3098,	3788,	4413,	5099,	5776,	6462,	7199,	7949,	8654,	9528,	10397,	11431,	12563,	13612,	14659,	15827,	652,	1373,	2100,	2843,	3492,	4220,	4923,	5622,	6374,	7087,	7898,	8517,	9326,	10086,	10951,	11983,	13045,	14024,	14773,	15996,	705,	1480,	2291,	3097,	3800,	4579,	5304,	6039,	6794,	7575,	8290,	9033,	9906,	10603,	11438,	12202,	13287,	14353,	15090,	16030,	755,	1570,	2428,	3310,	4035,	4858,	5641,	6383,	7148,	7919,	8715,	9387,	10190,	10987,	11821,	12614,	13421,	14578,	780,	1646,	2528,	3431,	4211,	5077,	5853,	6663,	7437,	8260,	9014,	9863,	10600,	11293,	12042,	12795,	795,	1683,	2574,	3473,	4257,	5180,	5920,	6756,	7539,	8401,	9178,	9945,	10598,	11232,	11953,	797,	1690,	2576,	3464,	4281,	5202,	5930,	6805,	7616,	8513,	9223,	9904,	10505,	11038,	795,	1686,	2559,	3414,	4264,	5159,	5887,	6742,	7596,	8505,	9142,	9732,	10311,	10805,	783,	1676,	2511,	3342,	4180,	5031,	5753,	6593,	7424,	8155,	8848,	9389,	9892,	10360,	765,	1623,	2421,	3225,	4018,	4819,	5530,	6293,	6997,	7661,	8257,	8809,	9234,	9736,	731,	1500,	2254,	2993,	3705,	4377,	4941,	5475,	6009,	6574,	7154,	7641,	8107,	671,	1338,	2003,	2648,	3217,	3657,	4020,	4301,	4651,	4880,	569,	1108,	1669,	2178,	2599,	2921,	3191,	3437,	3646,	453,	894,	1334,	1697,	1963,	2169,	2327,	2465,	276,	574,	876,	1123,	1302,	1422,	1590,	1638,	90,	223,	386,	529,	641,	722,	807,	-137,	-155,	-105,	-47,	-10,	19,	93,	-272,	-461,	-581,	-634,	-650,	-657,	-670,	-454,	-791,	-1063,	-1262,	-1389,	-1459,	-1523,	-1525,	-623,	-1122,	-1547,	-1922,	-2162,	-2328,	-2462,	-2534,	-754,	-1379,	-1972,	-2533,	-2986,	-3315,	-3553,	-3692,	-3809,	-944,	-1733,	-2499,	-3293,	-4011,	-4655,	-5261,	-5709,	-6040,	-6299,	-6601,	-1055,	-1956,	-2850,	-3773,	-4652,	-5545,	-6493,	-7343,	-8223,	-9037,	-9813,	-10723,	-11486,	-12405,	-13410,	-14260,	-1127,	-2141,	-3115,	-4102,	-5113,	-6078,	-7171,	-8185,	-9289,	-10380,	-11464,	-12651,	-13822,	-15030,	-16508,	-18208,	-20150,	-1135,	-2208,	-3193,	-4185,	-5176,	-6289,	-7293,	-8324,	-9374,	-10438,	-11532,	-12682,	-13854,	-15081,	-16665,	-18310,	-20850,	-1122,	-2218,	-3181,	-4172,	-5164,	-6249,	-7261,	-8274,	-9353,	-10403,	-11435,	-12549,	-13617,	-14786,	-16283,	-17837,	-19956,	-1096,	-2199,	-3136,	-4138,	-5143,	-6220,	-7183,	-8202,	-9241,	-10309,	-11314,	-12434,	-13396,	-14444,	-15770,	-17170,	-19240,	-1081,	-2153,	-3076,	-4075,	-5026,	-6055,	-7019,	-8039,	-9065,	-10075,	-11078,	-12240,	-13284,	-14064,	-15205,	-16465,	-1030,	-2028,	-2948,	-3882,	-4831,	-5779,	-6728,	-7674,	-8659,	-9667,	-10726,	-11899,	-12828,	-13463,	-14270,	-959,	-1901,	-2764,	-3645,	-4545,	-5463,	-6340,	-7280,	-8202,	-9268,	-10410,	-11555,	-12426,	-868,	-1701,	-2513,	-3287,	-4068,	-4922,	-5718,	-6601,	-7450,	-8530,	-9596,	-10736,	-754,	-1487,	-2211,	-2896,	-3521,	-4281,	-4986,	-5803,	-6600,	-7436,	-8282,	-607,	-1204,	-1773,	-2277,	-2743,	-3244,	-3784,	-4408,	-4870,	-5599,	-469,	-899,	-1317,	-1666,	-1991,	-2347,	-2683,	-3080,	-3432,	-3740,	-4073,	-313,	-600,	-874,	-1094,	-1296,	-1510,	-1712,	-1951,	-2195,	-2383,	-2561,	-2916,	-137,	-267,	-368,	-406,	-448,	-481,	-478,	-456,	-365,	-334,	-376,	-336,	9,	39,	107,	193,	262,	366,	525,	685,	859,	1070,	1384,	1750,	2088,	186,	402,	681,	950,	1262,	1654,	2091,	2515,	3002,	3558,	4169,	4762,	5729,	318,	680,	1103,	1515,	1985,	2518,	3073,	3782,	4330,	5023,	5686,	6355,	7395,	8339,	426,	901,	1446,	1990,	2569,	3176,	3808,	4546,	5139,	5948,	6715,	7529,	8492,	9336,	10493,	11660,	525,	1088,	1721,	2309,	2953,	3584,	4288,	4974,	5650,	6421,	7389,	8137,	9064,	10041,	11065,	12065,	13180,	14075,	635,	1269,	2014,	2687,	3417,	4099,	4814,	5545,	6242,	7136,	7965,	8738,	9715,	10493,	11570,	12509,	13399,	14271,	682,	1378,	2189,	2904,	3678,	4412,	5223,	5954,	6668,	7535,	8328,	9088,	9965,	10898,	11687,	12594,	13452,	14244,	720,	1473,	2322,	3070,	3834,	4665,	5479,	6301,	7057,	7928,	8620,	9426,	10218,	10940,	11823,	12624,	13328,	13960,	739,	1537,	2429,	3211,	3973,	4829,	5691,	6498,	7314,	8285,	8937,	9609,	10470,	11091,	11654,	12424,	738,	1558,	2477,	3217,	4045,	4896,	5754,	6654,	7428,	8345,	9077,	9787,	10423,	11046,	11558,	744,	1578,	2474,	3211,	4079,	4925,	5721,	6684,	7475,	8291,	8983,	9683,	10314,	10837,	743,	1579,	2442,	3204,	4053,	4900,	5681,	6603,	7428,	8133,	8828,	9454,	10061,	10607,	742,	1568,	2396,	3157,	3990,	4812,	5580,	6416,	7217,	7902,	8553,	9113,	9558,	10175,	10731,	736,	1522,	2287,	3048,	3862,	4598,	5344,	6093,	6810,	7409,	8020,	8451,	8902,	9442,	9921,	717,	1446,	2126,	2828,	3546,	4222,	4873,	5441,	6009,	6540,	7038,	7409,	7749,	8125,	8592,	653,	1272,	1922,	2531,	3130,	3656,	4097,	4431,	4727,	5042,	5157,	547,	1058,	1583,	2086,	2541,	2914,	3223,	3460,	3626,	3799,	430,	839,	1237,	1615,	1932,	2191,	2367,	2517,	2583,	2696,	242,	492,	787,	1038,	1261,	1434,	1553,	1628,	1728,	27,	138,	288,	457,	582,	676,	743,	820,	858,	-171,	-243,	-216,	-182,	-139,	-104,	-80,	-26,	-318,	-520,	-668,	-751,	-785,	-797,	-809,	-784,	-473,	-826,	-1089,	-1311,	-1483,	-1584,	-1662,	-1709,	-1732,	-624,	-1137,	-1551,	-1931,	-2264,	-2466,	-2613,	-2711,	-2775,	-753,	-1400,	-1961,	-2498,	-3011,	-3419,	-3709,	-3903,	-4042,	-4186,	-959,	-1757,	-2498,	-3244,	-3993,	-4675,	-5313,	-5807,	-6203,	-6480,	-6780,	-7008,	-1076,	-1992,	-2836,	-3731,	-4630,	-5535,	-6411,	-7315,	-8149,	-9017,	-9772,	-10623,	-11334,	-12037,	-12655,	-13289,	-1154,	-2153,	-3091,	-4094,	-5111,	-6149,	-7121,	-8130,	-9089,	-10152,	-11210,	-12233,	-13453,	-14732,	-16253,	-17660,	-19565,	-1171,	-2198,	-3160,	-4160,	-5222,	-6264,	-7365,	-8406,	-9393,	-10487,	-11479,	-12425,	-13296,	-14510,	-15871,	-17259,	-18980,	-1169,	-2190,	-3154,	-4149,	-5226,	-6276,	-7344,	-8398,	-9356,	-10402,	-11435,	-12321,	-13137,	-14305,	-15577,	-16877,	-18304,	-1148,	-2157,	-3126,	-4119,	-5177,	-6203,	-7255,	-8254,	-9233,	-10228,	-11319,	-12233,	-12990,	-14004,	-15195,	-16192,	-1119,	-2110,	-3045,	-4056,	-5082,	-6047,	-7078,	-7971,	-9013,	-9923,	-11017,	-11859,	-12694,	-13660,	-14634,	-15458,	-1075,	-2031,	-2898,	-3887,	-4905,	-5816,	-6712,	-7680,	-8685,	-9571,	-10661,	-11570,	-12296,	-13066,	-13865,	-996,	-1918,	-2728,	-3646,	-4603,	-5515,	-6386,	-7316,	-8268,	-9080,	-10205,	-11138,	-11787,	-885,	-1746,	-2491,	-3331,	-4166,	-5023,	-5833,	-6792,	-7641,	-8378,	-9323,	-10349,	-764,	-1529,	-2213,	-2914,	-3687,	-4424,	-5148,	-6004,	-6679,	-7401,	-8172,	-603,	-1247,	-1821,	-2346,	-2899,	-3483,	-3979,	-4595,	-5283,	-5930,	-468,	-980,	-1409,	-1804,	-2204,	-2612,	-2992,	-3391,	-3887,	-4117,	-305,	-652,	-944,	-1194,	-1433,	-1677,	-1955,	-2153,	-2441,	-2602,	-146,	-288,	-420,	-505,	-573,	-600,	-545,	-544,	-454,	-4,	-2,	19,	85,	172,	244,	440,	670,	933,	156,	351,	601,	898,	1267,	1667,	2020,	2567,	3018,	3558,	265,	621,	1017,	1473,	1975,	2580,	3168,	3724,	4264,	5122,	5834,	373,	860,	1356,	1906,	2519,	3181,	3879,	4553,	5034,	5991,	6879,	7661,	447,	1018,	1600,	2216,	2890,	3577,	4254,	4994,	5750,	6581,	7502,	8320,	9335,	10285,	539,	1226,	1921,	2640,	3398,	4084,	4864,	5642,	6418,	7223,	8145,	9003,	9936,	10844,	11832,	586,	1335,	2111,	2901,	3678,	4390,	5189,	6044,	6855,	7670,	8583,	9412,	10224,	11043,	11916,	624,	1404,	2264,	3098,	3865,	4685,	5493,	6402,	7303,	8216,	8978,	9599,	10447,	11295,	12001,	652,	1448,	2344,	3217,	3971,	4890,	5756,	6679,	7556,	8251,	9064,	9721,	10686,	11423,	669,	1468,	2367,	3220,	4023,	4971,	5877,	6784,	7680,	8433,	9165,	9875,	10667,	678,	1492,	2354,	3215,	4037,	5011,	5891,	6773,	7594,	8272,	8945,	9800,	690,	1501,	2337,	3191,	4007,	4936,	5812,	6704,	7447,	8117,	8753,	9453,	695,	1489,	2302,	3144,	3952,	4846,	5671,	6532,	7239,	7855,	8487,	9196,	687,	1463,	2217,	3053,	3790,	4623,	5373,	6145,	6774,	7361,	7985,	8501,	9016,	653,	1373,	2088,	2819,	3467,	4182,	4863,	5505,	5991,	6478,	7015,	7478,	8001,	599,	1226,	1872,	2472,	3041,	3638,	4158,	4559,	4869,	5109,	5305,	5426,	512,	1011,	1524,	1993,	2449,	2902,	3263,	3539,	3730,	3858,	3949,	391,	789,	1165,	1518,	1848,	2163,	2393,	2556,	2671,	2713,	2819,	204,	442,	689,	940,	1182,	1387,	1550,	1645,	1724,	1722,	-12,	115,	244,	363,	493,	617,	704,	771,	827,	847,	-190,	-304,	-330,	-302,	-263,	-227,	-201,	-189,	-155,	-127,	-337,	-567,	-726,	-843,	-909,	-942,	-960,	-968,	-988,	-501,	-844,	-1144,	-1374,	-1559,	-1709,	-1799,	-1866,	-1911,	-1929,	-644,	-1152,	-1593,	-1970,	-2317,	-2581,	-2774,	-2888,	-2949,	-3009,	-3059,	-771,	-1375,	-1970,	-2509,	-3022,	-3478,	-3839,	-4074,	-4257,	-4337,	-4468,	-994,	-1822,	-2591,	-3298,	-4024,	-4735,	-5407,	-5983,	-6457,	-6761,	-7127,	-7390,	-7681,	-7959,	-1140,	-2054,	-2979,	-3812,	-4736,	-5627,	-6501,	-7388,	-8266,	-9128,	-9911,	-10583,	-11114,	-11791,	-12519,	-13383,	-1221,	-2210,	-3280,	-4205,	-5187,	-6240,	-7157,	-8191,	-9240,	-10230,	-11194,	-12190,	-13109,	-14268,	-15464,	-16791,	-1237,	-2251,	-3351,	-4343,	-5349,	-6406,	-7408,	-8418,	-9466,	-10572,	-11580,	-12628,	-13893,	-15242,	-16796,	-18398,	-1230,	-2256,	-3335,	-4301,	-5324,	-6390,	-7370,	-8400,	-9447,	-10477,	-11532,	-12694,	-13958,	-15261,	-16830,	-1205,	-2234,	-3276,	-4246,	-5238,	-6309,	-7288,	-8280,	-9357,	-10334,	-11408,	-12728,	-13952,	-15225,	-1180,	-2205,	-3191,	-4172,	-5153,	-6189,	-7113,	-8080,	-9160,	-10171,	-11210,	-12472,	-13794,	-1110,	-2101,	-3058,	-4044,	-4976,	-5979,	-6874,	-7806,	-8889,	-9903,	-10869,	-12162,	-13626,	-1009,	-1947,	-2901,	-3806,	-4720,	-5697,	-6530,	-7436,	-8510,	-9359,	-10471,	-11773,	-904,	-1811,	-2680,	-3532,	-4409,	-5322,	-6110,	-7109,	-8057,	-8854,	-9943,	-769,	-1630,	-2399,	-3148,	-3939,	-4744,	-5394,	-6409,	-7105,	-7858,	-594,	-1310,	-1925,	-2566,	-3129,	-3782,	-4363,	-4968,	-5561,	-444,	-1035,	-1532,	-2029,	-2409,	-2898,	-3271,	-3725,	-316,	-676,	-1023,	-1374,	-1663,	-1965,	-2020,	-145,	-324,	-500,	-631,	-764,	-717,	-735,	-30,	-39,	-24,	-11,	149,	271,	699,	128,	320,	554,	891,	1305,	1685,	2206,	234,	578,	1012,	1568,	2169,	2588,	3196,	3618,	308,	813,	1341,	1953,	2723,	3255,	3972,	4506,	5168,	353,	961,	1568,	2268,	2968,	3665,	4413,	5052,	5739,	6598,	7447,	439,	1203,	1859,	2729,	3480,	4276,	5112,	5828,	6591,	7406,	8278,	9313,	10441,	483,	1286,	2086,	2941,	3757,	4573,	5494,	6308,	6985,	7852,	8908,	9897,	10956,	513,	1355,	2243,	3135,	3969,	4897,	5801,	6728,	7383,	8295,	9247,	10156,	11156,	538,	1411,	2334,	3276,	4134,	5157,	6030,	6818,	7623,	8514,	9431,	10262,	554,	1448,	2363,	3316,	4182,	5239,	6102,	6908,	7783,	8572,	9372,	561,	1470,	2355,	3299,	4173,	5316,	6097,	6817,	7606,	8415,	564,	1479,	2347,	3262,	4162,	5220,	6108,	6811,	7487,	8208,	567,	1475,	2299,	3216,	4091,	5159,	5952,	6593,	7304,	556,	1451,	2248,	3099,	3903,	4818,	5576,	6270,	6990,	7499,	545,	1376,	2089,	2828,	3517,	4306,	4950,	5529,	6045,	6558,	7045,	7564,	8045,	517,	1218,	1837,	2488,	3022,	3640,	4239,	4631,	5028,	5330,	5675,	5960,	6186,	451,	975,	1475,	1990,	2421,	2862,	3233,	3612,	3807,	4041,	4118,	4317,	4433,	337,	707,	1106,	1486,	1808,	2115,	2374,	2575,	2729,	2811,	2928,	2987,	161,	374,	619,	868,	1102,	1317,	1498,	1636,	1741,	1803,	1859,	-44,	20,	131,	271,	416,	540,	653,	723,	777,	808,	829,	-227,	-381,	-432,	-442,	-423,	-398,	-366,	-354,	-349,	-311,	-334,	-374,	-611,	-803,	-945,	-1040,	-1084,	-1120,	-1128,	-1143,	-1129,	-533,	-905,	-1206,	-1470,	-1662,	-1823,	-1940,	-2014,	-2073,	-2113,	-2143,	-668,	-1208,	-1650,	-2064,	-2398,	-2703,	-2928,	-3065,	-3174,	-3223,	-3296,	-815,	-1454,	-2050,	-2610,	-3089,	-3553,	-3927,	-4245,	-4464,	-4599,	-4734,	-1067,	-1907,	-2736,	-3485,	-4232,	-4910,	-5557,	-6207,	-6733,	-7176,	-7631,	-8072,	-8538,	-8982,	-1218,	-2179,	-3243,	-4077,	-5018,	-5863,	-6756,	-7662,	-8547,	-9327,	-10061,	-10866,	-11636,	-12429,	-1301,	-2356,	-3523,	-4512,	-5521,	-6480,	-7492,	-8497,	-9652,	-10773,	-11846,	-12828,	-13844,	-14978,	-1313,	-2415,	-3618,	-4678,	-5668,	-6749,	-7734,	-8880,	-10099,	-11216,	-12405,	-13594,	-14643,	-15792,	-1310,	-2429,	-3618,	-4682,	-5656,	-6763,	-7831,	-8948,	-10181,	-11289,	-12380,	-13534,	-1285,	-2392,	-3556,	-4644,	-5585,	-6646,	-7814,	-8900,	-10099,	-11112,	-12277,	-13431,	-1258,	-2345,	-3499,	-4567,	-5507,	-6702,	-7749,	-8820,	-10054,	-10996,	-12147,	-1184,	-2266,	-3388,	-4452,	-5343,	-6566,	-7652,	-8790,	-9954,	-11036,	-1077,	-2124,	-3187,	-4250,	-5128,	-6309,	-7374,	-8575,	-9588,	-961,	-1963,	-2966,	-3944,	-4878,	-5971,	-6957,	-8171,	-9205,	-789,	-1756,	-2688,	-3540,	-4335,	-5267,	-6213,	-7284,	-626,	-1454,	-2160,	-2802,	-3508,	-4202,	-4920,	-5834,	-460,	-1163,	-1847,	-2386,	-2909,	-3548,	-345,	-843,	-1303,	-1636,	-1893,	-147,	-403,	-623,	-803,	-935,	-36,	-25,	-48,	-8,	101,	111,	352,	585,	1038,	1613,	218,	619,	981,	1638,	2253,	293,	818,	1417,	2136,	3007,	333,	999,	1673,	2553,	3415,	4315,	5157,	428,	1269,	2059,	3008,	3922,	4789,	5654,	6567,	466,	1361,	2229,	3285,	4302,	5242,	6129,	7121,	500,	1414,	2377,	3378,	4351,	5320,	6374,	7276,	523,	1477,	2471,	3398,	4454,	5480,	6325,	7209,	537,	1510,	2505,	3365,	4443,	5441,	6296,	540,	1536,	2514,	3360,	4348,	5406,	6328,	549,	1549,	2515,	3287,	4312,	5348,	6258,	547,	1545,	2450,	3343,	4278,	5347,	550,	1519,	2335,	3241,	4088,	5071,	5910,	539,	1426,	2189,	3029,	3706,	4354,	5096,	5592,	518,	1261,	1874,	2581,	3126,	3646,	4136,	4620,	5062,	427,	984,	1480,	1978,	2477,	2842,	3231,	3535,	3816,	4118,	4237,	4392,	4553,	312,	697,	1049,	1405,	1760,	2066,	2292,	2546,	2696,	2850,	2950,	3012,	3108,	128,	336,	550,	784,	1011,	1230,	1405,	1610,	1717,	1822,	1898,	1971,	-67,	-45,	57,	181,	302,	440,	561,	619,	645,	704,	760,	785,	-261,	-433,	-520,	-568,	-580,	-577,	-560,	-537,	-528,	-506,	-490,	-510,	-418,	-688,	-894,	-1045,	-1145,	-1213,	-1261,	-1287,	-1311,	-1282,	-1267,	-547,	-889,	-1284,	-1547,	-1766,	-1898,	-2094,	-2174,	-2255,	-2289,	-2323,	-739,	-1299,	-1705,	-2147,	-2453,	-2796,	-3101,	-3252,	-3453,	-3480,	-3561,	-938,	-1533,	-2144,	-2712,	-3117,	-3542,	-3906,	-4219,	-4642,	-4852,	-4952,	-1202,	-2048,	-2999,	-3693,	-4515,	-5062,	-5672,	-6293,	-6847,	-7324,	-7741,	-1380,	-2353,	-3576,	-4433,	-5494,	-6129,	-6949,	-7833,	-8713,	-9468,	-10101,	-1506,	-2528,	-3793,	-4785,	-5968,	-6741,	-7767,	-8742,	-9877,	-10934,	-11708,	-1534,	-2704,	-3934,	-5029,	-6055,	-7056,	-8149,	-9169,	-10439,	-1503,	-2660,	-3884,	-5078,	-6075,	-7095,	-8162,	-9188,	-1514,	-2651,	-3832,	-5005,	-5918,	-7048,	-8122,	-1427,	-2595,	-3821,	-4954,	-5852,	-6925,	-8059,	-1283,	-2527,	-3633,	-4820,	-5717,	-6683,	-7950,	-1029,	-2270,	-3330,	-4498,	-5408,	-6427,	-874,	-2093,	-3144,	-4178,	-5082,	-6060,	-770,	-1835,	-2913,	-3755,	-4608,	-661,	-1638,	-2556,	-3309,	-499,	-1363,	-2001,	-390,	-962,	-1475,	-171,	-378,	-546,	-115,	-26,	13,	51,	326,	637,	106,	617,	1066,	216,	788,	1480,	326,	1069,	1754,	2566,	435,	1278,	2119,	3017,	542,	1431,	2423,	3392,	4102,	541,	1523,	2586,	3511,	4225,	592,	1656,	2726,	3651,	4362,	644,	1644,	2738,	3666,	642,	1725,	2721,	3708,	640,	1712,	2740,	3638,	638,	1646,	2644,	3516,	688,	1570,	2538,	3373,	633,	1449,	2296,	3145,	580,	1288,	1933,	2623,	477,	988,	1494,	1959,	2481,	276,	745,	1032,	1335,	1697,	1852,	80,	280,	500,	740,	918,	1130,	1257,	1413,	-162,	-128,	-102,	81,	209,	326,	435,	539,	675,	669,	767,	-259,	-442,	-566,	-681,	-750,	-781,	-779,	-785,	-697,	-685,	-673,	-451,	-711,	-986,	-1154,	-1277,	-1362,	-1440,	-1464,	-1463,	-1477,	-1520,
};


const int Munsell_B[5000] =//*100
{
603,	518,	396,	512,	297,	354,	218,	242,	143,	152,	64,	39,	-41,	-150,	-190,	-366,	-425,	-766,	-586,	-1044,	-764,	-1337,	-889,	-1550,	-986,	-1714,	-1040,	-1775,	-1105,	-1870,	-2552,	-1148,	-1981,	-2705,	-1197,	-2011,	-2802,	-3544,	-4406,	-6080,	-1218,	-1991,	-2729,	-3416,	-4100,	-4919,	-5884,	-7224,	-9953,	-20689,	-1196,	-1871,	-2511,	-3088,	-3554,	-4181,	-4862,	-5691,	-7000,	-9667,	-1130,	-1762,	-2340,	-2875,	-3297,	-3784,	-4344,	-4993,	-5930,	-1046,	-1633,	-2120,	-2629,	-3005,	-3423,	-3895,	-990,	-1579,	-2030,	-2520,	-2876,	-3327,	-926,	-1490,	-1970,	-2412,	-2749,	-858,	-1392,	-1845,	-2229,	-2610,	-747,	-1219,	-1627,	-1992,	-597,	-974,	-1317,	-1638,	-442,	-738,	-1006,	-249,	-433,	-589,	-80,	-186,	43,	25,	159,	197,	327,	469,	532,	813,	664,	784,	799,	782,	755,	899,	722,	1222,	580,	1032,	453,	851,	334,	674,	257,	506,	165,	305,	87,	99,	1,	-178,	-144,	-473,	-342,	-920,	-495,	-1206,	-1593,	-693,	-1489,	-2065,	-799,	-1633,	-2262,	-905,	-1764,	-2483,	-987,	-1848,	-2608,	-1066,	-1903,	-2706,	-3890,	-1119,	-1957,	-2816,	-3895,	-1173,	-1985,	-2830,	-3787,	-4407,	-5106,	-5589,	-6186,	-6822,	-8065,	-1215,	-1969,	-2719,	-3418,	-3994,	-4651,	-5070,	-5767,	-6465,	-7408,	-8276,	-10311,	-13940,	-26368,	-1206,	-1868,	-2495,	-3096,	-3684,	-4267,	-4698,	-5231,	-5917,	-6554,	-7595,	-8894,	-11108,	-14934,	-1146,	-1756,	-2331,	-2893,	-3511,	-3963,	-4465,	-4959,	-5583,	-6156,	-7072,	-7911,	-9881,	-1029,	-1620,	-2126,	-2670,	-3234,	-3731,	-4192,	-4710,	-5198,	-5807,	-954,	-1512,	-1992,	-2504,	-3067,	-3572,	-4062,	-4494,	-5112,	-874,	-1410,	-1845,	-2350,	-2966,	-3413,	-3827,	-4347,	-800,	-1294,	-1719,	-2165,	-2728,	-3163,	-3627,	-689,	-1129,	-1499,	-1940,	-2498,	-2865,	-528,	-900,	-1232,	-1592,	-2129,	-379,	-660,	-936,	-1246,	-1735,	-188,	-357,	-536,	-781,	-1137,	-6,	-66,	-153,	-274,	131,	147,	121,	65,	250,	337,	390,	409,	446,	635,	765,	921,	631,	936,	1115,	792,	1187,	946,	1033,	1054,	1017,	941,	1887,	764,	1654,	623,	1427,	495,	1136,	1316,	366,	869,	1059,	295,	639,	805,	216,	415,	474,	126,	198,	130,	22,	-63,	-297,	-94,	-365,	-774,	-290,	-792,	-1397,	-433,	-1042,	-1675,	-603,	-1352,	-2050,	-750,	-1541,	-2278,	-3309,	-884,	-1692,	-2428,	-3516,	-973,	-1783,	-2558,	-3581,	-1037,	-1837,	-2665,	-3658,	-1081,	-1898,	-2741,	-3621,	-4480,	-5168,	-1120,	-1925,	-2763,	-3605,	-4340,	-4948,	-5520,	-6156,	-6984,	-8130,	-9382,	-1148,	-1924,	-2675,	-3339,	-3968,	-4503,	-4998,	-5536,	-6212,	-6982,	-7692,	-8616,	-10230,	-13165,	-17630,	-1138,	-1805,	-2413,	-3040,	-3533,	-3986,	-4552,	-5016,	-5587,	-6215,	-6902,	-7629,	-8819,	-10697,	-14531,	-1080,	-1695,	-2235,	-2792,	-3242,	-3683,	-4333,	-4713,	-5301,	-5793,	-6392,	-7017,	-8000,	-9759,	-1011,	-1539,	-2029,	-2562,	-2970,	-3404,	-3988,	-4427,	-5033,	-5405,	-6037,	-6476,	-922,	-1432,	-1883,	-2389,	-2805,	-3250,	-3844,	-4370,	-4777,	-5266,	-858,	-1339,	-1769,	-2250,	-2664,	-3051,	-3683,	-4082,	-4565,	-780,	-1220,	-1609,	-2053,	-2455,	-2812,	-3421,	-3877,	-648,	-1018,	-1388,	-1751,	-2139,	-2473,	-2999,	-3418,	-488,	-798,	-1115,	-1426,	-1771,	-2117,	-2569,	-323,	-581,	-839,	-1100,	-1407,	-1749,	-144,	-287,	-443,	-624,	-827,	-1158,	29,	21,	-15,	-87,	-153,	198,	266,	306,	298,	276,	331,	496,	600,	660,	717,	537,	852,	1050,	1281,	1439,	748,	1168,	1476,	1778,	887,	1479,	1051,	1805,	1160,	1953,	1188,	1117,	1996,	1012,	2352,	827,	2050,	681,	1658,	2092,	534,	1269,	1689,	1812,	411,	960,	1341,	1495,	315,	719,	994,	1123,	240,	488,	632,	695,	159,	276,	284,	175,	70,	36,	-112,	-416,	-79,	-292,	-564,	-978,	-253,	-668,	-1097,	-1685,	-446,	-994,	-1500,	-2106,	-608,	-1261,	-1879,	-2539,	-699,	-1417,	-2103,	-2795,	-828,	-1580,	-2345,	-3140,	-911,	-1701,	-2487,	-3262,	-4257,	-986,	-1797,	-2592,	-3413,	-4402,	-1050,	-1850,	-2683,	-3535,	-4386,	-5545,	-1108,	-1908,	-2746,	-3546,	-4222,	-5326,	-6388,	-7505,	-8662,	-10414,	-1121,	-1890,	-2654,	-3352,	-3841,	-4525,	-5381,	-6094,	-6720,	-7511,	-8571,	-10105,	-11786,	-13912,	-17881,	-25811,	1057626,	266691,	-1090,	-1803,	-2413,	-2996,	-3423,	-3976,	-4535,	-5123,	-5583,	-6123,	-6795,	-7519,	-8123,	-9065,	-9787,	-11041,	-12884,	-14225,	-1030,	-1667,	-2190,	-2704,	-3086,	-3540,	-4061,	-4606,	-4987,	-5437,	-5889,	-6427,	-6854,	-7634,	-7941,	-8664,	-9326,	-10460,	-933,	-1529,	-1980,	-2452,	-2816,	-3290,	-3805,	-4260,	-4605,	-4945,	-5397,	-5776,	-6210,	-6617,	-7219,	-846,	-1405,	-1841,	-2289,	-2594,	-3034,	-3575,	-4040,	-4317,	-4623,	-4970,	-5300,	-5671,	-774,	-1294,	-1689,	-2127,	-2416,	-2825,	-3325,	-3714,	-4001,	-4272,	-4577,	-4925,	-694,	-1157,	-1525,	-1917,	-2204,	-2602,	-3091,	-3377,	-3631,	-3915,	-4141,	-569,	-940,	-1261,	-1614,	-1861,	-2229,	-2573,	-2863,	-3102,	-3268,	-435,	-725,	-996,	-1297,	-1521,	-1796,	-2118,	-2357,	-277,	-482,	-702,	-958,	-1157,	-1384,	-1633,	-108,	-204,	-335,	-494,	-642,	-813,	-998,	97,	90,	67,	17,	-30,	-104,	240,	347,	429,	489,	534,	573,	381,	629,	817,	936,	1053,	1115,	624,	1020,	1269,	1514,	1810,	2088,	841,	1393,	1744,	2076,	995,	1693,	2150,	1113,	2073,	1205,	2302,	1225,	2475,	1152,	2487,	1062,	2056,	893,	1931,	2231,	757,	1718,	2084,	578,	1365,	1786,	2089,	426,	1041,	1460,	1782,	1870,	319,	736,	1056,	1235,	1302,	231,	508,	694,	779,	792,	137,	281,	363,	371,	347,	47,	58,	6,	-125,	-265,	-85,	-245,	-439,	-640,	-832,	-250,	-616,	-974,	-1378,	-1844,	-381,	-870,	-1366,	-1895,	-535,	-1165,	-1787,	-2407,	-647,	-1363,	-2080,	-2807,	-761,	-1547,	-2303,	-3089,	-4121,	-862,	-1688,	-2452,	-3296,	-4261,	-950,	-1767,	-2560,	-3411,	-4318,	-1011,	-1819,	-2647,	-3501,	-4381,	-5405,	-6175,	-1046,	-1844,	-2686,	-3528,	-4409,	-5198,	-5775,	-6493,	-7010,	-7636,	-8126,	-8696,	-9191,	-9763,	-10201,	-10694,	-11257,	-11909,	-12677,	-13606,	-14758,	-16272,	-1070,	-1835,	-2622,	-3325,	-3923,	-4484,	-5026,	-5624,	-6068,	-6579,	-7068,	-7622,	-8186,	-8796,	-9470,	-10195,	-10787,	-11414,	-12156,	-13051,	-14169,	-15631,	-17680,	-20918,	-1057,	-1742,	-2419,	-2983,	-3547,	-4104,	-4626,	-5174,	-5623,	-6131,	-6611,	-7170,	-7819,	-8475,	-9207,	-9850,	-10352,	-11250,	-11984,	-12404,	-13398,	-14656,	-16352,	-18852,	-989,	-1596,	-2146,	-2690,	-3182,	-3720,	-4211,	-4730,	-5149,	-5627,	-6142,	-6627,	-7294,	-7867,	-8653,	-9434,	-9904,	-10441,	-11401,	-12195,	-13170,	-901,	-1449,	-1938,	-2437,	-2905,	-3394,	-3884,	-4366,	-4747,	-5202,	-5714,	-6097,	-6737,	-7299,	-8133,	-8826,	-829,	-1330,	-1783,	-2251,	-2697,	-3148,	-3644,	-4076,	-4450,	-4867,	-5320,	-5738,	-6324,	-740,	-1196,	-1628,	-2076,	-2493,	-2917,	-3385,	-3781,	-4153,	-4567,	-5013,	-5474,	-635,	-1050,	-1434,	-1853,	-2236,	-2619,	-3049,	-3422,	-3795,	-4230,	-4688,	-489,	-834,	-1163,	-1527,	-1859,	-2198,	-2523,	-2829,	-3127,	-3578,	-359,	-623,	-884,	-1167,	-1453,	-1735,	-2007,	-2257,	-207,	-386,	-583,	-815,	-1051,	-1308,	-1538,	-1745,	-39,	-111,	-212,	-340,	-484,	-649,	-778,	135,	183,	199,	171,	117,	21,	289,	475,	604,	675,	709,	713,	455,	782,	999,	1159,	1272,	1370,	680,	1187,	1470,	1682,	1828,	1957,	907,	1571,	1956,	2283,	2658,	1055,	1820,	2241,	1181,	1979,	1251,	2059,	1246,	2103,	1189,	2102,	1065,	2631,	3893,	928,	2334,	3652,	4565,	791,	1967,	3200,	4162,	4671,	601,	1512,	2473,	3285,	3829,	4188,	4494,	468,	1118,	1762,	2433,	2971,	3337,	3567,	3726,	3778,	335,	739,	1103,	1411,	1740,	1975,	2131,	2227,	2307,	237,	513,	727,	879,	1015,	1110,	1166,	1198,	1198,	153,	313,	422,	496,	544,	572,	576,	570,	546,	59,	105,	121,	110,	78,	26,	-34,	-109,	-173,	-52,	-145,	-244,	-363,	-487,	-603,	-709,	-811,	-918,	-201,	-464,	-696,	-980,	-1231,	-1443,	-1651,	-1822,	-318,	-705,	-1061,	-1438,	-1760,	-2024,	-2282,	-2516,	-450,	-983,	-1485,	-2031,	-2507,	-2976,	-3322,	-553,	-1193,	-1828,	-2481,	-3110,	-3618,	-647,	-1388,	-2156,	-2966,	-3719,	-4360,	-753,	-1560,	-2384,	-3235,	-4050,	-4696,	-5457,	-847,	-1679,	-2528,	-3410,	-4226,	-4965,	-5789,	-899,	-1756,	-2639,	-3528,	-4357,	-5167,	-6083,	-6872,	-7756,	-947,	-1803,	-2680,	-3562,	-4402,	-5183,	-6051,	-6866,	-7606,	-8120,	-8712,	-9397,	-9917,	-10201,	-10504,	-10828,	-11364,	-11756,	-12412,	-13160,	-14043,	-15091,	-16391,	-970,	-1802,	-2605,	-3396,	-4152,	-4770,	-5382,	-5978,	-6560,	-7083,	-7654,	-8161,	-8630,	-9098,	-9639,	-10163,	-10704,	-11257,	-11888,	-12611,	-13457,	-14469,	-15726,	-17329,	-19525,	-945,	-1688,	-2376,	-3107,	-3764,	-4318,	-4898,	-5437,	-5985,	-6543,	-7079,	-7612,	-8154,	-8677,	-9301,	-9877,	-10436,	-11058,	-11679,	-12392,	-13237,	-14239,	-15474,	-17069,	-19232,	-875,	-1529,	-2100,	-2761,	-3341,	-3899,	-4431,	-4937,	-5454,	-5978,	-6510,	-7064,	-7626,	-8257,	-8940,	-9648,	-10120,	-10833,	-11454,	-12160,	-12995,	-13994,	-785,	-1363,	-1866,	-2480,	-3001,	-3524,	-4052,	-4508,	-4990,	-5478,	-5983,	-6519,	-7093,	-7707,	-8502,	-9274,	-9881,	-10587,	-688,	-1191,	-1651,	-2208,	-2702,	-3178,	-3662,	-4118,	-4562,	-4989,	-5519,	-5997,	-6541,	-7273,	-8182,	-578,	-1005,	-1421,	-1920,	-2350,	-2792,	-3237,	-3669,	-4096,	-4510,	-4981,	-5530,	-6029,	-6768,	-425,	-736,	-1058,	-1458,	-1820,	-2191,	-2585,	-2950,	-3314,	-3677,	-4136,	-4571,	-5083,	-265,	-487,	-725,	-1032,	-1330,	-1614,	-1921,	-2218,	-2482,	-2818,	-3146,	-137,	-278,	-429,	-628,	-842,	-1071,	-1313,	-1546,	-1801,	-2091,	-9,	-55,	-113,	-207,	-329,	-471,	-634,	-798,	-966,	138,	220,	275,	310,	321,	292,	246,	189,	285,	505,	703,	916,	1033,	1126,	1218,	1241,	410,	791,	1153,	1531,	1863,	2146,	2391,	2596,	563,	1110,	1645,	2222,	2674,	3036,	3340,	3584,	701,	1436,	2212,	3025,	3592,	4174,	4757,	5217,	854,	1806,	2825,	3746,	4447,	996,	2125,	3250,	4158,	1099,	2375,	3515,	4344,	1178,	2592,	3687,	4481,	1193,	2713,	3837,	1147,	2744,	3907,	1243,	2634,	4183,	5615,	1130,	2476,	3884,	5352,	6462,	1000,	2177,	3407,	4884,	5935,	6771,	772,	1727,	2706,	3820,	4862,	5645,	6222,	610,	1330,	2051,	2787,	3519,	4270,	4902,	5329,	5695,	5928,	6090,	6203,	433,	889,	1299,	1646,	1983,	2264,	2501,	2673,	2807,	2921,	3008,	3065,	299,	619,	871,	1051,	1210,	1328,	1417,	1478,	1524,	1559,	1593,	1613,	198,	401,	531,	616,	688,	740,	770,	796,	812,	816,	813,	801,	86,	158,	195,	212,	210,	196,	170,	136,	99,	59,	16,	-45,	-43,	-114,	-190,	-277,	-382,	-477,	-569,	-655,	-736,	-804,	-878,	-963,	-201,	-449,	-666,	-897,	-1125,	-1336,	-1520,	-1703,	-1845,	-1969,	-2126,	-341,	-739,	-1080,	-1399,	-1712,	-1977,	-2231,	-2452,	-2666,	-2875,	-477,	-1021,	-1516,	-1966,	-2441,	-2834,	-3237,	-3527,	-3761,	-600,	-1278,	-1873,	-2457,	-3051,	-3583,	-4117,	-4488,	-713,	-1488,	-2181,	-2908,	-3649,	-4328,	-5014,	-5575,	-820,	-1650,	-2400,	-3189,	-3937,	-4716,	-5475,	-6083,	-905,	-1753,	-2550,	-3351,	-4121,	-4941,	-5708,	-6378,	-7227,	-962,	-1834,	-2658,	-3468,	-4267,	-5076,	-5882,	-6665,	-7529,	-8242,	-9068,	-997,	-1871,	-2711,	-3483,	-4302,	-5126,	-5923,	-6791,	-7615,	-8315,	-8988,	-9762,	-10308,	-10909,	-11453,	-12204,	-12714,	-13077,	-13674,	-14327,	-14809,	-15331,	-15901,	-16858,	-17979,	-1008,	-1877,	-2694,	-3439,	-4209,	-4921,	-5653,	-6370,	-6950,	-7478,	-8098,	-8683,	-9199,	-9664,	-10258,	-10822,	-11247,	-11861,	-12545,	-13112,	-13751,	-14459,	-15262,	-16197,	-17281,	-992,	-1787,	-2517,	-3219,	-3941,	-4524,	-5176,	-5800,	-6318,	-6851,	-7399,	-7954,	-8483,	-9014,	-9533,	-10183,	-10679,	-11295,	-11924,	-12644,	-13461,	-14170,	-14964,	-15878,	-16949,	-923,	-1653,	-2304,	-2913,	-3539,	-4076,	-4669,	-5217,	-5717,	-6204,	-6752,	-7282,	-7781,	-8336,	-8893,	-9418,	-9923,	-10737,	-11333,	-12002,	-12959,	-13633,	-14667,	-837,	-1474,	-2043,	-2611,	-3187,	-3718,	-4224,	-4719,	-5199,	-5632,	-6149,	-6674,	-7136,	-7671,	-8169,	-8725,	-9460,	-10064,	-10750,	-11538,	-12461,	-704,	-1254,	-1755,	-2287,	-2776,	-3290,	-3753,	-4181,	-4647,	-5033,	-5485,	-5967,	-6411,	-6940,	-7397,	-8056,	-8726,	-9386,	-10146,	-559,	-1022,	-1446,	-1904,	-2341,	-2795,	-3212,	-3580,	-4015,	-4373,	-4769,	-5197,	-5595,	-6156,	-6611,	-7274,	-8049,	-365,	-690,	-993,	-1315,	-1651,	-1972,	-2289,	-2610,	-2947,	-3226,	-3580,	-3883,	-4197,	-4710,	-5106,	-161,	-339,	-518,	-726,	-943,	-1163,	-1411,	-1665,	-1903,	-2134,	-2502,	-2765,	-3032,	-1,	-31,	-83,	-163,	-250,	-366,	-494,	-643,	-798,	-943,	-1161,	166,	290,	387,	465,	518,	554,	561,	560,	530,	459,	316,	578,	810,	1050,	1247,	1415,	1555,	1684,	1793,	1858,	454,	876,	1259,	1671,	2042,	2344,	2611,	2887,	3126,	3300,	581,	1142,	1664,	2242,	2834,	3376,	3850,	4325,	4770,	5169,	715,	1384,	2057,	2796,	3571,	4442,	5111,	5691,	6378,	7118,	858,	1679,	2583,	3573,	4705,	5633,	6412,	7080,	999,	1939,	3001,	4186,	5317,	6270,	1109,	2181,	3350,	4642,	5780,	1210,	2385,	3683,	4985,	6068,	1275,	2548,	3964,	5288,	6319,	1296,	2634,	4130,	5533,	1291,	2663,	4229,	5660,	1384,	2863,	4269,	5776,	7165,	7983,	1245,	2675,	4018,	5499,	7050,	7983,	1129,	2398,	3625,	4991,	6583,	7671,	8754,	880,	1909,	2953,	4080,	5257,	6585,	7575,	8489,	9157,	716,	1501,	2327,	3127,	3951,	4836,	5783,	6567,	7295,	7955,	8402,	8718,	9032,	9237,	488,	983,	1477,	1901,	2309,	2687,	2965,	3194,	3388,	3536,	3665,	3770,	3858,	3984,	4101,	348,	691,	990,	1226,	1415,	1580,	1689,	1772,	1844,	1892,	1930,	1968,	1996,	2041,	2036,	231,	451,	621,	747,	854,	927,	973,	1014,	1050,	1068,	1083,	1093,	1097,	1094,	1095,	118,	211,	272,	310,	333,	339,	338,	319,	299,	275,	249,	223,	197,	163,	137,	-20,	-67,	-130,	-205,	-302,	-396,	-477,	-547,	-616,	-681,	-742,	-802,	-851,	-914,	-169,	-383,	-599,	-826,	-1047,	-1250,	-1441,	-1611,	-1778,	-1937,	-2056,	-2130,	-2216,	-311,	-655,	-1003,	-1349,	-1691,	-1997,	-2270,	-2500,	-2709,	-2888,	-3047,	-3171,	-439,	-930,	-1412,	-1909,	-2389,	-2813,	-3217,	-3509,	-3843,	-4106,	-4376,	-552,	-1172,	-1749,	-2383,	-2982,	-3505,	-4077,	-4413,	-4950,	-5261,	-646,	-1356,	-2041,	-2798,	-3488,	-4173,	-4888,	-5557,	-6184,	-727,	-1503,	-2246,	-3043,	-3767,	-4508,	-5279,	-6066,	-6634,	-785,	-1593,	-2356,	-3184,	-3932,	-4701,	-5491,	-6293,	-7036,	-7711,	-831,	-1662,	-2455,	-3292,	-4047,	-4813,	-5643,	-6471,	-7277,	-8100,	-8974,	-9557,	-873,	-1687,	-2495,	-3338,	-4104,	-4853,	-5672,	-6499,	-7373,	-8313,	-9130,	-9811,	-10376,	-10903,	-11467,	-11953,	-12479,	-13198,	-14369,	-15138,	-884,	-1686,	-2469,	-3264,	-4033,	-4765,	-5480,	-6230,	-6955,	-7749,	-8291,	-8911,	-9539,	-10054,	-10565,	-11236,	-11989,	-12834,	-13648,	-14575,	-874,	-1618,	-2362,	-3071,	-3770,	-4452,	-5116,	-5717,	-6321,	-6983,	-7460,	-8000,	-8609,	-9141,	-9704,	-10399,	-11184,	-11961,	-12847,	-13883,	-825,	-1519,	-2152,	-2777,	-3378,	-3993,	-4560,	-5130,	-5696,	-6276,	-6719,	-7218,	-7788,	-8301,	-8804,	-9457,	-10206,	-10889,	-11793,	-12690,	-737,	-1357,	-1920,	-2471,	-3025,	-3552,	-4087,	-4643,	-5132,	-5646,	-6072,	-6554,	-7044,	-7590,	-8059,	-8654,	-9500,	-10013,	-10693,	-11585,	-606,	-1123,	-1607,	-2088,	-2554,	-3022,	-3514,	-3993,	-4470,	-4912,	-5330,	-5736,	-6225,	-6657,	-7197,	-7717,	-8405,	-9024,	-9625,	-10413,	-458,	-873,	-1277,	-1674,	-2058,	-2450,	-2851,	-3219,	-3660,	-4129,	-4432,	-4806,	-5222,	-5608,	-6115,	-6597,	-7059,	-7653,	-8223,	-299,	-578,	-872,	-1156,	-1425,	-1720,	-2001,	-2287,	-2612,	-2968,	-3237,	-3534,	-3805,	-4143,	-4659,	-4988,	-5354,	-110,	-227,	-355,	-498,	-633,	-801,	-965,	-1142,	-1347,	-1571,	-1745,	-1997,	-2245,	-2462,	-2780,	46,	73,	87,	74,	46,	-5,	-60,	-139,	-239,	-361,	-471,	-608,	-788,	176,	331,	472,	597,	698,	791,	855,	904,	949,	966,	964,	922,	303,	586,	853,	1110,	1339,	1564,	1776,	1945,	2136,	2334,	2419,	2609,	443,	891,	1313,	1749,	2153,	2564,	2991,	3388,	3772,	4112,	4439,	4784,	540,	1119,	1678,	2280,	2843,	3428,	4064,	4640,	5260,	5961,	6706,	7354,	693,	1431,	2150,	2926,	3731,	4654,	5822,	6770,	7865,	8905,	9942,	11109,	871,	1745,	2607,	3555,	4622,	5916,	7413,	8526,	9808,	1037,	2067,	3045,	4078,	5284,	6646,	7971,	1163,	2340,	3411,	4556,	5951,	7162,	8477,	1317,	2580,	3748,	4970,	6427,	7451,	1405,	2755,	4009,	5346,	6766,	7681,	1445,	2875,	4201,	5598,	6993,	7860,	1442,	2917,	4290,	5741,	7141,	7940,	1397,	2880,	4381,	5798,	7228,	8700,	9849,	1292,	2657,	4099,	5542,	6929,	8500,	9670,	1171,	2384,	3685,	5014,	6366,	7976,	9257,	10255,	952,	1926,	2983,	4049,	5240,	6464,	7849,	9061,	10106,	11081,	759,	1517,	2325,	3155,	3993,	4882,	5701,	6670,	7832,	8927,	9706,	10404,	10972,	11551,	11969,	12308,	543,	1045,	1539,	2019,	2440,	2871,	3246,	3582,	3865,	4100,	4304,	4458,	4589,	4702,	4823,	4914,	377,	725,	1028,	1312,	1533,	1739,	1877,	2005,	2108,	2178,	2231,	2299,	2339,	2378,	2406,	2464,	265,	490,	672,	840,	962,	1077,	1152,	1211,	1269,	1308,	1341,	1367,	1382,	1401,	1399,	1384,	132,	241,	324,	395,	442,	479,	494,	501,	502,	495,	476,	462,	449,	434,	420,	408,	16,	10,	-18,	-70,	-135,	-227,	-299,	-394,	-475,	-548,	-626,	-685,	-742,	-787,	-850,	-922,	-154,	-318,	-482,	-695,	-912,	-1110,	-1256,	-1450,	-1639,	-1775,	-1911,	-2017,	-2126,	-2206,	-2285,	-292,	-567,	-861,	-1189,	-1519,	-1845,	-2087,	-2365,	-2633,	-2867,	-3056,	-3225,	-3327,	-419,	-851,	-1308,	-1757,	-2216,	-2655,	-3007,	-3418,	-3809,	-4152,	-4434,	-4652,	-515,	-1069,	-1637,	-2206,	-2780,	-3372,	-3853,	-4402,	-4873,	-5434,	-602,	-1246,	-1937,	-2641,	-3323,	-4048,	-4705,	-5437,	-6137,	-6765,	-667,	-1402,	-2139,	-2884,	-3625,	-4415,	-5157,	-5971,	-6717,	-7350,	-729,	-1496,	-2264,	-3044,	-3825,	-4620,	-5399,	-6235,	-6936,	-7746,	-8492,	-760,	-1540,	-2369,	-3162,	-3966,	-4768,	-5552,	-6426,	-7285,	-8127,	-8981,	-9952,	-771,	-1566,	-2403,	-3195,	-3993,	-4795,	-5603,	-6483,	-7393,	-8274,	-9008,	-10079,	-11117,	-12203,	-13322,	-14505,	-772,	-1547,	-2387,	-3158,	-3882,	-4690,	-5484,	-6257,	-7083,	-7876,	-8559,	-9406,	-10192,	-10957,	-11825,	-12810,	-13945,	-739,	-1487,	-2287,	-2993,	-3672,	-4380,	-5092,	-5792,	-6378,	-7041,	-7691,	-8350,	-9113,	-9789,	-10643,	-11504,	-12346,	-13440,	-681,	-1392,	-2082,	-2757,	-3345,	-3964,	-4538,	-5119,	-5662,	-6254,	-6850,	-7421,	-7979,	-8727,	-9297,	-10111,	-10930,	-11844,	-12623,	-598,	-1223,	-1840,	-2436,	-2969,	-3546,	-4049,	-4587,	-5099,	-5607,	-6133,	-6664,	-7152,	-7738,	-8364,	-9096,	-9802,	-10500,	-11152,	-11872,	-465,	-979,	-1496,	-2005,	-2446,	-2935,	-3380,	-3833,	-4311,	-4767,	-5256,	-5643,	-6135,	-6582,	-7098,	-7712,	-8301,	-8877,	-9325,	-9991,	-355,	-737,	-1144,	-1556,	-1919,	-2321,	-2687,	-3060,	-3445,	-3831,	-4191,	-4568,	-4995,	-5344,	-5758,	-6134,	-6675,	-7200,	-7548,	-7998,	-225,	-481,	-756,	-1046,	-1296,	-1583,	-1855,	-2118,	-2391,	-2678,	-2972,	-3221,	-3520,	-3828,	-4146,	-4436,	-4756,	-5185,	-59,	-145,	-239,	-344,	-454,	-580,	-707,	-848,	-983,	-1134,	-1280,	-1457,	-1633,	-1800,	-1961,	-2145,	60,	124,	162,	181,	190,	174,	151,	112,	60,	-1,	-71,	-140,	-239,	-293,	-386,	174,	367,	542,	707,	841,	972,	1063,	1161,	1253,	1342,	1355,	1445,	1468,	1507,	265,	576,	889,	1189,	1462,	1735,	1950,	2177,	2386,	2598,	2679,	2782,	2870,	2952,	377,	834,	1289,	1740,	2197,	2638,	3026,	3459,	3900,	4247,	4561,	4768,	5044,	5274,	497,	1081,	1678,	2311,	2925,	3552,	4169,	4853,	5483,	6117,	7063,	8143,	8712,	9635,	650,	1404,	2190,	3005,	3880,	4813,	5694,	6774,	8369,	9920,	11426,	12812,	13946,	827,	1676,	2612,	3604,	4623,	5674,	6846,	8298,	9995,	11421,	982,	1997,	3086,	4194,	5264,	6390,	7846,	9332,	10570,	1102,	2247,	3461,	4662,	5825,	7148,	8543,	9972,	1258,	2527,	3855,	5104,	6346,	7751,	9174,	10338,	1363,	2736,	4163,	5443,	6694,	8235,	9519,	1429,	2882,	4365,	5679,	7025,	8547,	9737,	1436,	2923,	4451,	5822,	7240,	8673,	9934,	1485,	2883,	4373,	5873,	7254,	8577,	10415,	11299,	1402,	2735,	4126,	5619,	6993,	8403,	10163,	10997,	1297,	2505,	3748,	5101,	6455,	7825,	9389,	10717,	11793,	1076,	2051,	3061,	4180,	5284,	6410,	7728,	9171,	10463,	11389,	12484,	846,	1626,	2423,	3255,	4049,	4877,	5779,	6609,	7573,	8663,	10052,	11508,	12639,	13695,	14486,	15040,	616,	1139,	1652,	2135,	2607,	3036,	3477,	3859,	4210,	4527,	4803,	5050,	5260,	5439,	5581,	5715,	5841,	421,	789,	1105,	1375,	1611,	1841,	2014,	2170,	2299,	2411,	2506,	2585,	2661,	2708,	2774,	2800,	2817,	292,	546,	728,	907,	1053,	1190,	1291,	1381,	1459,	1522,	1577,	1618,	1662,	1704,	1726,	1759,	1772,	157,	292,	385,	473,	546,	602,	643,	668,	688,	701,	707,	708,	703,	693,	685,	668,	677,	38,	60,	55,	19,	-32,	-94,	-149,	-217,	-291,	-357,	-429,	-511,	-564,	-602,	-644,	-726,	-119,	-263,	-419,	-583,	-743,	-916,	-1078,	-1236,	-1389,	-1534,	-1732,	-1864,	-1982,	-2066,	-2134,	-265,	-545,	-817,	-1081,	-1363,	-1632,	-1876,	-2132,	-2373,	-2633,	-2882,	-3086,	-3282,	-382,	-808,	-1231,	-1628,	-2022,	-2445,	-2824,	-3221,	-3575,	-3949,	-4307,	-4662,	-475,	-1000,	-1567,	-2109,	-2617,	-3193,	-3686,	-4226,	-4672,	-5172,	-5722,	-556,	-1179,	-1866,	-2551,	-3211,	-3890,	-4585,	-5311,	-5967,	-6593,	-611,	-1311,	-2066,	-2807,	-3530,	-4321,	-5098,	-5951,	-6724,	-7436,	-8140,	-658,	-1387,	-2189,	-2967,	-3723,	-4584,	-5418,	-6330,	-7142,	-7835,	-8442,	-9253,	-685,	-1437,	-2273,	-3082,	-3885,	-4770,	-5632,	-6618,	-7396,	-8187,	-8910,	-9767,	-691,	-1457,	-2328,	-3130,	-3915,	-4812,	-5717,	-6748,	-7420,	-8288,	-9081,	-9954,	-10937,	-695,	-1461,	-2299,	-3038,	-3801,	-4689,	-5570,	-6513,	-7224,	-7999,	-8774,	-9553,	-10680,	-663,	-1404,	-2183,	-2881,	-3601,	-4388,	-5148,	-6020,	-6646,	-7450,	-8197,	-8852,	-9813,	-10723,	-633,	-1292,	-2018,	-2670,	-3324,	-3953,	-4589,	-5257,	-5827,	-6537,	-7204,	-7855,	-8648,	-9371,	-10242,	-11121,	-575,	-1159,	-1789,	-2359,	-2951,	-3491,	-4081,	-4648,	-5194,	-5793,	-6522,	-7064,	-7712,	-8433,	-9135,	-9822,	-10576,	-11200,	-438,	-886,	-1420,	-1879,	-2383,	-2828,	-3301,	-3770,	-4216,	-4777,	-5296,	-5766,	-6364,	-6851,	-7504,	-8075,	-8614,	-9099,	-325,	-686,	-1078,	-1436,	-1819,	-2187,	-2594,	-2967,	-3333,	-3771,	-4172,	-4553,	-4992,	-5465,	-5827,	-6284,	-6708,	-7093,	-195,	-417,	-674,	-908,	-1154,	-1430,	-1696,	-1972,	-2236,	-2543,	-2792,	-3079,	-3374,	-3634,	-3942,	-4232,	-4499,	-4724,	-43,	-99,	-182,	-258,	-341,	-446,	-574,	-693,	-824,	-990,	-1102,	-1251,	-1407,	-1551,	-1661,	-1844,	66,	133,	195,	238,	269,	275,	265,	247,	214,	172,	154,	72,	6,	-80,	-165,	177,	370,	570,	729,	903,	1061,	1195,	1355,	1469,	1538,	1595,	1655,	1660,	1737,	274,	579,	920,	1210,	1517,	1806,	2059,	2332,	2581,	2807,	2948,	3031,	3179,	3176,	367,	812,	1272,	1679,	2137,	2569,	2987,	3451,	3894,	4220,	4525,	4740,	4920,	5129,	5249,	513,	1079,	1638,	2238,	2888,	3516,	4183,	4889,	5605,	6169,	6982,	7571,	8167,	9081,	9912,	661,	1415,	2139,	2905,	3775,	4666,	5632,	6651,	8035,	10002,	12190,	14066,	15539,	17230,	18870,	842,	1687,	2590,	3485,	4494,	5574,	6710,	7910,	9563,	11192,	12422,	1000,	1980,	3059,	4130,	5235,	6384,	7592,	9033,	10386,	11954,	1137,	2229,	3409,	4604,	5844,	7069,	8343,	10057,	11119,	12133,	1296,	2515,	3806,	5084,	6353,	7626,	9047,	10522,	11421,	1421,	2729,	4118,	5473,	6799,	8048,	9643,	10916,	11849,	1488,	2872,	4336,	5757,	7099,	8337,	10086,	11203,	1505,	2915,	4429,	5894,	7258,	8531,	10330,	11252,	1518,	2939,	4325,	5783,	7339,	8688,	10163,	11775,	12726,	1458,	2798,	4097,	5513,	7057,	8444,	10033,	11662,	12511,	1367,	2580,	3765,	5046,	6462,	7848,	9318,	10869,	12123,	13506,	1117,	2109,	3104,	4149,	5278,	6431,	7715,	8999,	10455,	12045,	13640,	14835,	888,	1696,	2460,	3276,	4096,	4939,	5778,	6653,	7502,	8522,	9590,	11380,	12947,	14457,	15729,	17092,	639,	1185,	1694,	2229,	2746,	3231,	3681,	4111,	4511,	4889,	5241,	5527,	5832,	6058,	6264,	6430,	6547,	448,	810,	1128,	1431,	1709,	1953,	2172,	2346,	2499,	2637,	2746,	2831,	2902,	2989,	3042,	3086,	3124,	317,	565,	776,	969,	1150,	1308,	1444,	1550,	1643,	1726,	1791,	1846,	1882,	1935,	1954,	1984,	2013,	181,	322,	440,	536,	624,	695,	747,	788,	817,	845,	863,	882,	888,	876,	896,	905,	79,	105,	115,	114,	80,	42,	-8,	-59,	-119,	-173,	-237,	-286,	-353,	-429,	-471,	-537,	-84,	-197,	-315,	-453,	-607,	-748,	-897,	-1047,	-1195,	-1331,	-1528,	-1650,	-1740,	-1830,	-1944,	-233,	-475,	-705,	-964,	-1230,	-1480,	-1714,	-1974,	-2211,	-2419,	-2669,	-2849,	-2988,	-357,	-742,	-1094,	-1507,	-1909,	-2320,	-2692,	-3088,	-3432,	-3722,	-4067,	-4452,	-456,	-967,	-1456,	-1974,	-2520,	-3074,	-3570,	-4156,	-4599,	-5040,	-5532,	-540,	-1160,	-1792,	-2432,	-3183,	-3943,	-4585,	-5231,	-5967,	-6617,	-583,	-1276,	-1974,	-2734,	-3549,	-4381,	-5142,	-5923,	-6814,	-7550,	-614,	-1361,	-2116,	-2922,	-3789,	-4708,	-5495,	-6409,	-7281,	-8202,	-625,	-1426,	-2213,	-3050,	-3972,	-4892,	-5873,	-6827,	-7720,	-642,	-1435,	-2257,	-3106,	-4031,	-5005,	-5938,	-6891,	-7794,	-627,	-1420,	-2252,	-3111,	-4020,	-4949,	-5770,	-6698,	-7530,	-8513,	-606,	-1379,	-2143,	-2936,	-3774,	-4651,	-5415,	-6141,	-6868,	-7763,	-8628,	-572,	-1269,	-1950,	-2653,	-3379,	-4115,	-4824,	-5500,	-6003,	-6815,	-7581,	-8283,	-505,	-1126,	-1737,	-2343,	-2993,	-3617,	-4198,	-4811,	-5426,	-6085,	-6797,	-7443,	-8209,	-8884,	-358,	-838,	-1321,	-1817,	-2324,	-2786,	-3297,	-3794,	-4299,	-4844,	-5399,	-5951,	-6541,	-7112,	-7725,	-268,	-620,	-993,	-1384,	-1767,	-2122,	-2520,	-2944,	-3347,	-3760,	-4219,	-4660,	-5125,	-5549,	-5996,	-160,	-364,	-616,	-861,	-1090,	-1353,	-1611,	-1912,	-2216,	-2529,	-2825,	-3031,	-3322,	-3643,	-3880,	-28,	-71,	-134,	-198,	-269,	-373,	-478,	-609,	-733,	-875,	-985,	-1108,	-1328,	-1448,	70,	150,	222,	273,	315,	342,	348,	344,	290,	278,	248,	199,	108,	173,	368,	579,	790,	970,	1170,	1345,	1509,	1682,	1832,	1912,	1965,	273,	580,	915,	1246,	1558,	1915,	2208,	2502,	2749,	2954,	3067,	3319,	362,	807,	1253,	1708,	2152,	2632,	3089,	3539,	3902,	4219,	4496,	4785,	490,	1069,	1654,	2304,	2897,	3583,	4268,	4998,	5491,	6027,	6596,	7082,	7647,	621,	1385,	2149,	2957,	3722,	4667,	5621,	6670,	7448,	8607,	10475,	12515,	14615,	795,	1679,	2608,	3514,	4418,	5550,	6773,	7940,	9143,	10473,	12353,	13705,	982,	1991,	3053,	4066,	5173,	6381,	7636,	8893,	10148,	11638,	13364,	1141,	2259,	3423,	4561,	5756,	7058,	8368,	9684,	11097,	12339,	13673,	1310,	2564,	3815,	5041,	6295,	7650,	8995,	10350,	11935,	12917,	1446,	2759,	4081,	5394,	6771,	8125,	9492,	10984,	12104,	13116,	1512,	2918,	4310,	5667,	7141,	8492,	9788,	11362,	12434,	13403,	1527,	2958,	4365,	5786,	7296,	8640,	10018,	11613,	12637,	1619,	2987,	4406,	5814,	7247,	8725,	10119,	11544,	12979,	14304,	1576,	2862,	4220,	5552,	7028,	8491,	10037,	11438,	12901,	14154,	15354,	1468,	2694,	3944,	5164,	6502,	7946,	9454,	10737,	12429,	13651,	15160,	1206,	2229,	3271,	4313,	5400,	6578,	7829,	9120,	10548,	11878,	14473,	16185,	18356,	20101,	962,	1788,	2624,	3373,	4226,	5086,	5914,	6764,	7677,	8624,	9592,	10642,	11330,	12629,	14288,	16208,	692,	1255,	1850,	2358,	2889,	3415,	3862,	4369,	4838,	5266,	5642,	5979,	6193,	6503,	6737,	6902,	488,	859,	1230,	1544,	1822,	2079,	2309,	2507,	2684,	2844,	2969,	3072,	3153,	3275,	3365,	3347,	344,	609,	860,	1052,	1248,	1424,	1572,	1714,	1825,	1938,	1981,	2068,	2138,	2183,	2246,	206,	358,	491,	598,	694,	772,	842,	898,	952,	982,	990,	1001,	1011,	997,	103,	146,	180,	187,	178,	153,	115,	74,	15,	-27,	-90,	-149,	-232,	-53,	-135,	-239,	-365,	-477,	-601,	-724,	-850,	-1021,	-1130,	-1253,	-1380,	-1544,	-224,	-427,	-699,	-929,	-1154,	-1400,	-1604,	-1817,	-2109,	-2357,	-2593,	-2837,	-332,	-696,	-1064,	-1427,	-1785,	-2170,	-2493,	-2897,	-3293,	-3573,	-3923,	-419,	-926,	-1425,	-1943,	-2485,	-3027,	-3554,	-4171,	-4783,	-5360,	-494,	-1150,	-1812,	-2545,	-3276,	-4169,	-4815,	-5687,	-6535,	-537,	-1268,	-2026,	-2866,	-3766,	-4725,	-5539,	-6459,	-568,	-1381,	-2190,	-3117,	-4026,	-5045,	-6082,	-586,	-1436,	-2309,	-3240,	-4217,	-5213,	-6296,	-588,	-1444,	-2383,	-3307,	-4359,	-5201,	-6366,	-563,	-1415,	-2346,	-3363,	-4397,	-5158,	-6149,	-524,	-1331,	-2219,	-3180,	-4140,	-4900,	-5745,	-6420,	-477,	-1252,	-2014,	-2831,	-3649,	-4306,	-5036,	-5593,	-6236,	-429,	-1117,	-1776,	-2491,	-3174,	-3831,	-4467,	-4978,	-5517,	-6212,	-6897,	-291,	-805,	-1264,	-1848,	-2350,	-2866,	-3436,	-3889,	-4364,	-4865,	-5394,	-6014,	-6679,	-209,	-578,	-961,	-1375,	-1774,	-2171,	-2625,	-3010,	-3339,	-3777,	-4274,	-4765,	-5287,	-122,	-329,	-547,	-800,	-1032,	-1317,	-1595,	-1878,	-2109,	-2405,	-2716,	-3075,	-3437,	-17,	-49,	-96,	-163,	-235,	-333,	-417,	-535,	-658,	-827,	-1004,	-1163,	65,	165,	249,	307,	367,	405,	430,	420,	371,	341,	309,	145,	388,	612,	842,	1044,	1298,	1509,	1635,	1807,	1929,	227,	602,	938,	1314,	1680,	2113,	2430,	2659,	2961,	3242,	310,	835,	1299,	1801,	2287,	2837,	3277,	3656,	4048,	426,	1101,	1733,	2421,	3069,	3855,	4347,	4923,	5521,	6069,	547,	1420,	2221,	3068,	3863,	4838,	5700,	6476,	7430,	8235,	9370,	10923,	13884,	723,	1751,	2649,	3643,	4594,	5677,	6787,	7768,	8766,	9872,	11164,	13133,	15196,	955,	2045,	3070,	4238,	5269,	6447,	7674,	8958,	9928,	10969,	12475,	14585,	16153,	1144,	2297,	3430,	4664,	5823,	7070,	8339,	9692,	11022,	12360,	13884,	15707,	1335,	2572,	3813,	5100,	6317,	7615,	8945,	10257,	11628,	13143,	14525,	1488,	2776,	4114,	5410,	6721,	8103,	9470,	10820,	12162,	13745,	15114,	1572,	2941,	4332,	5718,	7107,	8553,	9934,	11291,	12626,	14033,	15063,	1615,	2995,	4404,	5835,	7239,	8716,	10098,	11461,	12852,	14029,	1713,	3139,	4551,	6014,	7362,	8822,	10292,	11643,	13157,	14419,	16663,	1670,	3070,	4428,	5841,	7173,	8681,	10133,	11487,	13241,	14458,	16623,	1588,	2936,	4224,	5536,	6821,	8213,	9599,	11092,	12705,	14503,	16747,	1326,	2416,	3565,	4641,	5779,	6905,	8050,	9419,	10824,	12351,	15548,	18163,	21770,	25123,	1047,	1923,	2873,	3650,	4549,	5333,	6195,	7086,	7979,	8790,	9555,	10366,	11466,	12624,	756,	1370,	2043,	2588,	3140,	3653,	4188,	4713,	5312,	5842,	6261,	6667,	6944,	7305,	543,	946,	1354,	1684,	1972,	2260,	2488,	2727,	2898,	3024,	3131,	3207,	3262,	3300,	378,	673,	957,	1193,	1384,	1576,	1713,	1884,	2019,	2083,	2185,	2243,	240,	409,	559,	695,	779,	868,	890,	921,	965,	951,	980,	953,	121,	176,	218,	242,	246,	234,	218,	201,	158,	112,	69,	-27,	-92,	-179,	-288,	-398,	-544,	-703,	-858,	-1040,	-1192,	-212,	-427,	-665,	-916,	-1122,	-1368,	-1587,	-1814,	-2032,	-320,	-709,	-1085,	-1412,	-1810,	-2130,	-2454,	-2801,	-3142,	-438,	-978,	-1483,	-1977,	-2440,	-2915,	-3375,	-3889,	-505,	-1233,	-1943,	-2546,	-3154,	-3905,	-4503,	-5185,	-531,	-1377,	-2119,	-2862,	-3548,	-4205,	-551,	-1479,	-2348,	-3116,	-3990,	-557,	-1535,	-2498,	-3364,	-4188,	-554,	-1535,	-2572,	-3463,	-4441,	-534,	-1535,	-2520,	-3625,	-4604,	-490,	-1458,	-2414,	-3477,	-4521,	-481,	-1317,	-2180,	-3105,	-4077,	-421,	-1235,	-1934,	-2748,	-3645,	-4346,	-5055,	-280,	-832,	-1369,	-2068,	-2729,	-3397,	-4055,	-4683,	-198,	-593,	-987,	-1496,	-1973,	-2386,	-2853,	-3327,	-113,	-333,	-564,	-862,	-1072,	-1368,	-1660,	-1894,	-11,	-36,	-84,	-137,	-220,	-325,	-449,	-498,	73,	190,	296,	327,	462,	508,	557,	148,	424,	677,	919,	1107,	1344,	1533,	220,	643,	1050,	1364,	1717,	2112,	2404,	324,	899,	1441,	1869,	2334,	2856,	441,	1202,	1918,	2546,	3130,	3728,	4301,	558,	1519,	2372,	3168,	3950,	4732,	5539,	6324,	760,	1853,	2856,	3839,	4702,	5578,	6556,	7513,	8499,	999,	2204,	3279,	4339,	5389,	6403,	7436,	8570,	9575,	10926,	12081,	13730,	16013,	1226,	2485,	3641,	4839,	6005,	7079,	8166,	9319,	10611,	12074,	13621,	16429,	19040,	1428,	2762,	4012,	5283,	6573,	7674,	8843,	10014,	11197,	12688,	14908,	17662,	1567,	2953,	4267,	5587,	6865,	8187,	9462,	10852,	12031,	13416,	16162,	18383,	1654,	3089,	4486,	5870,	7199,	8622,	10045,	11429,	12727,	14007,	16333,	18839,	1708,	3138,	4555,	5970,	7333,	8790,	10246,	11568,	13005,	14389,	16314,	1792,	3298,	4617,	6112,	7430,	8832,	10431,	11733,	13341,	14815,	17485,	1826,	3222,	4621,	6032,	7302,	8805,	10269,	11560,	13516,	14721,	17242,	1750,	3119,	4451,	5919,	7074,	8490,	9686,	11050,	12870,	14381,	16333,	1489,	2586,	3833,	4999,	6188,	7111,	8131,	9324,	10649,	11871,	12912,	1158,	2054,	3086,	3856,	4878,	5535,	6372,	7209,	8142,	8907,	9556,	903,	1532,	2272,	2815,	3432,	3897,	4500,	5090,	5752,	6374,	6854,	629,	1067,	1451,	1802,	2106,	2399,	2646,	2902,	3186,	427,	728,	1033,	1324,	1521,	1743,	1940,	2099,	317,	476,	612,	742,	849,	936,	1037,	132,	188,	224,	241,	322,	268,	249,	-37,	-49,	-117,	-198,	-335,	-434,	-592,	-229,	-493,	-737,	-989,	-1210,	-1441,	-345,	-813,	-1202,	-1511,	-1822,	-2183,	-423,	-1008,	-1509,	-1977,	-2482,	-445,	-1279,	-1845,	-2457,	-506,	-1424,	-2200,	-528,	-1537,	-2463,	-572,	-1602,	-2578,	-555,	-1619,	-2672,	-503,	-1635,	-2630,	-485,	-1551,	-2515,	-450,	-1381,	-2275,	-415,	-1236,	-1946,	-2735,	-322,	-871,	-1461,	-2078,	-229,	-642,	-1113,	-1501,	-1869,	-114,	-371,	-620,	-874,	-1086,	19,	36,	-6,	-107,	-166,	94,	211,	311,	418,	209,	541,	867,	1085,	323,	716,	1125,	1511,	438,	930,	1522,	1978,	571,	1318,	2039,	2630,	781,	1686,	2552,	3305,	933,	1975,	3000,	3945,	1123,	2317,	3461,	4550,	5578,	1331,	2619,	3837,	5026,	6238,	7198,	1537,	2988,	4163,	5371,	6731,	7753,	8831,	10149,	1666,	3144,	4438,	5659,	6967,	8211,	9576,	10805,	12007,	13695,	15411,	1684,	3225,	4599,	5928,	7269,	8668,	10099,	11553,	12950,	14287,	16729,	1775,	3269,	4663,	6058,	7407,	8732,	10318,	11744,	13289,	14619,	17242,

};

const float Munsell_H_D[5000] =
{
2.5,	5,	7.5,	7.5,	10,	10,	2.5,	2.5,	5,	5,	7.5,	7.5,	10,	10,	2.5,	2.5,	5,	5,	7.5,	7.5,	10,	10,	2.5,	2.5,	5,	5,	7.5,	7.5,	10,	10,	10,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	2.5,	2.5,	2.5,	5,	5,	7.5,	7.5,	10,	10,	2.5,	2.5,	5,	5,	7.5,	10,	2.5,	5,	7.5,	10,	2.5,	2.5,	5,	5,	7.5,	7.5,	10,	10,	2.5,	2.5,	5,	5,	7.5,	7.5,	10,	10,	2.5,	2.5,	5,	5,	7.5,	7.5,	7.5,	10,	10,	10,	2.5,	2.5,	2.5,	5,	5,	5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	7.5,	7.5,	10,	2.5,	5,	7.5,	10,	10,	2.5,	2.5,	5,	5,	7.5,	7.5,	7.5,	10,	10,	10,	2.5,	2.5,	2.5,	5,	5,	5,	7.5,	7.5,	7.5,	10,	10,	10,	2.5,	2.5,	2.5,	5,	5,	5,	7.5,	7.5,	7.5,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	7.5,	7.5,	10,	10,	2.5,	2.5,	5,	7.5,	7.5,	10,	10,	2.5,	2.5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	10,	10,	2.5,	2.5,	5,	5,	7.5,	7.5,	10,	10,	2.5,	2.5,	2.5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	10,	10,	2.5,	2.5,	5,	5,	7.5,	7.5,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	10,	10,	10,	2.5,	2.5,	2.5,	5,	5,	5,	7.5,	7.5,	7.5,	10,	10,	10,	2.5,	2.5,	2.5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	10,	10,	10,	10,	10,	10,	10,	10,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	2.5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,	7.5,
}
;

const char Munsell_H_Z[5000][3] =
{
"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"Y",	"Y",	"Y",	"Y",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"Y",	"Y",	"Y",	"Y",	"Y",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"GY",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"G",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"BG",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"B",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"PB",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"P",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"RP",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"R",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"YR",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",	"Y",
};

const float MNDC[] =
{
0.15,	0.27,	5.42,	5.72,	0.43,	0.48,	0.26,	0.28,	0.55,	0.59,	8.89,	9.85,	0.67,	0.6,	0.22,	0.24,	0,	0.02,	10.77,	12.24,	9.46,	10.36,	0.25,	0.37,	0.13,	0.16,	9.98,	11.02,	1.17,	2.13,	0.73,	0.82,	1.19,	1.72,	0.26,	0.27,	0.24,	0.24,	0.13,	0.06,	0.24,	0.28,	0.04,	0.06,	0.04,	0.13,	8.92,	11.49,	11.09,	12.47,	10.66,	12.03,	5.57,	6.54,	4.37,	8.21,	0.24,	0.29,	9.37,	11.17,	5.43,	6.37,	4.33,	8.21,	10.66,	12.02,	5.18,	6.04,	5.42,	6.35,	7.65,	8.26,	11.55,	13.02,	0.13,	0.69,	0.05,	0.16,	0.04,	0.05,	0.55,	0.63,	0.04,	0.07,	0.14,	0.1,	0.23,	0.27,	0.04,	0.04,	0.04,	0.14,	8.95,	11.53,	11.11,	12.5,	10.69,	12.05,	5.58,	6.56,	4.4,	8.24,	15.57,	17.45,	19.22,	14.57,	17.33,	0.14,	0.1,	0.24,	0.27,	0.03,	0.03,	0.04,	0.14,	8.95,	11.57,	11.1,	12.5,	10.68,	12.07,	5.58,	6.59,	4.41,	8.39,	0.11,	0.13,	0.34,	0.37,	0.34,	0.36,	0.1,	0.12,	0.11,	0.17,	8.79,	11.21,	11.06,	12.38,	10.64,	11.94,	5.57,	6.49,	3.97,	7.2,	4.2,	7.49,

};

const float MNDV[] =
{
2.72,	1.85,	8.82,	8.63,	6.7,	6.34,	8.36,	8.21,	5.99,	5.71,	5.09,	4.54,	7.3,	7.07,	7.86,	7.64,	9.24,	9.12,	8.14,	7.88,	3.64,	3.18,	2.72,	1.32,	8.55,	8.37,	5.71,	5.43,	2.95,	1.64,	6.28,	5.95,	3.16,	2.38,	9.47,	9.27,	9.42,	9.23,	9.77,	9.54,	9.48,	9.26,	5.82,	5.41,	3.51,	2.64,	4.41,	3.56,	6.48,	6.14,	8.3,	8.05,	5.38,	4.92,	2.74,	1.11,	9.47,	9.26,	4.37,	3.74,	5.37,	4.92,	2.79,	1.15,	8.29,	8.04,	4.5,	3.86,	5.43,	4.99,	5.29,	4.83,	6.52,	6.18,	2.58,	0.63,	3.51,	2.65,	5.88,	5.49,	5.77,	5.36,	8.16,	7.9,	9.8,	9.58,	9.51,	9.3,	5.85,	5.45,	3.52,	2.65,	4.42,	3.58,	6.5,	6.17,	8.32,	8.09,	5.4,	4.95,	2.74,	1.12,	10.06,	9.88,	7.77,	5.7,	5.32,	9.8,	9.58,	9.51,	9.31,	5.84,	5.44,	3.52,	2.64,	4.41,	3.56,	6.5,	6.17,	8.32,	8.09,	5.4,	4.94,	2.74,	1.08,	9.77,	9.54,	9.47,	9.26,	9.47,	9.26,	5.82,	5.41,	3.53,	2.7,	4.43,	3.61,	6.48,	6.15,	8.29,	8.05,	5.38,	4.93,	2.79,	1.28,	2.79,	1.28,

};

const char MND_H[][7] =
{
"4.5PB",	"2.0PB",	"4.9GY",	"4.8GY",	"6.3GY",	"5.8GY",	"8.5GY",	"7.5GY",	"5.3YR",	"5.7YR",	"9.3B",	"8.4B",	"2.3PB",	"1.7PB",	"4.0RP",	"4.7RP",	"6.4GY",	"10.0Y",	"4.2Y",	"4.4Y",	"8.7PB",	"8.4PB",	"4.0PB",	"1.7PB",	"1.3Y",	"0.8Y",	"2.4G",	"2.2G",	"8.4RP",	"1.1R",	"1.7Y",	"2.0Y",	"2.9PB",	"2.1PB",	"0.2YR",	"2.2YR",	"1.2YR",	"2.7YR",	"4.2PB",	"1.0PB",	"8.5Y",	"7.5Y",	"1.4YR",	"7.9YR",	"9.9YR",	"3.1Y",	"5.5R",	"7.7R",	"2.4YR",	"2.9YR",	"5.4Y",	"5.6Y",	"3.7G",	"3.4G",	"9.2PB",	"8.3PB",	"8.4Y",	"7.2Y",	"4.8R",	"6.1R",	"3.4G",	"3.0G",	"9.2PB",	"8.2PB",	"5.3Y",	"5.6Y",	"9.7RP",	"0.4R",	"2.1G",	"1.9G",	"0.6PB",	"0.1PB",	"2.3YR",	"2.7YR",	"2.5PB",	"0.1PB",	"9.1YR",	"3.7Y",	"7.5RP",	"6.6YR",	"5.2G",	"3.8G",	"7.5GY",	"2.4GY",	"4.8PB",	"3.6PB",	"8.4Y",	"7.7Y",	"0.9YR",	"8.2YR",	"1.4Y",	"5.0Y",	"5.5R",	"7.7R",	"2.4YR",	"2.9YR",	"5.4Y",	"5.6Y",	"3.8G",	"3.5G",	"9.2PB",	"8.3PB",	"1.2GY",	"1.2GY",	"4.2YR",	"1.6G",	"1.3G",	"5.1PB",	"4.1PB",	"8.1Y",	"7.5Y",	"8.4R",	"8.9YR",	"10.0YR",	"4.0Y",	"5.5R",	"7.7R",	"2.4YR",	"2.9YR",	"5.4Y",	"5.7Y",	"3.8G",	"3.5G",	"9.2PB",	"8.2PB",	"8.1GY",	"7.0GY",	"2.0GY",	"0.8GY",	"2.0GY",	"0.8GY",	"0.3GY",	"9.1Y",	"0.2GY",	"7.2Y",	"5.7R",	"7.5R",	"2.4YR",	"2.9YR",	"5.4Y",	"5.6Y",	"3.6G",	"3.3G",	"9.1PB",	"8.3PB",	"9.2PB",	"8.4PB"

};

const float MND_H_D[] =
{
4.6,	2.4,	5,	4.9,	6.3,	5.8,	8.5,	7.5,	5.3,	5.7,	9.2,	8.3,	1.9,	1.4,	9,	10,	0,	2.7,	4.2,	4.4,	8.7,	8.4,	4.1,	2.5,	10,	9.6,	2.4,	2.2,	8.3,	1,	1.9,	2.1,	2.9,	2.2,	4,	8.9,	4.6,	9.4,	3.8,	0.5,	7.4,	6.6,	1.4,	8.1,	9.4,	3.1,	5.4,	7.6,	2.5,	3,	5.4,	5.6,	3.7,	3.4,	9.3,	8.3,	7.2,	6.4,	4.7,	6,	3.4,	3,	9.3,	8.3,	5.3,	5.5,	9.7,	0.4,	2.1,	1.9,	0.5,	10,	2.4,	2.8,	2.9,	3,	8.7,	3.6,	7.7,	6.9,	5.1,	3.8,	7.5,	2.6,	4.5,	3,	7.3,	6.7,	0.7,	8.4,	0.9,	4.7,	5.4,	7.6,	2.5,	3,	5.3,	5.6,	3.7,	3.4,	9.3,	8.3,	1.3,	1.3,	3.7,	1.6,	1.2,	4.8,	3.6,	6.9,	6.5,	8.6,	9.2,	9.5,	3.9,	5.4,	7.7,	2.5,	3,	5.4,	5.6,	3.7,	3.4,	9.3,	8.3,	9,	7.5,	1.9,	0.5,	1.9,	0.4,	9.2,	7.9,	9.4,	6,	5.5,	7.4,	2.5,	3,	5.4,	5.6,	3.6,	3.4,	9.1,	8.4,	9.3,	8.5,

};
const int Munsell_C[5000] = //*0
{
2,	2,	2,	4,	2,	4,	2,	4,	2,	4,	2,	4,	2,	4,	2,	4,	2,	4,	2,	4,	2,	4,	2,	4,	2,	4,	2,	4,	2,	4,	6,	2,	4,	6,	2,	4,	6,	8,	10,	12,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	2,	4,	6,	8,	10,	12,	14,	16,	18,	2,	4,	6,	8,	10,	12,	14,	2,	4,	6,	8,	10,	12,	2,	4,	6,	8,	10,	2,	4,	6,	8,	10,	2,	4,	6,	8,	2,	4,	6,	8,	2,	4,	6,	2,	4,	6,	2,	4,	2,	4,	2,	4,	2,	4,	2,	4,	2,	2,	2,	2,	2,	2,	2,	4,	2,	4,	2,	4,	2,	4,	2,	4,	2,	4,	2,	4,	2,	4,	2,	4,	2,	4,	2,	4,	6,	2,	4,	6,	2,	4,	6,	2,	4,	6,	2,	4,	6,	2,	4,	6,	8,	2,	4,	6,	8,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	2,	4,	6,	8,	10,	12,	14,	16,	18,	2,	4,	6,	8,	10,	12,	14,	16,	2,	4,	6,	8,	10,	12,	14,	2,	4,	6,	8,	10,	12,	2,	4,	6,	8,	10,	2,	4,	6,	8,	10,	2,	4,	6,	8,	10,	2,	4,	6,	8,	2,	4,	6,	8,	2,	4,	6,	8,	2,	4,	6,	8,	2,	4,	6,	2,	4,	2,	2,	2,	2,	2,	4,	2,	4,	2,	4,	2,	4,	6,	2,	4,	6,	2,	4,	6,	2,	4,	6,	2,	4,	6,	2,	4,	6,	2,	4,	6,	2,	4,	6,	2,	4,	6,	2,	4,	6,	2,	4,	6,	8,	2,	4,	6,	8,	2,	4,	6,	8,	2,	4,	6,	8,	2,	4,	6,	8,	10,	12,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	30,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	30,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	2,	4,	6,	8,	10,	12,	14,	16,	18,	2,	4,	6,	8,	10,	12,	14,	16,	2,	4,	6,	8,	10,	12,	14,	16,	2,	4,	6,	8,	10,	12,	14,	2,	4,	6,	8,	10,	12,	2,	4,	6,	8,	10,	12,	2,	4,	6,	8,	10,	2,	4,	6,	8,	10,	2,	4,	6,	8,	10,	2,	4,	6,	8,	10,	2,	4,	6,	8,	2,	4,	2,	4,	2,	4,	2,	2,	4,	2,	4,	2,	4,	2,	4,	6,	2,	4,	6,	8,	2,	4,	6,	8,	2,	4,	6,	8,	2,	4,	6,	8,	2,	4,	6,	8,	2,	4,	6,	8,	2,	4,	6,	8,	2,	4,	6,	8,	2,	4,	6,	8,	2,	4,	6,	8,	2,	4,	6,	8,	2,	4,	6,	8,	2,	4,	6,	8,	10,	2,	4,	6,	8,	10,	2,	4,	6,	8,	10,	12,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	30,	32,	34,	36,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	30,	32,	34,	36,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	30,	32,	34,	36,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	30,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	2,	4,	6,	8,	10,	12,	14,	16,	2,	4,	6,	8,	10,	12,	14,	2,	4,	6,	8,	10,	12,	14,	2,	4,	6,	8,	10,	12,	2,	4,	6,	8,	10,	12,	2,	4,	6,	8,	10,	12,	2,	4,	6,	8,	10,	12,	2,	4,	6,	8,	2,	4,	6,	2,	4,	2,	4,	2,	4,	2,	4,	2,	4,	2,	4,	6,	2,	4,	6,	2,	4,	6,	8,	2,	4,	6,	8,	10,	2,	4,	6,	8,	10,	2,	4,	6,	8,	10,	2,	4,	6,	8,	10,	2,	4,	6,	8,	10,	2,	4,	6,	8,	10,	2,	4,	6,	8,	10,	2,	4,	6,	8,	2,	4,	6,	8,	2,	4,	6,	8,	2,	4,	6,	8,	10,	2,	4,	6,	8,	10,	2,	4,	6,	8,	10,	2,	4,	6,	8,	10,	12,	14,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	30,	32,	34,	36,	38,	40,	42,	44,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	30,	32,	34,	36,	38,	40,	42,	44,	46,	48,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	30,	32,	34,	36,	38,	40,	42,	44,	46,	48,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	30,	32,	34,	36,	38,	40,	42,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	30,	32,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	2,	4,	6,	8,	10,	12,	14,	16,	2,	4,	6,	8,	10,	12,	14,	16,	2,	4,	6,	8,	10,	12,	14,	2,	4,	6,	8,	10,	12,	2,	4,	6,	8,	10,	12,	2,	4,	6,	8,	10,	12,	2,	4,	6,	8,	10,	12,	2,	4,	6,	8,	10,	2,	4,	6,	2,	4,	2,	4,	2,	4,	2,	4,	2,	4,	6,	2,	4,	6,	8,	2,	4,	6,	8,	10,	2,	4,	6,	8,	10,	12,	14,	2,	4,	6,	8,	10,	12,	14,	16,	18,	2,	4,	6,	8,	10,	12,	14,	16,	18,	2,	4,	6,	8,	10,	12,	14,	16,	18,	2,	4,	6,	8,	10,	12,	14,	16,	18,	2,	4,	6,	8,	10,	12,	14,	16,	18,	2,	4,	6,	8,	10,	12,	14,	16,	18,	2,	4,	6,	8,	10,	12,	14,	16,	2,	4,	6,	8,	10,	12,	14,	16,	2,	4,	6,	8,	10,	12,	14,	2,	4,	6,	8,	10,	12,	2,	4,	6,	8,	10,	12,	2,	4,	6,	8,	10,	12,	14,	2,	4,	6,	8,	10,	12,	14,	2,	4,	6,	8,	10,	12,	14,	16,	18,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	30,	32,	34,	36,	38,	40,	42,	44,	46,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	30,	32,	34,	36,	38,	40,	42,	44,	46,	48,	50,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	30,	32,	34,	36,	38,	40,	42,	44,	46,	48,	50,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	30,	32,	34,	36,	38,	40,	42,	44,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	30,	32,	34,	36,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	30,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	2,	4,	6,	8,	10,	12,	14,	16,	18,	2,	4,	6,	8,	10,	12,	14,	16,	2,	4,	6,	8,	10,	12,	14,	16,	2,	4,	6,	8,	10,	12,	14,	16,	2,	4,	6,	8,	10,	12,	14,	16,	2,	4,	6,	8,	10,	12,	14,	16,	2,	4,	6,	8,	10,	2,	4,	6,	8,	2,	4,	6,	8,	2,	4,	6,	8,	2,	4,	6,	2,	4,	6,	2,	4,	6,	8,	2,	4,	6,	8,	10,	2,	4,	6,	8,	10,	12,	2,	4,	6,	8,	10,	12,	14,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	2,	4,	6,	8,	10,	12,	14,	16,	18,	2,	4,	6,	8,	10,	12,	14,	16,	2,	4,	6,	8,	10,	12,	14,	16,	2,	4,	6,	8,	10,	12,	14,	16,	2,	4,	6,	8,	10,	12,	14,	16,	18,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	30,	32,	34,	36,	38,	40,	42,	44,	46,	48,	50,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	30,	32,	34,	36,	38,	40,	42,	44,	46,	48,	50,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	30,	32,	34,	36,	38,	40,	42,	44,	46,	48,	50,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	30,	32,	34,	36,	38,	40,	42,	44,	46,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	30,	32,	34,	36,	38,	40,	42,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	30,	32,	34,	36,	38,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	30,	32,	34,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	30,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	2,	4,	6,	8,	10,	12,	14,	16,	2,	4,	6,	8,	10,	12,	2,	4,	6,	8,	10,	2,	4,	6,	8,	10,	2,	4,	6,	8,	10,	2,	4,	6,	8,	2,	4,	6,	8,	2,	4,	6,	8,	10,	12,	2,	4,	6,	8,	10,	12,	2,	4,	6,	8,	10,	12,	14,	2,	4,	6,	8,	10,	12,	14,	16,	18,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	30,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	30,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	30,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	30,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	2,	4,	6,	8,	10,	12,	14,	16,	18,	2,	4,	6,	8,	10,	12,	14,	16,	18,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	30,	32,	34,	36,	38,	40,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	30,	32,	34,	36,	38,	40,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	30,	32,	34,	36,	38,	40,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	30,	32,	34,	36,	38,	40,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	30,	32,	34,	36,	38,	40,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	30,	32,	34,	36,	38,	40,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	30,	32,	34,	36,	38,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	30,	32,	34,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	30,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	2,	4,	6,	8,	10,	12,	14,	16,	18,	2,	4,	6,	8,	10,	12,	14,	2,	4,	6,	8,	10,	12,	14,	2,	4,	6,	8,	10,	12,	2,	4,	6,	8,	10,	12,	2,	4,	6,	8,	10,	12,	2,	4,	6,	8,	10,	12,	2,	4,	6,	8,	10,	12,	14,	2,	4,	6,	8,	10,	12,	14,	2,	4,	6,	8,	10,	12,	14,	16,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	30,	32,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	30,	32,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	30,	32,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	30,	32,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	30,	32,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	30,	32,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	30,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	30,	32,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	30,	32,	34,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	30,	32,	34,	36,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	30,	32,	34,	36,	38,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	30,	32,	34,	36,	38,	40,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	30,	32,	34,	36,	38,	40,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	30,	32,	34,	36,	38,	40,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	30,	32,	34,	36,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	30,	32,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	30,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	2,	4,	6,	8,	10,	12,	14,	16,	18,	2,	4,	6,	8,	10,	12,	14,	16,	2,	4,	6,	8,	10,	12,	14,	16,	2,	4,	6,	8,	10,	12,	14,	2,	4,	6,	8,	10,	12,	14,	2,	4,	6,	8,	10,	12,	14,	2,	4,	6,	8,	10,	12,	14,	16,	2,	4,	6,	8,	10,	12,	14,	16,	2,	4,	6,	8,	10,	12,	14,	16,	18,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	30,	32,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	30,	32,	34,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	30,	32,	34,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	30,	32,	34,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	30,	32,	34,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	30,	32,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	30,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	30,	32,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	30,	32,	34,	36,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	30,	32,	34,	36,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	30,	32,	34,	36,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	30,	32,	34,	36,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	30,	32,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	30,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	30,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	30,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	30,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	2,	4,	6,	8,	10,	12,	14,	16,	18,	2,	4,	6,	8,	10,	12,	14,	16,	18,	2,	4,	6,	8,	10,	12,	14,	16,	2,	4,	6,	8,	10,	12,	14,	16,	2,	4,	6,	8,	10,	12,	14,	16,	18,	2,	4,	6,	8,	10,	12,	14,	16,	18,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	30,	32,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	30,	32,	34,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	30,	32,	34,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	30,	32,	34,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	30,	32,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	30,	32,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	30,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	2,	4,	6,	8,	10,	12,	14,	16,	18,	2,	4,	6,	8,	10,	12,	14,	16,	18,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	30,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	30,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	30,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	2,	4,	6,	8,	10,	12,	14,	16,	18,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	30,	32,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	30,	32,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	30,	32,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	30,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	2,	4,	6,	8,	10,	12,	14,	16,	18,	2,	4,	6,	8,	10,	12,	14,	16,	2,	4,	6,	8,	10,	12,	14,	2,	4,	6,	8,	10,	12,	14,	2,	4,	6,	8,	10,	12,	14,	2,	4,	6,	8,	10,	12,	14,	2,	4,	6,	8,	10,	12,	14,	16,	2,	4,	6,	8,	10,	12,	14,	16,	18,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	2,	4,	6,	8,	10,	12,	14,	16,	18,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	28,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	2,	4,	6,	8,	10,	12,	14,	16,	18,	2,	4,	6,	8,	10,	12,	14,	16,	18,	2,	4,	6,	8,	10,	12,	14,	16,	2,	4,	6,	8,	10,	12,	14,	16,	2,	4,	6,	8,	10,	12,	2,	4,	6,	8,	10,	2,	4,	6,	8,	10,	2,	4,	6,	8,	10,	2,	4,	6,	8,	10,	2,	4,	6,	8,	10,	2,	4,	6,	8,	10,	2,	4,	6,	8,	10,	12,	14,	2,	4,	6,	8,	10,	12,	14,	16,	2,	4,	6,	8,	10,	12,	14,	16,	2,	4,	6,	8,	10,	12,	14,	16,	2,	4,	6,	8,	10,	12,	14,	16,	2,	4,	6,	8,	10,	12,	14,	2,	4,	6,	8,	10,	12,	14,	2,	4,	6,	8,	10,	12,	14,	2,	4,	6,	8,	10,	12,	2,	4,	6,	8,	10,	12,	14,	2,	4,	6,	8,	10,	12,	14,	16,	2,	4,	6,	8,	10,	12,	14,	16,	18,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	26,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	24,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	2,	4,	6,	8,	10,	12,	14,	16,	18,	2,	4,	6,	8,	10,	12,	14,	16,	2,	4,	6,	8,	10,	12,	14,	2,	4,	6,	8,	10,	12,	14,	2,	4,	6,	8,	10,	12,	14,	2,	4,	6,	8,	10,	12,	2,	4,	6,	8,	10,	12,	2,	4,	6,	8,	10,	2,	4,	6,	8,	2,	4,	6,	2,	4,	6,	2,	4,	6,	2,	4,	6,	2,	4,	6,	2,	4,	6,	2,	4,	6,	2,	4,	6,	8,	2,	4,	6,	8,	2,	4,	6,	8,	10,	2,	4,	6,	8,	10,	2,	4,	6,	8,	10,	2,	4,	6,	8,	2,	4,	6,	8,	2,	4,	6,	8,	2,	4,	6,	8,	2,	4,	6,	8,	2,	4,	6,	8,	2,	4,	6,	8,	2,	4,	6,	8,	10,	2,	4,	6,	8,	10,	12,	2,	4,	6,	8,	10,	12,	14,	16,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,	2,	4,	6,	8,	10,	12,	14,	16,	18,	20,	22,

};

const char Munsell_H[5000][6] =
{
"2.5GY",	"5GY",	"7.5GY",	"7.5GY",	"10GY",	"10GY",	"2.5G",	"2.5G",	"5G",	"5G",
"7.5G",	"7.5G",	"10G",	"10G",	"2.5BG",	"2.5BG",	"5BG",	"5BG",	"7.5BG",	"7.5BG",
"10BG",	"10BG",	"2.5B",	"2.5B",	"5B",	"5B",	"7.5B",	"7.5B",	"10B",	"10B",
"10B",	"2.5PB",	"2.5PB",	"2.5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",
"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",
"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",
"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"5P",
"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",
"7.5P",	"7.5P",	"10P",	"10P",	"10P",	"10P",	"10P",	"2.5RP",	"2.5RP",	"2.5RP",
"2.5RP",	"2.5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"7.5RP",	"7.5RP",	"7.5RP",
"7.5RP",	"10RP",	"10RP",	"10RP",	"2.5R",	"2.5R",	"2.5R",	"5R",	"5R",	"7.5R",
"7.5R",	"10R",	"10R",	"2.5YR",	"2.5YR",	"5YR",	"5YR",	"7.5YR",	"10YR",	"2.5Y",	"5Y",	"7.5Y",	"10Y",	"2.5GY",	"2.5GY",	"5GY",	"5GY",	"7.5GY",	"7.5GY",	"10GY",	"10GY",	"2.5G",	"2.5G",	"5G",	"5G",	"7.5G",	"7.5G",	"10G",	"10G",	"2.5BG",	"2.5BG",	"5BG",	"5BG",	"7.5BG",	"7.5BG",	"7.5BG",	"10BG",	"10BG",	"10BG",	"2.5B",	"2.5B",	"2.5B",	"5B",	"5B",	"5B",	"7.5B",	"7.5B",	"7.5B",	"10B",	"10B",	"10B",	"10B",	"2.5PB",	"2.5PB",	"2.5PB",	"2.5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"10RP",	"10RP",	"10RP",	"10RP",	"10RP",	"2.5R",	"2.5R",	"2.5R",	"2.5R",	"2.5R",	"5R",	"5R",	"5R",	"5R",	"7.5R",	"7.5R",	"7.5R",	"7.5R",	"10R",	"10R",	"10R",	"10R",	"2.5YR",	"2.5YR",	"2.5YR",	"2.5YR",	"5YR",	"5YR",	"5YR",	"7.5YR",	"7.5YR",	"10YR",	"2.5Y",	"5Y",	"7.5Y",	"10Y",	"10Y",	"2.5GY",	"2.5GY",	"5GY",	"5GY",	"7.5GY",	"7.5GY",	"7.5GY",	"10GY",	"10GY",	"10GY",	"2.5G",	"2.5G",	"2.5G",	"5G",	"5G",	"5G",	"7.5G",	"7.5G",	"7.5G",	"10G",	"10G",	"10G",	"2.5BG",	"2.5BG",	"2.5BG",	"5BG",	"5BG",	"5BG",	"7.5BG",	"7.5BG",	"7.5BG",	"10BG",	"10BG",	"10BG",	"2.5B",	"2.5B",	"2.5B",	"2.5B",	"5B",	"5B",	"5B",	"5B",	"7.5B",	"7.5B",	"7.5B",	"7.5B",	"10B",	"10B",	"10B",	"10B",	"2.5PB",	"2.5PB",	"2.5PB",	"2.5PB",	"2.5PB",	"2.5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"10RP",	"10RP",	"10RP",	"10RP",	"10RP",	"10RP",	"2.5R",	"2.5R",	"2.5R",	"2.5R",	"2.5R",	"2.5R",	"5R",	"5R",	"5R",	"5R",	"5R",	"7.5R",	"7.5R",	"7.5R",	"7.5R",	"7.5R",	"10R",	"10R",	"10R",	"10R",	"10R",	"2.5YR",	"2.5YR",	"2.5YR",	"2.5YR",	"2.5YR",	"5YR",	"5YR",	"5YR",	"5YR",	"7.5YR",	"7.5YR",	"10YR",	"10YR",	"2.5Y",	"2.5Y",	"5Y",	"7.5Y",	"7.5Y",	"10Y",	"10Y",	"2.5GY",	"2.5GY",	"5GY",	"5GY",	"5GY",	"7.5GY",	"7.5GY",	"7.5GY",	"7.5GY",	"10GY",	"10GY",	"10GY",	"10GY",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"5G",	"5G",	"5G",	"5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"10G",	"10G",	"10G",	"10G",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"5BG",	"5BG",	"5BG",	"5BG",	"7.5BG",	"7.5BG",	"7.5BG",	"7.5BG",	"10BG",	"10BG",	"10BG",	"10BG",	"2.5B",	"2.5B",	"2.5B",	"2.5B",	"5B",	"5B",	"5B",	"5B",	"7.5B",	"7.5B",	"7.5B",	"7.5B",	"7.5B",	"10B",	"10B",	"10B",	"10B",	"10B",	"2.5PB",	"2.5PB",	"2.5PB",	"2.5PB",	"2.5PB",	"2.5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"10RP",	"10RP",	"10RP",	"10RP",	"10RP",	"10RP",	"10RP",	"2.5R",	"2.5R",	"2.5R",	"2.5R",	"2.5R",	"2.5R",	"2.5R",	"5R",	"5R",	"5R",	"5R",	"5R",	"5R",	"7.5R",	"7.5R",	"7.5R",	"7.5R",	"7.5R",	"7.5R",	"10R",	"10R",	"10R",	"10R",	"10R",	"10R",	"2.5YR",	"2.5YR",	"2.5YR",	"2.5YR",	"2.5YR",	"2.5YR",	"5YR",	"5YR",	"5YR",	"5YR",	"7.5YR",	"7.5YR",	"7.5YR",	"10YR",	"10YR",	"2.5Y",	"2.5Y",	"5Y",	"5Y",	"7.5Y",	"7.5Y",	"10Y",	"10Y",	"2.5GY",	"2.5GY",	"2.5GY",	"5GY",	"5GY",	"5GY",	"7.5GY",	"7.5GY",	"7.5GY",	"7.5GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"10G",	"10G",	"10G",	"10G",	"10G",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"5BG",	"5BG",	"5BG",	"5BG",	"5BG",	"7.5BG",	"7.5BG",	"7.5BG",	"7.5BG",	"10BG",	"10BG",	"10BG",	"10BG",	"2.5B",	"2.5B",	"2.5B",	"2.5B",	"5B",	"5B",	"5B",	"5B",	"5B",	"7.5B",	"7.5B",	"7.5B",	"7.5B",	"7.5B",	"10B",	"10B",	"10B",	"10B",	"10B",	"2.5PB",	"2.5PB",	"2.5PB",	"2.5PB",	"2.5PB",	"2.5PB",	"2.5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"10RP",	"10RP",	"10RP",	"10RP",	"10RP",	"10RP",	"10RP",	"10RP",	"2.5R",	"2.5R",	"2.5R",	"2.5R",	"2.5R",	"2.5R",	"2.5R",	"5R",	"5R",	"5R",	"5R",	"5R",	"5R",	"7.5R",	"7.5R",	"7.5R",	"7.5R",	"7.5R",	"7.5R",	"10R",	"10R",	"10R",	"10R",	"10R",	"10R",	"2.5YR",	"2.5YR",	"2.5YR",	"2.5YR",	"2.5YR",	"2.5YR",	"5YR",	"5YR",	"5YR",	"5YR",	"5YR",	"7.5YR",	"7.5YR",	"7.5YR",	"10YR",	"10YR",	"2.5Y",	"2.5Y",	"5Y",	"5Y",	"7.5Y",	"7.5Y",	"10Y",	"10Y",	"10Y",	"2.5GY",	"2.5GY",	"2.5GY",	"2.5GY",	"5GY",	"5GY",	"5GY",	"5GY",	"5GY",	"7.5GY",	"7.5GY",	"7.5GY",	"7.5GY",	"7.5GY",	"7.5GY",	"7.5GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"10G",	"10G",	"10G",	"10G",	"10G",	"10G",	"10G",	"10G",	"10G",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"5BG",	"5BG",	"5BG",	"5BG",	"5BG",	"5BG",	"5BG",	"5BG",	"7.5BG",	"7.5BG",	"7.5BG",	"7.5BG",	"7.5BG",	"7.5BG",	"7.5BG",	"7.5BG",	"10BG",	"10BG",	"10BG",	"10BG",	"10BG",	"10BG",	"10BG",	"2.5B",	"2.5B",	"2.5B",	"2.5B",	"2.5B",	"2.5B",	"5B",	"5B",	"5B",	"5B",	"5B",	"5B",	"7.5B",	"7.5B",	"7.5B",	"7.5B",	"7.5B",	"7.5B",	"7.5B",	"10B",	"10B",	"10B",	"10B",	"10B",	"10B",	"10B",	"2.5PB",	"2.5PB",	"2.5PB",	"2.5PB",	"2.5PB",	"2.5PB",	"2.5PB",	"2.5PB",	"2.5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"10RP",	"10RP",	"10RP",	"10RP",	"10RP",	"10RP",	"10RP",	"10RP",	"10RP",	"2.5R",	"2.5R",	"2.5R",	"2.5R",	"2.5R",	"2.5R",	"2.5R",	"2.5R",	"5R",	"5R",	"5R",	"5R",	"5R",	"5R",	"5R",	"5R",	"7.5R",	"7.5R",	"7.5R",	"7.5R",	"7.5R",	"7.5R",	"7.5R",	"7.5R",	"10R",	"10R",	"10R",	"10R",	"10R",	"10R",	"10R",	"10R",	"2.5YR",	"2.5YR",	"2.5YR",	"2.5YR",	"2.5YR",	"2.5YR",	"2.5YR",	"2.5YR",	"5YR",	"5YR",	"5YR",	"5YR",	"5YR",	"7.5YR",	"7.5YR",	"7.5YR",	"7.5YR",	"10YR",	"10YR",	"10YR",	"10YR",	"2.5Y",	"2.5Y",	"2.5Y",	"2.5Y",	"5Y",	"5Y",	"5Y",	"7.5Y",	"7.5Y",	"7.5Y",	"10Y",	"10Y",	"10Y",	"10Y",	"2.5GY",	"2.5GY",	"2.5GY",	"2.5GY",	"2.5GY",	"5GY",	"5GY",	"5GY",	"5GY",	"5GY",	"5GY",	"7.5GY",	"7.5GY",	"7.5GY",	"7.5GY",	"7.5GY",	"7.5GY",	"7.5GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"10G",	"10G",	"10G",	"10G",	"10G",	"10G",	"10G",	"10G",	"10G",	"10G",	"10G",	"10G",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"5BG",	"5BG",	"5BG",	"5BG",	"5BG",	"5BG",	"5BG",	"5BG",	"5BG",	"5BG",	"5BG",	"7.5BG",	"7.5BG",	"7.5BG",	"7.5BG",	"7.5BG",	"7.5BG",	"7.5BG",	"7.5BG",	"7.5BG",	"7.5BG",	"10BG",	"10BG",	"10BG",	"10BG",	"10BG",	"10BG",	"10BG",	"10BG",	"10BG",	"2.5B",	"2.5B",	"2.5B",	"2.5B",	"2.5B",	"2.5B",	"2.5B",	"2.5B",	"5B",	"5B",	"5B",	"5B",	"5B",	"5B",	"5B",	"5B",	"7.5B",	"7.5B",	"7.5B",	"7.5B",	"7.5B",	"7.5B",	"7.5B",	"7.5B",	"10B",	"10B",	"10B",	"10B",	"10B",	"10B",	"10B",	"10B",	"10B",	"2.5PB",	"2.5PB",	"2.5PB",	"2.5PB",	"2.5PB",	"2.5PB",	"2.5PB",	"2.5PB",	"2.5PB",	"2.5PB",	"2.5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"10RP",	"10RP",	"10RP",	"10RP",	"10RP",	"10RP",	"10RP",	"10RP",	"10RP",	"10RP",	"2.5R",	"2.5R",	"2.5R",	"2.5R",	"2.5R",	"2.5R",	"2.5R",	"2.5R",	"2.5R",	"2.5R",	"5R",	"5R",	"5R",	"5R",	"5R",	"5R",	"5R",	"5R",	"5R",	"5R",	"7.5R",	"7.5R",	"7.5R",	"7.5R",	"7.5R",	"7.5R",	"7.5R",	"7.5R",	"7.5R",	"7.5R",	"10R",	"10R",	"10R",	"10R",	"10R",	"10R",	"10R",	"10R",	"10R",	"10R",	"2.5YR",	"2.5YR",	"2.5YR",	"2.5YR",	"2.5YR",	"2.5YR",	"2.5YR",	"2.5YR",	"5YR",	"5YR",	"5YR",	"5YR",	"5YR",	"5YR",	"7.5YR",	"7.5YR",	"7.5YR",	"7.5YR",	"7.5YR",	"10YR",	"10YR",	"10YR",	"10YR",	"10YR",	"2.5Y",	"2.5Y",	"2.5Y",	"2.5Y",	"2.5Y",	"5Y",	"5Y",	"5Y",	"5Y",	"7.5Y",	"7.5Y",	"7.5Y",	"7.5Y",	"10Y",	"10Y",	"10Y",	"10Y",	"10Y",	"10Y",	"2.5GY",	"2.5GY",	"2.5GY",	"2.5GY",	"2.5GY",	"2.5GY",	"5GY",	"5GY",	"5GY",	"5GY",	"5GY",	"5GY",	"5GY",	"7.5GY",	"7.5GY",	"7.5GY",	"7.5GY",	"7.5GY",	"7.5GY",	"7.5GY",	"7.5GY",	"7.5GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"10G",	"10G",	"10G",	"10G",	"10G",	"10G",	"10G",	"10G",	"10G",	"10G",	"10G",	"10G",	"10G",	"10G",	"10G",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"5BG",	"5BG",	"5BG",	"5BG",	"5BG",	"5BG",	"5BG",	"5BG",	"5BG",	"5BG",	"5BG",	"5BG",	"5BG",	"7.5BG",	"7.5BG",	"7.5BG",	"7.5BG",	"7.5BG",	"7.5BG",	"7.5BG",	"7.5BG",	"7.5BG",	"7.5BG",	"7.5BG",	"7.5BG",	"10BG",	"10BG",	"10BG",	"10BG",	"10BG",	"10BG",	"10BG",	"10BG",	"10BG",	"10BG",	"10BG",	"2.5B",	"2.5B",	"2.5B",	"2.5B",	"2.5B",	"2.5B",	"2.5B",	"2.5B",	"2.5B",	"2.5B",	"5B",	"5B",	"5B",	"5B",	"5B",	"5B",	"5B",	"5B",	"5B",	"7.5B",	"7.5B",	"7.5B",	"7.5B",	"7.5B",	"7.5B",	"7.5B",	"7.5B",	"7.5B",	"10B",	"10B",	"10B",	"10B",	"10B",	"10B",	"10B",	"10B",	"10B",	"10B",	"2.5PB",	"2.5PB",	"2.5PB",	"2.5PB",	"2.5PB",	"2.5PB",	"2.5PB",	"2.5PB",	"2.5PB",	"2.5PB",	"2.5PB",	"2.5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"10RP",	"10RP",	"10RP",	"10RP",	"10RP",	"10RP",	"10RP",	"10RP",	"10RP",	"10RP",	"10RP",	"10RP",	"2.5R",	"2.5R",	"2.5R",	"2.5R",	"2.5R",	"2.5R",	"2.5R",	"2.5R",	"2.5R",	"2.5R",	"2.5R",	"2.5R",	"5R",	"5R",	"5R",	"5R",	"5R",	"5R",	"5R",	"5R",	"5R",	"5R",	"5R",	"5R",	"7.5R",	"7.5R",	"7.5R",	"7.5R",	"7.5R",	"7.5R",	"7.5R",	"7.5R",	"7.5R",	"7.5R",	"7.5R",	"7.5R",	"10R",	"10R",	"10R",	"10R",	"10R",	"10R",	"10R",	"10R",	"10R",	"10R",	"10R",	"10R",	"2.5YR",	"2.5YR",	"2.5YR",	"2.5YR",	"2.5YR",	"2.5YR",	"2.5YR",	"2.5YR",	"2.5YR",	"5YR",	"5YR",	"5YR",	"5YR",	"5YR",	"5YR",	"5YR",	"7.5YR",	"7.5YR",	"7.5YR",	"7.5YR",	"7.5YR",	"7.5YR",	"7.5YR",	"10YR",	"10YR",	"10YR",	"10YR",	"10YR",	"10YR",	"2.5Y",	"2.5Y",	"2.5Y",	"2.5Y",	"2.5Y",	"2.5Y",	"5Y",	"5Y",	"5Y",	"5Y",	"5Y",	"5Y",	"7.5Y",	"7.5Y",	"7.5Y",	"7.5Y",	"7.5Y",	"7.5Y",	"10Y",	"10Y",	"10Y",	"10Y",	"10Y",	"10Y",	"10Y",	"2.5GY",	"2.5GY",	"2.5GY",	"2.5GY",	"2.5GY",	"2.5GY",	"2.5GY",	"5GY",	"5GY",	"5GY",	"5GY",	"5GY",	"5GY",	"5GY",	"5GY",	"7.5GY",	"7.5GY",	"7.5GY",	"7.5GY",	"7.5GY",	"7.5GY",	"7.5GY",	"7.5GY",	"7.5GY",	"7.5GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"10G",	"10G",	"10G",	"10G",	"10G",	"10G",	"10G",	"10G",	"10G",	"10G",	"10G",	"10G",	"10G",	"10G",	"10G",	"10G",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"5BG",	"5BG",	"5BG",	"5BG",	"5BG",	"5BG",	"5BG",	"5BG",	"5BG",	"5BG",	"5BG",	"5BG",	"5BG",	"5BG",	"5BG",	"7.5BG",	"7.5BG",	"7.5BG",	"7.5BG",	"7.5BG",	"7.5BG",	"7.5BG",	"7.5BG",	"7.5BG",	"7.5BG",	"7.5BG",	"7.5BG",	"7.5BG",	"10BG",	"10BG",	"10BG",	"10BG",	"10BG",	"10BG",	"10BG",	"10BG",	"10BG",	"10BG",	"10BG",	"10BG",	"2.5B",	"2.5B",	"2.5B",	"2.5B",	"2.5B",	"2.5B",	"2.5B",	"2.5B",	"2.5B",	"2.5B",	"5B",	"5B",	"5B",	"5B",	"5B",	"5B",	"5B",	"5B",	"5B",	"5B",	"7.5B",	"7.5B",	"7.5B",	"7.5B",	"7.5B",	"7.5B",	"7.5B",	"7.5B",	"7.5B",	"7.5B",	"10B",	"10B",	"10B",	"10B",	"10B",	"10B",	"10B",	"10B",	"10B",	"10B",	"10B",	"2.5PB",	"2.5PB",	"2.5PB",	"2.5PB",	"2.5PB",	"2.5PB",	"2.5PB",	"2.5PB",	"2.5PB",	"2.5PB",	"2.5PB",	"2.5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"10RP",	"10RP",	"10RP",	"10RP",	"10RP",	"10RP",	"10RP",	"10RP",	"10RP",	"10RP",	"10RP",	"10RP",	"10RP",	"10RP",	"2.5R",	"2.5R",	"2.5R",	"2.5R",	"2.5R",	"2.5R",	"2.5R",	"2.5R",	"2.5R",	"2.5R",	"2.5R",	"2.5R",	"2.5R",	"2.5R",	"5R",	"5R",	"5R",	"5R",	"5R",	"5R",	"5R",	"5R",	"5R",	"5R",	"5R",	"5R",	"5R",	"5R",	"7.5R",	"7.5R",	"7.5R",	"7.5R",	"7.5R",	"7.5R",	"7.5R",	"7.5R",	"7.5R",	"7.5R",	"7.5R",	"7.5R",	"7.5R",	"7.5R",	"10R",	"10R",	"10R",	"10R",	"10R",	"10R",	"10R",	"10R",	"10R",	"10R",	"10R",	"10R",	"10R",	"2.5YR",	"2.5YR",	"2.5YR",	"2.5YR",	"2.5YR",	"2.5YR",	"2.5YR",	"2.5YR",	"2.5YR",	"2.5YR",	"5YR",	"5YR",	"5YR",	"5YR",	"5YR",	"5YR",	"5YR",	"5YR",	"5YR",	"7.5YR",	"7.5YR",	"7.5YR",	"7.5YR",	"7.5YR",	"7.5YR",	"7.5YR",	"7.5YR",	"10YR",	"10YR",	"10YR",	"10YR",	"10YR",	"10YR",	"10YR",	"10YR",	"2.5Y",	"2.5Y",	"2.5Y",	"2.5Y",	"2.5Y",	"2.5Y",	"2.5Y",	"5Y",	"5Y",	"5Y",	"5Y",	"5Y",	"5Y",	"5Y",	"7.5Y",	"7.5Y",	"7.5Y",	"7.5Y",	"7.5Y",	"7.5Y",	"7.5Y",	"10Y",	"10Y",	"10Y",	"10Y",	"10Y",	"10Y",	"10Y",	"10Y",	"2.5GY",	"2.5GY",	"2.5GY",	"2.5GY",	"2.5GY",	"2.5GY",	"2.5GY",	"2.5GY",	"5GY",	"5GY",	"5GY",	"5GY",	"5GY",	"5GY",	"5GY",	"5GY",	"5GY",	"7.5GY",	"7.5GY",	"7.5GY",	"7.5GY",	"7.5GY",	"7.5GY",	"7.5GY",	"7.5GY",	"7.5GY",	"7.5GY",	"7.5GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"10G",	"10G",	"10G",	"10G",	"10G",	"10G",	"10G",	"10G",	"10G",	"10G",	"10G",	"10G",	"10G",	"10G",	"10G",	"10G",	"10G",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"5BG",	"5BG",	"5BG",	"5BG",	"5BG",	"5BG",	"5BG",	"5BG",	"5BG",	"5BG",	"5BG",	"5BG",	"5BG",	"5BG",	"5BG",	"7.5BG",	"7.5BG",	"7.5BG",	"7.5BG",	"7.5BG",	"7.5BG",	"7.5BG",	"7.5BG",	"7.5BG",	"7.5BG",	"7.5BG",	"7.5BG",	"7.5BG",	"10BG",	"10BG",	"10BG",	"10BG",	"10BG",	"10BG",	"10BG",	"10BG",	"10BG",	"10BG",	"10BG",	"10BG",	"2.5B",	"2.5B",	"2.5B",	"2.5B",	"2.5B",	"2.5B",	"2.5B",	"2.5B",	"2.5B",	"2.5B",	"2.5B",	"5B",	"5B",	"5B",	"5B",	"5B",	"5B",	"5B",	"5B",	"5B",	"5B",	"7.5B",	"7.5B",	"7.5B",	"7.5B",	"7.5B",	"7.5B",	"7.5B",	"7.5B",	"7.5B",	"7.5B",	"7.5B",	"10B",	"10B",	"10B",	"10B",	"10B",	"10B",	"10B",	"10B",	"10B",	"10B",	"10B",	"10B",	"2.5PB",	"2.5PB",	"2.5PB",	"2.5PB",	"2.5PB",	"2.5PB",	"2.5PB",	"2.5PB",	"2.5PB",	"2.5PB",	"2.5PB",	"2.5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"10RP",	"10RP",	"10RP",	"10RP",	"10RP",	"10RP",	"10RP",	"10RP",	"10RP",	"10RP",	"10RP",	"10RP",	"10RP",	"10RP",	"2.5R",	"2.5R",	"2.5R",	"2.5R",	"2.5R",	"2.5R",	"2.5R",	"2.5R",	"2.5R",	"2.5R",	"2.5R",	"2.5R",	"2.5R",	"2.5R",	"5R",	"5R",	"5R",	"5R",	"5R",	"5R",	"5R",	"5R",	"5R",	"5R",	"5R",	"5R",	"5R",	"5R",	"5R",	"7.5R",	"7.5R",	"7.5R",	"7.5R",	"7.5R",	"7.5R",	"7.5R",	"7.5R",	"7.5R",	"7.5R",	"7.5R",	"7.5R",	"7.5R",	"7.5R",	"7.5R",	"10R",	"10R",	"10R",	"10R",	"10R",	"10R",	"10R",	"10R",	"10R",	"10R",	"10R",	"10R",	"10R",	"10R",	"10R",	"2.5YR",	"2.5YR",	"2.5YR",	"2.5YR",	"2.5YR",	"2.5YR",	"2.5YR",	"2.5YR",	"2.5YR",	"2.5YR",	"2.5YR",	"5YR",	"5YR",	"5YR",	"5YR",	"5YR",	"5YR",	"5YR",	"5YR",	"5YR",	"5YR",	"7.5YR",	"7.5YR",	"7.5YR",	"7.5YR",	"7.5YR",	"7.5YR",	"7.5YR",	"7.5YR",	"7.5YR",	"7.5YR",	"10YR",	"10YR",	"10YR",	"10YR",	"10YR",	"10YR",	"10YR",	"10YR",	"10YR",	"2.5Y",	"2.5Y",	"2.5Y",	"2.5Y",	"2.5Y",	"2.5Y",	"2.5Y",	"2.5Y",	"2.5Y",	"5Y",	"5Y",	"5Y",	"5Y",	"5Y",	"5Y",	"5Y",	"5Y",	"7.5Y",	"7.5Y",	"7.5Y",	"7.5Y",	"7.5Y",	"7.5Y",	"7.5Y",	"7.5Y",	"10Y",	"10Y",	"10Y",	"10Y",	"10Y",	"10Y",	"10Y",	"10Y",	"10Y",	"2.5GY",	"2.5GY",	"2.5GY",	"2.5GY",	"2.5GY",	"2.5GY",	"2.5GY",	"2.5GY",	"2.5GY",	"5GY",	"5GY",	"5GY",	"5GY",	"5GY",	"5GY",	"5GY",	"5GY",	"5GY",	"5GY",	"7.5GY",	"7.5GY",	"7.5GY",	"7.5GY",	"7.5GY",	"7.5GY",	"7.5GY",	"7.5GY",	"7.5GY",	"7.5GY",	"7.5GY",	"7.5GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"10G",	"10G",	"10G",	"10G",	"10G",	"10G",	"10G",	"10G",	"10G",	"10G",	"10G",	"10G",	"10G",	"10G",	"10G",	"10G",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"5BG",	"5BG",	"5BG",	"5BG",	"5BG",	"5BG",	"5BG",	"5BG",	"5BG",	"5BG",	"5BG",	"5BG",	"5BG",	"5BG",	"5BG",	"7.5BG",	"7.5BG",	"7.5BG",	"7.5BG",	"7.5BG",	"7.5BG",	"7.5BG",	"7.5BG",	"7.5BG",	"7.5BG",	"7.5BG",	"7.5BG",	"7.5BG",	"10BG",	"10BG",	"10BG",	"10BG",	"10BG",	"10BG",	"10BG",	"10BG",	"10BG",	"10BG",	"10BG",	"10BG",	"2.5B",	"2.5B",	"2.5B",	"2.5B",	"2.5B",	"2.5B",	"2.5B",	"2.5B",	"2.5B",	"2.5B",	"2.5B",	"5B",	"5B",	"5B",	"5B",	"5B",	"5B",	"5B",	"5B",	"5B",	"5B",	"7.5B",	"7.5B",	"7.5B",	"7.5B",	"7.5B",	"7.5B",	"7.5B",	"7.5B",	"7.5B",	"7.5B",	"10B",	"10B",	"10B",	"10B",	"10B",	"10B",	"10B",	"10B",	"10B",	"10B",	"2.5PB",	"2.5PB",	"2.5PB",	"2.5PB",	"2.5PB",	"2.5PB",	"2.5PB",	"2.5PB",	"2.5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"10RP",	"10RP",	"10RP",	"10RP",	"10RP",	"10RP",	"10RP",	"10RP",	"10RP",	"10RP",	"10RP",	"10RP",	"2.5R",	"2.5R",	"2.5R",	"2.5R",	"2.5R",	"2.5R",	"2.5R",	"2.5R",	"2.5R",	"2.5R",	"2.5R",	"2.5R",	"5R",	"5R",	"5R",	"5R",	"5R",	"5R",	"5R",	"5R",	"5R",	"5R",	"5R",	"5R",	"7.5R",	"7.5R",	"7.5R",	"7.5R",	"7.5R",	"7.5R",	"7.5R",	"7.5R",	"7.5R",	"7.5R",	"7.5R",	"7.5R",	"7.5R",	"10R",	"10R",	"10R",	"10R",	"10R",	"10R",	"10R",	"10R",	"10R",	"10R",	"10R",	"10R",	"10R",	"2.5YR",	"2.5YR",	"2.5YR",	"2.5YR",	"2.5YR",	"2.5YR",	"2.5YR",	"2.5YR",	"2.5YR",	"2.5YR",	"2.5YR",	"2.5YR",	"5YR",	"5YR",	"5YR",	"5YR",	"5YR",	"5YR",	"5YR",	"5YR",	"5YR",	"5YR",	"5YR",	"7.5YR",	"7.5YR",	"7.5YR",	"7.5YR",	"7.5YR",	"7.5YR",	"7.5YR",	"7.5YR",	"7.5YR",	"7.5YR",	"7.5YR",	"10YR",	"10YR",	"10YR",	"10YR",	"10YR",	"10YR",	"10YR",	"10YR",	"10YR",	"10YR",	"2.5Y",	"2.5Y",	"2.5Y",	"2.5Y",	"2.5Y",	"2.5Y",	"2.5Y",	"2.5Y",	"2.5Y",	"2.5Y",	"5Y",	"5Y",	"5Y",	"5Y",	"5Y",	"5Y",	"5Y",	"5Y",	"5Y",	"5Y",	"7.5Y",	"7.5Y",	"7.5Y",	"7.5Y",	"7.5Y",	"7.5Y",	"7.5Y",	"7.5Y",	"7.5Y",	"10Y",	"10Y",	"10Y",	"10Y",	"10Y",	"10Y",	"10Y",	"10Y",	"10Y",	"10Y",	"2.5GY",	"2.5GY",	"2.5GY",	"2.5GY",	"2.5GY",	"2.5GY",	"2.5GY",	"2.5GY",	"2.5GY",	"2.5GY",	"2.5GY",	"5GY",	"5GY",	"5GY",	"5GY",	"5GY",	"5GY",	"5GY",	"5GY",	"5GY",	"5GY",	"5GY",	"7.5GY",	"7.5GY",	"7.5GY",	"7.5GY",	"7.5GY",	"7.5GY",	"7.5GY",	"7.5GY",	"7.5GY",	"7.5GY",	"7.5GY",	"7.5GY",	"7.5GY",	"7.5GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"10G",	"10G",	"10G",	"10G",	"10G",	"10G",	"10G",	"10G",	"10G",	"10G",	"10G",	"10G",	"10G",	"10G",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"5BG",	"5BG",	"5BG",	"5BG",	"5BG",	"5BG",	"5BG",	"5BG",	"5BG",	"5BG",	"5BG",	"5BG",	"5BG",	"7.5BG",	"7.5BG",	"7.5BG",	"7.5BG",	"7.5BG",	"7.5BG",	"7.5BG",	"7.5BG",	"7.5BG",	"7.5BG",	"7.5BG",	"7.5BG",	"10BG",	"10BG",	"10BG",	"10BG",	"10BG",	"10BG",	"10BG",	"10BG",	"10BG",	"10BG",	"10BG",	"2.5B",	"2.5B",	"2.5B",	"2.5B",	"2.5B",	"2.5B",	"2.5B",	"2.5B",	"2.5B",	"2.5B",	"5B",	"5B",	"5B",	"5B",	"5B",	"5B",	"5B",	"5B",	"5B",	"7.5B",	"7.5B",	"7.5B",	"7.5B",	"7.5B",	"7.5B",	"7.5B",	"7.5B",	"10B",	"10B",	"10B",	"10B",	"10B",	"10B",	"10B",	"2.5PB",	"2.5PB",	"2.5PB",	"2.5PB",	"2.5PB",	"2.5PB",	"2.5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"10RP",	"10RP",	"10RP",	"10RP",	"10RP",	"10RP",	"10RP",	"10RP",	"10RP",	"10RP",	"2.5R",	"2.5R",	"2.5R",	"2.5R",	"2.5R",	"2.5R",	"2.5R",	"2.5R",	"2.5R",	"2.5R",	"5R",	"5R",	"5R",	"5R",	"5R",	"5R",	"5R",	"5R",	"5R",	"7.5R",	"7.5R",	"7.5R",	"7.5R",	"7.5R",	"7.5R",	"7.5R",	"7.5R",	"7.5R",	"7.5R",	"10R",	"10R",	"10R",	"10R",	"10R",	"10R",	"10R",	"10R",	"10R",	"10R",	"10R",	"10R",	"10R",	"2.5YR",	"2.5YR",	"2.5YR",	"2.5YR",	"2.5YR",	"2.5YR",	"2.5YR",	"2.5YR",	"2.5YR",	"2.5YR",	"2.5YR",	"2.5YR",	"2.5YR",	"5YR",	"5YR",	"5YR",	"5YR",	"5YR",	"5YR",	"5YR",	"5YR",	"5YR",	"5YR",	"5YR",	"5YR",	"5YR",	"7.5YR",	"7.5YR",	"7.5YR",	"7.5YR",	"7.5YR",	"7.5YR",	"7.5YR",	"7.5YR",	"7.5YR",	"7.5YR",	"7.5YR",	"7.5YR",	"10YR",	"10YR",	"10YR",	"10YR",	"10YR",	"10YR",	"10YR",	"10YR",	"10YR",	"10YR",	"10YR",	"2.5Y",	"2.5Y",	"2.5Y",	"2.5Y",	"2.5Y",	"2.5Y",	"2.5Y",	"2.5Y",	"2.5Y",	"2.5Y",	"2.5Y",	"5Y",	"5Y",	"5Y",	"5Y",	"5Y",	"5Y",	"5Y",	"5Y",	"5Y",	"5Y",	"5Y",	"7.5Y",	"7.5Y",	"7.5Y",	"7.5Y",	"7.5Y",	"7.5Y",	"7.5Y",	"7.5Y",	"7.5Y",	"7.5Y",	"10Y",	"10Y",	"10Y",	"10Y",	"10Y",	"10Y",	"10Y",	"10Y",	"10Y",	"10Y",	"10Y",	"2.5GY",	"2.5GY",	"2.5GY",	"2.5GY",	"2.5GY",	"2.5GY",	"2.5GY",	"2.5GY",	"2.5GY",	"2.5GY",	"2.5GY",	"5GY",	"5GY",	"5GY",	"5GY",	"5GY",	"5GY",	"5GY",	"5GY",	"5GY",	"5GY",	"5GY",	"7.5GY",	"7.5GY",	"7.5GY",	"7.5GY",	"7.5GY",	"7.5GY",	"7.5GY",	"7.5GY",	"7.5GY",	"7.5GY",	"7.5GY",	"7.5GY",	"7.5GY",	"7.5GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"10G",	"10G",	"10G",	"10G",	"10G",	"10G",	"10G",	"10G",	"10G",	"10G",	"10G",	"10G",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"5BG",	"5BG",	"5BG",	"5BG",	"5BG",	"5BG",	"5BG",	"5BG",	"5BG",	"5BG",	"7.5BG",	"7.5BG",	"7.5BG",	"7.5BG",	"7.5BG",	"7.5BG",	"7.5BG",	"7.5BG",	"7.5BG",	"10BG",	"10BG",	"10BG",	"10BG",	"10BG",	"10BG",	"10BG",	"10BG",	"10BG",	"2.5B",	"2.5B",	"2.5B",	"2.5B",	"2.5B",	"2.5B",	"2.5B",	"2.5B",	"5B",	"5B",	"5B",	"5B",	"5B",	"5B",	"5B",	"5B",	"7.5B",	"7.5B",	"7.5B",	"7.5B",	"7.5B",	"7.5B",	"10B",	"10B",	"10B",	"10B",	"10B",	"2.5PB",	"2.5PB",	"2.5PB",	"2.5PB",	"2.5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"10PB",	"10PB",	"10PB",	"10PB",	"10PB",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"2.5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"10P",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"10RP",	"10RP",	"10RP",	"10RP",	"10RP",	"10RP",	"10RP",	"2.5R",	"2.5R",	"2.5R",	"2.5R",	"2.5R",	"2.5R",	"2.5R",	"5R",	"5R",	"5R",	"5R",	"5R",	"5R",	"7.5R",	"7.5R",	"7.5R",	"7.5R",	"7.5R",	"7.5R",	"7.5R",	"10R",	"10R",	"10R",	"10R",	"10R",	"10R",	"10R",	"10R",	"2.5YR",	"2.5YR",	"2.5YR",	"2.5YR",	"2.5YR",	"2.5YR",	"2.5YR",	"2.5YR",	"2.5YR",	"5YR",	"5YR",	"5YR",	"5YR",	"5YR",	"5YR",	"5YR",	"5YR",	"5YR",	"5YR",	"5YR",	"5YR",	"5YR",	"7.5YR",	"7.5YR",	"7.5YR",	"7.5YR",	"7.5YR",	"7.5YR",	"7.5YR",	"7.5YR",	"7.5YR",	"7.5YR",	"7.5YR",	"7.5YR",	"7.5YR",	"10YR",	"10YR",	"10YR",	"10YR",	"10YR",	"10YR",	"10YR",	"10YR",	"10YR",	"10YR",	"10YR",	"10YR",	"2.5Y",	"2.5Y",	"2.5Y",	"2.5Y",	"2.5Y",	"2.5Y",	"2.5Y",	"2.5Y",	"2.5Y",	"2.5Y",	"2.5Y",	"2.5Y",	"5Y",	"5Y",	"5Y",	"5Y",	"5Y",	"5Y",	"5Y",	"5Y",	"5Y",	"5Y",	"5Y",	"5Y",	"7.5Y",	"7.5Y",	"7.5Y",	"7.5Y",	"7.5Y",	"7.5Y",	"7.5Y",	"7.5Y",	"7.5Y",	"7.5Y",	"7.5Y",	"10Y",	"10Y",	"10Y",	"10Y",	"10Y",	"10Y",	"10Y",	"10Y",	"10Y",	"10Y",	"10Y",	"2.5GY",	"2.5GY",	"2.5GY",	"2.5GY",	"2.5GY",	"2.5GY",	"2.5GY",	"2.5GY",	"2.5GY",	"2.5GY",	"2.5GY",	"5GY",	"5GY",	"5GY",	"5GY",	"5GY",	"5GY",	"5GY",	"5GY",	"5GY",	"5GY",	"5GY",	"7.5GY",	"7.5GY",	"7.5GY",	"7.5GY",	"7.5GY",	"7.5GY",	"7.5GY",	"7.5GY",	"7.5GY",	"7.5GY",	"7.5GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"10GY",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"2.5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"7.5G",	"10G",	"10G",	"10G",	"10G",	"10G",	"10G",	"10G",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"2.5BG",	"5BG",	"5BG",	"5BG",	"5BG",	"5BG",	"5BG",	"5BG",	"7.5BG",	"7.5BG",	"7.5BG",	"7.5BG",	"7.5BG",	"7.5BG",	"10BG",	"10BG",	"10BG",	"10BG",	"10BG",	"10BG",	"2.5B",	"2.5B",	"2.5B",	"2.5B",	"2.5B",	"5B",	"5B",	"5B",	"5B",	"7.5B",	"7.5B",	"7.5B",	"10B",	"10B",	"10B",	"2.5PB",	"2.5PB",	"2.5PB",	"5PB",	"5PB",	"5PB",	"7.5PB",	"7.5PB",	"7.5PB",	"10PB",	"10PB",	"10PB",	"2.5P",	"2.5P",	"2.5P",	"5P",	"5P",	"5P",	"5P",	"7.5P",	"7.5P",	"7.5P",	"7.5P",	"10P",	"10P",	"10P",	"10P",	"10P",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"2.5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"7.5RP",	"10RP",	"10RP",	"10RP",	"10RP",	"2.5R",	"2.5R",	"2.5R",	"2.5R",	"5R",	"5R",	"5R",	"5R",	"7.5R",	"7.5R",	"7.5R",	"7.5R",	"10R",	"10R",	"10R",	"10R",	"2.5YR",	"2.5YR",	"2.5YR",	"2.5YR",	"5YR",	"5YR",	"5YR",	"5YR",	"5YR",	"7.5YR",	"7.5YR",	"7.5YR",	"7.5YR",	"7.5YR",	"7.5YR",	"10YR",	"10YR",	"10YR",	"10YR",	"10YR",	"10YR",	"10YR",	"10YR",	"2.5Y",	"2.5Y",	"2.5Y",	"2.5Y",	"2.5Y",	"2.5Y",	"2.5Y",	"2.5Y",	"2.5Y",	"2.5Y",	"2.5Y",	"5Y",	"5Y",	"5Y",	"5Y",	"5Y",	"5Y",	"5Y",	"5Y",	"5Y",	"5Y",	"5Y",	"7.5Y",	"7.5Y",	"7.5Y",	"7.5Y",	"7.5Y",	"7.5Y",	"7.5Y",	"7.5Y",	"7.5Y",	"7.5Y",	"7.5Y",

}
;
const float Munsell_V[5000] =
{
 0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.2,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.4,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.6,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	0.8,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	1,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	3,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	5,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	7,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	9,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,
 //2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	2,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	4,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	6,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	8,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	10,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	20,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	30,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	40,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	50,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	60,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	70,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	80,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	90,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,	100,
};
typedef struct
{
	float x[10];
	float y[10];
	char H[10][5];
	float C[10];
	float lenth[10];
	int count[10];
	float  tancita[10];
	float    H_D[10];
	char H_Z[10][2];
}Pointer_linshi;

int C_coordinates[9] = { 0 };
char H_coordinates[9][5] = { 0 };
float lenth0 = 0, lenth1 = 0, lenth2 = 0, lenthE = 0, lenthA = 0, lenthG = 0, lenthD = 0, lenthMax = 0, lenthMin = 0;
int countk = 0;
int countE = 0, countA = 0, countG = 0, countD = 0;
int fgE = 0, fgA = 0, fgG = 0, fgD = 0;
double Degree[3] = { 0 };
float V_Difference = 0;
unsigned char step1, step2;
float LENTH1;
Pointer_linshi Pointer_linshiE;
Pointer_linshi Pointer_linshiA;
Pointer_linshi Pointer_linshiG;
Pointer_linshi Pointer_linshiD;
float tan0, tan1, tan2, tan3;
int i, j, n;
double value_m[4] = { 98.34,0 };
clralgo_CIEYxy  value_n = { 10.05,0.4002,0.3504 };




void Get_Cpoint(float V, float x, float y)
{
	unsigned char CountPoint2 = 0, CountPoint3 = 0, CountPoint4 = 0, CountPoint5 = 0;
	float value[6], mum, mum2;
	int L;
	int find, find2;
	if (V > 10) i = 13;
	else
	{
		for (i = 0; i < 13; i++)
		{
			if (V_value[i] < V & V_value[i + 1] > V)
			{
				V_Difference = V - V_value[i];
				break;
			}
		}
	}
	fgA = 0;
	fgD = 0;
	lenth1 = sqrt(pow((x - 310.1), 2) + pow((y - 316.3), 2));
	if (lenth1 < 41) Degree[2] = 20;
	else if (lenth1 > 100) Degree[2] = 120;
	else  Degree[2] = 20;
	LENTH1 = lenth1;
	tan1 = (y - 316.3) / (x - 310.1);
	Degree[0] = (atan2((y - 316.3), (x - 310.1)) / 3.14159) * 180;
	if (Degree[0] < 0)  Degree[0] += 360;
	memset(&Pointer_linshiA, 0, sizeof(Pointer_linshiA));
	memset(&Pointer_linshiD, 0, sizeof(Pointer_linshiD));
	for (countk = 0; countk < 2; countk++)   //上下两个明度平面
	{
		lenthA = 0;
		lenthD = 0;
		CountPoint2 = 0;
		CountPoint3 = 0;
		lenthMax = 0;
		lenthMin = 1000;
		Pointer_linshiD.tancita[2 + countk * 2] = 400;
		Pointer_linshiD.tancita[8 + countk] = 400;
		Pointer_linshiD.lenth[8 + countk] = 1000;
		Pointer_linshiD.tancita[countk] = 400;
		for (j = V_COUNT_START[i + countk] + 1; j < V_COUNT_START[i + countk] + V_COUNT[i + countk]; j++)
		{
			lenth0 = sqrt(pow((Munsell_x[j] - 310.1), 2) + pow((Munsell_y[j] - 316.3), 2));
			lenth2 = sqrt(pow((Munsell_x[j] - x), 2) + pow((Munsell_y[j] - y), 2));
			Degree[1] = 180 * (atan2((Munsell_y[j] - 316.3), (Munsell_x[j] - 310.1)) / 3.1415926);
			if (lenth0 > lenth1)
			{
				if (((lenth2 < lenthD) || CountPoint2 == 0) && (fabs(Degree[1] - Degree[0]) < Degree[2]) && fabs(Degree[1] - Degree[0]) < Pointer_linshiD.tancita[countk])
				{
					lenthD = lenth2;
					CountPoint2++;
					countD = 0;
					fgD = countk;
					Pointer_linshiD.lenth[fgD] = lenth2;
					Pointer_linshiD.tancita[fgD] = fabs(Degree[1] - Degree[0]);
					Pointer_linshiD.x[fgD] = Munsell_x[j];
					Pointer_linshiD.y[fgD] = Munsell_y[j];
					Pointer_linshiD.C[fgD] = Munsell_C[j];
					Pointer_linshiD.H_D[fgD] = Munsell_H_D[j];
					for (L = 0; L < 2; L++)
					{
						Pointer_linshiD.H_Z[fgD][L] = Munsell_H_Z[j][L];
					}
					for (L = 0; L < 5; L++)
					{
						Pointer_linshiD.H[fgE][L] = Munsell_H[j][L];
					}

				}
				if (lenth0 > lenthMax && Munsell_C[j] == 2)
				{
					lenthMax = lenth0;
					fgD = 2 + countk * 2;
					Pointer_linshiD.lenth[fgD] = lenth0;
					Pointer_linshiD.lenth[6 + countk] = lenth1;
					Pointer_linshiD.tancita[6 + countk] = (Degree[0]);
					Pointer_linshiD.tancita[fgD] = (Degree[1]);
					Pointer_linshiD.x[fgD] = Munsell_x[j];
					Pointer_linshiD.y[fgD] = Munsell_y[j];
					Pointer_linshiD.C[fgD] = Munsell_C[j];
				}

			}
			else if (CountPoint2 == 0 && Munsell_C[j] > Pointer_linshiD.C[4] && (fabs(lenth0 - lenth1) < Pointer_linshiD.lenth[4] || CountPoint5 == 0))
			{
				CountPoint5++;
				Pointer_linshiD.C[4] = Munsell_C[j];
				Pointer_linshiD.lenth[4] = fabs(lenth0 - lenth1);
				Pointer_linshiD.lenth[countk] = lenth0;
			}



			if (Munsell_C[j] == 2)
			{
				//                printf("NUM=: %d,lenth=:%1.2f, tancita = %1.2f,\r\n", j, lenth0,Degree[1]);
			}
		}

		for (j = V_COUNT_START[i + countk] + 1; j < V_COUNT_START[i + countk] + V_COUNT[i + countk]; j++)  //查表法找距离目标最近的四个点  
		{
			lenth0 = sqrt(pow((Munsell_x[j] - 310.1), 2) + pow((Munsell_y[j] - 316.3), 2));
			lenth2 = sqrt(pow((Munsell_x[j] - x), 2) + pow((Munsell_y[j] - y), 2));
			Degree[1] = 180 * (atan2((Munsell_y[j] - 316.3), (Munsell_x[j] - 310.1)) / 3.1415926);
			if (Degree[1] < 0)  Degree[1] += 360;
			if (lenth0 < lenth1 && (Munsell_C[j] < Pointer_linshiD.C[countk] || Pointer_linshiD.C[countk] == 0))
			{
				if (((lenth2 < lenthA) || CountPoint3 == 0) && (fabs(Degree[1] - Degree[0]) < Degree[2]))
				{
					lenthA = lenth2;
					CountPoint3++;
					countD = 0;
					fgA = countk;
					Pointer_linshiA.lenth[fgA] = lenth2;
					Pointer_linshiA.x[fgA] = Munsell_x[j];
					Pointer_linshiA.y[fgA] = Munsell_y[j];
					Pointer_linshiA.tancita[fgA] = fabs(Degree[1] - Degree[0]);
					Pointer_linshiA.C[fgA] = Munsell_C[j];
					Pointer_linshiA.H_D[fgD] = Munsell_H_D[j];
					for (L = 0; L < 2; L++)
					{
						Pointer_linshiA.H_Z[fgD][L] = Munsell_H_Z[j][L];
					}
					for (L = 0; L < 5; L++)
					{
						Pointer_linshiA.H[fgG][L] = Munsell_H[j][L];
					}
				}
			}
			//
			if (lenth0 < lenthMin && Munsell_C[j] == 2 && fabs(Pointer_linshiD.tancita[2 + countk * 2] - Degree[1]) < 100)
			{
				lenthMin = lenth0;
				countD = 0;
				fgD = 3 + countk * 2;
				Pointer_linshiD.lenth[fgD] = lenth0;
				Pointer_linshiD.tancita[fgD] = (Degree[1]);
				Pointer_linshiD.x[fgD] = Munsell_x[j];
				Pointer_linshiD.y[fgD] = Munsell_y[j];
				Pointer_linshiD.C[fgD] = Munsell_C[j];
			}
		}

	}



}

float get_V(float c, float Y)
{
	float x, x1, y1, y2;
	x = c;
	do
	{
		x1 = x;
		y1 = 0.000862 * pow(x, 5) - 0.0215489 * pow(x, 4) + 0.2456654 * pow(x, 3) - 0.2370495 * pow(x, 2) + 1.2533028 * x - Y;
		y2 = 0.00431 * pow(x, 4) - 0.0861956 * pow(x, 3) + 0.7369962 * pow(x, 2) + 1.2533028;
		x = x1 - y1 / y2;

	} while (fabs(x - x1) > 1e-5);
	return x1;
}


void Get_Fourpoint(float V, float x, float y, float C)
{
	unsigned char CountPoint = 0, CountPoint1 = 0, CountPoint2 = 0, CountPoint3 = 0, CountPoint4 = 0;
	float value[6], mum, mum2;
	unsigned char m;

	int find, find2;
	if (V > 10) i = 13;
	else
	{
		for (i = 0; i < 13; i++)
		{
			if (V_value[i] < V & V_value[i + 1] > V)
			{
				V_Difference = V - V_value[i];
				break;
			}
		}
	}
	fgE = 0;
	fgA = 0;
	fgG = 0;
	fgD = 0;
	lenth1 = sqrt(pow((x - 310.1), 2) + pow((y - 316.3), 2));
	LENTH1 = lenth1;
	Degree[0] = (atan2((y - 316.3), (x - 310.1)) / 3.14159) * 180;
	if (Degree[0] < 0)  Degree[0] += 360;
	memset(&Pointer_linshiE, 0, sizeof(Pointer_linshiE));
	memset(&Pointer_linshiG, 0, sizeof(Pointer_linshiG));
	for (countk = 0; countk < 2; countk++)   //上下两个明度平面
	{
		lenthE = 0;
		lenthA = 0;
		lenthG = 0;
		lenthD = 0;
		CountPoint = 0;
		CountPoint1 = 0;
		CountPoint2 = 0;
		CountPoint3 = 0;
		lenthMax = 0;
		for (j = V_COUNT_START[i + countk] + 1; j < V_COUNT_START[i + countk] + V_COUNT[i + countk]; j++)  //查表法找距离目标最近的四个点  
		{
			lenth0 = sqrt(pow((Munsell_x[j] - 310.1), 2) + pow((Munsell_y[j] - 316.3), 2));
			lenth2 = sqrt(pow((Munsell_x[j] - x), 2) + pow((Munsell_y[j] - y), 2));
			Degree[1] = 180 * (atan2((Munsell_y[j] - 316.3), (Munsell_x[j] - 310.1)) / 3.1415926);
			if (Degree[1] < 0)  Degree[1] += 360;
			if (Degree[1] > Degree[0])
			{
				if (((Degree[1] - Degree[0]) < Pointer_linshiE.tancita[fgE]) || CountPoint == 0)   //E点
				{
					if (fabs(Munsell_C[j] - C) <= 2 || (C < 2 && Munsell_C[j] == 2))
					{
						CountPoint++;
						countE = 0;
						fgE = countk;
						Pointer_linshiE.lenth[fgE] = fabs(lenth0 - lenth1);
						Pointer_linshiE.x[fgE] = Munsell_x[j];
						Pointer_linshiE.y[fgE] = Munsell_y[j];
						Pointer_linshiE.tancita[fgE] = Degree[1] - Degree[0];
						Pointer_linshiE.H_D[fgE] = Munsell_H_D[j];
						Pointer_linshiE.C[fgE] = Munsell_C[j];
						for (m = 0; m < 2; m++)
						{
							Pointer_linshiE.H_Z[fgE][m] = Munsell_H_Z[j][m];
						}
						for (m = 0; m < 5; m++)
						{
							Pointer_linshiE.H[fgE][m] = Munsell_H[j][m];
						}
					}

				}
			}
			else
			{
				if ((fabs(Degree[1] - Degree[0]) < fabs(Pointer_linshiG.tancita[fgG])) || CountPoint1 == 0)
				{
					if (fabs(Munsell_C[j] - C) <= 2 || (C < 2 && Munsell_C[j] == 2))
					{
						CountPoint1++;
						countG = 0;
						fgG = countk;
						Pointer_linshiG.lenth[fgG] = fabs(lenth0 - lenth1);
						Pointer_linshiG.x[fgG] = Munsell_x[j];
						Pointer_linshiG.y[fgG] = Munsell_y[j];
						Pointer_linshiG.tancita[fgG] = Degree[1] - Degree[0];
						Pointer_linshiG.H_D[fgG] = Munsell_H_D[j];
						Pointer_linshiG.C[fgG] = Munsell_C[j];
						for (m = 0; m < 2; m++)
						{
							Pointer_linshiG.H_Z[fgG][m] = Munsell_H_Z[j][m];
						}
						for (m = 0; m < 5; m++)
						{
							Pointer_linshiG.H[fgG][m] = Munsell_H[j][m];
						}
					}

				}
			}
		}
	}
	//    if(fabs(Pointer_linshiG.tancita[0])>6 && fabs(Pointer_linshiG.tancita[1])>6)
	//    {
	//        Get_Fourpoint2( V, x,  y, C);
	//    } 
}

float Get_H_VALUE(char* H_str)
{
	float H;
	float  value_h;
	float  value_h2;
	char fk = 0, fk2 = 0;

	if (Pointer_linshiE.H_Z[0][0] == Pointer_linshiG.H_Z[0][0] && Pointer_linshiE.H_Z[0][1] == Pointer_linshiG.H_Z[0][1]
		&& Pointer_linshiE.H_Z[1][0] == Pointer_linshiG.H_Z[1][0] && Pointer_linshiE.H_Z[1][1] == Pointer_linshiG.H_Z[1][1]
		)
	{
		value_h = fabs(Pointer_linshiE.tancita[0]) / (fabs(Pointer_linshiE.tancita[0]) + fabs(Pointer_linshiG.tancita[0]));
		value_h = Pointer_linshiE.H_D[0] - value_h * fabs(Pointer_linshiE.H_D[0] - Pointer_linshiG.H_D[0]);

		value_h2 = fabs(Pointer_linshiE.tancita[1]) / (fabs(Pointer_linshiE.tancita[1]) + fabs(Pointer_linshiG.tancita[1]));
		value_h2 = Pointer_linshiE.H_D[1] - value_h2 * fabs(Pointer_linshiE.H_D[1] - Pointer_linshiG.H_D[1]);

		value_h2 = value_h2 * V_Difference + value_h * (1 - V_Difference);
		//sprintf(H_str, "%1.1f", value_h2);
		H = value_h2;
		H_str[0] = Pointer_linshiE.H_Z[0][0];
		H_str[1] = Pointer_linshiE.H_Z[0][1];

	}
	else if (Pointer_linshiE.H_D[0] == 0 && Pointer_linshiE.H_D[1] == 0)
	{
		value_h = Pointer_linshiG.H_D[0];
		value_h2 = Pointer_linshiG.H_D[1];
		value_h2 = value_h2 * V_Difference + value_h * (1 - V_Difference);
		H = value_h2;
		//sprintf(H_str, "%1.1f", value_h2);
		H_str[0] = Pointer_linshiG.H_Z[0][0];
		H_str[1] = Pointer_linshiG.H_Z[0][1];
	}
	else if (Pointer_linshiG.H_D[0] == 0 && Pointer_linshiG.H_D[1] == 0)
	{
		value_h = Pointer_linshiE.H_D[0];
		value_h2 = Pointer_linshiE.H_D[1];
		value_h2 = value_h2 * V_Difference + value_h * (1 - V_Difference);
		H = value_h2;
		//sprintf(H_str, "%1.1f", value_h2);
		H_str[0] = Pointer_linshiE.H_Z[0][0];
		H_str[1] = Pointer_linshiE.H_Z[0][1];
	}
	else if (Pointer_linshiE.H_D[0] * Pointer_linshiE.H_D[1] * Pointer_linshiG.H_D[0] * Pointer_linshiG.H_D[1] == 0)
	{
		if (Pointer_linshiE.H_D[0] == 0)
			value_h = Pointer_linshiG.H_D[0];
		else if (Pointer_linshiG.H_D[0] == 0)
			value_h = Pointer_linshiE.H_D[0];
		else
		{
			fk2 = 1;
			value_h = fabs(Pointer_linshiE.tancita[0]) / (fabs(Pointer_linshiE.tancita[0]) + fabs(Pointer_linshiG.tancita[0]));
			value_h = Pointer_linshiE.H_D[0] - value_h * fabs(Pointer_linshiE.H_D[0] - Pointer_linshiG.H_D[0]);
		}

		if (Pointer_linshiE.H_D[1] == 0)
			value_h2 = Pointer_linshiG.H_D[1];
		else if (Pointer_linshiG.H_D[1] == 0)
			value_h2 = Pointer_linshiE.H_D[1];
		else
		{
			fk2 = 2;
			value_h2 = fabs(Pointer_linshiE.tancita[1]) / (fabs(Pointer_linshiE.tancita[1]) + fabs(Pointer_linshiG.tancita[1]));
			value_h2 = Pointer_linshiE.H_D[1] - value_h2 * fabs(Pointer_linshiE.H_D[1] - Pointer_linshiG.H_D[1]);
		}
		value_h2 = value_h2 * V_Difference + value_h * (1 - V_Difference);
		H = value_h2;
		//sprintf(H_str, "%1.1f", value_h2);
		H = value_h2;
		if (fk2 == 1)
		{
			H_str[0] = Pointer_linshiE.H_Z[0][0];
			H_str[1] = Pointer_linshiE.H_Z[0][1];
		}
		else if (fk2 == 2)
		{
			H_str[0] = Pointer_linshiG.H_Z[0][0];
			H_str[1] = Pointer_linshiG.H_Z[0][1];
		}

	}
	else
	{
		if ((fabs(Pointer_linshiE.tancita[0]) < 1 && fabs(Pointer_linshiG.tancita[0]) > 10))
		{
			value_h = Pointer_linshiE.H_D[0];
		}
		else if ((fabs(Pointer_linshiE.tancita[0]) > 10 && fabs(Pointer_linshiG.tancita[0]) < 1))
		{
			value_h = Pointer_linshiG.H_D[0];
		}

		if ((fabs(Pointer_linshiE.tancita[1]) < 1 && fabs(Pointer_linshiG.tancita[1]) > 10))
		{
			value_h2 = Pointer_linshiE.H_D[1];
			fk = 1;
		}
		else if ((fabs(Pointer_linshiE.tancita[1]) > 10 && fabs(Pointer_linshiG.tancita[1]) < 1))
		{
			value_h2 = Pointer_linshiG.H_D[1];
			fk = 2;
		}
		else
		{
			if (Pointer_linshiE.H_Z[0][0] != Pointer_linshiG.H_Z[0][0] || Pointer_linshiE.H_Z[0][1] != Pointer_linshiG.H_Z[0][1])
			{
				if (Pointer_linshiE.H_Z[1][0] != Pointer_linshiG.H_Z[1][0] || Pointer_linshiE.H_Z[1][1] != Pointer_linshiG.H_Z[1][1])
				{
					if (Pointer_linshiG.H_D[0] == 10 && Pointer_linshiG.H_D[1] == 10)
					{
						Pointer_linshiG.H_D[0] = 0;
						Pointer_linshiG.H_D[1] = 0;
					}
					else
					{
						Pointer_linshiE.H_D[0] += 10;
						Pointer_linshiE.H_D[1] += 10;
					}
					value_h = fabs(Pointer_linshiE.tancita[0]) / (fabs(Pointer_linshiE.tancita[0]) + fabs(Pointer_linshiG.tancita[0]));
					value_h = Pointer_linshiE.H_D[0] - value_h * fabs(Pointer_linshiE.H_D[0] - Pointer_linshiG.H_D[0]);

				}
				else
				{
					if (Pointer_linshiE.H_Z[0][0] != Pointer_linshiG.H_Z[1][0] || Pointer_linshiE.H_Z[0][1] != Pointer_linshiG.H_Z[1][1])
					{
						value_h = Pointer_linshiG.H_D[1];
						fk = 2;
					}
					else
					{
						value_h = Pointer_linshiE.H_D[0];
						fk = 1;
					}
				}
				value_h2 = fabs(Pointer_linshiE.tancita[1]) / (fabs(Pointer_linshiE.tancita[1]) + fabs(Pointer_linshiG.tancita[1]));
				value_h2 = Pointer_linshiE.H_D[1] - value_h2 * fabs(Pointer_linshiE.H_D[1] - Pointer_linshiG.H_D[1]);
			}
			else
			{
				value_h = fabs(Pointer_linshiE.tancita[0]) / (fabs(Pointer_linshiE.tancita[0]) + fabs(Pointer_linshiG.tancita[0]));
				value_h = Pointer_linshiE.H_D[0] - value_h * fabs(Pointer_linshiE.H_D[0] - Pointer_linshiG.H_D[0]);

				if (Pointer_linshiE.H_Z[1][0] != Pointer_linshiG.H_Z[0][0] || Pointer_linshiE.H_Z[1][1] != Pointer_linshiG.H_Z[0][1])
				{
					value_h2 = Pointer_linshiG.H_D[1];
					fk = 2;
				}
				else
				{
					value_h2 = Pointer_linshiE.H_D[1];
					fk = 1;
				}
			}
		}

		value_h2 = value_h2 * V_Difference + value_h * (1 - V_Difference);
		if (fk == 0)
		{
			if (value_h2 < 10)
			{
				fk = 3;
			}
			else
			{
				value_h2 -= 10;
			}
		}
		//sprintf(H_str, "%1.1f", value_h2);
		H = value_h2;
		if (fk == 1)
		{
			H_str[0] = Pointer_linshiE.H_Z[0][0];
			H_str[1] = Pointer_linshiE.H_Z[0][1];
		}
		if (fk == 3 || fk == 2)
		{
			H_str[0] = Pointer_linshiG.H_Z[0][0];
			H_str[1] = Pointer_linshiG.H_Z[0][1];
		}
		else
		{
			H_str[0] = Pointer_linshiE.H_Z[0][0];
			H_str[1] = Pointer_linshiE.H_Z[0][1];

		}
	}

	return H;
}

float Get_C_VALUE(void)
{
	float value = 0, value1 = 0;
	float  value_h;
	float  value_h2;
	float lentcos;
	char i, j;
	if (Pointer_linshiA.C[0] == 0 && Pointer_linshiA.C[1] == 0)
	{
		lentcos = (Pointer_linshiD.lenth[2] - Pointer_linshiD.lenth[3]) / (fabs(Pointer_linshiD.tancita[2] - Pointer_linshiD.tancita[3]));

		for (j = 0; j < 2; j++)
		{
			if (Pointer_linshiD.tancita[2 + 2 * j] < Pointer_linshiD.tancita[3 + 2 * j])
			{
				if (Pointer_linshiD.tancita[6 + j] > Pointer_linshiD.tancita[3 + 2 * j])
				{
					for (i = 0; i < 5; i++)
					{
						if ((Pointer_linshiD.tancita[6 + j] - 90 * (i + 1)) < Pointer_linshiD.tancita[3 + 2 * j])
						{
							Pointer_linshiD.tancita[6 + j] = Pointer_linshiD.tancita[6 + j] - 90 * (i + 1);
							break;
						}

					}
				}
				else if (Pointer_linshiD.tancita[6 + j] < Pointer_linshiD.tancita[2 + 2 * j])
				{
					for (i = 0; i < 5; i++)
					{
						if ((Pointer_linshiD.tancita[6 + j] + 90 * (i + 1)) > Pointer_linshiD.tancita[2 + 2 * j])
						{
							Pointer_linshiD.tancita[6 + j] = Pointer_linshiD.tancita[6 + j] + 90 * (i + 1);
							break;
						}

					}
				}

			}
			else if (Pointer_linshiD.tancita[2 + 2 * j] >= Pointer_linshiD.tancita[3 + 2 * j])
			{
				if (Pointer_linshiD.tancita[6 + j] > Pointer_linshiD.tancita[2 + 2 * j])
				{
					for (i = 0; i < 5; i++)
					{
						if ((Pointer_linshiD.tancita[6 + j] - 90 * (i + 1)) < Pointer_linshiD.tancita[2 + 2 * j])
						{
							Pointer_linshiD.tancita[6 + j] = Pointer_linshiD.tancita[6 + j] - 90 * (i + 1);
							break;
						}
					}
				}
				else if (Pointer_linshiD.tancita[6 + j] < Pointer_linshiD.tancita[3 + 2 * j])
				{
					for (i = 0; i < 5; i++)
					{
						if ((Pointer_linshiD.tancita[6 + j] + 90 * (i + 1)) > Pointer_linshiD.tancita[3 + 2 * j])
						{
							Pointer_linshiD.tancita[6 + j] = Pointer_linshiD.tancita[6 + j] + 90 * (i + 1);
							break;
						}
					}
				}
			}

		}

		value = Pointer_linshiD.lenth[3] + fabs(Pointer_linshiD.tancita[6] - Pointer_linshiD.tancita[3]) * lentcos;
		value = 2 * Pointer_linshiD.lenth[6] / value;

		lentcos = (Pointer_linshiD.lenth[4] - Pointer_linshiD.lenth[5]) / (fabs(Pointer_linshiD.tancita[4] - Pointer_linshiD.tancita[5]));

		value1 = Pointer_linshiD.lenth[5] + fabs(Pointer_linshiD.tancita[7] - Pointer_linshiD.tancita[5]) * lentcos;
		value1 = 2 * Pointer_linshiD.lenth[7] / value1;
		value = value1 * V_Difference + value * (1 - V_Difference);

	}
	else
	{
		if (Pointer_linshiA.C[0] == 0)
		{
			value = Pointer_linshiD.C[0];
		}
		else if (Pointer_linshiD.C[0] == 0)
		{
			value = Pointer_linshiA.C[0];
		}
		else
		{
			value = (Pointer_linshiD.C[0] - Pointer_linshiA.C[0]) / (Pointer_linshiD.lenth[0] + Pointer_linshiA.lenth[0]);
			value = Pointer_linshiD.C[0] - value * Pointer_linshiD.lenth[0];
		}

		if (Pointer_linshiA.C[1] == 0)
		{
			value1 = Pointer_linshiD.C[1];
		}
		else if (Pointer_linshiD.C[1] == 0)
		{
			value1 = Pointer_linshiA.C[1];
		}
		else
		{
			value1 = (Pointer_linshiD.C[1] - Pointer_linshiA.C[1]) / (Pointer_linshiD.lenth[1] + Pointer_linshiA.lenth[1]);
			value1 = Pointer_linshiD.C[1] - value1 * Pointer_linshiD.lenth[1];
		}

		value = value1 * V_Difference + value * (1 - V_Difference);
		//        if(Pointer_linshiD.C[0]==0 && Pointer_linshiD.C[1]==0)
		//            value += 0.5;        
	}

	return    value;
}


void clralgo_xyz_to_munsell(struct clralgo_CIEXYZ* xyz, struct clralgo_Munsell* munsell)
{
	//clralgo_CIEYxy value_n;
	char c[2] = { "--" };
	//int Vcount = 0;
	clralgo_CIEYxy yxy;
	//float value[3] = {0};
	clralgo_xyz_to_yxy(xyz, &yxy);
	value_n.Y = yxy.Y * 1000;
	value_n.x = yxy.x * 1000;
	value_n.y = yxy.y * 1000;
	value_m[0] = roundf(value_n.Y);
	value_m[1] = roundf(value_n.x);
	value_m[2] = roundf(value_n.y);

	for (j = 0; j < 2; j++)
	{
		munsell->H_str[j] = c[j];
	}
	if (value_n.Y == 0 && value_n.x == 0 && value_n.y == 0)  return;
	for (i = 0; i < 5000; i++)
	{
		if (value_m[0] >= Munsell_Y[i] && value_m[0] <= Munsell_Y[i + 1])
		{
			//Vcount = i;
			munsell->V = get_V(Munsell_V[i], value_m[0] / 1000) - 0.03;  //获取V值

			if (munsell->V > 8)  munsell->V -= 0.06;
			else if (munsell->V > 3)  munsell->V -= 0.03;
			break;
		}
	}
	Get_Cpoint(munsell->V, value_m[1], value_m[2]);
	munsell->C = Get_C_VALUE();
	Get_Fourpoint(munsell->V, value_m[1], value_m[2], munsell->C);
	munsell->H = Get_H_VALUE(munsell->H_str);
}

double clralgo_CarbonBlack_My(const struct clralgo_Spectra* spectra)
{
	return 0.0;
}

float clralgo_calculate_color_value_swl_at(const struct clralgo_Spectra* spectra, int wavelength, int isTransmittance)
{
	float (*f)(float) = isTransmittance ? absorbance : K_S;
	return f(clralgo_Spectra_get(spectra, wavelength));
}


float clralgo_pharmacopoeia_colorimetric(PharmacopoeiaTone_t tone, const clralgo_CIEXYZ* sample) {
	/**
	* 算法说明：该函数使用黄度和色号间的关系，过多项式拟合来实现的。
	*/

	// 1. 计算出黄度
	float YI = clralgo_calculate_yellowness_astm_e313_2005(
		clralgo_D65, clralgo_DEGREE_10, sample);
	if (clralgo_IS_NAN(YI)) {
		return clralgo_FLOAT_NAN;
	}

	double tone_number;
	switch (tone) {
	case PHARMACOPOEIA_TONE_GY:
		if (YI <= 15.56) {
			tone_number = 7E-05 * YI * YI * YI + 0.0017 * YI * YI + 0.3477 * YI - 0.0584;
		}
		else {
			tone_number = -0.0001663990 * YI * YI * YI + 0.0133148340 * YI * YI - 0.1711824880 * YI + 6.0700531610;
		}
		break;
	case PHARMACOPOEIA_TONE_YG:
		if (YI <= 14.65) {
			tone_number = 8E-05 * YI * YI * YI + 0.0009 * YI * YI + 0.3836 * YI - 0.0626;
		}
		else {
			tone_number = -0.0001839770 * YI * YI * YI + 0.0139914010 * YI * YI - 0.1654388100 * YI + 6.0034882550;
		}
		break;
	case PHARMACOPOEIA_TONE_Y:
		if (YI <= 16.93) {
			tone_number = 0.0017 * YI * YI + 0.3297 * YI - 0.0754;
		}
		else {
			tone_number = -0.0001085100 * YI * YI * YI + 0.0095439680 * YI * YI - 0.1195122640 * YI + 5.8239413920;
		}
		break;
	case PHARMACOPOEIA_TONE_OY:
		if (YI <= 15.16) {
			tone_number = 0.0001 * YI * YI * YI - 0.0014 * YI * YI + 0.39 * YI - 0.0987;
		}
		else {
			tone_number = -0.0001402960 * YI * YI * YI + 0.0112221010 * YI * YI - 0.1273839730 * YI + 5.8456435520;
		}
		break;
	case PHARMACOPOEIA_TONE_OR:
		if (YI <= 16.95) {
			tone_number = 0.0017 * YI * YI + 0.3297 * YI - 0.0752;
		}
		else {
			tone_number = -0.0000867107 * YI * YI * YI + 0.0075798410 * YI * YI - 0.0693105440 * YI + 5.4259223390;
		}
		break;
	case PHARMACOPOEIA_TONE_BR:
		if (YI <= 11.2) {
			tone_number = -0.0001 * YI * YI * YI + 0.0067 * YI * YI + 0.4826 * YI - 0.0488;
		}
		else {
			tone_number = -0.0002565610 * YI * YI * YI + 0.0146514610 * YI * YI - 0.0572266710 * YI + 5.1701489540;
		}
		break;
	default:
		return clralgo_FLOAT_NAN;
	}

	return (float)tone_number;
}

float clralgo_calculate_sayblot(const clralgo_CIEXYZ* xyz)
{
	float YI = clralgo_calculate_yellowness_astm_e313_2005(clralgo_C, clralgo_DEGREE_2, xyz);
	if (clralgo_IS_NAN(YI) || YI < -0.5f || YI > 25.0f) {
		return clralgo_FLOAT_NAN;
	}


	/**
	* 判断样品是否接迎参考白
	*/
	clralgo_CIELab lab;
	clralgo_xyz_to_lab(clralgo_C, clralgo_DEGREE_2, xyz, &lab);
	clralgo_CIEXYZ white_point_xyz = clralgo_get_white_point_of_illuminant(clralgo_DEGREE_2, clralgo_C);
	clralgo_CIELab white_point_lab;
	clralgo_xyz_to_lab(clralgo_C, clralgo_DEGREE_2, &white_point_xyz, &white_point_lab);
	if (clralgo_calculate_dEab(&lab, &white_point_lab) < 0.2) {
		return 30.0f;
	}

	double saybolt = -1.479307 * pow(10.0, -4) * pow(YI, 4)
		+ 7.805632 * pow(10.0, -3) * pow(YI, 3)
		- 5.003989 * pow(10.0, -2) * pow(YI, 2)
		- 3.234167 * YI
		+ 3.048802E+01;

	if (saybolt > 30.0) {
		saybolt = 30.0;
	}
	if (saybolt < -16.0) {
		saybolt = -16.0;
	}

	return (float)saybolt;
}


float clralgo_calculate_astm_d1500(const clralgo_CIEXYZ* xyz)
{
	double sumd = log10(xyz->X / 98.072) - log10(xyz->Y / 100.0) - log10(xyz->Z / 118.225);
	if (sumd < -0.2 || sumd > 10.0) {
		// 色品超标
		return clralgo_FLOAT_NAN;
	}

	// 判断是否接近参考白
	clralgo_CIELab lab;
	clralgo_xyz_to_lab(clralgo_C, clralgo_DEGREE_2, xyz, &lab);
	clralgo_CIEXYZ white_point_xyz = clralgo_get_white_point_of_illuminant(clralgo_DEGREE_2, clralgo_C);
	clralgo_CIELab white_point_lab;
	clralgo_xyz_to_lab(clralgo_C, clralgo_DEGREE_2, &white_point_xyz, &white_point_lab);
	if (clralgo_calculate_dEab(&lab, &white_point_lab) < 0.2) {
		return 0.0f;
	}

	return (float)(0.0518970 + 0.946984 * sumd);
}



CLRALGO_API float* clralgo_reflect_400_700_to_reflect_360_780(clralgo_Spectra* source, float* reflect)
{
	clralgo_WavelengthRange range = { 360, 10, (780 - 360) / 10 + 1 };
	//float array_sin_cof[] = { clralgo_PI / 16, clralgo_PI * 17 / 16, clralgo_PI / 8, clralgo_PI * 15 / 16, clralgo_PI * 7 / 8, 0, clralgo_PI * 17 / 16, clralgo_PI * 15 / 8 };
	float circumference = clralgo_PI;
	float array_sin_cof[] = { circumference * 15 / 16, circumference * 2 / 16, circumference * 17 / 16, circumference * 30 / 16,  0,circumference * 1 / 16, circumference * 13 / 16, circumference * 14 / 16 };
	float array_cof[8] = { 0.0f };
	float array_poly[2] = { -18 / 30000.0f, 0.005 + 1 / 3000.0f };
	float base_700_780 = 0.0f;
	float array_tmp_diff[8] = { 0 };
	int index_cnt = 0;
	struct clralgo_CIEXYZ xyz;
	float out_y_data_cp[9] = { 0.0f };
	clralgo_interpolate(&source->range, source->data, &range, reflect);
	// 计算360 370
	if (reflect[3] < 0)
	{
		reflect[3] = 0.0001f;
		reflect[2] = 0.0001f;
	}
	else if (reflect[2] < 0)
		reflect[2] = 0.0001f;
	reflect[1] = reflect[2] * 0.98;
	reflect[0] = reflect[1] * 0.98;

	clralgo_calculate_xyz(source, clralgo_D65, clralgo_DEGREE_10, &xyz);
	base_700_780 = array_poly[0] * xyz.Y + array_poly[1];
	for (int i = 0; i < (sizeof(array_sin_cof) / sizeof(float)); i++)
	{
		array_cof[i] = sin(array_sin_cof[i]);
		array_tmp_diff[i] = base_700_780 * array_cof[i];
	}
	out_y_data_cp[0] = reflect[34];
	out_y_data_cp[1] = (reflect[34] + reflect[35]) / 2;
	out_y_data_cp[2] = reflect[35];
	out_y_data_cp[3] = (reflect[35] + reflect[36]) / 2;
	out_y_data_cp[4] = reflect[36];
	out_y_data_cp[5] = (reflect[36] + (reflect[37] - reflect[36]) * 7 / 24);
	out_y_data_cp[6] = (reflect[36] + (reflect[37] - reflect[36]) * 13 / 24);
	out_y_data_cp[7] = (reflect[36] + (reflect[37] - reflect[36]) * 19 / 24);
	out_y_data_cp[8] = reflect[37];
	for (int i = 35; i < 43; i++)
	{
		//reflect[i] = reflect[34] * (array_tmp_diff[index_cnt++] + 1);
		reflect[i] = out_y_data_cp[i - 34] * (array_tmp_diff[index_cnt++] + 1);
	}
	return &reflect;
}

CLRALGO_API float clralgo_calculate_whiteness_gost_16873(const clralgo_CIELab* lab)
{
	return 100 - sqrt((100 - lab->L) * (100 - lab->L) + lab->a * lab->a + lab->b * lab->b);
}

//MI  输入的反射率量级应该是0-1  不是0-100

// MI = 100 * log10 ( 1 / F_680nm );
void calculate_MI(const struct clralgo_Spectra* spectra, float* MI)
{
	//printf("\r\n 反射率：%f,位置：%d \r\n", spectra->data[(680 - spectra->range.start) / spectra->range.interval], (680 - spectra->range.start) / spectra->range.interval);
	*MI = 100 * log10(1 / spectra->data[(680 - spectra->range.start) / spectra->range.interval]);
}

//EI

// EI = 100 * log10 ( 2 * F_680nm /  (F_550nm + F_560nm)  );
void calculate_EI(const struct clralgo_Spectra* spectra, float* EI)
{
	float value = 0;
	value = spectra->data[(680 - spectra->range.start) / spectra->range.interval] / (spectra->data[(550 - spectra->range.start) / spectra->range.interval] + spectra->data[(560 - spectra->range.start) / spectra->range.interval]);
	*EI = 100 * log10(2 * value);
}


