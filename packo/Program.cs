using System;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;

namespace packo {
    internal class Program {


        static void Main(string[] args) {
            Console.WriteLine("{0} {1}", AppInfo.Name, AppInfo.Version);
            Console.WriteLine();
            var stop = false;
            var process = true;
            foreach (var a in args.Where(m => m.StartsWith("-"))) {
                //Command
                switch (a.TrimStart('-').Trim().ToLower()) {
                    case CommandNames.Stop:
                        stop = true;
                        break;
                    case CommandNames.Validate:
                        process = false;
                        break;
                }
            }


            if (args.Any(m => !m.StartsWith("-"))) {
                foreach (var a in args.Where(m => !m.StartsWith("-"))) {
                    ProcessFolder(a, process);
                }
            } else {
                var dir = new DirectoryInfo(Environment.CurrentDirectory);
                if (dir.GetFiles("build.json").Length == 0) {
                    Console.WriteLine(Message.Error, Message.MissingBuildFile);
                } else {
                    var buildFile = dir.GetFiles("build.json").FirstOrDefault();
                    if (buildFile != null) {
                        ProcessFolder(buildFile.FullName, process);
                    } else {
                        Console.WriteLine(Message.Error, Message.MissingBuildFile);
                    }
                }
            }

            if (!stop) return;

            Console.WriteLine();
            Console.WriteLine(Message.Exit);
            Console.ReadLine();
        }

        private static void ProcessFolder(string folder, bool process) {
            if (File.Exists(folder)) {
                var p = new PackageProcessor(Read(folder), new FileInfo(folder));

                if (!p.PreProcess()) {
                    foreach (var err in p.Errors) {
                        Console.WriteLine(err.Message);
                    }
                    return;
                }

                if (!process) return;
                p.Process();

            } else {
                Console.WriteLine(Message.DoesNotExists, folder);
            }
        }

        private static BuildPackage Read(string file) {
            var package = new BuildPackage();

            if (!File.Exists(file)) return package;

            var json = File.ReadAllText(file);
            var js = new JavaScriptSerializer();
            package = (BuildPackage)js.Deserialize(json, typeof(BuildPackage));

            return package;
        }


    }



}
