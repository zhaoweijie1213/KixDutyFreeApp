using AixDutyFreeCrawler.App.Manage;

namespace AixDutyFreeCrawler.App.Models.Response
{
    /// <summary>
    /// 添加到购物车返回结果
    /// </summary>
    public class CartAddProductResponse
    {
        public string? Action { get; set; }

        public string? QueryString { get; set; }

        public string? Locale { get; set; }

        public string? ReportingURL { get; set; }

        public int QuantityTotal { get; set; }

        public string? Message { get; set; }

        public Cart? Cart { get; set; }

        //public NewBonusDiscountLineItem? NewBonusDiscountLineItem { get; set; }

        public bool Error { get; set; }

        public string? PliUUID { get; set; }

        public string? MinicartCountOfItems { get; set; }

        public string? PdpStockErrorMsg { get; set; }

        public string? UpdatedPid { get; set; }
    }

    public class Cart
    {
        public bool HasBonusProduct { get; set; }

        public ActionUrls? ActionUrls { get; set; }

        public int NumOfShipments { get; set; }

        public Totals? Totals { get; set; }

        public List<Shipment>? Shipments { get; set; }

        public List<object>? ApproachingDiscounts { get; set; }

        public List<Item>? Items { get; set; }

        public int NumItems { get; set; }

        public Valid? Valid { get; set; }

        public CartAddProductResources? Resources { get; set; }

        public bool LotteryExclusive { get; set; }
    }

    public class ActionUrls
    {
        public string? RemoveProductLineItemUrl { get; set; }

        public string? UpdateQuantityUrl { get; set; }

        public string? SelectShippingUrl { get; set; }

        public string? SubmitCouponCodeUrl { get; set; }

        public string? RemoveCouponLineItem { get; set; }

        public string? GetFlightDataUrl { get; set; }

        public string? SaveFlightDataUrl { get; set; }

        public string? HomePageUrl { get; set; }
    }

    public class Totals
    {
        public int SubTotal { get; set; }

        public string? TotalShippingCost { get; set; }

        public int GrandTotal { get; set; }

        public string? TotalTax { get; set; }

        public DiscountTotal? OrderLevelDiscountTotal { get; set; }

        public DiscountTotal? ShippingLevelDiscountTotal { get; set; }

        public List<object>? Discounts { get; set; }

        public string? DiscountsHtml { get; set; }

        public string? SubTotalFormatted { get; set; }

        public int EstimatedTotal { get; set; }

        public string? EstimatedTotalFormatted { get; set; }

        public int ProductLevelDiscountTotal { get; set; }

        public string? ProductLevelDiscountTotalFormatted { get; set; }

        public int TotalSavedAmount { get; set; }

        public string? TotalSavedAmountFormatted { get; set; }

        public string? GrandTotalFormatted { get; set; }
    }

    public class DiscountTotal
    {
        public int Value { get; set; }
        public string? Formatted { get; set; }
    }

    public class Shipment
    {
        public List<object>? ShippingMethods { get; set; }

        public string? SelectedShippingMethod { get; set; }
    }

    public class Valid
    {
        public bool Error { get; set; }
        public object? Message { get; set; }
    }

    public class CartAddProductResources
    {
        public string? NumberOfItems { get; set; }

        public string? MinicartCountOfItems { get; set; }

        public string? EmptyCartMsg { get; set; }
    }

    public class Item
    {
        public string? BrandCode { get; set; }

        public string? BrandName { get; set; }

        public int SelectedQuantity { get; set; }

        public string? MinOrderQuantity { get; set; }

        public string? MaxOrderQuantity { get; set; }

        public CardAddProductAvailability? Availability { get; set; }

        public bool Available { get; set; }

        public bool PlusButtonDisabled { get; set; }

        public bool MinusButtonDisabled { get; set; }

        public List<Quantity>? Quantities { get; set; }

        public string? Uuid { get; set; }

        public string? Id { get; set; }

        public string? ProductName { get; set; }

        public string? ProductType { get; set; }

        public string? Brand { get; set; }

        public PriceInfo? Price { get; set; }

        public string? RenderedPrice { get; set; }

        public Images? Images { get; set; }

        public object? VariationAttributes { get; set; }

        public int Quantity { get; set; }

        public bool IsGift { get; set; }

        public List<PromotionInfo>? ApplicablePromotions { get; set; }

        public string? RenderedPromotions { get; set; }

        public string? UUID { get; set; }

        public bool IsOrderable { get; set; }

        public string? ShipmentUUID { get; set; }

        public bool IsBonusProductLineItem { get; set; }

        public PriceTotal? PriceTotal { get; set; }

        public QuantityOptions? QuantityOptions { get; set; }

        public List<object>? Options { get; set; }

        public object? BonusProductLineItemUUID { get; set; }

        public object? PreOrderUUID { get; set; }

        public List<object>? DiscountLineItems { get; set; }

        public bool IsAllowSameDayReservation { get; set; }

        public bool IsAllowSameDayPickUp { get; set; }

        public bool IsAgeVerificationRequired { get; set; }
        public string? ProductUrl { get; set; }

        public object? IsLotteryExclusive { get; set; }

        public List<Category>? Categories { get; set; }

        public bool IsSameDayPickup { get; set; }

        public List<PromotionInfo>? Promotions { get; set; }

        public List<object>? BonusProducts { get; set; }
    }

    public class CardAddProductAvailability
    {
        public bool Available { get; set; }

        public string? MaxOrderQuantity { get; set; }

        public bool Quantity { get; set; }

        public List<Message>? Messages { get; set; }

        public string? MaxQuantitySafetyStock { get; set; }

        public bool IsMaxQuantityLimitedStock { get; set; }

        public int MaxQuantityLimitedStock { get; set; }

        public string? Status { get; set; }

        public bool IsQuantityLimited { get; set; }
    }


    public class PriceInfo
    {
        public Sales? Sales { get; set; }

        public object? List { get; set; }
    }



    public class PromotionInfo
    {
        public string? Id { get; set; }

        public string? Name { get; set; }

        public object? CallOutMsg { get; set; }

        public string? Details { get; set; }

        public string? PromotionClass { get; set; }
    }

    public class PriceTotal
    {
        public string? Price { get; set; }

        public string? RenderedPrice { get; set; }
    }

    public class QuantityOptions
    {
        public int MinOrderQuantity { get; set; }
        public int MaxOrderQuantity { get; set; }
    }

}
