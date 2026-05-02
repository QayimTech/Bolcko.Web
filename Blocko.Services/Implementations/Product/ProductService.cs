using Blocko.Services.Interfaces.Product;
using Bolcko.Domain.Entities.Product;
using Bolcko.Domain.Interfaces;

namespace Blocko.Services.Implementations.Product
{
    public class ProductService : IProductService
    {
        private readonly IUnitOfWork _unitOfWork;
        public ProductService(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public async Task<IEnumerable<Product>> GetAllProductsAsync() => await _unitOfWork.Products.GetAllAsync();
        public async Task<Product?> GetProductByIdAsync(int id) => await _unitOfWork.Products.GetByIdAsync(id);
        public async Task<IEnumerable<Product>> GetProductsByCategoryAsync(int categoryId) => await _unitOfWork.Products.GetProductsByCategoryAsync(categoryId);
        public async Task<IEnumerable<Product>> GetFeaturedProductsAsync() => await _unitOfWork.Products.GetFeaturedProductsAsync();

        public async Task AddProductAsync(Product product)
        {
            await _unitOfWork.Products.AddAsync(product);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task UpdateProductAsync(Product product)
        {
            _unitOfWork.Products.Update(product);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task DeleteProductAsync(int id)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(id);
            if (product != null)
            {
                _unitOfWork.Products.Remove(product);
                await _unitOfWork.SaveChangesAsync();
            }
        }
    }
}