﻿using Newtonsoft.Json;

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using RueHelper.util;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using RobotpenGateway;
using RobotPenTestDll;

namespace RueHelper
{
    public partial class FormDraw : Form
    {
        private static log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public class Trail
        {
            public List<string> datas = new List<string>();
            public void Add(int cmd, int x, int y)
            {
                string r = cmd + "," + x + "," + y;
                datas.Add(r);
            }
            public void Clear()
            {
                datas.Clear();
            }
        }

        int screenWidth = Screen.PrimaryScreen.Bounds.Width;
        int screenHeight = Screen.PrimaryScreen.Bounds.Height;
        bool bBtn = false;                       //正在保存状态标志

        private int m_nDeviceW = 22015;
        private int m_nDeviceH = 17539;
        private bool bScreen = true;
        private bool bScreenO = false;
        private const int MAX = 100;
        private static int[] szStatus = new int[MAX];

        /// <summary>
        /// 轨迹数据的数组
        /// </summary>
        private Trail[] szTrail = new Trail[MAX];

        /// <summary>
        /// 每个学生的手写板画图对象
        /// </summary>
        private Bitmap[] szBMP = new Bitmap[MAX];

        /// <summary>
        /// 每个学生 最后落笔点
        /// </summary>
        private PointF[] m_szLastPoint = new PointF[MAX];

        private int m_padding = 30;

        private static RobotPenImages[] RECORD = new RobotPenImages[MAX];

        private bool m_bDrawing = false;
        private bool m_bDrawingOthers = false;
        private bool[] szDrawing = new bool[MAX];
        private PointF m_lastPoint;

        private static int nFlags = 0;
        private static int[] szFlags = new int[MAX];

        private PointF m_point;
        private int m_nPenStatus = 0;

        private string m_strVersion;
        private string m_strCustomNum;
        private string m_strClassNum;
        private string m_strDeviceNum;
        private string m_version_label;
        private string m_status_label;
        private string m_error_label;
        public string rid = "";
        
        public System.Timers.Timer t;
        public System.Timers.Timer t1;
        int inTimer = 0;
        public DateTime tm_create = DateTime.Now;

