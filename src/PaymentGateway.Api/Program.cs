using System.Net;
using System.Text.Json.Serialization;

using PaymentGateway.Api.Services;
using PaymentGateway.Api.Services.FirstAcquiringBank;
using PaymentGateway.Api.Validators;

using Polly;

public class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services
            .AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(
                    new JsonStringEnumConverter()
                );
            });

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        builder.Services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        builder.Services.AddSingleton<IPaymentsRepository, PaymentsRepository>();
        builder.Services.AddSingleton<PaymentValidator>();

        builder.Services.AddScoped<IAcquiringBankClientFactory, AcquiringBankClientFactory>();
        builder.Services.AddScoped<IPaymentGatewayService, PaymentGatewayService>();

        builder.Services.AddHttpClient<IAcquiringBankClient, FirstAcquiringBankClient>(client =>
        {
            var bankUrl = builder.Configuration["AcquiringBank:BaseUrl"];
            if (!int.TryParse(builder.Configuration["AcquiringBank:TimeoutSeconds"], out var ttl)) ttl = 30;

            client.BaseAddress = new Uri(bankUrl);
            client.Timeout = TimeSpan.FromSeconds(ttl);
        })
        .AddTransientHttpErrorPolicy(policy => policy
            .OrResult(response => response.StatusCode == HttpStatusCode.ServiceUnavailable)
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}