using Akilli_Cihaz_Izleme_Sistemi_Server.Repository;
using Akilli_Cihaz_Izleme_Sistemi_Server.Hubs;
using Akilli_Cihaz_Izleme_Sistemi_Server.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.SetIsOriginAllowed(origin => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// SQL Server LocalDB baðlantýsý. appsettings.json'da "DefaultConnection" tanýmlý deðilse
// aþaðýdaki varsayýlan LocalDB connection string kullanýlýr.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=(localdb)\\mssqllocaldb;Database=CihazIzlemeSistemiDb;Trusted_Connection=True;MultipleActiveResultSets=true";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddScoped<DeviceRepository>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHostedService<DeviceSimulationService>();


var app = builder.Build();

// Uygulama her baþladýðýnda: DB þemasýný oluþtur (migration'lar varsa uygula)
// ve cihaz/kullanýcý verilerini seed deðerleriyle resetle.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
   
}


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");

app.UseAuthorization();


app.MapControllers();
app.MapHub<DeviceHub>("/hub/devices");


app.Run();