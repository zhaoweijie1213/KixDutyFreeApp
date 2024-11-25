namespace AixDutyFreeCrawler.App.Services
{

    /// <summary>
    /// 结账地址
    /// </summary>
    public static class CheckoutServices
    {
        /// <summary>
        /// 保存航班信息
        /// </summary>
        public const string FlightSaveInfo = "https://www.kixdutyfree.jp/on/demandware.store/Sites-KixDutyFree-Site/zh_CN/Flight-SaveInfo";

        /// <summary>
        /// 提交支付信息
        /// </summary>
        public const string SubmitPayment= "https://www.kixdutyfree.jp/on/demandware.store/Sites-KixDutyFree-Site/zh_CN/CheckoutServices-SubmitPayment";

        /// <summary>
        /// 下单
        /// </summary>
        public const string PlaceOrder = "https://www.kixdutyfree.jp/on/demandware.store/Sites-KixDutyFree-Site/zh_CN/CheckoutServices-PlaceOrder";
    }

    /// <summary>
    /// 购物车
    /// </summary>
    public static class Cart
    {
        /// <summary>
        /// 商品数量变化
        /// </summary>
        public const string ProductVariation = "https://www.kixdutyfree.jp/on/demandware.store/Sites-KixDutyFree-Site/zh_CN/Product-Variation?pid=2411900004&quantity=20";

        /// <summary>
        /// 添加到购物车
        /// </summary>
        public const string AddProduct = "https://www.kixdutyfree.jp/on/demandware.store/Sites-KixDutyFree-Site/zh_CN/Cart-AddProduct";

        /// <summary>
        /// 修改购物车商品数量
        /// </summary>
        public const string UpdateQuantity = "https://www.kixdutyfree.jp/on/demandware.store/Sites-KixDutyFree-Site/zh_CN/Cart-UpdateQuantity";
    }
}
