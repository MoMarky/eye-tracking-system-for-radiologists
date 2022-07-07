#region License

// Copyright (c) 2013, ClearCanvas Inc.
// All rights reserved.
// http://www.clearcanvas.ca
//
// This file is part of the ClearCanvas RIS/PACS open source project.
//
// The ClearCanvas RIS/PACS open source project is free software: you can
// redistribute it and/or modify it under the terms of the GNU General Public
// License as published by the Free Software Foundation, either version 3 of the
// License, or (at your option) any later version.
//
// The ClearCanvas RIS/PACS open source project is distributed in the hope that it
// will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General
// Public License for more details.
//
// You should have received a copy of the GNU General Public License along with
// the ClearCanvas RIS/PACS open source project.  If not, see
// <http://www.gnu.org/licenses/>.

#endregion

#region Additional permission to link with DotNetMagic

// Additional permission under GNU GPL version 3 section 7
// 
// If you modify this Program, or any covered work, by linking or combining it
// with DotNetMagic (or a modified version of that library), containing parts
// covered by the terms of the Crownwood Software DotNetMagic license, the
// licensors of this Program grant you additional permission to convey the
// resulting work.

#endregion

using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ClearCanvas.Desktop.Actions;
using Crownwood.DotNetMagic.Common;
using Crownwood.DotNetMagic.Controls;
using Crownwood.DotNetMagic.Docking;
using Crownwood.DotNetMagic.Forms;
using System.Runtime.InteropServices;
using System.Threading;
using System.IO;
using System.Diagnostics;  //for  new Process();
using ClearCanvas.Common;
using ClearCanvas.Common.Utilities;
using ClearCanvas.Desktop;
using ClearCanvas.Desktop.Tools;
using ClearCanvas.ImageViewer;
using ClearCanvas.ImageViewer.StudyManagement;
using ClearCanvas.ImageViewer.Explorer.Local;



namespace ClearCanvas.Desktop.View.WinForms
{
    /// <summary>
	/// Form used by the <see cref="DesktopWindowView"/> class.
    /// </summary>
    /// <remarks>
    /// This class may be subclassed.
    /// </remarks>
    public partial class DesktopForm : DotNetMagicForm
    {
        private ActionModelNode _menuModel;
        private ActionModelNode _toolbarModel;

        //MouseHook mh;
        bool LeftTag = false;
        bool RightTag = false;
        bool StartListen = false;
        
        public static bool Is_aSeePro_running = false;
        public static float get_gaze_x = -1, get_gaze_y = -1;
        public static long get_time = -1;
        Point p1 = new Point(0, 0);
        Point p2 = new Point(0, 0);
        //InfoCollect infowithtime = new InfoCollect();
        //Info inf = new InfoCollect(); 

        //InfoCollect.Info my_info = new InfoCollect.Info(); #1020

        //StreamWriter sw = new StreamWriter("info_log.txt"); #1020

        public static string test = "";//测试静态变量传参
        //public struct ManagedDemoStruct  #1020
        //{
        //    public float gazex, gazey;
        //    public long time;

        //}
        public static Thread thread, thread_report;

        aSeePro_StartDLL aSeePro_start = new aSeePro_StartDLL();


        //Set_Materials的 索引，表示当前正在显示csv中的哪一行
        int Show_csv_line_index = -1;
        int Case_Num = 0;

        /// <summary>
        /// Constructor
        /// </summary>
        public DesktopForm()
        {
#if !MONO
			SplashScreenManager.DismissSplashScreen(this);
#endif
            //mh = new MouseHook();  //0520 
            //mh.SetHook();
            //mh.MouseDownEvent += mh_MouseDownEvent;

            //ManagedDemoStruct argStruct = new ManagedDemoStruct();


            //mh.MouseUpEvent += mh_MouseUpEvent;
            //info_init(my_info);  #1020

            InitializeComponent();
            
            //Set both to be initially invisible, since there's nothing on them.
            _toolbar.Visible = false;
            _mainMenu.Visible = false;

			// manually subscribe this event handler *after* the call to InitializeComponent()
			_toolbar.ParentChanged += OnToolbarParentChanged;
            _dockingManager = new DockingManager(_toolStripContainer.ContentPanel, VisualStyle.IDE2005);
            _dockingManager.ActiveColor = SystemColors.Control;
            _dockingManager.InnerControl = _tabbedGroups;
			_dockingManager.TabControlCreated += OnDockingManagerTabControlCreated;
            
			_tabbedGroups.DisplayTabMode = DisplayTabModes.HideAll;
			_tabbedGroups.TabControlCreated += OnTabbedGroupsTabControlCreated;
            //this.KeyPress += new KeyEventHandler(this.Form1_KeyPress);


            //GlobalMouseHandler globalClick = new GlobalMouseHandler();
            //Application.AddMessageFilter(globalClick);
            

            if (_tabbedGroups.ActiveLeaf != null)
			{
				InitializeTabControl(_tabbedGroups.ActiveLeaf.TabControl);
			}

			ToolStripSettings.Default.PropertyChanged += OnToolStripSettingsPropertyChanged;
			OnToolStripSettingsPropertyChanged(ToolStripSettings.Default, new PropertyChangedEventArgs("WrapLongToolstrips"));
			OnToolStripSettingsPropertyChanged(ToolStripSettings.Default, new PropertyChangedEventArgs("IconSize"));
        }


