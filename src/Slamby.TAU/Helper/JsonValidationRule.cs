using Newtonsoft.Json;
using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;
using Newtonsoft.Json.Linq;

namespace Slamby.TAU.Helper
{
    public class JsonValidationRule : ValidationRule
    {

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            var boundValue = GetBoundValue(value);
            if (boundValue == null) return new ValidationResult(false, "Error");
            var stringValue = boundValue.ToString();
            try
            {
                JsonConvert.DeserializeObject(stringValue);
            }
            catch (Exception exception)
            {
                return new ValidationResult(false, string.Format("Not a valid Json{0}{1}", Environment.NewLine, exception.Message));
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
                if (dataItem == null)
                    return null;
                object propertyValue = null;

                foreach (var propertyName in binding.ParentBinding.Path.Path.Split('.'))
                {
                    // Extract the value of the property.
                    propertyValue = dataItem.GetType().GetProperty(propertyName).GetValue(dataItem, null);
                    dataItem = propertyValue;
                }

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
