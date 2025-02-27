namespace Aniyum.ViewModels;

public class UserViewModel : BaseViewModel
{
    public string? FullName { get; set; }
    public string? Username { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; }
    public List<string>? Roles { get; set; }
}