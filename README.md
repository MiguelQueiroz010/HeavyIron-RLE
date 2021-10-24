# HeavyIron-RLE
Heavy Iron Rle handle tool.

# Using
Extract from the game ELF from PS2 version of Spongebob Battle for Bikini Bottom, the
lookup file (0x1a0 of size) and the compressed data above.

Use the tool to de-compress it or compress from a png texture.

# IMPORTANT
SLUS_206.80 ELF POSITIONS:

LOOKUP: 0x308C30 (0x1A0 length)
COMPRESSED DATA: 0x308DD0 (0xCC17 length)

COMPRESSED DATA POINTER(size): 0x86330(Uint16 LE)
COMPRESSED DATA POINTER(offset): 0x86334(Uint16 LE) + 0x300080 = 0x308DD0

YOU ABSOLUTELY NEED TO CHANGE THE SIZE POINTER, OTHERWISE EE WILL THROW STORE AND LOAD ERRORS.
Maybe you will need to change the whole ELF data size if it gets bigger.

# Credits
Compression/De-Compresion logic by denim and MummRa(STR Brasil).
Using in the tool, Rainbow IMGLIB.
Thanks the Heavy Iron Modding Community for decompiling game variables and all involved.
