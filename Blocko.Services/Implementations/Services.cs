using Blocko.Services.Interfaces;
using Bolcko.Domain.Entities;
using Bolcko.Domain.Interfaces;

namespace Blocko.Services.Implementations
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _unitOfWork;
        public UserService(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public async Task<User?> AuthenticateAsync(string email, string password)
        {
            var user = await _unitOfWork.Users.GetByEmailAsync(email);
            if (user == null || user.PasswordHash != password) return null; // Simplified for MVP
            return user;
        }

        public async Task<User> RegisterUserAsync(User user, string password)
        {
            user.PasswordHash = password; // Simplified
            await _unitOfWork.Users.AddAsync(user);
            await _unitOfWork.CompleteAsync();
            return user;
        }

        public async Task<User?> GetUserByIdAsync(int id) => await _unitOfWork.Users.GetByIdAsync(id);
    }

    public class ProductService : IProductService
    {
        private readonly IUnitOfWork _unitOfWork;
        public ProductService(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public async Task<IEnumerable<Product>> GetAllProductsAsync() => await _unitOfWork.Products.GetAllAsync();
        public async Task<Product?> GetProductByIdAsync(int id) => await _unitOfWork.Products.GetByIdAsync(id);
        public async Task<IEnumerable<Product>> GetProductsByCategoryAsync(int categoryId) => await _unitOfWork.Products.GetProductsByCategoryAsync(categoryId);
        public async Task<IEnumerable<Product>> GetFeaturedProductsAsync() => await _unitOfWork.Products.GetFeaturedProductsAsync();
    }

    public class CategoryService : ICategoryService
    {
        private readonly IUnitOfWork _unitOfWork;
        public CategoryService(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public async Task<IEnumerable<Category>> GetRootCategoriesAsync() => await _unitOfWork.Categories.GetRootCategoriesAsync();
        public async Task<IEnumerable<Category>> GetSubCategoriesAsync(int parentId) => await _unitOfWork.Categories.FindAsync(c => c.ParentCategoryId == parentId);
    }

    public class OrderService : IOrderService
    {
        private readonly IUnitOfWork _unitOfWork;
        public OrderService(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public async Task<Order> PlaceOrderAsync(Order order)
        {
            await _unitOfWork.Orders.AddAsync(order);
            await _unitOfWork.CompleteAsync();
            return order;
        }

        public async Task<IEnumerable<Order>> GetUserOrdersAsync(int userId) => await _unitOfWork.Orders.GetUserOrdersAsync(userId);
    }

    public class TenderService : ITenderService
    {
        private readonly IUnitOfWork _unitOfWork;
        public TenderService(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public async Task<Tender> CreateTenderAsync(Tender tender)
        {
            await _unitOfWork.Tenders.AddAsync(tender);
            await _unitOfWork.CompleteAsync();
            return tender;
        }

        public async Task<IEnumerable<Tender>> GetOpenTendersAsync() => await _unitOfWork.Tenders.GetOpenTendersAsync();
    }
}