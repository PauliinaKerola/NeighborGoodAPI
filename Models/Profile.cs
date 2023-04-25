using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NeighborGoodAPI.Models
{
    public class Profile
    {
        [Required]
        public int Id { get; set; }
        [Required]
        public string Auth0Id { get; set; } = null!;
        [Required]
        public string FirstName { get; set; } = null!;
        [Required]
        public string LastName { get; set; } = null!;
        [Required]
        public string Phone { get; set; } = null!;
        [Required]
        public Address Address { get; set; } = null!;
        public string ImageUrl { get; set; }
        public string Email { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public ICollection<Item> Items { get; set; } = new List<Item>();

    }
}
