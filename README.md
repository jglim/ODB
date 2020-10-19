# ODB

Utilities for Softing binary ObjectDB files that are commonly used in automotive ECU diagnostics definition files (e.g. SMR-D). 

- **ObjectDB (Library)** : Parse, decrypt and decompress ODB files
- **ODBExtract (Utility)** : Extracts DLLs and JARs from a given ODB file

## Requirements

- ObjectDB : netcoreapp3.1
- ODBExtract : net5.0

No external dependencies. The high .NET version requirement is due to the usage of `System.Reflection.PortableExecutable` during DLL extraction.

## ODBExtract

Drop a compatible ODB file onto the binary. Valid JAR and DLL files will be automatically extracted into the application's folder.

When an ODB file is successfully loaded, the embedded files can be accessed with some processing. The files are lumped together into a single blob, so ODBExtract looks out for their headers (Zip: `PK` , DLL: `MZ` ) and extracts them after parsing their headers.

## ObjectDB

.NET library to work with ODB files. 

When an ODB file is loaded (either by file name, or an array of bytes), all recognized sections (such as MetaInfo, string table, binary section etc.) are made available after being processed with the appropriate decompression and decryption functions. For some ODB variants like SMR-F, the additional flash payload is decrypted without further parsing.

---

## License

MIT

---

## Thanks

- [@angelovAlex](https://github.com/angelovAlex)
- [@rnd-ash](https://github.com/rnd-ash)

Thank you for your contributions.

---

# ObjectDB Documentation

Softing applications interact with ODB files through ODBase.dll, sometimes through a higher-level library like VODBR.dll.

## Magic

ODB files start with a fixed, 16 byte magic identifier:

`52 90 D4 30 67 14 7E 47 81 F2 3C 4B 73 F0 F7 37`

## Header

For compatible files, a 0x44 byte header will follow the magic.

- Header size : `UInt32` - Always 0x44
- Unknown1 : `UInt32` - Appears to be the ODB type
- Hashblock offset : `UInt32` - Offset to hashblock from file start
- Client ID : `Int32` - Identifies the vendor that the ODB is intended for. Affects decryption
- Xor Mask Size : `UInt32` - Defines the XOR pad's size in first layer of decryption
- MetaInfo Block Size : `UInt32` - Size of MetaInfo content + UInt32 MetaInfo size prefix
- Constant1 : `UInt32` - Appears to be hardcoded as `0` (ODBase, SMR files)
- Unknown2 : `UInt32` - Unknown, seems to be the length/size of something.
- Section1 Size: `UInt32` - Size of section 1
- Section1 Attributes: `UInt32` - Attributes of section 1
- Section2 Size: `UInt32` - Size of section 2
- Section2 Attributes: `UInt32` - Attributes of section 2
- Section3 Size: `UInt32` - Size of section 3
- Section3 Attributes: `UInt32` - Attributes of section 3
- Uninitialized1 : `UInt32` - Header is `memset()` with `0xEE`, this value was never assigned to
- Uninitialized2 : `UInt32` - Header is `memset()` with `0xEE`, this value was never assigned to
- Flash Payload Size : `UInt32` - Size of the raw flash section, used in SMR-F

- MetaInfo Content Size: `UInt32` - Size of the following MetaInfo section's content (This field technically isn't part of the header, instead it should belong to the MetaInfo block)

## MetaInfo

*Requires: none (plaintext)*

MetaInfo contains information such as its parent ODB file, the build arguments, and the operator that created the ODB file. MetaInfo is plain, unencrypted and can be directly extracted into a string. Its size is defined in the header. 

## HashBlock

*Requires: XOR transform*

This section has a fixed size of 0x20 bytes containing two MD5 hashes. 

- The first header hash is created with `md5( file_magic + header + metainfo )`
- The second body hash is created with `md5( xor(section1) + xor(section2) + xor(section3) )`

## Section1

*Requires: XOR transform + blowfish decrypt + optional zlib inflate (section1 attributes)*

This section contains very low-entropy unknown data. If I had to guess, this contains the class type for every entry in the ODB.

## Section2

*Requires: XOR transform + blowfish decrypt + optional zlib inflate (section2 attributes)*

This section contains binary data. Embedded files can also be found here, and they are stored in a contiguous manner. With a known offset and file size, they can be directly copied out into a file.

## Section3

*Requires: XOR transform + blowfish decrypt + optional zlib inflate (section3 attributes)*

This section contains only strings. Strings are delineated by null terminators `00`.

## Optional flash data

For SMR-D files, the data should end exactly after Section3. SMR-F files have an additional flash payload after Section3 that can be accessed from `ODBFile.ODBFlashBinary`.

# Cryptography

## XOR Transform

Subsequent sections after the MetaInfo section will require a XOR transformation as a means of decryption. The XOR pad/mask is generated using a custom PRNG, where the size of the PRNG is also the PRNG seed. If the input file is larger than the XOR pad, the XOR pad is reused. The PRNG size and seed is specified in the header. 

## Blowfish

After the XOR transformation, an additional Blowfish decryption is required. The Blowfish key is selected from a key table, and depends indirectly on the vendor ID (e.g. Dаіmlеr → Vendor# 4; Key #1 = `A1679B7953B446A799543FAD6BE4CB902C5E7AE8DC4F4AF4AF6CE14637CFF81E`). The key size is a non-standard 256-bit key. This library automates the decryption process, including automatic key selection.

## Inflate (zlib)

A section may optionally be compressed (`SectionAttributes & 0x100 > 0`). The compressed binary has an extra `UInt32` to describe the decompressed data's size, and also has a 2-byte zlib header.

# File Table

At this point, the ODB content should be mostly legible except for the lack of a file table. My original intent was to build a tool to extract files from an ODB container (SMR-D), and I could somewhat achieve this by looking for file headers. As such, no file table features are implemented at this point. Contributions are welcome!