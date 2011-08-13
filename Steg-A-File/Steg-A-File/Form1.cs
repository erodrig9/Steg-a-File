using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Security.Cryptography;
using System.IO.Compression;
using ICSharpCode.SharpZipLib.Zip;

namespace Steg_A_File
{
    public partial class Form1 : Form
    {

        public static string File_Path = "";
        public static string MP3_Path = "";
        public static string Output_Path = "";
        public static string Password = "";

        public Form1()
        {
            InitializeComponent();
        }
        //**************************STEG Portion *********************************
        
        private void button1_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "All files (*.*)|*.*|MP3 files (*.mp3)|*.mp3";
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string filePath = ofd.FileName;
                string safeFilePath = ofd.SafeFileName;
            }

            label4.Text = ofd.SafeFileName;
            File_Path = ofd.FileName;
        }
        private void button2_Click_1(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "MP3 files (*.mp3)|*.mp3";
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string filePath = ofd.FileName;
                string safeFilePath = ofd.SafeFileName;
            }
            label5.Text = ofd.SafeFileName;
            MP3_Path = ofd.FileName;
        }

        private void button6_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "MP3 files (*.mp3)|*.mp3";
            if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string filePath = sfd.FileName;
            }
            label7.Text = sfd.FileName;
            Output_Path = sfd.FileName;
        }  
        //Steg -> Password Box 1
        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            

        }
            
        //Steg -> Password Box 2 (Confirm Password)
        private void textBox2_TextChanged_1(object sender, EventArgs e)
        {

        }

        private void progressBar1_Click(object sender, EventArgs e)
        {
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
        }

        private void button3_Click_1(object sender, EventArgs e)
        {
            progressBar1.Maximum = 100;
            progressBar1.Minimum = 0;

            if (File_Path == "")
            {
                MessageBox.Show("Please enter a file to hide",
         "Caution", MessageBoxButtons.OK);
                return;
            }
            else if (MP3_Path == "")
            {
                MessageBox.Show("Please enter an MP3",
         "Caution", MessageBoxButtons.OK);
                return;
            }
            else if (Output_Path == "")
            {
                MessageBox.Show("Please enter an output path",
         "Caution", MessageBoxButtons.OK);
                return;
            }


            if (textBox1.Text != textBox2.Text)
            {
                MessageBox.Show("Please enter matching passwords",
         "Caution", MessageBoxButtons.OK);
                return;
            }
            else
            {
                int MP3_length, File_length;

                Password = textBox1.Text;

                StegFunctions.Compress(File_Path, "comp", 4096);

                progressBar1.Value += 20;

                StegFunctions.Encrypt("comp", "enc", Password);

                FileStream MP3_Read = File.OpenRead(MP3_Path);
                FileStream File_Read = File.OpenRead("enc");
                MP3_length = (int)MP3_Read.Length;
                File_length = (int)File_Read.Length;

                progressBar1.Value += 20;
                
                byte[] MP3_stream = new byte[MP3_length];
                byte[] File_stream = new byte[File_length];
                MP3_stream = StegFunctions.Read(MP3_Read, MP3_length);
                File_stream = StegFunctions.Read(File_Read, File_length);

                progressBar1.Value += 20;

                MP3_Read.Close();
                File_Read.Close();
                File.Delete("enc");
                File.Delete("comp");

                progressBar1.Value += 20;

                Password = StegFunctions.Hash(Password, "MD5HashAlgorithm", true);

                StegFunctions.Steg(MP3_stream, MP3_length, File_stream, File_length, Output_Path, Password);
                
                progressBar1.Value += 20;
                MessageBox.Show("You have Been STEGGED!", "", MessageBoxButtons.OK);
                progressBar1.Value = 0;
            }
            
        }

        

        //***********************Un STEG Portion*************************

        private void button4_Click_1(object sender, EventArgs e)
        {
            System.Windows.Forms.OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "MP3 files (*.mp3)|*.mp3|All files (*.*)|*.*";
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string filePath = ofd.FileName;
                string safeFilePath = ofd.SafeFileName;
            }
            label6.Text = ofd.SafeFileName;
            MP3_Path = ofd.FileName;
        }
        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            if (textBox1.Text == "")
            {

            }
        }
        private void button5_Click_1(object sender, EventArgs e)
        {
            progressBar2.Maximum = 100;
            progressBar2.Minimum = 0;

            if (MP3_Path == "")
            {
                MessageBox.Show("Please enter an MP3",
         "Caution", MessageBoxButtons.OK);
                return;
            }
            else if (Output_Path == "")
            {
                MessageBox.Show("Please enter an output path",
         "Caution", MessageBoxButtons.OK);
                return;
            }


            if (textBox3.Text == null)
            {
                MessageBox.Show("Enter a valid Password",
         "Caution", MessageBoxButtons.OK);
                return;
            }
            else
            {
                int MP3_length;
                string stream_pass = "";

                Password = textBox3.Text;

                progressBar2.Value += 20;

                FileStream MP3_Read = File.OpenRead(MP3_Path);
                MP3_length = (int)MP3_Read.Length;

                byte[] MP3_stream = new byte[MP3_length];
                MP3_stream = StegFunctions.Read(MP3_Read, MP3_length);

                MP3_Read.Close();

                progressBar2.Value += 20;

                stream_pass = StegFunctions.Unsteg(MP3_stream, MP3_length, "dec");
                stream_pass = StegFunctions.Unhash(stream_pass, "MD5HashAlgorithm", true);

                if (stream_pass != Password)
                {
                    MessageBox.Show("Enter a valid Password",
         "Caution", MessageBoxButtons.OK);
                    File.Delete("dec");
                    progressBar2.Value = 0;
                    return;
                }

                progressBar2.Value += 20;

                StegFunctions.Decrypt("dec", "uncomp", Password);
                StegFunctions.Uncompress("uncomp", Output_Path, 4096);

                progressBar2.Value += 20;

                File.Delete("dec");
                File.Delete("uncomp");

                progressBar2.Value += 20;

                MessageBox.Show("UNSTEGGABELIEVABLE!",
         "", MessageBoxButtons.OK);
                progressBar2.Value = 0;

            }
        }

        private void progressBar2_Click_1(object sender, EventArgs e)
        {
            this.progressBar2 = new System.Windows.Forms.ProgressBar();
        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void button7_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "All files (*.*)|*.*|MP3 files (*.mp3)|*.mp3"; 
            if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string filePath = sfd.FileName;
            }
            label8.Text = sfd.FileName;
            Output_Path = sfd.FileName;
        }

        private void label6_Click(object sender, EventArgs e)
        {

        }

 
    }
}
