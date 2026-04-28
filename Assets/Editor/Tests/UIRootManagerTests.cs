using System.Reflection;
using NUnit.Framework;

public sealed class UIRootManagerTests
{
    [Test]
    public void HandbookHotkeyOpensPhotoAlbumWhenPhotosExist()
    {
        Assert.AreEqual(IllustratedHandbookPage.PhotoAlbum, ResolveHandbookHotkeyPage(true));
    }

    [Test]
    public void HandbookHotkeyKeepsPersonalInfoFallbackWithoutPhotos()
    {
        Assert.AreEqual(IllustratedHandbookPage.PersonalInformation, ResolveHandbookHotkeyPage(false));
    }

    private static IllustratedHandbookPage ResolveHandbookHotkeyPage(bool hasPhotoEntries)
    {
        MethodInfo method = typeof(UIRootManager).GetMethod(
            "ResolveHandbookHotkeyPage",
            BindingFlags.Static | BindingFlags.NonPublic,
            null,
            new[] { typeof(bool) },
            null);
        Assert.IsNotNull(method);

        object resolved = method.Invoke(null, new object[] { hasPhotoEntries });
        return (IllustratedHandbookPage)resolved;
    }
}
