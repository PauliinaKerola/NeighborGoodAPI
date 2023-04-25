using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NeighborGoodAPI.Models;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs;
using Azure;
using Microsoft.SqlServer.Server;

namespace NeighborGoodAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ItemsController : ControllerBase
    {
        private readonly NGDbContext _context;
        private readonly IConfiguration _configuration;

        public ItemsController(NGDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // GET: api/Items
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Item>>> GetItems()
        {
            return await _context.Items.Include(i=>i.Category).Include(i => i.Owner).ThenInclude(p => p.Address)
                .OrderByDescending(i =>i.ItemAdded).ToListAsync();
        }
        
        // GET: get user's items
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<Item>>> GetProfileByAuthId(int userId)
        {
            return await _context.Items.Where(p => userId == p.Owner.Id)
                .OrderByDescending(i => i.ItemAdded ).ToListAsync();
        }

        // GET: api/items: search
        [HttpGet("searchByName/{name}")]
        public async Task<ActionResult<List<Item>>> GetItemByName(string name)
        {
            return await _context.Items.Include(i => i.Category).Include(t => t.Owner).ThenInclude(p => p.Address)
                .Where(t => t.Name.Contains(name)).ToListAsync();
        }

        // GET: api/items: search
        [HttpGet("searchExtended")]
        public async Task<ActionResult<List<Item>>> GetItemExtended(string? name, string? city, string? category)
        {
            return await _context.Items.Include(i => i.Category).Include(t => t.Owner).ThenInclude(p => p.Address)
                .Where(t => (name == null ? true : t.Name.Contains(name))
                             && (city == null ? true : t.Owner.Address.City.Contains(city))
                             && (category == null ? true : t.Category.Name.Equals(category))).ToListAsync();
        }

        //GET: api/Items/Cities
        [HttpGet("Cities")]
        public async Task<List<string>> GetCities()
        {
            return await _context.Items.Select(i => i.Owner.Address.City).Distinct().ToListAsync();
        }

        //GET: api/Items/Categories
        [HttpGet("Categories")]
        public async Task<List<string>> GetItemCategories()
        {
            return await _context.ItemCategories.Select(ic => ic.Name).ToListAsync();
        }

        // GET: api/Items/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Item>> GetItem(int id)
        {
            var item = await _context.Items.Include(i => i.Category)
                        .Include(i => i.Owner)
                        .ThenInclude(p => p.Address)
                        .SingleOrDefaultAsync(i => i.Id == id);

            if (item == null)
            {
                return NotFound($"Tuotetta ei löynyt id:llä {id}");
            }

            return item;
        }

        // PUT: api/Items/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutItem(int id, IFormCollection formData)
        {
            var item = await _context.Items.FindAsync(id);
            if (item == null)
            {
                return NotFound($"Ei tuotetta id:llä {id}");
            }
            ItemCategory? category = await _context.ItemCategories.SingleOrDefaultAsync(i => i.Name.Equals(formData["category"].First()));
            if (category == null)
            {
                return NotFound("Tuote kategoriaa ei löytynyt");
            }

            var file = formData.Files.FirstOrDefault();
            string? newFileName = null;
            string? newFileUrl = null;
            if (file != null && IsImage(file))
            {
                newFileName = $"{Guid.NewGuid()}_{file.FileName}";
                newFileUrl = await UploadImageToAzureAsync(file, newFileName);
                if (newFileUrl == null)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError,
                        new { message = "Failed to upload image to Azure :(" });
                }
                await DeleteImageFromAzureAsync(item!.ImageUrl);
                item.ImageUrl = newFileUrl;
            }


            item.Name = formData["itemName"].First();
            item.Description = formData["description"].FirstOrDefault();
            item.BorrowTime = formData["borrowTime"].FirstOrDefault();
            item.AddInfo = formData["addInfo"].FirstOrDefault();
            item.Category = category;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ItemExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Items
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Item>> PostItem(IFormCollection formData)
        {
            var file = formData.Files.FirstOrDefault();
            string? fileName = null;
            string? fileUrl = null;
            if (file != null && IsImage(file))
            {
                fileName = $"{Guid.NewGuid()}_{file.FileName}";
                fileUrl = await UploadImageToAzureAsync(file, fileName);
                if (fileUrl == null)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError,
                        new { message = "Failed to upload image to Azure :(" });
                }
            }

            var itemName = formData["itemName"].FirstOrDefault();
            var description = formData["description"].FirstOrDefault();
            var auth0Id = formData["userId"].FirstOrDefault();
            var borrowTime = formData["borrowTime"].FirstOrDefault();
            var addInfo = formData["addInfo"].FirstOrDefault();
            var category = formData["category"].FirstOrDefault();

            if (itemName == null)
            {
                return BadRequest("Item name missing");
            }
            if(auth0Id == null)
            {
                return BadRequest("User_id missing");

            }
            var owner = await _context.Profiles.SingleOrDefaultAsync(p => p.Auth0Id == auth0Id);
            if (owner == null)
            {
                return NotFound($"Cannot find user with user_id: {auth0Id}");
            }

            var itemCategory = await _context.ItemCategories.SingleOrDefaultAsync(c => c.Name == category);

            Item item = new()
            {
                Name = itemName,
                Owner = owner,
                Description = description,
                ImageUrl = fileUrl,
                BorrowTime = borrowTime,
                AddInfo = addInfo,
                Category = itemCategory,
                ItemAdded = DateTime.Now,
            };

            _context.Items.Add(item);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetItem", new { id = item.Id }, item);
        }

        // DELETE: api/Items/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteItem(int id)
        {
            var item = await _context.Items.FindAsync(id);
            if (item == null)
            {
                return NotFound();
            }
            var reservations = await _context.Reservations.Where(r => r.Item.Id == id).ToListAsync();

            foreach(var reservation in reservations)
            {
                _context.Reservations.Remove(reservation);
            }

            
            _context.Items.Remove(item);
            await _context.SaveChangesAsync();
            await DeleteImageFromAzureAsync(item.ImageUrl);

            return NoContent();
        }

        private bool IsImage(IFormFile file)
        {
            string[] allowedContentTypes = new[] { "image/png", "image/gif", "image/jpeg"};
            string[] allowedExtensions = new[] { ".png", ".jfif", ".pjpeg", ".jpeg", ".pjp", ".jpg" };

            if (!allowedContentTypes.Contains(file.ContentType))
            {
                return false;
            }
            string fileExtension = Path.GetExtension(file.FileName);
            if (!allowedExtensions.Contains(fileExtension))
            {
                return false;
            }
            return true;
        }

        private bool ItemExists(int id)
        {
            return _context.Items.Any(e => e.Id == id);
        }

        private async Task<string?> UploadImageToAzureAsync(IFormFile file, string imageName)
        {
            try
            {
                BlobServiceClient serviceClient = new BlobServiceClient(_configuration.GetValue<string>("BlobConnectionString"));
                BlobContainerClient containerClient = serviceClient.GetBlobContainerClient(_configuration.GetValue<string>("BlobContainerName"));

                BlobClient blobClient = containerClient.GetBlobClient(imageName);

                await using (var data = file.OpenReadStream())
                {
                    var response = await blobClient.UploadAsync(data, new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = file.ContentType } });
                    if (response.GetRawResponse().IsError)
                    {
                        return null;
                    }
                }
                BlobHttpHeaders headers = new()
                {
                    ContentType = file.ContentType
                };
                blobClient.SetHttpHeaders(headers);
                return blobClient.Uri.ToString();
            }
            catch (RequestFailedException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                return null;
            }
        }

        private async Task DeleteImageFromAzureAsync(string imageUrl)
        {
            string blobName = imageUrl.Split('/').Last();
            System.Diagnostics.Debug.WriteLine($"{blobName}");
            BlobServiceClient serviceClient = new BlobServiceClient(_configuration.GetValue<string>("BlobConnectionString"));
            BlobContainerClient containerClient = serviceClient.GetBlobContainerClient(_configuration.GetValue<string>("BlobContainerName"));
            await containerClient.GetBlobClient(blobName).DeleteAsync(snapshotsOption: DeleteSnapshotsOption.IncludeSnapshots);
        }
    }
}
