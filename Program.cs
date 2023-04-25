using Microsoft.EntityFrameworkCore;
using NeighborGoodAPI.Models;
using System.Text.Json.Serialization;

namespace NeighborGoodAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            string corsPolicyName = "_origins";
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddCors(options =>
            {
                options.AddPolicy(name: corsPolicyName,
                                  policy =>
                                  {
                                      policy.WithOrigins(builder.Configuration.GetValue<string>("Front"))
                                      .AllowAnyHeader()
                                      .AllowAnyMethod();
                                  });
            });
            //builder.Services.AddControllers();
            builder.Services.AddControllers()
                            .AddJsonOptions(x =>
                                x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);

            builder.Services.AddDbContext<NGDbContext>(options =>
                {
                    options.UseSqlServer(builder.Configuration.GetConnectionString("DbConnection"));
                });

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            
            app.UseHttpsRedirection();

            app.UseCors(corsPolicyName);

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}