        #region Public properties

        /// <summary>
        /// Gets or sets the menu model.
        /// </summary>
        public ActionModelNode MenuModel
        {
            get { return _menuModel; }
            set
            {
                _menuModel = value;

                //Unsubscribe, so we don't update visibility as the model is being figured out.
                _mainMenu.LayoutCompleted -= OnMenuLayoutCompleted;
                BuildToolStrip(ToolStripBuilder.ToolStripKind.Menu, _mainMenu, _menuModel);

                _mainMenu.LayoutCompleted += OnMenuLayoutCompleted;
                //Subscribe so the visibility updates if all actions suddenly become invisible or unavailable.
                OnMenuLayoutCompleted(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Gets or sets the toolbar model.
        /// </summary>
        public ActionModelNode ToolbarModel
        {
            get { return _toolbarModel; }
            set
            {
                _toolbarModel = value;

                //Unsubscribe, so we don't update visibility as the model is being figured out.
                _toolbar.LayoutCompleted -= OnToolbarLayoutCompleted;
                BuildToolStrip(ToolStripBuilder.ToolStripKind.Toolbar, _toolbar, _toolbarModel);

                //Subscribe so the visibility updates if all actions suddenly become invisible or unavailable.
                _toolbar.LayoutCompleted += OnToolbarLayoutCompleted;
                OnToolbarLayoutCompleted(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Gets the <see cref="TabbedGroups"/> object that manages workspace tab groups.
        /// </summary>
        public TabbedGroups TabbedGroups
        {
            get { return _tabbedGroups; }
        }

        /// <summary>
        /// Gets the <see cref="DockingManager"/> object that manages shelf docking windows.
        /// </summary>
        public DockingManager DockingManager
        {
            get { return _dockingManager; }
        }

        #endregion

        #region Form event handlers

        private void OnTabbedGroupsTabControlCreated(TabbedGroups tabbedGroups, Crownwood.DotNetMagic.Controls.TabControl tabControl)
        {
            InitializeTabControl(tabControl);
        }

        private void OnDockingManagerTabControlCreated(Crownwood.DotNetMagic.Controls.TabControl tabControl)
        {
            InitializeTabControl(tabControl);
        }

    	private void OnToolStripSettingsPropertyChanged(object sender, PropertyChangedEventArgs e)
    	{
    		ToolStripSettings settings = ToolStripSettings.Default;
    		if (e.PropertyName == "WrapLongToolstrips" || e.PropertyName == "ToolStripDock")
    		{
				// handle both wrapping and docking together because both affect flow direction
    			bool verticalOrientation = ReferenceEquals(_toolbar.Parent, _toolStripContainer.LeftToolStripPanel)
    			                           || ReferenceEquals(_toolbar.Parent, _toolStripContainer.RightToolStripPanel);

                _toolbar.SuspendLayout();
    			_toolbar.LayoutStyle = settings.WrapLongToolstrips ? ToolStripLayoutStyle.Flow : ToolStripLayoutStyle.StackWithOverflow;
    			if (settings.WrapLongToolstrips)
    				((FlowLayoutSettings) _toolbar.LayoutSettings).FlowDirection = verticalOrientation ? FlowDirection.TopDown : FlowDirection.LeftToRight;
    			_toolbar.ResumeLayout(true);

    			ToolStripPanel targetParent = ConvertToToolStripPanel(_toolStripContainer, settings.ToolStripDock);
    			if (targetParent != null && !ReferenceEquals(targetParent, _toolbar.Parent))
    			{
    				_toolStripContainer.SuspendLayout();
    				targetParent.Join(_toolbar);

    				_toolStripContainer.ResumeLayout(true);
    			}
    		}
			else if (e.PropertyName == "IconSize")
			{
				ToolStripBuilder.ChangeIconSize(_toolbar, settings.IconSize);
			}
    	}

    	private void OnToolbarParentChanged(object sender, EventArgs e)
    	{
    		ToolStripDock dock = ConvertToToolStripDock(_toolStripContainer, _toolbar);
    		if (dock != ToolStripDock.None)
    		{
    			ToolStripSettings settings = ToolStripSettings.Default;
    			settings.ToolStripDock = dock;
    			settings.Save();
    		}
    	}

        /// <summary>
        /// This will fire anytime the main menu layout changes, which includes items 
        /// changing their visibility/availability and the menu being rebuilt.
        /// </summary>
        private void OnToolbarLayoutCompleted(object sender, EventArgs e)
        {
            var anyVisible = _toolbar.Items.Cast<ToolStripItem>().Any(i => i.Available);
            if (_toolbar.Visible != anyVisible)
                _toolbar.Visible = anyVisible;
        }

        /// <summary>
        /// This will fire anytime the toolbar layout changes, which includes items 
        /// changing their visibility/availability and the toolbar being rebuilt.
        /// </summary>
        private void OnMenuLayoutCompleted(object sender, EventArgs e)
        {
            var anyVisible = _mainMenu.Items.Cast<ToolStripItem>().Any(i => i.Available);
            if (_mainMenu.Visible != anyVisible)
                _mainMenu.Visible = anyVisible;
        }

        #endregion

        #region Helper methods

        /// <summary>
        /// Called to initialize a <see cref="Crownwood.DotNetMagic.Controls.TabControl"/>. Override
        /// this method to perform custom initialization.
        /// </summary>
        /// <param name="tabControl"></param>
        protected virtual void InitializeTabControl(Crownwood.DotNetMagic.Controls.TabControl tabControl)
		{
			if (tabControl == null)
				return;
            
            tabControl.TextTips = true;
			tabControl.ToolTips = false;
			tabControl.MaximumHeaderWidth = 256;
        }

        /// <summary>
        /// Called to build menus and toolbars.  Override this method to customize menu and toolbar building.
        /// </summary>
        /// <remarks>
        /// The default implementation simply clears and re-creates the toolstrip using methods on the
        /// utility class <see cref="ToolStripBuilder"/>.
        /// </remarks>
        /// <param name="kind"></param>
        /// <param name="toolStrip"></param>
        /// <param name="actionModel"></param>
        protected virtual void BuildToolStrip(ToolStripBuilder.ToolStripKind kind, ToolStrip toolStrip, ActionModelNode actionModel)
        {
            // avoid flicker
            toolStrip.SuspendLayout();
            // very important to clean up the existing ones first
            ToolStripBuilder.Clear(toolStrip.Items);

            if (actionModel != null)
            {
				if (actionModel.ChildNodes.Count > 0)
				{
					// Toolstrip should only be visible if there are items on it
					if (kind == ToolStripBuilder.ToolStripKind.Toolbar)
						ToolStripBuilder.BuildToolStrip(kind, toolStrip.Items, actionModel.ChildNodes, ToolStripBuilder.ToolStripBuilderStyle.GetDefault(), ToolStripSettings.Default.IconSize);
					else
						ToolStripBuilder.BuildToolStrip(kind, toolStrip.Items, actionModel.ChildNodes);
                }
            }

            toolStrip.ResumeLayout();
        }

    	private static ToolStripPanel ConvertToToolStripPanel(ToolStripContainer toolStripContainer, ToolStripDock dock)
    	{
    		switch (dock)
    		{
    			case ToolStripDock.Left:
    				return toolStripContainer.LeftToolStripPanel;
    			case ToolStripDock.Top:
    				return toolStripContainer.TopToolStripPanel;
    			case ToolStripDock.Right:
    				return toolStripContainer.RightToolStripPanel;
    			case ToolStripDock.Bottom:
    				return toolStripContainer.BottomToolStripPanel;
    			case ToolStripDock.None:
    			default:
    				return null;
    		}
    	}

    	private static ToolStripDock ConvertToToolStripDock(ToolStripContainer toolStripContainer, ToolStrip toolStrip)
    	{
    		ToolStripPanel parent = toolStrip.Parent as ToolStripPanel;
    		if (ReferenceEquals(parent, toolStripContainer.TopToolStripPanel))
    			return ToolStripDock.Top;
    		else if (ReferenceEquals(parent, toolStripContainer.LeftToolStripPanel))
    			return ToolStripDock.Left;
    		else if (ReferenceEquals(parent, toolStripContainer.BottomToolStripPanel))
    			return ToolStripDock.Bottom;
    		else if (ReferenceEquals(parent, toolStripContainer.RightToolStripPanel))
    			return ToolStripDock.Right;
			else
				return ToolStripDock.None;
    	}

        /// <summary>
        /// 菜单栏startDlg点击 打开aSee线程
        /// </summary>
        private void startDlgToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Is_aSeePro_running = true;
            //thread.Abort();
            //thread.Suspend();
            //System.Threading.ThreadState state = thread.ThreadState;
            //if (state != System.Threading.ThreadState.Running)
            if(!Is_aSeePro_running)
            {
                thread = new Thread(new ParameterizedThreadStart(aSeePro_start.TestStart));//创建线程

                thread.Start(true);
            }
        }
        /// <summary>
        /// 菜单栏timer点击 打开ShowTime窗口 显示时间戳
        /// </summary>
        private void timerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //ShowTime st = new ShowTime();
            //st.ShowDialog();
        }
        /// <summary>
        /// 点击菜单获取当前窗体内的控件，可以得到位置信息
        /// </summary>
        private void getToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string output = ClearCanvas.Common.PublicMethod.Screen_x.ToString() + '\n' +
                            ClearCanvas.Common.PublicMethod.Screen_y.ToString() + '\n' +
                            ClearCanvas.Common.PublicMethod.Screen_w.ToString() + '\n' +
                            ClearCanvas.Common.PublicMethod.Screen_h.ToString(); 
            System.Windows.Forms.MessageBox.Show(output);
            //foreach (Control control in this.Controls)
            //{
            //    //遍历窗体内所有控件
            //    //control.Enabled = false;
            //    System.Windows.Forms.MessageBox.Show(control.ToString());
            //}
            ////使用反射获取系统控件
            //System.Reflection.FieldInfo[] fieldInfo = this.GetType().GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            ////System.Windows.Forms.MessageBox.Show(fieldInfo[1].ToString());
            //String s = "";
            //for (int i = 0; i < fieldInfo.Length; i++)
            //{
            //    s += ("#@@#" + fieldInfo[i].ToString());
            //}
            //System.Windows.Forms.MessageBox.Show(s);
        }
        /// <summary>
        /// Set_Materials,设置影像列表，用于医生阅读
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void setMaterialsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //打开 set_material 页面
            Process proc = null;
            try
            {
                string targetDir = string.Format(@".\");//this is where mybatch.bat lies
                proc = new Process();
                proc.StartInfo.WorkingDirectory = targetDir;
                proc.StartInfo.FileName = "run_set_materials.bat";
                proc.StartInfo.Arguments = string.Format("10");//this is argument
                //proc.StartInfo.CreateNoWindow = false;
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.CreateNoWindow = true;
                proc.Start();
                proc.WaitForExit();
                Show_csv_line_index = 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception Occurred :{0},{1}", ex.Message, ex.StackTrace.ToString());
            }

            // 设置窗口模式，true即为4窗
            PhysicalWorkspace.isMammo = true;

            //打开保存writer
            if (!ClearCanvas.Common.PublicSaveLog.SetLogSaveWriter())
                System.Windows.Forms.MessageBox.Show("Csv Writer is opening...");

            DicomImageLoaderTool open_img = new DicomImageLoaderTool();
            Case_Num = open_img.Open_Materials(@".\EyeTracker\res\Material_List.csv", Show_csv_line_index, true);
            if (Case_Num != 0)
            {
                this.nextToolStripMenuItem.Enabled = true;
                this.preiousToolStripMenuItem.Enabled = true;
                this.saveReportToolStripMenuItem.Enabled = true;
                this.toolStripTextBox1.Text = Show_csv_line_index.ToString() + "/" + Case_Num.ToString();
            }
            //设置完 阅读数据后，在显示图像后，自动打开报告编写
            object ss = null;
            EventArgs arg = null;
            this.saveReportToolStripMenuItem_Click(ss, arg);
        }
        /// <summary>
        /// 下一张影像，Show_csv_line_index++，若超过总行数，则无操作
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void nextToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //一张影像阅读完，切换下一张时，先判断 报告界面是否在运行，若运行，则提示要保存并关闭。，没运行，则打开新的报告界面
            if (thread_report == null)
            {
                System.Windows.Forms.MessageBox.Show("Please save report first!!!");
            }
            else if (thread_report.ThreadState != System.Threading.ThreadState.Stopped)
            {
                System.Windows.Forms.MessageBox.Show("Please save report first!!!");
            }
            else
            {
                Show_csv_line_index++;
                // 设置窗口模式，true即为4窗
                PhysicalWorkspace.isMammo = true;
                DicomImageLoaderTool open_img = new DicomImageLoaderTool();
                int flag = open_img.Open_Materials(@".\EyeTracker\res\Material_List.csv", Show_csv_line_index, false);
                if (flag != 0)
                {
                    if (flag != 3)  //如果index没有大于 总材料数目，就index+1并且显示报告界面
                    {
                        this.toolStripTextBox1.Text = Show_csv_line_index.ToString() + "/" + Case_Num.ToString();
                        ////调出报告界面
                        object ss = null;
                        EventArgs arg = null;
                        this.saveReportToolStripMenuItem_Click(ss, arg);
                    }
                    else  //否则还原index数值，再进行next操作时，也只是显示最后一例
                        Show_csv_line_index -= 1;
                }
            }
           
        }
        
        /// <summary>
        /// 上一张影像，Show_csv_line_index--， 若低于0，则无操作
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void preiousToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //一张影像阅读完，切换下一张时，先判断 报告界面是否在运行，若运行，则提示要保存并关闭。，没运行，则打开新的报告界面
            if (thread_report.ThreadState != System.Threading.ThreadState.Stopped)
            {
                System.Windows.Forms.MessageBox.Show("Please save report first!!!");
            }
            else
            {
                Show_csv_line_index--;
                // 设置窗口模式，true即为4窗
                PhysicalWorkspace.isMammo = true;
                DicomImageLoaderTool open_img = new DicomImageLoaderTool();
                int flag = open_img.Open_Materials(@".\EyeTracker\res\Material_List.csv", Show_csv_line_index, false);
                if (flag != 0)
                {
                    if (flag != 2)  //如果index索引不低于0，则正常index-1，并调出报告界面
                    {
                        this.toolStripTextBox1.Text = Show_csv_line_index.ToString() + "/" + Case_Num.ToString();
                        ////调出报告界面
                        object ss = null;
                        EventArgs arg = null;
                        this.saveReportToolStripMenuItem_Click(ss, arg);
                    }
                    else  //否则还原index数值，再进行pre操作时，也只是显示第一例
                        Show_csv_line_index += 1;
                    
                }
            }
        }
        
        /// <summary>
        /// 打开 眼动仪采集页面
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void startEyetrackerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (thread == null || thread.ThreadState == System.Threading.ThreadState.Stopped)
            {
                thread = new Thread(new ParameterizedThreadStart(this.start_collector));//创建线程
                thread.Start(true);
            }
        }
        private void start_collector(object flag)
        {
            Process proc = null;
            try
            {
                string targetDir = string.Format(@".\");//this is where mybatch.bat lies
                proc = new Process();
                proc.StartInfo.WorkingDirectory = targetDir;
                proc.StartInfo.FileName = "run_eye_tracker.bat";
                proc.StartInfo.Arguments = string.Format("10");//this is argument
                //proc.StartInfo.CreateNoWindow = false;
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.CreateNoWindow = true;
                proc.Start();
                proc.WaitForExit();
                
                thread.Abort(true);
                thread = null;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception Occurred :{0},{1}", ex.Message, ex.StackTrace.ToString());
            }
        }
        
        /// <summary>
        /// 打开用户信息采集页面，第一步
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void saveUserInfoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process proc = null;
            try
            {
                string targetDir = string.Format(@".\");//this is where mybatch.bat lies
                proc = new Process();
                proc.StartInfo.WorkingDirectory = targetDir;
                proc.StartInfo.FileName = "run_save_user_info.bat";
                proc.StartInfo.Arguments = string.Format("10");//this is argument
                //proc.StartInfo.CreateNoWindow = false;
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.CreateNoWindow = true;
                proc.Start();
                proc.WaitForExit();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception Occurred :{0},{1}", ex.Message, ex.StackTrace.ToString());
            }
        }
        /// <summary>
        /// 打开 编写报告页面，开始对当前图像进行报告总结
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void saveReportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (thread_report == null || thread_report.ThreadState == System.Threading.ThreadState.Stopped)
            {
                thread_report = new Thread(new ParameterizedThreadStart(this.reportor));//创建线程
                thread_report.Start(true);
            }
            
        }
        public void reportor(object flag)
        {
            Process proc = null;
            try
            {
                string targetDir = string.Format(@".\");//this is where mybatch.bat lies
                proc = new Process();
                proc.StartInfo.WorkingDirectory = targetDir;
                proc.StartInfo.FileName = "run_save_erport.bat";
                proc.StartInfo.Arguments = string.Format("10");//this is argument
                //proc.StartInfo.CreateNoWindow = false;
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.CreateNoWindow = true;
                proc.Start();
                proc.WaitForExit();
                thread_report.Abort(true);
                thread_report = null;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception Occurred :{0},{1}", ex.Message, ex.StackTrace.ToString());
            }
        }

        /// <summary>
        /// 菜单listening按下
        /// </summary>
        private void listenningToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!ClearCanvas.Common.PublicSaveLog.SetLogSaveWriter())
                System.Windows.Forms.MessageBox.Show("Csv Writer is opening...");

