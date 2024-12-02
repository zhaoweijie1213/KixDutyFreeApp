using Newtonsoft.Json;

namespace KixDutyFree.App.Models.Response
{
    public class SubmitPaymentResponse
    {
        public string? Action { get; set; }

        public string? QueryString { get; set; }

        public string? Locale { get; set; }

        public Address? Address { get; set; }

        public Email? Email { get; set; }

        public Phone? Phone { get; set; }

        public PaymentMethod? PaymentMethod { get; set; }

        public object? RenderedPaymentInstruments { get; set; }

        public Customer? Customer { get; set; }

        public Order? Order { get; set; }

        public SubmitPaymentResponseForm? Form { get; set; }

        public bool Error { get; set; }
    }

    public class Address
    {
        public FieldValue? FirstName { get; set; }
    }

    public class Email
    {
        public string? Value { get; set; }
    }

    public class Phone
    {
        public object? Value { get; set; }
    }

    public class PaymentMethod
    {
        public string? Value { get; set; }

        public string? HtmlName { get; set; }
    }

    public class FieldValue
    {
        public object? Value { get; set; }
    }

    public class Customer
    {
        public Profile? Profile { get; set; }

        public List<AddressInfo>? Addresses { get; set; }

        public AddressInfo? PreferredAddress { get; set; }

        public object? Payment { get; set; }

        public bool RegisteredUser { get; set; }

        public bool IsExternallyAuthenticated { get; set; }

        public List<object>? CustomerPaymentInstruments { get; set; }
    }

    public class Profile
    {
        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        public string? Email { get; set; }

        public string? EmailConfirm { get; set; }

        public string? Birthday { get; set; }

        public string? PhonePrefix { get; set; }

        public string? PhonePrefixNumber { get; set; }

        public string? Phone { get; set; }

        public bool EmailOptIn { get; set; }

        public string? Password { get; set; }
    }

    public class AddressInfo
    {
        public object? Address1 { get; set; }

        public object? Address2 { get; set; }

        public object? City { get; set; }

        public object? FirstName { get; set; }

        public object? LastName { get; set; }

        public string? ID { get; set; }

        public string? AddressId { get; set; }

        public object? Phone { get; set; }
        public object? PostalCode { get; set; }

        public object? StateCode { get; set; }

        public object? PostBox { get; set; }

        public object? Salutation { get; set; }

        public object? SecondName { get; set; }

        public object? CompanyName { get; set; }

        public object? Suffix { get; set; }

        public object? Suite { get; set; }

        public object? Title { get; set; }

        public CountryCode? CountryCode { get; set; }
    }

    public class CountryCode
    {
        public string? DisplayValue { get; set; }
        public string? Value { get; set; }
    }

    public class Order
    {
        public SubmitPaymentResources? Resources { get; set; }

        public bool Shippable { get; set; }

        public bool UsingMultiShipping { get; set; }

        public object? OrderNumber { get; set; }

        public string? PriceTotal { get; set; }

        public string? CreationDate { get; set; }

        public string? OrderEmail { get; set; }

        public object? OrderStatus { get; set; }

        public int ProductQuantityTotal { get; set; }

        public Totals? Totals { get; set; }
        public Steps? Steps { get; set; }

        public OrderItems? Items { get; set; }

        public Billing? Billing { get; set; }

        public List<Shipping>? Shipping { get; set; }

        public object? CustomerName { get; set; }

        public SubmitPaymentFlightData? FlightData { get; set; }

        public string? ReservationType { get; set; }
    }

    public class SubmitPaymentResources
    {
        public string? NoSelectedPaymentMethod { get; set; }
        public string? CardType { get; set; }
        public string? CardEnding { get; set; }
        public string? ShippingMethod { get; set; }
        public string? Items { get; set; }
        public string? Item { get; set; }
        public string? AddNewAddress { get; set; }
        public string? NewAddress { get; set; }
        public string? ShipToAddress { get; set; }
        public string? ShippingAddresses { get; set; }
        public string? AccountAddresses { get; set; }
        public string? ShippingTo { get; set; }
        public string? ShippingAddress { get; set; }
        public string? AddressIncomplete { get; set; }
        public string? GiftMessage { get; set; }
    }

    public class SubmitPaymentTotals
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

    public class Steps
    {
        public Step? Shipping { get; set; }
        public Step? Billing { get; set; }
    }

    public class Step
    {
        [JsonProperty("iscompleted")]
        public bool IsCompleted { get; set; }
    }

