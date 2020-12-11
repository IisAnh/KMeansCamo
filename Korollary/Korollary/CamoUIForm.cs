using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Korollary
{
    public partial class CamoUIForm : Form
    {   
        //Path of uploaded image
        private string imagePath = "";
        private string haskellProgramPath = @".\Haskell Programs";
        private string haskellProgramName = "Kmeans.exe";
        private string haskellProgramArguments = "";
        //Stores the image in this byte array.
        private byte[] rgbValues;
        private int bytesInPixels;

        private BindingList<String> outputModeList = new BindingList<String>(Enum.GetNames(typeof(OutputMode)));
        private int currentOutputMode;
        private int currentKClusters;
        struct rgb_values
        {
            public byte red, green, blue,alpha;
        }
        
        enum OutputMode
        {
            Image,
            Hexadecimal
        }

        public CamoUIForm()
        {
            InitializeComponent();
            listBox1.DataSource = outputModeList;
            button2.Enabled = false;
            button3.Enabled = false;
            listBox1.SelectedIndex = listBox1.TopIndex;
            currentOutputMode = listBox1.SelectedIndex;
            currentKClusters = (int)numericUpDown1.Value;
        }


        private void tableLayoutPanel3_Paint(object sender, PaintEventArgs e)
        {

        }


        //Button Select Image
        private void label5_Click(object sender, EventArgs e)
        {
           
        }

        private void button1_Click(object sender, EventArgs e)
        {
            pictureBox1.ImageLocation = null;
            //String imagePath = "";
            try {
                OpenFileDialog dialog = new OpenFileDialog();
                dialog.Filter = "Image files (*.jpg, *.jpeg, *.jpe, *.jfif, *.png, *.bmp, *.tiff) | *.jpg; *.jpeg; *.jpe; *.jfif; *.png; *.bmp; *.tiff";
                if(dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                    Cursor.Current = Cursors.WaitCursor;
                    this.Enabled = false;
                    imagePath = dialog.FileName;
                    System.Diagnostics.Debug.WriteLine(imagePath);
                    pictureBox1.ImageLocation = imagePath;
                    this.Enabled = true;
                    Cursor.Current = Cursors.Default;
                }

            } catch (Exception) {
                System.Windows.Forms.MessageBox.Show($"Invalid image file \"{imagePath}\"");
            }
            button2.Enabled = true;
        }

        //Compute onClick: Haskell Call
        private void button2_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("***HELLO***");
            Cursor.Current = Cursors.WaitCursor;
            this.Enabled = false;
            PixelConvert2();
            ExternalProgramCall(haskellProgramPath, haskellProgramName, currentKClusters.ToString());
            this.Enabled = true;
            Cursor.Current = Cursors.Default;
            System.Diagnostics.Debug.WriteLine("***DONE***");
            pictureBox2.Image = CreateImageFromByteArray();
            button3.Enabled = true;
        }

        private void ExternalProgramCall(string programPath, string programName, string arguments)
        {            
            string filePath = Path.Combine(programPath, programName);

            System.Diagnostics.ProcessStartInfo processInfo = new System.Diagnostics.ProcessStartInfo(filePath, arguments);
            processInfo.UseShellExecute = false;
            processInfo.CreateNoWindow = true;
            processInfo.RedirectStandardOutput = true;
            processInfo.RedirectStandardInput = true;

            System.Diagnostics.Process externalProccess = new System.Diagnostics.Process { StartInfo = processInfo };

            externalProccess.Start();
            
            //externalProccess.StandardInput.WriteLine("TestInputStandardInput");
           
            for(int i = 0 ; i < rgbValues.Length ; i+=bytesInPixels)
            {
                if(bytesInPixels != 4 || rgbValues[i+3] != 0) {
                    string rgbpixel = $"{rgbValues[i]} {rgbValues[i+1]} {rgbValues[i+2]}";
                    externalProccess.StandardInput.WriteLine(rgbpixel);
                }
            }
            
            externalProccess.StandardInput.Close();

            int index = 0;
            while (!externalProccess.StandardOutput.EndOfStream)
            {
                string line = externalProccess.StandardOutput.ReadLine();
                string[] pixelValues = line.Trim().Split(' ');

                //RGB format back
                if (bytesInPixels == 4)
                {
                    while (rgbValues[index + 3] == 0)
                    {
                        index += bytesInPixels;
                    }
                }
                rgbValues[index] = byte.Parse(pixelValues[0]);
                rgbValues[index + 1] = byte.Parse(pixelValues[1]);
                rgbValues[index + 2] = byte.Parse(pixelValues[2]);
                
                index += bytesInPixels;
            }

            System.Diagnostics.Debug.WriteLine("Finished getting pixels");

            

            //string output = externalProccess.StandardOutput.ReadToEnd();
            //string error = externalProccess.StandardError?.ReadToEnd();
        }

        //private void PixelConvert()
        //{
        //    Bitmap img = new Bitmap(imagePath);

        //    pixels = new rgb_values[img.Width, img.Height];
        //    for (int i = 0; i < img.Width; i++)
        //    {
        //        for (int j = 0; j < img.Height; j++)
        //        {
        //            Color pixel = img.GetPixel(i, j);
        //            pixels[i, j].red = pixel.R;
        //            pixels[i, j].green = pixel.G;
        //            pixels[i, j].blue = pixel.B;
        //            //System.Diagnostics.Debug.WriteLine("R:" + pixels[i,j].red + " G:" + pixels[i,j].green + " B:" + pixels[i,j].blue );
        //        }
        //    }
        //}

        private void PixelConvert2()
        {
            Bitmap bmp = new Bitmap(imagePath);
            int width = bmp.Width;
            int height = bmp.Height;
            int maxPointer = width*height*4; //There are 4 bytes per pixels
            //int stride = w * 4;

            //is it rgba or rgb (3 or 4)
            bytesInPixels = (bmp.PixelFormat == PixelFormat.Format32bppArgb || bmp.PixelFormat == PixelFormat.Format32bppRgb) ? 4 : 3;
            Rectangle rect = new Rectangle(0,0, width, height);
            BitmapData bmpData = bmp.LockBits(rect,ImageLockMode.ReadWrite, bmp.PixelFormat); //Canonical : The default pixel format of 32 bits per pixel. The format specifies 24-bit color depth and an 8-bit alpha channel.

        
            //byte* scan0 = (byte*)bmpData.Scan0.ToPointer();
            IntPtr ptr = bmpData.Scan0;
            
            int bytes = Math.Abs(bmpData.Stride) * height;
            //byte[] rbgValues = new byte[bytes];

            rgbValues = new byte[bytes];
            System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, bytes);

            System.Diagnostics.Debug.WriteLine("The Type: " + bmp.GetType());

            bmp.UnlockBits(bmpData);
        }

        //Save Image onClick:   
        private void button3_Click(object sender, EventArgs e)
        {
            Image currentImage = pictureBox2.Image;
            ImageFormat imageFormat = currentImage.RawFormat;

            //Stream myStream;
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();

            saveFileDialog1.Filter = "Image files (*.jpg, *.jpeg, *.gif, *.png, *.bmp, *.tiff) | *.jpg; *.jpeg; *.gif; *.png; *.bmp; *.tiff";
            saveFileDialog1.FilterIndex = 1;
            saveFileDialog1.RestoreDirectory = true;
            saveFileDialog1.DefaultExt = imageFormat.ToString();
            saveFileDialog1.FileName = "result";

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                //if ((myStream = saveFileDialog1.OpenFile()) != null)
                //{

                //    myStream.Close();
                //}
                string fileExtension = System.IO.Path.GetExtension(saveFileDialog1.FileName);

                switch(fileExtension) {
                    case ".jpg":
                        imageFormat = ImageFormat.Jpeg;
                        break;
                    case ".jpeg":
                        imageFormat = ImageFormat.Jpeg;
                        break;
                    case ".gif":
                        imageFormat = ImageFormat.Gif;
                        break;
                    case ".png":
                        imageFormat = ImageFormat.Png;
                        break;
                    case ".bmp":
                        imageFormat = ImageFormat.Bmp;
                        break;
                    case ".tiff":
                        imageFormat = ImageFormat.Tiff;
                        break;
                    default:
                        imageFormat = ImageFormat.Jpeg;
                        break;
                }

                currentImage.Save(saveFileDialog1.FileName, imageFormat);
            }
        }

        private Image CreateImageFromByteArray()
        {
            Bitmap newImage = new Bitmap(imagePath);
            Rectangle area = new Rectangle(0, 0, newImage.Width, newImage.Height);
            BitmapData newImageData = newImage.LockBits(area, ImageLockMode.ReadWrite, newImage.PixelFormat);
            int stride = newImageData.Stride;
            
            int pixelcounter = 0;

            unsafe
            {
                byte* ptr = (byte*)newImageData.Scan0;

                for (int y = 0; y < newImage.Height; y++)
                {
                    for (int x = 0; x < newImage.Width; x++)
                    {
                        ptr[(x * bytesInPixels) + y * stride] = rgbValues[pixelcounter++];//B
                        ptr[(x * bytesInPixels) + y * stride + 1] = rgbValues[pixelcounter++];//G
                        ptr[(x * bytesInPixels) + y * stride + 2] = rgbValues[pixelcounter++];//R
                        if (bytesInPixels == 4)
                        {
                            ptr[(x * bytesInPixels) + y * stride + 3] = rgbValues[pixelcounter++];//A
                        }
                    }
                }
            }
            newImage.UnlockBits(newImageData);

            return newImage;
        }
        

        private void numericUpDown1_ValueChanged_1(object sender, EventArgs e)
        {
            //K Means Clusters Change Value
            Type currentType = sender.GetType();
            
            if(currentType.Equals(typeof(NumericUpDown)))
            {
                NumericUpDown temp = (NumericUpDown)sender;
                currentKClusters = (int)temp.Value;
            }
            
        }
        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void pictureBox2_Click_1(object sender, EventArgs e)
        {

        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            //Output Mode Change
            Type currentType = sender.GetType();

            if (currentType.Equals(typeof(ListBox)))
            {
                ListBox temp = (ListBox)sender;
                currentOutputMode = (int)temp.SelectedIndex;
            }
        }
    }
}
