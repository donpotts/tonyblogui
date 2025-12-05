using Microsoft.AspNetCore.Mvc;

namespace GoogleSheetCRUD.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly GenericGoogleSheetsService _sheetsService;
    private const string SheetName = "Product";

    public ProductsController(GenericGoogleSheetsService sheetsService)
    {
        _sheetsService = sheetsService;
    }

    /// <summary>
    /// Gets all products from the Google Sheet
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<Product>>> GetAll()
    {
        var products = await _sheetsService.GetAllAsync<Product>(SheetName);
        return Ok(products);
    }

    /// <summary>
    /// Gets a specific product by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Product>> GetById(string id)
    {
        var products = await _sheetsService.GetAllAsync<Product>(SheetName);
        var product = products.FirstOrDefault(p => p.Id == id);

        if (product == null)
            return NotFound();

        return Ok(product);
    }

    /// <summary>
    /// Creates a new product
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<Product>> Create([FromBody] Product product)
    {
        var createdProduct = await _sheetsService.AddAsync(product, SheetName);
        return CreatedAtAction(nameof(GetById), new { id = createdProduct.Id }, createdProduct);
    }

    /// <summary>
    /// Updates an existing product
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult> Update(string id, [FromBody] Product product)
    {
        product.Id = id;
        var success = await _sheetsService.UpdateAsync(product, SheetName);

        if (!success)
            return NotFound();

        return NoContent();
    }

    /// <summary>
    /// Deletes a product by ID
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(string id)
    {
        var success = await _sheetsService.DeleteAsync(id, SheetName);

        if (!success)
            return NotFound();

        return NoContent();
    }
}
