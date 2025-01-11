using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Runtime.Rendering
{
    public class AutoExposureRendererFeature : ScriptableRendererFeature
    {
        public AutoExposureRenderPass pass;
        public Material blitMaterial;
        public Texture2D buffer;
        public Color color;
        
        public override void Create()
        {
            //pass = new AutoExposureRenderPass();
            //pass.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
            //buffer = new Texture2D(1, 1, GraphicsFormat.B10G11R11_UFloatPack32, TextureCreationFlags.None);
        }

        private void OnDestroy()
        {
            //Destroy(buffer);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            //pass.blitMaterial = blitMaterial;
            //pass.buffer = buffer;
            //color = pass.color;
            //renderer.EnqueuePass(pass);
        }

        public class AutoExposureRenderPass : ScriptableRenderPass
        {
            public Material blitMaterial;
            public Texture2D buffer;
            public Color color;

            public AutoExposureRenderPass()
            {
                profilingSampler = new ProfilingSampler("AutoExposure");
            }
            
            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                if (blitMaterial == null) return;
                
                var resourcesData = frameData.Get<UniversalResourceData>();
                
                using (var builder = renderGraph.AddUnsafePass("AutoExposure", out PassData passData, profilingSampler))
                {
                    builder.AllowPassCulling(false);
                    
                    passData.src = resourcesData.activeColorTexture;
                    
                    var textureDesc = renderGraph.GetTextureDesc(resourcesData.cameraColor);
                    var width = Mathf.Min(textureDesc.width, textureDesc.height);
                    var count = 0;
                    while (width > 1)
                    {
                        width /= 2;
                        count++;
                    }

                    passData.pyramid = new TextureHandle[count];
                    for (var i = 0; i < count - 1; i++)
                    {
                        textureDesc.name = $"_AutoExposureMip{i}";
                        textureDesc.width /= 2;
                        textureDesc.height /= 2;

                        passData.pyramid[i] = renderGraph.CreateTexture(textureDesc);
                        builder.UseTexture(passData.pyramid[i], AccessFlags.ReadWrite);
                    }
                    
                    textureDesc.name = $"_AutoExposureMip{count - 1}";
                    textureDesc.width = 1;
                    textureDesc.height = 1;

                    passData.pyramid[^1] = renderGraph.CreateTexture(textureDesc);
                    builder.UseTexture(passData.pyramid[^1], AccessFlags.ReadWrite);
                    
                    builder.SetRenderFunc<PassData>((data, ctx) =>
                    {
                        var cmd = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);
                        Blitter.BlitCameraTexture(cmd, data.src, data.pyramid[0], blitMaterial, 0);
                        for (var i = 0; i < data.pyramid.Length - 1; i++)
                        {
                            Blitter.BlitCameraTexture(cmd, data.pyramid[i], data.pyramid[i + 1], blitMaterial, 0);
                        }

                        var rt = ((RTHandle)data.pyramid[^1]).rt;
                        Debug.Log(rt.graphicsFormat);
                        Graphics.CopyTexture(rt, buffer);
                        color = buffer.GetPixel(0, 0);
                    });
                }
            }

            public class PassData
            {
                public Material material;
                public TextureHandle src;
                public TextureHandle[] pyramid;
            }
        }
    }
}