using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;

namespace Slamby.TAU.Helper
{
    public class DatasetNameValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            var stringValue= GetBoundValue(value) as string;
            var illegalCharacters = new List<char> { '\\', '/', '*', '?', '.', '"', '<', '>', '|', ',' };
            if (string.IsNullOrWhiteSpace(stringValue ?? ""))
            {
                return new ValidationResult(false, "Field is required.");
            }
            //else if ((stringValue ?? "").Length < 3)
            //{
            //    return new ValidationResult(false, "Name did not contain at least 3 character");
            //}
            //else if ((stringValue ?? "") != (stringValue ?? "").ToLower() || illegalCharacters.Intersect(stringValue ?? "").Any())
            //{
            //    return new ValidationResult(false, string.Format("Name cannot contain:{0} - upper alpha characters{0} - any of the following characters: {1}", Environment.NewLine, string.Join(" ", illegalCharacters)));
            //}
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
