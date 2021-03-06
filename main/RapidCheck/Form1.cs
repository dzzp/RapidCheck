﻿using MaterialSkin;
using MaterialSkin.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Forms;
using System.IO;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;
using System.Threading;
//using AxWMPLib; //player
using Shell32;
using CefSharp;
using CefSharp.WinForms;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Axes;
using System.Diagnostics;

namespace RapidCheck
{
    public partial class Form1 : MaterialForm
    {
        //----------------------------전역변수-----------------------------
        string videoPath = null;
        OpenFileDialog videoFilePath;
        int outputFrameNum = 0;
        RapidCheck.OverlayVideo rapidCheck;
        delegate void rapidModule();
        delegate void rapidChain(rapidModule dele);
        Thread overlayModule;
        List<rapidModule> myRapidModule = new List<rapidModule>();
        string createTime;
        int directionPosition = -1;
        int colorPosition = -1;
        int classPosition = -1;
        bool searchPeople = true;
        bool searchCar = true;

        //------------------------------Form------------------------------
        public Form1()
        {
            InitializeComponent();
            var skinManager = MaterialSkinManager.Instance;
            skinManager.AddFormToManage(this);
            //skinManager.Theme = MaterialSkinManager.Themes.DARK;
            skinManager.Theme = MaterialSkinManager.Themes.LIGHT;
            skinManager.ColorScheme = new ColorScheme(
                Primary.Grey50, // tab contorol
                Primary.Grey50, //최상단 
                Primary.BlueGrey700,
                Accent.LightBlue400,
                TextShade.BLACK);
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            Cef.Initialize(new CefSettings()); // chrome initialize
            setUI(); //ui enable false
            defaultColor(); //color setting
        }
        //------------------------------Overlay Module------------------------------
        private void startOverlayModule()
        {
            createTime = setCreateTime(System.IO.Path.GetDirectoryName(videoFilePath.FileName), System.IO.Path.GetFileName(videoFilePath.FileName));
            int maxFrameNum = 10000;
            int analysisFPS = 5; //default
            int minTrackingLength = 10;
            int clusterNum = trackBar2.Value;
            //int clusterNum = 20;
            outputFrameNum = 1500;
            rapidCheck = new RapidCheck.OverlayVideo(labelEndTime, labelVideoInfo1, labelVideoInfo2, labelVideoInfo3, labelProgress, dataGridView1, dataGridView2, startBtn, trackBar1, pictureBoxVideo, videoPath, createTime, maxFrameNum, analysisFPS, minTrackingLength, clusterNum, outputFrameNum); //ObjList setting
            rapidFunc();
            overlayModule = new Thread(() => rapidRun());
            overlayModule.Start();
            pictureBoxProgress.Visible = true;
        }
        private void basicFlow(rapidModule dele) { dele(); }
        private void rapidFunc()
        {
            myRapidModule.Add(rapidCheck.getMysqlObjList);
            myRapidModule.Add(rapidCheck.barChartSetting);// chart
            myRapidModule.Add(rapidCheck.pieChartSetting);// chart
            myRapidModule.Add(rapidCheck.LineChartSetting);// chart
            myRapidModule.Add(rapidCheck.addObj);
            myRapidModule.Add(rapidCheck.objCount);// chart
            myRapidModule.Add(rapidCheck.imageCrop);
            myRapidModule.Add(rapidCheck.objectClustering);
            myRapidModule.Add(rapidCheck.buildOverlayOrderUsingCluster);
            myRapidModule.Add(rapidCheck.setPictureBoxSize);
            myRapidModule.Add(rapidCheck.overlayLive);
        } 
        private void rapidRun()
        {
            rapidChain myRapidChain = new rapidChain(basicFlow);
            for (int idx = 0; idx < myRapidModule.Count; idx++)
            {
                if (myRapidModule[idx].Method.ToString() == "Void overlayLive()" )
                {
                    setOverlayUI();
                }
                myRapidChain(myRapidModule[idx]);

                if (myRapidModule[idx].Method.ToString() == "Void barChartSetting()")
                {
                    plotViewBar.Model = rapidCheck.modelBarChart;
                }
                else if (myRapidModule[idx].Method.ToString() == "Void pieChartSetting()")
                {
                    plotViewPie1.Model = rapidCheck.modelPieChartPeople;
                    plotViewPie2.Model = rapidCheck.modelLineChart;
                }
                else if(myRapidModule[idx].Method.ToString() == "Void LineChartSetting()")
                {
                    plotViewLine.Model = rapidCheck.modelLineChart;
                }
                else if (myRapidModule[idx].Method.ToString() == "Void objCount()")
                {
                    labelCarCnt.Text = rapidCheck.carTotal.ToString();
                    labelPeopleCnt.Text = rapidCheck.peopleTotal.ToString();
                }
            }
        }
        private string setCreateTime(string folderPath, string fileName) //파일에서 시간 가져오는 함수
        {
            Shell32.Shell shell = new Shell32.Shell();
            Shell32.Folder objFolder;
            objFolder = shell.NameSpace(folderPath);
            Shell32.FolderItem fi;
            fi = objFolder.ParseName(fileName);
            string temp = objFolder.GetDetailsOf(fi, 4);
            temp = temp.Split('오')[1];
            string AmPm = temp.Split(' ')[0];
            string hour = temp.Split(' ')[1].Split(':')[0];
            string min = temp.Split(' ')[1].Split(':')[1];
            if (AmPm == "후")
            {
                int h = Int32.Parse(hour) + 12;
                if (h == 24) h = 12;
                hour = h.ToString();
            }
            else if (Int32.Parse(hour) == 12)
            {
                hour = "0";
            }
            return hour + ":" + min;
        }
        //------------------------------UI SETTING------------------------------
        private void defaultColor()
        {
            int style = 1;
            Color background;
            Color module;
            Color conditionModule;
            Color conditionModuleTable;
            if(style == 1)
            {
                background = Color.FromArgb(35, 144, 182);
                module = Color.FromArgb(171, 219, 248);
                conditionModule = Color.FromArgb(64, 127, 149);
                conditionModuleTable = Color.FromArgb(105, 180, 203);
            }
            else if(style == 2)
            {
                background = Color.WhiteSmoke;
                module = Color.FloralWhite;
                conditionModule = Color.Gray;
                conditionModuleTable = Color.DarkGray;
            }
            else
            {
                background = Color.FromArgb(45,45,48);
                module = Color.FromArgb(37,37,38);
                conditionModule = Color.FromArgb(51, 51, 54);
                conditionModuleTable = Color.FromArgb(30, 30, 30);
                //pictureBoxVideo.BackColor = Color.FromArgb(30, 30, 30);
            }
            panelVideo.BackColor = background;
            panelCondition.BackColor = background;
            panelObject.BackColor = background;

            panelConditionModule.BackColor = module;
            panelVideoControl.BackColor = module;
            trackBar1.BackColor = module;
            //panelLog.BackColor = module;
            dataGridView1.BackgroundColor = module;
            dataGridView2.BackgroundColor = module;

            panelColor.BackColor = conditionModule;
            panelDensity.BackColor = conditionModule;
            panelDirection.BackColor = conditionModule;
            panelFile.BackColor = conditionModule;
            panelTarget.BackColor = conditionModule;

            panelDensityTable.BackColor = conditionModuleTable;
            panelDirectionTable.BackColor = conditionModuleTable;
            panelColorTable.BackColor = conditionModuleTable;
            pictureBoxTargetCar.BackColor = conditionModuleTable;
            pictureBoxTargetPeople.BackColor = conditionModuleTable;
        }
        private void setUI() //default UI setting
        {
            //set tabkpage
            tabPage1.Text = "분석";
            tabPage2.Text = "영상";
            tabPage3.Text = "통계";
            tabPage4.Text = "학습";
            //video player
            MaximizeBox = false; //최대화 단추... 표시 여부
            //speed Btn
            radioButtonX1.Enabled = false;
            radioButtonX2.Enabled = false;
            radioButtonX4.Enabled = false;
            //trackBar
            trackBar1.Enabled = false;
            //start Btn
            startBtn.Enabled = false;
            //Target Btn
            pictureBoxTargetPeople.Enabled = false;
            pictureBoxTargetCar.Enabled = false;
            //Direction Btn
            pictureBoxDirection1.Enabled = false;
            pictureBoxDirection2.Enabled = false;
            pictureBoxDirection3.Enabled = false;
            pictureBoxDirection4.Enabled = false;
            pictureBoxDirection5.Enabled = false;
            pictureBoxDirection6.Enabled = false;
            pictureBoxDirection7.Enabled = false;
            pictureBoxDirection8.Enabled = false;
            pictureBoxDirection9.Enabled = false;
            //Color Btn
            buttonColor0.Enabled = false;
            buttonColor1.Enabled = false;
            buttonColor2.Enabled = false;
            buttonColor3.Enabled = false;
            buttonColor4.Enabled = false;
            buttonColor5.Enabled = false;
            buttonColor6.Enabled = false;
            buttonColor7.Enabled = false;
            buttonColor8.Enabled = false;
            buttonColor9.Enabled = false;
        }
        private void setOverlayUI() //UI enable = True
        {
            pictureBoxProgress.Visible = false;
            startBtn.Text = "Pause";
            pictureBoxStart.Image = overlayPause;
            //trackBar
            trackBar1.Minimum = 0;
            //trackBar1.Maximum = outputFrameNum - 1;
            trackBar1.Value = 0;
            //enable
            radioButtonX1.Enabled = true;
            radioButtonX2.Enabled = true;
            radioButtonX4.Enabled = true;
            radioButtonX1.Checked = true;
            startBtn.Enabled = true;
            //speed Btn
            radioButtonX1.Enabled = true;
            radioButtonX2.Enabled = true;
            radioButtonX4.Enabled = true;
            //trackBar
            trackBar1.Enabled = true;
            //start Btn
            startBtn.Enabled = true;
            //Target Btn
            pictureBoxTargetPeople.Enabled = true;
            pictureBoxTargetCar.Enabled = true;
            //Direction Btn
            pictureBoxDirection1.Enabled = true;
            pictureBoxDirection2.Enabled = true;
            pictureBoxDirection3.Enabled = true;
            pictureBoxDirection4.Enabled = true;
            pictureBoxDirection5.Enabled = true;
            pictureBoxDirection6.Enabled = true;
            pictureBoxDirection7.Enabled = true;
            pictureBoxDirection8.Enabled = true;
            pictureBoxDirection9.Enabled = true;
            //Color Btn
            buttonColor0.Enabled = true;
            buttonColor1.Enabled = true;
            buttonColor2.Enabled = true;
            buttonColor3.Enabled = true;
            buttonColor4.Enabled = true;
            buttonColor5.Enabled = true;
            buttonColor6.Enabled = true;
            buttonColor7.Enabled = true;
            buttonColor8.Enabled = true;
            buttonColor9.Enabled = true;
        }
        //------------------------------Read video EVENT------------------------------
        private void buttonReadFile_Click(object sender, EventArgs e)
        {
            //if (overlayModule.IsAlive == true)
            //{
            //    overlayModule.Abort();
            //}
            if(buttonReadFile.Text == "파일")
            {
                videoFilePath = new OpenFileDialog();
                videoFilePath.Filter = "All Files (*.*)|*.*";
                videoFilePath.FilterIndex = 1;
                videoFilePath.Multiselect = true;
                videoFilePath.InitialDirectory = @"C:\videos";
                if (videoFilePath.ShowDialog() == DialogResult.OK)
                {
                    videoPath = videoFilePath.FileName;
                    startOverlayModule();
                    labelProgress.Text = "Loading...";

                    //read video file -> search로 변경
                    buttonReadFile.Text = "검색";
                }
            }
            else if(buttonReadFile.Text == "검색")
            {
                replay();
            }
        }
        //------------------------------Video controler------------------------------
        private void trackBar1_MouseDown(object sender, MouseEventArgs e)
        {
            trackBar1.Value = Convert.ToInt32(1.0 * trackBar1.Maximum * e.Location.X / trackBar1.Width);
            //MessageBox.Show(trackBar1.Value.ToString());
        }
        private void trackBar1_ValueChanged(object sender, EventArgs e)
        {
            rapidCheck.resFrame = trackBar1.Value;
            int min = rapidCheck.resFrame / rapidCheck.analysisFPS / 60;
            int sec = rapidCheck.resFrame / rapidCheck.analysisFPS % 60;
            labelCurrentTime.Text =  min.ToString("00") + ":" + sec.ToString("00");
            //rapidCheck.overlayObjIdx = trackBar1.Value;
            rapidCheck.overlayObjIdx = 0;
        }
        Bitmap overlayStart = new Bitmap(@"C:\Users\SoMa\Desktop\RapidCheck\main\RapidCheck\asset\play.png");
        Bitmap overlayPause = new Bitmap(@"C:\Users\SoMa\Desktop\RapidCheck\main\RapidCheck\asset\pause.png");
        private void startBtn_Click(object sender, EventArgs e)
        {
            if (startBtn.Text == "Start") 
            { 
                startBtn.Text = "Pause";
                pictureBoxStart.Image = overlayPause;
            }
            else 
            {
                startBtn.Text = "Start";
                pictureBoxStart.Image = overlayStart;
            }
        }
        //------------------------------Video Click EVENT------------------------------
        private void pictureBoxVideo_MouseDown(object sender, MouseEventArgs e) //비디오 클릭하면 원본 영상 틀어주는 함수
        {
            if (trackBar1.Enabled == true)
            {
                int fps = rapidCheck.fps;
                startBtn.Text = "Start";
                Point clickPosition = e.Location;
                double clickPositionOriginX = (double)clickPosition.X / pictureBoxVideo.Width * rapidCheck.videoWidth;
                double clickPositionOriginY = (double)clickPosition.Y / pictureBoxVideo.Height * rapidCheck.videoHeight;
                int frameNum = rapidCheck.getClickedObjectOriginalFrameNum(clickPositionOriginX, clickPositionOriginY);
                if (frameNum >= 0)
                {
                    materialTabControl1.SelectedTab = tabPage3;
                    axWindowsMediaPlayer1.URL = videoFilePath.FileName;
                    axWindowsMediaPlayer1.Ctlcontrols.play();
                    axWindowsMediaPlayer1.Ctlcontrols.currentPosition = (double)frameNum / fps;
                }
            }
        }
        //------------------------------tab page control------------------------------
        public ChromiumWebBrowser browser;
        private void materialTabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            int idx = materialTabControl1.SelectedIndex;
            if (idx ==3) // 라벨링
            {
                browser = new ChromiumWebBrowser("http://127.0.0.1:5000")
                {
                    Dock = DockStyle.Fill,
                    Size = Size,
                };
                panelWebbrowser.Controls.Add(browser);
                browser.Size = new Size(panelWebbrowser.Size.Width, panelWebbrowser.Size.Height);
            }
            else if( idx == 2) //통계
            {
                //chart
            }
        }
        //------------------------------DataGridView Click EVENT------------------------------
        private void dataGridView2_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            //e.RowIndex;
            int id = rapidCheck.gridViewList2[e.RowIndex];
            DateTime startTime = rapidCheck.getClickedGridViewOriginalDataTime(id);