    public class OrderItems
    {
        public List<SubmitPaymentItem>? Items { get; set; }

        public int TotalQuantity { get; set; }
    }

    public class SubmitPaymentItem
    {
        public string? BrandCode { get; set; }

        public string? BrandName { get; set; }

        public int SelectedQuantity { get; set; }

        public string? MinOrderQuantity { get; set; }

        public string? MaxOrderQuantity { get; set; }

        public SubmitPaymentAvailability? Availability { get; set; }

        public bool Available { get; set; }

        public bool PlusButtonDisabled { get; set; }

        public bool MinusButtonDisabled { get; set; }

        public List<Quantity>? Quantities { get; set; }

        public string? Uuid { get; set; }

        public string? Id { get; set; }

        public string? ProductName { get; set; }

        public string? ProductType { get; set; }

        public string? Brand { get; set; }

        public SubmitPaymentPrice? Price { get; set; }

        public string? RenderedPrice { get; set; }

        public Images? Images { get; set; }

        public object? VariationAttributes { get; set; }

        public int Quantity { get; set; }

        public bool IsGift { get; set; }

        public List<Promotion>? ApplicablePromotions { get; set; }

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

        public List<Promotion>? Promotions { get; set; }

        public object? BonusProducts { get; set; }
    }

    public class SubmitPaymentAvailability
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

    public class SubmitPaymentPrice
    {
        public SubmitPaymentSales? Sales { get; set; }
        public object? List { get; set; }
    }

    public class SubmitPaymentSales
    {
        public int Value { get; set; }
        public string? Currency { get; set; }
        public string? Formatted { get; set; }
        public string? DecimalPrice { get; set; }
    }

    public class UrlInfo
    {
        // 如果有具体的属性，可以在此添加
    }

    public class Billing
    {
        public BillingAddress? BillingAddress { get; set; }

        public Payment? Payment { get; set; }

        public string? MatchingAddressId { get; set; }
    }

    public class BillingAddress
    {
        public AddressInfo? Address { get; set; }
    }

    public class Payment
    {
        public List<PaymentMethodInfo>? ApplicablePaymentMethods { get; set; }

        public List<PaymentCard>? ApplicablePaymentCards { get; set; }

        public List<SelectedPaymentInstrument>? SelectedPaymentInstruments { get; set; }
    }

    public class PaymentMethodInfo
    {
        public string? ID { get; set; }

        public string? Name { get; set; }
    }

    public class PaymentCard
    {
        public string? CardType { get; set; }
        public string? Name { get; set; }
    }

    public class SelectedPaymentInstrument
    {
        public string? PaymentMethod { get; set; }
        public int Amount { get; set; }
        public string? PaymentMethodName { get; set; }
    }

    public class Shipping
    {
        public string? UUID { get; set; }

        public ProductLineItems? ProductLineItems { get; set; }

        public List<object>? ApplicableShippingMethods { get; set; }

        public SelectedShippingMethod? SelectedShippingMethod { get; set; }

        public string? MatchingAddressId { get; set; }

        public object? ShippingAddress { get; set; }

        public bool IsGift { get; set; }

        public object? GiftMessage { get; set; }
    }

    public class ProductLineItems
    {
        public List<Item>? Items { get; set; }

        public int TotalQuantity { get; set; }
    }

    public class SelectedShippingMethod
    {
        public string? ID { get; set; }

        public string? DisplayName { get; set; }

        public string? Description { get; set; }

        public object? EstimatedArrivalTime { get; set; }

        public bool Default { get; set; }

        public string? ShippingCost { get; set; }

        public bool Selected { get; set; }
    }

    public class SubmitPaymentFlightData
    {
        public string? AirlinesNo { get; set; }

        public string? FlightNo { get; set; }

        public string? ConnectingFlight { get; set; }

        public string? DepartureTime { get; set; }

        public string? DepartureDate { get; set; }

        public string? ReservationDate { get; set; }

        public string? ReservationTime { get; set; }

        public int DateDiff { get; set; }

        public int Day { get; set; }

        public string? PickupStoreCode { get; set; }

        public string? MidNightFlight { get; set; }

        public string? Airlines { get; set; }

        public string? AgreeProductLimits { get; set; }

        public string? ProductType { get; set; }

        public string? ReservationType { get; set; }
    }

    public class SubmitPaymentResponseForm
    {
        public bool Valid { get; set; }

        public string? HtmlName { get; set; }

        public string? DynamicHtmlName { get; set; }

        public object? Error { get; set; }

