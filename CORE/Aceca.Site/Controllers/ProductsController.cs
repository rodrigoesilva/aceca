using Aceca.Site.Helper;
using Microsoft.AspNetCore.Mvc;

namespace Aceca.Site.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        [HasPermission(Permission.CanViewProducts)]
        [HttpGet("GetMaterials")]
        public async Task<IActionResult> GetProductsAsync() 
        { 
            return Ok(); 
        }

        [HasPermission(Permission.CanCreateProduct)]
        [HttpPost("CreateMaterial")]
        public async Task<IActionResult> CreateProductAsync() 
        { 
            return Ok(); 
        }
    }
}
