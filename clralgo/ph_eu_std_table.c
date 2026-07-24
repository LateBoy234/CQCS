#include "ph_eu_std_table.h"
#include "tristimulus_weighting_factors.h"

const clralgo_CIELab s_ph_eu_std_table[PH_EU_HUE_COUNT][PH_EU_SCALE_COUNT] = {
    /** B */
    {
        {86.96f, 2.17f,  31.2f},
        {90.95f, -0.48f, 24.66f},
        {93.17f, -1.37f, 20.64f},
        {95.39f, -2.05f, 15.81f},
        {97.65f, -2.17f, 10.07f},
        {99.14f, -1.32f, 4.78f},
        {99.68f, -0.78f, 2.64f},
        {99.61f, -0.47f, 1.57f},
        {99.81f, -0.32f, 1.06f},
    },
    /** BY */
    {
        {93.05f, -4.64f, 38.74f},
        {94.94f, -5.47f, 32.75f},
        {96.35f, -5.85f, 25.84f},
        {98.43f, -4.72f, 15.81f},
        {99.28f, -3.3f,  9.51f},
        {99.93f, -1.66f, 4.42f},
        {99.86f, -0.93f, 2.38f},
        {0,0,0},
        {0,0,0},
    },
    /** Y */
    {
        {95.18f, -8.62f, 44.18f},
        {96.05f, -8.65f, 36.56f},
        {97.27f, -7.58f, 26.32f},
        {98.81f, -5.36f, 15.25f},
        {99.34f, -3.3f,  8.5f},
        {99.6f,  -1.85f, 4.33f},
        {99.95f, -1.00f,    2.31f},
        {0,0,0},
        {0,0,0},
    },
    /** GY */
    {
        {97.58f, -13.33f, 36.7f},
        {98.61f, -10.25f, 24.87f},
        {99.25f, -7.29f,  16.26f},
        {99.53f, -4.96f,  10.55f},
        {99.58f, -3.3f,   6.76f},
        {99.73f, -1.75f,  3.47f},
        {99.99f, -0.92f,  1.82f},
        {0,0,0},
        {0,0,0},
    },
    /** R */
    {
        {90.05f, 8.2f,  29.37f},
        {95.82f, 2.45f, 13.17f},
        {97.28f, 1.31f, 8.27f},
        {97.92f, 0.9f,  6.34f},
        {98.62f, 0.51f, 4.2f},
        {99.28f, 0.25f, 2.17f},
        {99.6f,  0.12f, 0.87f},
        {0,0,0},
        {0,0,0},
    },
};

float ph_eu_calculate_color_scale(PH_EU_Hue_t hue, const clralgo_CIEXYZ* sample)
{
    /**
    * 判断样品是否是清水
    */
    clralgo_CIELab lab;
    clralgo_xyz_to_lab(clralgo_D65, clralgo_DEGREE_10, sample, &lab);
    clralgo_CIEXYZ white_point_xyz = clralgo_get_white_point_of_illuminant(clralgo_DEGREE_10, clralgo_D65);
    clralgo_CIELab white_point_lab;
    clralgo_xyz_to_lab(clralgo_D65, clralgo_DEGREE_10, &white_point_xyz, &white_point_lab);
    if (clralgo_calculate_dEab(&lab, &white_point_lab) < 0.2) {
        return 0;
    }
    // 1. 计算出黄度
    float YI = clralgo_calculate_yellowness_astm_e313_2005(
        clralgo_D65, clralgo_DEGREE_10, sample);
    if (clralgo_IS_NAN(YI)) {
        return clralgo_FLOAT_NAN;
    }

    double hue_number;
    switch (hue) {
    case PH_EU_HUE_B:
        if (YI <= 7.73)
        {
            hue_number = -0.0309 * YI * YI * YI + 0.5069 * YI * YI - 2.9414 * YI + 12.717;
        }
        else
        {
            hue_number = 0.0003 * YI * YI - 0.1255 * YI + 6.9842;
        }
        break;
    case PH_EU_HUE_BY:
        if (YI <= 23.84)
        {
            hue_number = 0.0046 * YI * YI - 0.2668 * YI + 7.7989;
        }
        else
        {
            hue_number = -0.001 * YI * YI - 0.0063 * YI + 4.7211;
        }

        break;
    case PH_EU_HUE_Y:
        if (YI <= 22.41)
        {
            hue_number = 0.0065 * YI * YI - 0.3218 * YI + 7.9645;
        }
        else
        {
            hue_number = -0.0007 * YI * YI - 0.0197 * YI + 4.7806;
        }
        break;
    case PH_EU_HUE_GY:
        if (YI <= 22.53)
        {
            hue_number = 0.0059 * YI * YI - 0.3431 * YI + 7.7489;
        }
        else
        {
            hue_number = 0.0008 * YI * YI - 0.1362 * YI + 5.6633;
        }
        break;
    case PH_EU_HUE_R:
        if (YI <= 25.48)
        {
            hue_number = -0.03156 * YI + 2.8043;
        }
        else
        {
            hue_number = 0.0062 * YI * YI - 0.3769 * YI + 7.5779;
        }
        break;
    default:
        return clralgo_FLOAT_NAN;
    }


    if (hue_number < -0.2f) {
        hue_number = 0.0f;
    }
    return hue_number >= 0 ? (float)hue_number : clralgo_FLOAT_NAN;
}

int ph_en_find_hue(const clralgo_CIEXYZ* sample)
{
    clralgo_CIELab lab;
    clralgo_xyz_to_lab(clralgo_D65, clralgo_DEGREE_10, sample, &lab);
    return ph_eu_find_hue_by_lab(&lab);
}

int ph_eu_find_hue_by_lab(const clralgo_CIELab* sample)
{
    int hue_min = -1;
    // PH_CP_ColorScale_t scale_min = 0;

    float de_min = 20.0f * 20.0f;
    for (PH_EU_Hue_t hue = 0; hue < PH_EU_HUE_COUNT; ++hue) {
        for (PH_EU_ColorScale_t scale = 0; scale < PH_EU_SCALE_COUNT; ++scale) {
            float dL = sample->L - s_ph_eu_std_table[hue][scale].L;
            float da = sample->a - s_ph_eu_std_table[hue][scale].a;
            float db = sample->b - s_ph_eu_std_table[hue][scale].b;
            float de = dL * dL + da * da + db * db;
            if (de < de_min) {
                hue_min = hue;
                de_min = de;
                // scale_min = scale;
            }
        }
    }
    return hue_min;
}