            //my_info.gaze.x = 0;
            //my_info.gaze.y = 0;
            //my_info.time.now_time = "";
            //my_info.key_mouse.key = "";
            //my_info.key_mouse.mouse = "";

            ////实例化Timer类，设置间隔时间为20毫秒；
            //System.Timers.Timer t = new System.Timers.Timer(50);
            //t.Elapsed += new System.Timers.ElapsedEventHandler(timeout);//到达时间的时候执行 数据保存；

            //t.AutoReset = true;//设置是执行一次（false）还是一直执行(true)；

            //t.Enabled = true;//是否执行System.Timers.Timer.Elapsed事件；

        }
        /// <summary>
        /// 菜单listening按下，开启计时器后的timeout响应函数，用于保存操作数据
        /// </summary>
        //public void timeout(object source, System.Timers.ElapsedEventArgs e) #1020
        //{
        //    //表示不检查控件的非法跨线程调用
        //    //Control.CheckForIllegalCrossThreadCalls = false;

        //    String key_str, mouse_str, time_str, gaze_str, current_dcm_path, current_img;
        //    //String Img_posX, Img_posY, Img_SizeX, Img_SizeY, current_img;
        //    //TimeSpan ts = DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, 0);
        //    //String t = Convert.ToInt64(ts.TotalMilliseconds).ToString();
        //    String t = DateTime.Now.ToString("yyyy-MM-dd") + "_" + "_" + DateTime.Now.Hour.ToString() + "-" +
        //        DateTime.Now.Minute.ToString() + "-" + DateTime.Now.Second.ToString() + "-" +
        //        DateTime.Now.Millisecond.ToString();