        public string? Attributes { get; set; }

        public string? FormType { get; set; }

        public ShippingAddressUseAsBillingAddress? ShippingAddressUseAsBillingAddress { get; set; }

        public AddressFields? AddressFields { get; set; }

        public ContactInfoFields? ContactInfoFields { get; set; }

        public PaymentMethodField? PaymentMethod { get; set; }

        public CreditCardFields? CreditCardFields { get; set; }

        public GmoCreditCardFields? GmoCreditCardFields { get; set; }

        public Subscribe? Subscribe { get; set; }

        public object? Base { get; set; }
    }

    public class ShippingAddressUseAsBillingAddress
    {
        public string? HtmlValue { get; set; }

        public string? Mandatory { get; set; }

        public string? DynamicHtmlName { get; set; }

        public string? HtmlName { get; set; }

        public bool Valid { get; set; }

        public string? Label { get; set; }

        public bool Checked { get; set; }

        public bool Selected { get; set; }

        public string? FormType { get; set; }
    }

    public class AddressFields
    {
        public bool Valid { get; set; }

        public string? HtmlName { get; set; }

        public string? DynamicHtmlName { get; set; }

        public object? Error { get; set; }

        public string? Attributes { get; set; }

        public string? FormType { get; set; }

        public FormField? AddressId { get; set; }

        public FormField? FirstName { get; set; }

        public FormField? LastName { get; set; }

        public FormField? Address1 { get; set; }

        public FormField? Address2 { get; set; }

        public FormField? City { get; set; }

        public FormField? PostalCode { get; set; }

        public FormField? Country { get; set; }

        public FormField? Phone { get; set; }

        public FormAction? Apply { get; set; }

        public FormAction? Remove { get; set; }
    }

    public class FormField
    {
        public string? HtmlValue { get; set; }

        public string? Mandatory { get; set; }

        public string? DynamicHtmlName { get; set; }

        public string? HtmlName { get; set; }

        public bool Valid { get; set; }

        public string? Label { get; set; }

        public int? MaxLength { get; set; }

        public int? MinLength { get; set; }

        public string? RegEx { get; set; }

        public string? Description { get; set; }

        public string? FormType { get; set; }

        public List<SubmitPaymentOption>? Options { get; set; }

        public string? SelectedOption { get; set; }
    }

    public class FormAction
    {
        public object? Description { get; set; }

        public object? Label { get; set; }

        public bool Submitted { get; set; }

        public bool Triggered { get; set; }

        public string? FormType { get; set; }
    }

    public class SubmitPaymentOption
    {
        public bool Checked { get; set; }

        public string? HtmlValue { get; set; }

        public string? Label { get; set; }

        public string? Id { get; set; }

        public bool Selected { get; set; }

        public string? Value { get; set; }
    }

    public class ContactInfoFields
    {
        public bool Valid { get; set; }

        public string? HtmlName { get; set; }

        public string? DynamicHtmlName { get; set; }

        public object? Error { get; set; }

        public string? Attributes { get; set; }

        public string? FormType { get; set; }

        public FormField? Phone { get; set; }

        public FormField? Email { get; set; }
    }

    public class PaymentMethodField
    {
        public string? HtmlValue { get; set; }

        public bool Mandatory { get; set; }

        public string? DynamicHtmlName { get; set; }

        public string? HtmlName { get; set; }

        public bool Valid { get; set; }

        public int? MaxLength { get; set; }

        public int? MinLength { get; set; }

        public string? RegEx { get; set; }
        public string? FormType { get; set; }
    }

    public class CreditCardFields
    {
        public bool Valid { get; set; }

        public string? HtmlName { get; set; }

        public string? DynamicHtmlName { get; set; }

        public object? Error { get; set; }

        public string? Attributes { get; set; }

        public string? FormType { get; set; }
        // 其他字段...
    }

    public class GmoCreditCardFields
    {
        public bool Valid { get; set; }

        public string? HtmlName { get; set; }

        public string? DynamicHtmlName { get; set; }

        public object? Error { get; set; }

        public string? Attributes { get; set; }

        public string? FormType { get; set; }
    }

    public class Subscribe
    {
        public string? HtmlValue { get; set; }

        public string? Mandatory { get; set; }

        public string? DynamicHtmlName { get; set; }

        public string? HtmlName { get; set; }

        public bool Valid { get; set; }

        public bool Checked { get; set; }

        public bool Selected { get; set; }

        public string? FormType { get; set; }
    }

}
