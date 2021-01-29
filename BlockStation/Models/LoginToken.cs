using System;
using System.Runtime.Serialization;

[DataContract]
public class LoginToken
{
    /// <summary>有効期間(通秒)</summary>
    [DataMember]
    public long exp = 0;

    /// <summary>発行日(通秒)</summary>
    [DataMember]
    public long iat = 0;

    /// <summary>アカウント名</summary>
    [DataMember]
    public string id = "";

    /// <summary>ユーザーレベル</summary>
    [DataMember]
    public int level = 0;

    /// <summary>
    /// トークン作成日時
    /// </summary>
    [IgnoreDataMember]
    public DateTime IssuedAt {
        get { return iat.ToDateTime(); }
        set { iat = value.ToEpochSec(); }
    }

    /// <summary>
    /// 有効期限日時
    /// </summary>
    [IgnoreDataMember]
    public DateTime ExpirationTime {
        get { return exp.ToDateTime(); }
        set { exp = value.ToEpochSec(); }
    }
}