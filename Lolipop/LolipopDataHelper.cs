using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.Configuration;
using System.Diagnostics;
using System.Reflection;
using System.IO;
using System.Globalization;

namespace Lolipop
{
    public abstract class LolipopDataHelper
    {
        // 获取本类的调用堆栈
        private MethodBase mClassContext = new StackTrace().GetFrame(1).GetMethod();

        private string mFilePath = ConfigurationManager.AppSettings["DBFilePath"] ?? "";
        private SQLiteConnection mConn = null;
        
        private List<string> mColumsType = new List<string>(); // 字段类型
        private List<string> mColumsName = new List<string>(); // 字段名
        private List<PropertyInfo> mColumnsObj = new List<PropertyInfo>(); // 存储属性的对象
        private string mOldDataObj = "";
        private long mOldDataROWID = -129980099;

        private const string SQL_CREATE_TABLE = "CREATE TABLE IF NOT EXISTS {0} ({1})";
        private const string SQL_INSERT_DATA = "INSERT OR IGNORE INTO {0} values({1})";
        private const string SQL_SELECT_ROWID = "SELECT rowid FROM {0} WHERE {1}";
        private const string SQL_UPDATE_DATA = "UPDATE OR IGNORE {0} SET {1} WHERE OID={2}";
        private const string SQL_DELETE_DATA = "DELETE FROM {0} WHERE OID={1}";
        private const string SQL_SELECT_ALL_DATA = "SELECT * FROM {0}";

        private long ROWID { get; set; } = -129980099;
        private string TABLENAME { get; set; } // 表名
        public LolipopDataHelper()
        {
            init();
        }

        public LolipopDataHelper(string filePath)
        {
            this.mFilePath = filePath;
            init();
        }

        private void init()
        {
            // 初始化连接
            mConn = new SQLiteConnection("Data Source=" + mFilePath);
            Debug.Print("=============DBFilePath===============");
            printDebugInfo(mFilePath);
            if (mFilePath.Equals(""))
            {
                throw new LolipopDataException("在App.config中找不到DBFilePath节点");
            }

            // 获取类信息，成员信息
            Debug.Print("=============ClassInfo===============");
            this.TABLENAME = getClassName();
            printDebugInfo(TABLENAME);
            PropertyInfo[] fields = this.mClassContext.ReflectedType.GetProperties();
            foreach (PropertyInfo item in fields)
            {
                // 属性对象
                mColumnsObj.Add(item);
                // 变量标识符
                mColumsName.Add(item.Name);
                // SQL数据类型
                switch (item.PropertyType.Name.ToUpper())
                {
                    case "INT32":
                        mColumsType.Add("INTEGER");
                        break;
                    case "INT64":
                        mColumsType.Add("INTEGER");
                        break;
                    case "BOOLEAN":
                        mColumsType.Add("NUMERIC");
                        break;
                    case "SINGLE":
                        mColumsType.Add("REAL");
                        break;
                    case "DOUBLE":
                        mColumsType.Add("REAL");
                        break;
                    case "STRING":
                        mColumsType.Add("TEXT");
                        break;
                    case "CHAR":
                        mColumsType.Add("TEXT");
                        break;
                    case "DATETIME":
                        mColumsType.Add("INTEGER");
                        break;
                }
                printDebugInfo("属性名： " + item.Name + "值类型： " + item.PropertyType.Name);
            }

            // 拼合建表语句
            string tempColumnStr = "";            
            for (int i = 0; i < mColumsName.Count; i++)
            {
                string fieldsAttr = ((LolipopColumnAttr)mColumnsObj[i].GetCustomAttribute(typeof(LolipopColumnAttr)))?.columnAttr;                
                tempColumnStr += mColumsName[i] + " " + mColumsType[i] + " " + fieldsAttr + ", ";
            }
            tempColumnStr = tempColumnStr.Substring(0, tempColumnStr.Length - 2);
            // 执行建表SQL查询
            excuteQuery(string.Format(SQL_CREATE_TABLE, TABLENAME, tempColumnStr));
        }

        public void save()
        {
            Debug.Print("=============SaveMethod===============");
            // 插入数据
            excuteQuery(string.Format(SQL_INSERT_DATA, TABLENAME, getAllPropertyValueString(false)));

            // 更新ROWID
            getRowID();

            // 保存当前实例，供以后回滚操作
            this.mOldDataObj = this.getAllPropertyValueString();
            this.mOldDataROWID = this.ROWID;
        }

        public void update()
        {
            Debug.Print("=============UpdateMethod===============");
            // 更新数据
            excuteQuery(string.Format(SQL_UPDATE_DATA, TABLENAME, getAllPropertyValueString(), ROWID));

            //// 更新ROWID
            getRowID();
        }

        public void delete()
        {
            Debug.Print("=============DeleteMethod===============");
            // 删除当前项
            excuteQuery(string.Format(SQL_DELETE_DATA, TABLENAME, ROWID));
        }

        public void rollBack()
        {
            Debug.Print("============" + mOldDataObj);

            Debug.Print("=============RollBackMethod===============");
            // 更新数据
            excuteQuery(string.Format(SQL_UPDATE_DATA, TABLENAME, mOldDataObj, ROWID));

            //// 更新ROWID
            getRowID(true);            
        }

