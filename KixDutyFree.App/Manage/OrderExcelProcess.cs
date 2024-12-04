using KixDutyFree.App.Models;
using KixDutyFree.App.Models.Excel;
using Magicodes.ExporterAndImporter.Core.Extension;
using Magicodes.ExporterAndImporter.Excel;
using QYQ.Base.Common.Extension;
using QYQ.Base.Common.IOCExtensions;
using System.IO;

namespace KixDutyFree.App.Manage
{
    public class ExcelProcess(ILogger<ExcelProcess> logger) : ISingletonDependency
    {

        private readonly object _lock = new();

        /// <summary>
        /// 导出订单信息
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public Task OrderExportAsync(OrderExcel data)
        {
            try
            {
                lock (_lock)
                {
                    List<OrderExcel> list = [];
                    var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"订单_{DateTime.Now:yyyyMMdd}.xlsx");
                    if (File.Exists(path))
                    {
                        var importer = new ExcelImporter();
                        var importResult = importer.Import<OrderExcel>(path, null).GetAwaiter().GetResult();
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
                    var result = exporter.Export(path, list).GetAwaiter().GetResult(); ;
                    //result.ToExcelExportFileInfo(path);
                }
            }
            catch (Exception e)
            {
                logger.BaseErrorLog("OrderExportAsync", e);
            }

            return Task.CompletedTask;
        }
    }
}
