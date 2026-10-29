using PetSpaApi.Core;
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();
DatabaseManager.GetInstance();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
var defaultFilesOptions = new DefaultFilesOptions();
defaultFilesOptions.DefaultFileNames.Clear();
defaultFilesOptions.DefaultFileNames.Add("login.html"); 

app.UseDefaultFiles(defaultFilesOptions);
app.UseStaticFiles();
app.UseHttpsRedirection();

app.UseAuthorization();


app.MapControllers();

app.Run();
