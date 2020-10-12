using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection.PortableExecutable;
using System.IO;
using System.Linq;
using System.IO.Compression;

namespace ObjectDB
{
    /// <summary>
    /// Utilities to extract meaningful data from ODB binary data, as the ODB file layout is not yet understood.
    /// </summary>
    public class ExtractUtility
    {
        /// <summary>
        /// Given a binary blob, try to extract as many DLLs as possible.
        /// DLL files are identified by their DOS stub. Their file sizes are calculated from their PE header.
        /// </summary>
        /// <param name="odbBinaryBlock"></param>
        /// <returns>List of DLL files, each as a byte array</returns>
        public static List<byte[]> ExtractDLLs(byte[] odbBinaryBlock)
        {
            byte[] dosStubLongSignature = Encoding.ASCII.GetBytes("This program cannot be run in DOS mode");
            byte[] mzSignature = Encoding.ASCII.GetBytes("MZ");
            List<byte[]> result = new List<byte[]>();

            int fileCursor = 0;
            while (fileCursor < odbBinaryBlock.Length) 
            {
                // find the dos header stub
                int nextDosHeaderPosition = BytearraySearch(odbBinaryBlock, dosStubLongSignature, fileCursor);
                if (nextDosHeaderPosition == -1) 
                {
                    break;
                }
                // with a dos header stub, look backwards for a MZ magic
                int mzHeaderPosition = -1;
                for (int i = nextDosHeaderPosition - mzSignature.Length; i >= mzSignature.Length; i--) 
                {
                    if ((mzSignature[0] == odbBinaryBlock[i]) && (mzSignature[1] == odbBinaryBlock[i + 1])) 
                    {
                        mzHeaderPosition = i;
                        break;
                    }
                }
                if (mzHeaderPosition != -1) 
                {
                    // PEHeaders will adjust the stream cursor and requires the MZ signature to be at the start of the file
                    MemoryStream dllStream = new MemoryStream(odbBinaryBlock.Skip(mzHeaderPosition).ToArray());
                    PEHeaders pe = new PEHeaders(dllStream);
                    int peSize = pe.PEHeader.SizeOfHeaders;
                    foreach (SectionHeader section in pe.SectionHeaders)
                    {
                        peSize += section.SizeOfRawData;
                    }
                    byte[] dllBytes = new byte[peSize];
                    Array.ConstrainedCopy(odbBinaryBlock, mzHeaderPosition, dllBytes, 0, peSize);
                    result.Add(dllBytes);
                    // Console.WriteLine($"Found valid MZ @ {mzHeaderPosition:X}, size: {peSize}");
                    fileCursor = mzHeaderPosition + peSize;
                }

                fileCursor = nextDosHeaderPosition + dosStubLongSignature.Length;
            }
            return result;
        }
        /// <summary>
        /// Given a binary blob, try to extract as many ZIP files (and JAR files) as possible.
        /// ZIP files are identified by their PK header. Their file sizes are calculated from the position of the EOCD record
        /// </summary>
        /// <param name="zipBytes">Array of bytes containing the ZIP file</param>
        /// <returns>List of ZIP files, each as a byte array</returns>
        public static List<byte[]> ExtractZips(byte[] zipBytes)
        {
            // Helpful guide on zip headers: https://users.cs.jmu.edu/buchhofp/forensics/formats/pkzip.html
            List<byte[]> result = new List<byte[]>();
            byte[] zipMagicSignature = new byte[] { 0x50, 0x4B, 0x03, 0x04 };
            byte[] zipEocdSignature = new byte[] { 0x50, 0x4B, 0x05, 0x06 };
            int fileCursor = 0;
            while (fileCursor < zipBytes.Length)
            {
                // find the zip PK signature
                int nextZipLocation = BytearraySearch(zipBytes, zipMagicSignature, fileCursor);
                if (nextZipLocation == -1)
                {
                    break;
                }
                int nextEocdLocation = BytearraySearch(zipBytes, zipEocdSignature, fileCursor);
                if (nextEocdLocation == -1)
                {
                    break;
                }
                // EOCD has a variable length comment field, extract the field size to know the final length of the zip file
                int eocdCommentLength = (zipBytes[nextEocdLocation+0x15] << 8) | zipBytes[nextEocdLocation+0x14];
                int zipFinalSize = (nextEocdLocation + 0x16 + eocdCommentLength) - nextZipLocation;
                // Console.WriteLine($"Zip head: {nextZipLocation:X}, EOCD: {nextEocdLocation:X}, sizeof(comment): {eocdCommentLength:X}, sum: {zipFinalSize:X}");
                result.Add(zipBytes.Skip(nextZipLocation).Take(zipFinalSize).ToArray());
                fileCursor = nextZipLocation + zipFinalSize;
            }
            return result;
        }

        /// <summary>
        /// Given a zip file, attempt to identify if it is a Java JAR file, and generate a meaningful file name.
        /// Name data is inferred from the contents of the JAR's MANIFEST.MF
        /// </summary>
        /// <param name="zipBytes">Array of bytes containing the ZIP file</param>
        /// <param name="fallbackId">If a usable name cannot be generated, this number is used as the file name instead.</param>
        /// <returns>Inferred filename, including file extension</returns>
        public static string GenerateNameForZipFile(byte[] zipBytes, int fallbackId = 0)
        {
            // Find the java manifests file, and pick the most sensible name
            string symbolicNameKey = "Bundle-SymbolicName:";
            string mainClassKey = "Main-Class:";

            MemoryStream zipStream = new MemoryStream(zipBytes);
            try
            {
                ZipArchive za = new ZipArchive(zipStream, ZipArchiveMode.Read, true);
                int entryCount = za.Entries.Count;
                // fallback name is a zip by default (e.g. if the file is not a valid zip)
                string fallbackName = $"{fallbackId}.zip"; ;
                foreach (ZipArchiveEntry entry in za.Entries)
                {
                    if (entry.Name.ToUpper() == "MANIFEST.MF")
                    {
                        // read the contents of manifest.mf, split the lines, and look for Bundle-SymbolicName (preferred) or Main-Class (less desirable)
                        using (var reader = new StreamReader(entry.Open()))
                        {
                            string[] mfText = reader.ReadToEnd().Replace("\r", "\n").Split("\n", StringSplitOptions.RemoveEmptyEntries);
                            foreach (string manifestsRow in mfText)
                            {
                                if (manifestsRow.Contains(symbolicNameKey))
                                {
                                    return manifestsRow.Substring(symbolicNameKey.Length + 1) + ".jar";
                                }
                                if (manifestsRow.Contains(mainClassKey))
                                {
                                    fallbackName = manifestsRow.Substring(mainClassKey.Length + 1) + ".jar";
                                }
                            }
                        }
                    }
                }

                return fallbackName;
            }
            catch (Exception)
            {
                return $"{fallbackId}.bin";
            }
        }

        private static int BytearraySearch(byte[] haystack, byte[] needle, int offset)
        {
            int limit = haystack.Length - needle.Length;
            for (int i = offset; i <= limit; i++)
            {
                int k = 0;
                while (k < needle.Length)
                {
                    if (needle[k] != haystack[i + k]) 
                    { 
                        break; 
                    }
                    k++;
                }
                if (k == needle.Length) 
                {
                    return i;
                }
            }
            return -1;
        }
    }
}
