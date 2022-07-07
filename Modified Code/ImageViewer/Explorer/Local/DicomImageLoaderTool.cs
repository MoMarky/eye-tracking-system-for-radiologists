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

using System;
using System.Collections.Generic;
using System.IO;
using ClearCanvas.Common;
using ClearCanvas.Common.Utilities;
using ClearCanvas.Desktop;
using ClearCanvas.Desktop.Actions;
using ClearCanvas.Desktop.Tools;
using ClearCanvas.ImageViewer.Configuration;
using ClearCanvas.ImageViewer.StudyManagement;

namespace ClearCanvas.ImageViewer.Explorer.Local
{
	[MenuAction("Open", "explorerlocal-contextmenu/MenuOpenFiles", "Open")]
	[Tooltip("Open", "OpenDicomFilesVerbose")]
	[IconSet("Open", "Icons.OpenToolSmall.png", "Icons.OpenToolMedium.png", "Icons.OpenToolLarge.png")]
	[EnabledStateObserver("Open", "Enabled", "EnabledChanged")]
	//
	[ExtensionOf(typeof (LocalImageExplorerToolExtensionPoint))]
	public class DicomImageLoaderTool : Tool<ILocalImageExplorerToolContext>
	{
		private bool _enabled;
		private event EventHandler _enabledChanged;

		/// <summary>
		/// Default constructor.  A no-args constructor is required by the
		/// framework.  Do not remove.
		/// </summary>
		public DicomImageLoaderTool()
		{
			_enabled = true;
		}

		/// <summary>
		/// Called by the framework to initialize this tool.
		/// </summary>
		public override void Initialize()
		{
			base.Initialize();
			this.Context.DefaultActionHandler = Open;
			Context.SelectedPathsChanged += OnContextSelectedPathsChanged;
		}

		protected override void Dispose(bool disposing)
		{
			Context.SelectedPathsChanged -= OnContextSelectedPathsChanged;
			base.Dispose(disposing);
		}

		/// <summary>
		/// Called to determine whether this tool is enabled/disabled in the UI.
		/// </summary>
		public bool Enabled
		{
			get { return _enabled; }
			protected set
			{
				if (_enabled != value)
				{
					_enabled = value;
					EventsHelper.Fire(_enabledChanged, this, EventArgs.Empty);
				}
			}
		}

		/// <summary>
		/// Notifies that the Enabled state of this tool has changed.
		/// </summary>
		public event EventHandler EnabledChanged
		{
			add { _enabledChanged += value; }
			remove { _enabledChanged -= value; }
		}

		public void Open()
		{
			string[] files;
			try
			{
				files = BuildFileList();
			}
			catch (Exception ex)
			{
				ExceptionHandler.Report(ex, SR.MessageUnableToOpenImages, Context.DesktopWindow);
				return;
			}

			if (files.Length == 0)
			{
				Context.DesktopWindow.ShowMessageBox(SR.MessageNoFilesSelected, MessageBoxActions.Ok);
				return;
			}

			try
			{
				new OpenFilesHelper(files) {WindowBehaviour = ViewerLaunchSettings.WindowBehaviour}.OpenFiles();
			}
			catch (Exception e)
			{
				ExceptionHandler.Report(e, SR.MessageUnableToOpenImages, Context.DesktopWindow);
			}
		}

        private string[] BuildFileList()
		{
			List<string> fileList = new List<string>();

			foreach (string path in this.Context.SelectedPaths)
			{
				if (string.IsNullOrEmpty(path))
					continue;

				if (File.Exists(path))
					fileList.Add(path);
				else if (Directory.Exists(path))
					fileList.AddRange(Directory.GetFiles(path, "*.*", SearchOption.AllDirectories));
			}

			return fileList.ToArray();
		}

		private void OnContextSelectedPathsChanged(object sender, EventArgs e)
		{
			Enabled = Context.SelectedPaths.Count > 0;
		}

        public int Open_Materials(string set_path, int show_index, bool initial)
        {
            show_index = show_index - 1;
            string[] files, split_line;
            List<string> root_path_list = new List<string>(); //记录csv中所有 路径
            List<int> row_line_list = new List<int>();  //记录 csv中所有 窗口布局的 行数
            List<int> col_line_list = new List<int>();  //记录 csv中所有 窗口布局的 列数
            List<string> fileList = new List<string>();
            string strLine;
            System.IO.FileStream fs = new System.IO.FileStream(set_path, System.IO.FileMode.Open, System.IO.FileAccess.Read);
            System.IO.StreamReader sr = new System.IO.StreamReader(fs, System.Text.Encoding.UTF8);
            int line_count = 0;
            while ((strLine = sr.ReadLine()) != null)
            {
                if (line_count != 0)
                {
                    split_line = strLine.Split(',');
                    root_path_list.Add(split_line[0]);

                    row_line_list.Add(int.Parse(split_line[1]));
                    col_line_list.Add(int.Parse(split_line[2]));
                }
                line_count++;
            }
            sr.Close();
            fs.Close();

            if (show_index > root_path_list.Count-1)
                return 3;  //索引超过最大病例数，返回flag=3
            if (show_index < 0)
                return 2;  //索引低于0，返回flag=2

            DirectoryInfo TheFolder = new DirectoryInfo(root_path_list[show_index]);
            foreach (FileInfo NextFile in TheFolder.GetFiles())
                fileList.Add(TheFolder.FullName + "\\" + NextFile.Name);

            files = fileList.ToArray();
            if (files.Length == 0)
            {
                Context.DesktopWindow.ShowMessageBox(SR.MessageNoFilesSelected, MessageBoxActions.Ok);
                return 0;
            }

            try
            {
                PhysicalWorkspace.show_row = row_line_list[show_index];
                PhysicalWorkspace.show_col = col_line_list[show_index];
                new OpenFilesHelper(files) { WindowBehaviour = ViewerLaunchSettings.WindowBehaviour }.OpenFiles();

                //将当前图像PID的目录 写入temp_info.csv中
                System.IO.FileStream temp_now_path_fs = new System.IO.FileStream(".\\EyeTracker\\res\\temp_now_img.csv", System.IO.FileMode.Create, System.IO.FileAccess.Write);
                System.IO.StreamWriter temp_writer = new System.IO.StreamWriter(temp_now_path_fs);
                temp_writer.WriteLine(root_path_list[show_index]);
                temp_writer.Close();
                temp_now_path_fs.Close();      

                if (initial)
                    return root_path_list.Count;
                else
                    return 1;
            }
            catch (Exception e)
            {
                ExceptionHandler.Report(e, SR.MessageUnableToOpenImages, Context.DesktopWindow);
                return 0;
            }
        }

    }
}