﻿using System;
using System.Collections.Generic;
using ObjectDB;
using System.IO;

namespace ODBExtract
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "ODBExtract";
            if (args.Length > 0)
            {
                string fileName = args[0];
                if (File.Exists(fileName))
                {
                    Console.WriteLine($"ODBExtract: Loading {fileName}");
                    ODBFile odb = ODBFile.FromFile(fileName);
                    Console.WriteLine($"MetaInfo\r\n{odb.MetaInfo}\r\n");
                    Console.WriteLine($"Preparing to extract files");
                    ExtractDLLs(odb, fileName);
                    ExtractZips(odb, fileName); 
                }
            }
            else
            {
                Console.WriteLine($"ODBExtract: please target a SMR-D file.");
            }
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }

        static void ExtractDLLs(ODBFile odb, string fileName) 
        {
            List<byte[]> extractedDlls = ExtractUtility.ExtractDLLs(odb.ODBBinary);
            for (int i = 0; i < extractedDlls.Count; i++)
            {
                string preferredName = $"{Path.GetFileNameWithoutExtension(fileName)}_{i}.dll";
                Console.WriteLine(preferredName);
                File.WriteAllBytes(preferredName, extractedDlls[i]);
            }
            Console.WriteLine($"{extractedDlls.Count} DLLs extracted");
        }

        static void ExtractZips(ODBFile odb, string fileName)
        {
            List<byte[]> extractedZips = ExtractUtility.ExtractZips(odb.ODBBinary);
            for (int i = 0; i < extractedZips.Count; i++)
            {
                string preferredName = ExtractUtility.GenerateNameForZipFile(extractedZips[i], i);
                Console.WriteLine(preferredName);
                File.WriteAllBytes($"{Path.GetFileNameWithoutExtension(fileName)}_{i}_{preferredName}", extractedZips[i]);
            }
            Console.WriteLine($"{extractedZips.Count} ZIP/JARs extracted");
        }

    }
}
