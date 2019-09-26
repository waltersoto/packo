using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Text.RegularExpressions;
using Yahoo.Yui.Compressor;

namespace packo {
    public class PackageProcessor {

        public List<ProcessingError> Errors { set; get; }

        public PackageProcessor() { Errors = new List<ProcessingError>(); }

        public PackageProcessor(BuildPackage package, FileInfo buildFile) : this() {
            Package = package;
            BuildFile = buildFile;
        }

        public bool PreProcess() {
            if (!Valid()) return false;

            string src = Package.Source.Trim();
            string rel = Package.Release.Trim();

            if (src.StartsWith("./") || src.StartsWith(@".\")) {
                src = src.Substring(2);
                src = string.Concat(BuildFile.DirectoryName, @"\", src);
                Package.Source = src.TrimEnd('/').TrimEnd(@"\"[0]);

                if (!Directory.Exists(src)) {
                    Errors.Add(new ProcessingError {
                        Id = 404,
                        Message = string.Format(Message.DoesNotExists, PackageFolderNames.Source)
                    });
                }

            }

            if (!rel.StartsWith("./") && !rel.StartsWith(@".\")) return Errors.Count == 0;

            rel = rel.Substring(2);
            rel = string.Concat(BuildFile.DirectoryName, @"\", rel);

            Package.Release = rel.TrimEnd('/').TrimEnd(@"\"[0]);

            if (!Directory.Exists(rel)) {
                Errors.Add(new ProcessingError {
                    Id = 404,
                    Message = string.Format(Message.DoesNotExists, PackageFolderNames.Release)
                });
            }


            return Errors.Count == 0;

        }

        public void Process() {

            foreach (var action in Package.Actions) {
                switch (action.Type.Trim().ToLower()) {
                    case ActionNames.Bundle:
                        Bundle(action);
                        break;
                    case ActionNames.Minify:
                        Minify(action);
                        break;
                }
            }

        }

        private Dictionary<string, string> ReadFileFromSetting(string filename, string defaultResult) {
            if ((filename.Contains("/") || filename.Contains(@"\"[0])) && File.Exists(filename)) {
                return new Dictionary<string, string> { { filename, File.ReadAllText(filename) } };
            }

            var addPath = string.Concat(Package.Source, @"/", filename);

            return File.Exists(addPath)
                ? new Dictionary<string, string>() { { addPath, File.ReadAllText(addPath) } }
                : new Dictionary<string, string>() { { addPath, defaultResult } };
        }



        private void Bundle(Action action) {
            // action.Filename;
            var source = new DirectoryInfo(Package.Source);
            var toSkip = new List<string>();

            if (!source.Exists) return;

            var settings = action.Settings;
            var useEncloseFile = false;
            var enclose = Templates.DefaultEnclose;

            var license = "";

            if (settings != null && settings.Length > 0) {
                foreach (var setting in settings) {
                    switch (setting.Key.Trim().ToLower()) {
                        case SettingNames.Enclose: {
                                Console.WriteLine(Message.AddEnclose, setting.Value);
                                var result = ReadFileFromSetting(setting.Value, Templates.DefaultEnclose).FirstOrDefault();
                                enclose = result.Value;
                                var file = new FileInfo(result.Key);
                                toSkip.Add(file.Name);
                                useEncloseFile = true;
                            }

                            break;
                        case SettingNames.License: {
                                Console.WriteLine(Message.AddLicense, setting.Value);
                                var result = ReadFileFromSetting(setting.Value, "").FirstOrDefault();
                                license = result.Value;
                                var file = new FileInfo(result.Key);
                                toSkip.Add(file.Name);
                            }

                            break;
                    }
                }
            }


            var toBundle = new Dictionary<string, string>();

            foreach (FileInfo file in source.GetFiles().Where(m => m.Extension.Trim().ToLower() == SupportedExtensions.Js)
                .Where(file => toSkip.All(m => !string.Equals(m.Trim(), file.Name.Trim(), StringComparison.CurrentCultureIgnoreCase)))) {

                toBundle.Add(string.Format(Templates.Enclose, file.Name), File.ReadAllText(file.FullName));
                Console.WriteLine(Message.Adding, file.Name);
            }

            var bundled = string.Concat(Path.GetRandomFileName(), SupportedExtensions.Js);

            if (!string.IsNullOrEmpty(action.Filename)) {
                bundled = action.Filename.Replace(Templates.Version, Package.Version).Replace(Templates.Package, Package.Package);
            }

            var toSave = string.Concat(Package.Release, @"\", bundled);

            enclose = useEncloseFile ? toBundle.Aggregate(enclose, (current, h) => current.Replace(h.Key, h.Value)) : string.Join(Environment.NewLine, toBundle.Select(m => m.Value));

            var content = string.Concat(license, Environment.NewLine, Environment.NewLine, enclose);
            Console.WriteLine(Message.Saving, bundled);
            File.WriteAllText(toSave, content);

            Console.WriteLine(Message.Completed, ActionNames.Bundle);
        }

        private void Minify(Action action) {
            var source = new DirectoryInfo(Package.Release);
            var ignore = new List<string>();

            if (string.IsNullOrEmpty(action.Filename)) {
                action.Filename = Templates.MinifyFilename;
            }

            if (action.Ignore != null && action.Ignore.Length > 0) {
                ignore = action.Ignore.ToList();
            }

            if (!source.Exists) return;

            foreach (var file in source.GetFiles()
                .Where(m => m.Extension.Trim().ToLower() == SupportedExtensions.Js
                    || m.Extension.Trim().ToLower() == SupportedExtensions.Css)) {
                bool skip = false;

                string extension = file.Extension.Trim().ToLower();
                string cleanExtension = extension;

                if (cleanExtension.StartsWith(".")) {
                    cleanExtension = cleanExtension.TrimStart('.');
                }

                foreach (string pattern in ignore) {

                    string toMatch = pattern.Replace("*", "");

                    if (pattern.StartsWith("*") && pattern.EndsWith("*")) {
                        skip = (file.Name.Contains(toMatch));
                    } else if (pattern.StartsWith("*")) {
                        skip = (file.Name.EndsWith(toMatch));
                    } else if (pattern.EndsWith("*")) {
                        skip = file.Name.StartsWith(toMatch);
                    } else {
                        skip = string.Equals(file.Name.Trim(), toMatch.Trim(), StringComparison.CurrentCultureIgnoreCase);
                    }

                }

                if (skip) continue;

                string txt = File.ReadAllText(file.FullName);


                switch (extension) {
                    case SupportedExtensions.Js: {
                            var compressor = new JavaScriptCompressor {
                                PreserveAllSemicolons = true,
                                ErrorReporter = new CustomErrorReporter(LoggingType.None)
                            };
                            try {
                                txt = compressor.Compress(txt);
                            } catch (EcmaScript.NET.EcmaScriptRuntimeException ex) {
                                Console.WriteLine(Message.Error, ex.Message);
                            }

                        }
                        break;
                    case SupportedExtensions.Css: {
                            var compressor = new CssCompressor();

                            txt = compressor.Compress(txt);

                        }
                        break;
                }

                string extensionless = file.Name.TrimEnd(extension.ToCharArray());

                string newFile = action.Filename.Replace(Templates.Filename, extensionless)
                    .Replace(Templates.Version, Package.Version)
                    .Replace(Templates.Package, Package.Package)
                    .Replace(Templates.Extension, cleanExtension);

                string toSave = string.Concat(Package.Release, @"\", newFile);

                Console.WriteLine(Message.Saving, newFile);

                File.WriteAllText(toSave, txt);
            }

            Console.WriteLine(Message.Completed, ActionNames.Minify);

        }

        public bool Valid() {
            if (Package == null) return false;
            if (Package.Actions != null && !Package.Actions.Any()) return false;
            if (Package.Source == null) {
                Console.WriteLine(Message.Invalid, PackageFolderNames.Source);
                return false;
            }
            if (Package.Release == null) {
                Console.WriteLine(Message.Invalid, PackageFolderNames.Release);
                return false;
            }

            int validActions = 0;

            if (Package.Actions != null) {
                validActions = Package.Actions.Where(action => action != null).Count(action => action.Type != null);
            }

            if (validActions < 1) {
                Console.WriteLine(Message.NoValidActions);
            }

            return validActions > 0;

        }

        public BuildPackage Package { set; get; }
        public FileInfo BuildFile { set; get; }

    }
}
