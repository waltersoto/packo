using System;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;

namespace packo
{
    class Program
    {
        

        static void Main(string[] args)
        {
            Console.WriteLine("{0} {1}", AppInfo.Name, AppInfo.Version);
            Console.WriteLine();
            bool stop = false;
            bool process = true;

            if (args.Length > 0)
            {
                foreach (var a in args.Where(m => m.StartsWith("-")))
                {
                    
                        //Command
                        switch (a.TrimStart('-').Trim().ToLower())
                        {
                            case CommandNames.Stop:
                                stop = true;
                                break;
                            case CommandNames.Validate:
                                process = false;
                                break;

                        }
                    
                }

                foreach (var a in args.Where(m => !m.StartsWith("-")))
                {
                    
                    if (File.Exists(a))
                    {
                        var p = new PackageProcessor(Read(a), new FileInfo(a));

                        if (p.PreProcess())
                        {
                            if (process)
                            {
                                p.Process();
                            }
                            
                        }
                    }
                    else
                    {
                        Console.WriteLine(Message.DoesNotExists,a);
                    }
                   
                }
  

            }


            if (!stop) return;

            Console.ReadLine();
            Console.WriteLine(Message.Exit);
            Console.ReadLine();
        }

        static BuildPackage Read(string file)
        {
            var package = new BuildPackage();

            if (!File.Exists(file)) return package;

            string json = File.ReadAllText(file);
            var js = new JavaScriptSerializer();
            package = (BuildPackage)js.Deserialize(json, typeof(BuildPackage));

            return package;
        } 
        

    }



}
