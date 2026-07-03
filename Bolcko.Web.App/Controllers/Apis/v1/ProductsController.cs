using System.Threading.Tasks;
using Bolcko.Web.App.Controllers.Apis.v1;
using Blocko.Services.Interfaces.Product;
using Blocko.Services.Interfaces.Category;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Bolcko.Web.App.Controllers.Apis.v1
{
    [AllowAnonymous] // Assuming products can be viewed without logging in
    public class ProductController : BaseApiController
    {
        private readonly IProductService _productService;
        private readonly ICategoryService _categoryService;

        public ProductController(IProductService productService, ICategoryService categoryService)
        {
            _productService = productService;
            _categoryService = categoryService;
        }

        [HttpGet("Categories")]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _categoryService.GetRootCategoriesAsync();
            return OkResponse(categories, "Categories retrieved successfully");
        }

        [HttpGet]
        public async Task<IActionResult> GetProducts([FromQuery] string? query, [FromQuery] int? categoryId , [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10)
        {
            var products = await _productService.SearchCatalogProductsPagedAsync(query, categoryId, pageIndex, pageSize);
            return OkResponse(products, "Products retrieved successfully");
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetProductInfo(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null)
                return ErrorResponse("Product not found", statusCode: 404);

            return OkResponse(product, "Product retrieved successfully");
        }
    }
}
