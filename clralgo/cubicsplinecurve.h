#ifndef CUBICSPLINECURVE_H
#define CUBICSPLINECURVE_H

#include "clralgo_dll_defines.h"

#ifdef __cplusplus
extern "C" {
#endif

typedef struct CubicSplineCurve
{
    int n; // 节点个数
    double *x; // 用于拟合曲线的x值
    double *y; // 用于拟合曲线的y值
    double *m; // 拟合得到的系数
}CubicSplineCurve;


/* 销毁CubicSplineCurve对象，释放其内存
 * [ref] curve : 由CubicSplineCurve_Construct创建的对象
 */
CLRALGO_API void CubicSplineCurve_Destruct(CubicSplineCurve *curve);



/* 拟合曲线
 * [in] n : 节点数
 * [in] x : 节点的x值
 * [in] y : 节点的y值
 * [in] boundType : 边界条件，1表示第一类边界条件，2表示第二类边界条件
 * [in] b1 : 左边界的值
 * [in] b2 : 右边界的值
 * return : 如果成功，返回CubicSplineCurve指针，该指针需要使用CubicSplineCurve_Destruct清除；否则返回NULL
 *
 * 注意：如果boundType为2，b1、b2都为0，表示自然边界
 */
CLRALGO_API CubicSplineCurve* CubicSplineCurve_Simulate(
        int n, const double *x, const double *y, int boundType, double b1, double b2);

/* 根据给定的x值，返回对应的y值
 * [in] curve : 有效的曲线对象
 * [in] x : 输入的x值
 * return : 返回x对应的y值
 */
CLRALGO_API double CubicSplineCurve_GetItem(CubicSplineCurve *curve, double x);


/* 拟合曲线，使用Float
 * [in] n : 节点数
 * [in] x : 节点的x值
 * [in] y : 节点的y值
 * [in] boundType : 边界条件，1表示第一类边界条件，2表示第二类边界条件
 * [in] b1 : 左边界的值
 * [in] b2 : 右边界的值
 * return : 如果成功，返回CubicSplineCurve指针，该指针需要使用CubicSplineCurve_Destruct清除；否则返回NULL
 *
 * 注意：如果boundType为2，b1、b2都为0，表示自然边界
 */
CLRALGO_API CubicSplineCurve* CubicSplineCurve_Simulate_f(int n, const float *x, const float *y, int boundType, float b1, float b2);

/* 根据给定的x值，返回对应的y值，使用Float型数据
 * [in] curve : 有效的曲线对象
 * [in] x : 输入的x值
 * return : 返回x对应的y值
 */
CLRALGO_API float CubicSplineCurve_GetItem_f(CubicSplineCurve *curve, float x);

#ifdef __cplusplus
}
#endif

#endif // CUBICSPLINECURVE_H
