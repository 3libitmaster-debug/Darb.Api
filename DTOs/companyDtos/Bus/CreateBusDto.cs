using System.ComponentModel.DataAnnotations;
using Darb.Api.Models;

namespace Darb.Api.Dtos
{
    public class CreateBusDto
    {
        [Required(ErrorMessage = "رقم اللوحة مطلوب.")]
        [StringLength(20, ErrorMessage = "رقم اللوحة طويل جداً.")]
        public string ?PlateNumber { get; set; }

        [Required(ErrorMessage = "موديل الحافلة مطلوب.")]
        public string ?Model { get; set; }

        [Required(ErrorMessage = "سعة الحافلة (عدد المقاعد) مطلوبة.")]
        [Range(5, 100, ErrorMessage = "يجب أن تكون السعة بين 5 و 100 مقعد.")]
        public int Capacity { get; set; }
    }
}