            int hour = Convert.ToInt32(createTime.Split(':')[0]);
            int min = Convert.ToInt32(createTime.Split(':')[1]);
            hour = startTime.Hour - hour;
            min = startTime.Minute - min;
            int sec = startTime.Second;
            int position = (hour * 60 + min)*60 + sec;

            if(startTime != null)
            {
                materialTabControl1.SelectedTab = tabPage3;
                axWindowsMediaPlayer1.URL = videoFilePath.FileName;
                axWindowsMediaPlayer1.Ctlcontrols.play();
                axWindowsMediaPlayer1.Ctlcontrols.currentPosition = (double)position;
            }
        }
        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            //e.RowIndex;
            int id = rapidCheck.gridViewList1[e.RowIndex];
            DateTime startTime = rapidCheck.getClickedGridViewOriginalDataTime(id);

            int hour = Convert.ToInt32(createTime.Split(':')[0]);
            int min = Convert.ToInt32(createTime.Split(':')[1]);
            hour = startTime.Hour - hour;
            min = startTime.Minute - min;
            int sec = startTime.Second;
            int position = (hour * 60 + min) * 60 + sec;

            if (startTime != null)
            {
                materialTabControl1.SelectedTab = tabPage3;
                axWindowsMediaPlayer1.URL = videoFilePath.FileName;
                axWindowsMediaPlayer1.Ctlcontrols.play();
                axWindowsMediaPlayer1.Ctlcontrols.currentPosition = (double)position;
            }
        }
        //------------------------------speed Btn Click ENVET------------------------------
        private void radioButtonX1_CheckedChanged(object sender, EventArgs e) { rapidCheck.speed = 1; }
        private void radioButtonX2_CheckedChanged(object sender, EventArgs e) { rapidCheck.speed = 2; }
        private void radioButtonX4_CheckedChanged(object sender, EventArgs e) { rapidCheck.speed = 4; }
        //------------------------------Object type Click EVENT------------------------------
        Bitmap peopleImgOn = new Bitmap(@"C:\Users\SoMa\Desktop\RapidCheck\main\RapidCheck\asset\human_on@2x.png");
        Bitmap peopleImgOff = new Bitmap(@"C:\Users\SoMa\Desktop\RapidCheck\main\RapidCheck\asset\human_off@2x.png");
        private void pictureBoxTargetPeople_Click(object sender, EventArgs e) 
        {
            if (searchPeople)
            {
                searchPeople = false;
                pictureBoxTargetPeople.Image = peopleImgOff;
            }
            else
            {
                searchPeople = true;
                pictureBoxTargetPeople.Image = peopleImgOn;
            }
        } // 0 = people
        Bitmap carImgOn = new Bitmap(@"C:\Users\SoMa\Desktop\RapidCheck\main\RapidCheck\asset\car_on@2x.png");
        Bitmap carImgOff = new Bitmap(@"C:\Users\SoMa\Desktop\RapidCheck\main\RapidCheck\asset\car_off@2x.png");
        private void pictureBoxTargetCar_Click(object sender, EventArgs e)
        {
            if (searchCar)
            {
                searchCar = false;
                pictureBoxTargetCar.Image = carImgOff;
            }
            else
            {
                searchCar = true;
                pictureBoxTargetCar.Image = carImgOn;
            }
        } // 1 = car
        //------------------------------Direction Btn Click EVENT------------------------------
        private void pictureBoxDirection1_Click(object sender, EventArgs e) {
            if (directionPosition == 1)
            {
                directionPosition = -1;
            }
            else
            {
                directionPosition = 1;
            }
            setTogleDirectionBtn();
        }
        private void pictureBoxDirection2_Click(object sender, EventArgs e)
        {
            if (directionPosition == 2)
            {
                directionPosition = -1;
            }
            else
            {
                directionPosition = 2;
            }
            setTogleDirectionBtn();
        }
        private void pictureBoxDirection3_Click(object sender, EventArgs e)
        {
            if (directionPosition == 3)
            {
                directionPosition = -1;
            }
            else
            {
                directionPosition = 3;
            }
            setTogleDirectionBtn();
        }
        private void pictureBoxDirection4_Click(object sender, EventArgs e)
        {
            if (directionPosition == 4)
            {
                directionPosition = -1;
            }
            else
            {
                directionPosition = 4;
            }
            setTogleDirectionBtn();
        }
        private void pictureBoxDirection5_Click(object sender, EventArgs e)
        {
            if (directionPosition == 5)
            {
                colorPosition = -1;
            }
            else
            {
                directionPosition = 5;
            }
        } //검색 설정 초기화
        private void pictureBoxDirection6_Click(object sender, EventArgs e)
        {
            if (directionPosition == 6)
            {
                colorPosition = -1;
            }
            else
            {
                directionPosition = 6;
            }
            setTogleDirectionBtn();
        }
        private void pictureBoxDirection7_Click(object sender, EventArgs e)
        {
            if (directionPosition == 7)
            {
                directionPosition = -1;
            }
            else
            {
                directionPosition = 7;
            }
            setTogleDirectionBtn();
        }
        private void pictureBoxDirection8_Click(object sender, EventArgs e)
        {
            if (directionPosition == 8)
            {
                directionPosition = -1;
            }
            else
            {
                directionPosition = 8;
            }
            setTogleDirectionBtn();
        }
        private void pictureBoxDirection9_Click(object sender, EventArgs e)
        {
            if (directionPosition == 9)
            {
                directionPosition = -1;
            }
            else
            {
                directionPosition = 9;
            }
            setTogleDirectionBtn();
        }
        //------------------------------Color Btn Click EVENT------------------------------
        private void buttonColor0_Click(object sender, EventArgs e)
        {
            if (colorPosition == 0)
            {
                colorPosition = -1;
                buttonColor0.FlatStyle = FlatStyle.Flat;
            }
            else
            {
                colorPosition = 0;
                buttonColor0.FlatAppearance.BorderColor = Color.Black;
                buttonColor0.FlatAppearance.BorderSize = 4;
            }
            setTogleColorBtn();
        }
        private void buttonColor1_Click(object sender, EventArgs e)
        {
            if (colorPosition == 1)
            {
                colorPosition = -1;                
            }
            else
            {
                colorPosition = 1;

            }
            setTogleColorBtn();
        }
        private void buttonColor2_Click(object sender, EventArgs e)
        {
            if (colorPosition == 2)
            {
                colorPosition = -1;
            }
            else
            {
                colorPosition = 2;
            }
            setTogleColorBtn();
        }
        private void buttonColor3_Click(object sender, EventArgs e)
        {
            if (colorPosition == 3)
            {
                colorPosition = -1;
            }
            else
            {
                colorPosition = 3;
            }
            setTogleColorBtn();
        }
        private void buttonColor4_Click(object sender, EventArgs e)
        {
            if (colorPosition == 4)
            {
                colorPosition = -1;
            }
            else
            {
                colorPosition = 4;
            }
            setTogleColorBtn();
        }
        private void buttonColor5_Click(object sender, EventArgs e)
        {
            if (colorPosition == 5)
            {
                colorPosition = -1;
            }
            else
            {
                colorPosition = 5;
            }
            setTogleColorBtn();
        }
        private void buttonColor6_Click(object sender, EventArgs e)
        {
            if (colorPosition == 6)
            {
                colorPosition = -1;
            }
            else
            {
                colorPosition = 6;
            }
            setTogleColorBtn();
        }
        private void buttonColor7_Click(object sender, EventArgs e)
        {
            if (colorPosition == 7)
            {
                colorPosition = -1;
            }
            else
            {
                colorPosition = 7;
            }
            setTogleColorBtn();
        }
        private void buttonColor8_Click(object sender, EventArgs e)
        {
            if (colorPosition == 8)
            {
                colorPosition = -1;
            }
            else
            {
                colorPosition = 8;
            }
            setTogleColorBtn();
        }
        private void buttonColor9_Click(object sender, EventArgs e)
        {
            if (colorPosition == 9)
            {
                colorPosition = -1;
            }
            else
            {
                colorPosition = 9;
            }
            setTogleColorBtn();
        }
        private void setTogleColorBtn()
        {
            buttonColor0.FlatAppearance.BorderSize = 0;
            buttonColor1.FlatAppearance.BorderSize = 0;
            buttonColor2.FlatAppearance.BorderSize = 0;
            buttonColor3.FlatAppearance.BorderSize = 0;
            buttonColor4.FlatAppearance.BorderSize = 0;
            buttonColor5.FlatAppearance.BorderSize = 0;
            buttonColor6.FlatAppearance.BorderSize = 0;
            buttonColor7.FlatAppearance.BorderSize = 0;
            buttonColor8.FlatAppearance.BorderSize = 0;
            buttonColor9.FlatAppearance.BorderSize = 0;
            switch(colorPosition)
            {
                case 0: buttonColor0.FlatAppearance.BorderSize = 4; break;
                case 1: buttonColor1.FlatAppearance.BorderSize = 4; break;
                case 2: buttonColor2.FlatAppearance.BorderSize = 4; break;
                case 3: buttonColor3.FlatAppearance.BorderSize = 4; break;
                case 4: buttonColor4.FlatAppearance.BorderSize = 4; break;
                case 5: buttonColor5.FlatAppearance.BorderSize = 4; break;
                case 6: buttonColor6.FlatAppearance.BorderSize = 4; break;
                case 7: buttonColor7.FlatAppearance.BorderSize = 4; break;
                case 8: buttonColor8.FlatAppearance.BorderSize = 4; break;
                case 9: buttonColor9.FlatAppearance.BorderSize = 4; break;                    
            }
        }
        //------------------------------Search Function------------------------------
        Bitmap direct1on  = new Bitmap(@"C:\Users\SoMa\Desktop\RapidCheck\main\RapidCheck\asset\1_on.png");
        Bitmap direct2on  = new Bitmap(@"C:\Users\SoMa\Desktop\RapidCheck\main\RapidCheck\asset\2_on.png");
        Bitmap direct3on  = new Bitmap(@"C:\Users\SoMa\Desktop\RapidCheck\main\RapidCheck\asset\3_on.png");
        Bitmap direct4on  = new Bitmap(@"C:\Users\SoMa\Desktop\RapidCheck\main\RapidCheck\asset\4_on.png");
        //Bitmap direct5on  = new Bitmap(@"C:\Users\SoMa\Desktop\RapidCheck\main\RapidCheck\asset_\5on.png");
        Bitmap direct6on  = new Bitmap(@"C:\Users\SoMa\Desktop\RapidCheck\main\RapidCheck\asset\6_on.png");
        Bitmap direct7on  = new Bitmap(@"C:\Users\SoMa\Desktop\RapidCheck\main\RapidCheck\asset\7_on.png");
        Bitmap direct8on  = new Bitmap(@"C:\Users\SoMa\Desktop\RapidCheck\main\RapidCheck\asset\8_on.png");
        Bitmap direct9on  = new Bitmap(@"C:\Users\SoMa\Desktop\RapidCheck\main\RapidCheck\asset\9_on.png");
        Bitmap direct1off = new Bitmap(@"C:\Users\SoMa\Desktop\RapidCheck\main\RapidCheck\asset\1_off.png");
        Bitmap direct2off = new Bitmap(@"C:\Users\SoMa\Desktop\RapidCheck\main\RapidCheck\asset\2_off.png");
        Bitmap direct3off = new Bitmap(@"C:\Users\SoMa\Desktop\RapidCheck\main\RapidCheck\asset\3_off.png");
        Bitmap direct4off = new Bitmap(@"C:\Users\SoMa\Desktop\RapidCheck\main\RapidCheck\asset\4_off.png");
        //Bitmap direct5off = new Bitmap(@"C:\Users\SoMa\Desktop\RapidCheck\main\RapidCheck\asset_\5off.png");
        Bitmap direct6off = new Bitmap(@"C:\Users\SoMa\Desktop\RapidCheck\main\RapidCheck\asset\6_off.png");
        Bitmap direct7off = new Bitmap(@"C:\Users\SoMa\Desktop\RapidCheck\main\RapidCheck\asset\7_off.png");
        Bitmap direct8off = new Bitmap(@"C:\Users\SoMa\Desktop\RapidCheck\main\RapidCheck\asset\8_off.png");
        Bitmap direct9off = new Bitmap(@"C:\Users\SoMa\Desktop\RapidCheck\main\RapidCheck\asset\9_off.png");
        private void setTogleDirectionBtn()
        {
            pictureBoxDirection1.Image = direct1off;
            pictureBoxDirection2.Image = direct2off;
            pictureBoxDirection3.Image = direct3off;
            pictureBoxDirection4.Image = direct4off;
            //pictureBoxDirection5.Image = direct5off;
            pictureBoxDirection6.Image = direct6off;
            pictureBoxDirection7.Image = direct7off;
            pictureBoxDirection8.Image = direct8off;
            pictureBoxDirection9.Image = direct9off;
            switch (directionPosition)
            {
                case 1: pictureBoxDirection1.Image = direct1on; break;
                case 2: pictureBoxDirection2.Image = direct2on; break;
                case 3: pictureBoxDirection3.Image = direct3on; break;
                case 4: pictureBoxDirection4.Image = direct4on; break;
                //case 5: pictureBoxDirection5.Image = direct5on; break;
                case 6: pictureBoxDirection6.Image = direct6on; break;
                case 7: pictureBoxDirection7.Image = direct7on; break;
                case 8: pictureBoxDirection8.Image = direct8on; break;
                case 9: pictureBoxDirection9.Image = direct9on; break;
            }
        }
        private void replay()
        {
            startBtn.Text = "Start";
            pictureBoxStart.Image = overlayStart;
            //target
            if (!searchPeople && !searchCar)
            {
                MessageBox.Show("검색 대상(차, 사람)을 선택하세요.");
                return;
            }
            else if (searchPeople && !searchCar)
            {
                rapidCheck.conditionTarget = "and classId = 1";
            }
            else if (!searchPeople && searchCar)
            {
                rapidCheck.conditionTarget = "and classId = 0";
            }
            else
            {
                rapidCheck.conditionTarget = "";
            }
            //color
            double colorThres = 0.25;
            switch (colorPosition)
            {
                case 0:
                    rapidCheck.conditionColor = "and color0 > " + colorThres;
                    break;
                case 1:
                    rapidCheck.conditionColor = "and color1 > " + colorThres;
                    break;
                case 2:
                    rapidCheck.conditionColor = "and color2 > " + colorThres;
                    break;
                case 3:
                    rapidCheck.conditionColor = "and color3 + color4 > " + colorThres;
                    break;
                case 4:
                    rapidCheck.conditionColor = "and color5 + color6 > " + colorThres;
                    break;
                case 5:
                    rapidCheck.conditionColor = "and color7 > " + colorThres;
                    break;
                case 6:
                    rapidCheck.conditionColor = "and color8 + color9 > " + colorThres;
                    break;
                case 7:
                    rapidCheck.conditionColor = "and color10 + color11 > " + colorThres;
                    break;
                case 8:
                    rapidCheck.conditionColor = "and color12 > " + colorThres;
                    break;
                case 9:
                    rapidCheck.conditionColor = "and color13 > " + colorThres;
                    break;
                default:
                    rapidCheck.conditionColor = "";
                    break;
            }
            //direction
            double directionThres = 0.2, speedThres = 0.2;
            switch (directionPosition)
            {
                case 1:
                    rapidCheck.conditionDirection = "and direction13 + direction14  > " + directionThres;
                    break;
                case 2:
                    rapidCheck.conditionDirection = "and direction11 + direction12 > " + directionThres;
                    break;
                case 3:
                    rapidCheck.conditionDirection = "and direction9 + direction10 > " + directionThres;
                    break;
                case 4:

                    rapidCheck.conditionDirection = "and direction0 + direction15 > " + directionThres;
                    break;
                case 5:
                    //ObjReset();
                    break;
                case 6:
                    rapidCheck.conditionDirection = "and direction7 + direction8 > " + directionThres;
                    break;
                case 7:
                    rapidCheck.conditionDirection = "and direction1 + direction2 > " + directionThres;
                    break;
                case 8:
                    rapidCheck.conditionDirection = "and direction3 + direction4  > " + directionThres;
                    break;
                case 9:
                    rapidCheck.conditionDirection = "and direction5 + direction6 > " + directionThres;
                    break;
                default:
                    rapidCheck.conditionDirection = "";
                    break;
            }
            rapidCheck.conditionDirection += " and speed > " + speedThres;
            overlayModule.Abort();
            Thread.Sleep(1);
            myRapidModule.Clear();
            myRapidModule.Add(rapidCheck.setFileterObjectidList); //filetering test
            myRapidModule.Add(rapidCheck.objectClustering);
            myRapidModule.Add(rapidCheck.buildOverlayOrderUsingCluster);
            myRapidModule.Add(rapidCheck.overlayLive);

            overlayModule = new Thread(() => rapidRun());
            overlayModule.Start();
        }
        //------------------------------초기화 EVENT------------------------------
        private void ObjReset()
        {
            overlayModule.Abort();
            Thread.Sleep(1);
            myRapidModule.Clear();
            myRapidModule.Add(rapidCheck.resetObjectidList);
            myRapidModule.Add(rapidCheck.objectClustering);
            myRapidModule.Add(rapidCheck.buildOverlayOrderUsingCluster);
            myRapidModule.Add(rapidCheck.overlayLive);
            overlayModule = new Thread(() => rapidRun());
            overlayModule.Start();
        }


        Bitmap fileImgMouseOver = new Bitmap(@"C:\Users\SoMa\Desktop\RapidCheck\main\RapidCheck\asset\file_mouseover@2x.png");
        private void pictureBoxHead_MouseHover(object sender, EventArgs e)
        {
            pictureBoxHead.Image = fileImgMouseOver;
            buttonReadFile.BackColor = Color.FromArgb(47, 125, 143);
        }
        Bitmap fileImgMouseLeave = new Bitmap(@"C:\Users\SoMa\Desktop\RapidCheck\main\RapidCheck\asset\file@2x.png");
        private void pictureBoxHead_MouseLeave(object sender, EventArgs e)
        {
            pictureBoxHead.Image = fileImgMouseLeave;
            buttonReadFile.BackColor = Color.FromArgb(76, 150, 173);
        }


        Bitmap videoSpeed1on  = new Bitmap(@"C:\Users\SoMa\Desktop\RapidCheck\main\RapidCheck\asset\ff1.png");
        Bitmap videoSpeed2on = new Bitmap(@"C:\Users\SoMa\Desktop\RapidCheck\main\RapidCheck\asset\play2.png");
        Bitmap videoSpeed4on = new Bitmap(@"C:\Users\SoMa\Desktop\RapidCheck\main\RapidCheck\asset\play4.png");
        Bitmap videoSpeed1off = new Bitmap(@"C:\Users\SoMa\Desktop\RapidCheck\main\RapidCheck\asset\ff1_off.png");
        Bitmap videoSpeed2off = new Bitmap(@"C:\Users\SoMa\Desktop\RapidCheck\main\RapidCheck\asset\ff2_off.png");
        Bitmap videoSpeed4off = new Bitmap(@"C:\Users\SoMa\Desktop\RapidCheck\main\RapidCheck\asset\ff4_off.png");
        private void pictureBoxSpeed1_Click(object sender, EventArgs e)
        {
            radioButtonX1.Checked = true;
            pictureBoxSpeed1.Image = videoSpeed1on;
            pictureBoxSpeed2.Image = videoSpeed2off;
            pictureBoxSpeed4.Image = videoSpeed4off;
        }

        private void pictureBoxSpeed2_Click(object sender, EventArgs e)
        {
            radioButtonX2.Checked = true;
            pictureBoxSpeed1.Image = videoSpeed1off;
            pictureBoxSpeed2.Image = videoSpeed2on;
            pictureBoxSpeed4.Image = videoSpeed4off;
        }

        private void pictureBoxSpeed4_Click(object sender, EventArgs e)
        {
            radioButtonX4.Checked = true;
            pictureBoxSpeed1.Image = videoSpeed1off;
            pictureBoxSpeed2.Image = videoSpeed2off;
            pictureBoxSpeed4.Image = videoSpeed4on;
        }

        private void trackBar2_ValueChanged(object sender, EventArgs e)
        {
            labelDensity.Text = trackBar2.Value.ToString();
            if(outputFrameNum == 1500) //임시적으로..
                rapidCheck.clusterNum = trackBar2.Value;
        }

        private void buttonTraining_Click(object sender, EventArgs e)
        {
            buttonTraining.Enabled = false;
            buttonTraining.BackColor = Color.Gray;
            panelWebbrowser.Dock = DockStyle.Left;
            panelWebbrowser.Width = 1440;
            textBoxTrainLog.Visible = true;

            string dir = @"..\..\..\..\Detection_Engine\";
            System.IO.Directory.SetCurrentDirectory(dir);


            new Thread(() => playTrainingTool()).Start();
        }
        private void playTrainingTool()
        {
            string pro = @"C:\Users\SoMa\Anaconda3\envs\venvJupyter\python.exe";
            string args = string.Format(@"training.py");
            var p = new System.Diagnostics.Process();
            p.StartInfo.FileName = pro;
            p.StartInfo.Arguments = args;

            //p.StartInfo.CreateNoWindow = true;
            p.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.UseShellExecute = false;
            p.OutputDataReceived += processOutputHandler;

            p.Start();
            p.BeginOutputReadLine();
            p.WaitForExit();
            int result = p.ExitCode;
            if (result == 0)
            {
                textBoxTrainLog.AppendText("\nDone.");
            }
            else
            {
                MessageBox.Show("Train ERROR");
            }
        }
        private void processOutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            if (outLine.Data != null)
            {
                textBoxTrainLog.AppendText(outLine.Data + "\n");
                textBoxTrainLog.SelectionStart = textBoxTrainLog.TextLength;
                textBoxTrainLog.ScrollToCaret();
            }
        }

        private void plotViewBar_Click(object sender, EventArgs e)
        {

        }
    }
}
