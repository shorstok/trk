using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace trackvisualizer.View
{
    public enum Dimension
    {
        MetersToKilometersLength,
        MetersHeight,
        ExtremeHeightsList,
        TimeHours,
        WeightKg,
    }

    public class DimensionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is IEnumerable enumerable && 
                parameter is Dimension dimension && 
                dimension == Dimension.ExtremeHeightsList)
                return string.Join(@" — ", enumerable.Cast<double>().Select(v => v.ToString(@"0.")));

            var numeric = (double)System.Convert.ChangeType(value, typeof(double));

            switch (parameter)
            {
                case Dimension.MetersToKilometersLength:
                    return (numeric/1e3).ToString(@"0.0");
                case Dimension.MetersHeight:
                    return numeric.ToString(@"0.");
                case Dimension.TimeHours:
                    return numeric.ToString(@"0.0");
                case Dimension.WeightKg:
                    return numeric.ToString(@"0.0");
                default:
                    return value?.ToString()??String.Empty;
            }
        }
        
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
