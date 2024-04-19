using System;
using System.Collections.Generic;
using System.IO;

namespace MangaManager.Tasks.Convert
{
    public static class ImageDetection
    {
        //https://en.wikipedia.org/wiki/List_of_file_signatures
        private static readonly Dictionary<string, string[][]> SignatureTable = new Dictionary<string, string[][]>
        {
            {
                "png",
                new[]
                {
                    new[] {"89", "50", "4E", "47", "0D", "0A", "1A", "0A"},
                }
            },
            {
                "jpg",
                new[]
                {
                    new[] {"FF", "D8", "FF"},
                    //new[] {"FF", "D8", "FF", "DB"},
                    //new[] {"FF", "D8", "FF", "EE"},
                    //new[] {"FF", "D8", "FF", "E0", "00", "10", "4A", "46", "49", "46", "00", "01"},
                    //new[] {"FF", "D8", "FF", "E1", ""  , ""  , "45", "78", "69", "66", "00", "00"},
                    //new[] {"FF", "4F", "FF", "51"},
                    new[] {"00", "00", "00", "0C", "6A", "50", "20", "20", "0D", "0A", "87", "0A"},
                }
            },
            {
                "webp",
                new[]
                {
                    new[] {"52", "49", "46", "46", ""  , ""  , ""  , ""   , "57", "45", "42", "50"},
                }
            },
            {
                "bmp",
                new []
                {
                    new[] { "42", "4D" },
                }
            },
            {
                "gif",
                new[]
                {
                    new [] { "47", "49", "46", "38", "37", "61" },
                    new [] { "47", "49", "46", "38", "39", "61" },
                }
            },
            {
                "tiff",
                new[]
                {
                    new [] { "49", "49", "2A", "00" },
                    new [] { "4D", "4D", "00", "2A" },
                }
            },
            {
                "ico",
                new[]
                {
                    new[] {"00", "00", "01", "00"},
                }
            },
        };

        private static string GetImageExtension(this byte[] imageData)
        {
            foreach (KeyValuePair<string, string[][]> signatureEntry in SignatureTable)
            {
                foreach (string[] signature in signatureEntry.Value)
                {
                    if (imageData.Length < signature.Length)
                    {
                        continue;
                    }

                    bool isMatch = true;
                    for (int i = 0; i < signature.Length; i++)
                    {
                        if (string.IsNullOrEmpty(signature[i]) || signature[i] == imageData[i].ToString("X2"))
                        {
                            continue;
                        }
                        isMatch = false;
                        break;
                    }

                    if (isMatch)
                    {
                        return signatureEntry.Key;
                    }
                }
            }
            throw new ArgumentException("The byte array did not match any known image file signatures.");
        }
        private static string GetImageExtension(this Stream imageStream)
        {
            foreach (KeyValuePair<string, string[][]> signatureEntry in SignatureTable)
            {
                foreach (string[] signature in signatureEntry.Value)
                {
                    if (imageStream.Length < signature.Length)
                    {
                        continue;
                    }

                    var imageData = new byte[signature.Length];
                    imageStream.Read(imageData, 0, signature.Length);
                    imageStream.Position = 0;

                    bool isMatch = true;
                    for (int i = 0; i < signature.Length; i++)
                    {
                        if (string.IsNullOrEmpty(signature[i]) || signature[i] == imageData[i].ToString("X2"))
                        {
                            continue;
                        }
                        isMatch = false;
                        break;
                    }

                    if (isMatch)
                    {
                        return signatureEntry.Key;
                    }
                }
            }
            throw new ArgumentException("The byte array did not match any known image file signatures.");
        }

        public static bool TryGetImageExtension(this byte[] imageData, out string extenstion)
        {
            try
            {
                extenstion = imageData.GetImageExtension();
            }
            catch
            {
                extenstion = string.Empty;
                return false;
            }
            return true;
        }
        public static bool TryGetImageExtension(this Stream imageStream, out string extenstion)
        {
            try
            {
                extenstion = imageStream.GetImageExtension();
            }
            catch
            {
                extenstion = string.Empty;
                return false;
            }
            return true;
        }
        public static bool TryGetImageExtensionFromFile(string filePath, out string extenstion)
        {
            try
            {
                using var inputStream = File.OpenRead(filePath);
                extenstion = inputStream.GetImageExtension();
            }
            catch
            {
                extenstion = string.Empty;
                return false;
            }
            return true;
        }
    }
}