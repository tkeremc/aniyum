using Aniyum_Backend.Models;
using Aniyum.Models;
using Aniyum.ViewModels;
using AutoMapper;

namespace Aniyum.Mappers;

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