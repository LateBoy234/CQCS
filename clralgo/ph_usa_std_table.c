
#include "ph_usa_std_table.h"

const clralgo_CIELab s_ph_usa_std_table[PH_USA_HUE_COUNT] = {
 
        {98.16f, -2.64f, 9.77f}, //ma
        {95.18f, -4.62f, 21.25f}, //mb
        {98.03f, -4.87f, 15.34f}, //mc
        {95.48f, -2.53f, 13.19f}, //md
        {93.92f, -4.83f, 28.53f}, //me
        {95.63f, -5.1f, 30.85f}, //mf
        {93.35f, -2.32f, 29.79f}, //mg
        {96.43f, -8.54f, 35.83f}, //mh
        {93.62f, -7.5f, 48.1f}, //mi
        {92.52f, -8.83f, 64.41f}, //mj
        {91.06f, -5.43f, 75.86f}, //mk
        {88.66f, -0.55f, 70.06f}, //ml
        {96.81f, -13.32f, 43.02f}, //mm
        {95.36f, -17.89f, 79.04f}, //mn
        {94.49f, -15.57f, 77.75f}, //mo
        {97.22f, -0.89f, 10.38f}, //mp
        {98.28f, 0.43f, 7.21f}, //mq
        {96.07f, 0.58f, 9.76f}, //mr
        {97.76f, 3.24f, 2.49f}, //ms
        {93.68f, 2.16f, 11.39f}, //mt

};

int ph_usa_find_hue(const clralgo_CIEXYZ *sample)
{
    clralgo_CIELab lab;
    clralgo_xyz_to_lab(clralgo_D65, clralgo_DEGREE_10, sample, &lab);
    return ph_usa_find_hue_by_lab(&lab);
}

int ph_usa_find_hue_by_lab(const clralgo_CIELab *sample)
{
    int hue_min = -1;
    // PH_CP_ColorScale_t scale_min = 0;

    float de_min = 20.0f*20.0f;
    for (PH_USA_Hue_t hue = 0; hue < PH_USA_HUE_COUNT; ++hue) {
		float dL = sample->L - s_ph_usa_std_table[hue].L;
		float da = sample->a - s_ph_usa_std_table[hue].a;
		float db = sample->b - s_ph_usa_std_table[hue].b;
		float de = dL*dL + da*da + db*db;
		if (de < de_min) {
			hue_min = hue;
			de_min = de;
			// scale_min = scale;
		}

    }
    return hue_min;
}



