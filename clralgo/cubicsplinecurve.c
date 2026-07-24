#include <memory.h>
#include <math.h>
#include <stdlib.h>
#include "cubicsplinecurve.h"

#ifdef __cplusplus
extern "C" {
#endif

#define PRINT_ARRAY(array, n) \
    do {\
        int i; \
        printf("\n%s:", #array); \
        for (i = 0; i < n; i++) {\
            if (i%10 == 0) \
                printf("\n\t"); \
            printf("%.2f, ", (double)array[i]); \
        } \
    } while(0)

/* 创建一个CubicSplineCurve曲线
 * return: 返回一个CubicSplineCurve曲线，所有参数都是初始值
 */
CubicSplineCurve *CubicSplineCurve_Construct()
{
    CubicSplineCurve *curve = (CubicSplineCurve *) malloc(sizeof(CubicSplineCurve));
    curve->n = 0;
    curve->x = curve->y = curve->m = NULL;
    return curve;
}


/* 清理curve的数据，但不释放curve结构
 * [ref] curve : 要清理的曲线对象
 */
void CubicSplineCurve_Clear(CubicSplineCurve *curve)
{
    if (curve) {
        curve->n = 0;
        if (curve->x) {
            free(curve->x);
            curve->x = NULL;
        }
        if (curve->y) {
            free(curve->y);
            curve->y = NULL;
        }
        if (curve->m) {
            free(curve->m);
            curve->m = NULL;
        }
    }
}

/* 销毁CubicSplineCurve对象
 * [ref] curve : 由CubicSplineCurve_Construct创建的对象
 */
void CubicSplineCurve_Destruct(CubicSplineCurve *curve)
{
    if (curve) {
        CubicSplineCurve_Clear(curve);
        free(curve);
    }
}

/* 判断曲线是否有效
 * [in] curve : 要判断是否有效的曲线
 * return : 如果曲线有效返回非零，否则返回零值
 */
int CubicSplineCurve_IsValid(CubicSplineCurve *curve)
{
    return curve && curve->n >= 3 && curve->x && curve->y && curve->m;
}


static double * resolve(int n, double *b, double *a, double *c, double *d)
{
    int i;
    double *l = NULL;
    double *u = NULL;
    double *y = NULL;
    double *x = NULL;

    l = (double*)malloc(sizeof(double) * n);
    u = (double*)malloc(sizeof(double) * n);
    y = (double*)malloc(sizeof(double) * n);

    if (l == NULL || u == NULL || y == NULL) {
        free(l);
        free(u);
        free(y);
        return NULL;
    }

    l[0] = a[0];
    u[0] = c[0] / l[0];
    for (i = 1; i < n; i++) {
        l[i] = a[i] - b[i] * u[i-1];
        u[i] = c[i] / l[i];
    }

    y[0] = d[0] / l[0];
    for (i = 1; i < n; i++) {
        y[i] = (d[i] - b[i]*y[i-1]) / l[i];
    }

    x = l;
    l = NULL;
    x[n-1] = y[n-1];
    for (i = n-2; i >=0; i--) {
        x[i] = y[i] - u[i]*x[i+1];
    }

    //free(l);
    free(u);
    free(y);
    return x;
}



/* 拟合曲线
 * [out] curve : 在拟合成功时返回拟合好的曲线
 * [in] n : 节点数
 * [in] x : 节点的x值
 * [in] y : 节点的y值
 * [in] boundType : 边界条件，1表示第一类边界条件，2表示第二类边界条件
 * [in] b1 : 左边界的值
 * [in] b2 : 右边界的值
 * return : 如果成功，CubicSplineCurve的指针；失败返回NULL
 *
 * 注意：如果boundType为2，b1、b2都为0，表示自然边界
 */
CubicSplineCurve* CubicSplineCurve_Simulate(int n,
                              const double *x,
                              const double *y,
                              int boundType, double b1, double b2)
{
    int i;
    double *h = NULL;
    double *u = NULL;
    double *v = NULL;
    double *d = NULL;
    double *m = NULL;
    CubicSplineCurve *curve = NULL;

    if (!curve || n < 3 || !x || !y || (boundType != 1 && boundType != 2)) {
        return 0;
    }

    curve = CubicSplineCurve_Construct();
    if (curve == NULL) {
        return NULL;
    }

    h = (double*)malloc(sizeof(double) * n);
    u = (double*)malloc(sizeof(double) * n);
    v = (double*)malloc(sizeof(double) * n);
    d = (double*)malloc(sizeof(double) * n);
    if (!h || !u || !v || !d) {
        free(h);
        free(u);
        free(v);
        free(d);

        CubicSplineCurve_Destruct(curve);
        return NULL;
    }

    for (i = 0; i < n - 1; i++) {
        h[i] = x[i+1] - x[i];
    }
    for (i = 1; i < n - 1; i++) {
        u[i] = h[i-1] / (h[i-1] + h[i]);
        v[i] = 1.0 - u[i];
        d[i] = 6.0 * ((y[i+1] - y[i]) / h[i] -
                (y[i] - y[i-1]) / h[i-1]) / (h[i-1] + h[i]);
    }
    if (boundType == 1) {  // 第一类边界条件
        v[0] = 1.0;
        u[n-1] = 1.0;
        d[0] = 6.0 * ((y[1] - y[0]) / h[0] - b1) / h[0];
        d[n-1] = 6.0 * (b2 - (y[n] - y[n-1]) / h[n-1]) / h[n-1];
    } else { // 第二类边界条件
        v[0] = 0.0;
        u[n-1] = 0.0;
        d[0] = 2.0 * b1;
        d[n-1] = 2.0 * b2;
    }

    for (i = 0; i < n; i++) {
        h[i] = 2.0;
    }
    m = resolve(n, u, h, v, d);
    if (m == NULL) {
        free(h);
        free(u);
        free(v);
        free(d);

        CubicSplineCurve_Destruct(curve);
        return NULL;
    }

	curve->n = n;
	curve->x = u;
	u = NULL;
	curve->y = v;
	v = NULL;
	curve->m = m;

	memcpy(curve->x, x, sizeof(double) * n);
	memcpy(curve->y, y, sizeof(double) * n);


    free(h);
    free(d);
    return curve;
}

/* 根据给定的x值，返回对应的y值
 * [in] curve : 有效的曲线对象
 * [in] x : 输入的x值
 * return : 返回x对应的y值
 */
double CubicSplineCurve_GetItem(CubicSplineCurve *curve, double x)
{
    /*
    if (!CubicSplineCurve_IsValid(curve)) {
        return 0.0;
    }
    */
    int i = 0;
    double y, hi, xi, xi_1, Ai, Bi;

    if (x < curve->x[0]) {
        i = 0;
    } else if (x > curve->x[curve->n - 1]) {
        i = curve->n - 2;
    } else {
        for (i = 0; i < curve->n - 2; i++) {
            if (x >= curve->x[i] && x <= curve->x[i+1])
                break;
        }
    }

    hi = curve->x[i+1] - curve->x[i];
    xi_1 = curve->x[i+1] - x;
    xi = x - curve->x[i];
    y = pow(xi_1, 3.0) * curve->m[i] / (6 * hi);
    y += (pow(xi, 3.0) * curve->m[i + 1] / (6 * hi));
    Ai = (curve->y[i+1] - curve->y[i]) / hi - (curve->m[i+1] - curve->m[i]) * hi / 6.0;
    y += Ai * x;
    Bi = curve->y[i+1] - curve->m[i+1] * hi * hi / 6.0 - Ai * curve->x[i+1];
    y += Bi;
    return y;
}

/* 拟合曲线
 * [out] curve : 在拟合成功时返回拟合好的曲线
 * [in] n : 节点数
 * [in] x : 节点的x值
 * [in] y : 节点的y值
 * [in] boundType : 边界条件，1表示第一类边界条件，2表示第二类边界条件
 * [in] b1 : 左边界的值
 * [in] b2 : 右边界的值
 * return : 如果成功，CubicSplineCurve的指针；失败返回NULL
 *
 * 注意：如果boundType为2，b1、b2都为0，表示自然边界
 */
CubicSplineCurve* CubicSplineCurve_Simulate_f(int n,
                              const float *x,
                              const float *y,
                              int boundType, float b1, float b2)
{
    int i;
    double *h = NULL;
    double *u = NULL;
    double *v = NULL;
    double *d = NULL;
    double *m = NULL;
    CubicSplineCurve *curve = NULL;

	//if (!curve || n < 3 || !x || !y || (boundType != 1 && boundType != 2)) {
	if (n < 3 || !x || !y || (boundType != 1 && boundType != 2)) {
		return 0;
	}

    curve = CubicSplineCurve_Construct();
    if (curve == NULL) {
        return NULL;
    }

    h = (double*)malloc(sizeof(double) * n);
    u = (double*)malloc(sizeof(double) * n);
    v = (double*)malloc(sizeof(double) * n);
    d = (double*)malloc(sizeof(double) * n);
    if (!h || !u || !v || !d) {
        free(h);
        free(u);
        free(v);
        free(d);

        CubicSplineCurve_Destruct(curve);
        return NULL;
    }

    for (i = 0; i < n - 1; i++) {
        h[i] = x[i+1] - x[i];
    }
	for (i = 1; i < n - 1; i++) {
		u[i] = h[i - 1] / (h[i - 1] + h[i]);
		v[i] = 1.0f - u[i];
		d[i] = 6.0f * ((y[i + 1] - y[i]) / h[i] -
			(y[i] - y[i - 1]) / h[i - 1]) / (h[i - 1] + h[i]);
	}
	if (boundType == 1) {  // 第一类边界条件
		v[0] = 1.0f;
		u[n - 1] = 1.0f;
		d[0] = 6.0f * ((y[1] - y[0]) / h[0] - b1) / h[0];
		d[n - 1] = 6.0f * (b2 - (y[n] - y[n - 1]) / h[n - 1]) / h[n - 1];
	}
	else { // 第二类边界条件
		v[0] = 0.0f;
		u[n - 1] = 0.0f;
		d[0] = 2.0f * b1;
		d[n - 1] = 2.0f * b2;
	}

	for (i = 0; i < n; i++) {
		h[i] = 2.0f;
	}
	m = resolve(n, u, h, v, d);
	if (m == NULL) {
		free(h);
		free(u);
		free(v);
		free(d);

		CubicSplineCurve_Destruct(curve);
		return NULL;
	}

	curve->n = n;
	curve->x = u;
	u = NULL;
	curve->y = v;
	v = NULL;
	curve->m = m;

	for (i = 0; i < n; i++) {
		curve->x[i] = (double)x[i];
		curve->y[i] = (double)y[i];
	}


    free(h);
    free(d);
    return curve;
}

/* 根据给定的x值，返回对应的y值
 * [in] curve : 有效的曲线对象
 * [in] x : 输入的x值
 * return : 返回x对应的y值
 */
float CubicSplineCurve_GetItem_f(CubicSplineCurve *curve, float x)
{
   return (float)CubicSplineCurve_GetItem(curve, x);
}
#ifdef __cplusplus
}
#endif
