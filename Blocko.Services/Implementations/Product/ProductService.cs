using Blocko.Services.Interfaces.Product;
using Bolcko.Domain.Entities.Product;
using Bolcko.Domain.Interfaces;

namespace Blocko.Services.Implementations.Product
{
    public class ProductService : IProductService
    {
        private readonly IUnitOfWork _unitOfWork;
        public ProductService(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public async Task<IEnumerable<Bolcko.Domain.Entities.Product.Product>> GetAllProductsAsync() => await _unitOfWork.Products.GetAllAsync();
        public async Task<Bolcko.Domain.Entities.Product.Product?> GetProductByIdAsync(int id) => await _unitOfWork.Products.GetByIdAsync(id);
        public async Task<IEnumerable<Bolcko.Domain.Entities.Product.Product>> GetProductsByCategoryAsync(int categoryId) => await _unitOfWork.Products.GetProductsByCategoryAsync(categoryId);
        public async Task<IEnumerable<Bolcko.Domain.Entities.Product.Product>> GetFeaturedProductsAsync() => await _unitOfWork.Products.GetFeaturedProductsAsync();
    }
}