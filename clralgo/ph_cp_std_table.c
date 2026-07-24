
#include "clralgo.h"
#include "ph_cp_std_table.h"
#include "tristimulus_weighting_factors.h"

const clralgo_CIELab s_ph_cp_std_table[PH_CP_HUE_COUNT][PH_CP_SCALE_COUNT] = {
    /** GY */
    {
        {95.74f, -0.54f,  1.05f},
        {95.66f, -0.92f,  1.95f},
        {95.44f, -1.68f,  3.70f},
        {95.23f, -2.40f,  5.45f},
        {95.02f, -3.09f,  7.12f},
        {94.81f, -3.75f,  8.71f},
        {94.59f, -4.37f, 10.30f},
        {94.02f, -6.17f, 14.87f},
        {93.43f, -7.74f, 19.07f},
        {92.86f, -9.25f, 23.27f},
        {92.00f, -11.5f, 29.89f},
    },
    /** YG */
    {
        {95.76f, -0.39f,  0.92f},
        {95.69f, -0.64f,  1.71f},
        {95.48f, -1.12f,  3.24f},
        {95.26f, -1.57f,  4.76f},
        {95.10f, -2.01f,  6.25f},
        {94.91f, -2.42f,  7.69f},
        {94.74f, -2.82f,  9.10f},
        {94.22f, -3.90f, 13.18f},
        {93.75f, -4.84f, 16.96f},
        {93.24f, -5.70f, 20.64f},
        {92.50f, -6.99f, 26.61f},
    },
    /** Y */
    {
        {95.59f, -0.27f,  1.02f},
        {95.68f, -0.39f,  1.83f},
        {95.44f, -0.58f,  3.47f},
        {95.24f, -0.78f,  5.05f},
        {94.97f, -0.91f,  6.63f},
        {94.81f, -1.06f,  8.15f},
        {94.62f, -1.16f,  9.67f},
        {93.99f, -1.42f, 13.88f},
        {93.38f, -1.51f, 18.09f},
        {92.83f, -1.61f, 21.80f},
        {91.89f, -1.55f, 28.13f},
    },
    /** OY */
    {
        {95.74f, -0.08f, 0.82f},
        {95.50f,  0.03f, 1.49f},
        {95.07f,  0.19f, 2.75f},
        {94.74f,  0.40f, 4.06f},
        {94.33f,  0.57f, 5.27f},
        {93.94f,  0.83f, 6.50f},
        {93.57f,  0.99f, 7.64f},
        {92.49f,  1.70f, 11.07f},
        {91.41f,  2.38f, 14.21f},
        {90.39f,  3.05f, 17.22f},
        {88.79f,  4.23f, 22.27f},
    },
    /** OR */
    {
        {95.95f, -0.18f, 0.33f},
        {95.50f, 0.16f,  1.60f},
        {95.07f, 0.47f,  3.03f},
        {94.68f, 0.76f,  4.37f},
        {94.29f, 1.11f,  5.75f},
        {93.84f, 1.43f,  7.01f},
        {93.49f, 1.79f,  8.29f},
        {92.32f, 2.82f, 11.86f},
        {91.24f, 3.88f, 15.37f},
        {90.20f, 4.93f, 18.62f},
        {88.58f, 6.63f, 24.00f},
    },
    /** BR */
    {
        {95.55f, 0.22f, 0.50f},
        {95.19f, 0.58f, 0.87f},
        {94.42f, 1.27f, 1.59f},
        {93.65f, 1.93f, 2.25f},
        {92.84f, 2.57f, 2.92f},
        {92.12f, 3.19f, 3.55f},
        {91.40f, 3.79f, 4.17f},
        {89.39f, 5.45f, 5.88f},
        {87.47f, 7.05f, 7.54f},
        {85.59f, 8.54f, 9.03f},
        {82.54f, 10.91f, 11.32f},
    },
};


