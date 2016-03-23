using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CMR.Helpers
{
    public class ConvertHelper
    {
        public DateTime? YearStringToDateTime(string year)
        {
            try
            {
                DateTime result = new DateTime(Convert.ToInt32(year), 1, 1);
                return result;
            }catch(Exception ex)
            {
                return null;
            }
        }
    }
}