using LegendaryDependencyInjection;
using System.Text.Encodings.Web;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLazyScoped<ILazyClass, DefaultLazyClass>();
builder.Services.AddLazyScoped<ILazyClass, FirstLazyClass>();

// Add services to the container.
builder.Services.AddMvc().AddJsonOptions(op =>
{
    var opp = op.JsonSerializerOptions;
    opp.WriteIndented = true;
    opp.PropertyNamingPolicy = null;
    opp.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
}).AddLegendaryDependencyInjector();

var app = builder.Build();
app.UseRouting();

app.MapControllers();
app.Run();


public interface ILazyClass
{
    string Name { get; set; }
    int Age { get; set; }
    int Balance { get; set; }
}

public class DefaultLazyClass:ILazyClass
{
    public string Name { get; set; } = "Ä¬ÈÏ";
    public int Age { get; set; } = 3389;
    public int Balance { get; set; } = 1000;
}

public class FirstLazyClass : ILazyClass
{
    public string Name { get; set; } = "µÚÒ»";
    public int Age { get; set; } = 1;
    public int Balance { get; set; } = 1;
}