        private bool bRunning = false;
        private int m_index = -1;
        private bool m_dDrawing = false;
        private bool m_ShowHistoryImage = false;
        private string createtime = "";
        public delegate void InvokeVoteState(string context, PictureBox pic, PictureBox text, int i);
        public delegate void InvokeColumnState(string context, PictureBox pic);
        public delegate void InvokeDrawingEvent(ref PointF pos, int nCompress = 0);
        private Image panelImg = null;
        private string m_ImgDir = Application.StartupPath + "\\" + DateTime.Now.ToString("yyyyMMdd");
        public FormDraw()
        {
            InitializeComponent();
            this.DoubleBuffered = true;

            Log.Info("FormDraw.create");

            for (int i = 0; i < MAX; i++)
            {
                szTrail[i] = new Trail();
                if (RECORD[i] == null)
                {
                    RECORD[i] = new RobotPenImages();
                }
                    
            }

            //No xiti.id
            rid = Global.getSchoolID() + "-" + Global.getClassID() + "-" + DateTime.Now.ToString("yyyyMMddHHmmss");
            SetPanel();
            this.Height = screenHeight;
            this.Width = screenWidth;

            StartPosition = FormStartPosition.Manual;
            SetDesktopLocation(0, screenHeight - this.Height);
            if(panelImg == null)
            {
                panelImg = new Bitmap(panel1.Width, panel1.Height);
            }

            this.TopMost = true;
#if DEBUG
            this.TopMost = false;//PPTPractise
#endif
            this.Hide();
            labelName.Text = "";
            labelIndex.Text = "";
            init();
            openDevice();

            //清空界面
            Graphics grap = this.panel1.CreateGraphics();
            grap.Clear(this.BackColor);
            grap.Dispose();
        }
        public void StartExercise()
        {
            //进入ms模式
            //robotpenController.GetInstance()._Send(cmdId.WriteStart);//v1.0
            robotpenController.GetInstance()._Send(cmdId.WriteBegin);//v1.1

            //TODO: 重置发起时间
            createtime = DateTime.Now.ToString("yyyyMMddHHmmss");
            tm_create = DateTime.Now;
            rid = Global.getSchoolID() + "-" + Global.getClassID() + "-" + DateTime.Now.ToString("yyyyMMddHHmmss");
        }
        public void CloseMs()
        {
            robotpenController.GetInstance()._Send(cmdId.WriteEnd);
        }
        public static int[] getStatus()
        {
            return szStatus;
        }
        public void shutdown()
        {
            closeDevice();
            this.Dispose();
        }
        public void ShowHistoryImage(string path,int index)
        {
            m_ShowHistoryImage = true;
            StudentInfo si = Global.getUserInfoBySeat(index);
            if (si != null)
            {
                labelName.Text = si.Name;
                labelIndex.Text = index + "";
            }

            //清空界面
            Graphics grap = this.panel1.CreateGraphics();
            grap.Clear(this.BackColor);

            System.Drawing.Image originalImg = System.Drawing.Image.FromFile(path);
            System.Drawing.Rectangle destRect = new System.Drawing.Rectangle(new System.Drawing.Point(0, 0), new System.Drawing.Size(panel1.Width, panel1.Height));//目标位置
            System.Drawing.Rectangle origRect = new System.Drawing.Rectangle(new System.Drawing.Point(panel1.Left, panel1.Top), new System.Drawing.Size(panel1.Width, panel1.Height));//原图位置（默认从原图中截取的图片大小等于目标图片的大小）
            grap.DrawImage(originalImg, destRect, origRect, System.Drawing.GraphicsUnit.Pixel);
            grap.Dispose();
        }


        //add微信拍照图片适配绘制
        public static void DrawWeixinImg(string filename)
        {
            string path = Application.StartupPath + "\\" + DateTime.Now.ToString("yyyyMMdd") + @"\" + filename;
            //清空界面
            Bitmap oldBmp = new Bitmap(path);
            Image imgTemp = new Bitmap(1024, 768);
            Graphics grap = Graphics.FromImage(imgTemp);
            //grap.Clear(Color.FromArgb(0));
            System.Drawing.Image originalImg = oldBmp;
            int iW = originalImg.Width * 768 / originalImg.Height;
            int iH = 768;
            int iL = (int)((1024 - iW) / 2);
            if (originalImg != null)
            {
                System.Drawing.Rectangle destRect = new System.Drawing.Rectangle(new System.Drawing.Point(iL, 0), new System.Drawing.Size(iW, iH));//目标位置
                System.Drawing.Rectangle origRect = new System.Drawing.Rectangle(new System.Drawing.Point(0, 0), new System.Drawing.Size(originalImg.Width, originalImg.Height));//原图位置（默认从原图中截取的图片大小等于目标图片的大小）
                grap.DrawImage(originalImg, destRect, origRect, System.Drawing.GraphicsUnit.Pixel);
            }
            path = Application.StartupPath + "\\" + DateTime.Now.ToString("yyyyMMdd") + @"\Copy" + filename;
            if (!System.IO.File.Exists(path))
            {
                imgTemp.Save(path);
            }
            grap.Dispose();
        }


