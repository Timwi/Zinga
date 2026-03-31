using System;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

Console.WriteLine("Started.");
var builder = WebAssemblyHostBuilder.CreateDefault(args);
await builder.Build().RunAsync();