using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

/// <summary>Offscreen preview of the real game state. Works in the editor and a hidden standalone test.</summary>
public static class VisualCapture
{
    public static void Capture(Camera camera,string name,int width=1600,int height=900,string folder="Logs/Screenshots")
    {
        if(SystemInfo.graphicsDeviceType==GraphicsDeviceType.Null)return;
        Directory.CreateDirectory(folder);
        var canvas=Object.FindAnyObjectByType<Canvas>();
        var target=new RenderTexture(width,height,24);
        var previous=RenderTexture.active;var oldTarget=camera.targetTexture;
        var mode=canvas.renderMode;var oldCamera=canvas.worldCamera;float distance=canvas.planeDistance;
        var scaler=canvas.GetComponent<CanvasScaler>();bool scalerEnabled=scaler.enabled;float scale=canvas.scaleFactor;
        try
        {
            camera.targetTexture=target;
            scaler.enabled=false;canvas.scaleFactor=Mathf.Min(width/1600f,height/900f);
            canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=camera.nearClipPlane+.01f;
            foreach(var graphic in canvas.GetComponentsInChildren<Graphic>())graphic.SetAllDirty();
            Canvas.ForceUpdateCanvases();camera.Render();
            RenderTexture.active=target;
            var pixels=new Texture2D(width,height,TextureFormat.RGB24,false);
            pixels.ReadPixels(new Rect(0,0,width,height),0,0);pixels.Apply();
            float light=0;
            for(int y=1;y<8;y++)for(int x=1;x<12;x++)light+=pixels.GetPixel(x*width/12,y*height/8).grayscale;
            if(light<.001f) { Object.DestroyImmediate(pixels);throw new System.InvalidOperationException("Blank rendered frame: "+name); }
            File.WriteAllBytes(System.IO.Path.Combine(folder,name+".png"),pixels.EncodeToPNG());Object.DestroyImmediate(pixels);
        }
        finally
        {
            camera.targetTexture=oldTarget;canvas.renderMode=mode;canvas.worldCamera=oldCamera;canvas.planeDistance=distance;
            canvas.scaleFactor=scale;scaler.enabled=scalerEnabled;Canvas.ForceUpdateCanvases();
            RenderTexture.active=previous;target.Release();Object.DestroyImmediate(target);
        }
    }
}
