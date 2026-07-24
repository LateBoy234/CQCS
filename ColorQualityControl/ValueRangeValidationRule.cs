using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Controls;

namespace ThreeNH.ColorQualityControl
{
    class ValueRangeValidationRule : ValidationRule
    {
        double? minValue = null;
        double? maxValue = null;
        bool canBeNull = false;

        public double? MinValue
        {
            get { return this.minValue; }
            set { this.minValue = value; }
        }

        public double? MaxValue
        {
            get { return this.maxValue; }
            set { this.maxValue = value; }
        }

        public bool CanBeNull
        {
            get { return canBeNull; }
            set { canBeNull = value; }
        }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (CanBeNull && string.IsNullOrWhiteSpace(value as string))
                return new ValidationResult(true, null);

            double margin;

            // Is a number?
            if (!double.TryParse((string)value, out margin))
            {
                return new ValidationResult(false, "Not a number.");
            }

            // Is in range?
            if ((margin < this.minValue) || (margin > this.maxValue))
            {
                string msg = string.Format("Value must be between {0} and {1}.", this.minValue, this.maxValue);
                return new ValidationResult(false, msg);
            }

            // Number is valid
            return new ValidationResult(true, null);
        }
    }
}
