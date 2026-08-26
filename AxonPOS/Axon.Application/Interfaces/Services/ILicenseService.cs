namespace Axon.Application.Interfaces.Services
{
    public interface ILicenseService
    {
        string GetHardwareId();
        bool IsLicenseValid();
        bool ValidateAndActivate(string licenseKey, out string errorMessage);
    }
}