        //    float[] now_iamge_pos = new float[13];
        //    now_iamge_pos = ClearCanvas.Common.PublicMethod.GetPresentImagePos();


        //    time_str = "{\"time\": \"" + t + "\", ";
        //    //gaze_str = "'gaze': [{" + "" + "}], ";
        //    //mouse_str = "\"mouse\": [" + my_info.key_mouse.mouse + "], ";  #1020
        //    //key_str = "\"key\": [" + my_info.key_mouse.key + "], ";  #1020
        //    current_dcm_path = "\"dcm_path\": \"" + ClearCanvas.Common.PublicMethod.GetPresentDcmPath().Replace('\\', '/') + "\", ";
        //    current_img = "\"dcm_info\": [{\"dcmx\": " + now_iamge_pos[0].ToString() +
        //        ", \"dcmy\": " + now_iamge_pos[1].ToString() +
        //        ", \"dcmw\": " + now_iamge_pos[2].ToString() +
        //        ", \"dcmh\": " + now_iamge_pos[3].ToString() + "}]}\n";

        //    //sw.Write(time_str + mouse_str + key_str + current_dcm_path + current_img);
        //    //1015
        //    //sw.Write(time_str + current_dcm_path + current_img); //now use

        //    //sw.Close();
        //    //info_init(my_info);  #1020
        //    //sw.Flush();  #1020
        //    //float scale_x = -1, scale_y = -1;
        //    //int offset_x = -1, offset_y = -1;

