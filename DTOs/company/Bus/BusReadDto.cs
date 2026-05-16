namespace Darb.Api.Dtos
{
    public class BusReadDto
    {
        public int BusId { get; set; }


        public string PlateNumber { get; set; } = string.Empty;


        public string Model { get; set; } = string.Empty;

       
        public int Capacity { get; set; }

   
        public string Status { get; set; } = string.Empty;
    }
}