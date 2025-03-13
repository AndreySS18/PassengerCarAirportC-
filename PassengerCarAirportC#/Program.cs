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
                    // Регистрация менеджера транспорта
                    services.AddSingleton<VehicleManager>();
                    // Регистрация автобусов
                    services.AddTransient<PassengerBus>(sp => 
                    {
                        var bus = new PassengerBus(
                            sp.GetRequiredService<IGroundControlClient>(),
                            sp.GetRequiredService<ILogger<PassengerBus>>(),
                            sp.GetRequiredService<IHandlingSupervisorClient>());
                        
                        // Инициализируем в правильной локации
                        bus.CurrentLocation = "CG-1"; 
                        return bus;
                    });
                    // Регистрация сервиса как hosted service
                    services.AddHostedService<VehicleService>();

                    // Регистрация остальных зависимостей
                    services.AddHttpClient<IGroundControlClient, GroundControlClient>(client =>
                    {
                    client.BaseAddress = new Uri("https://crazy-plants-sleep.loca.lt");
                    });
                    services.AddHttpClient<IHandlingSupervisorClient, HandlingSupervisorClient>(client =>
                    {
                    client.BaseAddress = new Uri("https://strong-paths-roll.loca.lt");
                    });
                    services.AddSingleton(sp => new RabbitMQConsumer(
                        "amqp://xnyyznus:OSOOLzaQHT5Ys6NPEMAU5DxTChNu2MUe@hawk.rmq.cloudamqp.com:5672/xnyyznus",
                        "tasks.passengerBus",
                        sp.GetRequiredService<ILogger<RabbitMQConsumer>>()));
                });
    }
}