using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SNW_TextEditor
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        List<FileStruct> ListData = new List<FileStruct>();

        public class LangStrings
        {
            public string language;
            public string strings;

            public LangStrings() { }

            public LangStrings(string _language, string _strings)
            {
                this.language = _language;
                this.strings = _strings;
            }
        }

        public struct FileStruct
        {
            public int block_length; //Длина блока
            public int count_strings; //Количество строк в блоке
            public int lang_length; //Длина абривеатуры языка
            public byte[] lang; //Абривеатура языка (Ну там, INT, FRA, RUS и т.п.)
            public int text_length; //Длина строки
            public byte[] text; //Строка

            public FileStruct(int _block_length, int _count_strings, int _lang_length,
                byte[] _lang, int _text_length, byte[] _text)
            {
                this.block_length = _block_length;
                this.count_strings = _count_strings;
                this.lang_length = _lang_length;
                this.lang = _lang;
                this.text_length = _text_length;
                this.text = _text;
            }
        }

        public string[] getSoundNodeFiles(string Dir_path)
        {
            string[] snfinfo = Directory.GetFiles(Dir_path, "*.SoundNodeWave", SearchOption.TopDirectoryOnly);

            if (snfinfo.Length > 0) return snfinfo;
            else return null;
        }

        public static byte[] ReadFull(Stream stream)
        {
            byte[] buffer = new byte[3207];

            using (MemoryStream ms = new MemoryStream())
            {
                while (true)
                {
                    int read = stream.Read(buffer, 0, buffer.Length);
                    if (read <= 0)
                        return ms.ToArray();
                    ms.Write(buffer, 0, read);
                }
            }
        }

        public static string ConvertHexToString(byte[] array, int poz, int len_string)
        {
            try
            {
                byte[] temp_hex_string = new byte[len_string];
                Array.Copy(array, poz, temp_hex_string, 0, len_string);
                string result;
                result = ASCIIEncoding.ASCII.GetString(temp_hex_string);
                return result;
            }
            catch
            { return "error"; }
        }

        public static int FindStartOfStringSomething(byte[] array, int offset, string string_something)
        {
            int poz = offset;
            while (ConvertHexToString(array, poz, string_something.Length) != string_something)
            {
                poz++;
                if (ConvertHexToString(array, poz, string_something.Length) == string_something)
                {
                    return poz;
                }
                if ((poz + string_something.Length + 1) > array.Length)
                {
                    break;
                }
            }
            return poz;
        }

        private void exportBtn_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog FBD = new FolderBrowserDialog();
            FBD.Description = "Choose folder with files .SoundNodeWave";

            if (FBD.ShowDialog() == DialogResult.OK)
            {
                string path = FBD.SelectedPath;

                if (Directory.Exists(path))
                {
                    string[] getFiles = getSoundNodeFiles(path);
                    if (getFiles != null)
                    {
                        progressBar.Minimum = 0;
                        progressBar.Maximum = getFiles.Length - 1;

                        for (int i = 0; i < getFiles.Length; i++)
                        {

                            if (File.Exists(getFiles[i]))
                            {
                                try
                                {
                                    FileStream fs = new FileStream(getFiles[i], FileMode.Open);
                                    byte[] binContent = ReadFull(fs);
                                    fs.Close();

                                    int offset = 88; //Смещение к данным о количестве строк
                                    byte[] bin_count = new byte[4];
                                    Array.Copy(binContent, offset, bin_count, 0, bin_count.Length);
                                    offset += 44; //Смещаемся к длине первого блока
                                    byte[] block_length = new byte[4];
                                    Array.Copy(binContent, offset, block_length, 0, block_length.Length);
                                    offset += 8; //Смещаемся к началу блока
                                    int length = BitConverter.ToInt32(block_length, 0);
                                    offset += length + 16; //Пропускаем блок - он нам не нужен

                                    byte[] checkLength = new byte[4];
                                    Array.Copy(binContent, offset + 8, checkLength, 0, checkLength.Length);//- 16, checkLength, 0, checkLength.Length);

                                    if (BitConverter.ToInt32(checkLength, 0) == 17)//!= 13)
                                    {

                                        byte[] lengthBlock = new byte[4];
                                        Array.Copy(binContent, offset, lengthBlock, 0, lengthBlock.Length);
                                        length = BitConverter.ToInt32(lengthBlock, 0);
                                        offset += 12;
                                        byte[] text_block = new byte[length];
                                        Array.Copy(binContent, offset, text_block, 0, text_block.Length);

                                        binContent = null;

                                        //List<string> LangFlag = new List<string>();
                                        //List<string> ListText = new List<string>();

                                        string newPath = getFiles[i].Replace(".SoundNodeWave", ".txt");

                                        StreamWriter sw = new StreamWriter(newPath);

                                        offset = 24; //Для нового блока

                                        for (int j = 0; j < BitConverter.ToInt32(bin_count, 0); j++)
                                        {
                                            byte[] langLength = new byte[4];
                                            Array.Copy(text_block, offset, langLength, 0, langLength.Length);
                                            offset += 4;
                                            length = BitConverter.ToInt32(langLength, 0);
                                            byte[] getLang = new byte[length - 1];
                                            Array.Copy(text_block, offset, getLang, 0, getLang.Length);
                                            offset += length + 24;

                                            byte[] str_count = new byte[4];
                                            Array.Copy(text_block, offset, str_count, 0, str_count.Length);

                                            if (BitConverter.ToInt32(str_count, 0) > 1)
                                            {
                                                offset += 28;
                                                string langAndText = Encoding.GetEncoding(1251).GetString(getLang);

                                                for (int k = 0; k < BitConverter.ToInt32(str_count, 0); k++)
                                                {
                                                    langAndText += "\t";
                                                    byte[] textLength = new byte[4];
                                                    Array.Copy(text_block, offset, textLength, 0, textLength.Length);
                                                    offset += 4;
                                                    length = BitConverter.ToInt32(textLength, 0);
                                                    byte[] getText = new byte[length - 1];
                                                    Array.Copy(text_block, offset, getText, 0, getText.Length);
                                                    offset += length + 60;
                                                    langAndText += Encoding.GetEncoding(1251).GetString(getText) + "\r\n";
                                                }
                                                sw.Write(langAndText);
                                                offset += 58;
                                            }
                                            else
                                            {
                                                offset += 28;
                                                byte[] textLength = new byte[4];
                                                Array.Copy(text_block, offset, textLength, 0, textLength.Length);
                                                offset += 4;
                                                length = BitConverter.ToInt32(textLength, 0);
                                                byte[] getText = new byte[length - 1];
                                                Array.Copy(text_block, offset, getText, 0, getText.Length);
                                                offset += length + 118;
                                                string langString = Encoding.GetEncoding(1251).GetString(getLang);
                                                string textString = Encoding.GetEncoding(1251).GetString(getText);

                                                sw.WriteLine(langString + "\t" + textString);
                                            }

                                        }

                                        sw.Close();
                                        progressBar.Value = i;
                                    }
                                }
                                catch
                                {
                                    progressBar.ForeColor = Color.Blue;
                                    progressBar.Value = i;
                                }
                            }
                            else
                            {
                                progressBar.ForeColor = Color.Red;
                                progressBar.Value = i;
                            }
                        }
                    } 
                    else MessageBox.Show("В папке нет файлов формата .SoundNodeWave");
                }
                else MessageBox.Show("ТЫ КУДА, ГНИДА, МЕНЯ ЗАВЁЛ?!");
            }
        }

        private void importBtn_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fdb = new FolderBrowserDialog();

            if (fdb.ShowDialog() == DialogResult.OK)
            {
                string path = fdb.SelectedPath;

                if (Directory.Exists(path))
                {
                    string[] getFiles = getSoundNodeFiles(path);
                    if (getFiles != null)
                    {
                        bool first_file = true;
                        int length_block = 0;
                        int length_block2 = 0;
                        int difference = 0;
                        int difference2 = 0;
                        int diff1 = 0;
                        int diff2 = 0;
                        int hz = 0;
                        int comulator = 0;

                        progressBar.Minimum = 0;
                        progressBar.Maximum = getFiles.Length - 1;

                        for (int i = 0; i < getFiles.Length; i++)
                        {
                            FileStream fs = new FileStream(getFiles[i], FileMode.Open);
                            byte[] Content = ReadFull(fs);
                            fs.Close();

                            if (first_file)
                            {
                                byte[] getData = new byte[4];
                                Array.Copy(Content, Content.Length - 4, getData, 0, getData.Length);
                                length_block2 = BitConverter.ToInt32(getData, 0);
                                length_block = length_block2 - 16;
                                difference = length_block - Content.Length;
                                difference2 = length_block2 - Content.Length;
                            }

                            /*fs = new FileStream(getFiles[i] + ".bak", FileMode.OpenOrCreate);
                            fs.Write(Content, 0, Content.Length);
                            fs.Close();*/

                            int offset = 88; //Смещение к данным о количестве строк
                            byte[] bin_count = new byte[4];
                            Array.Copy(Content, offset, bin_count, 0, bin_count.Length);
                            offset += 44; //Смещаемся к длине первого блока
                            byte[] block_length = new byte[4];
                            Array.Copy(Content, offset, block_length, 0, block_length.Length);
                            offset += 8; //Смещаемся к началу блока
                            int length = BitConverter.ToInt32(block_length, 0);
                            offset += length + 16; //Пропускаем блок - он нам не нужен

                            byte[] checkLength = new byte[4];
                            Array.Copy(Content, offset + 8, checkLength, 0, checkLength.Length);//- 16, checkLength, 0, checkLength.Length);


                            if (BitConverter.ToInt32(checkLength, 0) == 17)//!= 13)
                            {
                                string textpath = getFiles[i].Replace(".SoundNodeWave", ".txt");
                                if (File.Exists(textpath))
                                {
                                    string[] strings = File.ReadAllLines(textpath);

                                    if (strings.Length > 0)
                                    {
                                        //List<string> Language = new List<string>();
                                        //List<string> Texts = new List<string>();

                                        List<LangStrings> par = new List<LangStrings>();
                                        


                                        byte[] lengthBlock = new byte[4];
                                        Array.Copy(Content, offset, lengthBlock, 0, lengthBlock.Length);
                                        length = BitConverter.ToInt32(lengthBlock, 0);
                                        offset += 12;

                                        hz = length;

                                        byte[] StartBlock = new byte[offset];
                                        Array.Copy(Content, 0, StartBlock, 0, StartBlock.Length);
                                        byte[] text_block = new byte[length];
                                        Array.Copy(Content, offset, text_block, 0, text_block.Length);
                                        offset += length;
                                        byte[] EndBlock = new byte[Content.Length - offset];
                                        Array.Copy(Content, offset, EndBlock, 0, EndBlock.Length);

                                        offset = 24; //Для нового блока

                                        int lang_c = 0;

                                        for (int j = 0; j < strings.Length; j++)
                                        {
                                            par.Add(new LangStrings(null, null));
                                            string[] substring = strings[j].Split('\t');

                                            if (substring[0] == "")
                                            {
                                                int k = j - 1;
                                                par[j].language = par[k].language;
                                                par[j].strings = substring[1] + "\0";
                                            }
                                            else
                                            {
                                                par[j].language = substring[0];
                                                par[j].strings = substring[1] + "\0";
                                                lang_c++;
                                            }
                                        }

                                        /*if (par.Count == BitConverter.ToInt32(bin_count, 0))
                                        {*/
                                            MemoryStream ms = new MemoryStream();
                                            byte[] block = new byte[76];
                                            Array.Copy(text_block, 0, block, 0, block.Length);
                                            ms.Write(block, 0, block.Length);
                                            offset = block.Length - 20;

                                            int c = 0; //Для нескольких строк

                                            for (int j = 0; j < lang_c; j++)//BitConverter.ToInt32(bin_count, 0); j++)
                                            {
                                               // byte[] langLength = new byte[4];
                                                //Array.Copy(text_block, offset, langLength, 0, langLength.Length);
                                                //offset += 4;
                                                //length = BitConverter.ToInt32(langLength, 0);
                                                //byte[] getLang = new byte[length - 1];
                                                //Array.Copy(text_block, offset, getLang, 0, getLang.Length);
                                                //offset += length + 24;

                                                byte[] str_count = new byte[4];
                                                Array.Copy(text_block, offset, str_count, 0, str_count.Length);
                                                offset += 28;

                                                if (BitConverter.ToInt32(str_count, 0) > 1)
                                                {
                                                    //offset += 28;

                                                    //string lang = Encoding.GetEncoding(1251).GetString(getLang);

                                                    for (int k = 0; k < BitConverter.ToInt32(str_count, 0); k++)
                                                    {
                                                        byte[] or_length = new byte[4];
                                                        Array.Copy(text_block, offset, or_length, 0, or_length.Length);

                                                        byte[] bin_str = Encoding.GetEncoding(1251).GetBytes(par[c].strings);

                                                        byte[] bin_length = new byte[4];
                                                        bin_length = BitConverter.GetBytes(bin_str.Length + 4);
                                                        byte[] bin_length2 = new byte[4];
                                                        bin_length2 = BitConverter.GetBytes(bin_str.Length);
                                                        ms.Write(bin_length, 0, bin_length.Length);
                                                        byte[] zero = { 0x00, 0x00, 0x00, 0x00 };
                                                        ms.Write(zero, 0, zero.Length);
                                                        ms.Write(bin_length2, 0, bin_length2.Length);
                                                        ms.Write(bin_str, 0, bin_str.Length);
                                                        offset += 4 + BitConverter.ToInt32(or_length, 0);
                                                        block = new byte[52];
                                                        Array.Copy(text_block, offset, block, 0, block.Length);
                                                        ms.Write(block, 0, block.Length);
                                                        offset += block.Length + 8;
                                                        c++;
                                                        /*byte[] textLength = new byte[4];
                                                        Array.Copy(text_block, offset, textLength, 0, textLength.Length);
                                                        offset += 4;
                                                        length = BitConverter.ToInt32(textLength, 0);
                                                        byte[] getText = new byte[length - 1];
                                                        Array.Copy(text_block, offset, getText, 0, getText.Length);
                                                        offset += length + 60;
                                                        lang += Encoding.GetEncoding(1251).GetString(getText) + "\r\n";*/
                                                    }
                                                    
                                                    offset -= 8;
                                                    
                                                    block = new byte[118];
                                                    if (block.Length > text_block.Length - offset)
                                                    {
                                                        block = new byte[text_block.Length - offset];
                                                    }
                                                    Array.Copy(text_block, offset, block, 0, block.Length);
                                                    ms.Write(block, 0, block.Length);
                                                    offset += block.Length - 20;
                                                }
                                                else
                                                {
                                                    byte[] or_length = new byte[4];
                                                    Array.Copy(text_block, offset, or_length, 0, or_length.Length);

                                                    byte[] bin_str = Encoding.GetEncoding(1251).GetBytes(par[c].strings);
                                                    c++;

                                                        byte[] bin_length = new byte[4];
                                                        bin_length = BitConverter.GetBytes(bin_str.Length + 4);
                                                        byte[] bin_length2 = new byte[4];
                                                        bin_length2 = BitConverter.GetBytes(bin_str.Length);
                                                        ms.Write(bin_length, 0, bin_length.Length);
                                                        byte[] zero = { 0x00, 0x00, 0x00, 0x00 };
                                                        ms.Write(zero, 0, zero.Length);
                                                        ms.Write(bin_length2, 0, bin_length2.Length);
                                                        ms.Write(bin_str, 0, bin_str.Length);
                                                        offset += 4 + BitConverter.ToInt32(or_length, 0);
                                                        block = new byte[170];

                                                        if (block.Length > text_block.Length - offset) block = new byte[text_block.Length - offset];

                                                    Array.Copy(text_block, offset, block, 0, block.Length);
                                                    ms.Write(block, 0, block.Length);
                                                    offset += block.Length - 20;

                                                    /*string langString = Encoding.GetEncoding(1251).GetString(getLang);
                                                    offset += 28;

                                                    byte[] textLength = new byte[4];
                                                    Array.Copy(text_block, offset, textLength, 0, textLength.Length);
                                                    offset += 4;
                                                    length = BitConverter.ToInt32(textLength, 0);
                                                    byte[] getText = new byte[length - 1];
                                                    Array.Copy(text_block, offset, getText, 0, getText.Length);
                                                    offset += length + 118;

                                                    string textString = Encoding.GetEncoding(1251).GetString(getText);*/
                                                }

                                            }

                                            par.Clear();
                                        //}
                                            byte[] new_block = ms.ToArray();
                                            ms.Close();

                                            /*if (hz != new_block.Length)
                                            {*/
                                            if (first_file)
                                            {
                                                diff1 = new_block.Length - hz;
                                                diff2 = new_block.Length - hz;
                                                int off = FindStartOfStringSomething(EndBlock, 0, "OggS") - 20;
                                                byte[] temp = new byte[4];
                                                Array.Copy(EndBlock, off, temp, 0, temp.Length);
                                                int i_temp = BitConverter.ToInt32(temp, 0) + diff1;
                                                temp = new byte[4];
                                                temp = BitConverter.GetBytes(i_temp);
                                                Array.Copy(temp, 0, EndBlock, off, temp.Length);

                                                off += 16;
                                                temp = new byte[4];
                                                Array.Copy(EndBlock, off, temp, 0, temp.Length);
                                                i_temp = BitConverter.ToInt32(temp, 0) + diff2;
                                                temp = new byte[4];
                                                temp = BitConverter.GetBytes(i_temp);
                                                Array.Copy(temp, 0, EndBlock, off, temp.Length);

                                                comulator += i_temp + (EndBlock.Length - (off + 4));
                                            }
                                            else
                                            {
                                                comulator += StartBlock.Length + new_block.Length + FindStartOfStringSomething(EndBlock, 0, "OggS");
                                                diff1 = new_block.Length - hz;
                                                diff2 = new_block.Length - hz;
                                                int off = FindStartOfStringSomething(EndBlock, 0, "OggS") - 20;
                                                byte[] temp = new byte[4];
                                                Array.Copy(EndBlock, off, temp, 0, temp.Length);
                                                int i_temp = comulator - 16;
                                                temp = new byte[4];
                                                temp = BitConverter.GetBytes(i_temp);
                                                Array.Copy(temp, 0, EndBlock, off, temp.Length);

                                                off += 16;
                                                temp = new byte[4];
                                                Array.Copy(EndBlock, off, temp, 0, temp.Length);
                                                i_temp = comulator;
                                                temp = new byte[4];
                                                temp = BitConverter.GetBytes(i_temp);
                                                Array.Copy(temp, 0, EndBlock, off, temp.Length);
                                                
                                                comulator += EndBlock.Length - (off + 4);
                                            }
                                            //}

                                            /*if (first_file)
                                            {
                                                byte[] temp = new byte[4];
                                                int off = 16;
                                                Array.Copy(EndBlock, off, temp, 0, temp.Length);
                                                diff1 = Content.Length - BitConverter.ToInt32(temp, 0);
                                                off += 16;
                                                temp = new byte[4];
                                                Array.Copy(EndBlock, off, temp, 0, temp.Length);
                                                diff2 = Content.Length - BitConverter.ToInt32(temp, 0);

                                                int t_length = diff1 + StartBlock.Length + EndBlock.Length;
                                                int t_length2 = diff2 + StartBlock.Length + EndBlock.Length;

                                                temp = new byte[4];
                                                temp = BitConverter.GetBytes(t_length);
                                                off = 16;

                                                Array.Copy(temp, 0, EndBlock, off, temp.Length);
                                                off += 16;

                                                temp = new byte[4];
                                                temp = BitConverter.GetBytes(t_length2);
                                                Array.Copy(temp, 0, EndBlock, off, temp.Length);

                                                diff1 = t_length;
                                                diff2 = t_length2;
                                            }
                                            else
                                            {
                                                diff1 += StartBlock.Length + new_block.Length + EndBlock.Length;
                                                diff2 += StartBlock.Length + new_block.Length + EndBlock.Length;
                                                byte[] temp = new byte[4];
                                                temp = BitConverter.GetBytes(diff1);
                                                int off = 16;

                                                Array.Copy(temp, 0, EndBlock, off, temp.Length);
                                                off += 16;

                                                temp = new byte[4];
                                                temp = BitConverter.GetBytes(diff2);
                                                Array.Copy(temp, 0, EndBlock, off, temp.Length);
                                            }*/

                                            ms = new MemoryStream();

                                            offset = StartBlock.Length - 12;
                                            byte[] bin_block_ln = new byte[4];
                                            bin_block_ln = BitConverter.GetBytes(new_block.Length);
                                            Array.Copy(bin_block_ln, 0, StartBlock, offset, bin_block_ln.Length);
                                            ms.Write(StartBlock, 0, StartBlock.Length);
                                            StartBlock = null;
                                            ms.Write(new_block, 0, new_block.Length);
                                            new_block = null;
                                            ms.Write(EndBlock, 0, EndBlock.Length);
                                            EndBlock = null;
                                            Content = ms.ToArray();
                                            ms.Close();

                                            if (first_file)
                                            {
                                                length_block = difference + Content.Length;
                                                length_block2 = difference2 + Content.Length;
                                                byte[] bin_length = new byte[4];
                                                bin_length = BitConverter.GetBytes(length_block);
                                                int of_offset = Content.Length - 20;
                                                Array.Copy(bin_length, 0, Content, of_offset, bin_length.Length);
                                                of_offset = Content.Length - 4;
                                                bin_length = BitConverter.GetBytes(length_block2);
                                                Array.Copy(bin_length, 0, Content, of_offset, bin_length.Length);
                                                first_file = false;
                                            }
                                            else
                                            {
                                                length_block += Content.Length;
                                                int of_offset = Content.Length - 20;
                                                byte[] bin_length = new byte[4];
                                                bin_length = BitConverter.GetBytes(length_block);
                                                Array.Copy(bin_length, 0, Content, of_offset, bin_length.Length);

                                                length_block2 += Content.Length;
                                                of_offset = Content.Length - 4;
                                                bin_length = new byte[4];
                                                bin_length = BitConverter.GetBytes(length_block2);
                                                Array.Copy(bin_length, 0, Content, of_offset, bin_length.Length);
                                            }

                                            //string test = getFiles[i].Replace(".SoundNodeWave", ".test");
                                            if (File.Exists(getFiles[i])) File.Delete(getFiles[i]);
                                            fs = new FileStream(getFiles[i], FileMode.CreateNew);
                                            fs.Write(Content, 0, Content.Length);
                                            fs.Close();
                                            
                                        
                                            progressBar.Value = i;
                                    }
                                }
                            }
                            else
                            {

                                if (first_file)
                                {
                                    length_block = difference + Content.Length;
                                    length_block2 = difference2 + Content.Length;
                                    byte[] bin_length = new byte[4];
                                    bin_length = BitConverter.GetBytes(length_block);
                                    int of_offset = Content.Length - 20;
                                    Array.Copy(bin_length, 0, Content, of_offset, bin_length.Length);
                                    of_offset = Content.Length - 4;
                                    bin_length = BitConverter.GetBytes(length_block2);
                                    Array.Copy(bin_length, 0, Content, of_offset, bin_length.Length);
                                    first_file = false;
                                }
                                else
                                {
                                    length_block += Content.Length;
                                    int of_offset = Content.Length - 20;
                                    byte[] bin_length = new byte[4];
                                    bin_length = BitConverter.GetBytes(length_block);
                                    Array.Copy(bin_length, 0, Content, of_offset, bin_length.Length);

                                    length_block2 += Content.Length;
                                    of_offset = Content.Length - 4;
                                    bin_length = new byte[4];
                                    bin_length = BitConverter.GetBytes(length_block2);
                                    Array.Copy(bin_length, 0, Content, of_offset, bin_length.Length);
                                }

                                if (first_file)
                                {
                                    if (FindStartOfStringSomething(Content, 0, "OggS") < Content.Length - 100)
                                    {
                                        int pos = FindStartOfStringSomething(Content, 0, "OggS") - 4;
                                        byte[] binLength = new byte[4];
                                        Array.Copy(Content, pos, binLength, 0, binLength.Length);
                                        comulator = BitConverter.ToInt32(binLength, 0) + (Content.Length - (pos + 4));
                                        first_file = false;
                                    }
                                }
                                else
                                {
                                    if (FindStartOfStringSomething(Content, 0, "OggS") < Content.Length - 100)
                                    {
                                        comulator += FindStartOfStringSomething(Content, 0, "OggS");
                                        int pos = FindStartOfStringSomething(Content, 0, "OggS") - 20;
                                        byte[] binLength = new byte[4];
                                        binLength = BitConverter.GetBytes(comulator - 16);
                                        Array.Copy(binLength, 0, Content, pos, binLength.Length);
                                        pos += 16;

                                        binLength = new byte[4];
                                        binLength = BitConverter.GetBytes(comulator);
                                        Array.Copy(binLength, 0, Content, pos, binLength.Length);

                                        comulator = BitConverter.ToInt32(binLength, 0) + (Content.Length - (pos + 4));
                                    }
                                }

                                //string test = getFiles[i].Replace(".SoundNodeWave", ".test");
                                if (File.Exists(getFiles[i])) File.Delete(getFiles[i]);
                                fs = new FileStream(getFiles[i], FileMode.CreateNew);
                                fs.Write(Content, 0, Content.Length);
                                fs.Close();

                                progressBar.Value = i;
                            }
                        }
                    }
                }
            }
        }
    }
}
