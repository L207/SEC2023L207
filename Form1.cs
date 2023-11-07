using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RestForm
{
    public partial class Form1 : Form
    {
        public const int MaxCameras = 5;

        public delegate void UpdateImage();
        public UpdateImage imageDelegate;
        public Label messageBar;
        public PictureBox mapPB;
        public PictureBox[] cameraPBs;
        public Image mapImage;
        public Image[] cameraImages;
        public string message;

        void UpdateControls()
        {
            messageBar.Text = message;
            //refresh will automatically update picture boxes from updated images
            Refresh();
        }

        public Form1()
        {
            InitializeComponent();
            imageDelegate = new UpdateImage(UpdateControls);
            cameraPBs = new PictureBox[MaxCameras];
            //cameraImages = new Image[MaxCameras];
            message = "Challenge";
        }

        /// <summary>
        /// SEt Up form controls
        /// </summary>
        public void SetUpFormControls(Bitmap mapBMP, ref Bitmap[] cameraBMPs)
        {

            //text box
            messageBar = new Label
            {
                Name = "Challenge",
                Size = new Size(2 * Form1.imageSize, Form1.borderWidth),
                Location = new Point(2 * Form1.borderWidth + Form1.imageSize / 2, Form1.borderWidth / 2),
                Text = "Challenge",
                TextAlign = ContentAlignment.MiddleCenter,
            };
            Controls.Add(messageBar);

            //map
            mapPB = new PictureBox
            {
                Name = "Map",
                Size = new Size(Form1.imageSize, Form1.imageSize),
                SizeMode = PictureBoxSizeMode.StretchImage,
                Location = new Point(Form1.borderWidth, Form1.borderWidth * 2),
                Image = mapBMP,
            };
            Controls.Add(mapPB);

            for (int i = 0; i < cameraPBs.Length; i++)
            {
                int col = (i + 1) % 3;
                int row = (i + 1) / 3;
                //row = 1 - row;

                cameraPBs[i] = new PictureBox
                {
                    Name = "Camera",
                    Size = new Size(Form1.imageSize, Form1.imageSize),
                    SizeMode = PictureBoxSizeMode.StretchImage,
                    Location = new Point((col + 1) * Form1.borderWidth + col * Form1.imageSize, (row + 2) * Form1.borderWidth + row * Form1.imageSize),
                    Image = cameraBMPs[i],
                };
                Controls.Add(cameraPBs[i]);
            }
        }

        /// <summary>
        /// Shut down form controls
        /// </summary>
        public void ShutDownFormControls()
        {
            Controls.Remove(messageBar);
            //release memory by disposing
            messageBar.Dispose();

            Controls.Remove(mapPB);
            //release memory by disposing
            mapPB.Dispose();

            for (int i = 0; i < cameraPBs.Length; i++)
            {
                Controls.Remove(cameraPBs[i]);
                cameraPBs[i].Dispose();
            }
        }
    }
}
