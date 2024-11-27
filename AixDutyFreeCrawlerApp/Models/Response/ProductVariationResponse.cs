using System.Resources;

namespace AixDutyFreeCrawler.App.Models.Response
{
    public class ProductVariationResponse
    {
        /// <summary>
        /// 
        /// </summary>
        public string? Action { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? QueryString { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? Locale { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public Product? Product { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public Resources? Resources { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public WishlistDetails? WishlistDetails { get; set; }
    }

    public class Product
    {
        /// <summary>
        /// 
        /// </summary>
        public string? BrandName { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? BrandCode { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public bool IsSameDayPickup { get; set; }

        public RetailPrice? RetailPrice { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public List<AttributeTable>? AttributeTable { get; set; }

        public string? Uuid { get; set; }

        public string? Id { get; set; }

        public string? ProductName { get; set; }

        public string? ProductType { get; set; }

        public string? Brand { get; set; }

        public Price? Price { get; set; }

        public string? RenderedPrice { get; set; }

        public Images? Images { get; set; }

        public int SelectedQuantity { get; set; }

        public string? MinOrderQuantity { get; set; }

        public string? MaxOrderQuantity { get; set; }

        public List<VariationAttribute>? VariationAttributes { get; set; }

        public string? LongDescription { get; set; }

        public string? ShortDescription { get; set; }

        public double Rating { get; set; }

        public List<Promotion>? Promotions { get; set; }

        public List<Attribute>? Attributes { get; set; }

        /// <summary>
        /// 可用性
        /// </summary>
        public Availability? Availability { get; set; }

        /// <summary>
        /// 是否可用
        /// </summary>
        public bool Available { get; set; }

        public bool PlusButtonDisabled { get; set; }

        public bool MinusButtonDisabled { get; set; }

        //public List<Option>? Options { get; set; }

        public List<Quantity>? Quantities { get; set; }

        public string? SelectedProductUrl { get; set; }

        public bool ReadyToOrder { get; set; }

        public bool Online { get; set; }
        public string? PageTitle { get; set; }

        public string? PageDescription { get; set; }

        public string? PageKeywords { get; set; }

        //public List<PageMetaTag>? PageMetaTags { get; set; }

        public string? Template { get; set; }

        public string? Franchise { get; set; }
        public List<Category>? Categories { get; set; }
        public string? AttributesHtml { get; set; }
        public string? PromotionsHtml { get; set; }
        public string? OptionsHtml { get; set; }
    }

    public class RetailPrice
    {
        public bool Available { get; set; }
        public string? Html { get; set; }
    }

    public class AttributeTable
    {
        public bool Available { get; set; }
        public string? Value { get; set; }
        public string? Label { get; set; }
    }

    public class Price
    {
        public Sales? Sales { get; set; }
        public string? List { get; set; }
        public string? Html { get; set; }
    }

    public class Sales
    {
        public int Value { get; set; }
        public string? Currency { get; set; }
        public string? Formatted { get; set; }
        public string? DecimalPrice { get; set; }
    }

    public class Images
    {
        public List<ImageDetail>? Large { get; set; }
        public List<ImageDetail>? Small { get; set; }
        public List<ImageDetail>? Medium { get; set; }
    }

    public class ImageDetail
    {
        public string? Index { get; set; }

        public string? Alt { get; set; }

        public string? Title { get; set; }

        public string? Url { get; set; }

        public string? AbsUrl { get; set; }
    }

    public class VariationAttribute
    {
        // Add properties if needed
    }

    public class Promotion
    {
        public string? CalloutMsg { get; set; }

        public string? Details { get; set; }

        public bool Enabled { get; set; }

        public string? Id { get; set; }

        public string? Name { get; set; }

        public string? PromotionClass { get; set; }

        public int? Rank { get; set; }
    }

    public class Attribute
    {
        public string? ID { get; set; }

        public string? Name { get; set; }

        public List<AttributeDetail>? Attributes { get; set; }
    }

    public class AttributeDetail
    {
        public string? Label { get; set; }

        public List<string>? Value { get; set; }
    }

    public class Availability
    {
        public bool Available { get; set; }

        public int MaxOrderQuantity { get; set; }

        public bool Quantity { get; set; }

        public List<Message>? Messages { get; set; }

        public string? Status { get; set; }

        public bool IsQuantityLimited { get; set; }
    }

    public class Message
    {
        public string? ClassName { get; set; }

        public InfoMsg? InfoMsg { get; set; }

        public WarningMsg? WarningMsg { get; set; }
    }

    public class InfoMsg
    {
        public bool Display { get; set; }

        public string? Header { get; set; }

        public string? Text { get; set; }
    }

    public class WarningMsg
    {
        public bool DisplayHeader { get; set; }

        public string? Header { get; set; }

        public bool DisplayText { get; set; }

        public string? Text { get; set; }

        public bool IsThresholdOver { get; set; }

        public string? ThresholdOverWarningText { get; set; }
    }

    public class Option
    {
        // Add properties if needed
    }

    public class Quantity
    {
        public string? Value { get; set; }
        public bool Selected { get; set; }
        public string? Url { get; set; }
    }

    public class PageMetaTag
    {
        // Add properties if needed
    }

    public class Category
    {
        public string? HtmlValue { get; set; }

        //public Url? Url { get; set; }

        public string? Id { get; set; }
    }

    public class Url
    {
        // Add properties if needed
    }

    public class Resources
    {
        public string? InfoSelectforstock { get; set; }
        public string? AssistiveSelectedText { get; set; }
    }

    public class WishlistDetails
    {
        public bool IsItemExists { get; set; }

        public string? Pid { get; set; }

        public bool IsMaster { get; set; }
    }

}
