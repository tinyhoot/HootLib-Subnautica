using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using Nautilus.Utility;
using UnityEngine;

namespace HootLib
{
    public static class BasicTextExtensions
    {
        /// <summary>
        /// Grab the component responsible for fading out the text.
        /// </summary>
        public static uGUI_TextFade GetTextFade(this BasicText basicText)
        {
            try
            {
                var field = AccessTools.Property(typeof(BasicText), "TextFade");
                return field?.GetValue(basicText) as uGUI_TextFade;
            }
            catch (NullReferenceException)
            {
                return null;
            }
        }
        
        /// <summary>
        /// Nautilus does not want us to have this, so we just take it anyway. GameObject the text is attached to.
        /// </summary>
        public static GameObject GetTextObject(this BasicText basicText)
        {
            try
            {
                var field = AccessTools.Property(typeof(BasicText), "TextObject");
                return field?.GetValue(basicText) as GameObject;
            } catch (NullReferenceException)
            {
                return null;
            }
        }
    }

    public static class EnumerableExtensions
    {
        /// <summary>
        /// Turns all elements of an enumerable into a string for easy printing. Evaluates the enumerable to completion.
        /// </summary>
        public static string ElementsToString(this IEnumerable enumerable)
        {
            string result = "";
            foreach (var element in enumerable)
            {
                string elemString;
                // Handle nested enumerables.
                if (element.GetType() != typeof(string) && element is IEnumerable innerEnumerable)
                    elemString = innerEnumerable.ElementsToString();
                else
                    elemString = element.ToString();
                
                if (result != "")
                    result += "| ";
                result += elemString;
            }
            return $"[{enumerable.GetType().Name}: {result} ]";
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