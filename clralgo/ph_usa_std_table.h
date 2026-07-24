#ifndef __PH_USA_STD_TABLE_H__
#define __PH_USA_STD_TABLE_H__

#include "clralgo.h"

#ifdef __cplusplus
extern "C" {
#endif

// 美国药典色号
typedef enum PH_USA_Hue_t {
    PH_USA_HUE_MA, 
    PH_USA_HUE_MB, 
    PH_USA_HUE_MC, 
    PH_USA_HUE_MD, 
    PH_USA_HUE_ME, 
    PH_USA_HUE_MF, 	
    PH_USA_HUE_MG, 
    PH_USA_HUE_MH, 
    PH_USA_HUE_MI, 
    PH_USA_HUE_MJ, 
    PH_USA_HUE_MK, 
    PH_USA_HUE_ML, 
    PH_USA_HUE_MM, 
    PH_USA_HUE_MN, 
    PH_USA_HUE_MO, 
    PH_USA_HUE_MP, 
    PH_USA_HUE_MQ, 
    PH_USA_HUE_MR, 	
    PH_USA_HUE_MS, 
    PH_USA_HUE_MT, 
    PH_USA_HUE_COUNT,  
} PH_USA_Hue_t;

extern const clralgo_CIELab s_ph_usa_std_table[PH_USA_HUE_COUNT];

/**
 * @brief 通过查表方法，查找样品色调号
 * 
 * @param sample 样品在D65/10度下测得的
 * @return int 找到的色调号PH_USA_Hue_t中定义的；如果没找到则返回-1
 */
int ph_usa_find_hue(const clralgo_CIEXYZ *sample);

/**
 * @brief 通过查表方法，查找样品色调号
 * 
 * @param sample 样品在D65/10度下测得的Lab值
 * @return int 找到的色调号PH_USA_Hue_t中定义的；如果没找到则返回-1
 */
int ph_usa_find_hue_by_lab(const clralgo_CIELab *sample);


#ifdef __cplusplus
}
#endif

#endif