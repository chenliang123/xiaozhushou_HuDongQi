﻿using Microsoft.Office.Core;
using Microsoft.Win32;
using RueHelper.util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using OFFICECORE = Microsoft.Office.Core;
using POWERPOINT = Microsoft.Office.Interop.PowerPoint;
using PPT = Microsoft.Office.Interop.PowerPoint;
namespace RueHelper
{

    public class MyPPT
    {
        [DllImport("user32.dll", EntryPoint = "PostMessage")]
        public static extern int PostMessage(IntPtr hwnd, int wMsg, int wParam, int lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int SendMessage(IntPtr HWnd, uint Msg, int WParam, int LParam);

        [DllImport("User32.dll ", EntryPoint = "FindWindow")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);//关键方法  

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern int ShowWindow(IntPtr hwnd, int nCmdShow);//0 关闭窗口; 1 正常大小显示窗口; 2 最小化窗口; 3 最大化窗口

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowThreadProcessId(IntPtr hWnd, IntPtr ProcessId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("User32.dll ", EntryPoint = "FindWindow")]
        public static extern IntPtr GetWindowLong(IntPtr hWnd, int index);//关键方法  
        //GWL_EXSTYLE= (-20) 扩展窗口样式 　　
        //GWL_STYLE=(-16) 窗口样式 　　
        //GWL_WNDPROC= (-4) 该窗口的窗口函数的地址 　　
        //GWL_HINSTANCE= (-6) 拥有窗口的实例的句柄 　　
        //GWL_HWNDPARENT= (-8) 该窗口之父的句柄。不要用SetWindowWord来改变这个值 　　
        //GWL_ID= (-12) 对话框中一个子窗口的标识符 　　
        //GWL_USERDATA = (-21) 含义由应用程序规定 　　
        //DWL_DLGPROC = 4 这个窗口的对话框函数地址 　　
        //DWL_MSGRESULT = 0 在对话框函数中处理的一条消息返回的值 　　
        //DWL_USER = 8 含义由应用程序规定 

        [DllImport("User32.dll")]
        public static extern int SetWindowPos(IntPtr hwnd, int hWndInsertAfter, int x, int y, int cx, int cy, int wFlags);
        public const int HWND_BOTTOM = 1;//将窗口置于Z序的底部。如果参数hWnd标识了一个顶层窗口，则窗口失去顶级位置，并且被置在其他窗口的底部。
        public const int HWND_NOTOPMOST = -2;//将窗口置于所有非顶层窗口之上（即在所有顶层窗口之后）。如果窗口已经是非顶层窗口则该标志不起作用。
        public const int HWND_TOP = 0;//将窗口置于Z序的顶部。
        public const int HWND_TOPMOST = -1;//将窗口置于所有非顶层窗口之上。即使窗口未被激活窗口也将保持顶级位置。
        public const int SWP_NOSIZE = 0x1;
        public const int SWP_NOMOVE = 0x2;
        public const int SWP_SHOWWINDOW = 0x40;
        public const int SWP_NOACTIVATE = 0x0010;

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);  //导入为windows窗体设置焦点的方法

        //[DllImport("user32.dll")]
        //private static extern bool AttachThreadInput(double idAttach, double idAttachTo, bool fAttach);
        [DllImport("user32.dll")]
        private static extern IntPtr AttachThreadInput(IntPtr idAttach, IntPtr idAttachTo, int fAttach);

        [DllImport("user32.dll")]
        public static extern IntPtr SetFocus(IntPtr hWnd);//设定焦点

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetCurrentThreadId();        //引入API函数，函数作用：将光标限制在矩形区域内

        [DllImport("USER32.DLL")]
        public static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);  //导入模拟键盘的方法

