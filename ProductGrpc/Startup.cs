using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ProductGrpc.Data;
using ProductGrpc.Services;

namespace ProductGrpc
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940

        // Register the ProductsContext as a service with the dependency injection container. The ProductsContext is a database context class that interacts with the underlying database.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<ProductsContext>(options => options.UseInMemoryDatabase("Products"));

            // Add gRPC services to the app.  It registers the gRPC server components and enables detailed error messages during development.
            services.AddGrpc(opt =>
            {
                opt.EnableDetailedErrors = true;
            });

            // Register mapping
            services.AddAutoMapper(typeof(Startup));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            // Register ProductService into aspnet pipeline to new grpc service.
            app.UseEndpoints(endpoints =>
            {
                // It maps the ProductService to the gRPC endpoint. It means that any gRPC request with the specified service contract (defined in product.proto) will be handled by the ProductService implementation.
                endpoints.MapGrpcService<ProductService>();

                // It maps a GET endpoint to the root URL ("/"). When a client makes a GET request to the root URL, the server will respond with a simple text response that indicates that communication with gRPC endpoints should be made through a gRPC client.
                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
                });
            });
        }
    }
}