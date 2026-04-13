namespace UploadService.Application.Abstractions.Identity;

public interface IUserContext
{
    Guid GetRequiredUserId();
}