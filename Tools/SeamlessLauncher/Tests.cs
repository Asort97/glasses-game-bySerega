using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace GlassesLauncher
{
    internal static class Tests
    {
        private static int Main()
        {
            string file = Path.Combine(Path.GetTempPath(), "GlassesCaptureTest_" + Guid.NewGuid().ToString("N"));
            try
            {
                using (var image = new Bitmap(2, 2, PixelFormat.Format32bppArgb))
                {
                    image.SetPixel(0, 0, Color.Red);
                    image.SetPixel(1, 0, Color.Lime);
                    image.SetPixel(0, 1, Color.Blue);
                    image.SetPixel(1, 1, Color.FromArgb(32, 12, 34, 56));
                    LaunchContext.WriteCapture(image, file);
                }

                using (var reader = new BinaryReader(File.OpenRead(file)))
                {
                    Check(reader.ReadInt32() == 2 && reader.ReadInt32() == 2, "Dimensions");
                    byte[] expected = { 0, 0, 255, 255, 0, 255, 0, 255, 255, 0, 0, 255, 56, 34, 12, 255 };
                    byte[] actual = reader.ReadBytes(expected.Length);
                    Check(actual.Length == expected.Length, "Pixel count");
                    for (int i = 0; i < expected.Length; i++)
                        Check(actual[i] == expected[i], "Top-down BGRA/opaque alpha byte " + i);
                    Check(reader.BaseStream.Position == reader.BaseStream.Length, "Exact payload length");
                }
                Console.WriteLine("PASS: capture header, BGRA channels, row order, opaque alpha, payload length.");
                return 0;
            }
            catch (Exception error)
            {
                Console.Error.WriteLine(error);
                return 1;
            }
            finally { if (File.Exists(file)) File.Delete(file); }
        }

        private static void Check(bool result, string name)
        {
            if (!result) throw new InvalidDataException(name);
        }
    }
}