        public void ShowPenTrail(int index)
        {
            int seat = index + 1;
            this.Show();
            this.BringToFront();

            //if (m_index == index)
            //    return;
            m_ShowHistoryImage = false;

            StudentInfo si = Global.getUserInfoBySeat(seat);
            if(si!=null){
                labelName.Text = si.Name;
                labelIndex.Text = seat + "";
            }

            m_index = index;
            m_dDrawing = false;

            //清空界面
          //  Graphics grap = Graphics.FromImage(panelImg);
            Graphics grap = this.panel1.CreateGraphics();
            grap.Clear(this.BackColor);
            System.Drawing.Image originalImg = szBMP[index];
            if(originalImg != null)
            {
                System.Drawing.Rectangle destRect = new System.Drawing.Rectangle(new System.Drawing.Point(0, 0), new System.Drawing.Size(panel1.Width, panel1.Height));//目标位置
                System.Drawing.Rectangle origRect = new System.Drawing.Rectangle(new System.Drawing.Point(panel1.Left, panel1.Top), new System.Drawing.Size(panel1.Width, panel1.Height));//原图位置（默认从原图中截取的图片大小等于目标图片的大小）
                grap.DrawImage(originalImg, destRect, origRect, System.Drawing.GraphicsUnit.Pixel);
            }
            grap.Dispose();
            this.panel1.BackgroundImage = panelImg;
        }

        #region panel上画
        private void doDrawing(ref PointF pos, int nCompress = 0)
        {
            //Graphics grap0 = this.CreateGraphics();//form区域
            Graphics grap = this.panel1.CreateGraphics();//panel1区域
            grap.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            //Debug.Write("画画中");
            grap.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            grap.DrawLine(new Pen(Color.Black, 2), m_lastPoint, pos);
            m_lastPoint = pos;
            grap.Dispose();
        }
        private void doDrawingEvent(ref PointF pos, int nCompress = 0)
        {
            if (this.InvokeRequired)
            {
                InvokeDrawingEvent cb = new InvokeDrawingEvent(doDrawingEvent);
                this.Invoke(cb, new object[] { pos,nCompress });
                return;
            }

            Graphics grap = this.panel1.CreateGraphics();//panel1区域
            grap.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            //Debug.Write("画画中");
            grap.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            grap.DrawLine(new Pen(Color.Black, 2), m_lastPoint, pos);
            m_lastPoint = pos;
            grap.Dispose();
            this.Show();
            this.BringToFront();
        }
        #endregion

        #region 内存中画所有人的轨迹
        private void doDrawingX(int index, ref PointF pos, int nCompress = 0)
        {
            try
            {
                int width = this.Width;
                //int width = this.panel1.Width;
                int height = this.Height;

                Graphics grap = null;
                if (szBMP[index] == null)
                {
                    szBMP[index] = new Bitmap(width, height);
                    grap = Graphics.FromImage(szBMP[index]);
                    grap.FillRectangle(Brushes.White, 0, 0, width, height);//新建的时候，设置背景色
                    //SolidBrush b = new SolidBrush(Color.White);//这里修改颜色
                    //grap.FillRectangle(b, 0, 0, 300, 300);
                }
                else
                {
                    grap = Graphics.FromImage(szBMP[index]);
                }

                grap.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                grap.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

                //内存中做图是全屏的，所以需要偏移x
                PointF f1 = m_szLastPoint[index];
                PointF f2 = pos;
                f1.X += (this.Width - this.panel1.Width) / 2;
                f2.X += (this.Width - this.panel1.Width) / 2;

                grap.DrawLine(new Pen(Color.Black, 2), f1, f2);
                m_szLastPoint[index] = pos;
                grap.Dispose();
            }catch(Exception e){
                Log.Error("doDrawingX: "+e.Message);
            }
                
        }
        #endregion

        private void Form2_FormClosing(object sender, FormClosingEventArgs e)
        {
            Win32.AnimateWindow(this.Handle, 200, Win32.AW_SLIDE | Win32.AW_HIDE | Win32.AW_BLEND);
        }
                                        
