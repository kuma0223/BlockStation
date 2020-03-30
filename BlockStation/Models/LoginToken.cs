using System;
using System.IO;
using System.Runtime.Serialization;
using System.Text;

[DataContract]
public class LoginToken
{
    /// <summary>アカウント名</summary>
    [DataMember]
    public string id = "";
    
    /// <summary>トークン作成日時</summary>
    [DataMember]
    public string time = "";

    /// <summary>有効期間(秒)</summary>
    [DataMember]
    public long expi = 0;

    /// <summary>ユーザーレベル</summary>
    [DataMember]
    public int level = 0;
    
    /// <summary>
    /// トークン作成日時
    /// </summary>
    [IgnoreDataMember]
    public DateTime TimeValue {
        get {
            if(time == null || time.Length < 12) {
                return new DateTime(1970,1,1);
            }
            return new DateTime(
                int.Parse(time.Substring(0,4)),
                int.Parse(time.Substring(4,2)),
                int.Parse(time.Substring(6,2)),
                int.Parse(time.Substring(8,2)),
                int.Parse(time.Substring(10,2)),
                0);
        }
        set {
            time = value.ToString("yyyyMMddHHmm");
        }
    }

    /// <summary>
    /// 有効期限日時
    /// </summary>
    [IgnoreDataMember]
    public DateTime ExpirationDate {
        get {
            return TimeValue.AddSeconds(expi);
        }
    }
}