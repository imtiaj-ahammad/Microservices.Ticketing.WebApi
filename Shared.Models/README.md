## Microservices.Ticketing.WebApi


### What is RabbitMQ?
- One of the most popular Message-Broker service
- It supports various messaging protocols
- It gives your applications a common platform for sending and receiving messages
- It ensures that your messages is never lost and successfully received by each intended consumer
- Simply you will have a publisher that publishes messages to the message broker
- The server stores the messages in a queue
- To particular queue, multiple consumers can subscribe
- Whenever there is a new message, each of the subscribers would receive it
- An applicatin can act as both producer/consumer based on how you configure it and what is the requirement demands
- A message could consist of any kind of information like a simple string to a complex nested class
- RabbitMQ stores these data within the server till a consumer connects and take the message off the queue for processing
- RabbitMQ also provides a cool Dashboard for monitoring the messages and queues

### Advantages of RabbitMQ
**Better Scalibility**: You do not have to depend on just on VM/processor/server to process your request. When it get's to the point where your first server finds it tough to process the incoming queue data, you can simply add another server that can share the load and improve the overall response time.  
**Clean User Experience**: Your users are less likely to see any errors, thanks to the microservice based message broker architecture  
**Higher Scalibility**: Even if the main Microservice is down to a technical glitch on on-going update, the messages are never lost. It gets stored to the RabbitMQ server. Once the Service comes online, it consumes the pending messages and processes it.  



Here is a simple demonstration of work-flow in a basic RabbitMQ setup. Here consumer#3 is offline for a specific time. This does not in any way affect the integrity of the system. Even if all the consumers are offline, the messages are still in RabbitMQ waiting for the consumers to come online and take the message off their particular queues.  
![image of rabbitMQ flow](../Resources/readme_images/RabbitMQ-flow.png?raw-true)  



### What we will build?
- Lets make a ticketing application where the user can book his/her ticket
- We will have 2 Microservices with RabbitMQ connection for communication
- The user buys a ticket via the front-end
- Internally buying a ticket generates a POST request from the ticket microservice
- The ticket details would be sent to a RabbitMQ Queue, which would later be consumed by the OrderProcessing Microservice
- These details will finally be stored to a database and the uesr will be notified by the email of the order state.  

**Why RabbitMQ in this scenario? Can't we directly POST to the OrderProcessing Microservice?**  
No, This defeats the purpose of having a microservice architecture. Since this is a client facing, business oriented application, we should ensure that the Order always get stored in the memory and doesn't fail/get lost at any point of time. Hence we push the order details to the Queue.  
**What happens if the OrderProcessing Microservice is offline?**  
Absolutely nothing. The user would be notified something like "Tickets confirmed! Please wait for the confirmation mail." When the OrderProcessing Microservice comes online, it takes the data from the RabbitMQ server and processes it and notifies the user by email or SMS.  

### Setting up the Environment
- We will be working with **ASP.NET CORE 6.0 WEBAPI** using **Visual Studio 2022 IDE**. Make sure you have them up and running with the latest SDK.  
- After that, We will need to setup the RabbitMQ server and dashboard

### Installing ErLang
Erlang is a programming language with which the RabbitMQ server is built on. Since we are installing the RabbitMQ server locally to our Machine, make sure that you install Erlang first.
Download the installer from https://www.erlang.org/downloads. Install it in your machine with Admn rights.

### Installing RabbitMQ as a Service in Windows
We will be installing the RabbitMQ server and a service within our windows machine. Download from https://www.rabbitmq.com/install-windows.html and install with Admin rights.

### Enabling RabbitMQ Management Plugin -Dashboard
Now that we have our RabbitMQ Service installed at the system level, we need to activate the management dashboard which by default is disabled. To do this, open up command promts with admin rights and enter the collowing command
```
cd C:\Program Files\RabbitMQ Server\rabbitmq_server-3.8.7\sbin
rabbitmq-plugins enable rabbitmq_management
net stop RabbitMQ
net start RabbitMQ
```
**Line#1** we make the installation directory as our default working directory for cmd. Please note that your directory path may differ  
**Line#2** Here we enable the management plugin  
**Line#3** we stop the RabbitMQ service  
**Line#4** we start the RabbitMQ service  
That is it. Now Navigate to http://localhost:15672/. Here is where you can find the management dashboard of RabbitMQ running
![image of rabbitMQ flow](../Resources/readme_images/RabbitMQ-dashboard.png?raw-true)

