using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;

namespace Slamby.TAU.Helper
{
    public class NgramCountValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            object boundValue = GetBoundValue(value);
            int? ngramCount = boundValue as int?;
            if (boundValue == null)
            {
                return new ValidationResult(false, "Field is required.");
            }
            else if (ngramCount == null)
            {
                return new ValidationResult(false, "Ngram count must be a number");
            }
            else if (ngramCount < 1 || ngramCount > 6)
            {
                return new ValidationResult(false, "Ngram count must be between 1 and 6");
            }
            return ValidationResult.ValidResult;
        }

        private object GetBoundValue(object value)
        {
            if (value is BindingExpression)
            {
                // ValidationStep was UpdatedValue or CommittedValue (Validate after setting)
                // Need to pull the value out of the BindingExpression.
                BindingExpression binding = (BindingExpression)value;

                // Get the bound object and name of the property
                object dataItem = binding.DataItem;
                string propertyName = binding.ParentBinding.Path.Path;

                // Extract the value of the property.
                object propertyValue = dataItem.GetType().GetProperty(propertyName).GetValue(dataItem, null);

                // This is what we want.
                return propertyValue;
            }
            else
            {
                // ValidationStep was RawProposedValue or ConvertedProposedValue
                // The argument is already what we want!
                return value;
            }
        }
    }
}