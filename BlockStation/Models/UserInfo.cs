using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Runtime.Serialization;

public class UserInfo
{
    /// <summary>
    /// ユーザーID
    /// </summary>
    [DataMember]
    public string id { get; set; }

    /// <summary>
    /// 名前
    /// </summary>
    [DataMember]
    public string name { get; set; }
    
    /// <summary>
    /// メアド
    /// </summary>
    [DataMember]
    public string mail { get; set; }
    
    /// <summary>
    /// エンコードパスワード
    /// </summary>
    [DataMember]
    public string pass_code { get; set; }

    /// <summary>
    /// 登録日
    /// </summary>
    [DataMember]
    public DateTime create_time { get; set; }

    /// <summary>
    /// ユーザレベル
    /// </summary>
    [DataMember]
    public int level { get; set; }

    /// <summary>
    /// パスワード平文
    /// </summary>
    public string password{
        set { pass_code = ToPasscode(value); }
    }

    public bool CheckPassword(string password)
    {
        var code = ToPasscode(password);
        return code == pass_code;
    }

    private string ToPasscode(string value)
    {
        if(value == null) value = "";
        var sha = new System.Security.Cryptography.SHA256CryptoServiceProvider();
        var bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(value));
        var buil = new System.Text.StringBuilder(bytes.Length * 2);
        foreach (var x in bytes) buil.Append(x.ToString("X2"));
        return buil.ToString();
    }
}