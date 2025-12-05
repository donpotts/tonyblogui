using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TonyBlogUI.Server.Services;
using TonyBlogUI.Shared;

namespace TonyBlogUI.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BlogsController : ControllerBase
{
    private readonly GoogleSheetsService _sheetsService;
    private const string SheetName = "Pillar #1";

    public BlogsController(GoogleSheetsService sheetsService)
    {
        _sheetsService = sheetsService;
    }

    /// <summary>
    /// Gets all blogs from the Google Sheet
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<Blog>>> GetAll()
    {
        try
        {
            var blogs = await _sheetsService.GetAllAsync<Blog>(SheetName);
            return Ok(blogs);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error retrieving blogs: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets a specific blog by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Blog>> GetById(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return BadRequest("ID is required");

        try
        {
            var blogs = await _sheetsService.GetAllAsync<Blog>(SheetName);
            var blog = blogs.FirstOrDefault(b => b.Id == id);

            if (blog == null)
                return NotFound();

            return Ok(blog);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error retrieving blog: {ex.Message}");
        }
    }

    /// <summary>
    /// Creates a new blog (Admin only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<Blog>> Create([FromBody] Blog blog)
    {
        if (blog == null)
            return BadRequest("Blog data is required");

        try
        {
            var createdBlog = await _sheetsService.AddAsync(blog, SheetName);
            return CreatedAtAction(nameof(GetById), new { id = createdBlog.Id }, createdBlog);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error creating blog: {ex.Message}");
        }
    }

    /// <summary>
    /// Updates an existing blog (Admin only)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Update(string id, [FromBody] Blog blog)
    {
        if (string.IsNullOrWhiteSpace(id))
            return BadRequest("ID is required");

        if (blog == null)
            return BadRequest("Blog data is required");

        try
        {
            blog.Id = id;
            var success = await _sheetsService.UpdateAsync(blog, SheetName);

            if (!success)
                return NotFound();

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error updating blog: {ex.Message}");
        }
    }

    /// <summary>
    /// Deletes a blog by ID (Admin only)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Delete(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return BadRequest("ID is required");

        try
        {
            var success = await _sheetsService.DeleteAsync(id, SheetName);

            if (!success)
                return NotFound();

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error deleting blog: {ex.Message}");
        }
    }
}
