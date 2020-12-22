using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.XPath;

namespace ShapeScriptMagic
{
    public class Program
    {
        static void Main(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var command = args[0];
            switch (command)
            {
                case "export":
                    {
                        var mdgFile = Environment.ExpandEnvironmentVariables(args[1]);
                        var outputDir = Environment.ExpandEnvironmentVariables(args[2]);
                        Export(mdgFile, outputDir);
                    }
                    break;
                case "modify":
                    {
                        var inputDir = Environment.ExpandEnvironmentVariables(args[1]);
                        var mdgFile = Environment.ExpandEnvironmentVariables(args[2]);
                        Modify(inputDir, mdgFile);
                    }
                    break;
                default:
                    break;
            }
        }

        public static void Export(string mdgFile, string outputDir)
        {
            XPathDocument sourceDoc = new XPathDocument(mdgFile);
            var navigator = sourceDoc.CreateNavigator();
            var stereotypes = navigator.Select("//UMLProfile/Content/Stereotypes/Stereotype[count(./Image)=1]");
            foreach (XPathNavigator x in stereotypes)
            {
                string name = x.GetAttribute("name", "");
                var image = x.SelectSingleNode("./Image");
                var base64encoded = image.Value;
                FromBase64Transform b64 = new FromBase64Transform(FromBase64TransformMode.IgnoreWhiteSpaces);
                byte[] inputBytes = Encoding.GetEncoding(1252).GetBytes(base64encoded);
                byte[] outputBytes = new byte[b64.OutputBlockSize];
                string zipFile = Path.Combine(outputDir, name + ".zip");
                using (var zipStream = new FileStream(zipFile, FileMode.Create))
                {
                    //Transform the data in chunks the size of InputBlockSize. 
                    int i = 0;
                    while (inputBytes.Length - i > b64.InputBlockSize)
                    {
                        int bytesWritten = b64.TransformBlock(inputBytes, i, b64.InputBlockSize, outputBytes, 0);
                        i += b64.InputBlockSize;
                        zipStream.Write(outputBytes, 0, bytesWritten);
                    }
                    zipStream.Flush();
                    zipStream.Close();
                    zipStream.Dispose();

                    using (var zipStream2 = new FileStream(zipFile, FileMode.Open))
                    {
                        ZipArchive zipArchive = new ZipArchive(zipStream2);
                        ZipArchiveEntry zipArchiveEntry = zipArchive.GetEntry("str.dat");
                        using (StreamReader reader = new StreamReader(zipArchiveEntry.Open(), Encoding.Unicode))
                        {
                            var script = reader.ReadToEnd();
                            StreamWriter streamWriter = new StreamWriter(new FileStream(Path.Combine(outputDir, name + ".shapescript"), FileMode.Create), Encoding.Unicode);
                            streamWriter.Write(script);
                            streamWriter.Flush();
                            streamWriter.Close();
                        }
                    }
                    File.Delete(zipFile);
                }
            }
        }

        public static void Modify(string inputDir, string mdgFile)
        {
            string[] files = Directory.GetFiles(inputDir, "*.shapescript");
            var filesDict = files.ToDictionary(s => Path.GetFileNameWithoutExtension(s));
            XmlDocument doc = new XmlDocument();
            doc.Load(mdgFile);
            var stereotypes = doc.SelectNodes("//UMLProfile/Content/Stereotypes/Stereotype[count(./Image)=1]");
            foreach (XmlNode x in stereotypes)
            {
                string name = x.Attributes["name"].Value;
                if (filesDict.TryGetValue(name, out string newShape))
                {
                    string zipFile = Path.Combine(Path.GetDirectoryName(newShape), Path.GetFileNameWithoutExtension(newShape) + ".zip");
                    using (FileStream newShapeStream = new FileStream(zipFile, FileMode.Create))
                    {
                        using (ZipArchive zipArchive = new ZipArchive(newShapeStream, ZipArchiveMode.Create, true))
                        {
                            ZipArchiveEntry zipArchiveEntry = zipArchive.CreateEntryFromFile(newShape, "str.dat", CompressionLevel.Fastest);
                        }
                    }
                    byte[] inputBytes = File.ReadAllBytes(zipFile);
                    // Create a new ToBase64Transform object to convert to base 64.
                    ToBase64Transform base64Transform = new ToBase64Transform();

                    // Create a new byte array with the size of the output block size.
                    byte[] outputBytes = new byte[base64Transform.OutputBlockSize];

                    // Verify that multiple blocks can not be transformed.
                    if (!base64Transform.CanTransformMultipleBlocks)
                    {
                        // Initializie the offset size.
                        int inputOffset = 0;

                        // Iterate through inputBytes transforming by blockSize.
                        int inputBlockSize = base64Transform.InputBlockSize;

                        using (var outputFileStream = new MemoryStream())
                        {
                            while (inputBytes.Length - inputOffset > inputBlockSize)
                            {
                                base64Transform.TransformBlock(
                                    inputBytes,
                                    inputOffset,
                                    inputBytes.Length - inputOffset,
                                    outputBytes,
                                    0);

                                inputOffset += base64Transform.InputBlockSize;
                                outputFileStream.Write(
                                    outputBytes,
                                    0,
                                    base64Transform.OutputBlockSize);
                            }

                            // Transform the final block of data.
                            outputBytes = base64Transform.TransformFinalBlock(
                                inputBytes,
                                inputOffset,
                                inputBytes.Length - inputOffset);

                            outputFileStream.Write(outputBytes, 0, outputBytes.Length);
                            outputFileStream.Seek(0, SeekOrigin.Begin);
                            var reader = new StreamReader(outputFileStream);
                            var base64String = reader.ReadToEnd();
                            var image = x.SelectSingleNode("./Image");
                            image.InnerText = base64String;
                        }
                    }
                }
            }
            doc.Save(XmlWriter.Create(Console.OpenStandardOutput(), new XmlWriterSettings
            {
                Encoding = Encoding.GetEncoding(1252),
                Indent = true,
                CheckCharacters = true,
                ConformanceLevel = ConformanceLevel.Document,
            }));
        }
    }
}