        //    //string str =" ImageX: " + now_iamge_pos[0].ToString() +
        //    //            " ImageY: " + now_iamge_pos[1].ToString() +
        //    //            " ImageSizeX: " + now_iamge_pos[2].ToString() +
        //    //            " ImageSizeY: " + now_iamge_pos[3].ToString() +
        //    //            " X_Scale: " + now_iamge_pos[4].ToString() +
        //    //            " Y_Sclae: " + now_iamge_pos[5].ToString() +
        //    //            " Box_X: " + now_iamge_pos[6].ToString() +
        //    //            " Box_Y: " + now_iamge_pos[7].ToString() +
        //    //            " Box_W: " + now_iamge_pos[8].ToString() +
        //    //            " Box_H: " + now_iamge_pos[9].ToString() +
        //    //            " SelectedBox: " + now_iamge_pos[10].ToString() +
        //    //            " ImageOffsetX: " + now_iamge_pos[11].ToString() +
        //    //            " ImageOffsetY: " + now_iamge_pos[12].ToString();

        //    //Console.WriteLine(  "##" +str);

        //}
        /// <summary>
        /// info信息的初始化
        /// </summary>
        //public void info_init(InfoCollect.Info info)  #1020
        //{
        //    info.gaze.x = 0;
        //    info.gaze.y = 0;
        //    info.key_mouse.key = "";
        //    info.key_mouse.mouse = "";
        //    info.time.now_time = "";
        //}

