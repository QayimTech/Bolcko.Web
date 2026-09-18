using Blocko.Services.Interfaces.Category;
using Blocko.Services.Interfaces.Order;
using Blocko.Services.Interfaces.Product;
using Blocko.Services.Interfaces.Tender;
using Blocko.Services.Interfaces.User;
using Blocko.Services.Interfaces.SEO;
using Blocko.Services.Interfaces.ShoppingCart;
using Blocko.Services.Interfaces.Delivery;
using Blocko.Services.Interfaces.Payment;

namespace Blocko.Services.Interfaces
{
    public interface IServiceManager
    {
        IUserService UserService { get; }
        IProductService ProductService { get; }
        ICategoryService CategoryService { get; }
        IMarketPriceService MarketPriceService { get; }
        IOrderService OrderService { get; }
        ITenderService TenderService { get; }
        ISEOService SEOService { get; }
        Blocko.Services.Interfaces.Content.IFAQService FAQService { get; }
        IProductSeoService ProductSeoService { get; }
        IShoppingCartService ShoppingCartService { get; }
        IProjectService ProjectService { get; }
        IDeliveryService DeliveryService { get; }
        IPaymentGatewayService PaymentGatewayService { get; }
        Blocko.Services.Interfaces.Financing.IFinancingService FinancingService { get; }
        Blocko.Services.Interfaces.Financing.ICrifCreditBureauService CrifCreditBureauService { get; }
        Blocko.Services.Interfaces.Subscription.ISubscriptionService SubscriptionService { get; }
        Blocko.Services.Interfaces.Subscription.ISubscriptionBillingEngine SubscriptionBillingEngine { get; }
        Blocko.Services.Interfaces.Auth.ISmsOtpService SmsOtpService { get; }
    }
}