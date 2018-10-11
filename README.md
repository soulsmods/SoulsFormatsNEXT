
# SoulsFormats
A .NET library for reading and writing various FromSoftware file formats, targeting .NET Framework 4.6.2.  
Dark Souls, Demon's Souls and Bloodborne are the main focus, but older From games may be supported to varying degrees. See below for a breakdown of each format.

## Usage
Objects for each format can be created with the static method Read, which accepts either a byte array or a file path. Using a path is preferable as it will be read with a stream, reducing memory consumption.
```cs
BND3 bnd = BND3.Read(@"C:\your\path\here.chrbnd");

// or

byte[] bytes = File.ReadAllBytes(@"C:\your\path\here.chrbnd");
BND3 bnd = BND3.Read(bytes);
```

The Write method can be used to create a new file from an object. If given a path it will be written to that location with a stream, otherwise a byte array will be returned.
```cs
bnd.Write(@"C:\your\path\here.chrbnd");

// or

byte[] bytes = bnd.Write();
File.WriteAllBytes(@"C:\your\path\here.chrbnd", bytes);
```

DCX (compressed files) will be decompressed automatically and the compression type will be remembered and reapplied when writing the file.
```cs
BND3 bnd = BND3.Read(@"C:\your\path\here.chrbnd.dcx");
bnd.Write(@"C:\your\path\here.chrbnd.dcx");
```

The compression type can be changed by either setting the Compression field of the object, or specifying one when calling Write.
```cs
BND3 bnd = BND3.Read(@"C:\your\path\here.chrbnd.dcx");
bnd.Write(@"C:\your\path\here.chrbnd", DCX.Type.None);

// or

BND3 bnd = BND3.Read(@"C:\your\path\here.chrbnd.dcx");
bnd.Compression = DCX.Type.None;
bnd.Write(@"C:\your\path\here.chrbnd");
```

Finally, DCX files can be generically read and written with static methods if necessary. DCX holds no important metadata so they read/write directly to/from byte arrays instead of creating an object.
```cs
byte[] bndBytes = DCX.Decompress(@"C:\your\path\here.chrbnd.dcx");
BND3 bnd = BND3.Read(bndBytes);

// or

byte[] dcxBytes = File.ReadAllBytes(@"C:\your\path\here.chrbnd.dcx");
byte[] bndBytes = DCX.Decompress(dcxBytes);
BND3 bnd = BND3.Read(bndBytes);
```

Writing a new DCX requires a DCX.Type parameter indicating which game it is for. DCX.Decompress has an optional out parameter indicating the detected type which should usually be used instead of specifying your own.
```cs
byte[] bndBytes = DCX.Decompress(@"C:\your\path\here.chrbnd.dcx", out DCX.Type type);
DCX.Compress(bndBytes, type, @"C:\your\path\here.chrbnd.dcx");

// or

byte[] bndBytes = DCX.Decompress(@"C:\your\path\here.chrbnd.dcx", out DCX.Type type);
byte[] dcxBytes = DCX.Compress(bndBytes, type);
File.WriteAllBytes(@"C:\your\path\here.chrbnd.dcx", dcxBytes);
```

## Formats
Game compatibility will always be listed for DS 1/R/2/3, DeS, and BB unless the format is unused in that game. Other From games may or may not be supported.

### BND0
A general-purpose file container used in PS2-era From games.  
Extension: `.bnd`  
* ACE 3: Partial Read and Write.

### BND3
A general-purpose file container used before DS2.  
Extension: `.*bnd`
* DS1: Full Read and Write
* DSR: Full Read and Write
* DeS: Partial Read and Write
* NB: Full Read and Write

### BND4
A general-purpose file container used from DS2 onwards.  
Extension: `.*bnd`
* DS2: Full Read and Write
* DS3: Full Read and Write
* BB: Full Read and Write

### BXF3
Essentially a BND3 split into separate header and data files.  
Extensions: `.*bhd` (header) and `.*bdt` (data)
* DS1: Full Read and Write
* DSR: Full Read and Write
* DeS: Untested

### BXF4
Essentially a BND4 split into separate header and data files.  
Extensions: `.*bhd` (header) and `.*bdt` (data)
* DS2: Full Read and Write
* DS3: Full Read and Write
* BB: Full Read and Write

### DCX
A wrapper for a single compressed file used in every game after NB.  
Extension: `.dcx`
* DS1: Full Read, Write, and Create
* DSR: Full Read, Write, and Create
* DS2: Full Read, Write, and Create
* DS3: Full Read, Write, and Create
* DeS: Full Read, Write, and Create
* BB: Full Read, Write, and Create

### DRB
An interface element configuration file used before DS2 when Scaleform was adopted. Very poorly supported.  
Extension: `.drb`
* DS1: Partial Read, No Write
* DSR: Partial Read, No Write
* DeS: Untested
* NB: Untested

### ENFL
Unknown. Believed to determine which assets load based on where you are in a map.
Extension: `.entryfilelist`
* DS3: Full Read and Write
* BB: Untested

### FILTPARAM
A graphics configuration format used in DS2.
Extension: `.filtparam`
* DS2: Partial Read and Write

### FLVER
A 3D model file used throughout the series.  
Extension: `.flv` or `.flver`
* DS1: Partial Read and Write
* DSR: Partial Read and Write
* DS2: Partial Read and Write
* DeS: Untested

### FLVER3
A 3D model file used in DS3 (and maybe BB).
Extension: `.flver`
* DS3: Partial Read and Write
* BB: Untested

### FMG
A text bundle format used throughout the series.
Extension: `.fmg`
* DS1: No support
* DS2: Untested
* DS3: Full Read and Write
* DeS: No support
* BB: Untested

### GPARAM
A graphics configuration format used in DS3 and BB.
* DS3: Partial Read and Write
* BB: Untested

### MSB64
A map definition format used in SotFS, DS3, and BB.
Extension : `.msb`
Scholar: No support
DS3: Partial Read and Write
BB: No support

### MTD
A material definition file used throughout the series.  
Extension: `.mtd`
* DS1: Full Read and Write
* DSR: Full Read and Write
* DS2: Full Read and Write
* DS3: Full Read and Write
* DeS: Untested
* BB: Untested
* NB: Untested

### PARAM64
A general configuration file used in DS3.
Extension: `.param`
* DS3: Full Read and Write

### TPF
A container for multiple DDS textures used throughout the series.  
Extension: `.tpf`
* DS1: Full Read and Write
* DSR: Full Read and Write
* DS2: Full Read and Write
* DS3: Full Read and Write
* DeS: Full Read and Write
* BB: Full Read and Write
* NB: Full Read and Write

## Special Thanks
To everyone below, for either creating tools that I learned from, or helping decipher these formats one way or another. Please yell at me on Discord if I missed you.
* Atvaark
* B3LYP
* HotPocketRemix
* Lance
* Meowmaritus
* Nyxojaele
* Pav
* SeanP
* Wulf2k
