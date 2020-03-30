using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Web;

public class LoginToken
{
    /// <summary>アカウント名</summary>
    public string id = "";
    /// <summary>トークン作成日時</summary>
    public long time = 0;
    /// <summary>有効期間(秒)</summary>
    public long expi = 0;
    
    /// <summary>
    /// トークンのデータ部からインスタンスを作成
    /// </summary>
    public static LoginToken GetInstanceFromToken(string tokenCode)
    {
        var spl = tokenCode.Split('.');
        var json = AuthTokenMaker.fromBase64url(spl[0]);
        return JsonSilializer<LoginToken>.FromJson(json);
    }
}