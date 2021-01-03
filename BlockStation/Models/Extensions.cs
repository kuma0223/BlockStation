using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlockStation.Models
{
    public static class Extensions
    {
        public static long ToEpochSec(this DateTime x) {
            return (long)(x - new DateTime(1970,1,1,9,0,0)).TotalSeconds;
        }
        public static DateTime ToDateTime(this long x) {
            return (new DateTime(1970, 1, 1, 9, 0, 0)).AddSeconds(x);
        }
    }
}
