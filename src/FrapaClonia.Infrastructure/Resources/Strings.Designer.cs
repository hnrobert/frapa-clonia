namespace FrapaClonia.Infrastructure.Resources;

using System.Resources;

/// <summary>
///   A strongly-typed resource class for looking up localized strings.
/// </summary>
public static class Strings
{
    private static ResourceManager resourceMan;

    /// <summary>
    ///   Returns the cached ResourceManager instance used by this class.
    /// </summary>
    public static ResourceManager ResourceManager
    {
        get
        {
            if (resourceMan is null)
            {
                resourceMan = new ResourceManager(
                    "FrapaClonia.Infrastructure.Resources.Strings",
                    typeof(Strings).Assembly);
            }
            return resourceMan;
        }
    }
}
