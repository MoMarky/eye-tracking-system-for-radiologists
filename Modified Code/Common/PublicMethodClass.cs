using System;
using System.IO;


namespace ClearCanvas.Common {

    public static class PublicMethod
    {
        //当前显示的dcm文件路径
        private static String present_dcmpath;
        //当前显示图片的大BOX相对屏幕的坐标
        private static int presentBoxContainer_x, presentBoxContainer_y, presentBoxContainer_w, presentBoxContainer_h;
        //当前显示图片的小box相对大box的坐标
        private static int presentImageBox_x, presentImageBox_y, presentImageBox_w, presentImageBox_h;
        //当前屏幕的原点和屏幕的大小
        public static int Screen_x, Screen_y, Screen_w, Screen_h;
        //当前图片的偏移量
        private static float presentImage_Offset_x = -1, presentImage_Offset_y = -1;
        //当前图片的原始大小
        private static int presentImage_Source_width = -1, presentImage_Source_height = -1;
        //当前图片 长宽缩放系数
        private static float presentImage_Scale_x = -1, presentImage_Scale_y = -1;

        //判断有动作发生时  何时保存 offset信息
        //public static bool _tileDraw_flag = false, PresentDraw_flag = false;
        //public static int sender_count = 0;

        //保存当前哪一个BOx被选中
        private static int now_selected_image_box_index;


        public static int sp_count = 0;

        public static void SetPresentDcmPath(String path)
        {
            present_dcmpath = path;
        }

        public static String GetPresentDcmPath()
        {
            if (present_dcmpath != null)
                return present_dcmpath;
            else
                return ("-1");
        }

        public static void SetScreenPara(int S_x, int S_y, int S_w, int S_h)
        {
            Screen_x = S_x;
            Screen_y = S_y;
            Screen_w = S_w;
            Screen_h = S_h;
        }
        public static void SetSelectedBoxIndex(int index)
        {
               now_selected_image_box_index = index;
        }

        public static int[] GetScreenPara()
        {
            int[] array = new int[4];
            array[0] = Screen_x;
            array[1] = Screen_y;
            array[2] = Screen_w;
            array[3] = Screen_h;
            return array;
        }
        /// <summary>
        /// 传输 当前小box 相对大Box的位置和大小
        /// </summary>
        /// <param name="Box_x"></param>
        /// <param name="Box_y"></param>
        /// <param name="Box_w"></param>
        /// <param name="Box_h"></param>
        public static void SetPresentImageBox(int Box_x, int Box_y, int Box_w, int Box_h)
        {
            presentImageBox_x = Box_x;
            presentImageBox_y = Box_y;
            presentImageBox_w = Box_w;
            presentImageBox_h = Box_h;

            //box_flag = true;
        }

        public static void SetPresentImageBox_info(int Box_x, int Box_y, int Box_w, int Box_h, int box_index)
        {
            presentImageBox_x = Box_x;
            presentImageBox_y = Box_y;
            presentImageBox_w = Box_w;
            presentImageBox_h = Box_h;
            now_selected_image_box_index = box_index;
            //box_flag = true;
        }

        public static void GetPresentImageBox()
        {
            //box_flag = false;
        }

        public static void SetPresentBoxContainer(int Con_x, int Con_y, int Con_w, int Con_h)
        {
            presentBoxContainer_x = Con_x;
            presentBoxContainer_y = Con_y;
            presentBoxContainer_w = Con_w;
            presentBoxContainer_h = Con_h;
        }

        

        /// <summary>
        /// 传参 图像原始大小参数
        /// </summary>
        /// <param name="Source_w"></param>
        /// <param name="Source_h"></param>
        public static void SetPresentImageSourceSize(int Source_w, int Source_h)
        {
            presentImage_Source_width = Source_w;
            presentImage_Source_height = Source_h;
            //image_flag = true;
        }

        public static void SetPresentImageScaleX(float Scale_x)
        { 
            presentImage_Scale_x = Scale_x;
        }

        public static void SetPresentImageScaleY(float Scale_y)
        {
            presentImage_Scale_y = Scale_y;
        }

        public static void SetPresentImageOffset(float Offset_x, float Offset_y)
        {
            presentImage_Offset_x = Offset_x;
            presentImage_Offset_y = Offset_y;
        }


        /// <summary>
        /// 0:scaleX 1:scaleY 2:offsetx  3:offsety 4:source_w 5:source_h
        /// </summary>
        /// <returns></returns>
        public static float[] GetPresentDcmPara()//float S_x,float S_y, float Offset_x, float Offset_y
        {
            float[] arry2 = new float[10];
            arry2[0] = presentImage_Scale_x;
            arry2[1] = presentImage_Scale_y;
            arry2[2] = presentImage_Offset_x;
            arry2[3] = presentImage_Offset_y;
            arry2[4] = presentImage_Source_width;
            arry2[5] = presentImage_Source_height;
            arry2[6] = presentBoxContainer_x;
            arry2[7] = presentBoxContainer_y;
            arry2[8] = presentBoxContainer_w;
            arry2[9] = presentBoxContainer_h;
            return arry2;
        }