        #region 设备初始化与关闭
        public void init()
        {

            robotpenController.GetInstance()._ConnectInitialize(0, false, IntPtr.Zero);

            //robotpenController.GetInstance()._ConnectInitialize(0, IntPtr.Zero);//v0.1
            robotpenController.GetInstance().gateWayStatusEvt += Form1_gateWayStatusEvt;
            robotpenController.GetInstance().nodeStatusEvt += Form1_nodeStatusEvt;      //子节点设备状态改变事件
            robotpenController.GetInstance().gatewayErrorEvt += Form1_gatewayErrorEvt;  // 设备错误事件
            robotpenController.GetInstance().exitVotePatternEvt += Form1_exitVotePatternEvt; // 退出投票模式
            robotpenController.GetInstance().gatewatVersionEvt += Form1_gatewatVersionEvt;
            //robotpenController.GetInstance().bigDataReportEvt += Form1_bigDataReportEvt;//v0.1
            robotpenController.GetInstance().onlineStatusEvt += Form1_onlineStatusEvt;

            RobotpenGateway.robotpenController.returnPointData date = null;
            date = new RobotpenGateway.robotpenController.returnPointData(Form1_bigDataReportEvt);
            robotpenController.GetInstance().initDeletgate(ref date);

        }

        public void openDevice()
        {
            if (!bRunning)
            {
                robotpenController.GetInstance()._ConnectOpen();
                bRunning = true;
            }
            //进入ms模式
            //robotpenController.GetInstance()._Send(cmdId.WriteStart);//v1.0
            //robotpenController.GetInstance()._Send(cmdId.WriteBegin);//v1.1
            robotpenController.GetInstance()._Send(cmdId.WriteEnd);
        }
        public void closeDevice()
        {
            if (bRunning)
            {
                robotpenController.GetInstance()._ConnectDispose();
                bRunning = false;
            }
        }
        #endregion


        #region 设备状态

        // 收到网关状态改变事件
        private void Form1_gateWayStatusEvt(GATEWAY_STATUS gwS)
        {
            string strStatus = string.Empty;
            switch (gwS)
            {
                case GATEWAY_STATUS.NEBULA_STATUS_OFFLINE:
                    {
                        strStatus = "NEBULA_STATUS_OFFLINE";
                    } break;
                case GATEWAY_STATUS.NEBULA_STATUS_STANDBY:
                    {
                        strStatus = "NEBULA_STATUS_STANDBY";
                    } break;
                case GATEWAY_STATUS.NEBULA_STATUS_VOTE:
                    {
                        strStatus = "NEBULA_STATUS_VOTE";
                    } break;
                case GATEWAY_STATUS.NEBULA_STATUS_MASSDATA:
                    {
                        strStatus = "NEBULA_STATUS_MASSDATA";
                    } break;
                case GATEWAY_STATUS.NEBULA_STATUS_END:
                    {
                        strStatus = "NEBULA_STATUS_END";
                    } break;
                default:
                    {
                        strStatus = "UNKNOW";
                    }
                    break;
            }
            CallDelegate(strStatus);
        }


