using Darb.Api.Models;
using Darb.Api.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Darb.Api.Dtos;

public class CreateTripDto
{
    [Required(ErrorMessage = "Start location is required.")]
    public int StartGoveId { get; set; }

    [Required(ErrorMessage = "Destination location is required.")]
    public int EndGoveId { get; set; }

    [Required(ErrorMessage = "Departure date and time are required.")]
    [DataType(DataType.DateTime)]
    public DateTime DepartureDateTime { get; set; }

    [Required(ErrorMessage = "Arrival date and time are required.")]
    [DataType(DataType.DateTime)]
    public DateTime? ArrivalDateTime { get; set; }

    [Required(ErrorMessage = "Trip period is required.")]
    [EnumDataType(typeof(Periods), ErrorMessage = "Invalid period value.")]
    public Periods Period { get; set; }

    [Required(ErrorMessage = "Bus selection is required.")]
    public int BusId { get; set; }
}