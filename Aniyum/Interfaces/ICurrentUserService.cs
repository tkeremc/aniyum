namespace Aniyum.Interfaces;

public interface ICurrentUserService
{
    string GetUserId();
    string GetUsername();
    string GetEmail();
    List<string> GetRoles();
    public string GetIpAddress();
    public string GetDeviceId();
}