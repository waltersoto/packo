using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Yahoo.Yui.Compressor;

namespace packo
{
    public class PackageProcessor
    {

        private readonly List<ProcessingError> _errors; 
      
        public PackageProcessor() { _errors = new List<ProcessingError>(); }

        public PackageProcessor(BuildPackage package,FileInfo buildFile):this()
        {
            Package = package;
            BuildFile = buildFile;
        }

        public bool PreProcess()
        {
            if (!Valid()) return false;

            string src = Package.Source.Trim();
            string rel = Package.Release.Trim();

            if (src.StartsWith("./") || src.StartsWith(@".\"))
            {
                src = src.Substring(2);
                src = string.Concat(BuildFile.DirectoryName, @"\", src);
                Package.Source = src.TrimEnd('/').TrimEnd(@"\"[0]); 

                if (!Directory.Exists(src))
                {
                    _errors.Add(new ProcessingError
                    {
                        Id = 404,
                        Message = string.Format(Message.DoesNotExists, PackageFolderNames.Source)
                    });
                }

            }

            if (!rel.StartsWith("./") && !rel.StartsWith(@".\")) return _errors.Count == 0;

            rel = rel.Substring(2);
            rel = string.Concat(BuildFile.DirectoryName, @"\", rel);

            Package.Release = rel.TrimEnd('/').TrimEnd(@"\"[0]);

            if (!Directory.Exists(rel))
            {
                _errors.Add(new ProcessingError
                {
                    Id = 404,
                    Message = string.Format(Message.DoesNotExists, PackageFolderNames.Release)
                });
            }


            return _errors.Count == 0;

        }

        public void Process()
        {

            foreach (Action action in Package.Actions)
            {
                switch (action.Type.Trim().ToLower())
                {
                    case ActionNames.Bundle: Bundle(action);
                        break;
                    case ActionNames.Minify: Minify(action);
                        break;
                }    
            }
            
        }

        private Dictionary<string,string> ReadFileFromSetting(string filename, string defaultResult)
        {
            if ((filename.Contains("/") || filename.Contains(@"\"[0])) && File.Exists(filename))
            {
                return new Dictionary<string, string> { { filename, File.ReadAllText(filename) } };
            }
         
            string addPath = string.Concat(Package.Source, @"/", filename);

            return File.Exists(addPath)
                ? new Dictionary<string, string>() { { addPath, File.ReadAllText(addPath) } } 
                : new Dictionary<string, string>() {{addPath, defaultResult}};
        }

     

        private void Bundle(Action action)
        {
            // action.Filename;
            var source = new DirectoryInfo(Package.Source);
            var toSkip = new List<string>();
           
            if (!source.Exists) return;

            var settings = action.Settings;

            string enclose = Templates.Enclose;
            string license = "";


            if (settings != null && settings.Length > 0)
            {
                foreach (var setting in settings)
                {
                    switch (setting.Key.Trim().ToLower())
                    {
                        case SettingNames.Enclose:
                        {
                            Console.WriteLine(Message.AddEnclose,setting.Value);
                            var result = ReadFileFromSetting(setting.Value, Templates.Enclose).FirstOrDefault(); 
                            enclose = result.Value;
                            var file = new FileInfo(result.Key);
                            toSkip.Add(file.Name);
                        }
                                
                            break;
                        case SettingNames.License:
                        {
                            Console.WriteLine(Message.AddLicense,setting.Value);
                            var result = ReadFileFromSetting(setting.Value, "").FirstOrDefault();
                            license = result.Value;
                            var file = new FileInfo(result.Key);
                            toSkip.Add(file.Name);
                        }
                                
                            break;
                    }
                }
            }

            StringBuilder bundle = new StringBuilder();

            foreach (FileInfo file in source.GetFiles().Where(m => m.Extension.Trim().ToLower() == SupportedExtensions.Js)
                .Where(file => toSkip.All(m => !String.Equals(m.Trim(), file.Name.Trim(), StringComparison.CurrentCultureIgnoreCase))))
            {
                bundle.Append(File.ReadAllText(file.FullName));
                bundle.Append(Environment.NewLine);
                Console.WriteLine(Message.Adding, file.Name);
            }

            string bundled = string.Concat(Path.GetRandomFileName(),SupportedExtensions.Js);

            if (!string.IsNullOrEmpty(action.Filename))
            {
                bundled = action.Filename.Replace(Templates.Version, Package.Version).Replace(Templates.Package, Package.Package);
                    
            }

            string toSave = string.Concat(Package.Release, @"\", bundled);

            string content = string.Concat(license, Environment.NewLine, Environment.NewLine,
                enclose.Replace(Templates.Enclose, bundle.ToString()));
            Console.WriteLine(Message.Saving,bundled);
            File.WriteAllText(toSave,content);

            Console.WriteLine(Message.Completed, ActionNames.Bundle);
        }

        private void Minify(Action action)
        {
            var source = new DirectoryInfo(Package.Release);
            List<string> ignore = new List<string>();

            if (string.IsNullOrEmpty(action.Filename))
            {
                action.Filename = Templates.MinifyFilename;
            }

            if (action.Ignore != null && action.Ignore.Length > 0)
            {
                ignore = action.Ignore.ToList();
            }

            if (!source.Exists) return;

            foreach (var file in source.GetFiles()
                .Where(m => m.Extension.Trim().ToLower() == SupportedExtensions.Js 
                    || m.Extension.Trim().ToLower() == SupportedExtensions.Css))
            {
                bool skip = false;

                string extension = file.Extension.Trim().ToLower();
                string cleanExtension = extension;
                
                if (cleanExtension.StartsWith("."))
                {
                    cleanExtension = cleanExtension.TrimStart('.');
                }

                foreach (string pattern in ignore)
                {
                    
                    string toMatch = pattern.Replace("*", "");

                    if (pattern.StartsWith("*") && pattern.EndsWith("*"))
                    {
                        skip = (file.Name.Contains(toMatch));
                    }
                    else if (pattern.StartsWith("*"))
                    {
                        skip = (file.Name.EndsWith(toMatch));
                    }
                    else if (pattern.EndsWith("*"))
                    {
                        skip = file.Name.StartsWith(toMatch);
                    }
                    else
                    {
                        skip = String.Equals(file.Name.Trim(), toMatch.Trim(), StringComparison.CurrentCultureIgnoreCase);
                    }
                        
                }

                if (skip) continue;

                string txt = File.ReadAllText(file.FullName);


                if (extension == SupportedExtensions.Js)
                {
                    var compressor = new JavaScriptCompressor
                    {
                        PreserveAllSemicolons = true
                    };

                    txt = compressor.Compress(txt);
                }
                else if (extension == SupportedExtensions.Css)
                {
                    var compressor = new CssCompressor();

                    txt = compressor.Compress(txt);

                }

                string extensionless = file.Name.TrimEnd(extension.ToCharArray());
                        
                string newFile = action.Filename.Replace(Templates.Filename,extensionless)
                    .Replace(Templates.Version, Package.Version)
                    .Replace(Templates.Package, Package.Package)
                    .Replace(Templates.Extension, cleanExtension);

                string toSave = string.Concat(Package.Release, @"\", newFile);

                Console.WriteLine(Message.Saving, newFile);

                File.WriteAllText(toSave, txt);
            }

            Console.WriteLine(Message.Completed, ActionNames.Minify);

        }

        public bool Valid()
        {
            if (Package == null) return false;
            if (Package.Actions != null && !Package.Actions.Any()) return false;
            if (Package.Source == null)
            {
                Console.WriteLine(Message.Invalid,PackageFolderNames.Source);
                return false;
            }
            if (Package.Release == null)
            {
                Console.WriteLine(Message.Invalid, PackageFolderNames.Release);
                return false;
            }

            int validActions = 0;

            if (Package.Actions != null)
            {
                validActions = Package.Actions.Where(action => action != null).Count(action => action.Type != null);
            }

            if (validActions < 1)
            {
                Console.WriteLine(Message.NoValidActions);
            }

            return validActions > 0;
            
        }

        public BuildPackage Package { set; get; }
        public FileInfo BuildFile { set; get; }

    }
}
