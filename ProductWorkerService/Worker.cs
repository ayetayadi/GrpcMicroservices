using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProductGrpc.Protos;

namespace ProductWorkerService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _config;
        private readonly ProductFactory _factory;

        public Worker(ILogger<Worker> logger, IConfiguration config, ProductFactory factory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

                // Create a gRPC client (ProductProtoServiceClient) using the gRPC channel.
                using var channel = GrpcChannel.ForAddress(_config.GetValue<string>("WorkerService:ServerUrl"));
                var client = new ProductProtoService.ProductProtoServiceClient(channel);

                _logger.LogInformation("AddProduct started..");

                // Create a new ProductModel with static data (replace with your desired values).
                var productToAdd = new ProductModel
                {
                    Name = "Sample Product",
                    Description = "This is a sample product.",
                    Price = 9.99f,
                    Status = ProductStatus.Instock,
                    CreatedTime = Timestamp.FromDateTime(DateTime.UtcNow)
                };

                // Prepare the AddProductRequest with the static product data.
                var addProductRequest = new AddProductRequest
                {
                    Product = productToAdd
                };

                try
                {
                    // Make the gRPC call to AddProduct with the static data.
                    var addProductResponse = await client.AddProductAsync(addProductRequest);

                    _logger.LogInformation("AddProduct Response: {product}", addProductResponse.ToString());
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error adding product");
                }

                // Awaits the delay before making the next call.
                await Task.Delay(_config.GetValue<int>("WorkerService:TaskInterval"), stoppingToken);
            }
        }
    }
}
