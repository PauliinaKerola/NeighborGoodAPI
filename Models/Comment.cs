using System.ComponentModel.DataAnnotations;

namespace NeighborGoodAPI.Models
{
    public class Comment
    {
        [Required]
        public int Id { get; set; }
        [Required]
        public string Text { get; set; } = null!;
        [Required]
        public Item Item { get; set; } = null!;
    }
}
