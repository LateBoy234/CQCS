/**
 * @File: DataTables.h
 * @Desc: 使用的参数表
 */
#ifndef DENSITYALGO_DENSITYDATATABLES_H_
#define DENSITYALGO_DENSITYDATATABLES_H_
#include <stdint.h>


#ifdef __cplusplus
extern "C" {
#endif

	enum DensityStatus;
	enum DensityColor;

	// 密度权重因子
	struct DensityWeightingFactors
	{
		uint16_t start;		// 起始波长
		uint16_t interval;	// 波长间隔
		uint16_t count;		// 波长个数
		float data[1];	// 权重因子，其大小为count
	};

	// 密度权重因子表
	struct DensityWeightingFactorsTable
	{
		const DensityWeightingFactors *c; // C权重因子
		const DensityWeightingFactors *m; // M权重因子
		const DensityWeightingFactors *y; // Y权重因子
	};

	// 获取权重系数
	const struct DensityWeightingFactors* density_get_weighting_factors(enum DensityStatus type, enum DensityColor color);
	// 获取权重表
	const struct DensityWeightingFactorsTable* density_get_weighting_factors_table(enum DensityStatus status);

#ifdef __cplusplus
}
#endif

#endif // DENSITYALGO_DENSITYDATATABLES_H_
