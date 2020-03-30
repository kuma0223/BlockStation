using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

public class AccountInfo
{
    public string id = null;
    public long createTime = 0;
    public string passcode = "";

    public string password{
        set { passcode = ToPasscode(value); }
    }

    public bool CheckPassword(string password)
    {
        var code = ToPasscode(password);
        return code == passcode;
    }

    private string ToPasscode(string value)
    {
        var sha = new System.Security.Cryptography.SHA256CryptoServiceProvider();
        var bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(value));
        var buil = new System.Text.StringBuilder(bytes.Length * 2);
        foreach (var x in bytes) buil.Append(x.ToString("X2"));
        return buil.ToString();
    }
}