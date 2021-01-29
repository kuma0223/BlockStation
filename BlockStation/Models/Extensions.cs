using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public static class Extensions
{
    //日時型->UNIX秒
    public static long ToEpochSec(this DateTime x) {
        return (long)(x - new DateTime(1970,1,1,9,0,0)).TotalSeconds;
    }
    //UNIX秒->日時型
    public static DateTime ToDateTime(this long x) {
        return (new DateTime(1970, 1, 1, 9, 0, 0)).AddSeconds(x);
    }
}
