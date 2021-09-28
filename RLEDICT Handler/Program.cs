using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;
using Rainbow.ImgLib;//Image codecs
using static HeavyIron;//Compression/de-compression heavyiron RLE

namespace RLEDICT_Handler
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Console.WriteLine("Bit.Raiden RLE Lookup Tool\nCompression Logic by denim/Mummm-Ra\n\n" +
                "Choose what you want to do:\n\n" +
                "1 - De-compress texture(Needs lookup and compressed data)\n" +
                "2 - Compress texture(needs only Direct Color PNG texture)\n" +
                "3 - Exit");

            switch (Console.ReadLine())
            {
                case "1":
                    var ap = new OpenFileDialog();
                    ap.Title = "Choose the Lookup dictionary binary data";
                    if (ap.ShowDialog() == DialogResult.OK)
                    {
                        byte[] lookup = File.ReadAllBytes(ap.FileName);
                        ap = new OpenFileDialog();
                        ap.Title = "Choose the compressed binary data";
                        if (ap.ShowDialog() == DialogResult.OK)
                        {
                            byte[] compreesedRLE = File.ReadAllBytes(ap.FileName);
                            var save = new SaveFileDialog();
                            save.Filter = "Portable Network Graphics(*.png)|*.png";
                            save.FileName = " output";
                            if(save.ShowDialog()==DialogResult.OK)
                            {
                                if (File.Exists("resolution.ini")) {
                                    string res = File.ReadAllText("resolution.ini");
                                    string[] entries = res.Split(new string[] { "x","X", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                                    int width = Convert.ToInt32(entries[0]);
                                    int height = Convert.ToInt32(entries[1]);
                                    try
                                    {
                                        var decoder = new Rainbow.ImgLib.Encoding.ImageDecoderDirectColor(Decompress(compreesedRLE, lookup), width, height, Rainbow.ImgLib.Encoding.ColorCodec.CODEC_32BIT_RGBA);
                                        var im = decoder.DecodeImage();
                                        im.Save(save.FileName);
                                        im.Dispose();
                                    }
                                    catch(Exception)
                                    {
                                        File.WriteAllBytes(save.FileName + ".bin", Decompress(compreesedRLE, lookup));
                                        Console.Clear();
                                        Console.WriteLine("The conversion after decompression was not successful!\n" +
                                            "Saving binary uncompressed texture data...\nMaybe i cannot convert textures" +
                                            "compressed with this tool to png,\nbut i can export them in raw binary\nSee the output: " +
                                    save.FileName+".bin");
                                        Atualizar();
                                    }
                                    Console.Clear();
                                    Console.WriteLine("The decompression was a success!\nSee the output: " +
                                    save.FileName);
                                    Atualizar();
                                }
                                else
                                {
                                    MessageBox.Show("resolution.ini with the resolution of the texture\n" +
                                        "was not found in the program executable folder.", "System Error");
                                    BackMenu();
                                }
                            }
                            else
                                BackMenu();
                        }
                        else
                            BackMenu();
                    }
                    else
                        BackMenu();
                    break;
                case "2":
                    var op = new OpenFileDialog();
                    op.Title = "Choose the PNG texture";
                    op.Filter = "Portable Network Graphics(*.png)|*.png";
                    if (op.ShowDialog() == DialogResult.OK)
                    {
                        var save = new FolderBrowserDialog();
                        save.Description = "Select the output, it'll create a new folder called 'HI'";
                        if (save.ShowDialog() == DialogResult.OK)
                        {
                            var decoder = new Rainbow.ImgLib.Encoding.ImageEncoderDirectColor(Image.FromFile(op.FileName), Rainbow.ImgLib.Encoding.ColorCodec.CODEC_32BIT_RGBA);
                            byte[] decoded = decoder.Encode();
                            if(!Directory.Exists(save.SelectedPath+@"\HI"))
                                Directory.CreateDirectory(save.SelectedPath + @"\HI");
                            var outArr = Compress(decoded);
                            if(outArr!=null){
                            File.WriteAllBytes(save.SelectedPath + @"\HI\Lookup.bin",outArr[0]);
                            File.WriteAllBytes(save.SelectedPath + @"\HI\Data.bin",outArr[1]);
                            Console.Clear();
                            Console.WriteLine("The compression was a success!\nSee the output folder: "+
                                save.SelectedPath);
                            }
                            else{
                               Console.Clear();
                               Console.WriteLine("The compression ended up with error(s)."); 
                               BackMenu();
                            }
                        }
                        else
                        {
                            BackMenu();
                        }
                    }
                    else
                    {
                        BackMenu();
                    }
                    Atualizar();
                    break;
                case "3":
                    Environment.Exit(0);
                    break;
                default:
                    #region Operação inválida+timer
                    Console.Clear();
                    var time = TimeSpan.FromSeconds((double)10);
                    long tick = time.Ticks;
                    Console.WriteLine("Invalid operation!\nVerify the operation code and try again.");
                    while (tick > 0)
                    {
                        tick--;
                    }
                    if (tick == 0)
                    {
                        BackMenu();
                    }
                    #endregion
                    break;
            }

        }
        public static void BackMenu()
        {
            Console.Clear();
            Main(new string[0]);
        }
        public static void Atualizar()
        {
            Console.WriteLine("Operation concluded!\n" +
                "Do you want to go to continue using the tool?\n" +
                "\n(Y)Yes/(N)No: ");
            switch (Console.ReadLine().ToLower())
            {
                case "y":
                    BackMenu();
                    break;
                case "n":
                    Environment.Exit(0);
                    break;
                default:
                    Console.Clear();
                    var time = TimeSpan.FromSeconds((double)10);
                    long tick = time.Ticks;
                    Console.WriteLine("Invalid operation!\nVerify the operation code and try again.");
                    while (tick > 0)
                    {
                        tick--;
                    }
                    if (tick == 0)
                    {
                        Atualizar();
                    }
                    break;
            }
        }
       
    }
}
