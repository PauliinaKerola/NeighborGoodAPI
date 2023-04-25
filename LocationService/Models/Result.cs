namespace Service.Models
{
    public class Result
    {
        public List<AddressComponent> address_components { get; set; }
        public Geometry geometry { get; set; }
    }
}