using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using ImageProcessor.Imaging.Formats;
using System.Drawing;
using ImageProcessor;

namespace PngCompress
{
    class Program
    {
        static int errorCount = 0;
        static ISupportedImageFormat format = new JpegFormat { Quality = 60 };
        static Size size = new Size(1920, 1080);
        static string[] patterns = new string[] { "*.png", "*.bmp", "*.jpg" };
        static void Main(string[] args)
        {
            if (args == null || args.Length <= 0)
            {
                Console.WriteLine("empty args");
                return;
            }

            string dir = args[0];

            if (!Directory.Exists(dir))
            {
                if (File.Exists(dir))
                {
                    handleFile(dir);
                }
                else
                {
                    Console.WriteLine("dir not exists: {0}", dir);
                }
                return;
            }
            else
            {
                handleDir(dir);
            }            
        }

        static void handleDir(string dir)
        {
            foreach (string pattern in patterns)
            {
                string[] files = Directory.GetFiles(dir, pattern, SearchOption.AllDirectories);

                Console.WriteLine("Find Files:{0}, p:{2}, d:{1}", files.Length, dir, pattern);

                files.AsParallel().ForAll(f =>
                {
                    handleFile(f);
                });
            }
        }

        static void handleFile(string file)
        {
            if (errorCount > 5)
            {
                Console.WriteLine("ERROR: Canceled {0}", file);
                return;
            }
            int dot = file.LastIndexOf('.');
            if (dot <= 0)
            {
                return;
            }
            string outputFile = file.Substring(0, dot) + ".jpg";
            bool success = Compress(file, outputFile);
            if (!success)
            {
                errorCount++;
            }
        }

        static bool Compress(string input, string output)
        {
            try
            {
                byte[] bytes = File.ReadAllBytes(input);


                using (MemoryStream stream = new MemoryStream(bytes))
                {
                    using (ImageFactory factory = new ImageFactory(preserveExifData: true))
                    {
                        factory.Load(stream);

                        if (factory.Image.Size.Height <= size.Height && factory.Image.Size.Width <= size.Width)
                        {
                            Console.WriteLine("IGNORE: Resize Enough, f:{0}", input);
                            return true;
                        }

                        using (MemoryStream outStream = new MemoryStream())
                        {
                            factory.Resize(size)
                                .Format(format)
                                .Save(outStream);

                            bytes = outStream.ToArray();
                        }
                    }
                }

                File.WriteAllBytes(output, bytes);
                if (!input.Equals(output))
                {
                    File.Delete(input);
                }
                Console.WriteLine("SUCCESS: {0}", output);
            } 
            catch (Exception e)
            {
                Console.WriteLine("ERROR: {0}, f:{1}", e.Message, input);
                return false;
            }
            return true;
        }
    }
}
