using UnityEngine;
using VRC.SDKBase;
using VRC.SDK3.Image;
using VRC.Udon.Common.Interfaces;
using UdonSharp;

[UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
public class CalendarImageLoader : UdonSharpBehaviour {
    [SerializeField] bool randomize;
    [SerializeField] float firstDelay = 0;
    [SerializeField] float updateTime = float.PositiveInfinity;
    [SerializeField] float retryTime = 10F;
    [SerializeField] int retryCount = 0;
    [SerializeField] VRCUrl[] sourcePaths;
    [SerializeField] Material sharedMaterial;
    [SerializeField] int materialIndex;
    [SerializeField] string texturePropertyName = "_MainTex";
    [SerializeField] Texture2D loadingImage, defaultImage;
    Texture2D previousImage;
    MaterialPropertyBlock propertyBlock;
    VRCImageDownloader loader;
    IVRCImageDownload downloadRequest;
    int currentRetryCount = 0;
    int imageNumber = -1;
    bool useSharedMaterials;
    new Renderer renderer;
    bool isDownloading;

    void Start() {
        if (sharedMaterial == null) {
            useSharedMaterials = false;
            propertyBlock = new MaterialPropertyBlock();
            renderer = GetComponentInChildren<Renderer>(true);
            sharedMaterial = renderer.sharedMaterials[materialIndex];
        } else useSharedMaterials = true;
        previousImage = defaultImage;
        SendCustomEventDelayedFrames(nameof(_DisplayImage), 0);
        Increment();
        SendCustomEventDelayedSeconds(nameof(_LoadImage), firstDelay);
    }

    public override void Interact() => _ReloadImage();

    public override void OnImageLoadSuccess(IVRCImageDownload image) {
        isDownloading = false;
        currentRetryCount = 0;
        previousImage = downloadRequest.Result;
        if (!useSharedMaterials) SendCustomEventDelayedFrames(nameof(_DisplayImage), 0);
        if (!float.IsInfinity(updateTime)) {
            Increment();
            SendCustomEventDelayedSeconds(nameof(_LoadImage), updateTime);
        }
    }
    

    public override void OnImageLoadError(IVRCImageDownload image) {
        isDownloading = false;
        SendCustomEventDelayedFrames(nameof(_DisplayImage), 0);
        if (currentRetryCount < retryCount) {
            SendCustomEventDelayedSeconds(nameof(_LoadImage), retryTime);
            currentRetryCount++;
        } else {
            currentRetryCount = 0;
            Increment();
            SendCustomEventDelayedSeconds(nameof(_LoadImage), retryTime);
        }
    }

    void Increment() {
        int count = sourcePaths.Length;
        if (count == 0) return;
        imageNumber = randomize ? Random.Range(0, sourcePaths.Length) : (imageNumber + 1) % sourcePaths.Length;
    }

    public void _LoadImage() {
        DisplayImage(loadingImage);
        isDownloading = true;
        if (loader == null) loader = new VRCImageDownloader();
        if (useSharedMaterials) {
            var textureInfo = new TextureInfo();
            textureInfo.MaterialProperty = texturePropertyName;
            downloadRequest = loader.DownloadImage(sourcePaths[imageNumber], sharedMaterial, (IUdonEventReceiver)this, textureInfo);
        } else
            downloadRequest = loader.DownloadImage(sourcePaths[imageNumber], null, (IUdonEventReceiver)this);
    }

    public void _ReloadImage() {
        if (isDownloading) return;
        _LoadImage();
    }

    public void _DisplayImage() => DisplayImage(previousImage != null ? previousImage : defaultImage);

    void DisplayImage(Texture2D image) {
        if (useSharedMaterials)
            sharedMaterial.SetTexture(texturePropertyName, image);
        else {
            propertyBlock.SetTexture(texturePropertyName, image);
            renderer.SetPropertyBlock(propertyBlock, materialIndex);
        }
    }
}
