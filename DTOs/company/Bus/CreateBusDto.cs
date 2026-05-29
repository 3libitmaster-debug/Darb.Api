using System.ComponentModel.DataAnnotations;
using Darb.Api.Models;

namespace Darb.Api.Dtos
{
    public class CreateBusDto
    {
        [Required(ErrorMessage = "ÑŞã ÇááæÍÉ ãØáæÈ.")]
        [StringLength(20, ErrorMessage = "ÑŞã ÇááæÍÉ Øæíá ÌÏÇğ.")]
        public string ?PlateNumber { get; set; }

        [Required(ErrorMessage = "ãæÏíá ÇáÍÇİáÉ ãØáæÈ.")]
        public string ?Model { get; set; }

        [Required(ErrorMessage = "ÓÚÉ ÇáÍÇİáÉ (ÚÏÏ ÇáãŞÇÚÏ) ãØáæÈÉ.")]
        [Range(5, 100, ErrorMessage = "íÌÈ Ãä Êßæä ÇáÓÚÉ Èíä 5 æ 100 ãŞÚÏ.")]
        public int Capacity { get; set; }
    }
}