using ShareX.UploadersLib;
using ShareX.UploadersLib.ImageUploaders;
using ShareXMac.Models;

namespace ShareXMac.Services;

public class UploadService
{
    public async Task<string?> UploadImageAsync(byte[] data, string fileName, AppSettings settings)
    {
        if (data.Length == 0) return null;
        if (settings.ActiveImageDestination == ImageDestination.Imgur
            && string.IsNullOrWhiteSpace(settings.ImgurClientId))
            return null;

        return await Task.Run(() =>
        {
            try
            {
                GenericUploader? uploader = CreateUploader(settings);
                if (uploader == null) return null;
                using var ms = new MemoryStream(data);
                UploadResult result = uploader.Upload(ms, fileName);
                return result?.IsSuccess == true ? result.URL : null;
            }
            catch
            {
                return null;
            }
        });
    }

    private static GenericUploader? CreateUploader(AppSettings settings) =>
        settings.ActiveImageDestination switch
        {
            ImageDestination.Imgur => new Imgur(
                new OAuth2Info(settings.ImgurClientId, ""))
            {
                UploadMethod = AccountType.Anonymous,
                DirectLink = true
            },
            _ => null
        };
}
