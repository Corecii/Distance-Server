using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Patcher
{
    class Program
    {
        static void Help()
        {
            Console.WriteLine("Distance Server DLL Patcher");
            Console.WriteLine("Patches Distance's Assembly-CSharp dll for use in the standalone Distance server.");
            Console.WriteLine("Usage:");
            Console.WriteLine(@".\Patcher.exe <path to Assembly-CSharp.dll>");
            Console.WriteLine("Or drag the dll to Patcher.exe in the File Explorer.");
            Console.WriteLine(@"For example: .\Patcher.exe ""C:\Program Files (x86)\Steam\steamapps\common\Distance\Distance_Data\Managed\Assembly-CSharp.dll""");
            Console.WriteLine("Press Enter to exit");
            Console.ReadLine();
        }
        static void Done()
        {
            Console.WriteLine($"Wrote Distance.dll to {GetPath()}");
        }
        static string GetPath()
        {
            return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\Distance.dll";
        }
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Help();
                return;
            }
            try
            {
                var mod = ModuleDefMD.Load(args[0]);

                mod.Assembly.Name = "Distance";

                mod.Write(GetPath());
                Done();
            } catch (Exception e)
            {
                Console.WriteLine($"Failed to patch {args[0]} because: {e}");
                Help();
            }
        }
    }
}
