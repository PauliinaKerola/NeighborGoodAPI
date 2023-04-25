namespace NeighborGoodAPI.Models
{
    public class Reservation
    {
        public int Id { get; set; }
        public DateTime ReservationDate { get; set; }
        public virtual Item? Item { get; set; }
        public virtual Profile? Reserver { get; set; }
    }
}
