using DriveeSmartAssistant.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.RegisterServices();
builder.ConfigureOptions();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddLogging();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();
app.UseAuthorization();
app.InitializeMLModels();
app.MapControllers();


app.Run();
