using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreeNH.ColorAlgorithm.IData;

namespace ThreeNH.ColorAlgorithm.Data
{
    public class Sample : ISample, ICloneable
    {
        private Dictionary<string, IColorData> _colors = new Dictionary<string, IColorData>();//20191206 去掉readonly
        private readonly Dictionary<string, IDensityData> _densities = new Dictionary<string, IDensityData>();


        public Sample() { }

        public Sample(ISample source, bool deepCopy = true)
        {
            if (deepCopy)
            {
                DeepCopyFrom(source);
            }
            else
            {
                ShallowCopyFrom(source);
            }
        }

        protected void ShallowCopyFrom(ISample source)
        {
            Name = source.Name;
            DateTime = source.DateTime;
            InstrumentInformation = source.InstrumentInformation;
            foreach (var channel in source.Channels)
            {
                _colors.Add(channel, source.GetColorData(channel));
            }
        }

        protected void DeepCopyFrom(ISample source)
        {
            Name = source.Name;
            DateTime = source.DateTime;
            InstrumentInformation = source.InstrumentInformation;
            foreach (var channel in source.Channels)
            {
                _colors.Add(channel, new ColorData(source.GetColorData(channel)));
            }
        }


        #region ISample

        public string Id { get; set; }
        public string Name { get; set; }

        public DateTime DateTime { get; set; }
        public string Material { get; set; }
        public string Remark { get; set; }
        public string ColorFormula { get; set; }

        public IInstrumentInformation InstrumentInformation { get; set; }

        public string[] Channels => _colors.Keys.ToArray();

        /// <summary>
        /// 由于使用反射率可以计算出密度，所以密度通道包括带反射率的颜色通道
        /// </summary>
        public string[] DensityChannels => _densities.Keys.ToArray();

        public object Tag { get; set; }

        public IColorData GetColorData(string channel)//s8_M s5_L
        {
            return _colors.ContainsKey(channel) ? _colors[channel] : null;
        }

        public IDensityData GetDensityData(string channel)
        {
            // 先查找密度值如果没有再查找使用反射率的色度值
            return _densities.ContainsKey(channel) ? _densities[channel] : null;
        }

        public void SetColorData(string channel, IColorData colorData)
        {
            if (colorData != null)
            {
                _colors[channel] = colorData;
            }
            else if (_colors.ContainsKey(channel))
            {
                _colors.Remove(channel);
            }
        }

        public void SetDensityData(string channel, IDensityData densityData)
        {
            if (densityData != null)
            {
                _densities[channel] = densityData;
            }
            else if (_densities.ContainsKey(channel))
            {
                _densities.Remove(channel);
            }
        }


        public virtual object Clone()
        {
            return new Sample(this);
        }

        #endregion
        #region 20191206 lilin add
        private bool StringArrayCompare(string[] Arr1, string[] Arr2)
        {
            try
            {
                int i = 0;
                foreach (var str in Arr1)
                {
                    if (!str.Equals(Arr2[i]))
                    {
                        return false;
                    }
                    i++;
                }
                i = 0;
                foreach (var str in Arr2)
                {
                    if (!str.Equals(Arr1[i]))
                    {
                        return false;
                    }
                    i++;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
        private bool SpectraDataCompare(Sample ele, Sample std)
        {
            IColorData colorData1 = ele.GetColorData(ele.Channels[0]);
            IColorData colorData2 = std.GetColorData(std.Channels[0]);
            if (colorData1 == null || colorData2 == null) return true; //数据有误,认为相等，不添加
            else
            {
                if (colorData1.SpectralData.Count == colorData2.SpectralData.Count &&
                    colorData1.SpectralData.WavelengthBegin == colorData2.SpectralData.WavelengthBegin &&
                    colorData1.SpectralData.WavelengthEnd == colorData2.SpectralData.WavelengthEnd &&
                    colorData1.SpectralData.WavelengthInterval == colorData2.SpectralData.WavelengthInterval)
                {
                    try
                    {
                        for (int i = colorData1.SpectralData.WavelengthBegin; i <= colorData1.SpectralData.WavelengthEnd;)
                        {
                            if (colorData1.SpectralData[i] != colorData2.SpectralData[i])
                            {
                                return false;
                            }
                            i += colorData1.SpectralData.WavelengthInterval;
                        }
                        return true;
                    }
                    catch
                    {
                        return true; //数据有误不添加
                    }
                }
            }
            return true;
        }
        public bool EqualObj(object obj1, object obj2)
        {
            if (obj1 == null || obj2 == null)
            {
                if (obj1 == null && obj2 == null)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                var std = obj1 as Sample;
                var sam = obj2 as Sample;
                bool bl;
                bl = StringArrayCompare(sam.DensityChannels, std.DensityChannels);
                if (!bl) return false;
                bl = StringArrayCompare(sam.Channels, std.Channels);
                if (!bl) return false;
                bl = sam.ColorFormula == std.ColorFormula;
                if (!bl) return false;
                bl = sam.DateTime.Equals(std.DateTime);
                if (!bl) return false;
                if (sam.InstrumentInformation == null || std.InstrumentInformation == null)
                {
                    if (sam.InstrumentInformation == null && std.InstrumentInformation == null)
                    {
                        bl = true;
                    }
                    else
                    {
                        bl = false;
                    }
                }
                else
                {
                    bl = sam.InstrumentInformation.Equals(std.InstrumentInformation);
                }
                if (!bl) return false;
                bl = sam.Id == std.Id;
                if (!bl) return false;
                bl = sam.Material == std.Material;
                if (!bl) return false;
                bl = sam.Name == std.Name;
                if (!bl) return false;
                bl = sam.Remark == std.Remark;
                if (!bl) return false;
                bl = SpectraDataCompare(sam, std);
                if (!bl) return false;
                return true;
            }
        }
        #endregion lilin end

    }
}
