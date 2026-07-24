#include "DensityDataTables.h"
#include "DensityAlgo.h"
#include <cassert>
#include <stdlib.h>

struct _WeightingFactors
{
	uint16_t start; // 起始波长
	uint16_t interval; // 波长间隔
	uint16_t count; // 波长个数
	float factors[44]; // 青色权重因子
};

// K weighting factors
static const struct _WeightingFactors weighting_factors_k = {
	340,
	10,
	44,
	{
		0, 0, 0, 0, 0, 0,
		0.000540247, 0.001983789, 0.007789673, 0.026545221, 0.061234187,
		0.116619163, 0.210438607, 0.36177262, 0.622083468, 1.040264009,
		1.793455422, 3.082286007, 4.774360028, 6.32712001, 7.605407135,
		8.574650671, 9.229250882, 9.464774686, 9.234753185, 8.54752619,
		7.552786028, 6.361415424, 5.075481173, 3.707611767, 2.564370547,
		1.637890203, 0.972989647, 0.530622376, 0.292395005, 0.14616105,
		0.075435595, 0, 0, 0, 0,
		0, 0, 0,
	}
};

/*************ISO 5 STATUS T*********************************/

// status T Y weighting factors
static const struct _WeightingFactors status_t_weighting_factors_y = {
	340,
	10,
	44,
	{
		0.0003, 0.0014, 0.0028, 0.0125, 0.0385, 0.21, 0.8323, 2.4537, 5.5297, 8.5297,
		11.448, 13.261, 14.039, 13.648, 11.932, 9.1433, 5.602, 2.5683, 0.7408, 0.0055,
		0.0001, 0.0013, 0.0003, 0.0001, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0,
	}
};

// status T M weighting factors
static const struct _WeightingFactors status_t_weighting_factors_m = {
	340,
	10,
	44,
	{
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -0.0013, -0.0096, 0.1727, 0.964, 5.4663, 12.8804, 17.5926, 18.9941, 16.7862,
		12.5972, 8.0105, 4.2078, 1.7152, 0.4815, 0.1252, 0.0163, 0.0009, 0.0001, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0,
	}
};

// status T C weighting factors
static const struct _WeightingFactors status_t_weighting_factors_c = {
	340,
	10,
	44,
	{
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0.0001, 0.0004, 0.0027, -0.0419, -0.3728, 11.4921,
		30.713, 27.2826, 17.3613, 8.0369, 3.1981, 1.5211, 0.4947, 0.1529, 0.0911, 0.0479, 0.016, 0.0031, 0.0005, 0.0001,
		0,
		0, 0, 0,

	}
};

/*************ISO 5 STATUS E*********************************/

// status E C weighting factors
static const struct _WeightingFactors status_e_weighting_factors_y = {
	340,
	10,
	44,
	{
		0, 0, 0, -0.003, 0.039, 0.536, 2.422, 5.837, 11.2, 11.793, 18.702, 17.464, 14.343, 8.886, 3.517, 1.106, 0.159,
		-0.001, 0.001, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
	}
};


// status E M weighting factors
static const struct _WeightingFactors status_e_weighting_factors_m = {
	340,
	10,
	44,
	{
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -0.001, -0.01, 0.172, 0.964, 5.466, 12.88, 17.593, 18.994, 16.786, 12.597,
		8.011, 4.208, 1.715, 0.482, 0.125, 0.016, 0.001, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
	}
};


// status E Y weighting factors
static const struct _WeightingFactors status_e_weighting_factors_c = {
	340,
	10,
	44,
	{
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -0.042, -0.373, 11.492, 30.713, 27.283,
		17.361, 8.037, 3.198, 1.521, 0.495, 0.153, 0.091, 0.048, 0.016, 0.003, 0.001, 0, 0, 0, 0, 0,

	}
};


/*************ISO 5 STATUS I*********************************/

// status I C weighting factors
static const struct _WeightingFactors status_i_weighting_factors_y = {
	340,
	10,
	44,
	{
		0, 0, 0, 0, 0, 0, -0.006, -1.85, 15.673, 72.367, 15.373, -1.85, -0.006, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,

	}
};


// status I M weighting factors
static const struct _WeightingFactors status_i_weighting_factors_m = {
	340,
	10,
	44,
	{
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -0.288, -1.282, 51.569, 51.569, -1.282, -0.288, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
	}
};


// status I Y weighting factors
static const struct _WeightingFactors status_i_weighting_factors_c = {
	340,
	10,
	44,
	{
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -0.288, -1.282, 51.569, 51.569,
		-1.282, -0.288, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,

	}
};

/*************ISO 5 STATUS A*********************************/

// status A C weighting factors
static const struct _WeightingFactors status_a_weighting_factors_y = {
	340,
	10,
	44,
	{
		0, 0, 0, 0, 0, 0, -0.003, -0.373, 2.763, 20.848, 32.395, 26.684, 13.711, 3.723, 0.275, -0.022, -0.002, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
	}
};


// status A M weighting factors
static const struct _WeightingFactors status_a_weighting_factors_m = {
	340,
	10,
	44,
	{
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -0.012, -0.256, 2.887, 19.135, 31.434, 25.84, 14.144, 5.365, 1.296,
		0.166, 0.004, -0.002, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
	}
};


// status A Y weighting factors
static const struct _WeightingFactors status_a_weighting_factors_c = {
	340,
	10,
	44,
	{
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -0.108, -0.3, 16.166, 33.797, 25.312,
		13.862, 6.532, 2.723, 1.185, 0.498, 0.201, 0.081, 0.0031, 0.012, 0.005, 0.002, 0.001, 0, 0,
	}
};


static const DensityWeightingFactorsTable weighting_factors_tables[] = {
	// status T
	{
		(const DensityWeightingFactors *)&status_t_weighting_factors_c,
		(const DensityWeightingFactors *)&status_t_weighting_factors_m,
		(const DensityWeightingFactors *)&status_t_weighting_factors_y
	},
	// status E
	{
		(const DensityWeightingFactors *)&status_e_weighting_factors_c,
		(const DensityWeightingFactors *)&status_e_weighting_factors_m,
		(const DensityWeightingFactors *)&status_e_weighting_factors_y
	},
	// status I
	{
		(const DensityWeightingFactors *)&status_i_weighting_factors_c,
		(const DensityWeightingFactors *)&status_i_weighting_factors_m,
		(const DensityWeightingFactors *)&status_i_weighting_factors_y
	},
	// status A
	{
		(const DensityWeightingFactors *)&status_a_weighting_factors_c,
		(const DensityWeightingFactors *)&status_a_weighting_factors_m,
		(const DensityWeightingFactors *)&status_a_weighting_factors_y
	},
};


// 获取权重表
const struct DensityWeightingFactorsTable* density_get_weighting_factors_table(enum DensityStatus status)
{
	assert(status < sizeof(weighting_factors_tables)/sizeof(weighting_factors_tables[0]));
	return &weighting_factors_tables[status];
}

// 获取权重系收
const struct DensityWeightingFactors* density_get_weighting_factors(enum DensityStatus status, enum DensityColor color)
{
	const struct DensityWeightingFactorsTable* table;

	if (color == DENSITY_K)
	{
		return (const DensityWeightingFactors *)&weighting_factors_k;
	}

	table = density_get_weighting_factors_table(status);

	switch (color)
	{
	case DENSITY_C: return table->c;
	case DENSITY_M: return table->m;
	case DENSITY_Y: return table->y;
	default:
		assert(false);
		return NULL;
	}
}
