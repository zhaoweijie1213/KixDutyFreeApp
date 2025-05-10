using Dm;
using KixDutyFree.App.Manage;
using KixDutyFree.App.Models.Response;
using KixDutyFree.Shared.Models.Response;
using log4net.Core;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OpenQA.Selenium.BiDi.Modules.Network;
using QYQ.Base.Common.IOCExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace KixDutyFree.Shared.Services
{
    public class KixDutyFreeApiService(ILogger<KixDutyFreeApiService> logger,IHttpClientFactory httpClientFactory) : ITransientDependency
    {


        #region API请求

        /// <summary>
        /// 登录
        /// </summary>
        /// <param name="email"></param>
        /// <param name="password"></param>
        /// <param name="csrfToken"></param>
        /// <returns></returns>
        public async Task<LoginResponse?> LoginAsync(HttpClient httpClient, string email, string password, string csrfToken)
        {
            var data = new Dictionary<string, string>
            {
                ["loginEmail"] = email,
                ["loginPassword"] = password,
                ["csrf_token"] = csrfToken
            };
            HttpRequestMessage httpRequest = new(HttpMethod.Post, KixDutyFreeApi.Login)
            {
                Content = new FormUrlEncodedContent(data)
            };
            //using var request = new FormUrlEncodedContent(data);
            //request.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
            var res = await httpClient.SendAsync(httpRequest);
            if (!res.IsSuccessStatusCode)
            {
                logger.LogError("HttpAccountClient: 登录失败，状态码 {StatusCode}", res.StatusCode);
                return null;
            }
            var content = await res.Content.ReadAsStringAsync();
            logger.LogDebug("LoginAsync.响应:{content}", content);
            var response = JsonConvert.DeserializeObject<LoginResponse>(content);
            return response;
        }

        /// <summary>
        /// 商品数量变化
        /// </summary>
        /// <returns></returns>
        public async Task<ProductVariationResponse?> ProductVariationAsync(HttpClient httpClient, string productId, int quantity, CancellationToken cancellationToken)
        {
            ProductVariationResponse? data = null;
            HttpRequestMessage request = new(HttpMethod.Get, KixDutyFreeApi.Cart.ProductVariation + $"?pid={productId}&quantity={quantity}");
            var response = await httpClient.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogDebug("ProductVariationAsync.响应:{content}", content);
            if (response.IsSuccessStatusCode)
            {
                data = JsonConvert.DeserializeObject<ProductVariationResponse>(content, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore, // 忽略 null 值
                    DefaultValueHandling = DefaultValueHandling.Populate // 使用默认值
                });
            }
            return data;
        }

        /// <summary>
        /// 添加到购物车
        /// </summary>
        /// <returns></returns>
        public async Task<CartAddProductResponse?> CartAddProductAsync(HttpClient httpClient, string productId, int quantity, CancellationToken cancellationToken)
        {
            CartAddProductResponse? data = null;
            var request = new HttpRequestMessage(HttpMethod.Post, KixDutyFreeApi.Cart.AddProduct);
            // 构建表单数据
            var formData = new List<KeyValuePair<string, string>>
            {
                new("pid", productId),
                new("quantity", quantity.ToString())
            };
            // 使用 FormUrlEncodedContent 设置请求内容
            request.Content = new FormUrlEncodedContent(formData);
            var response = await httpClient.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogInformation("CartAddProductAsync.响应:{content}", content);
            if (response.IsSuccessStatusCode)
            {
                data = JsonConvert.DeserializeObject<CartAddProductResponse>(content, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore, // 忽略 null 值
                    DefaultValueHandling = DefaultValueHandling.Populate // 使用默认值
                });
            }
            return data;
        }

        /// <summary>
        /// 修改购物车商品数量
        /// </summary>
        public async Task<CartUpdateQuantityResponse?> CartUpdateQuantityAsync(HttpClient httpClient, string productId, int quantity, string uuid, CancellationToken cancellationToken)
        {
            CartUpdateQuantityResponse? data = null;
            var request = new HttpRequestMessage(HttpMethod.Get, KixDutyFreeApi.Cart.UpdateQuantity + $"?pid={productId}&quantity={quantity}&uuid={uuid}");
            var response = await httpClient.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogInformation("CartUpdateQuantityAsync.响应:{content}", content);
            if (response.IsSuccessStatusCode)
            {
                data = JsonConvert.DeserializeObject<CartUpdateQuantityResponse>(content, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore, // 忽略 null 值
                    DefaultValueHandling = DefaultValueHandling.Populate // 使用默认值
                });
            }
            return data;
        }

        /// <summary>
        /// 移除购物车商品
        /// </summary>
        public async Task<RemoveProductLineItemResponse?> RemoveProductLineItemAsync(HttpClient httpClient, string productId, string uuid, CancellationToken cancellationToken)
        {
            RemoveProductLineItemResponse? data = null;
            var request = new HttpRequestMessage(HttpMethod.Get, KixDutyFreeApi.Cart.RemoveProductLineItem + $"?pid={productId}&uuid={uuid}");
            var response = await httpClient.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogInformation("RemoveProductLineItemAsync.响应:{content}", content);
            if (response.IsSuccessStatusCode)
            {
                data = JsonConvert.DeserializeObject<RemoveProductLineItemResponse>(content, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore, // 忽略 null 值
                    DefaultValueHandling = DefaultValueHandling.Populate // 使用默认值
                });
            }
            return data;
        }

        /// <summary>
        /// 航班信息
        /// </summary>
        /// <param name="date">yyyy/MM/dd</param>
        /// <param name="time">HH:mm</param>
        /// <returns></returns>
        public async Task<FlightGetInfoResponse?> FlightGetInfoAsync(HttpClient httpClient, DateTime dateTime, CancellationToken cancellationToken)
        {
            FlightGetInfoResponse? data = null;
            var request = new HttpRequestMessage(HttpMethod.Get, KixDutyFreeApi.Cart.FlightGetInfo + $"?date={dateTime:yyyy/MM/dd}&time={dateTime:HH:mm}");
            var response = await httpClient.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogInformation("FlightGetInfoAsync.响应:{content}", content);
            if (response.IsSuccessStatusCode)
            {
                data = JsonConvert.DeserializeObject<FlightGetInfoResponse>(content, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore, // 忽略 null 值
                    DefaultValueHandling = DefaultValueHandling.Populate // 使用默认值
                });
            }
            return data;
        }

        /// <summary>
        /// 保存航班信息
        /// </summary>
        /// <param name="csrfToken"></param>
        /// <param name="calendarStartDate"></param>
        /// <param name="departureDate"></param>
        /// <param name="departureTime"></param>
        /// <param name="airlinesNo">航空公司</param>
        /// <param name="flightNo">航班号  其他航班号:other</param>
        /// <param name="otherflightno">其他航班号</param>
        /// <param name="connectingFlight">是否转机 yes/no</param>
        /// <param name="agreeProductLimits"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<FlightSaveInfoResponse?> FlightSaveInfoAsync(HttpClient httpClient, string csrfToken, string calendarStartDate, string departureDate, string departureTime, string airlinesNo, string flightNo, string otherflightno
            , string connectingFlight, CancellationToken cancellationToken, string agreeProductLimits = "yes")
        {
            FlightSaveInfoResponse? data = null;
            // 构建表单数据
            var formData = new List<KeyValuePair<string, string>>
            {
                new("csrf_token", csrfToken),
                new("midNightFlight", "no"),
                new("calendarStartDate", calendarStartDate),
                new("departureDate", departureDate),
                new("departureTime", departureTime),
                new("airlinesNo", airlinesNo),
                new("flightNo", flightNo),
                new("otherflightno", otherflightno),
                new("connectingFlight", connectingFlight),
                new("agreeProductLimits", agreeProductLimits)
            };
            var request = new HttpRequestMessage(HttpMethod.Post, KixDutyFreeApi.Cart.FlightSaveInfo)
            {
                Content = new FormUrlEncodedContent(formData)
            };
            var response = await httpClient.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync();
            logger.LogInformation("FlightSaveInfoAsync.响应:{content}", content);
            if (response.IsSuccessStatusCode)
            {
                data = JsonConvert.DeserializeObject<FlightSaveInfoResponse>(content, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore, // 忽略 null 值
                    DefaultValueHandling = DefaultValueHandling.Populate // 使用默认值
                });
            }

            return data;
        }

        /// <summary>
        /// 提交付款信息
        /// </summary>
        /// <returns></returns>
        public async Task<SubmitPaymentResponse?> SubmitPaymentAsync(HttpClient httpClient, string email, string csrfToken, CancellationToken cancellationToken)
        {
            SubmitPaymentResponse? data = null;
            // 构建表单数据
            var formData = new List<KeyValuePair<string, string>>
            {
                //new ("hidCardYear", "undefined"),
                //new ("hidCardMonth", "undefined"),
                //new ("hidCardNo", ""),
                //new ("hidCardType", ""),
                //new ("addressSelector", "new"),
                //new ("dwfrm_billing_addressFields_firstName", ""),
                //new ("dwfrm_billing_addressFields_lastName", ""),
                //new ("dwfrm_billing_addressFields_address1", ""),
                //new ("dwfrm_billing_addressFields_address2", ""),
                //new ("dwfrm_billing_addressFields_country", ""),
                //new ("dwfrm_billing_addressFields_city", ""),
                //new ("dwfrm_billing_addressFields_postalCode", ""),
                new ("csrf_token", csrfToken),
                //new ("localizedNewAddressTitle", "新地址"),
                new ("dwfrm_billing_contactInfoFields_email", email),
                //new ("dwfrm_billing_contactInfoFields_phone", ""),
                new ("dwfrm_billing_paymentMethod", "Instore_Payment") //到店支付
            };

            var request = new HttpRequestMessage(HttpMethod.Post, KixDutyFreeApi.CheckoutServices.SubmitPayment)
            {
                Content = new FormUrlEncodedContent(formData)
            };
            var response = await httpClient.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogInformation("SubmitPaymentAsync.响应:{content}", content);
            if (response.IsSuccessStatusCode)
            {
                data = JsonConvert.DeserializeObject<SubmitPaymentResponse>(content);
            }
   
            return data;
        }

        /// <summary>
        /// 下单
        /// </summary>
        /// <returns></returns>
        public async Task<PlaceOrderResponse?> PlaceOrderAsync(HttpClient httpClient, CancellationToken cancellationToken)
        {
            PlaceOrderResponse? data = null;
            var request = new HttpRequestMessage(HttpMethod.Post, KixDutyFreeApi.CheckoutServices.PlaceOrder);
            var response = await httpClient.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogInformation("PlaceOrderAsync.响应:{content}", content);
            if (response.IsSuccessStatusCode)
            {
                data = JsonConvert.DeserializeObject<PlaceOrderResponse>(content, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore, // 忽略 null 值
                    DefaultValueHandling = DefaultValueHandling.Populate // 使用默认值
                });
            }

            return data;
        }

        #endregion
    }

    public static class KixDutyFreeApi
    {
        /// <summary>
        /// 登录
        /// </summary>
        public const string Login = "/on/demandware.store/Sites-KixDutyFree-Site/zh_CN/Account-Login?rurl=1";

        /// <summary>
        /// 结账地址
        /// </summary>
        public static class CheckoutServices
        {
            /// <summary>
            /// 提交支付信息
            /// </summary>
            public const string SubmitPayment = "/on/demandware.store/Sites-KixDutyFree-Site/zh_CN/CheckoutServices-SubmitPayment";
 

            /// <summary>
            /// 下单
            /// </summary>
            public const string PlaceOrder = "/on/demandware.store/Sites-KixDutyFree-Site/zh_CN/CheckoutServices-PlaceOrder";
        }

        /// <summary>
        /// 购物车
        /// </summary>
        public static class Cart
        {
            /// <summary>
            /// 商品数量变化
            /// </summary>
            public const string ProductVariation = "/on/demandware.store/Sites-KixDutyFree-Site/zh_CN/Product-Variation";

            /// <summary>
            /// 添加到购物车
            /// </summary>
            public const string AddProduct = "/on/demandware.store/Sites-KixDutyFree-Site/zh_CN/Cart-AddProduct";

            /// <summary>
            /// 修改购物车商品数量
            /// </summary>
            public const string UpdateQuantity = "/on/demandware.store/Sites-KixDutyFree-Site/zh_CN/Cart-UpdateQuantity";


            /// <summary>
            /// 从购物车移除该商品
            /// </summary>
            public const string RemoveProductLineItem = "/on/demandware.store/Sites-KixDutyFree-Site/zh_CN/Cart-RemoveProductLineItem";

            /// <summary>
            /// 获取航班信息
            /// </summary>
            public const string FlightGetInfo = "/on/demandware.store/Sites-KixDutyFree-Site/zh_CN/Flight-GetInfo?date=2024/12/07&time=18:19";

            /// <summary>
            /// 保存航班信息
            /// </summary>
            public const string FlightSaveInfo = "/on/demandware.store/Sites-KixDutyFree-Site/zh_CN/Flight-SaveInfo";


        }
    }

   
}
