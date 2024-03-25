using MassTransit;
using Shared.Models;

namespace TicketProcessor.Microservice;

public class TicketConsumer : IConsumer<Ticket>
{
    public async Task Consume(ConsumeContext<Ticket> context)
    {
        var data = context.Message;
        //Validate the Ticket Data
        //Store to Database
        //Notify the user via Email / SMS
    }
}
