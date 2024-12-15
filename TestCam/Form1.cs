using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.Util;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using System.Threading.Tasks;
namespace TestCam
{
    public partial class Form1 : Form
    {
        private Capture capture;
        private Image<Bgr, Byte> IMG;
        private Image<Gray, Byte> GrayImg;
        private Image<Gray, Byte> Inv_bin;

//(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)
//(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)
        
        
        public Form1()
        {
            InitializeComponent();
            
            textBox3.Text = trackBar1.Value.ToString();
            textBox4.Text = trackBar2.Value.ToString();
        }
        
//(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)
//(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)
        private void processFrame(object sender, EventArgs e)
        {
            if (capture == null)//very important to handle exception
            {
                try
                {
                    capture = new Capture();
                    capture.SetCaptureProperty(CAP_PROP.CV_CAP_PROP_FRAME_WIDTH, 640);
                    capture.SetCaptureProperty(CAP_PROP.CV_CAP_PROP_FRAME_HEIGHT, 480);
                }
                catch (NullReferenceException excpt)
                {
                    MessageBox.Show(excpt.Message);
                }
            }

            IMG = capture.QueryFrame();
            
            GrayImg = IMG.Convert<Gray, Byte>();
            Inv_bin = GrayImg.ThresholdBinaryInv(new Gray(trackBar1.Value), new Gray(255)); // Calibrate here
            Inv_bin = applyAreaFilter(Inv_bin, trackBar2.Value);
            
            double Xpx = 0;
            double Ypx = 0;
            int N = 0;
            
            for (int i = 0; i < Inv_bin.Width; i++) {
            	for (int j = 0; j < Inv_bin.Height; j++) {
            		if(Inv_bin[j, i].Intensity > 128){
            			N++;
        				Xpx += i;
        				Ypx += j;
            		}
            	}
            }
            
            Xpx = Xpx / N;
            Ypx = Ypx / N;
            
            textBox1.Text = Xpx.ToString();
            textBox2.Text = Ypx.ToString();
            
            
            try
            {
                
                imageBox1.Image = IMG;
                imageBox2.Image = GrayImg;
                imageBox3.Image = Inv_bin;
                
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

//(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)
//(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)

        private void button1_Click(object sender, EventArgs e)
        {
            Application.Idle += processFrame;
            button1.Enabled = false;
            button2.Enabled = true;
        }
//(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)
//(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)
        private void button2_Click(object sender, EventArgs e)
        {
            Application.Idle -= processFrame;
            button1.Enabled = true;
            button2.Enabled = false;
        }    
//(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)
//(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)
        private void button3_Click(object sender, EventArgs e)
        {
            IMG.Save("Image" +  ".jpg");
        }       
//(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)
//(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)(*)
        
        void TrackBar1Scroll(object sender, EventArgs e)
        {
        	textBox3.Text = trackBar1.Value.ToString();
        }
        
        private Image<Gray, Byte> applyAreaFilter(Image<Gray, Byte> imgRef, int rounds){
        	var img = imgRef.Clone();
        	List<Tuple<int, int>> positionsToZero = new List<Tuple<int, int>>(); // keep which positions to set to 0 in this list
        	object lockObj = new object(); // lock object for thread-safety
        	
        	// directions
        	int[] dirW = {0, 1, 0, -1};
        	int[] dirH = {1, 0, -1, 0};
        	
        	// find 1s with at least one "0" neighbor
        	int r = 0;
        	while (r < rounds) {
        		Parallel.For(0, img.Width, w => {
	             	for (int h = 0; h < img.Height; h++) {
	             		if(img[h, w].Intensity >128){
	             			// check neighbors
	             			for (int i = 0; i < 4; i++) {
	             				int nW = w + dirW[i];
	             				int nH = h + dirH[i];
	             				
	             				// check if out of bounds and
	             				if (nW >= 0 && nW < img.Width && nH >= 0 && nH < img.Height){
	             					// check if neighbor is 0
	             					if(img[nH, nW].Intensity <= 128){
	             						// use lock to avoid racing
	             						lock(lockObj){
	             							positionsToZero.Add(new Tuple<int, int>(h, w));
	             						}
	             						// no need to check other directions since at least one "0" neighbor is found
	             						break;
	             					}
	             				}
	             			}
	             		}
	             	}
	         	});
	        	
	        	// now use the positionsToZero List to alter the image
	        	Parallel.ForEach(positionsToZero, p =>{
	             	img[p.Item1, p.Item2] = new Gray(0);
	         	});
        		
        		r++;
        	}
        	
        	return img;
        }

        
        void TrackBar2Scroll(object sender, EventArgs e)
        {
        	textBox4.Text = trackBar2.Value.ToString();
        }
    }
}
