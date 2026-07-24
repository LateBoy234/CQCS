//////////////////////////////////////////////////////////////////////////
// @File: util.h
// @Author: Zhong Jianfeng
// @Date: 2017/9/29
// @Description: 一些有用的函数
//////////////////////////////////////////////////////////////////////////
#ifndef TNH_CLRALGO_UTIL_H_
#define  TNH_CLRALGO_UTIL_H_
#include <math.h>

#ifdef __cplusplus
extern "C" {
#endif

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
    double result = (int) x;
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

#ifdef __cplusplus
}
#endif

#endif // TNH_CLRALGO_UTIL_H_
