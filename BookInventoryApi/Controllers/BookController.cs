using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BookInventoryApi.Data;
using BookInventoryApi.Models;
using OfficeOpenXml;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace BookInventoryApi.Controllers
{
    public enum FileType
    {
        Excel,
        Json
    }

    [Route("api/[controller]")]
    [ApiController]
    public class BookController : ControllerBase
    {
        private readonly AppDbContext _context;

        public BookController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadData(
        IFormFile file,
            [Required][FromQuery] FileType fileType)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File is empty");

            switch (fileType)
            {
                case FileType.Excel:
                    return await UploadExcel(file);
                case FileType.Json:
                    return await UploadJson(file);
                default:
                    return BadRequest("Invalid file type. Use 'Excel' or 'Json'.");
            }
        }

        [HttpPost("delete")]
        public async Task<IActionResult> DeleteData(
        IFormFile file,
            [Required][FromQuery] FileType fileType)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File is empty");

            switch (fileType)
            {
                case FileType.Excel:
                    return await DeleteExcelData(file);
                case FileType.Json:
                    return await DeleteJsonData(file);
                default:
                    return BadRequest("Invalid file type. Use 'Excel' or 'Json'.");
            }
        }

        private async Task<IActionResult> UploadExcel(IFormFile file)
        {
            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                using (var package = new ExcelPackage(stream))
                {
                    var worksheet = package.Workbook.Worksheets[0];
                    var rowCount = worksheet.Dimension.Rows;

                    for (int row = 2; row <= rowCount; row++) // Assuming first row is header
                    {
                        var book = new Book
                        {
                            Title = worksheet.Cells[row, 1].Value?.ToString(),
                            Author = worksheet.Cells[row, 2].Value?.ToString(),
                            Year = int.TryParse(worksheet.Cells[row, 3].Value?.ToString(), out int year) ? year : 0
                        };

                        _context.Books.Add(book);
                    }

                    await _context.SaveChangesAsync();
                }
            }

            return Ok("Excel data imported successfully");
        }

        private async Task<IActionResult> UploadJson(IFormFile file)
        {
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

        private async Task<IActionResult> DeleteExcelData(IFormFile file)
        {
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

        private async Task<IActionResult> DeleteJsonData(IFormFile file)
        {
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