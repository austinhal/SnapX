using System.IO;

namespace ShareX.UploadersLib.Properties
{
    // Stub resource class — replaces Windows ResourceManager/resx approach.
    // Image/Icon properties return null; string properties return hard-coded values.
    internal static class Resources
    {
        // Strings
        internal static string Error => "Error";
        internal static string OAuthInfo_OAuthInfo_New_account => "New account";
        internal static string GoogleDrive_MyDrive_My_drive => "My drive";
        internal static string OneDrive_RootFolder_Root_folder => "Root folder";
        internal static string UploadersConfigForm_ConnectSFTPAccount_Key_file_not_found => "Key file does not exist.";
        internal static string CustomUploaderItem_GetRequestURL_RequestURLMustBeConfigured => "\"Request URL\" must be configured.";
        internal static string CustomUploaderItem_GetFileFormName_FileFormNameMustBeConfigured => "\"File form name\" must be configured.";

        // OAuth callback HTML — read from the file deployed alongside the assembly
        internal static string OAuthCallbackPage
        {
            get
            {
                string path = Path.Combine(AppContext.BaseDirectory, "Resources", "OAuthCallbackPage.html");
                if (File.Exists(path))
                {
                    return File.ReadAllText(path);
                }
                return "<html><body>{0}</body></html>";
            }
        }
    }
}
