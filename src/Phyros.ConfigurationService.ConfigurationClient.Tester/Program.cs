using Serilog;

namespace Phyros.ConfigurationService.ConfigurationClient.Tester;

public class Program
{
	public static void Main(string[] args)
	{
		var builder = WebApplication.CreateBuilder(args);
		
		using var logger = new LoggerConfiguration()
			.WriteTo.Console()
			.WriteTo.File("./logs.txt")
			.CreateLogger();

		builder.Services.AddSingleton<Serilog.ILogger>(logger);

		logger.Information("Starting up...");

		builder.AddPhyrosConfiguration("Phyros .NET 8 Monolith", builder.Configuration["ClientName"]!);


		builder.Services.AddHttpClient();

		var connectionString = builder.Configuration["ServiceBusConnectionString"];
		//builder.Services.AddPhyrosServiceBusConnection(configurator =>
		//{
		//	configurator.AddPhyrosConfigurationSubscriptions("Phyros .NET 8 Monolith", TimeSpan.FromMinutes(5));
		//}, connectionString!);


		builder.Services.AddControllers()
			.AddJsonOptions(options =>
			{
				options.AllowInputFormatterExceptionMessages = true;
			});
		// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
		builder.Services.AddEndpointsApiExplorer();
		builder.Services.AddSwaggerGen();

		var app = builder.Build();

		app.UsePhyrosConfigurationEvents(options =>
		{
			options.SetChangeHandler((key) =>
			{
				logger.Information("Updated configuration setting for key value {key}", key);
			});
			options.SetDeletedHandler((organizationalUnit, key) =>
			{
				logger.Information($"Deleted configuration setting for key value {key} and organizational unit '{organizationalUnit}'", key, organizationalUnit);
			});
			options.SetIgnoredChangeHandler(key =>
			{
				logger.Information("Ignored configuration setting for key value {key}", key);
			});
		});
		// Configure the HTTP request pipeline.
		if (app.Environment.IsDevelopment())
		{
			app.UseSwagger();
			app.UseSwaggerUI();
		}

		app.UseAuthorization();

		app.MapControllers();

		//var value1 = app.Configuration["New Key"];
		var value2 = app.Configuration.GetSection("RecurringPaymentProcess");
		app.Run();
	}
}
