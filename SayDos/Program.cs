using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SayDos
{
    public enum WriteArgs
    {
        action,
        diskImagePath,
        inFolder,
    }

    public enum DumpArgs
    {
        action,
        diskImagePath,
        outFolder,
    }

    class Program
    {
        private const string ROOT_DIRECTORY_FILE = "root directory.json";
        private const int SECTOR_SIZE = 0x200;

        private const string FOLDER_SECTOR = "sectors";
        private const string FOLDER_FILES = "files";

        private const ushort SECTOR_FILE_START = 0x64;

        static void Main(string[] args)
        {

            if (args.Length == 0)
            {
                throw new Exception($"Cannot have 0 arguments.");
            }

            var action = args[0];

            switch (action)
            {
                case "Dump":
                    {
                        Console.WriteLine($"Dumping");

                        var requiredLength = (int)Enum.GetValues(typeof(DumpArgs)).Cast<DumpArgs>().Max() + 1;
                        if (args.Length != requiredLength)
                        {
                            throw new Exception($"Required argument number: {requiredLength}. Received: {args.Length}");
                        }

                        // Read arguments
                        var folder = args[(int)DumpArgs.outFolder];
                        var romPath = args[(int)DumpArgs.diskImagePath];

                        if (!Directory.Exists(folder))
                        {
                            throw new Exception($"Folder doesn't exist: {folder}");
                        }

                        if (!File.Exists(romPath))
                        {
                            throw new Exception($"File doesn't exist: {romPath}");
                        }

                        byte[] source = File.ReadAllBytes(romPath);

                        // Obtain all sectors of disk image
                        List<byte[]> sectors = new();

                        for (int i = 0; i < source.Length; i += SECTOR_SIZE)
                        {
                            var buffer = new byte[SECTOR_SIZE];
                            Array.Copy(source, i, buffer, 0, SECTOR_SIZE);
                            sectors.Add(buffer);
                        }

                        // Set new working directory
                        if (!Directory.Exists(folder))
                        {
                            throw new Exception($"Path does not exist: {folder}");
                        }

                        Directory.SetCurrentDirectory(folder);

                        // Dump all sectors
                        Directory.CreateDirectory(FOLDER_SECTOR);
                        int j = 0;
                        foreach (byte[] b in sectors)
                        {
                            File.WriteAllBytes($@"{FOLDER_SECTOR}\{j++:0000}.bin", b);
                        }

                        // Read all root directory entries
                        List<SayDosRootDirectory> rdTable = new();

                        for (int s = 10; s < SECTOR_FILE_START; s++)
                        {
                            for (int rdEntry = 0; rdEntry < sectors[s].Length / SayDosRootDirectory.RD_SIZE; rdEntry++)
                            {
                                var b = new byte[SayDosRootDirectory.RD_SIZE];
                                Array.Copy(sectors[s], rdEntry * SayDosRootDirectory.RD_SIZE, b, 0x0, SayDosRootDirectory.RD_SIZE);
                                rdTable.Add(new(b));
                            }
                        }

                        // Dump all files with their corresponding name and sectors
                        Directory.CreateDirectory(FOLDER_FILES);

                        foreach (var rdEntry in rdTable)
                        {
                            if (!rdEntry.IsEmpty)
                            {
                                File.WriteAllBytes(@$"{FOLDER_FILES}\{rdEntry.File}",
                                    sectors.Skip(rdEntry.StartSector).Take(rdEntry.SizeInSectors).SelectMany(x => x).ToArray());
                            }
                        }

                        // Write root directory data
                        // Will be used for writing
                        File.WriteAllText(ROOT_DIRECTORY_FILE, JsonConvert.SerializeObject(rdTable, Formatting.Indented));
                        break;
                    }
                case "Write":
                    {
                        Console.WriteLine($"Writing");

                        var requiredLength = (int)Enum.GetValues(typeof(WriteArgs)).Cast<WriteArgs>().Max() + 1;
                        if (args.Length != requiredLength)
                        {
                            throw new Exception($"Required argument number: {requiredLength}. Received: {args.Length}");
                        }

                        // Read arguments
                        var folder = args[(int)WriteArgs.inFolder];
                        var romPath = args[(int)WriteArgs.diskImagePath];

                        if (!Directory.Exists(folder))
                        {
                            throw new Exception($"Folder doesn't exist: {folder}");
                        }

                        if (!File.Exists(Path.Combine(folder, ROOT_DIRECTORY_FILE)))
                        {
                            throw new Exception($"File doesn't exist: {Path.Combine(folder, ROOT_DIRECTORY_FILE)}");
                        }

                        Directory.SetCurrentDirectory(folder);
                        Directory.SetCurrentDirectory(FOLDER_SECTOR);

                        // Read dumped root directory info
                        var rdTable = JsonConvert.DeserializeObject<List<SayDosRootDirectory>>(new StreamReader(Path.Combine(folder, ROOT_DIRECTORY_FILE)).ReadToEnd());

                        // Read dumped MBR header and other system data (e.g. stuff that aren't files)
                        List<byte[]> systemSectors = new();
                        List<byte[]> fileSectors = new();

                        for (int s = 0; s < 10; s++)
                        {
                            systemSectors.Add(File.ReadAllBytes($@"{s:0000}.bin"));
                        }

                        ushort fileSector = SECTOR_FILE_START;
                        ushort battleSector = 0x0000;

                        // Read files
                        Directory.SetCurrentDirectory("../" + FOLDER_FILES);

                        List<byte[]> rdBytes = new();
                        foreach (var rdEntry in rdTable)
                        {
                            if (!rdEntry.IsEmpty)
                            {
                                if (!File.Exists(rdEntry.File))
                                {
                                    throw new Exception($"File doesn't exist: {Path.GetFullPath(rdEntry.File)}");
                                }

                                var fileBytes = File.ReadAllBytes(rdEntry.File);

                                if (fileBytes.Length % SECTOR_SIZE != 0)
                                {
                                    throw new Exception($"File doesn't fit exactly within {MyMath.DecToHex(SECTOR_SIZE, Prefix.X)} byte sectors. " +
                                        $"File: {rdEntry.FileName}; Size: {MyMath.DecToHex(fileBytes.Length)}");
                                }

                                var fs = fileBytes.Select((s, i) => fileBytes.Skip(i * SECTOR_SIZE).Take(SECTOR_SIZE).ToArray()).Where(a => a.Any()).ToArray();
                                fileSectors = fileSectors.Concat(fs).ToList();

                                rdEntry.StartSector = fileSector;
                                fileSector += (ushort)((fileBytes.Length / SECTOR_SIZE) - 1);
                                rdEntry.EndSector = fileSector;
                                fileSector++;

                                // Special case: adjust the pointer for the battle sectors
                                if (rdEntry.File == "BATTLE.COM")
                                {
                                    battleSector = rdEntry.StartSector;
                                }
                            }

                            rdBytes.Add(rdEntry.CreateRootDirectoryEntry());
                        }

                        var merged = systemSectors.SelectMany(x => x).ToArray();
                        merged = merged.Concat(rdBytes.SelectMany(x => x).ToArray()).ToArray();
                        merged = merged.Concat(fileSectors.SelectMany(x => x).ToArray()).ToArray();

                        // Read the battle system sectors

                        // The game hardcodes the sector number for this data in assembly instead of having it in the root directory.
                        // As far as I know, this is the only occurance. Similarly with the sector based file system,
                        // I'd assume this is a case of security through obscurity to prevent file copying (not that this prevent disk image copying).
                        Directory.SetCurrentDirectory("../" + FOLDER_SECTOR);
                        for (ushort s = 0x44C; s < 0x541; s++)
                        {
                            merged = merged.Concat(File.ReadAllBytes($@"{s:0000}.bin")).ToArray();
                        }

                        var pointer = BitConverter.GetBytes(fileSector);
                        Array.Copy(pointer, 0, merged, battleSector * SECTOR_SIZE + 0xf6c, pointer.Length);

                        byte[] final = Enumerable.Repeat((byte)0xFF, 0xB4000).ToArray();

                        Array.Copy(merged, 0, final, 0, merged.Length);

                        File.WriteAllBytes(romPath, final);

                        break;
                    }
                default:
                    throw new Exception($"Invalid first parameter: {action}");
            }
        }
    }
}