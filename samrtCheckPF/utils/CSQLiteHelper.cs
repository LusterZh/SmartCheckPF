using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.Data;
using System.IO;

namespace smartCheckPF.utils
{
    class CSQLiteHelper
    {
        private string _dbName = "";
        private SQLiteConnection _SQLiteConn = null;     //连接对象
        private SQLiteTransaction _SQLiteTrans = null;   //事务对象
        private bool _IsRunTrans = false;        //事务运行标识
        private string _SQLiteConnString = null; //连接字符串
        private bool _AutoCommit = false; //事务自动提交标识

        public string SQLiteConnString
        {
            set { this._SQLiteConnString = value; }
            get { return this._SQLiteConnString; }
        }

        public CSQLiteHelper(string dbPath)
        {
            this._dbName = dbPath;
            this._SQLiteConnString = "Data Source=" + dbPath;
        }

        /// <summary>
        /// 新建数据库文件
        /// </summary>
        /// <param name="dbPath">数据库文件路径及名称</param>
        /// <returns>新建成功，返回true，否则返回false</returns>
        static public Boolean NewDbFile(string dbPath)
        {
            try
            {
                SQLiteConnection.CreateFile(dbPath);
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("新建数据库文件" + dbPath + "失败：" + ex.Message);
            }
        }


        /// <summary>
        /// 创建表
        /// </summary>
        /// <param name="dbPath">指定数据库文件</param>
        /// <param name="tableName">表名称</param>
        static public void NewTable(string dbPath, string tableName)
        {

            SQLiteConnection sqliteConn = new SQLiteConnection("data source=" + dbPath);
            if (sqliteConn.State != System.Data.ConnectionState.Open)
            {
                sqliteConn.Open();
                SQLiteCommand cmd = new SQLiteCommand();
                cmd.Connection = sqliteConn;
                cmd.CommandText = "CREATE TABLE " + tableName + "(imagename varchar NOT NULL UNIQUE,Testresult varchar NOT NULL UNIQUE, create_time DATETEXT NOT NULL UNIQUE)";
                cmd.ExecuteNonQuery();
            }
            sqliteConn.Close();
        }
        /// <summary>
        /// 打开当前数据库的连接
        /// </summary>
        /// <returns></returns>
        public Boolean OpenDb()
        {
            try
            {
                if (File.Exists(this._dbName))
                {//如果已经存在，直接打开
                    this._SQLiteConn = new SQLiteConnection(this._SQLiteConnString);
                    this._SQLiteConn.Open();
                    return true;
                }
                else
                {
                    if(NewDbFile(this._dbName))
                    {
                        NewTable(this._dbName, "TEST");
                        this._SQLiteConn = new SQLiteConnection(this._SQLiteConnString);
                        this._SQLiteConn.Open();
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("打开数据库：" + _dbName + "的连接失败：" + ex.Message);
            }
        }

        /// <summary>
        /// 打开指定数据库的连接
        /// </summary>
        /// <param name="dbPath">数据库路径</param>
        /// <returns></returns>
        public Boolean OpenDb(string dbPath)
        {
            try
            {
                string sqliteConnString = "Data Source=" + dbPath;

                this._SQLiteConn = new SQLiteConnection(sqliteConnString);
                this._dbName = dbPath;
                this._SQLiteConnString = sqliteConnString;
                this._SQLiteConn.Open();
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("打开数据库：" + dbPath + "的连接失败：" + ex.Message);
            }
        }

        /// <summary>
        /// 关闭数据库连接
        /// </summary>
        public void CloseDb()
        {
            if (this._SQLiteConn != null && this._SQLiteConn.State != ConnectionState.Closed)
            {
                if (this._IsRunTrans && this._AutoCommit)
                {
                    this.Commit();
                }
                this._SQLiteConn.Close();
                this._SQLiteConn = null;
            }
        }

        /// <summary>
        /// 开始数据库事务
        /// </summary>
        public void BeginTransaction()
        {
            this._SQLiteConn.BeginTransaction();
            this._IsRunTrans = true;
        }

        /// <summary>
        /// 开始数据库事务
        /// </summary>
        /// <param name="isoLevel">事务锁级别</param>
        public void BeginTransaction(IsolationLevel isoLevel)
        {
            this._SQLiteConn.BeginTransaction(isoLevel);
            this._IsRunTrans = true;
        }

        /// <summary>
        /// 提交当前挂起的事务
        /// </summary>
        public void Commit()
        {
            if (this._IsRunTrans)
            {
                this._SQLiteTrans.Commit();
                this._IsRunTrans = false;
            }
        }

        /// <summary>
        /// 插入表数据
        /// </summary>
        /// <param name="dbPath">指定数据库文件</param>
        /// <param name="tableName">表名称</param>
         public int InsertTableData(string tableName, string name, string result)
        {
            if(_SQLiteConn.State  == System.Data.ConnectionState.Open)
            {
                string s = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");

                SQLiteCommand cmd = new SQLiteCommand();
                cmd.Connection = _SQLiteConn;

                cmd.CommandText = "insert into " + tableName + "(imagename,Testresult,create_time) values(@imagename,@Testresult,@create_time)";
                cmd.Parameters.AddWithValue("@imagename", name);
                cmd.Parameters.AddWithValue("@Testresult", result);
                cmd.Parameters.AddWithValue("@create_time", s);
                cmd.ExecuteNonQuery();
                return 0;
            }
            else
            {
                return -1;
            }
        }

        /// <summary>
        /// 查询表数据
        /// </summary>
        /// <param name="tableName">表名称</param>
        /// <param name="name">图片名称</param>
        public string QueryTableData(string tableName, string name)
        {
            try
            {
                if (_SQLiteConn.State == System.Data.ConnectionState.Open)
                {
                    SQLiteCommand cmd = new SQLiteCommand();
                    cmd.Connection = _SQLiteConn;

                    string commandStr = @"select * from " + tableName + " where imagename ='" + name + "'";
                    cmd.CommandText = commandStr;
                    SQLiteDataReader sr = cmd.ExecuteReader();
                    DataTable dTable = new DataTable();
                    dTable.Load(sr);
                    return dTable.Rows[0]["Testresult"].ToString();
                }
                else
                {
                    return string.Empty;
                }
            }
            catch(Exception ex)
            {
                return string.Empty;
            } 
        }
    }

}
