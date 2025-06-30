using KixDutyFree.App.Models;
using KixDutyFree.App.Models.Excel;
using Magicodes.ExporterAndImporter.Core.Extension;
using Magicodes.ExporterAndImporter.Excel;
using Microsoft.Extensions.Logging;
using QYQ.Base.Common.Extension;
using QYQ.Base.Common.IOCExtensions;
using System.IO;
using System.Threading;

namespace KixDutyFree.App.Manage
{
    public class ExcelProcess(ILogger<ExcelProcess> logger) : ISingletonDependency
    {

        private readonly SemaphoreSlim _lock = new(1, 1);

        /// <summary>
        /// 导出订单信息
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task OrderExportAsync(OrderExcel data)
        {
            try
            {
                await _lock.WaitAsync();
                try
                {
                    List<OrderExcel> list = [];
                    var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"订单_{DateTime.Now:yyyyMMdd}.xlsx");
                    if (File.Exists(path))
                    {
                        var importer = new ExcelImporter();
                        var importResult = await importer.Import<OrderExcel>(path, null);
                        if (importResult.Data != null && importResult.Data.Count > 0)
                        {
                            list.AddRange(importResult.Data);
                        }
                        File.Delete(path); // 删除原文件
                    }
                    // 添加新的订单数据并排序
                    list.Add(data);
                    list = list.OrderBy(i => i.CreateTime).ToList();
                    //导出表格
                    IExcelExporter exporter = new ExcelExporter();
                    var result = await exporter.Export(path, list);
                    //result.ToExcelExportFileInfo(path);
                }
                finally
                {
                    _lock.Release();
                }
            }
            catch (Exception e)
            {
                logger.BaseErrorLog("OrderExportAsync", e);
            }
        }
    }
}
