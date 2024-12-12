// See https://aka.ms/new-console-template for more information
using LegendaryDependencyInjection;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddLazyScoped<LazyServices>();
services.AddScoped<ILazyClass,DefaultLazyClass>();
services.AddKeyedScoped<ILazyClass, FirstLazyClass>("first");
services.AddKeyedScoped<ILazyClass, DefaultLazyClass>("default");
services.AddLegendaryDependencyInjector();

var sp =  services.BuildServiceProvider();
var scope = sp.CreateScope();

var lazys = scope.ServiceProvider.GetRequiredService<LazyServices>();

Console.WriteLine(lazys.LazyClass.Name);
Console.WriteLine(lazys.FirstLazyClass.Name);
Console.WriteLine(lazys.DefaultLazyClass.Name);








public class LazyServices
{
    public LazyServices()
    {

    }
    public virtual ILazyClass LazyClass { get; set; } = default!;
    [Keyed("first")]
    public virtual ILazyClass FirstLazyClass { get; set; } = default!;
    [Keyed("default")]
    public virtual ILazyClass DefaultLazyClass { get; set; } = default!;
}
public interface ILazyClass
{
    string Name { get; set; }
    int Age { get; set; }
    int Balance { get; set; }
}

public class DefaultLazyClass : ILazyClass
{
    public string Name { get; set; } = "默认";
    public int Age { get; set; } = 3389;
    public int Balance { get; set; } = 1000;
}

public class FirstLazyClass : ILazyClass
{
    public string Name { get; set; } = "第一";
    public int Age { get; set; } = 1;
    public int Balance { get; set; } = 1;
}
