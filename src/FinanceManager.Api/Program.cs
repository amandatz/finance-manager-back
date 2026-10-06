using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("FinanceManager API")
            .WithTheme(ScalarTheme.DeepSpace);
    });
}

app.UseHttpsRedirection();

app.Run();