The default credentials are guest/guest. Use this to login to your dashboard
![image of rabbitMQ flow](../Resources/readme_images/RabbitMQ-dashboardDetails.png?raw-true)

You can see that our RabbitMQ server is up and running. We will go through the required tabs when we start setting up the entire application.

### Getting Started- RabbitMQ with ASP.NET Core
Now that our server is configured, let's build the actual microservices that can interact with each other via RabbitMQ.
1. Create a new blank solution 'Microservices.Ticketing.WebApi.'
    ```
    mkdir Microservices.Ticketing.WebApi
    cd Microservices.Ticketing.WebApi
    dotnet new sln -n Microservices.Ticketing.WebApi
    code .
    ```
2. Create a new .NET Core library project and name it **Shared.Models**. Here we will define the shared models, which in our case is a simple Ticket Model
    ```
    dotnet new classlib -f net6.0 -n Shared.Models
    dotnet sln add Shared.Models/Shared.Models.csproj
    ```
3. Create a folder for models and a Model class named "Ticket"
    ```
    mkdir Models
    dotnet new class -n Ticket
    ```
    ```
    public class Ticket
    {
    public string UserName { get; set; }
    public DateTime BookedOn { get; set; }
    public string Boarding { get; set; }
    public string Destination { get; set; }
    }
    ```
4. Create a new ASP.NET Core 6.0 WebApi project and name it **Ticketing.Microsevice**. This will act as the publisher that sends a ticket model to the queue of RabbitMQ
    ```
    dotnet new webapi -f net6.0 -n Ticketing.Microservice
    dotnet sln add Ticketing.Microservice/Ticketing.Microservice.csproj
    ```
5. Add packages for MassTransit
    ```
    dotnet add package MassTransit --version 6.3.2
    dotnet add package MassTransit.RabbitMQ --version 6.3.2
    dotnet add package MassTransit.AspNetCore --version 6.3.2
    ```
#### What is MassTransit?
- MassTransit is a .NET friendly asbtraction over message-broker technologies like RabbitMQ
- It makes it easier to work with RabbitMQ by providing a lot of dev-friendly configurations
- It essentially helps developers to route messages over Messaging Service Buses, with native support for RabbitMQ
- It does not have a specific implementation, it basically works like an interface, an abstraction over the whole message bus concept
- It supports all the Major Messaging Bus Implementationl like RabbitMQ, Kafka and more
6. We will now configure the **Ticketing.Microservice** as a publisher in ASP.NET Core container. Go to program file and add the followings-
    ```
    // Add masstransit
    builder.Services.AddMassTransit(x => 
    {
        x.AddBus(provider => Bus.Factory.CreateUsingRabbitMq(config => 
            {
                config.UseHealthCheck(provider);
                config.Host(new Uri("rabbitmq://localhost"), h =>
                    {
                        h.Username("guest");
                        h.Password("guest");
                    });
            }));
    });// this add the MassTransit Service to the ASP.NET Core service container
    builder.Services.AddMassTransitHostedService();// this creates a new Service Bus using RabbitMQ with the provided paremeters like the host url, username, password etc
    ```
7. Add project Shared.Models reference to the Ticketing.Microservice project
    ```
    <ItemGroup>
        <ProjectReference Include="..\shared.models\shared.models.csproj" />
    </ItemGroup>
    ```
8. Create a simple API controller  named **TicketController** that can take in a Ticket Model passed by the user(via POSTMAN here).
    ```
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
            // we are naming our queue as ticketQueue, if it does not exist, RabbitMQ will create one.
            var endPoint = await _bus.GetSendEndpoint(uri);
            await endPoint.Send(ticket);
            return Ok();
        }
        return BadRequest();
    }
    ```
9. Now test it with POSTMAN. We will POST data to the ticket endpoint with POSTMAN and the POST request will pass to the ticket endpoint. 
    ```
    {
    "userName" : "Mukesh",
    "Boarding" : "Muscat",
    "Destination" :"Trivandrum"
    }
    ```
