#ifndef __PH_EU_STD_TABLE_H__
#define __PH_EU_STD_TABLE_H__

#include "clralgo.h"

#ifdef __cplusplus
extern "C" {
#endif

// 欧洲药典色号
typedef enum PH_EU_Hue_t {
    PH_EU_HUE_B,  // 棕色
    PH_EU_HUE_BY, // 宗黄色
    PH_EU_HUE_Y,  // 黄色
    PH_EU_HUE_GY, // 黄绿色
    PH_EU_HUE_R,  // 红色
    PH_EU_HUE_COUNT,  // 色调个数
} PH_EU_Hue_t;

typedef enum PH_EU_ColorScale_t {
    PH_EU_SCALE_1,      
    PH_EU_SCALE_2,
    PH_EU_SCALE_3,
    PH_EU_SCALE_4,
    PH_EU_SCALE_5,
    PH_EU_SCALE_6,
    PH_EU_SCALE_7,
    PH_EU_SCALE_8,
    PH_EU_SCALE_9,
    PH_EU_SCALE_COUNT, 
} PH_EU_ColorScale_t;

extern const clralgo_CIELab s_ph_eu_std_table[PH_EU_HUE_COUNT][PH_EU_SCALE_COUNT];

/**
 * @brief 计算欧洲药典的色号
 * 
 * @param hue 色调
 * @param sample 样品色，在D65/10度下测得
 * @return float 相应色调号
 */
float ph_eu_calculate_color_scale(PH_EU_Hue_t hue, const clralgo_CIEXYZ *sample);


/**
 * @brief 通过查表方法，查找样品色调号
 * 
 * @param sample 样品在D65/10度下测得的
 * @return int 找到的色调号PH_EU_Hue_t中定义的；如果没找到则返回-1
 */
int ph_eu_find_hue(const clralgo_CIEXYZ *sample);

/**
 * @brief 通过查表方法，查找样品色调号
 * 
 * @param sample 样品在D65/10度下测得的Lab值
 * @return int 找到的色调号PH_EU_Hue_t中定义的；如果没找到则返回-1
 */
int ph_eu_find_hue_by_lab(const clralgo_CIELab *sample);

#ifdef __cplusplus
}
#endif

#endif