        public static float[] GetPresentImagePos()
        {
            float[] arry2 = new float[20];
            float now_inside_x = -1, now_inside_y = -1;
            float now_size_x = -1, now_size_y = -1;
            //nowinsdie点 是实际图像左上角的点 相对于ImageBox即小Box左上点的坐标
            now_size_x = (float)presentImage_Source_width * presentImage_Scale_x;
            now_size_y = (float)presentImage_Source_height * presentImage_Scale_y;

            now_inside_x = ((float)presentImageBox_w - now_size_x) / 2  + presentImage_Offset_x;
            now_inside_y = ((float)presentImageBox_h - now_size_y) / 2 + presentImage_Offset_y;
            //0 1两个值 为 实际图像左上角点相对屏幕左上角点的位置
            //arry2[0] = now_inside_x + (float)Screen_x + (float)presentBoxContainer_x + (float)presentImageBox_x;
            //arry2[1] = now_inside_y + (float)Screen_y + (float)presentBoxContainer_y + (float)presentImageBox_y;
            arry2[0] = now_inside_x + (float)Screen_x + (float)presentImageBox_x;
            arry2[1] = now_inside_y + (float)Screen_y + (float)presentImageBox_y;
            //2 3两个值 为当前图像的X Y长度
            arry2[2] = now_size_x;
            arry2[3] = now_size_y;
            //4 5为X Y的Scale
            arry2[4] = presentImage_Scale_x;
            arry2[5] = presentImage_Scale_y;
            //6789为Box参数
            arry2[6] = presentImageBox_x;
            arry2[7] = presentImageBox_y;
            arry2[8] = presentImageBox_w;
            arry2[9] = presentImageBox_h;
            //10 当前选中的Image Box 的索引
            arry2[10] = now_selected_image_box_index;
            //11 12 
            arry2[11] = presentImage_Offset_x;
            arry2[12] = presentImage_Offset_y;
            //13 14
            arry2[13] = Screen_x;
            arry2[14] = Screen_y;
            arry2[15] = Screen_w;
            arry2[16] = Screen_h;
            return arry2;
        }


        public static bool SaveLog()
        {
            string now_time_file_name;
            now_time_file_name = ".\\save_log\\" + DateTime.Now.ToString("yyyy-MM-dd") +
                "_" + DateTime.Now.Hour.ToString() + "-" +
                DateTime.Now.Minute.ToString() + "-" +
                DateTime.Now.Second.ToString() + "_Pacs.txt";

            FileInfo flinfo = new FileInfo("info_log.txt");
            flinfo.CopyTo(now_time_file_name, false);
            //if (Directory.Exists(now_time_file_name))
            return true;
            //else
            //    return false;
        } 

    }

    public static class PublicSaveLog
    {
        static StreamWriter csv_writer = null;
        static string now_time_file_name;
        public static bool SetLogSaveWriter()
        {
            if (csv_writer == null)
            {
                string[] files, split_line;
                string strLine, save_root = ""; //找到UserInfoTemp中的保存路径，
                System.IO.FileStream fs = new System.IO.FileStream(".\\EyeTracker\\res\\UserInfoTemp.csv", System.IO.FileMode.Open, System.IO.FileAccess.Read);
                System.IO.StreamReader sr = new System.IO.StreamReader(fs, System.Text.Encoding.UTF8);
                int line_count = 0;
                while ((strLine = sr.ReadLine()) != null)
                {
                    if (line_count == 1)
                    {
                        split_line = strLine.Split(',');
                        save_root = split_line[4];
                    }
                    line_count++;
                }
                sr.Close();
                fs.Close();

                int eid_index = save_root.IndexOf("EID");
                string EID = save_root.Substring(eid_index);
                //now_time_file_name = save_root + "\\" + DateTime.Now.ToString("yyyy-MM-dd") +
                //                    "_" + DateTime.Now.Hour.ToString() + "-" +
                //                    DateTime.Now.Minute.ToString() + "-" +
                //                    DateTime.Now.Second.ToString() + "_Pacs.csv";
                now_time_file_name = save_root + "\\Pacs_" + EID.Replace('_', '-') + ".csv";
                csv_writer = new StreamWriter(now_time_file_name, true, System.Text.Encoding.UTF8);

                csv_writer.Write("timestamp," +
                                 "img_pos_x," +
                                 "img_pos_y," +
                                 "img_size_w," +
                                 "img_size_h," +
                                 "img_scale_x," +
                                 "img_scale_y," +
                                 "box_pos_x," +
                                 "box_pos_y," +
                                 "box_size_w," +
                                 "box_size_h," +
                                 "box_index," +
                                 "dcm_path\r\n"
                                 );
                return true;
            }
            else
                return false;
        }

        public static void wirte_line(string line)
        {
            if (csv_writer != null)
            {
                csv_writer.Write(line);
                csv_writer.Flush();
            }
        }

        public static void wirte_array(string nowtime, float[] write_param, string dcm_img_path)
        {
            if (csv_writer != null)
            {
                string writer_line = nowtime + ",";
                for (int i = 0; i < 11; i++)
                {
                    writer_line += write_param[i] + ",";
                }
                writer_line += dcm_img_path + "\r\n";
                csv_writer.Write(writer_line);
                csv_writer.Flush();
            }
        }
        public static bool close_csv_writer()
        {
            if (csv_writer != null)
            {
                csv_writer.Close();
                return true;
            }
            else
                return false;
        }

    }

    

}