        /// <summary>
        /// 判断键盘按下，只能监听字母和数字
        /// </summary>
        //private void DesktopForm_KeyPress(object sender, KeyPressEventArgs e) #1020
        //{
        //    char Key_Char = e.KeyChar;//判断按键的 Keychar  
        //    System.Windows.Forms.MessageBox.Show(Key_Char.ToString());//
        //}
        /// <summary>
        /// 判断键盘按下，监听除PS键以外的所有，字母不分大小写
        /// </summary>
        //private void DesktopForm_KeyDown(object sender, KeyEventArgs e) #1020
        //{
        //    //Keys key = e.KeyCode;//判断按键的 KeyCode  
        //    String k = e.KeyCode.ToString();
        //    //my_info.key_mouse.key = "{\"key\": \"" + k + "\"}";  #1020

        //    TimeSpan ts = DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, 0);        
        //    String t = Convert.ToInt64(ts.TotalMilliseconds).ToString();
        //    //System.Windows.Forms.MessageBox.Show(k +=  Convert.ToInt64(ts.TotalMilliseconds).ToString());//
        //    Console.WriteLine(k + ": " + t);
        //}
        /// <summary>
        /// 监听鼠标滑轮，e.Delta > 0滑轮上滑，e.Delta  小于零下滑 
        /// </summary>
        //private void DesktopForm_MouseWheel(object sender, MouseEventArgs e) #1020
        //{
        //    if (e.Delta > 0)
        //    {
        //        TimeSpan ts = DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, 0);
        //        String t = Convert.ToInt64(ts.TotalMilliseconds).ToString();
        //        //System.Windows.Forms.MessageBox.Show("Wheel Up");
        //        String str = "{\"Wheel\": 1}";
        //        //my_info.key_mouse.mouse = str;  #1020

