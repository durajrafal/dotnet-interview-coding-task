using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using Users.Application;
using Users.Console;
using Users.Persistence;

var services = new ServiceCollection();
services.AddDbContext<UserContext>(options =>
{
    options.UseSqlite(@"Data Source=Users.db;");
});

services.AddTransient<UserUpdatesProcessor>();

IServiceProvider sp = services.BuildServiceProvider();

string path = "user_updates.jsonl";

using (var scope = sp.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<UserContext>();
    await new Seed(dbContext, path).Run();
}

var processor = sp.GetRequiredService<UserUpdatesProcessor>();

var stopwatch = new Stopwatch();
using (var reader = new StreamReader(path))
{
    stopwatch.Start();
    await processor.Process(reader);
    stopwatch.Stop();
}
Console.WriteLine($"Processed in {stopwatch.ElapsedMilliseconds} ms.");
Console.WriteLine();