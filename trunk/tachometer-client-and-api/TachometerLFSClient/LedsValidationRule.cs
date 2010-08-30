using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;

namespace TachometerLFSClient
{
    class LedsValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
        {
            if (value != null)
            {
                int validatedValue;
                if (!int.TryParse(value.ToString(), out validatedValue)) 
                {
                    return new ValidationResult(false, "must be a whole number");
                }
                if (validatedValue < 0 || validatedValue > 12)
                {
                    return new ValidationResult(false, "must be beetwen 0 and 12 inclusive");
                }                
            }
            return new ValidationResult(true, null);
        }
    }
}
