﻿using BackTestingPlatform.Model.Common;
using BackTestingPlatform.Utilities;
using NLog;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackTestingPlatform.DataAccess
{
    /// <summary>
    /// 按每天存取时间序列数据的Repository
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class SequentialByDayRepository<T> where T : Sequential, new()
    {
        const string PATH_KEY = "CacheData.Path.SequentialByDay";
        Logger log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// 由DataTable中的行向实体类的转换函数的默认实现。
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        public virtual T toEntityFromCsv(DataRow row)
        {
            return DataTableUtils.CreateItemFromRow<T>(row);
        }

        /// <summary>
        ///  尝试从Wind获取数据,可能会抛出异常
        /// </summary>
        /// <param name="code"></param>
        /// <param name="date"></param>
        /// <returns></returns>
        protected abstract List<T> readFromWind(string code, DateTime date);

        /// <summary>
        /// 尝试从默认MSSQL源获取数据,可能会抛出异常
        /// </summary>
        /// <param name="code"></param>
        /// <param name="date"></param>
        /// <returns></returns>
        protected abstract List<T> readFromDefaultMssql(string code, DateTime date);

        /// <summary>
        ///  尝试从本地csv文件获取数据,可能会抛出异常
        /// </summary>
        /// <param name="code"></param>
        /// <param name="date"></param>
        /// <param name="tag"></param>
        /// <returns></returns>
        public List<T> readFromLocalCsv(string code, DateTime date, string tag = null)
        {
            if (tag == null) tag = typeof(T).ToString();
            var filePath = FileUtils.GetCacheDataFilePath(PATH_KEY, tag, code, date.ToString("yyyyMMdd"));
            DataTable dt = CsvFileUtils.ReadFromCsvFile(filePath);
            if (dt == null) return null;
            return dt.AsEnumerable().Select(toEntityFromCsv).ToList();
        }

        /// <summary>
        /// 尝试从本地csv文件，Wind获取数据。
        /// </summary>
        /// <param name="code"></param>
        /// <param name="date"></param>
        /// <param name="tag"></param>
        /// <returns></returns>
        public List<T> fetchFromLocalCsv(string code, DateTime date, string tag = null)
        {
            return fetch0(code, date, tag, true, false, false, false);
        }

        /// <summary>
        /// 尝试从Wind获取数据。
        /// </summary>
        /// <param name="code"></param>
        /// <param name="date"></param>
        /// <param name="tag"></param>
        /// <returns></returns>
        public List<T> fetchFromWind(string code, DateTime date, string tag = null)
        {
            return fetch0(code, date, tag, false, true, false, false);
        }

        /// <summary>
        /// 尝试从默认MSSQL源获取数据。
        /// </summary>
        /// <param name="code"></param>
        /// <param name="date"></param>
        /// <param name="tag"></param>
        /// <returns></returns>
        public List<T> fetchFromMssql(string code, DateTime date, string tag = null)
        {
            return fetch0(code, date, tag, false, false, true, false);
        }

        /// <summary>
        /// 先后尝试从本地csv文件，Wind获取数据。若无本地csv，则保存到CacheData文件夹。
        /// </summary>
        /// <param name="code"></param>
        /// <param name="date"></param>
        /// <param name="tag"></param>
        /// <returns></returns>
        public List<T> fetchFromLocalCsvOrWindAndSave(string code, DateTime date, string tag = null)
        {
            return fetch0(code, date, tag, true, true, false, true);
        }
        /// <summary>
        /// 先后尝试从本地csv文件，默认MSSQL数据库源获取数据。
        /// </summary>
        /// <param name="code"></param>
        /// <param name="date"></param>
        /// <param name="tag"></param>
        /// <returns></returns>
        public List<T> fetchFromLocalCsvOrMssql(string code, DateTime date, string tag = null)
        {
            return fetch0(code, date, tag, true, false, true, false);
        }

        /// <summary>
        /// 先后尝试从本地csv文件，默认MSSQL数据库源获取数据。若无本地csv，则保存到CacheData文件夹。
        /// </summary>
        /// <param name="code"></param>
        /// <param name="date"></param>
        /// <param name="tag"></param>
        /// <returns></returns>
        public List<T> fetchFromLocalCsvOrMssqlAndSave(string code, DateTime date, string tag = null)
        {
            return fetch0(code, date, tag, true, false, true, true);
        }

        /// <summary>
        /// 尝试Wind获取数据。然后将数据覆盖保存到CacheData文件夹
        /// </summary>
        /// <param name="code"></param>
        /// <param name="date"></param>
        /// <param name="tag"></param>
        /// <returns></returns>
        public List<T> fetchFromWindAndSave(string code, DateTime date, string tag = null)
        {
            return fetch0(code, date, tag, false, true, false, true);
        }

        private List<T> fetch0(string code, DateTime date, string tag, bool tryCsv, bool tryWind, bool tryMssql0, bool saveToCsv)
        {
            if (tag == null) tag = typeof(T).ToString();
            List<T> result = null;
            bool csvHasData = false;

            if (tryCsv)
            {
                //尝试从csv获取
                log.Info("尝试从csv获取...");
                try
                {
                    result = readFromLocalCsv(code, date, tag);
                }
                catch (Exception e)
                {
                    log.Error(e, "尝试从csv获取失败！");
                }
                if (result != null) csvHasData = true;
            }
            if (result == null && tryWind)
            {
                //尝试从Wind获取
                log.Info("尝试从Wind获取...");
                try
                {
                    result = readFromWind(code, date);
                }
                catch (Exception e)
                {
                    log.Error(e, "尝试从Wind获取失败！");
                }
            }
            if (result == null && tryMssql0)
            {
                try
                {
                    //尝试从默认MSSQL源获取
                    log.Info("尝试从默认MSSQL源获取...");
                    result = readFromDefaultMssql(code, date);
                }
                catch (Exception e)
                {
                    log.Error(e, "尝试从默认MSSQL源获取失败！");
                }

            }
            if (!csvHasData && result != null && saveToCsv)
            {
                //如果数据不是从csv获取的，可保存至本地，存为csv文件
                log.Info("正在保存到本地csv文件...");
                saveToLocalCsvFile(result, code, date, tag);
            }
            log.Info("获取{0}数据列表成功.共{1}行.", tag, result.Count);
            return result;
        }

        /// <summary>
        /// 将数据以csv文件的形式保存到CacheData文件夹下的预定路径
        /// </summary>
        /// <param name="data"></param>
        /// <param name="code"></param>
        /// <param name="date"></param>
        /// <param name="tag"></param>
        public void saveToLocalCsvFile(IList<T> data, string code, DateTime date, string tag = null)
        {
            if (tag == null) tag = typeof(T).ToString();
            if (data == null || data.Count == 0)
            {
                log.Warn("没有任何内容可以保存到csv！");
                return;
            }
            var dt = DataTableUtils.ToDataTable(data);
            var path = FileUtils.GetCacheDataFilePath(PATH_KEY, tag, code, date.ToString("yyyyMMdd"));
            try
            {
                var s = (File.Exists(path)) ? "覆盖" : "新增";
                CsvFileUtils.WriteToCsvFile(path, dt);
                log.Info("文件已{0}：{1} ", s, path);
            }
            catch (Exception e)
            {
                log.Error(e, "保存到本地csv文件失败！");
            }


        }
    }
}
