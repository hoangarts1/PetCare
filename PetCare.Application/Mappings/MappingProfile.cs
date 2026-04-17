using AutoMapper;
using PetCare.Application.DTOs.User;
using PetCare.Application.DTOs.Product;
using PetCare.Application.DTOs.Order;
using PetCare.Application.DTOs.Appointment;
using PetCare.Application.DTOs.Category;
using PetCare.Application.DTOs.Service;
using PetCare.Application.DTOs.Notification;
using PetCare.Application.DTOs.Payment;
using PetCare.Domain.Entities;

namespace PetCare.Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // User mappings
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.Role != null ? src.Role.RoleName : null));
        CreateMap<CreateUserDto, User>();
        CreateMap<UpdateUserDto, User>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

        // Product mappings
        CreateMap<Product, ProductDto>()
            .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category != null ? src.Category.CategoryName : null))
            .ForMember(dest => dest.ProviderName, opt => opt.MapFrom(src => src.Provider != null ? src.Provider.FullName : null))
            .ForMember(dest => dest.SalePrice, opt => opt.MapFrom(_ => (decimal?)null))
            .ForMember(dest => dest.Images, opt => opt.MapFrom(src => src.Images.OrderBy(i => i.DisplayOrder).Select(i => i.ImageUrl).ToList()));
             
        CreateMap<CreateProductDto, Product>()
            .ForMember(dest => dest.Images, opt => opt.Ignore()); // Handled manually service side
        CreateMap<UpdateProductDto, Product>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

        // Order mappings
        CreateMap<Order, OrderDto>();
        CreateMap<OrderItem, OrderItemDto>();

        // Appointment mappings
        CreateMap<Appointment, AppointmentDto>()
            .ForMember(dest => dest.Pet, opt => opt.MapFrom(src => src.Pet))
            .ForMember(dest => dest.ServiceName, opt => opt.MapFrom(src => src.Service != null ? src.Service.ServiceName : null))
            .ForMember(dest => dest.StaffName, opt => opt.MapFrom(src => src.AssignedStaff != null ? src.AssignedStaff.FullName : null));

        // Product Category mappings
        CreateMap<ProductCategory, ProductCategoryDto>();

        // Service mappings
        CreateMap<Service, ServiceDto>();

        // Notification mappings
        CreateMap<Notification, NotificationDto>();

        // Payment mappings
        CreateMap<Payment, PaymentDto>()
            .ForMember(dest => dest.OrderNumber, opt => opt.MapFrom(src => src.Order != null ? src.Order.OrderNumber : string.Empty));
    }
}
