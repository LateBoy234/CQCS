using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using ThreeNH.ColorAlgorithm.IData;

namespace ThreeNH.ColorAlgorithm.Data
{
    /// <summary>
    /// 约定：如果下限或上限是NaN，表明不使用下限或上限判断
    /// </summary>
    [DataContract]
    public sealed class ToleranceItem : IToleranceItem, INotifyPropertyChanged
    {
        [DataMember(Name = "Lower")]
        private double _lower;
        [DataMember(Name = "Upper")]
        private double _upper;


        /// <summary>
        /// 下限，如果为NaN，表明不使用下限做判断
        /// </summary>
        public double Lower
        {
            get => _lower;
            set
            {
                if (value.Equals(_lower)) return;
                _lower = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsValid));
                OnPropertyChanged(nameof(IsLowerUsed));
            }
        }

        /// <summary>
        /// 上限，如果为NaN表明不使用上限
        /// </summary>
        public double Upper
        {
            get => _upper;
            set
            {
                if (value.Equals(_upper)) return;
                _upper = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsValid));
                OnPropertyChanged(nameof(IsUpperUsed));
            }
        }

        /// <summary>
        /// 是否有效
        /// </summary>
        public bool IsValid => Lower <= Upper;

        /// <summary>
        /// 是否使用下限
        /// </summary>
        public bool IsLowerUsed => !double.IsNaN(Lower);

        /// <summary>
        /// 是否使用上限
        /// </summary>
        public bool IsUpperUsed => !double.IsNaN(Upper);

        /// <summary>
        /// 给定值是否合格
        /// </summary>
        public bool IsPass(double value)
        {
            return (!IsLowerUsed || value >= Lower) &&
                   (!IsUpperUsed || value <= Upper);
        }

        public ToleranceItem()
        {
            _lower = double.NaN;
            _upper = double.NaN;
        }

        public ToleranceItem(double lower, double upper)
        {
            _lower = lower;
            _upper = upper;
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
