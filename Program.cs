namespace AutoTrader {
    public class Program {

        public delegate void AppShutdown();
        public static event AppShutdown? ApplicationShuttingDown;

        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            //builder.Services.Configure<BrokerConfig>(builder.Configuration.GetSection("BrokerConfig"));


            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseAuthorization();

            app.MapControllers();

            //var options = app.Services.GetRequiredService<IOptions<BrokerConfig>>().Value;

            app.Run();
        }

        static void OnProcessExit(object sender, EventArgs e)
        {
            ApplicationShuttingDown?.Invoke();
            Console.WriteLine("Closing the application");
        }
    }
}