RabbitMQ will create a new Exchange Queue for us and store the passed data within the server. Let's observe the RabbitMQ dashboard after posting the message via POSTMAN.
![image of ticketQueue Exchange](../Resources/readme_images/RabbitMQ-Exchange.png?raw-true)
You can see that RabbitMQ has created a new Exchange for us named 'ticketQueue'. It is also important to note that since we do not have a subscriber yet for this publisher, the message we passed is not seen.  

10. Let us build a consumer. Create a new ASP.NET Core WebAPI project and name it **TicketProcessor.Microservice**. This microservice will be responsible for consuming the incoming messages from RabbitMQ server and process it further.
    ```
    dotnet new webapi -f net6.0 -n TicketProcessor.Microservice
    dotnet sln add TicketProcessor.Microservice/TicketProcessor.Microservice.csproj
    ```
11. Add packages for MassTransit
    ```
    dotnet add package MassTransit --version 6.3.2
    dotnet add package MassTransit.RabbitMQ --version 6.3.2
    dotnet add package MassTransit.AspNetCore --version 6.3.2
    ```
12. We will now configure the TicketProcessor.Microservice as a consumer in ASP.NET Core container. Go to program file and add the followings-
    ```
    // Add masstransit  
    builder.Services.AddMassTransit(x => 
        {
            x.AddConsumer<TicketConsumer>();
            x.AddBus(provider => Bus.Factory.CreateUsingRabbitMq(config => 
                {
                    config.UseHealthCheck(provider);
                    config.Host(new Uri("rabbitmq://localhost"), h =>
                        {
                            h.Username("guest");
                            h.Password("guest");
                        });
                    config.ReceiveEndpoint("ticketQueue", ep =>
                        {
                            ep.PrefetchCount = 16;
                            ep.UseMessageRetry(rt => rt.Interval(2, 100));
                            ep.ConfigureConsumer<TicketConsumer>(provider);
                        });
                }));
        });
    builder.Services.AddMassTransitHostedService();
    ```
    - We will add a new consumer, named **TicketConsumer**, We have not created it yet.
    - Here we are defining receiveEndpoint, as this is a consumer
    - Here we link the **ticketQueue** to the **TicketConsumer** class 
13. Add project Shared.Models reference to the TicketProcessor.Microservice project
    ```
    <ItemGroup>
        <ProjectReference Include="..\shared.models\shared.models.csproj" />
    </ItemGroup>
    ```
14. Create a folder named **Consumers** and create a class named **TicketConsumer**
    ```
    dotnet new class -n TicketConsumer
    ```
    ```
    public class TicketConsumer : IConsumer<Ticket>
    {
        public async Task Consume(ConsumeContext<Ticket> context)
        {
            var data = context.Message;//we are extracting the actual message from the Context
            //Validate the Ticket Data
            //Store to Database
            //Notify the user via Email / SMS
        }
    }
    ```
This is a very simple consumer that implements the **IConsumer** of the **MassTransit** class. Any message with the signature of the **Ticket** Model that is sent to the **ticketQueue** will be received by this consumer.  
15. Now we will test Microservices now. We need both the Microservices running in order to send and receive the ticket data. Enable Multiple Startup Projects and enable both the Microservices and test with a POST request.
#### Test Scenario 01: When the Consumer is Online 
- Run both the services online
- Try to pass some sample data
- Put a breakpoint at the consumer class and at the **ticketController** to verify the received data

#### Test Scenario 02: Consumer is offline. Back Online after N minutes
- Make the consumer project off
- Run the producer project only 
- Put a breakpoint at the consumer class and at the **ticketcontroller** to verify the received data
- Try to pass some sample data to make a request
- Once the request is processed by the producer, the request will be stored in the RabbitMQ server
![image of ticketQueue Queue](../Resources/readme_images/RabbitMQ-ticketQueue.png?raw-true)
![image of ticketQueue Queue Details ](../Resources/readme_images/RabbitMQ-ticketQueueDetails.png?raw-true)
- Run the consumer project after sometime and wait at the breakpoint to hold
- Once the consumer is online, the request from the rabbitMQ server will be released into the consumer project
![image of ticketQueue Queue Details ](../Resources/readme_images/RabbitMQ-ticketQueueDetailsAfterReleased.png?raw-true)


#### Reference: https://codewithmukesh.com/blog/rabbitmq-with-aspnet-core-microservice/