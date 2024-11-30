using SqlSugar;

namespace AixDutyFreeCrawler.App.Models.Entity
{
    /// <summary>
    /// 商品监控
    /// </summary>
    [SugarTable("product_monitor")]
    public class ProductMonitorEntity
    {
        /// <summary>
        /// 商品监控信息的唯一标识符
        /// </summary>
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int Id { get; set; }

        /// <summary>
        /// 账号
        /// </summary>
        public string Account { get;set; } = string.Empty;

        /// <summary>
        /// 商品Id
        /// </summary>
        public string ProductId { get; set; } = string.Empty;

        /// <summary>
        /// 订单当前的步骤状态
        /// </summary>
        public OrderSetup Setup { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 修改时间
        /// </summary>
        public DateTime UpdateTime { get; set; }
    }

    /// <summary>
    /// 表示订单当前所处的步骤
    /// </summary>
    public enum OrderSetup
    {
        /// <summary>
        /// 已将商品添加到购物车
        /// </summary>
        AddedToCart = 1,

        /// <summary>
        /// 已保存航班信息
        /// </summary>
        FlightInfoSaved = 2,

        /// <summary>
        /// 已提交支付信息
        /// </summary>
        PaymentSubmitted = 3,

        /// <summary>
        /// 订单已下单
        /// </summary>
        OrderPlaced = 4,

        /// <summary>
        /// 订单完成
        /// </summary>
        Completed = 5
    }
}
