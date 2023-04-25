using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace NeighborGoodAPI.Models
{
    public class Address
    {
        public int Id { get; set; }
        public string Street { get; set; } = null!;
        public string City { get; set; } = null!;
        public string ZipCode { get; set; } = null!;

    }
}
