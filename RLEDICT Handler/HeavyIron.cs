using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

/*
 Algorythm by Bit.Raiden--------2021
####################
Feel free to use, modify, and distribute this code,
in case of comercial use, give the credits to the author and
all people envolved.
####################
Descompression Logic(RLE with Dictionary): denim e MummRA(Str Brasil)
#################### 
  */
#region Information
/*
Structure:

The compressed data in this specific array at Spongebob Battle for Bikini Bottom PS2 ELF,
is a 32 bits Uncompressed Texture with integrated palette.

The compression works with a lookup dictionary of a bunch of RGBA entries(4 bytes each),
we begin at the first byte of the compressed data, that is a index of this lookup,
if this entry has the MSB set to 1, we take the last 7 bits and multiply them for 4 to a true
index of the color, and the next byte will be the repetition count + 2;

If the next, or even the first entry of lookup index has no MSB set to 1,
we just multiply the lookup index to 4 and repeat the RGBA entry only one time.

The lookup can be of a maximum of 127 colors, and 0x1FC of length in bytes.

SLUS_206.80 ELF POSITIONS:

LOOKUP: 0x308C30 (0x1A0 length)
COMPRESSED DATA: 0x308DD0 (0xCC17 length)

COMPRESSED DATA POINTER(size): 0x86330(Uint16 LE)
COMPRESSED DATA POINTER(offset): 0x86334(Uint16 LE) + 0x300080 = 0x308DD0

YOU ABSOLUTELY NEED TO CHANGE THE SIZE POINTER, OTHERWISE EE WILL THROW STORE AND LOAD ERRORS.
Maybe you will need to change the whole ELF data size if it gets bigger.
    */
