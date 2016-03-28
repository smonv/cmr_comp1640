using System;

namespace CMR.Custom
{
    public class ConvertHelper
    {
        public DateTime? YearStringToDateTime(string year)
        {
            try
            {
                var result = new DateTime(Convert.ToInt32(year), 1, 1);
                return result;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }
}