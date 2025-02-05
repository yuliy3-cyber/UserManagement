using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using UserManagement.Data;
using System.Text.Json.Serialization;
using System.Text.Json;
using UserManagementAPI.Models;

var builder = WebApplication.CreateBuilder(args);

// Cấu hình DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Đăng ký PasswordHasher
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

// Thêm các dịch vụ API với cấu hình JSON serialization
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Bỏ qua các vòng tham chiếu trong JSON (nếu có)
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;

        // Đổi tên thuộc tính thành camelCase
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;

        // Để dữ liệu trả về dễ đọc
        options.JsonSerializerOptions.WriteIndented = true;

        // Chỉ định cách xử lý giá trị null
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// Thêm Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Cấu hình Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
