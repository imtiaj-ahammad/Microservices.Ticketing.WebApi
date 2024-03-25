using System;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Shared.Models;

namespace Ticketing.Microservice.Controllers;

[ApiController]
[Route("[controller]")]
public class TicketController : ControllerBase
{
    private readonly IBus _bus;
    public TicketController(IBus bus)
    {
        _bus = bus;
    }

    [HttpPost]
    public async Task<IActionResult> CreateTicket(Ticket ticket)
    {
        if(ticket != null)
        {
            ticket.BookedOn = DateTime.Now;
            Uri uri = new Uri("rabbitmq://localhost/ticketQueue");
            var endPoint = await _bus.GetSendEndpoint(uri);
            await endPoint.Send(ticket);
            return Ok();
        }
        return BadRequest();
    }

}
