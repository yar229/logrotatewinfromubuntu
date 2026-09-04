namespace logrotate.Tests.Integration.NewWave.Wrappers;

public class XExtension
{
    public string Ext { get; private set; }

    public XExtension(string ext)
    {
        this.Ext = AddDotToExtension(ext);
    }

    private static string AddDotToExtension(string extension)
    {
        if (string.IsNullOrEmpty(extension))
            return extension;
        return extension.StartsWith('.')
                    ? extension
                    : $".{extension}";
    }
}
