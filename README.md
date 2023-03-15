# Final Fantasy MSX2: Say DOS Tool
## About

This is a file extractor and compactor for Final Fantasy MSX2's Say DOS file system. At boot, Final Fantasy identifies it uses the Say DOS file system along with the version 1.A0, so I assume this is used in other Microcabin games, but I never bothered to check. Due to a possible anti-piracy measure, this tool is specific to this game. More info in the technical section. Commonplace back in the day, games used custom file systems as a form of anti-piracy. A less talked about aspect of file systems is that they contain bootcode at the beginning to tell the CPU how to handle the file system. Today, we have standards and you can't just mount a disk image with a custom file system.

Hopefully this tool encourages further interest in Final Fantasy 1 for the NES and MSX. The filenames provide some decent self-documentation which can help in a disassembly project.

## Technical
There's two processes for the application: `dump` and `write`. `Dump` extracts all the files and sectors. The first 10 sectors don't correspond to any files and aren't part of the root directory. The data region starts at sector 100. I didn't need to dump the sectors in the data region, but did it for debugging purposes.

The root directory follows the 8.3 file format, which was common in most file systems back in the day. Followed by the filename is the start and end sector numbers in little endian.
```
4D41494E202020202E434F4D 6400 8300  MAIN    .COMd...
534D4150202020202E434F4D 8400 9F00  SMAP    .COM....
```

The file system has an implied folder structure where each folder is a group of files and are separated by rows of bytes of the value `0x40`. The folders don't have names so I just store where each root directory entry is located in `root directory.json` file and store all the files in a single folder.

A lot of files are unused duplicates. If I were to guess, this port is based off of the Famicom version's source code. So there's an intended structure to the project that's hacked around. For example, `MESSAGE.VEC` and `MESSAGE.MSG` are the dialogue pointer table and dialogue data respectively. The pointer table is an exact 1 to 1 match to the original Famicom game as is the dialogue data. This looks normal. However, you have the file `SHOP.MSG` without a pointer table, completely unused. The shop text that's actually read by the game is actually used in `SMAP.COM`. 

As stated before, custom file systems were implemented as an anti-piracy measure. This was done so you couldn't just copy over the files to another disk (I guess disk image copying was a foreign concept back then). However, if you were able to reverse engineer the format and repack the file system, the developers had another trick up their sleeve: there's data not referenced inside the root directory. If you were to extract the files and create a new image, you'd find that the battle graphics won't load. After some sleuthing, I determined there was data at sector number `044C` and beyond that weren't referenced by the root directory. Through static analysis, I determined sector number was loaded inside `BATTLE.COM`, hardcoded in Z80 assembly. Is it security through obscurity or just bad programming? I'm leaning towards the former. The tool accounts for this so it shouldn't be an issue.

## Usage
See below for terminal usage information.

### Dumping
```bash
saydos.exe "Dump" "%InputDiskImageFilePath%" "%OutputSectorAndFileFolderPath%"
```

* `InputDiskImageFilePath`: The disk image you want to extract sectors and files from
* `OuputSectorAndFileFolderPath`: The directory you want to output the data to. The folder will need to be created for the program to run.

### Writing

```bash
saydos.exe "Write" "%OutputDiskImageFilePath%" "%InputSectorAndFileFolderPath%"
```

* `OutputDiskImageFilePath`: The file path where disk image will be created.
* `InputSectorAndFileFolderPath`: The directory with the sectors and files you want to use to create a disk image.