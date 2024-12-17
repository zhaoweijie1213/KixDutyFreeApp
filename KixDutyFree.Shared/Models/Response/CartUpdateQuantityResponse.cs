using Newtonsoft.Json;

namespace KixDutyFree.App.Models.Response
{
    /// <summary>
    /// 修改购物车商品数量
    /// </summary>
    public class CartUpdateQuantityResponse
    {
        //[JsonProperty("action")]
        //public string? Action { get; set; }

        //[JsonProperty("queryString")]
        //public string? QueryString { get; set; }

        //[JsonProperty("locale")]
        //public string? Locale { get; set; }

        //[JsonProperty("hasBonusProduct")]
        //public bool? HasBonusProduct { get; set; }

        //[JsonProperty("actionUrls")]
        //public ActionUrls? ActionUrls { get; set; }

        //[JsonProperty("numOfShipments")]
        //public int? NumOfShipments { get; set; }

        //[JsonProperty("totals")]
        //public Totals? Totals { get; set; }

        //[JsonProperty("shipments")]
        //public List<Shipment>? Shipments { get; set; }

        //[JsonProperty("approachingDiscounts")]
        //public List<object>? ApproachingDiscounts { get; set; }

        //[JsonProperty("items")]
        //public List<Item>? Items { get; set; }

        //[JsonProperty("numItems")]
        //public int NumItems { get; set; }

        /// <summary>
        /// 验证信息
        /// </summary>
        [JsonProperty("valid")]
        public Valid? Valid { get; set; }

        //[JsonProperty("resources")]
        //public Resources? Resources { get; set; }

        //[JsonProperty("lotteryExclusive")]
        //public bool LotteryExclusive { get; set; }

        //[JsonProperty("isCategoryOrBrandLimitReached")]
        //public bool IsCategoryOrBrandLimitReached { get; set; }

        /// <summary>
        /// 修改的商品Id
        /// </summary>
        [JsonProperty("updatedPid")]
        public string? UpdatedPid { get; set; }

        /// <summary>
        /// 修购后的数量
        /// </summary>
        [JsonProperty("updatedQty")]
        public string? UpdatedQty { get; set; }
    }
}