        public long getRowID(bool isRollBack = false)
        {
            if (!isRollBack)
            {
                // 更新ROWID
                excuteQuery(string.Format(SQL_SELECT_ROWID, TABLENAME, getAllPropertyValueString().Replace(",", " and")), (data) => {
                    data.Read();
                    Debug.Print("=================ROWID=================");
                    printDebugInfo(data[0].ToString());
                    this.ROWID = (long)data[0];
                });
            }
            else
            {
                this.ROWID = mOldDataROWID;
            }
            return this.ROWID;
        }

        public delegate void OnReturnDataEvent(SQLiteDataReader reader);
        public void excuteQuery(string queryString, OnReturnDataEvent handle = null)
        {
            Debug.Print("=============QueryString===============");
            printDebugInfo(queryString);
            if (mConn != null)
            {
                try
                {
                    mConn.Open();
                    SQLiteCommand cmd = mConn.CreateCommand();
                    cmd.CommandText = queryString;
                    if (handle == null)
                    {
                        // 一般查询
                        cmd.ExecuteNonQuery();
                        mConn.Close();
                        return;                        
                    }
                    // 回调函数返回reader
                    handle(cmd.ExecuteReader());
                    mConn.Close();
                }
                catch (Exception ex)
                {
                    throw new LolipopDataException("一个未知的错误发生，详情请查看调用堆栈", ex);
                }
            }
            else
            {
                throw new LolipopDataException("数据库连接对象为null");
            }
        }

        public string getAllPropertyValueString(bool isContainsKey = true)
        {
            string tempStr = "";            
            for (int i = 0; i < mColumsName.Count; i++)
            {
                tempStr += isContainsKey? mColumsName[i] + "='" + getPropertyValue(mColumnsObj[i]) + "', "  : "'" + getPropertyValue(mColumnsObj[i]) + "', ";

            }
            // 返回 key=value,key=value
            return tempStr.Substring(0, tempStr.Length - 2);
        }

        private string getPropertyValue(PropertyInfo info)
        {
            return info.GetValue(this, null)?.ToString() ?? "";
        }

        private string getClassName()
        {
            return mClassContext.ReflectedType.Name;
        }

        private static void printDebugInfo(string info)
        {
            Debug.Print(string.Format("Debug  {0} -d:{1}", DateTime.Now.ToString(), info));
        }

        public List<T> findAll<T>()
        {
            List<T> tempList = new List<T>();
            excuteQuery(string.Format(SQL_SELECT_ALL_DATA, typeof(T).Name), reader => {
                while(reader.Read())
                {
                    T tempData = Activator.CreateInstance<T>();
                    PropertyInfo[] properties = tempData.GetType().GetProperties();
                    foreach (PropertyInfo property in properties)
                    {
                        // 数据类型转换
                        switch (property.PropertyType.Name.ToUpper())
                        {
                            case "INT32":
                                property.SetValue(tempData, int.Parse(reader[property.Name].ToString()));
                                break;
                            case "INT64":
                                property.SetValue(tempData, long.Parse(reader[property.Name].ToString()));
                                break;
                            case "BOOLEAN":                                
                                property.SetValue(tempData, bool.Parse(reader.GetTextReader(properties.ToList().IndexOf(property)).ReadLine()));
                                break;
                            case "SINGLE":
                                property.SetValue(tempData, float.Parse(reader[property.Name].ToString()));
                                break;
                            case "DOUBLE":
                                property.SetValue(tempData, double.Parse(reader[property.Name].ToString()));
                                break;
                            case "STRING":
                                property.SetValue(tempData, reader[property.Name].ToString());
                                break;
                            case "CHAR":
                                property.SetValue(tempData, char.Parse(reader.ToString()));
                                break;
                            case "DATETIME":
                                //printDebugInfo(reader.GetString(0).Replace('/', '-'));
                                property.SetValue(tempData, DateTime.Parse(reader.GetString(0).Replace('/', '-')));
                                //printDebugInfo(getPropertyValue(property));
                                break;
                        }                       
                    }
                    //printDebugInfo(tempData.GetType().GetProperty("time").GetValue(tempData, null).ToString());
                    tempList.Add(tempData);
                }
            });
            return tempList;
        }
    }

    public class LolipopDataException : ApplicationException
    {
        private string errorMsg;
        private Exception innerException;

        public LolipopDataException()
        {

        }

        public LolipopDataException(string errorMsg): base(errorMsg)
        {
            this.errorMsg = errorMsg;
        }

        public LolipopDataException(string errorMsg, Exception innerException): base(errorMsg, innerException)
        {
            this.errorMsg = errorMsg;
            this.innerException = innerException;
        }

        public string getError()
        {
            return this.errorMsg;
        }
    }


    // 标注数据库字段属性的特性
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = false)]
    public class LolipopColumnAttr : Attribute  
    {
        public string columnAttr;
        public LolipopColumnAttr(string attr) 
        {
            this.columnAttr = attr;
        }
    }


}
