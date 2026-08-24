namespace Axon.Application.Interfaces.Services
{
    public interface IBarcodeService
    {
        byte[] GenerateBarcode(string content);
        byte[] GenerateQRCode(string content);
    }
}
