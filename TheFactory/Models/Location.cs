namespace TheFactory.Models
{
    public class Location
    {
        public int LocationId { get; set; }
        public required string Name { get; set; }
        public int TownId { get; set; }
        public int RegionId { get; set; }
        public int CountryId { get; set; }
        public int LocationTypeId { get; set; }
    }
}