using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Nautilus.Assets;
using Nautilus.Utility;
using UnityEngine;

namespace SubnauticaCommons
{
    /// <summary>
    /// A collection of useful things that didn't fit anywhere else, or that are just very general Unity things.
    /// </summary>
    public static class Hootils
    {
        /// <summary>
        /// Get the mod assembly.
        /// </summary>
        public static Assembly GetAssembly()
        {
            return Assembly.GetExecutingAssembly();
        }

        /// <summary>
        /// Get the full path to an asset file.
        /// </summary>
        /// <param name="fileName">The name of the file, assuming that it is directly inside the Assets folder.</param>
        /// <param name="isEmbedded">Whether the file is shipped separately or embedded into the assembly as a
        /// resource.</param>
        public static string GetAssetHandle(string fileName, bool isEmbedded = false)
        {
            if (isEmbedded)
                return $"{GetAssembly().GetName().Name}.Assets.{fileName}";
            return Path.Combine(GetModDirectory(), "Assets", fileName);
        }
        
        /// <summary>
        /// Get the ideal filename for the config file based on the name of the mod.
        /// </summary>
        public static string GetConfigFileName(string modName)
        {
            return $"{modName.Replace(" ", string.Empty)}.cfg";
        }
        
        /// <summary>
        /// Get the installation directory of the mod.
        /// </summary>
        public static string GetModDirectory()
        {
            return new FileInfo(Assembly.GetExecutingAssembly().Location).Directory?.FullName;
        }
        
        // Enums
        
        /// <summary>
        /// Try parsing a string to the given type of Enum.
        /// </summary>
        /// <param name="value">The string to parse.</param>
        /// <returns>The parsed Enum if successful, or the Enum's default value if not.</returns>
        public static TEnum ParseEnum<TEnum>(string value) where TEnum : struct
        {
            if (!Enum.TryParse(value, true, out TEnum result))
            {
                return default;
            }

            return result;
        }
        
        // Prefabs

        /// <summary>
        /// Create a basic Nautilus prefabinfo with a sprite. Defaults to not unlocked at start.
        /// </summary>
        public static PrefabInfo CreatePrefabInfo(string classId, string displayName, string description,
            Atlas.Sprite sprite)
        {
            return PrefabInfo
                .WithTechType(classId, displayName, description, unlockAtStart: false, techTypeOwner: GetAssembly())
                .WithIcon(sprite);
        }
        
        // Sprites, textures, and colouring
        
        /// <summary>
        /// Add a color tag to the given text.
        /// </summary>
        public static string ColorText(string text, Color color)
        {
            return $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{text}</color>";
        }
        
        /// <summary>
        /// Subnautica packages its textures as non-readable, which makes it almost impossible to modify. This method
        /// produces an editable copy. Graciously provided by Nitrox.
        /// </summary>
        public static Texture2D CloneTexture(Texture2D sourceTexture)
        {
            // Create a temporary RenderTexture of the same size as the texture
            RenderTexture tmp = RenderTexture.GetTemporary(
                sourceTexture.width,
                sourceTexture.height,
                0,
                RenderTextureFormat.Default,
                RenderTextureReadWrite.Linear);

            // Blit the pixels on texture to the RenderTexture
            Graphics.Blit(sourceTexture, tmp);
            // Backup the currently set RenderTexture
            RenderTexture previous = RenderTexture.active;
            // Set the current RenderTexture to the temporary one we created
            RenderTexture.active = tmp;
            // Create a new readable Texture2D to copy the pixels to it
            Texture2D clonedTexture = new Texture2D(sourceTexture.width, sourceTexture.height);
            // Copy the pixels from the RenderTexture to the new Texture
            clonedTexture.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
            clonedTexture.Apply();
            // Reset the active RenderTexture
            RenderTexture.active = previous;
            // Release the temporary RenderTexture
            RenderTexture.ReleaseTemporary(tmp);

            return clonedTexture;
            // "clonedTexture" now has the same pixels from "texture" and it's readable.
        }

