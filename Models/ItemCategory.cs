using System.ComponentModel.DataAnnotations;

namespace NeighborGoodAPI.Models
{
    public class ItemCategory
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; } = null!;
    }
}
