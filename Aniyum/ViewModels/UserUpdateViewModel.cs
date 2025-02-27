﻿namespace Aniyum.ViewModels;

public class UserUpdateViewModel : BaseViewModel
{
    public string? FullName { get; set; }
    public string? Username { get; set; }
    public string? Email { get; set; }
    public string? HashedPassword { get; set; }
}