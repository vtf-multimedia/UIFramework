using UnityEngine;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.IO;

namespace UISystem
{
    /// <summary>
    /// Utility for asynchronously loading and caching textures from StreamingAssets.
    /// </summary>
    public static class TextureLoader
    {
        private static readonly Dictionary<string, Texture2D> _cache = new();
        private static readonly Dictionary<string, UniTask<Texture2D>> _pendingRequests = new();

        public static async UniTask<Texture2D> GetTextureAsync(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath)) return null;

            if (_cache.TryGetValue(relativePath, out var cached)) return cached;
            if (_pendingRequests.TryGetValue(relativePath, out var pending)) return await pending;

            var task = LoadTexture(relativePath);
            _pendingRequests[relativePath] = task;
            
            var result = await task;
            _pendingRequests.Remove(relativePath);
            return result;
        }

        private static async UniTask<Texture2D> LoadTexture(string relativePath)
        {
            // Resolve path for current platform
            string fullPath = Path.Combine(Application.streamingAssetsPath, relativePath).Replace("\\", "/");
            
            // UnityWebRequest requires "file://" prefix on some platforms but not others
            if (!fullPath.Contains("://"))
            {
                if (fullPath.StartsWith("/"))
                    fullPath = "file://" + fullPath;
                else
                    fullPath = "file:///" + fullPath;
            }

            using (var request = UnityWebRequestTexture.GetTexture(fullPath))
            {
                await request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"[TextureLoader] Failed to load texture at: {fullPath}. Error: {request.error}");
                    return null;
                }

                var texture = DownloadHandlerTexture.GetContent(request);
                if (texture != null)
                {
                    texture.name = relativePath;
                    _cache[relativePath] = texture;
                }
                return texture;
            }
        }
        
        public static void ClearCache()
        {
            foreach (var tex in _cache.Values)
            {
                if (tex != null) Object.Destroy(tex);
            }
            _cache.Clear();
        }
    }
}