        //        Console.WriteLine(str + t);
        //    }
        //    if (e.Delta < 0)
        //    {
        //        TimeSpan ts = DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, 0);
        //        String t = Convert.ToInt64(ts.TotalMilliseconds).ToString();
        //        // System.Windows.Forms.MessageBox.Show("Wheel Down");
        //        String str = "{\"Wheel\": -1}";
        //        //my_info.key_mouse.mouse = str;   #1020

        //        Console.WriteLine(str + t);
        //    }


        //}


        /// <summary>
        /// 按下鼠标键触发的事件
        /// </summary>
        //private void mh_MouseDownEvent(object sender, MouseEventArgs e)  #1020
        //{
        //    //ClearCanvas.ImageViewer.Layout


        //    p1 = e.Location;
        //    if (e.Button == MouseButtons.Left)
        //    {
        //        LeftTag = true;
        //        String str = "{\"Left\": [" + p1.X + ", " + p1.Y + "]}";
        //        my_info.key_mouse.mouse = str;
        //        //System.Windows.Forms.MessageBox.Show(str);
        //        TimeSpan ts = DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, 0);
        //        String t = Convert.ToInt64(ts.TotalMilliseconds).ToString();
        //        Console.WriteLine(str + ": " + t);
        //    }
        //    if (e.Button == MouseButtons.Right)
        //    {
        //        RightTag = true;
        //        String str = "{\"Right\": [" + p1.X + ", " + p1.Y + "]}";
        //        my_info.key_mouse.mouse = str; 
        //        //System.Windows.Forms.MessageBox.Show(str);
        //        TimeSpan ts = DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, 0);
        //        String t = Convert.ToInt64(ts.TotalMilliseconds).ToString();
        //        Console.WriteLine(str + ": " + t);
        //    }
        //    if (e.Button == MouseButtons.Middle)
        //    {
        //        String str = "{\"Middle\": [" + p1.X + ", " + p1.Y + "]}";
        //        my_info.key_mouse.mouse = str;
        //        //System.Windows.Forms.MessageBox.Show(str);
        //        TimeSpan ts = DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, 0);
        //        String t = Convert.ToInt64(ts.TotalMilliseconds).ToString();
        //        Console.WriteLine(str + ": " + t);
        //    }

        //}




        //松开鼠标键触发的事件
        /*private void mh_MouseUpEvent(object sender, MouseEventArgs e)
        {
            p2 = e.Location;
            double value = Math.Sqrt(Math.Abs(p1.X - p2.X) * Math.Abs(p1.X - p2.X) + Math.Abs(p1.Y - p2.Y) * Math.Abs(p1.Y - p2.Y));
            //if (LeftTag && RightTag && value > 100)
            //{
            //    MessageBox.Show("ok");
            //}
            if (e.Button == MouseButtons.Left)
            {
                //richTextBox1.AppendText("松开了左键\n");
            }
            if (e.Button == MouseButtons.Right)
            {
                //richTextBox1.AppendText("松开了右键\n");
            }
            //richTextBox1.AppendText("移动了" + value + "距离\n");
            RightTag = false;
            LeftTag = false;
            p1 = new Point(0, 0);
            p2 = new Point(0, 0);
        }*/

