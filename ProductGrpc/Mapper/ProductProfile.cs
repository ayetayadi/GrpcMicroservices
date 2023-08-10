using AutoMapper;
// Timestamp
using Google.Protobuf.WellKnownTypes;
using ProductGrpc.Protos;

namespace ProductGrpc.Mapper
{
    public class ProductProfile : Profile
    {
        public ProductProfile()
        {
            // it sets up a mapping configuration from the Models.Product class (the application's data model) to the ProductModel class (gRPC message class). It specifies how the properties of Models.Product should be mapped to the properties of ProductModel.
            CreateMap<Models.Product, ProductModel>()
                .ForMember(dest => dest.CreatedTime, opt => opt.MapFrom(src => Timestamp.FromDateTime(src.CreatedTime)));

            CreateMap<ProductModel, Models.Product>()
                .ForMember(dest => dest.CreatedTime, opt => opt.MapFrom(src => src.CreatedTime.ToDateTime()));

            // note : not use reverseMap. Timestamp should be converted manually.
        }
    }
}