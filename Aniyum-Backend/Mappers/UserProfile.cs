using Aniyum_Backend.Models;
using Aniyum_Backend.ViewModels;
using AutoMapper;

namespace Aniyum_Backend.Mappers;

public class UserProfile : Profile
{
    public UserProfile()
    {
        AllowNullCollections = true;
        CreateMap<UserViewModel, UserModel>().ReverseMap();
        CreateMap<UserCreateViewModel, UserModel>().ReverseMap();
        CreateMap<UserUpdateViewModel, UserModel>().ReverseMap();
    }
}