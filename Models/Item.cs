using System.ComponentModel.DataAnnotations;

namespace NeighborGoodAPI.Models
{
    public class Item
    {
        public int Id { get; set; }
        public Profile? Owner { get; set; }
        [Required]
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public ItemCategory? Category { get; set; }
        public string? BorrowTime { get; set; }
        public string? AddInfo { get; set; }

        public DateTime ItemAdded { get; set; }
        public bool isBorrowed { get; set; }
        public double? Rating { get; set; }
        public int BorrowedCount { get; set; }
        public string? ImageUrl { get; set; }
        public IFormFile? ImageFile { get; set; }
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    }
}


