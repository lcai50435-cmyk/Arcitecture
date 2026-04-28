using NUnit.Framework;
using UnityEngine;

public sealed class ScreenAdaptationManagerTests
{
    private GameObject cameraObject;

    [TearDown]
    public void TearDown()
    {
        if (cameraObject != null)
        {
            Object.DestroyImmediate(cameraObject);
        }
    }

    [Test]
    public void AdaptedOrthographicSizeKeepsFirstBaseWhenCameraWasTemporarilyZoomed()
    {
        cameraObject = new GameObject("RuntimeCamera", typeof(Camera));
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5f;

        Assert.IsTrue(ScreenAdaptationManager.TryGetAdaptedOrthographicSize(camera, out float initialSize));

        camera.orthographicSize = initialSize * 0.5f;

        Assert.IsTrue(ScreenAdaptationManager.TryGetAdaptedOrthographicSize(camera, out float sizeAfterTemporaryZoom));
        Assert.AreEqual(initialSize, sizeAfterTemporaryZoom, 0.001f);
    }
}
