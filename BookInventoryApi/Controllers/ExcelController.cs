using Microsoft.AspNetCore.Mvc;
using BookInventoryApi.Data;
using BookInventoryApi.Models;
using OfficeOpenXml;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace BookInventoryApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExcelController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ExcelController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadExcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File is empty");

            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                using (var package = new ExcelPackage(stream))
                {
                    var worksheet = package.Workbook.Worksheets[0];
                    var rowCount = worksheet.Dimension.Rows;

                    for (int row = 2; row <= rowCount; row++) // Assuming header is first row
                    {
                        var book = new Book
                        {
                            Title = worksheet.Cells[row, 1].Value?.ToString(),
                            Author = worksheet.Cells[row, 2].Value?.ToString(),
                            Year = int.TryParse(worksheet.Cells[row, 3].Value?.ToString(), out int year) ? year : 0,
                        };

                        _context.Books.Add(book);
                    }

                    await _context.SaveChangesAsync();
                }
            }

            return Ok("Excel data imported successfully");
        }
        [HttpPost("delete")]
        public async Task<IActionResult> DeleteExcelData(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File is empty");

            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                using (var package = new ExcelPackage(stream))
                {
                    var worksheet = package.Workbook.Worksheets[0];
                    var rowCount = worksheet.Dimension.Rows;
                    var titlesToDelete = new List<string>();

                    for (int row = 2; row <= rowCount; row++) // Assuming first row is header
                    {
                        var title = worksheet.Cells[row, 1].Value?.ToString();
                        if (!string.IsNullOrEmpty(title))
                        {
                            titlesToDelete.Add(title);
                        }
                    }

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
}