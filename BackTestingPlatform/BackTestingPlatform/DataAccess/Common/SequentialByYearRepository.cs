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
    /// 按每年存取时间序列数据的Repository
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class SequentialByYearRepository<T> : SequentialRepository<T> where T : Sequential, new()
    {
        const string PATH_KEY = "CacheData.Path.SequentialByYear";
        static Logger log = LogManager.GetCurrentClassLogger();


        /// <summary>
        /// 尝试从Wind获取数据,可能会抛出异常
        /// </summary>
        /// <param name="code">代码，如股票代码，期权代码</param>
        /// <param name="dateStart">开始时间，包含本身</param>
        /// <param name="dateEnd">结束时间，包含本身</param>
        /// <param name="tag">读写文件路径前缀，若为空默认为类名</param>
        /// <param name="options">其他选项</param>
        /// <returns></returns>
        protected abstract List<T> readFromWind(string code, DateTime dateStart, DateTime dateEnd, string tag = null, IDictionary<string, object> options = null);


        /// <summary>
        /// 尝试从默认MSSQL源获取数据,可能会抛出异常
        /// </summary>
        /// <param name="code">代码，如股票代码，期权代码</param>
        /// <param name="dateStart">开始时间，包含本身</param>
        /// <param name="dateEnd">结束时间，包含本身</param>
        /// <param name="tag">读写文件路径前缀，若为空默认为类名</param>
        /// <param name="options">其他选项</param>
        /// <returns></returns>
        protected abstract List<T> readFromDefaultMssql(string code, DateTime dateStart, DateTime dateEnd, string tag = null, IDictionary<string, object> options = null);


        /// <summary>
        /// 尝试从本地csv文件获取数据,可能会抛出异常
        /// </summary>
        /// <param name="code">代码，如股票代码，期权代码</param>
        /// <param name="dateStart">开始时间，包含本身</param>
        /// <param name="dateEnd">结束时间，包含本身</param>
        /// <param name="tag">读写文件路径前缀，若为空默认为类名</param>
        /// <param name="options">其他选项</param>
        /// <returns></returns>
        public List<T> readFromLocalCsv(string code, DateTime date1, DateTime date2, string tag = null, IDictionary<string, object> options = null)
        {
            var filePath = _buildCacheDataFilePath(code, date1, date2, tag);
            return readFromLocalCsv(filePath);
        }

        /// <summary>
        /// 尝试从本地csv文件，Wind获取数据。
        /// </summary>
        /// <param name="code">代码，如股票代码，期权代码</param>
        /// <param name="year">年份，默认获取从该年的1月1日到12月31日</param>      
        /// <param name="tag">读写文件路径前缀，若为空默认为类名</param>
        /// <param name="options">其他选项</param>
        /// <returns></returns>
        public List<T> fetchFromLocalCsv(string code, int year, string tag = null, IDictionary<string, object> options = null)
        {
            return fetch0(code, year, tag, options, true, false, false, false);
        }

        /// <summary>
        /// 尝试从Wind获取数据。
        /// </summary>
        /// <param name="code">代码，如股票代码，期权代码</param>
        /// <param name="year">年份，默认获取从该年的1月1日到12月31日</param>      
        /// <param name="tag">读写文件路径前缀，若为空默认为类名</param>
        /// <param name="options">其他选项</param>
        /// <returns></returns>
        public List<T> fetchFromWind(string code, int year, string tag = null, IDictionary<string, object> options = null)
        {
            return fetch0(code, year, tag, options, false, true, false, false);
        }

        /// <summary>
        /// 尝试从默认MSSQL源获取数据。
        /// </summary>
        /// <param name="code">代码，如股票代码，期权代码</param>
        /// <param name="year">年份，默认获取从该年的1月1日到12月31日</param>      
        /// <param name="tag">读写文件路径前缀，若为空默认为类名</param>
        /// <param name="options">其他选项</param>
        /// <returns></returns>
        public List<T> fetchFromMssql(string code, int year, string tag = null, IDictionary<string, object> options = null)
        {
            return fetch0(code, year, tag, options, false, false, true, false);
        }

        /// <summary>
        /// 先后尝试从本地csv文件，Wind获取数据。若无本地csv，则保存到CacheData文件夹。
        /// </summary>
        /// <param name="code">代码，如股票代码，期权代码</param>
        /// <param name="year">年份，默认获取从该年的1月1日到12月31日</param>      
        /// <param name="tag">读写文件路径前缀，若为空默认为类名</param>
        /// <param name="options">其他选项</param>
        /// <returns></returns>
        public List<T> fetchFromLocalCsvOrWindAndSave(string code, int year, string tag = null, IDictionary<string, object> options = null)
        {
            return fetch0(code, year, tag, options, true, true, false, true);
        }
        /// <summary>
        /// 先后尝试从本地csv文件，默认MSSQL数据库源获取数据。
        /// </summary>
        /// <param name="code">代码，如股票代码，期权代码</param>
        /// <param name="year">年份，默认获取从该年的1月1日到12月31日</param>      
        /// <param name="tag">读写文件路径前缀，若为空默认为类名</param>
        /// <param name="options">其他选项</param>
        /// <returns></returns>
        public List<T> fetchFromLocalCsvOrMssql(string code, int year, string tag = null, IDictionary<string, object> options = null)
        {
            return fetch0(code, year, tag, options, true, false, true, false);
        }

        /// <summary>
        /// 先后尝试从本地csv文件，默认MSSQL数据库源获取数据。若无本地csv，则保存到CacheData文件夹。
        /// </summary>
        /// <param name="code">代码，如股票代码，期权代码</param>
        /// <param name="year">年份，默认获取从该年的1月1日到12月31日</param>      
        /// <param name="tag">读写文件路径前缀，若为空默认为类名</param>
        /// <param name="options">其他选项</param>
        /// <returns></returns>
        public List<T> fetchFromLocalCsvOrMssqlAndSave(string code, int year, string tag = null, IDictionary<string, object> options = null)
        {
            return fetch0(code, year, tag, options, true, false, true, true);
        }

        /// <summary>
        /// 尝试Wind获取数据。然后将数据覆盖保存到CacheData文件夹
        /// </summary>
        /// <param name="code">代码，如股票代码，期权代码</param>
        /// <param name="year">年份，默认获取从该年的1月1日到12月31日</param>      
        /// <param name="tag">读写文件路径前缀，若为空默认为类名</param>
        /// <param name="options">其他选项</param>
        /// <returns></returns>
        public List<T> fetchFromWindAndSave(string code, int year, string tag = null, IDictionary<string, object> options = null)
        {
            return fetch0(code, year, tag, options, false, true, false, true);
        }

        /// <summary>
        /// 内部函数
        /// </summary>
        /// <param name="code"></param>
        /// <param name="year"></param>
        /// <param name="tag"></param>
        /// <param name="options"></param>
        /// <param name="tryCsv"></param>
        /// <param name="tryWind"></param>
        /// <param name="tryMssql0"></param>
        /// <param name="saveToCsv"></param>
        /// <returns></returns>
        private List<T> fetch0(string code, int year, string tag, IDictionary<string, object> options, bool tryCsv, bool tryWind, bool tryMssql0, bool saveToCsv)
        {
            if (tag == null) tag = typeof(T).ToString();
            List<T> result = null;
            bool csvHasData = false;
            var date1 = new DateTime(year, 1, 1);
            var date2 = new DateTime(year, 12, 31);
            log.Debug("正在获取{0}数据列表...", Kit.ToShortName(tag));
            if (tryCsv)
            {
                //尝试从csv获取
                log.Debug("尝试从csv获取...");
                try
                {
                    //result返回空集表示本地csv文件中没有数据，null表示本地csv不存在
                    result = readFromLocalCsv(code, date1, date2, tag, options);
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
                log.Debug("尝试从Wind获取...");
                try
                {                    
                    result = readFromWind(code, date1, date2, tag, options);
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
                    log.Debug("尝试从默认MSSQL源获取...");
                    result = readFromDefaultMssql(code, date1, date2, tag, options);
                }
                catch (Exception e)
                {
                    log.Error(e, "尝试从默认MSSQL源获取失败！");
                }

            }
            if (!csvHasData && result != null && saveToCsv)
            {
                //如果数据不是从csv获取的，可保存至本地，存为csv文件
                log.Debug("正在保存到本地csv文件...");
                saveToLocalCsv(result, code, date1, date2, tag);
            }
            log.Info("获取数据列表{0}(year={1})成功.共{2}行.", Kit.ToShortName(tag), year, result.Count);
            return result;
        }


        /// <summary>
        /// 将数据以csv文件的形式保存到CacheData文件夹下的预定路径
        /// </summary>
        /// <param name="data">要保存的数据</param>
        /// <param name="date1">开始时间，包含本身</param>
        /// <param name="date2">结束时间，包含本身</param>
        /// <param name="tag">读写文件路径前缀，若为空默认为类名</param>
        /// <param name="appendMode">是否为追加的文件尾部模式，否则是覆盖模式</param>
        public void saveToLocalCsv(IList<T> data, string code, DateTime date1, DateTime date2, string tag = null, bool appendMode = false)
        {
            var path = _buildCacheDataFilePath(code, date1, date2, tag);
            saveToLocalCsv(path, data, appendMode);
        }


        private static string _buildCacheDataFilePath(string code, DateTime date1, DateTime date2, string tag)
        {
            if (tag == null) tag = typeof(T).ToString();
            return FileUtils.GetCacheDataFilePath(PATH_KEY, new Dictionary<string, string>
            {
                ["{tag}"] = tag,
                ["{code}"] = code,
                ["{date1}"] = date1.ToString("yyyyMMdd"),
                ["{date2}"] = date2.ToString("yyyyMMdd")
            });
        }
    }
}
