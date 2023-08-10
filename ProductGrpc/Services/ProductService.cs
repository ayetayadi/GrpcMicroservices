using AutoMapper;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProductGrpc.Data; 
using ProductGrpc.Models;
using ProductGrpc.Protos;
using System;
using System.Threading.Tasks;

namespace ProductGrpc.Services
{
    //Class named ProductService, which inherits from ProductProtoService.ProductProtoServiceBase. This inheritance is a standard practice when implementing gRPC services in C# using the generated code from the .proto file.
    public class ProductService : ProductProtoService.ProductProtoServiceBase
    {
        private readonly ProductsContext _productDbContext;
        private readonly IMapper _mapper;
        private readonly ILogger<ProductService> _logger;

        // ProductService a un constructeur qui prend trois paramètres : productDbContext, mapper et logger.Ces paramètres sont utilisés pour l'injection de dépendances et sont fournis lors de la création d'une instance de ProductService.
        // Le constructeur initialise les champs privés avec les dépendances fournies.
        public ProductService(ProductsContext productDbContext, IMapper mapper, ILogger<ProductService> logger)
        {
            _productDbContext = productDbContext ?? throw new ArgumentNullException(nameof(productDbContext));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // Test method is an implementation of the gRPC method defined in product.proto with the same name and signature. It takes an Empty request (which is a predefined empty message in the protobuf library) and returns an Empty response.
        // In this implementation, the method simply calls the base Test method, which is used to handle unary methods with empty request and response messages
        public override Task<Empty> Test(Empty request, ServerCallContext context)
        {
            return base.Test(request, context);
        }

        // GetProduct method is an implementation of the GetProduct gRPC method,
        // which is used to retrieve a single product based on its ID.
        // It takes a GetProductRequest containing the productId as input and returns a ProductModel as a response.
        // It queries the productDbContext (the database context) to find the product with the given ID, maps the database entity to a ProductModel, and returns it as the response. If the product is not found, it throws a gRPC RpcException with a status code of NotFound.
        public override async Task<ProductModel> GetProduct(GetProductRequest request,
                                                                ServerCallContext context)
        {
            var product = await _productDbContext.Product.FindAsync(request.ProductId);
            if (product == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, $"Product with ID={request.ProductId} is not found."));
            }
            var productModel = _mapper.Map<ProductModel>(product);
            return productModel;
        }

        // GetAllProducts method is an implementation of the GetAllProducts gRPC method,
        // which is a server-streaming method that streams multiple product details to the client. It takes an GetAllProductsRequest (empty request) and an IServerStreamWriter<ProductModel> as input. It queries the productDbContext to retrieve all products, maps each product to a ProductModel, and streams each ProductModel to the client using the responseStream.
        public override async Task GetAllProducts(GetAllProductsRequest request,
                                                    IServerStreamWriter<ProductModel> responseStream,
                                                    ServerCallContext context)
        {
            var productList = await _productDbContext.Product.ToListAsync();
            foreach (var product in productList)
            {
                var productModel = _mapper.Map<ProductModel>(product);
                await responseStream.WriteAsync(productModel);
            }
        }


        // AddProduct method is an implementation of the AddProduct gRPC method, which is used to add a new product to the database. It takes an AddProductRequest containing the ProductModel to be added, and it returns the added ProductModel. It maps the ProductModel from the request to a Product entity, adds it to the productDbContext, saves the changes to the database, and returns the newly added ProductModel as the response.
        public override async Task<ProductModel> AddProduct(AddProductRequest request, ServerCallContext context)
        {
            var product = _mapper.Map<Product>(request.Product);

            _productDbContext.Product.Add(product);
            await _productDbContext.SaveChangesAsync();

            _logger.LogInformation("Product successfully added : {productId}_{productName}", product.ProductId, product.Name);

            var productModel = _mapper.Map<ProductModel>(product);
            return productModel;
        }


        // The UpdateProduct method is an implementation of the UpdateProduct gRPC method, which is used to update an existing product in the database. It takes an UpdateProductRequest containing the updated ProductModel and returns the updated ProductModel. It maps the ProductModel from the request to a Product entity, checks if the product with the given ID exists in the database, updates the database entity, and saves the changes to the database. If the product is not found, it throws a gRPC RpcException with a status code of NotFound.
        public override async Task<ProductModel> UpdateProduct(UpdateProductRequest request, ServerCallContext context)
        {
            var product = _mapper.Map<Product>(request.Product);

            bool isExist = await _productDbContext.Product.AnyAsync(p => p.ProductId == product.ProductId);
            if (!isExist)
            {
                throw new RpcException(new Status(StatusCode.NotFound, $"Product with ID={product.ProductId} is not found."));
            }

            _productDbContext.Entry(product).State = EntityState.Modified;

            try
            {
                await _productDbContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                throw;
            }

            var productModel = _mapper.Map<ProductModel>(product);
            return productModel;
        }


        // DeleteProduct method is an implementation of the DeleteProduct gRPC method, which is used to delete a product from the database. It takes a DeleteProductRequest containing the productId to be deleted and returns a DeleteProductResponse. It finds the product in the database, removes it from the productDbContext, saves the changes to the database, and returns a DeleteProductResponse indicating the success of the deletion.
        public override async Task<DeleteProductResponse> DeleteProduct(DeleteProductRequest request, ServerCallContext context)
        {
            var product = await _productDbContext.Product.FindAsync(request.ProductId);
            if (product == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, $"Product with ID={request.ProductId} is not found."));
            }

            _productDbContext.Product.Remove(product);
            var deleteCount = await _productDbContext.SaveChangesAsync();
            

            // deleteCount count the number of products deleted
            var response = new DeleteProductResponse
            {
                Success = deleteCount > 0
            };

            return response;
        }


        // InsertBulkProduct method is an implementation of the InsertBulkProduct gRPC method,
        // which is a client-streaming method used to insert multiple products in bulk.
        // It takes an IAsyncStreamReader<ProductModel> as input, which allows it to receive a stream of ProductModel objects from the client.
        // It iterates over the stream, maps each ProductModel to a Product entity, and adds them to the productDbContext. After processing all the items in the stream,
        // it saves the changes to the database and returns an InsertBulkProductResponse indicating the success of the bulk insertion and the number of products inserted.
        public override async Task<InsertBulkProductResponse> InsertBulkProduct(IAsyncStreamReader<ProductModel> requestStream, ServerCallContext context)
        {
            // https://csharp.hotexamples.com/examples/-/IAsyncStreamReader/-/php-iasyncstreamreader-class-examples.html

            while (await requestStream.MoveNext())
            {
                var product = _mapper.Map<Product>(requestStream.Current);
                _productDbContext.Product.Add(product);
            }

            var insertCount = await _productDbContext.SaveChangesAsync();

            var response = new InsertBulkProductResponse
            {
                Success = insertCount > 0,
                InsertCount = insertCount
            };

            return response;
        }

    }
}