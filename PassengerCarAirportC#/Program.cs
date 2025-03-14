using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PassengerTransport.Clients;
using PassengerTransport.Services;
using PassengerTransport.Vehicles;

namespace PassengerTransport
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {

                    services.AddSingleton<VehicleManager>();

                    services.AddTransient<PassengerBus>(sp => 
                    {
                        var bus = new PassengerBus(
                            sp.GetRequiredService<IGroundControlClient>(),
                            sp.GetRequiredService<ILogger<PassengerBus>>(),
                            sp.GetRequiredService<IHandlingSupervisorClient>(),
                            sp.GetRequiredService<IPassengerService>());
                        
                        bus.CurrentLocation = "CG-1"; 
                        return bus;
                    });

                    services.AddHostedService<VehicleService>();

                    services.AddHttpClient<IGroundControlClient, GroundControlClient>(client =>
                    {
                    client.BaseAddress = new Uri("https://common-horses-open.loca.lt");
                    });
                    services.AddHttpClient<IHandlingSupervisorClient, HandlingSupervisorClient>(client =>
                    {
                    client.BaseAddress = new Uri("https://calm-carrots-drop.loca.lt");
                    });
                    services.AddHttpClient<IPassengerService, PassengerService>(client => 
                    {
                        client.BaseAddress = new Uri("https://ready-memes-cheat.loca.lt");
                    });
                    services.AddSingleton(sp => new RabbitMQConsumer(
                        "amqp://xnyyznus:OSOOLzaQHT5Ys6NPEMAU5DxTChNu2MUe@hawk.rmq.cloudamqp.com:5672/xnyyznus",
                        "tasks.passengerBus",
                        sp.GetRequiredService<ILogger<RabbitMQConsumer>>()));
                });
    }
}