        // 子节点设备状态改变事件
        private void Form1_nodeStatusEvt(NODE_STATUS ns)
        {
            string strStatus = string.Empty;
            switch (ns)
            {
                case NODE_STATUS.DEVICE_POWER_OFF:
                    {
                        strStatus = "DEVICE_POWER_OFF";
                    }
                    break;
                case NODE_STATUS.DEVICE_STANDBY:
                    {
                        strStatus = "DEVICE_STANDBY";
                    }
                    break;
                case NODE_STATUS.DEVICE_INIT_BTN:
                    {
                        strStatus = "DEVICE_INIT_BTN";
                    }
                    break;
                case NODE_STATUS.DEVICE_OFFLINE:
                    {
                        strStatus = "DEVICE_OFFLINE";
                    }
                    break;
                case NODE_STATUS.DEVICE_ACTIVE:
                    {
                        strStatus = "DEVICE_ACTIVE";
                    }
                    break;
                case NODE_STATUS.DEVICE_LOW_POWER_ACTIVE:
                    {
                        strStatus = "DEVICE_LOW_POWER_ACTIVE";
                    }
                    break;
                case NODE_STATUS.DEVICE_OTA_MODE:
                    {
                        strStatus = "DEVICE_OTA_MODE";
                    }
                    break;
                case NODE_STATUS.DEVICE_OTA_WAIT_SWITCH:
                    {
                        strStatus = "DEVICE_OTA_WAIT_SWITCH";
                    }
                    break;
                case NODE_STATUS.DEVICE_DFU_MODE:
                    {
                        strStatus = "DEVICE_DFU_MODE";
                    }
                    break;
                case NODE_STATUS.DEVICE_TRYING_POWER_OFF:
                    {
                        strStatus = "DEVICE_TRYING_POWER_OFF";
                    }
                    break;
                case NODE_STATUS.DEVICE_FINISHED_PRODUCT_TEST:
                    {
                        strStatus = "DEVICE_FINISHED_PRODUCT_TEST";
                    }
                    break;
                case NODE_STATUS.DEVICE_SYNC_MODE:
                    {
                        strStatus = "DEVICE_SYNC_MODE";
                    }
                    break;
                //case NODE_STATUS.DEVICE_SEMI_FINISHED_PRODUCT_TEST:
                //    {
                //        strStatus = "DEVICE_SEMI_FINISHED_PRODUCT_TEST";
                //    }
                //    break;
                default:
                    {
                        strStatus = "UNKNOW";
                    }
                    break;
            }
            CallDelegate(strStatus);
        }

        // 设备错误事件
        private void Form1_gatewayErrorEvt(NEBULA_ERROR errorCode)
        {
            string strStatus = string.Empty;
            switch (errorCode)
            {
                case NEBULA_ERROR.ERROR_NONE:
                    {
                        strStatus = "ERROR_NONE";
                    }
                    break;
                case NEBULA_ERROR.ERROR_FLOW_NUM:
                    {
                        strStatus = "ERROR_FLOW_NUM";
                    }
                    break;
                case NEBULA_ERROR.ERROR_FW_LEN:
                    {
                        strStatus = "ERROR_FW_LEN";
                    }
                    break;
                case NEBULA_ERROR.ERROR_FW_CHECKSUM:
                    {
                        strStatus = "ERROR_FW_CHECKSUM";
                    }
                    break;
                case NEBULA_ERROR.ERROR_STATUS:
                    {
                        strStatus = "ERROR_STATUS";
                    }
                    break;
                case NEBULA_ERROR.ERROR_VERSION:
                    {
                        strStatus = "ERROR_VERSION";
                    }
                    break;
                case NEBULA_ERROR.ERROR_NAME_CONTENT:
                    {
                        strStatus = "ERROR_NAME_CONTENT";
                    }
                    break;
                case NEBULA_ERROR.ERROR_NO_NOTE:
                    {
                        strStatus = "ERROR_NO_NOTE";
                    }
                    break;
                default:
                    {
                        strStatus = "UNKNOW ERROR";
                    }
                    break;
            }
            CallDelegate(strStatus);
        }

        // 设备版本号 
        private void Form1_gatewatVersionEvt(string strVersion, byte bCustomNum, byte bClassNum, byte bDeviceNum)
        {
            m_strVersion = strVersion;
            m_strCustomNum = bCustomNum.ToString();
            m_strClassNum = bClassNum.ToString();
            m_strDeviceNum = bDeviceNum.ToString();
        }
        #endregion

