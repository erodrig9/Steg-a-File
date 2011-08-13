using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Security.Cryptography;
using System.IO.Compression;
using ICSharpCode.SharpZipLib.Zip;

namespace Steg_A_File
{
    class StegFunctions
    {
        public static void Compress(string FileIn, string FileOut, int BufferSize)
        {
            FileStream fileStreamIn = new FileStream(FileIn, FileMode.Open, FileAccess.Read);
            FileStream fileStreamOut = new FileStream(FileOut, FileMode.Create, FileAccess.Write);
            ZipOutputStream zipOutStream = new ZipOutputStream(fileStreamOut);

            byte[] buffer = new byte[BufferSize];

            ZipEntry entry = new ZipEntry(Path.GetFileName(FileIn));
            zipOutStream.PutNextEntry(entry);

            int size;
            do
            {
                size = fileStreamIn.Read(buffer, 0, buffer.Length);
                zipOutStream.Write(buffer, 0, size);
            } while (size > 0);

            zipOutStream.Close();
            fileStreamOut.Close();
            fileStreamIn.Close();
        }

        public static void Uncompress(string SrcFile, string DstFile, int BufferSize)
        {
            FileStream fileStreamIn = new FileStream(SrcFile, FileMode.Open, FileAccess.Read);
            ZipInputStream zipInStream = new ZipInputStream(fileStreamIn);
            ZipEntry entry = zipInStream.GetNextEntry();
            FileStream fileStreamOut = new FileStream(DstFile, FileMode.Create, FileAccess.Write);

            int size;
            byte[] buffer = new byte[BufferSize];
            do
            {
                size = zipInStream.Read(buffer, 0, buffer.Length);
                fileStreamOut.Write(buffer, 0, size);
            } while (size > 0);

            zipInStream.Close();
            fileStreamOut.Close();
            fileStreamIn.Close();
        }

        public static void Steg(byte[] MP3, int MP3_size, byte[] Hide, int Hide_size, string FileName, string Password)
        {
            int i, j;
            int position = 0, prev_position = 0;
            byte head = 0xFF, next = 0xFB, pass_flag = 0x41; // flag marks end of password

            FileStream MP3_Stream = new FileStream(FileName, FileMode.Create, FileAccess.Write);
            BinaryWriter MP3_Write = new BinaryWriter(MP3_Stream);

            for (i = 0; (i + 1) < MP3_size; i++)
            {
                if (MP3[i] == head && MP3[i + 1] == next)
                {
                    prev_position = position;
                    position = i - 1;
                }
            }
            position = prev_position;

            for (i = 0; i < MP3_size; i++)
            {
                if (i == position)
                {
                    MP3_Write.Write(head);
                    MP3_Write.Write(next);
                    MP3_Write.Write(pass_flag);
                    MP3_Write.Write(pass_flag);
                    MP3_Write.Write(Password);
                    MP3_Write.Write(pass_flag);
                    MP3_Write.Write(pass_flag);


                    for (j = 0; j < Hide_size; j++)
                        MP3_Write.Write(Hide[j]);

                    break;
                }
                else
                    MP3_Write.Write(MP3[i]);
            }

            MP3_Write.Close();
            MP3_Stream.Close();
        }

        public static string Unsteg(byte[] MP3, int MP3_size, string FileName)
        {
            int i, j;
            int position = 0;
            byte head = 0xFF, next = 0xFB, flag = 0x41; // flag marks end of password
            string Password = "";
            byte[] temp = new byte[20];

            FileStream MP3_Stream = new FileStream(FileName, FileMode.Create, FileAccess.Write);
            BinaryWriter File_Write = new BinaryWriter(MP3_Stream);

            for (i = 0; i + 1 < MP3_size; i++)
            {
                if (MP3[i] == head && MP3[i + 1] == next)
                {
                    if (MP3[i + 2] == flag && MP3[i + 3] == flag)
                        position = i + 4;
                }
            }

            i = 0;
            position++;

            while (position < MP3_size && i < 20)
            {
                if (MP3[position] == flag && MP3[position + 1] == flag)
                    break;

                temp[i] = MP3[position];
                position++;
                i++;
            }
            position += 2;

            byte[] pass_array = new byte[i];
            for (j = 0; j < i; j++)
            {
                pass_array[j] = temp[j];
            }

            ASCIIEncoding encoding = new ASCIIEncoding();
            Password = encoding.GetString(pass_array);

            for (j = position; j < MP3_size; j++)
                File_Write.Write(MP3[j]);



            File_Write.Close();
            MP3_Stream.Close();

            return Password;
        }

        public static byte[] Read(Stream stream, int initialLength)
        {
            byte[] buffer = new byte[initialLength];
            int read = 0;

            int chunk;
            while ((chunk = stream.Read(buffer, read, buffer.Length - read)) > 0)
            {
                read += chunk;

                // If we've reached the end of our buffer, check to see if there's
                // any more information
                if (read == buffer.Length)
                {
                    int nextByte = stream.ReadByte();

                    // End of stream? If so, we're done
                    if (nextByte == -1)
                    {
                        return buffer;
                    }

                    // Nope. Resize the buffer, put in the byte we've just
                    // read, and continue
                    byte[] newBuffer = new byte[buffer.Length * 2];
                    Array.Copy(buffer, newBuffer, buffer.Length);
                    newBuffer[read] = (byte)nextByte;
                    buffer = newBuffer;
                    read++;
                }
            }
            // Buffer is now too big. Shrink it.
            byte[] ret = new byte[read];
            Array.Copy(buffer, ret, read);

            return ret;
        }

