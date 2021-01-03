using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using System.Runtime.Serialization;
using System.Data.SQLite;
using System.Reflection;

/// <summary>
/// SQLiteコネクションカバー。
/// インスタンスごとにはDBへのアクセスが直列化されます。
/// </summary>
public class SQLiteAdapter
{
    SQLiteConnection con;
    object lockobj = new object();

    public SQLiteAdapter(string dbpath) {
        var str = "Data Source=" + dbpath;
        con = new SQLiteConnection(str);
    }

    ~SQLiteAdapter() {
        con.Close();
    }

    public void Open() {
        lock (lockobj) con.Open();
    }
    public void Close() {
        lock (lockobj) con.Close();
    }
    public int ExecuteNonQuery(string sql) {
        lock (lockobj) {
            using (var cmd = con.CreateCommand()) {
                cmd.CommandText = sql;
                return cmd.ExecuteNonQuery();
            }
        }
    }
    public List<Dictionary<string, object>> ExecuteSelect(string sql) {
        var names = new List<string>();
        var table = new List<Dictionary<string, object>>();

        lock (lockobj) {
            using (var cmd = con.CreateCommand()) {
                cmd.CommandText = sql;
                using (var reader = cmd.ExecuteReader()) {
                    for(int i=0; i<reader.FieldCount; i++) {
                        names.Add(reader.GetName(i));
                    }
                    while (reader.Read()) {
                        var rec = new Dictionary<string, object>(names.Count);
                        for (int i = 0; i < reader.FieldCount; i++) {
                            //var type = reader.GetFieldType(i);
                            rec[names[i]] = reader[i];
                        }
                        table.Add(rec);
                    }
                }
            }
        }
        return table;
    }

    private void ExecuteReader(string sql, Action<SQLiteDataReader> onread) {
        lock (lockobj) {
            using (var cmd = con.CreateCommand()) {
                cmd.CommandText = sql;
                using(var reader = cmd.ExecuteReader()) {
                    onread(reader);
                }
            }
        }
    }

    public string ToSql(object value) {
        if(value == null) return "NULL";
        if(value is string) return $"'{value}'";
        if(value is DateTime) return $"'{(DateTime)value:yyyy/MM/dd HH:mm:ss}'";
        return value.ToString();
    }

    public bool Insert(string table, object rec) {
        var columns = new StringBuilder();
        var values = new StringBuilder();
        
        var props = rec.GetType().GetProperties()
            .Where(x=>x.GetCustomAttributes(false).Any(y=>y is DataMemberAttribute));

        foreach (var pro in props) {
            if(columns.Length > 0) columns.Append(",");
            columns.Append(pro.Name);
                
            if(values.Length > 0) values.Append(",");
            values.Append(ToSql(pro.GetValue(rec)));
        };

        string sql = "INSERT INTO " + table + " (" + columns + ") VALUES(" + values + ")" ;

        return ExecuteNonQuery(sql) == 1;
    }

    public T SelectFirst<T>(string sql) where T:class, new() {
        var r = Select<T>(sql);
        return r.Count > 0 ? r.First() : null;
    }

    public List<T> Select<T>(string sql) where T:class, new() {
        if(!sql.StartsWith("SELECT") && !sql.StartsWith("select")){
            throw new Exception("SQL not starts with 'SELECT'");
        }

        var props = typeof(T).GetProperties()
            .Where(x=>x.GetCustomAttributes(false).Any(y=>y is DataMemberAttribute));
        
        var propmap = new Dictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);
        foreach(var prop in props) {
            propmap[prop.Name] = prop;
        }

        var list = new List<T>();

        ExecuteReader(sql, reader => {
            while (reader.Read()) {
                var rec = new T();
                for (int i = 0; i < reader.FieldCount; i++) {
                    if(reader.IsDBNull(i)) continue;
                    if(!propmap.ContainsKey(reader.GetName(i))) continue;

                    var prop = propmap[reader.GetName(i)];
                    if (prop.PropertyType == typeof(int)) {
                        prop.SetValue(rec, (int)(long)reader[i]);
                    } else if (prop.PropertyType == typeof(DateTime)) {
                        prop.SetValue(rec, DateTime.Parse((string)reader[i]));
                    } else {
                        prop.SetValue(rec, reader[i]);
                    }
                }
                list.Add(rec);
            }
        });
        return list;
    }
}