#endregion
public class HeavyIron
{
    #region Compression/Descompression RLE with Dictionary
    public static byte[] Decompress(byte[] input, byte[] lookup)
    {
        var bout = new List<byte>();//Output declaration
        #region Case input Length too small
        if (input.Length < 1)
        {
            return null;
        }
        #endregion
        #region Loop to Decompress
        for (int i = 0; i < input.Length;)
        {
            //The first entry always is the Index of the Palette Lookup
            byte indexLookup = input[i];

            //If the index of the lookup has the MSB setted to 1(>0x7F equivalent to >0b01111111)
            //0x7F = 0b01111111
            //0x80 = 0b10000000
            //If has this flag, we remove this flag, and use the last 7 bits
            if (indexLookup > 0x7f)
            {
                //Operation AND for remove of the flag
                //Example: byte 0x80: 0b10000000
                //0b10000000 = 0x80
                //0b01111111 = 0x7F
              //&=0b00000000 = 0x0
                byte[] gb = BitConverter.GetBytes(indexLookup & 0x7f);
                indexLookup = gb[0];

                //If we have a flag, then the next byte is a count of repetition
                //that is at a 0xFF maximum value(groups of that maximum)
                //take the count and add +2 to it(it's 0x101 maximum values for some reason)
                int count = input[i + 1] + 2;

                //Loop the count to add the RGBA 4 bytes of the lookup from the indexLookup to
                //the output List<byte>
                for (int k = 0; k < count; k++)
                {
                    //Add RGBA entry to the output list
                    bout.AddRange(ReadBlock(lookup, (uint)indexLookup * 4, 4));
                }

                //Add 2 if flag(IndexLookup + Count of Repetition)
                i += 2;
            }
            else // If has not flag in the next byte, the next index of the Lookup
            {
                //Add RGBA entry to the output list
                bout.AddRange(ReadBlock(lookup, (uint)indexLookup * 4, 4));

                //Add 1 if has no flag
                i++;
            }
        }
        #endregion
        return bout.ToArray(); //return byte[], output
    }
    public static List<byte[]> Compress(byte[] input)
    {
        var outB = new List<byte[]>();//Final output
        var bout = new List<byte>();//Lookup output
        #region Case input length too small
        if (input.Length < 1)
        {
            return null;
        }
        #endregion
        #region LOOKUP
        var lookupLIST = new List<uint>();//List of lookup entries(RGBA32)

        #region Search the used Colors and add to Lookup List
        for (int i = 0; i < input.Length - 4;i+=4)
        {
            uint rgba = (uint)ReadUInt(input, i, Int.UInt32); //First RGBA entry

            //If the RGBA color isn't already on the List
            if (!lookupLIST.Contains(rgba))
                lookupLIST.Add(rgba);//Add it
        }
        #endregion
         
        //Sort the List to Crescent Order(Optional)
        lookupLIST.Sort();

        //For each RGBA entry
        foreach (var item in lookupLIST)
        {
            //Adds it to the output as a LittleEndian Uint32, 4 bytes
            bout.AddRange(BitConverter.GetBytes((UInt32)item));
        }
        
        //If lookup > 0x1FC bytes
        if(bout.Count() > 0x1FC)
        {
           MessageBox.Show("The lookup table ended up too big!\nReduce texture colors and try again.","Error");
           return null;
        }
        else{
         if(bout.Count() > 0x1A0)
         {
          MessageBox.Show("The lookup table is bigger than the original size in ELF!\nReduce texture colors or continue if you will modify the\nELF variable for size.","Error");
         }
        }
        //Fill lookup until is at 0x1FC bytes length(Optional)
        while (bout.Count() < 0x1FC)
            bout.Add(0);
        #endregion
        #region COMPRESSED
        var compressedRLE = new List<byte>();//Compressed data output
        for (int k = 0; k < input.Length - 4;)
        {
            //Get RGBA entry
            uint rgba = (uint)ReadUInt(input, k, Int.UInt32);

            //Get RGBA entry index in the final Lookup list
            //Without multiplying to 4! 
            uint lookupINDEX = (uint)lookupLIST.FindIndex(x => x == rgba);

            //Get the amount of Repetition from that color
            uint repeat = Repeats(input, k, rgba);

            //If repeats more than one time
            if (repeat > 1)
            {
                //Divide the total to 0x101 that is the maximum to each count value(but -2, remember)
                uint Parts = repeat / 0x101;

                //Get the rest of the division
                uint Rest = repeat - (Parts * 0x101);

                //Add the flag to the Lookup Index
                //Repeats more than one time = Has Flag
                lookupINDEX += 0x80;

                //Add Index and Repetition Count(Get Chunk creates the Chunk for each RGBA repetition found)
                compressedRLE.AddRange(GetChunk(Parts, Rest, lookupINDEX));
            }
            else //If repeat one time
            {
                //Add the lookup index without flag and repetition count
                compressedRLE.Add((byte)lookupINDEX);
            }

            k += (int)(repeat * 4);  //Advances to the repeated count * 4(RGBA32)
        }
        #endregion

        //Output
        outB.Add(bout.ToArray());//Lookup to output, index 0
        outB.Add(compressedRLE.ToArray());//Compressed Data to output, index 1
        return outB;

    }
    public static byte[] GetChunk(uint parts, uint rest, uint lookup)
    {
        var ot = new List<byte>();//Chunk output

        //Loop for each part(divided by 0x101)
        for (int j = 0; j < parts; j++)
        {
            //Add the lookup flagged index
            ot.Add((byte)lookup);

            //Add the maximum(each part is equal to 0x101, so we subtract 2
            //0x101 - 2 = 0xFF, repetition count
            ot.Add(0xFF);
        }

        //If the rest is more than one
        if (rest > 1)
        {
            //Add the lookup flagged index
            ot.Add((byte)lookup);

            //Add the rest - 2, repetition count
            ot.Add((byte)(rest - 2));
        }
        else//If rest is 1, add the same to repeated entries just one time
        {
            //Only lookup index without flag
            ot.Add((byte)(lookup - 0x80));
        }

        return ot.ToArray();//Output
    }
    public static uint Repeats(byte[] rgba, int offs, uint entry)
    {
        uint count = 0; //Repetition count output

        //While readed entry equals to searched uint entry and offset is not the final entry
        while ((uint)ReadUInt(rgba, offs, Int.UInt32) == entry && offs < rgba.Length - 4)
        {
            //Repetition +1
            count++;

            //Next to read
            offs += 4;
        }
        return count; //Output
    }//Gets the total amount of Repeated RGBA entries
    #endregion
    #region Reading Helpers(NUC RAW TOOLS, BIT.RAIDEN)
    public static byte[] ReadBlock(byte[] s, uint offset, uint size)
    {
        byte[] bytes = new byte[size];
        var memory = new MemoryStream(s);
        var reader = new BinaryReader(memory);
        reader.BaseStream.Position = offset;
        bytes = reader.ReadBytes((int)size);
        reader.Close();
        memory.Close();
        return bytes;

    }
    public static string ReadSequence(byte[] file, int offset, Encoding encoding)
    {
        var sequence = new List<byte>();
        int i = offset;
        while (file[i] != 0)
        {
            sequence.Add(file[i]);
            i++;
        }
        string ret = encoding.GetString(sequence.ToArray());
        return ret;
    }
    public static ulong ReadUInt(byte[] s, int offset, Int type)
    {
        ulong retur = 0;
        var memory = new MemoryStream(s);
        var reader = new BinaryReader(memory);
        reader.BaseStream.Position = offset;
        switch (type)
        {
            case Int.UInt16:
                retur = reader.ReadUInt16();
                break;
            case Int.UInt32:
                retur = reader.ReadUInt32();
                break;
            case Int.UInt64:
                retur = reader.ReadUInt64();
                break;
        }
        reader.Close();
        memory.Close();
        return retur;
    }
    public static float ReadFloat(byte[] s, int offset, Int type)
    {
        float retur = 0;
        var memory = new MemoryStream(s);
        var reader = new BinaryReader(memory);
        reader.BaseStream.Position = offset;
        retur = reader.ReadSingle();
        reader.Close();
        memory.Close();
        return retur;
    }
    public enum Int
    {
        Int16,
        Int32,
        Int64,
        UInt16,
        UInt32,
        UInt64,
        Float32
    };
    #endregion
}
