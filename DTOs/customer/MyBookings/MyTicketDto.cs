using System;
using System.Collections.Generic;

namespace Darb.Api.DTOs.passengerDtos.bookingDtos
{
    public class MyTicketDto
        {
            public int ETicketId { get; set; }
            public string? TicketCode { get; set; }
            public int TicketStatus { get; set; } 
            public string PassengerName { get; set; } = string.Empty;

        }
}