using System;
using System.Collections.Generic;
using System.IO;
using ThreeNH.ColorSpecimen;
using ThreeNH.DataModel;

namespace ThreeNH.ColorimeterInterface
{
    /// <summary>
    /// 记录管理接口
    /// </summary>
    public interface IRecordManagement
    {
        /// <summary>
        /// 标样总数
        /// </summary>
        int TotalStandards { get; }

        /// <summary>
        /// 试样总数
        /// </summary>
        int TotalSamples { get; }

        /// <summary>
        /// 获取与指定标样关联的试样总数
        /// </summary>
        /// <param name="standardIndex">标样的序号</param>
        /// <returns>与相应标样关联的试样个数</returns>
        int GetSamplesCount(int standardIndex);

        /// <summary>
        /// 获取与指定标样ID号关联的试样的个数
        /// </summary>
        /// <param name="standardId">要获取与之关联的试样个数的标样的ID</param>
        /// <returns>相应的试样个数</returns>
        int GetSamplesCountById(int standardId);

        /// <summary>
        /// 上传指定的标样
        /// </summary>
        /// <param name="standardIndex">要上传的标样的序号</param>
        /// <returns>返回指定的标样</returns>
        StandardSpecimen UploadStandard(int standardIndex);

        /// <summary>
        /// 上传指定ID的标样
        /// </summary>
        /// <param name="standardId">要上传的标样的ID</param>
        /// <returns>返回指定的标样</returns>
        StandardSpecimen UploadStandardById(int standardId);

        /// <summary>
        /// 从指定标样序号开始上传若干个标样
        /// </summary>
        /// <param name="startIndex">起始标样序号</param>
        /// <param name="count">要上传的标样数；如果为-1上传起始序号开始的所有标样</param>
        /// <returns>上传的标样</returns>
        StandardSpecimen[] UploadStandards(int startIndex, int count);

        /// <summary>
        /// 上传指定的试样
        /// </summary>
        /// <param name="standardIndex">试样关联的标样序号</param>
        /// <param name="sampleIndex">要上传的试样的序号</param>
        /// <returns>返回相应的试样</returns>
        Specimen UploadSample(int standardIndex, int sampleIndex);

        /// <summary>
        /// 上传与指定标样ID关联的试样的个数
        /// </summary>
        /// <param name="standardId">要上传的试样关联的标样ID</param>
        /// <param name="sampleIndex">要上传的试样序号</param>
        /// <returns></returns>
        Specimen UploadSampleById(int standardId, int sampleIndex);

        /// <summary>
        /// 批量上传与指定标样ID相关联的试样
        /// </summary>
        /// <param name="standardIndex">要上传的试样关联的标样的序号</param>
        /// <param name="startSampleIndex">起始样品序号</param>
        /// <param name="count">要上传的样品个数；如果为-1上传指定序号开始的所有试样</param>
        /// <returns>上传的试样</returns>
        Specimen[] UploadSamples(int standardIndex, int startSampleIndex, int count);

        /// <summary>
        /// 批量上传与指定标样ID关联的试样
        /// </summary>
        /// <param name="standardId">要上传的试样关联的标样ID</param>
        /// <param name="startSampleIndex">起始样品序号</param>
        /// <param name="count">要上传的样品个数；如果为-1上传指定序号开始的所有试样</param>
        /// <returns>上传的试样</returns>
        Specimen[] UploadSamplesById(int standardId, int startSampleIndex, int count);

        /// <summary>
        /// 异步上传标样数据
        /// </summary>
        /// <param name="handler">接收样品数据的函数</param>
        /// <param name="from">起始序号，从0开始</param>
        /// <param name="count">上传的个数</param>
        //void UploadStandardAsync(SpecimenArrivedHandler handler, int from = 0, int count = -1);
        void UploadStandardAsync(EventHandler<SpecimenReceivedEventArgs> handler, int from = 0, int count = -1);
        /// <summary>
        /// 异步上传试样数据
        /// </summary>
        /// <param name="handler">接收样品数据的函数</param>
        /// <param name="standard">所属标样的索引</param>
        /// <param name="from">起始序号，从0开始</param>
        /// <param name="count">上传的个数</param>
        void UploadTestAsync(EventHandler<SpecimenReceivedEventArgs> handler, int standard, int from = 0, int count = -1);

        /// <summary>
        /// 停止异步上传（从 IColorimeter 下沉，避免色差仪接口承担记录职责）
        /// </summary>
        void StopUpload();

        /// <summary>
        /// 删除所有标样、试样数据
        /// </summary>
        void RemoveAllRecords();

        /// <summary>
        /// 删除所有样品数据
        /// </summary>
        void RemoveAllSamples();

        /// <summary>
        /// 删除与指定序号的标样关联的所有试样
        /// </summary>
        /// <param name="standardIndex">要删除其所有试样的标样的序号；如果标样序号为-1则删除所有试样</param>
        void RemoveAllSamples(int standardIndex);

        /// <summary>
        /// 删除与指定ID的标样关联的所有试样
        /// </summary>
        /// <param name="standardId">要删除其所有试样的标样的ID；如果标样序号为-1则删除所有试样</param>
        void RemoveAllSamplesById(int standardId);

        /// <summary>
        /// 删除指定序号的标样
        /// </summary>
        /// <param name="standardIndex">要删除的标样的序号</param>
        void DeleteStandard(int standardIndex);

        /// <summary>
        /// 删除指定ID的标样
        /// </summary>
        /// <param name="standardId">要删除的标样的ID</param>
        void DeleteStandardById(int standardId);

        /// <summary>
        /// 删除与指定标样序号关联的指定序号的试样
        /// </summary>
        /// <param name="standardIndex">要删除的试样关联的标样的序号</param>
        /// <param name="sampleIndex">要删除的试样的序号</param>
        void DeleteSample(int standardIndex, int sampleIndex);

        /// <summary>
        /// 删除与指定标样ID关联的指定序号的试样
        /// </summary>
        /// <param name="standardId">要删除的试样关联的标样的ID</param>
        /// <param name="sampleIndex">要删除的试样的序号</param>
        void DeleteSampleById(int standardId, int sampleIndex);

        /// <summary>
        /// 向仪器中写入一个标样
        /// </summary>
        /// <param name="standard">要写入仪器中的标样</param>
        void DownloadStandard(Specimen standard);

        /// <summary>
        /// 批量向仪器中写入标样
        /// </summary>
        /// <param name="standards"></param>
        void DownloadStandards(IEnumerable<Specimen> standards);

        /// <summary>
        /// 备份仪器中的数据到指定流
        /// </summary>
        /// <param name="stream">备份数据输出位置</param>
        void Backup(StreamWriter stream);

        /// <summary>
        /// 从指定数据流中恢复备份数据
        /// </summary>
        /// <param name="stream">存储备份数据的输入流</param>
        void Restory(StreamReader stream);
    }
}
