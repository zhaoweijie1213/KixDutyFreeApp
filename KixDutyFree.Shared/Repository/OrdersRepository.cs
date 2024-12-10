using KixDutyFree.App.Repository;
using KixDutyFree.Shared.Models.Entity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QYQ.Base.Common.IOCExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KixDutyFree.Shared.Repository
{
    public class OrdersRepository(ILogger<OrdersRepository> logger, IConfiguration configuration)
        : BaseRepository<OrdersEntity>(logger, configuration.GetConnectionString("DefaultConnection")!), ITransientDependency
    {
        public Task<List<OrdersEntity>> QueryAsync()
        {
            return Db.Queryable<OrdersEntity>().ToListAsync();
        }
    }
    
}