        [DllImport("USER32.DLL")]
        private static extern int mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);


        const int MOUSEEVENTF_MOVE = 0x1;
        const int MOUSEEVENTF_LEFTDOWN = 0x2;
        const int MOUSEEVENTF_LEFTUP = 0x4;
        const int MOUSEEVENTF_RIGHTDOWN = 0x8;
        const int MOUSEEVENTF_RIGHTUP = 0x10;
        const int MOUSEEVENTF_MIDDLEDOWN = 0x20;
        const int MOUSEEVENTF_MIDDLEUP = 0x40;
        const int MOUSEEVENTF_WHEEL = 0x800;
        const int MOUSEEVENTF_ABSOLUTE = 0x8000;

        public const int WM_SYSCOMMAND = 0x112;
        public const int SC_MINIMIZE = 0xF020;
        public const int SC_MAXIMIZE = 0xF030;

        public const int SW_HIDE = 0;
        public const int SW_SHOWNORMAL = 1;
        public const int SW_SHOWMINIMIZED = 2;
        public const int SW_SHOWMAXIMIZED = 3;
        public const int SW_SHOWNOACTIVATE = 4;
        public const int SW_Minimize = 6;
        public const int SW_ShowNA = 8;
        public const int SW_RESTORE = 9;
        public const int SW_SHOWDEFAULT = 10;
        
        private static log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        /// /////////////////////////////////////////////////////////////////////

        POWERPOINT.Application objApp = null;
        POWERPOINT.Presentation objPresSet = null;
        POWERPOINT.SlideShowSettings objSSS;
        POWERPOINT.Slides objSlides;
        public bool bPageTurning = true;
        public int pageTotal = 0;
        public int pageCurrent = 0;
        public int pageLast = 0;
        public string filename = "";
        public string filepath = "";
        public static Hashtable g_fileImgStatus = new Hashtable();//缩略图生成
        public string urls = "";
        private PPT.Hyperlinks pptHyperlinks = null;

        int screenWidth = Screen.PrimaryScreen.Bounds.Width;
        int screenHeight = Screen.PrimaryScreen.Bounds.Height;
        public static bool bExporting = false;
        public bool PPTOpen(string path)
        {
            filepath = path;
            filename = Path.GetFileName(path);

            if (this.objApp != null)
            {
                return true;
            }

            if (Global.getPPTMaximize())
            {
                minisizeProc();
            }

            try
            {
                //TODO: 需要将Form12关闭否？
                if (Form1.f12 != null)
                {
                    Form1.f12.Hide();
                }

                //显示桌面
                //Type oleType = Type.GetTypeFromProgID("Shell.Application");
                //object oleObject = System.Activator.CreateInstance(oleType);
                //oleType.InvokeMember("ToggleDesktop", BindingFlags.InvokeMethod, null, oleObject, null);


                objApp = new POWERPOINT.Application();
                objPresSet = objApp.Presentations.Open(filepath, OFFICECORE.MsoTriState.msoFalse, OFFICECORE.MsoTriState.msoFalse, OFFICECORE.MsoTriState.msoFalse);

                objSlides = objPresSet.Slides;
                pageTotal = objPresSet.Slides.Count;

                objSSS = this.objPresSet.SlideShowSettings;
                objSSS.LoopUntilStopped = MsoTriState.msoTrue;
                objSSS.Run();

                //最大化当前窗口
                maximizePPTProc();

                if (pageCurrent > 0)
                {
                    //GotoPage(pageCurrent);
                }
                else
                {
                    pageCurrent = 1;
                    pageLast = 1;
                }
                Log.Info("PPTOpen ok.");
                return true;
            }
            catch (Exception ex)
            {
                int errorcode = 0;
                var w32ex = ex as Win32Exception;
                if (w32ex == null)
                {
                    w32ex = ex.InnerException as Win32Exception;
                }
                if (w32ex != null)
                {
                    errorcode = w32ex.ErrorCode;
                }

                Log.Error("PPTOpen exception. " + ex.Message + ", errorcode=" + errorcode);//Error HRESULT E_FAIL has been returned from a call to a COM component.
                this.objApp.Quit();
                objApp = null;
                MessageBox.Show("打开" + filename + "失败，请检查该文件！", "警告", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                return false;
            }
            finally
            {

            }
        }

        public static int checkImgStatus(string name)
        {
            int status = -1;//-1 norecord, 0: importing, 1: ok
            if (g_fileImgStatus.Contains(name))
            {
                status = (int)g_fileImgStatus[name];
            }
            return status;
        }
        public static void exportImg(string path)
        {
            Log.Info("PPT.exportImg  path=" + path);
            string name = Path.GetFileName(path);

            string dir = Application.StartupPath +"\\"+ DateTime.Now.ToString("yyyyMMdd")+"\\";
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            if (name.IndexOf("ppt") < 0 && name.IndexOf("PPT")<0)
                return;



            if(g_fileImgStatus.Contains(name))
            {
                g_fileImgStatus.Remove(name);
            }
            g_fileImgStatus.Add(name, 0);//正在导入

            POWERPOINT.Application app = null;
            POWERPOINT.Presentation pre = null;
            try
            {
                app = new POWERPOINT.Application();

                #region check ppt or pptx
                try{
                    pre = app.Presentations.Open(path, OFFICECORE.MsoTriState.msoFalse, OFFICECORE.MsoTriState.msoFalse, OFFICECORE.MsoTriState.msoFalse);
                }catch(Exception e1){
                    Log.Info("Presentations.Open exception. " + e1+ ","+ path);
                    if (g_fileImgStatus.Contains(name))
                    {
                        g_fileImgStatus.Remove(name);
                        g_fileImgStatus.Add(name, 1);
                    }
                    Form1.DeleteFile(name);
                    MessageBox.Show("打开" + path + "失败，请检查该文件！", "警告", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                    app = null;
                }
                #endregion

                Log.Info("Presentations.Open succeed. " + name);
                POWERPOINT.Slides slides = pre.Slides;
                int pageTotal = pre.Slides.Count;

                bool bOver = true;
                for(int k=1; k<=pageTotal; k++)
                {
                    string imgpath1 = Application.StartupPath + "\\" + DateTime.Now.ToString("yyyyMMdd") + "\\" + name + "_" + k + ".jpg";
                    if (!File.Exists(imgpath1))
                    {
                        bOver = false;
                    }
                }

                if (bOver)
                {
                    try
                    {
                        if (pre != null)
                        {
                            pre.Close();
                            pre = null;
                        }
                    }
                    catch (Exception e2)
                    {
                        Log.Info("exportImg2 exception: " + e2.Message);
                    }
                    app = null;

                    return;
                }



                IEnumerator e = slides.GetEnumerator();
                int i = 0;

                while (e.MoveNext())
                {
                    i++;
                    PPT.Slide slide = (PPT.Slide)e.Current;
                    string imgpath = Application.StartupPath + "\\" + DateTime.Now.ToString("yyyyMMdd") + "\\" + name + "_" + i + ".jpg";
                    slide.Export(imgpath, "jpg", 400, 300);
                }

            }
            catch (Exception e1)
            {
                Log.Info("exportImg1 exception: " + e1.Message);
            }
            finally
            {
                if (g_fileImgStatus.Contains(name))
                {
                    g_fileImgStatus.Remove(name);
                    g_fileImgStatus.Add(name, 1);
                }

                try
                {
                    if (pre != null)
                    {
                        pre.Close();
                        pre = null;
                    }
                }
                catch (Exception e2)
                {
                    Log.Info("exportImg2 exception: " + e2.Message);
                }
                app = null;
            }

            Log.Info("PPT.exportImg over.  name=" + name);
        }
        public static void exportImg2(string path)
        {
            Log.Info("PPT.exportImg  path=" + path);
            string name = Path.GetFileName(path);

            string dir = Application.StartupPath + "\\" + DateTime.Now.ToString("yyyyMMdd") + "\\";
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            if (name.IndexOf("ppt") < 0 && name.IndexOf("PPT") < 0)
                return;

            string imgpath2 = Application.StartupPath + "\\" + DateTime.Now.ToString("yyyyMMdd") + "\\" + name + "_" + 2 + ".jpg";
            string imgpath3 = Application.StartupPath + "\\" + DateTime.Now.ToString("yyyyMMdd") + "\\" + name + "_" + 3 + ".jpg";
            if (File.Exists(imgpath2) && File.Exists(imgpath3))
            {
                return;
            }

            POWERPOINT.Application app = null;
            POWERPOINT.Presentation pre = null;
            try
            {
                app = new POWERPOINT.Application();

                #region check ppt or pptx
                try
                {
                    pre = app.Presentations.Open(path, OFFICECORE.MsoTriState.msoFalse, OFFICECORE.MsoTriState.msoFalse, OFFICECORE.MsoTriState.msoFalse);
                }
                catch (Exception e1)
                {
                    Log.Info("Presentations.Open exception. " + e1 + ", " + path);
                }
                #endregion


                POWERPOINT.Slides slides = pre.Slides;
                int pageTotal = pre.Slides.Count;

                IEnumerator e = slides.GetEnumerator();
                int i = 0;

                while (e.MoveNext())
                {
                    i++;
                    PPT.Slide slide = (PPT.Slide)e.Current;
                    string imgpath = Application.StartupPath + "\\" + DateTime.Now.ToString("yyyyMMdd") + "\\" + name + "_" + i + ".jpg";
                    slide.Export(imgpath, "jpg", 400, 300);
                }
            }
            catch (Exception e1)
            {
                Log.Info("exportImg1 exception: " + e1.Message);
            }
            finally
            {
                try
                {
                    if (pre != null)
                    {
                        pre.Close();
                        pre = null;
                    }
                }
                catch (Exception e2)
                {
                    Log.Info("exportImg2 exception: " + e2.Message);
                }
                app = null;
            }

            Log.Info("PPT.exportImg over.  name=" + name);
        }
        public static void clearImg()
        {
        }
        public bool isOpen()
        {
            if (this.objApp != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public string getImgData(int index)
        {
            string imgpath = Application.StartupPath + "\\" + DateTime.Now.ToString("yyyyMMdd") + "\\" + filename + "_" + index + ".jpg";
            return Util.GetFileBase64(imgpath);
        }
        public string getImgPath(int index)
        {
            string imgpath = Application.StartupPath + "\\" + DateTime.Now.ToString("yyyyMMdd") + "\\" + filename + "_" + index + ".jpg";
            return imgpath;
        }
        public void reOpen()
        {
            PPTOpen(filepath);
        }

        public string NextSlide()
        {
            string ret = "success";
            if (this.objApp != null)
            {
                try
                {
                    //maximizePPTProc();
                    //this.objPresSet.SlideShowWindow.View.Next();
                    MyTouch.PPTClick();
                    int page = GetCurrentPage();
                    if(pageLast!=pageCurrent)
                    {
                        bPageTurning = true;
                        pageLast = pageCurrent;
                    }
                    else
                    {
                        bPageTurning = false;
                    }
                }
                catch (Exception ex)
                {
                    ret = "PPT播放已关闭";
                    Log.Error("PPT.Next() exception. " + ex.Message);
                    PPTClose();
                    reOpen();
                }
            }
            else
            {
                return "error";
            }
            return ret;
        }
        public void PreviousSlide(string pageIndex)
        {

            if (this.objApp != null)
            {
                try
                {
                    maximizePPTProc();
                    int _cur = GetCurrentPage();
                    if (_cur > 1)
                    {
                        this.objPresSet.SlideShowWindow.View.Previous();
                    }
                    GetCurrentPage();
                    if (pageLast != pageCurrent)
                    {
                        bPageTurning = true;
                        pageLast = pageCurrent;
                    }
                    else
                    {
                        bPageTurning = false;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("PPT.Last() exception. " + ex.Message);
                    PPTClose();
                    reOpen();
                    int num = int.Parse(pageIndex);
                    GotoPage(num);
                }
            }
        }

        public void NoSlide(string pageIndex)
        {

            if (this.objApp != null)
            {
                try
                {
                    maximizePPTProc();
                    int _cur = GetCurrentPage();
                    GetCurrentPage();
                    this.objPresSet.Save();
                    if (pageLast != pageCurrent)
                    {
                        bPageTurning = true;
                        pageLast = pageCurrent;
                    }
                    else
                    {
                        bPageTurning = false;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("PPT.Last() exception. " + ex.Message);
                    PPTClose();
                    reOpen();
                    int num = int.Parse(pageIndex);
                    GotoPage(num);
                }
            }
        }
        public void savePPT()
        {
            if (this.objApp != null)
            {
                this.objPresSet.Save();
            }
        }
        private int GetCurrentPage()
        {
            // 获得当前选中的幻灯片   
            PPT.Slide slide;
            try
            {
                try
                {
                    // 在普通视图下这种方式可以获得当前选中的幻灯片对象
                    // 然而在阅读模式下，这种方式会出现异常
                    slide = objSlides[objApp.ActiveWindow.Selection.SlideRange.SlideNumber];
                }
                catch
                {
                    // 在阅读模式下出现异常时，通过下面的方式来获得当前选中的幻灯片对象
                    slide = objApp.SlideShowWindows[1].View.Slide;
                }
                int slideindex = slide.SlideIndex;
                int slidenum = slide.SlideNumber;
                int currentPosition = objPresSet.SlideShowWindow.View.CurrentShowPosition;
                pageCurrent = slideindex;

                if (pageCurrent == 0)
                    pageCurrent = 1;

                //开始读取每一个数据块
                {
                    urls="";
                    pptHyperlinks = slide.Hyperlinks;
                    try
                    {
                        for (int i = 1; i <= pptHyperlinks.Count; i++)
                        {
                            PPT.Hyperlink link = pptHyperlinks[i];
                            string text = link.TextToDisplay.Replace("#","").Replace("|","");
                            string url = link.Address.Replace("#","").Replace(";","");
                            //link.Address = "www.test.com";
                            //link.TextToDisplay = "test";
                            urls += (urls.Length>0?"|":"")  + text;//+ i +"#"+      + "#" + url
                            Log.Info(urls);
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Info(e.Message);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error("GetCurrentPage excepton: " + e.Message);
            }
            return pageCurrent;
        }
        public void GotoPage(int page)
        {
            if(pageTotal < page)
            {
                page = pageTotal;
            }
            try
            {
                PPT.SlideShowView ssv = objPresSet.SlideShowWindow.View;
                int currentShowPosition = objPresSet.SlideShowWindow.View.CurrentShowPosition;
                ssv.GotoSlide(page);
                int currentShowPosition2 = objPresSet.SlideShowWindow.View.CurrentShowPosition;
                //int clickcount2 = objPresSet.SlideShowWindow.View.GetClickCount();
                //int clickindex2 = objPresSet.SlideShowWindow.View.GetClickIndex();
                int a = 0;
                a++;
                if (currentShowPosition2 != page)
                {
                    Thread.Sleep(1000);
                    ssv.GotoSlide(page);
                    currentShowPosition2 = objPresSet.SlideShowWindow.View.CurrentShowPosition;
                }
                pageCurrent = currentShowPosition2;

                GetCurrentPage();
            }
            catch (Exception e)
            {
                Log.Error("GotoPage excepton: "+ e.Message);
            }
        }


        public void PPTClose()
        {
            try
            {
                if (this.objApp != null)
                {
                    try
                    {
                        this.objPresSet.Close();
                        this.objPresSet = null;
                    }catch(Exception e1){}
                    this.objApp.Quit();//TODO;trycatch
                    this.objApp = null;
                }
            }catch(Exception e)
            {
                Log.Error(e.Message);
                this.objApp = null;
            }finally
            {
                closeProc(filename);
                GC.Collect();
            }
        }

        public object ReturnObj()
        {
            return this.objApp;
        }

        private bool closeProc(string name)
        {
            string filename0 = "";
            if (name.Length > 0 && name.IndexOf('.')>0)
            {
                filename0 = name.Substring(0, name.LastIndexOf("."));
            }
            bool result = false;

            foreach (Process thisProc in Process.GetProcesses())
            {
                if (thisProc.ProcessName == "POWERPNT")
                {
                    string mainWindowTitle = thisProc.MainWindowTitle;
                    thisProc.Kill();
                    result = true;

                }
            }
            return result;
        }
        public bool maxisizeProc()
        {
            bool bFound = false;
            try
            {
                foreach (Process thisProc in Process.GetProcesses())
                {
                    if (thisProc.ProcessName == "POWERPNT")
                    {
                        IntPtr hWnd1 = thisProc.MainWindowHandle;
                        IntPtr hWnd = FindWindow(null, thisProc.MainWindowTitle);//old
                        SendMessage(hWnd, WM_SYSCOMMAND, SC_MAXIMIZE, 0);
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error("exception. " + e.Message);
            }
            return bFound;
        }

        public bool maximizePPTProc()
        {
            bool bFound = false;
            try
            {
                foreach (Process thisProc in Process.GetProcesses())
                {
                    if (thisProc.ProcessName == "POWERPNT")
                    {
                        IntPtr hWnd1 = thisProc.MainWindowHandle;
                        IntPtr hWnd = FindWindow(null, thisProc.MainWindowTitle);//old

                        SendMessage(hWnd, WM_SYSCOMMAND, SC_MAXIMIZE, 0);//无效

                        //int _ret1 = BringWindowToTop(hWnd);//无效
                        //Log.Info("MaxsimizePPT()...1 ret=" + _ret1);

                        //int _ret2 = ShowWindow(hWnd, SW_SHOWMAXIMIZED);//无效
                        //Log.Info("MaxsimizePPT()...2 ret=" + _ret2);
                        
                        if(!SetForegroundWindow(hWnd))
                        {
                            Log.Info("MaxsimizePPT()...ActivateWindow......");
                            ActivateWindow(hWnd);
                        }
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error("exception. " + e.Message);
            }

            return bFound;
        }

        [DllImport("user32.dll")]
        private static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);
        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);


        public void ActivateWindow(IntPtr hWnd)
        {
            if (hWnd == GetForegroundWindow())
                return;

            IntPtr ThreadID1 = GetWindowThreadProcessId(GetForegroundWindow(), IntPtr.Zero);
            IntPtr ThreadID2 = GetWindowThreadProcessId(hWnd, IntPtr.Zero);

            if (ThreadID1 != ThreadID2)
            {
                AttachThreadInput(ThreadID1, ThreadID2, 1);
                SetForegroundWindow(hWnd);
                AttachThreadInput(ThreadID1, ThreadID2, 0);
            }
            else
            {
                SetForegroundWindow(hWnd);
            }
            ShowWindowAsync(hWnd, SW_SHOWNORMAL);
            //if (IsIconic(hWnd))
            //{
            //    ShowWindowAsync(m_hWnd, SW_RESTORE);
            //}
            //else
            //{
            //    ShowWindowAsync(hWnd, SW_SHOWNORMAL);
            //}
        }

        public static void minisizeAllProc()
        {
            try
            {
                foreach (Process thisProc in Process.GetProcesses())
                {
                    IntPtr hWnd = FindWindow(null, thisProc.MainWindowTitle);//old
                    SendMessage(hWnd, WM_SYSCOMMAND, SC_MINIMIZE, 0);//无效
                }
            }
            catch (Exception e)
            {
                Log.Error("exception. " + e.Message);
            }
        }

        public void minisizeProc()
        {
            //https://msdn.microsoft.com/en-us/library/dd375731(v=vs.85).aspx
            ////VK_LWIN 0x5B
            ////VK_RWIN 0x5C
            ////D key 0x44
            ////M key 0x4D
            //keybd_event(0x5B, 0, 0, 0);
            //keybd_event(0x4D, 0, 0, 0);
            //keybd_event(0x4D, 0, 2, 0);
            //keybd_event(0x5B, 0, 2, 0);

            //IntPtr hWnd = GetForegroundWindow(); //获取当前窗口句柄
            //Log.Info("minisizeProc , currentWindow="+hWnd);
            ////PostMessage(hWnd, WM_SYSCOMMAND, SC_MINIMIZE, 0);
            //ShowWindow(hWnd, 2);

            Type oleType = Type.GetTypeFromProgID("Shell.Application");
            object oleObject = System.Activator.CreateInstance(oleType);
            oleType.InvokeMember("ToggleDesktop", BindingFlags.InvokeMethod, null, oleObject, null);
        }

        [DllImport("User32.dll")]
        private static extern bool SetCursorPos(int x, int y);
        public void PPTMouseMove(double x, double y)
        {
            double posX = screenWidth * x;
            double posY = screenHeight * y;
            System.Windows.Forms.Cursor.Position = new System.Drawing.Point((int)posX, (int)posY);
            SetCursorPos((int)posX, (int)posY);
        }
        public void PPTMouseClick(double x, double y)
        {
            double posX = screenWidth * x;
            double posY = screenHeight * y;
            System.Windows.Forms.Cursor.Position = new System.Drawing.Point((int)posX, (int)posY);
            SetCursorPos((int)posX, (int)posY);

            mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
        }
    }
}