        #region 所有学生的画笔轨迹数据上报
        private void Form1_bigDataReportEvt(byte bIndex, byte bPenStatus, short bx, short by, short bPress)
        {
            int nPenStatus = Convert.ToInt32(bPress);
            int nIndex = Convert.ToInt32(bIndex);
            if (nIndex >= szTrail.Length)
                return;
            
            //szTrail[nIndex].Add(Convert.ToInt32(bPress), Convert.ToInt32(bx), Convert.ToInt32(by));

            //内存中实时画出该index对应的学生的画笔轨迹
            //Graphics[nIndex]
            PointF lastPoint = m_szLastPoint[nIndex];
            recvDataX(nIndex, Convert.ToInt32(bPress), Convert.ToInt32(bx), Convert.ToInt32(by), 0);

            //显示  当前选中学生的  实时的  画笔轨迹
            if (m_index >= 0 && m_index == nIndex && m_dDrawing == false && m_ShowHistoryImage == false)//
            {
                recvData(nPenStatus, Convert.ToInt32(bx), Convert.ToInt32(by), 0);
            }

            //更新实时状态
            if (nPenStatus == 0)  // 笔离开到板子
            {
                szStatus[nIndex] = 1;//等待输入
            }
            else
            {
                szStatus[nIndex] = 2;//正在输入
            }
        }

        #endregion

        // 设备在线状态
        private void Form1_onlineStatusEvt(int nIndex, bool bOnLine)
        {
            if (bOnLine)
            {
                szStatus[nIndex] = 1;
            }
            else
            {
                szStatus[nIndex] = 0;
            }
        }

        #region 退出投票模式
        private void Form1_exitVotePatternEvt(string strValue)
        {
            
        }

        #endregion
  
        // 更新设备状态Label
        public void CallDelegate(string param)
        {
            m_status_label = param;
        }

        // 错误label提示
        public void updateErrorLabel(string strErrorInfo)
        {
            m_error_label = strErrorInfo;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            openDevice();
        }

        private bool pointIsInvalid(int nPenStatus, ref PointF pointValue)
        {
            if ((m_point == pointValue) && (m_nPenStatus == nPenStatus))
                return false;
            m_point = pointValue;
            m_nPenStatus = nPenStatus;
            return true;
        }
        public void recvData(int nPenStatus, int x, int y, int nCompress)
        {
            PointF pointf;
            if (!bScreenO)
            {
                pointf = new PointF(y, m_nDeviceW - x);
            }
            else
            {
                pointf = new PointF(x, y);
            }

            //if (!pointIsInvalid(nPenStatus, ref pointf))
            //    return;

            if (nPenStatus == 0)  // 笔离开到板子
            {
                m_bDrawing = false;
                nFlags = 0;
            }
            else
            {
                if (nFlags == 0)
                {
                    nFlags = 1;
                    compressPoint(ref pointf);
                    onBeginDraw(ref pointf);
                }
                else
                {
                    compressPoint(ref pointf);
                    onTrackDraw(ref pointf);
                }
            }
        }
        public void recvDataX(int index, int nPenStatus, int x, int y, int nCompress)
        {
            PointF pointf;
            if (!bScreenO)
            {
                pointf = new PointF(y, m_nDeviceW - x);
            }
            else
            {
                pointf = new PointF(x, y);
            }
            if (nPenStatus == 0)  // 笔离开到板子
            {
                szFlags[index] = 0;
                szDrawing[index] = false;
            }
            else
            {
                if (szFlags[index] == 0)
                {
                    szFlags[index] = 1;
                    compressPoint(ref pointf);
                    onBeginDrawX(index, ref pointf);
                }
                else
                {
                    if (!bBtn)
                    {
                        compressPoint(ref pointf);
                        onTrackDrawX(index, ref pointf);
                    }                   
                }
            }
        }
        private double m_nCompress = 0;
        private void compressPoint(ref PointF point)
        {
            //Log.Info("compressPoint=" + point.ToString());/////////////////////
            int width = this.Width;
            if (width >= 1366)
                width = 1450;

            int nBordereW = this.Width - this.ClientRectangle.Width;
            if (bScreenO)  // 横屏
            {
                int nValidWidth = this.ClientRectangle.Width - nBordereW / 2;
                int nValidHeight = this.ClientRectangle.Height - nBordereW;
                m_nCompress = ((double)(22015) / nValidWidth);  // 设备与屏幕的宽比例
                // 计算高的比例
                double nNeedCanvasHeight = (double)(15359 / m_nCompress);
                if (nNeedCanvasHeight > nValidHeight)
                    m_nCompress = (double)(17539 / nValidHeight);
            }
            else   // 竖屏
            {
                int nValidWidth = this.ClientRectangle.Height - nBordereW / 2;
                int nValidHeight = width - nBordereW;
                m_nCompress = ((double)(22015) / nValidWidth);  // 设备与屏幕的宽比例
                // 计算高的比例
                double nNeedCanvasHeight = (double)(15359 / m_nCompress);
                if (nNeedCanvasHeight > nValidHeight)
                    m_nCompress = (double)(17539 / nValidHeight);
            }


            float nx = (float)(point.X / m_nCompress);
            float ny = (float)(point.Y / m_nCompress);
            point.X = nx + m_padding;
            point.Y = ny;

            //Log.Info("pointXY=(" + point.X + ", " + point.Y + "), nxy="+nx+","+ny);/////////////////////
            //Console.WriteLine("压缩后的数据为:{0} {1}", nx, ny);
        }