        public static void Encrypt(string fileIn, string fileOut, string Password)
        {
            // First we are going to open the file streams 
            FileStream fsIn = new FileStream(fileIn, FileMode.Open, FileAccess.Read);
            FileStream fsOut = new FileStream(fileOut, FileMode.OpenOrCreate, FileAccess.Write);

            // Then we are going to derive a Key and an IV from the
            // Password and create an algorithm 

            PasswordDeriveBytes pdb = new PasswordDeriveBytes(Password,
                new byte[] {0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 
                0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76});

            Rijndael alg = Rijndael.Create();
            alg.Key = pdb.GetBytes(32);
            alg.IV = pdb.GetBytes(16);

            // Now create a crypto stream through which we are going
            // to be pumping data. 

            // Our fileOut is going to be receiving the encrypted bytes. 

            CryptoStream cs = new CryptoStream(fsOut,
                alg.CreateEncryptor(), CryptoStreamMode.Write);

            // Now will will initialize a buffer and will be processing
            // the input file in chunks. 

            // This is done to avoid reading the whole file (which can
            // be huge) into memory. 

            int bufferLen = 4096;
            byte[] buffer = new byte[bufferLen];
            int bytesRead;

            do
            {
                // read a chunk of data from the input file 
                bytesRead = fsIn.Read(buffer, 0, bufferLen);

                // encrypt it 
                cs.Write(buffer, 0, bytesRead);
            } while (bytesRead != 0);

            // close everything 
            cs.Close();
            fsIn.Close();
        }

        public static void Decrypt(string fileIn, string fileOut, string Password)
        {
            // First we are going to open the file streams 
            FileStream fsIn = new FileStream(fileIn, FileMode.Open, FileAccess.Read);
            FileStream fsOut = new FileStream(fileOut, FileMode.Create, FileAccess.Write);

            // Then we are going to derive a Key and an IV from
            // the Password and create an algorithm 
            PasswordDeriveBytes pdb = new PasswordDeriveBytes(Password,
                new byte[] {0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 
                0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76});
            Rijndael alg = Rijndael.Create();

            alg.Key = pdb.GetBytes(32);
            alg.IV = pdb.GetBytes(16);

            // Now create a crypto stream through which we are going
            // to be pumping data. 

            // Our fileOut is going to be receiving the Decrypted bytes. 

            CryptoStream cs = new CryptoStream(fsOut, alg.CreateDecryptor(), CryptoStreamMode.Write);

            // Now will will initialize a buffer and will be 
            // processing the input file in chunks. 

            // This is done to avoid reading the whole file (which can be
            // huge) into memory. 

            int bufferLen = 4096;
            byte[] buffer = new byte[bufferLen];
            int bytesRead;

            do
            {
                // read a chunk of data from the input file 

                bytesRead = fsIn.Read(buffer, 0, bufferLen);

                // Decrypt it 

                cs.Write(buffer, 0, bytesRead);

            } while (bytesRead != 0);

            // close everything 

            cs.Close(); // this will also close the unrelying fsOut stream 

            fsIn.Close();
        }

        public static string Hash(string toEncrypt, string key, bool useHashing)
        {
            string hashed_pass;
            byte[] keyArray;
            byte[] toEncryptArray = UTF8Encoding.UTF8.GetBytes(toEncrypt);

            FileStream fs = new FileStream(".hspass", FileMode.Append);
            StreamWriter w = new StreamWriter(fs, Encoding.UTF8);

            if (useHashing)
            {
                MD5CryptoServiceProvider hashmd5 = new MD5CryptoServiceProvider();
                keyArray = hashmd5.ComputeHash(UTF8Encoding.UTF8.GetBytes(key));
                hashmd5.Clear();
            }
            else
                keyArray = UTF8Encoding.UTF8.GetBytes(key);

            TripleDESCryptoServiceProvider tdes = new TripleDESCryptoServiceProvider();
            tdes.Key = keyArray;
            tdes.Mode = CipherMode.ECB;
            tdes.Padding = PaddingMode.PKCS7;

            ICryptoTransform cTransform = tdes.CreateEncryptor();
            byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);

            hashed_pass = Convert.ToBase64String(resultArray, 0, resultArray.Length);
            w.WriteLine(hashed_pass);

            // clean up
            tdes.Clear();
            w.Flush();
            w.Close();
            fs.Close();

            return hashed_pass;
        }

        public static string Unhash(string toDecrypt, string key, bool useHashing)
        {
            byte[] keyArray;
            byte[] toEncryptArray = Convert.FromBase64String(toDecrypt);

            if (useHashing)
            {
                MD5CryptoServiceProvider hashmd5 = new MD5CryptoServiceProvider();
                keyArray = hashmd5.ComputeHash(UTF8Encoding.UTF8.GetBytes(key));
                hashmd5.Clear();
            }
            else
                keyArray = UTF8Encoding.UTF8.GetBytes(key);

            TripleDESCryptoServiceProvider tdes = new TripleDESCryptoServiceProvider();
            tdes.Key = keyArray;
            tdes.Mode = CipherMode.ECB;
            tdes.Padding = PaddingMode.PKCS7;

            ICryptoTransform cTransform = tdes.CreateDecryptor();
            byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);

            tdes.Clear();

            return UTF8Encoding.UTF8.GetString(resultArray);
        }
    }
}
