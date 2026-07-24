/**
 * @file ph_ch_std_table.h
 * @author Zhong Jianfeng
 * @brief 中国药典标准颜色的Lab值
 * @version 0.1
 * @date 2022-07-29
 * 
 * @copyright Copyright (c) 2022
 * 
 */
#ifndef PH_CH_STD_TABLE_H
#define PH_CH_STD_TABLE_H
#include "clralgo.h"

#ifdef __cplusplus
extern "C" {
#endif

// 中国药典色号
typedef enum PH_CP_Hue_t {
    PH_CP_HUE_GY, // 绿黄色
    PH_CP_HUE_YG, // 黄绿色
    PH_CP_HUE_Y, // 黄色
    PH_CP_HUE_OY, // 橙黄色
    PH_CP_HUE_OR, // 橙红色
    PH_CP_HUE_BR, // 棕红色
    PH_CP_HUE_COUNT,  // 色调个数
} PH_CP_Hue_t;

typedef enum PH_CP_ColorScale_t {
    PH_CP_SCALE_0_5,    // 0.5
    PH_CP_SCALE_1,      
    PH_CP_SCALE_2,
    PH_CP_SCALE_3,
    PH_CP_SCALE_4,
    PH_CP_SCALE_5,
    PH_CP_SCALE_6,
    PH_CP_SCALE_7,
    PH_CP_SCALE_8,
    PH_CP_SCALE_9,
    PH_CP_SCALE_10,   
    PH_CP_SCALE_COUNT, 
} PH_CP_ColorScale_t;

extern const clralgo_CIELab s_ph_cp_std_table[PH_CP_HUE_COUNT][PH_CP_SCALE_COUNT];

/**
 * @brief 计算中国药典的色号
 * 
 * @param hue 色调
 * @param sample 样品色，在D65/10度下测得
 * @return float 相应色调号
 */
float ph_cp_calculate_color_scale(PH_CP_Hue_t hue, const clralgo_CIEXYZ *sample);


/**
 * @brief 通过查表方法，查找样品色调号
 * 
 * @param sample 样品在D65/10度下测得的
 * @return int 找到的色调号PH_CP_Hue_t中定义的；如果没找到则返回-1
 */
int ph_cp_find_hue(const clralgo_CIEXYZ *sample);

/**
 * @brief 通过查表方法，查找样品色调号
 * 
 * @param sample 样品在D65/10度下测得的Lab值
 * @return int 找到的色调号PH_CP_Hue_t中定义的；如果没找到则返回-1
 */
int ph_cp_find_hue_by_lab(const clralgo_CIELab *sample);

#ifdef __cplusplus
}
#endif

#endif
