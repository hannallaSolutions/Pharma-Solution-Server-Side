using Microsoft.AspNetCore.Mvc;  // for controllerbase support
using Microsoft.EntityFrameworkCore;
using SearchTool_ServerSide.Data;
using SearchTool_ServerSide.Models;
using SearchTool_ServerSide.Dtos.ProductDtos;

namespace SearchTool_ServerSide.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly SearchToolDBContext _db;

        public ProductsController(SearchToolDBContext db)
        {
            _db = db;
        }

    

        // GET: api/products
        [HttpGet]
        public async Task<ActionResult<List<ProductDto>>> GetAll()
        {
            var items = await _db.Products
                .OrderByDescending(x => x.CreatedAt)  //orderby createdat desc  
                .Select(x => new ProductDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Price = x.Price,
                    Stock = x.Stock,
                    Category = x.Category,
                    CreatedAt = x.CreatedAt
                })
                .ToListAsync();    //materialize query

            return Ok(items);
        }

        //get by me , only the name
         
         [HttpGet("names")]
         public async Task<ActionResult<List<string>>> GetNames()
        {
         var names = await _db.Products
              .OrderByDescending(x => x.CreatedAt)
              .Select(x => x.Name)
              .ToListAsync();

              return Ok(names);


        }

        //  GET: api/products/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ProductDto>> GetById(int id)
        {
            var product = await _db.Products.FirstOrDefaultAsync(x => x.Id == id);
            if (product == null) return NotFound(new { message = "Product not found" });

            var dto = new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Price = product.Price,
                Stock = product.Stock,
                Category = product.Category,
                CreatedAt = product.CreatedAt
            };

            return Ok(dto);
        }

        //  POST: api/products
        [HttpPost]
        public async Task<ActionResult<ProductDto>> Create([FromBody] ProductCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var product = new Product
            {
                Name = dto.Name,
                Price = dto.Price,
                Stock = dto.Stock,
                Category = dto.Category,
                CreatedAt = DateTime.UtcNow
            };

            _db.Products.Add(product);
            await _db.SaveChangesAsync();

            var result = new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Price = product.Price,
                Stock = product.Stock,
                Category = product.Category,
                CreatedAt = product.CreatedAt
            };

            return CreatedAtAction(nameof(GetById), new { id = product.Id }, result);
        }

       //post by me , create multiple products at once
    [HttpPost("bulk")]
    public async Task<ActionResult<List<ProductDto>>> CreateBulk([FromBody] List<ProductCreateDto> dtos)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState); // Validate the incoming model
        var products = new List<Product>();
        foreach (var dto in dtos)
        {
            var product = new Product
            {
                Name = dto.Name,
                Price = dto.Price,
                Stock = dto.Stock,
                Category = dto.Category,
                CreatedAt = DateTime.UtcNow
            };

            products.Add(product);
        }

        _db.Products.AddRange(products);
        await _db.SaveChangesAsync();

        var result = products.Select(product => new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Price = product.Price,
            Stock = product.Stock,
            Category = product.Category,
            CreatedAt = product.CreatedAt
        }).ToList();

        return CreatedAtAction(nameof(GetAll), result);
    }
  

        //  PUT: api/products/{id}
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] ProductUpdateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var product = await _db.Products.FirstOrDefaultAsync(x => x.Id == id);
            if (product == null) return NotFound(new { message = "Product not found" });

            product.Name = dto.Name;
            product.Price = dto.Price;
            product.Stock = dto.Stock;
            product.Category = dto.Category;

            await _db.SaveChangesAsync();

            return NoContent();
        }


//put by me , update multiple products at once
    [HttpPut("bulk")]
public async Task<IActionResult> UpdateBulk([FromBody] List<ProductBulkUpdateDto> updates)
{
    if (!ModelState.IsValid) return BadRequest(ModelState);

    foreach (var item in updates)
    {
        var product = await _db.Products.FirstOrDefaultAsync(x => x.Id == item.Id);
        if (product == null)
            return NotFound(new { message = $"Product with ID {item.Id} not found" });

        product.Name = item.Dto.Name;
        product.Price = item.Dto.Price;
        product.Stock = item.Dto.Stock;
        product.Category = item.Dto.Category;
    }

    await _db.SaveChangesAsync();
    return NoContent();
}

      
        //  DELETE: api/products/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _db.Products.FirstOrDefaultAsync(x => x.Id == id);
            if (product == null) return NotFound(new { message = "Product not found" });

            _db.Products.Remove(product);
            await _db.SaveChangesAsync();

            return NoContent();
        }






//  static list to simulate a database

        public static List<Product> products = new List<Product>
        {
            new Product { Id = 1, Name = "Laptop", Price = 999.99m, Stock = 10, Category = "Electronics", CreatedAt = DateTime.UtcNow },
            new Product { Id = 2, Name = "Smartphone", Price = 699.99m, Stock = 25, Category = "Electronics", CreatedAt = DateTime.UtcNow },
            new Product { Id = 3, Name = "Headphones", Price = 199.99m, Stock = 50, Category = "Accessories", CreatedAt = DateTime.UtcNow }
        };

        // get all products from the static list
        [HttpGet("static")]
        public IActionResult GetAllStatic()
        {
            var items = products
                .OrderByDescending(x => x.CreatedAt)
                .ToList();
            return Ok(items);

        }

        // get product by id from the static list
        [HttpGet("static/{id:int}")]
        public IActionResult GetStaticById(int id)
        {
            var product = products.FirstOrDefault(x => x.Id == id);
            if (product == null) return NotFound(new { message = "product not found"});

            return Ok(product);
        }

        // create product in the static list
        [HttpPost("static")]
        public IActionResult CreateStatic([FromBody] ProductCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var newId = products.Max(x => x.Id) + 1;  // generate new id
            var Product = new Product  /// create new product
            {
                Id = newId,
                Name = dto.Name,
                Price = dto.Price,
                Stock = dto.Stock,
                Category = dto.Category,
                CreatedAt = DateTime.UtcNow
            };
              products.Add(Product);  // add to the static list
              return CreatedAtAction(nameof(GetStaticById), new { id = Product.Id }, Product);
        }

        //put in static list
        [HttpPut("static/{id:int}")]
        public IActionResult UpdateStatic(int id, [FromBody] ProductUpdateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var product = products.FirstOrDefault(x => x.Id == id);
            if (product == null) return NotFound(new { message = "Product not found" });

            product.Name = dto.Name;
            product.Price = dto.Price;
            product.Stock = dto.Stock;
            product.Category = dto.Category;

            return NoContent();
        }

        //delete in static list
        [HttpDelete("static/{id:int}")]
        public IActionResult DeleteStatic(int id)
        {
            var product = products.FirstOrDefault(x => x.Id == id);
            if (product == null) return NotFound(new { message = "Product not found" });

            products.Remove(product);
            return NoContent();
        }

        // get products and add new one
        [HttpPost("static/test-multiple")]
        public IActionResult TestMultipleStatic([FromBody] ProductCreateDto dto)
        {
            // get all products
            var items = products
                .OrderByDescending(x => x.CreatedAt)
                .ToList();

            // add new product
            var newId = products.Max(x => x.Id) + 1;  // generate new id
            var Product = new Product  /// create new product
            {
                Id = newId,
                Name = dto.Name,
                Price = dto.Price,
                Stock = dto.Stock,
                Category = dto.Category,
                CreatedAt = DateTime.UtcNow
            };
            products.Add(Product);  // add to the static list

            return Ok(new { products = items, addedProduct = Product });
        }
    }
}
