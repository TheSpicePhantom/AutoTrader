using AutoTrader.Helpers;
using System.Diagnostics;

namespace AutoTrader {
    public class Program {

        public delegate void AppShutdown();
        public static event AppShutdown? ApplicationShuttingDown;

        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddApplicationInsightsTelemetry();

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddSingleton<SocketOperator>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            //if (app.Environment.IsDevelopment())
            //{
            app.UseSwagger();
            app.UseSwaggerUI();
            //}

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }

        static void OnProcessExit(object sender, EventArgs e)
        {
            ApplicationShuttingDown?.Invoke();
            Console.WriteLine("Closing the application");
            Task.Delay(10000);
        }
    }
}