        #region 开始画，落点
        public void onBeginDraw(ref PointF p, int nCompress = 0)
        {
            m_bDrawing = true;
            m_lastPoint = p;
        }
        public void onBeginDrawX(int index , ref PointF p, int nCompress = 0)
        {
            szDrawing[index] = true;
            m_szLastPoint[index] = p;
        }
        #endregion

        #region 正在画，连线
        public void onTrackDraw(ref PointF p, int nCompress = 0)
        {
            if (!m_bDrawing)
                return;
            doDrawing(ref p, nCompress);
            //doDrawingEvent(ref p, nCompress);
        }
        public void onTrackDrawX(int index, ref PointF p, int nCompress = 0)
        {
            if (!szDrawing[index])
                return;
            doDrawingX(index, ref p, nCompress);
        }
        #endregion

        private void button1_Click_1(object sender, EventArgs e)
        {
            ShowPenTrail(4);
        }
        public void Clear()
        {
            for (int i = 0; i < MAX; i++)
            {
                szTrail[i].Clear();
                if (szBMP[i] != null)
                {
                    szBMP[i].Dispose();
                    szBMP[i] = null;
                    //Debug.Write(i + "清除");
                }
            }

            m_index = -1;

            //清空界面
            try
            {
                Graphics grap = this.panel1.CreateGraphics();
                grap.Clear(this.BackColor);
                grap.Dispose();
            }
            catch (Exception e)
            {

            }


        }

        public static void ClearRecord()
        {
            foreach (RobotPenImages obj in RECORD)
            {
                obj.grouplist.Clear();
            }
        }

        private void SetPanel()
        {
            int panelWidth = 540 + 2 * m_padding;
            if (this.screenWidth > 1500)//1680
                panelWidth = 732 + 2 * m_padding;
            else if (this.screenWidth > 1400)//1440;
                panelWidth = 630 + 2 * m_padding;

            int left = (this.screenWidth - panelWidth) / 2;
            this.panel1.Location = new System.Drawing.Point(left, 0);//panelLH
            this.panel1.Size = new System.Drawing.Size(panelWidth, this.screenHeight);
            this.panel1.BringToFront();

            Log.Info("screenWidth=" + screenWidth + ", panel.width=" + panelWidth + ", left=" + left);


            this.panel2.Location = new System.Drawing.Point(0, 0);//panelLH
        }
        
        static System.Drawing.Bitmap GetPart(string pPath, int pPartStartPointX, int pPartStartPointY, int pPartWidth, int pPartHeight, int pOrigStartPointX, int pOrigStartPointY)
        {
            System.Drawing.Image originalImg = System.Drawing.Image.FromFile(pPath);

            System.Drawing.Bitmap partImg = new System.Drawing.Bitmap(pPartWidth, pPartHeight);
            System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(partImg);
            System.Drawing.Rectangle destRect = new System.Drawing.Rectangle(new System.Drawing.Point(pPartStartPointX, pPartStartPointY), new System.Drawing.Size(pPartWidth, pPartHeight));//目标位置
            System.Drawing.Rectangle origRect = new System.Drawing.Rectangle(new System.Drawing.Point(pOrigStartPointX, pOrigStartPointY), new System.Drawing.Size(pPartWidth, pPartHeight));//原图位置（默认从原图中截取的图片大小等于目标图片的大小）

            graphics.DrawImage(originalImg, destRect, origRect, System.Drawing.GraphicsUnit.Pixel);

            return partImg;
        }