        /// <summary>
        /// Get an already loaded sprite from the game by its TechType.
        /// </summary>
        public static Atlas.Sprite GetSprite(TechType techType)
        {
            return SpriteManager.Get(techType);
        }

        /// <summary>
        /// Load a sprite from the assets folder by its filename.
        /// </summary>
        /// <param name="fileName">The name of the file, assuming that it is directly inside the Assets folder.</param>
        /// <param name="isEmbedded">Whether the file is shipped separately or embedded into the assembly as a
        /// resource.</param>
        public static Atlas.Sprite LoadSprite(string fileName, bool isEmbedded = false)
        {
            if (!isEmbedded)
                return ImageUtils.LoadSpriteFromFile(GetAssetHandle(fileName));
            
            // Embedded files are a bit more difficult to deal with. Load them into memory first, then pass them
            // to the texture handlers.
            string resourceName = GetAssetHandle(fileName, true);
            if (!GetAssembly().GetManifestResourceNames().Any(r => r.Equals(resourceName)))
                throw new FileNotFoundException($"Could not find embedded resource {resourceName}");
            
            // Things can still go wrong while reading the texture (although it shouldn't!). Catch exceptions
            // just in case so the mod as a whole won't crash.
            var stream = GetAssembly().GetManifestResourceStream(resourceName);
            try
            {
                var texture = new Texture2D(2, 2, TextureFormat.BC7, false);
                texture.LoadImage(stream.ReadAllBytes());
                return ImageUtils.LoadSpriteFromTexture(texture);
            }
            catch (UnityException ex)
            {
                // We don't have access to any mod's logger here, so use Unity's instead.
                new Logger().LogException(ex);
                return null;
            }
        }
        
        /// <summary>
        /// Change the hue of all colors of a sprite. Saturation and vibrancy are retained.
        /// </summary>
        public static Sprite RecolorSprite(Sprite sprite, Color newColor)
        {
            Texture2D texture = CloneTexture(sprite.texture);
            for (int x = 0; x < texture.width; x++)
            {
                for (int y = 0; y < texture.height; y++)
                {
                    Color oldColor = texture.GetPixel(x, y);
                    var swapped = SwapColor(oldColor, newColor);
                    texture.SetPixel(x, y, swapped);
                }
            }
            texture.Apply();
            return Sprite.Create(texture, sprite.rect, sprite.pivot);
        }

        /// <summary>
        /// Swaps in a new color while retaining the general "feel" of the old one by changing hue but retaining
        /// saturation and vibrancy.
        /// </summary>
        public static Color SwapColor(Color oldColor, Color newColor)
        {
            Color.RGBToHSV(oldColor, out _, out float s, out float v);
            Color.RGBToHSV(newColor, out float replacementHue, out _, out _);
            return Color.HSVToRGB(replacementHue, s, v).WithAlpha(oldColor.a);
        }
        
        /// <summary>
        /// Wrap a try-catch block around a coroutine to more easily catch exceptions that happen inside.
        /// Calls the callback action if an exception occurs.
        /// </summary>
        public static IEnumerator WrapCoroutine(IEnumerator coroutine, Action<Exception> callback)
        {
            object current = null;
            while (true)
            {
                try
                {
                    if (!coroutine.MoveNext())
                        break;
                    current = coroutine.Current;
                }
                catch (Exception ex)
                {
                    callback(ex);
                }
                // Yield statements cannot be inside try-catch blocks. This is what made the whole method necessary.
                yield return current;
            }
        }
    }

    public static class StreamExtensions
    {
        /// <summary>
        /// Read a stream to the end and return the entire content all at once.
        /// </summary>
        public static byte[] ReadAllBytes(this Stream stream)
        {
            List<byte> bytes = new List<byte>();
            byte[] buffer = new byte[1024];

            long bytesToRead = stream.Length;
            while (bytes.Count < bytesToRead)
            {
                int numRead = stream.Read(buffer, 0, buffer.Length);
                if (numRead <= 0)
                    break;
                bytes.AddRange(buffer);
            }

            return bytes.ToArray();
        }
    }
}