        /*private void DeskForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            mh.UnHook();
        }*/
    }

    public class aSeePro_StartDLL
    {
        [DllImport(@"aSeeProDLL.dll", EntryPoint = "ShowDlg", SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = false, CallingConvention = CallingConvention.Cdecl)]
        public extern static void ShowDlg();
        [DllImport("kernel32.dll")]
        static extern IntPtr LoadLibrary(string lpFileName);
        [DllImport("kernel32.dll")]
        static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);
        public void TestStart(object flag)
        {
            //string str = "";
            //str += System.Environment.CurrentDirectory;
            //Console.WriteLine(str);
            IntPtr hModule = LoadLibrary("aSeeProDLL.dll");

            if (GetProcAddress(hModule, "ShowDlg") == IntPtr.Zero)
            {
                //throw (new Exception(" 没有找到 : InitInstance这个函数的入口点 "));
                System.Windows.Forms.MessageBox.Show("not load ");
            }
            else
            {
                DesktopForm.Is_aSeePro_running = true;
                ShowDlg();
                DesktopForm.thread.Abort(true);
                DesktopForm.Is_aSeePro_running = false;
            } 
                
        }

    }

    public class Win32Api
    {
        [StructLayout(LayoutKind.Sequential)]
        public class POINT
        {
            public int x;
            public int y;
        }
        //[StructLayout(LayoutKind.Sequential)]
        //public class MouseHookStruct
        //{
        //    public POINT pt;
        //    public int hwnd;
        //    public int wHitTestCode;
        //    public int dwExtraInfo;
        //}
        public delegate int HookProc(int nCode, IntPtr wParam, IntPtr lParam);
        //安装钩子
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hInstance, int threadId);
        //卸载钩子
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern bool UnhookWindowsHookEx(int idHook);
        //调用下一个钩子
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern int CallNextHookEx(int idHook, int nCode, IntPtr wParam, IntPtr lParam);
    }

    //public class MouseHook
    //{
    //    private Point point;
    //    private Point Point
    //    {
    //        get { return point; }
    //        set
    //        {
    //            if (point != value)
    //            {
    //                point = value;
    //                //if (MouseMoveEvent != null)
    //                //{
    //                //    var e = new MouseEventArgs(MouseButtons.None, 0, point.X, point.Y, 0);
    //                //    MouseMoveEvent(this, e);
    //                //}
    //            }
    //        }
    //    }
    //    private int hHook;
    //    private static int hMouseHook = 0;
    //    private const int WM_MOUSEMOVE = 0x200;
    //    private const int WM_LBUTTONDOWN = 0x201;
    //    private const int WM_RBUTTONDOWN = 0x204;
    //    private const int WM_MBUTTONDOWN = 0x207;
    //    private const int WM_LBUTTONUP = 0x202;
    //    private const int WM_RBUTTONUP = 0x205;
    //    private const int WM_MBUTTONUP = 0x208;
    //    private const int WM_LBUTTONDBLCLK = 0x203;
    //    private const int WM_RBUTTONDBLCLK = 0x206;
    //    private const int WM_MBUTTONDBLCLK = 0x209;

    //    public const int WH_MOUSE_LL = 14;
    //    public Win32Api.HookProc hProc;
    //    public MouseHook()
    //    {
    //        this.Point = new Point();
    //    }
    //    public int SetHook()
    //    {
    //        hProc = new Win32Api.HookProc(MouseHookProc);
    //        hHook = Win32Api.SetWindowsHookEx(WH_MOUSE_LL, hProc, IntPtr.Zero, 0);
    //        return hHook;
    //    }
    //    public void UnHook()
    //    {
    //        Win32Api.UnhookWindowsHookEx(hHook);
    //    }
    //    private int MouseHookProc(int nCode, IntPtr wParam, IntPtr lParam)
    //    {
    //        Win32Api.MouseHookStruct MyMouseHookStruct = (Win32Api.MouseHookStruct)Marshal.PtrToStructure(lParam, typeof(Win32Api.MouseHookStruct));
    //        if (nCode < 0)
    //        {
    //            return Win32Api.CallNextHookEx(hHook, nCode, wParam, lParam);
    //        }
    //        else
    //        {
    //            MouseButtons button = MouseButtons.None;
    //            int clickCount = 0;
    //            switch ((Int32)wParam)
    //            {
    //                case WM_LBUTTONDOWN:
    //                    button = MouseButtons.Left;
    //                    clickCount = 1;
    //                    MouseDownEvent(this, new MouseEventArgs(button, clickCount, point.X, point.Y, 0));
    //                    break;
    //                case WM_RBUTTONDOWN:
    //                    button = MouseButtons.Right;
    //                    clickCount = 1;
    //                    MouseDownEvent(this, new MouseEventArgs(button, clickCount, point.X, point.Y, 0));
    //                    break;
    //                case WM_MBUTTONDOWN:
    //                    button = MouseButtons.Middle;
    //                    clickCount = 1;
    //                    MouseDownEvent(this, new MouseEventArgs(button, clickCount, point.X, point.Y, 0));
    //                    break;
    //            }

    //            this.Point = new Point(MyMouseHookStruct.pt.x, MyMouseHookStruct.pt.y);
    //            return Win32Api.CallNextHookEx(hHook, nCode, wParam, lParam);
    //        }
    //    }

    //    //public delegate void MouseMoveHandler(object sender, MouseEventArgs e);
    //    //public event MouseMoveHandler MouseMoveEvent;

    //    //public delegate void MouseClickHandler(object sender, MouseEventArgs e);
    //    //public event MouseClickHandler MouseClickEvent;

    //    public delegate void MouseDownHandler(object sender, MouseEventArgs e);
    //    public event MouseDownHandler MouseDownEvent;

    //    //public delegate void MouseUpHandler(object sender, MouseEventArgs e);
    //    //public event MouseUpHandler MouseUpEvent;
    //}

    public class InfoCollect
    {
        public class Gaze
        {
            public float x = 0;
            public float y = 0;
        }
        public class Timer
        {
            public String now_time = "";
        }
        public class Key_mouse
        {
            public String key = "";
            public String mouse = "";
        }

        public class Info
        {
            public Gaze gaze = new Gaze();
            public Timer time = new Timer();
            public Key_mouse key_mouse = new Key_mouse();
        }

        
    }

    #endregion
}