        public void SaveImages()
        {
            string imgDir = Application.StartupPath + "\\" + DateTime.Now.ToString("yyyyMMdd");
            if (!Directory.Exists(imgDir))
                Directory.CreateDirectory(imgDir);

            bBtn = true;


            for (int i = 0; i < szBMP.Length; i++)
            {
                if (szBMP[i] != null)
                {
                   // string imgName = i + "_" + DateTime.Now.ToString("HHmmss") + ".jpg";
                    string imgName = Global.getSchoolID() + "_" + Global.getClassID() + "_" +  i + "_" + DateTime.Now.ToString("HHmmss") + ".jpg";
                    string imgPath = imgDir + "\\" + imgName;

                    szBMP[i].Save(imgPath);

                    RobotPenImageItem item = new RobotPenImageItem(DateTime.Now.ToString("HH:mm"), imgName);
                    RobotPenImageGroup group = new RobotPenImageGroup();
                    group.imglist.Add(item);
                    RECORD[i].grouplist.Add(group);
                }
            }

            bBtn = false;
        }
        public string SaveImages(int index)
        {
            if (!Directory.Exists(m_ImgDir))
                Directory.CreateDirectory(m_ImgDir);
            string imgName = index + "_" + DateTime.Now.ToString("HHmmss") + ".jpg";
            string imgPath = m_ImgDir + "\\" + imgName;

            bBtn = true;

            for (int i = 0; i < szBMP.Length; i++)
            {
                if (szBMP[i] != null && i == index)
                {
                    szBMP[i].Save(imgPath);
                    ////此处不保存图片的缓存中，因为“返回”或“重置”会自动保存
                    //RobotPenImageItem item = new RobotPenImageItem(DateTime.Now.ToString("HH:mm"), imgName);
                    //RobotPenImageGroup group = new RobotPenImageGroup();
                    //group.imglist.Add(item);
                    //RECORD[index].grouplist.Add(group);
                }
            }

            bBtn = false;
            return imgPath;
        }
        public string GetImages(int index)
        {
            RobotPenImages r = RECORD[index];
            return r.toJson();
        }
        public string UpdateImages()
        {
            DateTime tmnow = DateTime.Now;
            string result = "";
            int nsec = Util.getTimeDiff_Second(tm_create);
            int nUpload = 0;
            for(int i=0; i<RECORD.Length; i++)
            {
                RobotPenImages img = RECORD[i];
                if (img != null)
                {
                    int seat = i+1;
                    int uid = Global.getUidBySeat(seat);
                    string names = "";
                    foreach(RobotPenImageGroup group in img.grouplist)
                    {
                        if(group.status == 0)
                        {
                            //TODO: upload
                            foreach(RobotPenImageItem item in group.imglist)
                            {
                                names += (names.Length > 0 ? "|" : "") + item.imgName;
                                string imgPath = m_ImgDir + "\\" + item.imgName;
                                Common.uploadPicture(imgPath);
                                group.status = 1;
                                nUpload++;
                            }
                        }
                    }
                    if(names.Length > 0)
                    {
                        string result_item = uid + ":" + names;
                        result += (result.Length > 0 ? ";" : "") + result_item;
                    }
                }
            }

            if (nUpload > 0)
            {
                Common.uploadRobortPenEvent(rid, createtime, result, nsec);//同步到本地服务和云服务器
            }
            return "";
        }
    }

}