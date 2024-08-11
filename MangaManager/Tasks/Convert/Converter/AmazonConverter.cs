using IronPython.Hosting;
using IronPython.Runtime;
using Microsoft.Scripting.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace MangaManager.Tasks.Convert.Converter
{
    public class AmazonConverter : IWorkItemProvider, IWorkItemProcessor
    {
        private List<string> _acceptdExtensions = new List<string> { ".mobi", ".prc", ".azw", ".azw3", ".azw4" /*, ".azw6", ".azw.res", ".kfx" */};

        public IEnumerable<WorkItem> GetItems()
        {
            return _acceptdExtensions
                .SelectMany(extension => Directory.EnumerateFiles(Program.Options.SourceFolder, $"*{extension}", SearchOption.AllDirectories))
                .OrderBy(filePath => filePath)
                .Select(filePath => CacheWorkItems.Create(filePath));
        }

        public bool Accept(WorkItem workItem)
        {
            if (!Program.Options.Convert)
            {
                return false;
            }

            var workingFileName = workItem.FilePath;
            return _acceptdExtensions.Contains(Path.GetExtension(workingFileName));
        }

        private Lazy<dynamic> _pythonWrapperUnpackAzw6 = new Lazy<dynamic>(() =>
        {
            var scriptFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "python", "dumpAZWRES", "dumpAZWRES.py");
            var engine = Python.CreateEngine();
            engine.Runtime.IO.SetOutput(new MemoryStream(), Encoding.UTF8);
            engine.Runtime.IO.SetErrorOutput(new MemoryStream(), Encoding.UTF8);
            return engine.ExecuteFile(scriptFile).GetVariable("main");
        });
        private Lazy<dynamic> _pythonWrapperUnpackAzw8 = new Lazy<dynamic>(() =>
        {
            var scriptFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "python", "kindleUnpack", "kindleunpack.py");
            var engine = Python.CreateEngine();
            engine.Runtime.IO.SetOutput(new MemoryStream(), Encoding.UTF8);
            engine.Runtime.IO.SetErrorOutput(new MemoryStream(), Encoding.UTF8);
            return engine.ExecuteFile(scriptFile).GetVariable("main");
        });

        private void UnpackAzw6(string file, string tmpFolder)
        {
            _pythonWrapperUnpackAzw6.Value(file, tmpFolder);
        }
        private void UnpackAzw8(string file, string tmpFolder)
        {
            _pythonWrapperUnpackAzw8.Value(new[] { string.Empty, "-i", file, tmpFolder });
        }
        private void UnpackAzw10(string file, string tmpFolder)
        {
        }

        private IEnumerable<ArchiveItemStream> GetArchiveItemStreamsFromAzw6(string folder)
        {
            throw new FormatException();
        }
        private IEnumerable<ArchiveItemStream> GetArchiveItemStreamsFromAzw8(string folder)
        {
            //When many mobi version are extracted use latest one
            var opfPath = Directory.EnumerateFiles(folder, "*.opf", SearchOption.AllDirectories).OrderBy(_ => _).Last();
            using var opfReader = new XmlTextReader(opfPath);
            opfReader.Namespaces = false;
            var opfDocument = new XmlDocument();
            opfDocument.Load(opfReader);
            var manifestItems = opfDocument.GetElementsByTagName("manifest").OfType<XmlElement>().SelectMany(manifest => manifest.GetElementsByTagName("item").OfType<XmlElement>()).ToArray();
            var spineItemRefs = opfDocument.GetElementsByTagName("spine").OfType<XmlElement>().SelectMany(spine => spine.GetElementsByTagName("itemref").OfType<XmlElement>()).ToArray();
            var imageElements = new[] { "img", "image" };
            var imageSrcAttributes = new[] { "src", "xlink:src", "href", "xlink:href" };
            return spineItemRefs
                .Select(spineItemRef => spineItemRef.GetAttribute("idref"))
                .Select(itemRefId => manifestItems.Single(manifestItem => manifestItem.GetAttribute("id") == itemRefId).GetAttribute("href"))
                .Select(htmlRelativePath => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(opfPath), htmlRelativePath)))
                .SelectMany(htmlPath =>
                {
                    using var reader = new XmlTextReader(htmlPath);
                    reader.Namespaces = false;
                    var xmlDocument = new XmlDocument();
                    xmlDocument.Load(reader);
                    return xmlDocument.SelectNodes("//*").OfType<XmlElement>().Where(element => imageElements.Contains(element.Name))
                    .Select(imageElement => imageElement.Attributes.OfType<XmlAttribute>().Where(attribute => imageSrcAttributes.Contains(attribute.Name)).Single().Value)
                    .Select(imageRelativePath => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(htmlPath), imageRelativePath)));
                })
                .Select(file =>
                {
                    using var inputStream = File.OpenRead(file);
                    var ms = new MemoryStream();
                    inputStream.CopyTo(ms);
                    if (!ms.TryGetImageExtension(out var extension)) { throw new FormatException(); }
                    return new ArchiveItemStream { Stream = ms, TargetExtension = extension };
                });
        }
        private IEnumerable<ArchiveItemStream> GetArchiveItemStreamsFromAzw10(string folder)
        {
            throw new FormatException();
        }

        public void Process(WorkItem workItem)
        {
            var file = workItem.FilePath;

            var tmpFolder = FileHelper.CreateUniqueTempDirectory();
            try
            {
                Action<string, string> unpack;
                Func<string, IEnumerable<ArchiveItemStream>> GetArchiveItemStreams;

                var extension = Path.GetExtension(file);
                switch (extension)
                {
                    case ".res":
                    case ".azw6":
                        if (extension == ".res") { extension = ".azw.res"; }
                        unpack = UnpackAzw6;
                        GetArchiveItemStreams = GetArchiveItemStreamsFromAzw6;
                        break;

                    case ".kfx":
                        unpack = UnpackAzw10;
                        GetArchiveItemStreams = GetArchiveItemStreamsFromAzw10;
                        break;

                    default:
                        unpack = UnpackAzw8;
                        GetArchiveItemStreams = GetArchiveItemStreamsFromAzw8;
                        break;
                }

                unpack(file, tmpFolder);
                var outputPath = FileHelper.GetAvailableFilename(Path.Combine(Path.GetDirectoryName(file), file.Replace(extension, ".cbz")));
                var isSuccess = ArchiveHelper.CreateZipFromArchiveItemStreams(tmpFolder, outputPath, GetArchiveItemStreams);
                if (isSuccess)
                {
                    File.Delete(file);
                    workItem.UpdatePath(outputPath);
                }
            }
            finally
            {
                Directory.Delete(tmpFolder, true);
            }
        }
    }
}