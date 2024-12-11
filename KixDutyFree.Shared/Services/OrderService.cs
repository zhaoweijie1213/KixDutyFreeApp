using KixDutyFree.Shared.Models.Entity;
using KixDutyFree.Shared.Repository;
using QYQ.Base.Common.IOCExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KixDutyFree.Shared.Services
{
    public class OrderService(OrdersRepository ordersRepository) : ITransientDependency
    {
        public async Task<List<OrdersEntity>> GetOrdersAsync()
        {
            return await ordersRepository.QueryAsync();
        }

        public async Task AddOrderAsync(OrdersEntity entity)
        {
            await ordersRepository.InsertAsync(entity);
        }
    }
}
