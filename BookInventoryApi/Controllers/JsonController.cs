using Microsoft.AspNetCore.Mvc;
using BookInventoryApi.Data;
using BookInventoryApi.Models;
using Newtonsoft.Json;
using System.IO;
using Microsoft.EntityFrameworkCore;

namespace BookInventoryApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class JsonController : ControllerBase
    {
        private readonly AppDbContext _context;

        public JsonController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadJson(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File is empty");

            using (var reader = new StreamReader(file.OpenReadStream()))
            {
                var jsonContent = await reader.ReadToEndAsync();
                var books = JsonConvert.DeserializeObject<List<Book>>(jsonContent);

                foreach (var book in books)
                {
                    _context.Books.Add(book);
                }

                await _context.SaveChangesAsync();
            }

            return Ok("JSON data imported successfully");
        }
        [HttpPost("delete")]
        public async Task<IActionResult> DeleteJsonData(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File is empty");

            using (var reader = new StreamReader(file.OpenReadStream()))
            {
                var jsonContent = await reader.ReadToEndAsync();
                var booksToDelete = JsonConvert.DeserializeObject<List<Book>>(jsonContent);

                if (booksToDelete == null || !booksToDelete.Any())
                    return BadRequest("No books to delete in the JSON file");

                var titlesToDelete = booksToDelete.Select(b => b.Title).ToList();

                var booksInDb = await _context.Books
                    .Where(b => titlesToDelete.Contains(b.Title))
                    .ToListAsync();

                if (!booksInDb.Any())
                    return NotFound("No matching books found in the database");

                _context.Books.RemoveRange(booksInDb);
                await _context.SaveChangesAsync();

                return Ok($"Deleted {booksInDb.Count} books from the database");
            }
        }
    }
}