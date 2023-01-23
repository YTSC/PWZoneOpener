using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PWZoneOpener
{
    public partial class Form1 : Form
    {
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        
        bool elementLoaded = false;
        List<Zone> zones = new List<Zone>();
        string file = null;
        const string hexCode = "00 00 60 42 e0 2e 65 42 a6 9b 44 3b 0a d7 a3 3b 6f 12 83 3a 35 00 00 00 61 00 00 00 c1 00 00 00 85 01 00 00 01 03 00 00 07 06 00 00 07 0c 00 00 07 18 00 00 01 30 00 00 11 60 00 00 05 c0 00 00 0d 80 01 00 05 00 03 00 19 00 06 00 01 00 0c 00 05 00 18 00 0b 00 30 00 0d 00 60 00 05 00 c0 00 13 00 80 01 05 00 00 03 17 00 00 06 13 00 00 0c 05 00 00 18 59 00 00 30 05 00 00 60 01 00 00 c0 fb ff ff ff";
        public Form1()
        {
            InitializeComponent();           
            foreach (Control control in panelZones.Controls)
                if (control is PictureBox)
                {
                    control.MouseClick += drawOnPicture;
                    control.Paint += pictureBox_Paint;
                }                    
        }
       
        public void pictureBox_Paint(object sender, PaintEventArgs e)
        {
            if(elementLoaded)
            {
                PictureBox pb = (PictureBox)sender;
                foreach (Zone zone in zones)
                {
                    string name = "pictureBox" + zone.line + zone.column;                   
                    if (!zone.open && pb.Name == name)
                    {                       
                        Rectangle ee = new Rectangle(0, 0, pb.Width, pb.Height);
                        using (Pen pen = new Pen(Color.Red, 5))
                        {
                            e.Graphics.DrawRectangle(pen, ee);
                        }
                        break;
                    }
                }                
            }            
        }
      
        public void drawOnPicture(object sender, MouseEventArgs e)
        {
            PictureBox pb = (PictureBox)sender;           
            foreach (Zone zone in zones)
            {
                string name = "pictureBox" + zone.line + zone.column;
                if(pb.Name == name)
                {
                    if (zone.open) zone.closeZone();
                    else if (!zone.open) zone.openZone();
                    break;
                }
            }             
            pb.Refresh();           
        }

        public void Reload()
        {
            foreach (Control control in panelZones.Controls)
                if (control is PictureBox)
                    control.Refresh();
        }

        public static byte[] FromHex(string hex)
        {
            hex = hex.Replace(" ", "");
            byte[] raw = new byte[hex.Length / 2];
            for (int i = 0; i < raw.Length; i++)            
                raw[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);   
            
            return raw;
        }

        private long FindOffset(string file, byte[] bytes)
        {           
            long i, j;
            using (FileStream fs = new FileStream(file, FileMode.Open))
            {
                int begin = 4_000_000;
                if (fs.Length > 10_000_000)
                    begin = 9_000_000;

                this.progressBar1.Maximum = (int)fs.Length - begin;
                for (i = begin; i < fs.Length - bytes.Length; i++)
                {
                    progressBar1.Increment(1);
                    fs.Seek(i, SeekOrigin.Begin);
                    for (j = 0; j < bytes.Length; j++)
                    {                       
                        if (fs.ReadByte() != bytes[j]) break;                     
                    }
                    if (j == bytes.Length) break;
                  
                }
            }
            return i;
        }

        /////////////////////////////////////////////////////////////////////////////////////


        //
        //#Region Buttons
        //
        private void BtnExit_Click(object sender, EventArgs e)
        {
            foreach (Control control in this.Controls)
                control.Dispose();

            Application.Exit();
        }

        private void BtnLoad_Click(object sender, EventArgs e)
        {
            if (!elementLoaded)
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.FileName = "elementclient.exe";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                    try
                    {
                        Zone temp;
                        byte[] hexToByte = FromHex(hexCode);
                        long position = FindOffset(openFileDialog.FileName, hexToByte);
                        long offset = position + hexToByte.Length;
                        file = openFileDialog.FileName;
                        using (FileStream fs = new FileStream(openFileDialog.FileName, FileMode.Open))
                        {
                            using (BinaryReader binaryReader = new BinaryReader(fs))
                            {
                                foreach (Control control in panelZones.Controls)
                                    if (control is PictureBox)
                                    {
                                        fs.Position = offset;
                                        string hexValue = "";
                                        for (int x = 0; x < 4; x++)
                                            hexValue += fs.ReadByte().ToString("00");

                                        string line = control.Name.Substring(10, 2);
                                        string column = control.Name.Substring(12, 2);

                                        temp = new Zone(line, column, hexValue, offset);
                                        zones.Add(temp);

                                        offset += 4;
                                    }
                                elementLoaded = true;
                                Reload();
                            }

                        }
                        this.progressBar1.Value = 0;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Failed to open: " + ex.Message + "\n\n" +ex.StackTrace);
                    }
            }         
        }
        private void BtnSave_Click(object sender, EventArgs e)
        {
            if(elementLoaded && file != null)
            {
                try
                {
                    using (FileStream fs = new FileStream(file, FileMode.Open))
                    {
                        using (BinaryWriter binaryWriter = new BinaryWriter(fs))
                        {
                            this.progressBar1.Maximum = zones.Count;
                            foreach (Zone zone in zones)
                            {
                                fs.Position = zone.offset;
                                byte[] binaryData = FromHex(zone.hexValue);
                                binaryWriter.Write(binaryData);
                                progressBar1.Increment(1);
                            }
                            MessageBox.Show(this, "Salvo com sucesso.", "Sucesso!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                }catch(Exception ex)
                {
                    MessageBox.Show("Falha ao salvar: " + ex.Message);
                }               
            }
        }

        private void PanelZones_Scroll(object sender, ScrollEventArgs e)
        {
            if (e.ScrollOrientation == ScrollOrientation.VerticalScroll)
            {
                panelZones.VerticalScroll.Value = e.NewValue;
            }
        }

       
        
        //
        //#Region End
        //
    }
}
