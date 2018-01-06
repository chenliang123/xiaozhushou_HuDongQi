﻿using Microsoft.Win32;
using RueHelper.model;
using RueHelper.util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RueHelper
{
    public partial class Form1 : Form
    {
        private static log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool ReleaseCapture();
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool SendMessage(IntPtr hwnd, int wMsg, int wParam, int lParam);
        public const int WM_SYSCOMMAND = 0x0112;
        public const int SC_MOVE = 0xF010;
        public const int HTCAPTION = 0x0002;


        private RueSqlite m_db = new RueSqlite();
        public delegate void InvokeLoadNofityForm(string msg, int bHide, int seconds);

        ServiceHost m_Host;
        Httpd m_HttpdComet = null;

        MovingDisk mdisk = new MovingDisk();
        public static Form12 f12;
        public static FormNotify fNotify;
        public static FormConfig fConfig;
        public static bool bFormNotifyClosed;
        List<string> strList = new List<string>();

        Thread thread_comet = null;
        Thread thread_Manage = null;
        Thread thread_DeviceStatusCheck = null;
        
        private bool isExit = false;

        private Point m_MousePoint;
        private Point m_LastPoint;


        public Form1()
        {
            InitializeComponent();
            Log.Info("F1_1 set notifyIcon");

            //show in taskbar
            {
                this.ShowInTaskbar = false;
                this.notifyIcon1.Visible = true;//在通知区显示Form的Icon
                this.WindowState = FormWindowState.Minimized;
            }

            //if (Global.isWithCamera())
            //{
            //    this.toolStripMenuItem5.Visible = true;
            //}
            //else
            //{
            //    this.toolStripMenuItem5.Visible = false;
            //}

            FileInfo fi = new FileInfo(Application.StartupPath+"\\如e小助手.exe");
            string lasttime = fi.LastWriteTime.ToString("yyyyMMdd HHmmss");
            string MMdd = fi.LastWriteTime.ToString("MMdd");
            string version = GetAssembly(typeof(System.Reflection.AssemblyVersionAttribute));

            string[] szV = version.Split('.');
            string version_1 = szV[0] + "." + szV[1] + "." + szV[2] + "." + MMdd;
            this.Text = "如e小助手 v" + version;
            this.labelAbout.Text = "关于 如e小助手(v" + version_1 + ")";

            //设置自启动
            Log.Info("F1_2 set autorun");
            SetAutoRun(Global.getAutorun());

            //清除历史文件夹
            Log.Info("F1_3 remove historyDir");
            RemoveHistoryDir();      

            //获取学校和班级的参数
            int schoolid = Global.getSchoolID();
            int classid = Global.getClassID();
            Log.Info("F1_4 get parameters: schoolid=" + schoolid+", classid="+classid);
            string assistanturl = Global.url_assistant;

            Log.Info("F1_5 get class");
            Classes c = m_db.getClassById(classid);

            if (c != null)
            {
                Global.setClassID(c.id);
                Global.setClassname(c.name);
                Global.setGrade(c.grade);
            }

            //更新班级的接收机ID
            Thread th = new Thread(delegate()
            {
                Log.Info("F1_6 setPCIP/syncHDId");
                Thread.Sleep(3000);
                setHD_PcIp();

                string hdid = Common.getHDID();
                if (hdid.Length > 0)
                    Common.uploadHDBind();
            });
            th.Start();

            //上传历史数据
            Thread thOfflineProcess = new Thread(delegate()
            {
                Log.Info("F1_7 syncOfflineData");
                OfflineProcessor.UploadHistoryData();
            });
            thOfflineProcess.Start();
        }

        /// <summary>
        /// 删除历史文件夹
        /// </summary>
        private void RemoveHistoryDir()
        {
            DirectoryInfo folder = new DirectoryInfo(Application.StartupPath);

            foreach (DirectoryInfo dirInfo in folder.GetDirectories())//FileInfo  folder.GetFiles("*.txt")
            {
                if (dirInfo.Name.Length==8)
                {
                    try
                    {
                        int day = Util.toInt(dirInfo.Name);
                        //int dayNow = Util.toInt(DateTime.Now.ToString("yyyyMMdd"));
                        //if(day<dayNow)
                        //{
                        //    dirInfo.Delete(true);
                        //    Log.Info("remove history directory: " + dirInfo.Name);
                        //}
                        bool bNotDelete = false;
                        for (int i = 0; i < 7; i++)
                        {
                            int _day = Util.toInt(DateTime.Now.AddDays(0 - i).ToString("yyyyMMdd"));
                            if (_day == day)
                            {
                                bNotDelete = true;
                                break;
                            }
                        }
                        if(!bNotDelete)
                        {
                            dirInfo.Delete(true);
                            Log.Info("remove history directory: " + dirInfo.Name);
                        }
                    }catch(Exception e){
                        Log.Info("remove history directory exception. " + dirInfo.Name);
                    }
                }
            }
        }
        public static void SelectPPT(string filepath)
        {
            if (f12 != null)
            {
                Log.Info("Form1.SelectPPT.addFile: " + filepath);
                f12.AddFile(filepath, false);
            }
            else
            {
                Log.Info("Form1.SelectPPT. new Form12()/addFile: " + filepath);
                MovingDisk mdisk = new MovingDisk();
                List<string> strList = new List<string>();
                try
                {
                    DriveInfo[] drives = mdisk.GetUSBDiskList();
                    foreach (DriveInfo drive in drives)
                    {
                        if ((drive.DriveType == DriveType.Removable) && !drive.Name.Substring(0, 1).Equals("A"))
                        {
                            try
                            {
                                if (!strList.Contains(drive.Name + "|" + drive.VolumeLabel + "|" + drive.RootDirectory))
                                {
                                    strList.Add(drive.Name + "|" + drive.VolumeLabel + "|" + drive.RootDirectory);
                                }
                            }
                            catch (Exception ex)
                            {

                            }
                        }
                    }
                    f12 = new Form12(strList);
                    f12.AddFile(filepath, false);

                    f12.ShowInTaskbar = false;
                    f12.WindowState = FormWindowState.Minimized;
                    f12.Hide();
                    f12.Visible = false;
                }
                catch (Exception ex)
                {
                    string ss = ex.Message;
                }
            }
        }

        public static void DeleteFile(string filename)
        {
            if (f12 != null)
            {
                f12.DeleteFile(filename);
            }
        }

        protected override void WndProc(ref Message m)
        {
            int mess = mdisk.MessageResult(m);
            if (mess == 1)
            {
                try
                {
                    DriveInfo[] drives = mdisk.GetUSBDiskList();
                    foreach (DriveInfo drive in drives)
                    {
                        if ((drive.DriveType == DriveType.Removable) && !drive.Name.Substring(0, 1).Equals("A"))
                        {
                            try
                            {
                                if (!strList.Contains(drive.Name + "|" + drive.VolumeLabel + "|" + drive.RootDirectory))
                                {
                                    strList.Add(drive.Name + "|" + drive.VolumeLabel + "|" + drive.RootDirectory);
                                }
                            }
                            catch (Exception ex)
                            {

                            }
                        }
                    }

                    if (f12 == null)
                    {
                        f12 = new Form12(strList);
                    }
                    else
                    {
                        f12.UpdateForm(strList,f12.Visible);
                    }
                }
                catch(Exception ex)
                {
                    string ss = ex.Message;
                }
            }
            if (mess == 2)
            {
                if (f12 != null)
                {
                    List<string> removelist = new List<string>(strList.ToArray());
                    strList.Clear();
                    DriveInfo[] drives = mdisk.GetUSBDiskList();
                    int i = 0;
                    foreach (DriveInfo drive in drives)
                    {
                        if ((drive.DriveType == DriveType.Removable) && !drive.Name.Substring(0, 1).Equals("A"))
                        {
                            try
                            {
                                if (!strList.Contains(drive.Name + "|" + drive.VolumeLabel + "|" + drive.RootDirectory))
                                {
                                    strList.Add(drive.Name + "|" + drive.VolumeLabel + "|" + drive.RootDirectory);
                                }
                                DelStrList(drive.Name + "|" + drive.VolumeLabel + "|" + drive.RootDirectory, removelist);
                            }
                            catch (Exception ex)
                            {

                            }
                        }
                    }
                    f12.Hide();
                    f12.removeUSBFile(removelist);
                    f12.UpdateForm(strList,false);
                    
                }
            }
            base.WndProc(ref m);
        }

        public void loadConfigForm()
        {
            if (fConfig != null)
            {
                fConfig.Dispose();
                fConfig = null;
            }

            fConfig = new FormConfig();

            fConfig.ShowInTaskbar = true;
            fConfig.WindowState = FormWindowState.Normal;
            fConfig.Show();
        }

        public void loadNofityForm(string msg, int bHide, int seconds)
        {
            if (this.InvokeRequired)
            {
                InvokeLoadNofityForm cb = new InvokeLoadNofityForm(loadNofityForm);
                this.Invoke(cb, new object[] { msg, bHide, seconds });
                return;
            }
            string classname=Global.getClassname();
            if(Global.IsPublicClassroom())
            {
                classname = "公共教室";
            }
            string title = Global.getSchoolname() + " - " + classname + "";
            if (fNotify != null)
            {
                if (bHide == 1)
                {
                    fNotify.Hide();
                }
                else
                {
                    fNotify.updateForm(title, msg);
                    if (!FormNotify.m_PPTImgExporting)
                    {
                        fNotify.Show();
                    }
                }
            }
            else
            {
                fNotify = new FormNotify(title, msg, seconds);
                if (!FormNotify.m_PPTImgExporting)
                {
                    fNotify.Show();
                }
            }


        }
        private void DelStrList(string value,List<string> removelist)
        {
            for (int i = 0; i < removelist.Count; i++)
            {
                if (value != removelist[i].ToString())
                {
                    removelist.RemoveAt(i);
                }
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                EService service = new EService();
                m_Host = new ServiceHost(service);
                m_Host.Open();

                m_HttpdComet = new Httpd();
                thread_comet = new Thread(m_HttpdComet.run);
                thread_comet.Start();
                
                //////////////////////////////////////////////////////////////
                // 系统管理 发现IP地址变更后自动重启服务
                //////////////////////////////////////////////////////////////
                thread_Manage = new Thread(delegate()
                {
                    string ip1 = GetInternalIPList();
                    while (true)
                    {
                        string ip2 = GetInternalIPList();
                        if (ip1 != ip2)
                        {
                            Log.Info("ip1=" + ip1 + ". ip2=" + ip2);
                            ip1 = ip2;
                            restartHttpd();
                        }
                        Thread.Sleep(8000);
                    }
                });
                thread_Manage.Start();

                //////////////////////////////////////////////////////////////
                // 系统管理 发现IP地址变更后自动重启服务
                //////////////////////////////////////////////////////////////
                thread_DeviceStatusCheck = new Thread(Thread_CheckDeviceStatus);
                thread_DeviceStatusCheck.Start();
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                MessageBox.Show("小助手启动异常,请联系管理员","Warning!!!");
            }
        }
        private void Thread_CheckDeviceStatus()
        {
            int lastCode = 0;
            int nOK = 0;
            while (true)
            {
                bool bOK_360 = false;
                bool bOK_HD = false;
                string hdip = Global.getHDIP();
                string localip = GetInternalIPList();
                string[] szIP = localip.Split(',');
                if (szIP.Length == 1)
                {
                    string ip = szIP[0];
                    Log.Debug("Thread_CheckDeviceStatus1 ip=" + ip);
                    //string ip_4 = ip.Substring(ip.LastIndexOf(".") + 1);
                    if (ip == "172.18.201.3")
                    {
                        bOK_360 = true;
                    }
                }
                else
                {
                    foreach (string ip in szIP)
                    {
                        //string ip_4 = ip.Substring(ip.LastIndexOf(".") + 1);
                        if (ip == "172.18.201.3")
                        {
                            bOK_360 = true;
                            break;
                        }
                    }
                }

                //check 
                {
                    if (Common.HD_Test() == 1)
                    {
                        bOK_HD = true;
                    }
                    else
                    {
                        if (Common.HD_Test() == 1)
                        {
                            bOK_HD = true;
                        }
                    }
                }
                int code = 0;
                
                int Sec = 3 * 1000;
                if (!bOK_360)
                {
                    if (bOK_HD){
                        code = -1;
                    }else{
                        code = -3;
                    }
                }else{
                    if (bOK_HD)
                        code = 0;
                    else
                        code = -2;
                }
                //Log.Debug("Thread_CheckDeviceStatus code=" + code);
                int bHide = 0;
                if (code != lastCode)
                    bFormNotifyClosed = false;

                if (code == 0 && lastCode == 0 && nOK>=3)
                {
                    bHide = 1;
                }
                else
                {
                    bHide = 0;
                }

                if (!bFormNotifyClosed)
                {
                    if(code==-1){
                        //loadNofityForm("很抱歉，本机IP地址配置错误!\r\n请检查360wifi或无线路由是否正常！", bHide, 1000);
                        loadNofityForm("很抱歉，本机IP地址配置错误!", bHide, 1000);//\r\n请检查360wifi或无线路由是否正常！
                        nOK = 0;
                    }else if(code==-2){
                        loadNofityForm("很抱歉, 连接采集器失败!\r\n请检查采集器的参数是否正常！\r\n接收机IP：" + Global.getHDIP() + "\r\n本机IP：" + GetLocalIP_1(), bHide, 1000);
                        nOK = 0;
                    }else if(code==-3){
                        loadNofityForm("很抱歉，本机IP地址配置错误!", bHide, 1000);//\r\n采集器参数或网络异常！
                        nOK = 0;
                    }else{
                        loadNofityForm("\r\n亲爱的老师，可以开始上课啦!", bHide, 10);
                        nOK++;
                    }
                }
                lastCode = code;

                Thread.Sleep(Sec);
            }
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!isExit)//仅仅是关闭"关于"对话框
            {
                e.Cancel = true;

                WindowState = FormWindowState.Minimized;
                ShowInTaskbar = true;
                if (this.WindowState == FormWindowState.Minimized)
                {
                    this.ShowInTaskbar = false;
                    this.notifyIcon1.Visible = true;
                }
            }
        }
        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            Log.Info("Form1_FormClosed...");
        }

        private void notifyIcon1_DoubleClick(object sender, EventArgs e)
        {
            showUSBForm();
        }
        public void showUSBForm()
        {
            if (f12 != null)
            {
                try
                {
                    f12.Visible = true;
                    f12.ShowInTaskbar = true;
                    f12.WindowState = FormWindowState.Normal;
                    f12.Show();
                    f12.BringToFront();
                }
                catch (Exception e)
                {

                }
            }
            else
            {
                try
                {
                    DriveInfo[] drives = mdisk.GetUSBDiskList();
                    strList.Clear();
                    foreach (DriveInfo drive in drives)
                    {
                        if ((drive.DriveType == DriveType.Removable) && !drive.Name.Substring(0, 1).Equals("A"))
                        {
                            try
                            {
                                if (!strList.Contains(drive.Name + "|" + drive.VolumeLabel + "|" + drive.RootDirectory))
                                {
                                    strList.Add(drive.Name + "|" + drive.VolumeLabel + "|" + drive.RootDirectory);
                                }
                            }
                            catch (Exception ex)
                            {

                            }
                        }
                    }
                    if (f12 == null)
                    {
                        f12 = new Form12(strList,true);
                        f12.Visible = true;
                        f12.ShowInTaskbar = true;
                        f12.WindowState = FormWindowState.Normal;
                        f12.Show();
                        f12.BringToFront();
                    }
                    else
                    {
                        f12.UpdateForm(strList, true);
                    }
                }
                catch (Exception ex)
                {
                    string ss = ex.Message;
                }
            }
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            this.Show();
            this.ShowInTaskbar = true;  
            this.WindowState = FormWindowState.Normal;
            this.BringToFront(); 
        }

        private void Exit()
        {
            try
            {
                MyPPT.clearImg();
            }
            catch (Exception e) { Log.Error(e.Message); }

            isExit = true;
            //2016-04-09
            Log.Info("From1.exit now...");
            try
            {
                if(m_db!=null)
                {
                    m_db.Close();
                    m_db = null;
                }
            }
            catch (Exception e){ Log.Error(e.Message);}

            this.timer_lessonOff.Enabled = false;
            this.ShowInTaskbar = false;
            this.notifyIcon1.Visible = false;

            try
            {
                
                Process p = Process.GetCurrentProcess();
                if (p != null)
                {
                    Log.Info("From1 KillProcess now. pid="+p.Id);
                    Thread.Sleep(1000);
                    p.Kill();
                }
            }
            catch (Exception e) { Log.Error(e.Message); }

            try
            {
                if (m_Host != null)
                {
                    m_Host.Close();
                    m_Host = null;
                }
            }
            catch (Exception e) { Log.Error(e.Message); }

            try
            {
                if (m_HttpdComet != null)
                {
                    m_HttpdComet.stop();
                    m_HttpdComet = null;
                }
            }
            catch (Exception e) { Log.Error(e.Message); }

            try
            {
                if (thread_comet != null)
                {
                    thread_comet.Abort();
                    thread_comet.Interrupt();
                }
            }
            catch (Exception e) { Log.Error(e.Message); }

            try
            {
                if (thread_Manage != null)
                {
                    thread_Manage.Abort();
                    thread_Manage.Interrupt();
                }
            }
            catch (Exception e) { Log.Error(e.Message); }

            try
            {
                if (thread_DeviceStatusCheck != null)
                {
                    thread_DeviceStatusCheck.Abort();
                    thread_DeviceStatusCheck.Interrupt();
                }
            }
            catch (Exception e) { Log.Error(e.Message); }

            try
            {
                if (fNotify != null)
                {
                    fNotify.Close();
                    fNotify = null;
                }
            }
            catch (Exception e) { Log.Error(e.Message); }

            try
            {
                if (f12 != null)
                {
                    f12.Close();
                    f12 = null;
                }
                Application.Exit();
            }
            catch (Exception e) { Log.Error(e.Message); }


            Log.Info("From1 exit over.");
        }
        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            Log.Info("Form1_quit");
            Exit();
        }
        private void toolStripMenuItem3_Click(object sender, EventArgs e)
        {
            showUSBForm();
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Minimized;
            ShowInTaskbar = true;
            if (this.WindowState == FormWindowState.Minimized) 
            {
                this.ShowInTaskbar = false; 
                this.notifyIcon1.Visible = true; 
            }
        }


        public void SetAutoRun(bool isAutoRun)
        {
            Log.Info("SetAutoRun " + Global.m_Exe + ", " + isAutoRun);
            //set APP path
            {
                RegistryKey regApppath = null;
                string exepath = System.Windows.Forms.Application.ExecutablePath;
                regApppath = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\" + Global.m_Exe, RegistryKeyPermissionCheck.ReadWriteSubTree, System.Security.AccessControl.RegistryRights.FullControl);
                if (regApppath == null)
                {
                    regApppath = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\" + Global.m_Exe);
                }
                if (regApppath != null)
                {
                    regApppath.SetValue("", exepath);
                    regApppath.SetValue("path", Application.StartupPath);
                    regApppath.Close();
                }
            }


            string filepath = Application.StartupPath + @"\" + Global.m_Exe;
            RegistryKey reg = null;
            RegistryKey reg1 = null;
            try
            {
                if (!System.IO.File.Exists(filepath))
                    throw new Exception("该文件不存在!");
                reg1 = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", RegistryKeyPermissionCheck.ReadWriteSubTree, System.Security.AccessControl.RegistryRights.FullControl);
                if (reg1 == null)
                {
                    reg1 = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run");
                }
                if (isAutoRun)
                {
                    reg1.SetValue(Global.m_Exe, filepath);
                }
                else
                {
                    reg1.SetValue(Global.m_Exe, false);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());  
            }
            finally
            {
                if (reg != null)
                    reg.Close();
            }
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("iexplore.exe", "http://www.skyeducation.cn/"); 
        }

       
        private string GetLocalIPAddress()
        {
            string strServerIP = "";
            try
            {
                System.Net.IPAddress[] addressList = Dns.GetHostEntry(Dns.GetHostName()).AddressList;
                for (int i = 0; i < addressList.Length;i++ )
                {
                    if(addressList[i].ToString().Contains("."))
                    {
                        if (addressList[i].ToString().Split('.')[0] == "192" || addressList[i].ToString().Split('.')[0] == "172" || addressList[i].ToString().Split('.')[0] == "10")
                            strServerIP = addressList[i].ToString();
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error("GetLocalIPAddress exception. " + e);
            }
            
            return strServerIP;
        }

        private void toolStripMenuItem3_Click_1(object sender, EventArgs e)
        {
            loadConfigForm();
        }
        private void reloadCfg()
        {
            restartHttpd();
            MessageBox.Show("同步成功！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Question);
        }
        private void restartHttpd()
        {
            Log.Info("restartHttpd_8989 now...");
            {
                if (m_HttpdComet != null)
                {
                    m_HttpdComet.stop();
                    m_HttpdComet = null;
                }
                if (thread_comet != null)
                {
                    thread_comet.Abort();
                    thread_comet.Interrupt();
                }
                m_HttpdComet = new Httpd();
                thread_comet = new Thread(m_HttpdComet.run);
                thread_comet.Start();
            }
            Log.Info("restartHttpd_8989 over...");
        }
        private void SaveInfo()
        {
            string ip = GetLocalIPAddress();
            string sid = Global.getSchoolID()+"";
            string cid = Global.getClassID() + "";
            string apiurl = Global.url_hd;
            string str = HTTPReq.HttpGet(apiurl + "action=appsync&ip=" + ip + "&classid=" + cid + "&schoolid=" + sid + "");
        }

        private delegate void EnableLabelCallBack();
        
        private string getUTC()
        {
            string utctime = DateTime.Now.ToUniversalTime().ToString("r");

            DateTime dtStart = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));
            TimeSpan toNow = DateTime.Now.Subtract(dtStart);
            long timeStamp = toNow.Ticks;
            timeStamp = long.Parse(timeStamp.ToString().Substring(0, timeStamp.ToString().Length - 7));

            string strHex = Convert.ToString(timeStamp, 16).ToUpper();
            return strHex;
        }

        /// <summary>
        /// 选择u盘文件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripMenuItem4_Click(object sender, EventArgs e)
        {
            showUSBForm();
        }
        private string GetInternalIPList()
        {
            string ips = "";
            //ArrayList iplist = new ArrayList();
            //IPHostEntry host;

            //try
            //{
            //    host = Dns.GetHostEntry(Dns.GetHostName());
            //    Log.Info(host.ToString());
            //    foreach (IPAddress ip in host.AddressList)
            //    {
            //        Log.Info(ip.ToString());
            //        if (ip.AddressFamily.ToString() == "InterNetwork")
            //        {
            //            string _ip = ip.ToString();
            //            if (_ip == "127.0.0.1")
            //                continue;
            //            ips += _ip + ",";
            //        }
            //    }
            //}
            //catch(Exception e)
            //{
            //    Log.Error(e.Message);
            //    ips = "172.18.201.3";
            //}
            ips = "172.18.201.3";
            
            return ips;
        }

        private void timer_lessonOff_Tick(object sender, EventArgs e)
        {
            int lessonid = Global.getLessonID();
            string timenow = DateTime.Now.ToString("HHmm");
            string timeoff = Global.getTimeOff();
            string timeon = Global.getTimeOn();
            int lessonIndex = Global.getLessonIndex();
            if (lessonid == 0)
                return;
            //if(Global.isLessonOff())
            //    return;
            if (timenow.CompareTo(timeoff) > 0)//Global.getTeacherID() > 0 && 
            {
                //过了下课时间
                CourseTime ct = m_db.getCourseTime(lessonIndex + 1);
                if (ct != null)
                {
                    if (timenow.CompareTo(ct.timeOn) > 0)
                    {
                        //Log.Info("过了下节课的上课时间 " + ct.timeOn + ", teacherid" + Global.getTeacherID());
                    }
                    else
                    {
                        int minDiff = getTimediff(timenow, ct.timeOn);
                        if (minDiff < 5 && !Global.isLessonOff())
                        {
                            Common.setLessonOff(0,"","");//自动下课
                            Log.Info("还没有到下节课的上课时间 " + ct.timeOn + ", teacherid" + Global.getTeacherID() + ", 执行下课");
                        }
                        else
                        {
                            Log.Info("还没有到下节课的上课时间 " + ct.timeOn + ", teacherid" + Global.getTeacherID());
                        }
                        //执行下课

                        //清空lesson信息

                        //是否需要自动取lesson信息
                    }
                }

            }
        }

        private int getTimediff(string hhmm1,string hhmm2)
        {
            int min1 = Int32.Parse(hhmm1.Substring(0,2))*60+Int32.Parse(hhmm1.Substring(2));
            int min2 = Int32.Parse(hhmm2.Substring(0,2))*60+Int32.Parse(hhmm2.Substring(2));

            return min2-min1;
        }

        public static void closeFormConfig()
        {
            if(fConfig!=null)
            {
                fConfig.Close();
                fConfig.Dispose();
                fConfig = null;
            }
        }
        public static void closeFormNotify()
        {
            if (fNotify != null)
            {
                bFormNotifyClosed = true;
                fNotify = null;
            }
        }

        private string GetAssembly(Type type)
        {
            if (type.ToString() == "System.Reflection.AssemblyVersionAttribute")
            {//程序集版本号，要用这个方法获取，无法用下边的方法获取，原因不知
                return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            }
            object[] attributes = System.Reflection.Assembly.GetExecutingAssembly().GetCustomAttributes(type, false);
            if (attributes.Length > 0)
            {
                if (type.ToString() == "System.Reflection.AssemblyCompanyAttribute")
                {
                    #region//公司
                    System.Reflection.AssemblyCompanyAttribute company = (System.Reflection.AssemblyCompanyAttribute)attributes[0];
                    if (company.Company != "")
                    {
                        return company.Company;
                    }
                    #endregion
                }
                else if (type.ToString() == "System.Reflection.AssemblyCopyrightAttribute")
                {
                    #region//版权
                    System.Reflection.AssemblyCopyrightAttribute company = (System.Reflection.AssemblyCopyrightAttribute)attributes[0];
                    if (company.Copyright != "")
                    {
                        return company.Copyright;
                    }
                    #endregion
                }
                else if (type.ToString() == "System.Reflection.AssemblyTitleAttribute")
                {
                    #region//标题
                    System.Reflection.AssemblyTitleAttribute company = (System.Reflection.AssemblyTitleAttribute)attributes[0];
                    if (company.Title != "")
                    {
                        return company.Title;
                    }
                    #endregion
                }
                else if (type.ToString() == "System.Reflection.AssemblyDescriptionAttribute")
                {
                    #region//备注
                    System.Reflection.AssemblyDescriptionAttribute company = (System.Reflection.AssemblyDescriptionAttribute)attributes[0];
                    if (company.Description != "")
                    {
                        return company.Description;
                    }
                    #endregion
                }
                else if (type.ToString() == "System.Reflection.AssemblyProductAttribute")
                {
                    #region//产品名称
                    System.Reflection.AssemblyProductAttribute company = (System.Reflection.AssemblyProductAttribute)attributes[0];
                    if (company.Product != "")
                    {
                        return company.Product;
                    }
                    #endregion
                }
                else if (type.ToString() == "System.Reflection.AssemblyTrademarkAttribute")
                {
                    #region//商标
                    System.Reflection.AssemblyTrademarkAttribute company = (System.Reflection.AssemblyTrademarkAttribute)attributes[0];
                    if (company.Trademark != "")
                    {
                        return company.Trademark;
                    }
                    #endregion
                }
                else if (type.ToString() == "System.Reflection.AssemblyConfigurationAttribute")
                {
                    #region//获取程序集配置信息，具体什么内容，不清楚
                    System.Reflection.AssemblyConfigurationAttribute company = (System.Reflection.AssemblyConfigurationAttribute)attributes[0];
                    if (company.Configuration != "")
                    {
                        return company.Configuration;
                    }
                    #endregion
                }
                else if (type.ToString() == "System.Reflection.AssemblyCultureAttribute")
                {
                    #region//获取属性化程序集支持的区域性，具体什么内容，不清楚
                    System.Reflection.AssemblyCultureAttribute company = (System.Reflection.AssemblyCultureAttribute)attributes[0];
                    if (company.Culture != "")
                    {
                        return company.Culture;
                    }
                    #endregion
                }
                else if (type.ToString() == "System.Reflection.AssemblyVersionAttribute")
                {
                    #region//程序集版本号
                    System.Reflection.AssemblyVersionAttribute company = (System.Reflection.AssemblyVersionAttribute)attributes[0];
                    if (company.Version != "")
                    {
                        return company.Version;
                    }
                    #endregion
                }
                else if (type.ToString() == "System.Reflection.AssemblyFileVersionAttribute")
                {
                    #region//文件版本号
                    System.Reflection.AssemblyFileVersionAttribute company = (System.Reflection.AssemblyFileVersionAttribute)attributes[0];
                    if (company.Version != "")
                    {
                        return company.Version;
                    }
                    #endregion
                }
                else if (type.ToString() == "System.Reflection.AssemblyInformationalVersionAttribute")
                {
                    #region//产品版本号
                    System.Reflection.AssemblyInformationalVersionAttribute company = (System.Reflection.AssemblyInformationalVersionAttribute)attributes[0];
                    if (company.InformationalVersion != "")
                    {
                        return company.InformationalVersion;
                    }
                    #endregion
                }
            }
            //如果没有  属性，或者  属性为一个空字符串，则返回 .exe 的名称  
            return System.IO.Path.GetFileNameWithoutExtension(System.Reflection.Assembly.GetExecutingAssembly().CodeBase);
        }

        public static void updateFormConfig()
        {
            if (fConfig != null)
            {
                fConfig.refresh();
            }
        }

        private string GetLocalIP_1()
        {
            ArrayList iplist = new ArrayList();
            IPHostEntry host;
            host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ipaddr in host.AddressList)
            {
                if (ipaddr.AddressFamily.ToString() == "InterNetwork")
                {
                    string ip = ipaddr.ToString();
                    if (ip == "127.0.0.1")
                        continue;

                    string ip_4 = ip.Substring(ip.LastIndexOf(".") + 1);
                    if (ip == "172.18.201.3")
                    {
                        return ip;
                    }
                }
            }
            return "";
        }

        private void ToolStripMenuItemCamera_Click(object sender, EventArgs e)
        {
            if (EService.fCamera!=null)
                EService.fCamera.Show();
        }

        private void Form1_MouseClick(object sender, MouseEventArgs e)
        {
            this.Hide();
        }

        private void minimizeForm(object sender, EventArgs e)
        {
            this.Hide();
        }

        private void minimizeChangeStatus(object sender, EventArgs e)
        {
            this.pictureBox3.Image = global::RueHelper.Properties.Resources.close3B;
        }

        private void minimizeChangeStatus2(object sender, EventArgs e)
        {
            this.pictureBox3.Image = global::RueHelper.Properties.Resources.close3A;
        }

        private void OnTitleMouseDown(object sender, MouseEventArgs e)
        {
            base.OnMouseDown(e);
            this.m_LastPoint = this.Location;
            this.m_MousePoint = this.PointToScreen(e.Location);

            ReleaseCapture();
            SendMessage(this.Handle, WM_SYSCOMMAND, SC_MOVE + HTCAPTION, 0);
        }

        private void OnTitleMouseUp(object sender, MouseEventArgs e)
        {
            //base.OnMouseMove(e);
            //if (e.Button == MouseButtons.Left)
            //{
            //    Point t = this.PointToScreen(e.Location);
            //    Point l = this.m_LastPoint;

            //    l.Offset(t.X - this.m_MousePoint.X, t.Y - this.m_MousePoint.Y);
            //    this.Location = l;
            //}

        }

        private void setHD_PcIp()
        {
            string ips = GetInternalIPList();
            string[] iplist = ips.Split(',');
            string pcip = "";
            foreach (string ip in iplist)
            {
                if (ip.StartsWith("172.18.20"))
                {
                    pcip = ip;
                    break;
                }
            }

            if (pcip.Length == 0)
            {
                return;
            }

            pcip = "172.18.201.3";

            string host = Global.getHDIP();
            String url = "http://P1/EduApi/hd.do?action=pcip.get&callback=CB&tm=P2";//
            String utctime = getUTC();
            url = url.Replace("P1", host);
            url = url.Replace("P2", utctime);
            url = url.Replace("P3", pcip);

            string resp = HTTPReq.HttpGet(url);
            Log.Info(url + ", ret=" + resp);
            if(resp.IndexOf(pcip)<0)
            {
                String url_set = "http://P1/EduApi/hd.do?action=pcip.set&callback=CB&ip=P2&tm=P3";//
                url_set = url_set.Replace("P1", host);
                url_set = url_set.Replace("P2", pcip);
                url_set = url_set.Replace("P3", utctime);
                string resp2 = HTTPReq.HttpGet(url_set);
                Log.Info(url_set + ", ret=" + resp2);
            }
        }

        private void toolStripMenuItem5_Click(object sender, EventArgs e)
        {
            if (Global.g_teacherPinying.Length > 0)
            {
                if (fConfig != null)
                {
                    fConfig.Dispose();
                    fConfig = null;
                }
                string toUrl = "http://sso.api.xueyiyun.com/login/teacherLogin?loginname=" + Global.g_teacherPinying + "&username=" + Global.g_teacherName + "&accessToken=" + Common.getAccessToken() + "&email=&phonenumber=&schoolPhaesId=1&subjectId=13&appid=163369";
                string explorePath = GetDefaultWebBrowserFilePath();
                System.Diagnostics.Process.Start(explorePath, toUrl);
            }
            else 
            {
                bFormNotifyClosed = true;
                loadNofityForm("\r\n请在Pad上选择教师后查看", 0, 10);
            }
                        
            //System.Diagnostics.Process.Start("iexplore.exe", toUrl);
        }
        public String GetDefaultWebBrowserFilePath()
        {
            string _BrowserKey1 = @"Software\Clients\StartmenuInternet\";
            string _BrowserKey2 = @"\shell\open\command";

            RegistryKey _RegistryKey = Registry.CurrentUser.OpenSubKey(_BrowserKey1, false);
            if (_RegistryKey == null)
                _RegistryKey = Registry.LocalMachine.OpenSubKey(_BrowserKey1, false);
            String _Result = _RegistryKey.GetValue("").ToString();
            _RegistryKey.Close();

            _RegistryKey = Registry.LocalMachine.OpenSubKey(_BrowserKey1 + _Result + _BrowserKey2);
            _Result = _RegistryKey.GetValue("").ToString();
            _RegistryKey.Close();

            if (_Result.Contains("\""))
            {
                _Result = _Result.TrimStart('"');
                _Result = _Result.Substring(0, _Result.IndexOf('"'));
            }
            return _Result;
        }
    }
}