float ph_cp_calculate_color_scale(PH_CP_Hue_t hue, const clralgo_CIEXYZ *sample) {
    /**
    * 算法说明：该函数使用黄度和色号间的关系，过多项式拟合来实现的。
    */
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
    case PH_CP_HUE_GY:
        if (YI <= 15.56) {
            hue_number = 7E-05 * YI * YI * YI + 0.0017 * YI * YI + 0.3477 * YI - 0.0584;
        }
        else if (YI > 45) {
            return clralgo_FLOAT_NAN;
        }
        else {
            hue_number = -0.0001663990 * YI * YI * YI + 0.0133148340 * YI * YI - 0.1711824880 * YI + 6.0700531610;
        }
        break;
    case PH_CP_HUE_YG:
        if (YI <= 14.65) {
            hue_number = 8E-05 * YI * YI * YI + 0.0009 * YI * YI + 0.3836 * YI - 0.0626;
        }
        else if (YI > 45) {
            return clralgo_FLOAT_NAN;
        }
        else {
            hue_number = -0.0001839770 * YI * YI * YI + 0.0139914010 * YI * YI - 0.1654388100 * YI + 6.0034882550;
        }
        break;
    case PH_CP_HUE_Y:
        if (YI <= 16.93) {
            hue_number = 0.0017 * YI * YI + 0.3297 * YI - 0.0754;
        }
        else if (YI > 50) {
            return clralgo_FLOAT_NAN;
        }
        else {
            hue_number = -0.0001085100 * YI * YI * YI + 0.0095439680 * YI * YI - 0.1195122640 * YI + 5.8239413920;
        }
        break;
    case PH_CP_HUE_OY:
        if (YI <= 15.16) {
            hue_number = 0.0001 * YI * YI * YI - 0.0014 * YI * YI + 0.39 * YI - 0.0987;
        }
        else if (YI > 45) {
            return clralgo_FLOAT_NAN;
        }
        else {
            hue_number = -0.0001402960 * YI * YI * YI + 0.0112221010 * YI * YI - 0.1273839730 * YI + 5.8456435520;
        }
        break;
    case PH_CP_HUE_OR:
        if (YI <= 16.95) {
            hue_number = 0.0017 * YI * YI + 0.3297 * YI - 0.0752;
        }
        else if (YI > 50) {
            return clralgo_FLOAT_NAN;
        }
        else {
            hue_number = -0.0000867107 * YI * YI * YI + 0.0075798410 * YI * YI - 0.0693105440 * YI + 5.4259223390;
        }
        break;
    case PH_CP_HUE_BR:
        if (YI <= 11.2) {
            hue_number = -0.0001 * YI * YI * YI + 0.0067 * YI * YI + 0.4826 * YI - 0.0488;
        }
        else if (YI > 35) {
            return clralgo_FLOAT_NAN;
        }
        else {
            hue_number = -0.0002565610 * YI * YI * YI + 0.0146514610 * YI * YI - 0.0572266710 * YI + 5.1701489540;
        }
        break;
    default:
        return clralgo_FLOAT_NAN;
    }

    return (hue_number >= 0 && hue_number < 11) ? (float)hue_number : clralgo_FLOAT_NAN;
    //if (hue_number < -0.2f) { hue_number = 0.0f; }
    if (hue_number < 0) { hue_number = 0.0f; }
    return hue_number >= 0 ? (float)hue_number : clralgo_FLOAT_NAN;
}



int ph_cp_find_hue(const clralgo_CIEXYZ *sample)
{
    clralgo_CIELab lab;
    clralgo_xyz_to_lab(clralgo_D65, clralgo_DEGREE_10, sample, &lab);
    return ph_cp_find_hue_by_lab(&lab);
}

int ph_cp_find_hue_by_lab(const clralgo_CIELab* sample)
{
    int hue_min = -1;
    // PH_CP_ColorScale_t scale_min = 0;

    float de_min = 20.0f * 20.0f;
    for (PH_CP_Hue_t hue = 0; hue < PH_CP_HUE_COUNT; ++hue) {
        for (PH_CP_ColorScale_t scale = 0; scale < PH_CP_SCALE_COUNT; ++scale) {
            float dL = sample->L - s_ph_cp_std_table[hue][scale].L;
            float da = sample->a - s_ph_cp_std_table[hue][scale].a;
            float db = sample->b - s_ph_cp_std_table[hue][scale].b;
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