using System;
using System.Collections;
using System.IO;
//using System.Linq;
using System.Threading;
using System.Windows.Forms;
using System.Security.Cryptography;
using System.Drawing;


namespace Windows_FIleMonitor
{


    public partial class Form : System.Windows.Forms.Form
    {
        object thisLock = new Object();

        private string foldPath = null; //监控路径
        private string savePath = null; //备份路径
        private FileSystemWatcher fsWather;
        private Hashtable hstbWather;

        private delegate void setLogTextDelegate(FileSystemEventArgs e, string outlog);
        private delegate void setoutLogTextDelegate(string outlog);
        public Form()
        {
            InitializeComponent();
        }
        private class LogMessage
        {
            public string Message { get; set; }
            public Color Color { get; set; }
        }
        private void btnSelectFile_Click(object sender, EventArgs e)
        {
            openFileDialog1.Multiselect = false;
            openFileDialog1.FileName = "";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                TxtPath.Text = openFileDialog1.FileName;
                TxtResult.Text = "";
            }
        }
        private void btEcryption_Click(object sender, EventArgs e)
        {
            if (!File.Exists(TxtPath.Text))
            {
                MessageBox.Show("文件不存在！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            MD5 md5 = MD5.Create();
            using (FileStream fs = File.OpenRead(TxtPath.Text))
            {
                string s = string.Empty;
                byte[] b = md5.ComputeHash(fs);
                for (int i = 0; i < b.Length; i++)
                {
                    s += b[i].ToString("x2");
                }
                TxtResult.Text = s;
            }
            md5.Clear();
        }
        private bool autoDeleteEnabled = false; // 添加一个标志，用于表示是否启用自动删除
        

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            autoDeleteEnabled = checkBox1.Checked;
        }

        private void DelFileThread(object filepath)
        {
            while (true)
            {
                Thread.Sleep(1000);
                try
                {
                    if (autoDeleteEnabled && File.Exists(filepath.ToString()))
                    {
                        File.Delete(filepath.ToString());
                    }
                    if (autoDeleteEnabled)
                    {
                        this.richTextBox2.Invoke(new setoutLogTextDelegate(addeventlog), new object[] { "删除新文件:" + filepath.ToString() + "\r\n" });
                    
                    }
                    break;
                }
                catch (System.Reflection.TargetParameterCountException ex)
                {
                    Console.WriteLine("捕获到异常：" + ex.Message);
                    // 可以添加其他处理逻辑
                    throw; // 将异常重新抛出
                }
            }
        }

    



    //修改后拷贝原始文件
    private void CopySrcFileThread(object filepath)
        {
            while (true)
            {
                Thread.Sleep(1000);
                try
                {
                    if (File.Exists(filepath.ToString()))
                    {
                        File.Delete(filepath.ToString());
                    }
                    //this.richTextBox2.Invoke(new setoutLogTextDelegate(addeventlog), new object[] { "删除新文件:" + filepath.ToString() + "\n" });
                    break;
                }
                catch
                {
                    continue;
                }

            }

            lock (thisLock)
            {
                string temppath = null;
                temppath = filepath.ToString();
                temppath = temppath.Replace(foldPath, savePath);
                try
                {
                    Stop();
                    File.Copy(temppath, filepath.ToString(), true);
                    this.richTextBox2.Invoke(new setoutLogTextDelegate(addeventlog), new object[] { "拷贝原始文件:" + filepath.ToString() + "\n" });
                    Thread.Sleep(1000);
                    Start();
                }
                catch
                {
                    this.richTextBox2.Invoke(new setoutLogTextDelegate(addeventlog), new object[] { "拷贝原始文件失败，检查下文件监控日志，判断文件是是否被修改！\n" });
                }
            }
        }
        private void addwatherlog(FileSystemEventArgs e, string outlog)
        {

            this.richTextBox1.Focus();//获取焦点
            this.richTextBox1.Select(this.richTextBox2.TextLength, 0);//光标定位到文本最后
            this.richTextBox1.AppendText(outlog);
            this.richTextBox1.ScrollToCaret();//滚动到光标处
        }

        private void addeventlog(string outlog)
        {

            this.richTextBox2.Focus();//获取焦点
            this.richTextBox2.Select(this.richTextBox2.TextLength, 0);//光标定位到文本最后
            this.richTextBox2.AppendText(outlog);
            this.richTextBox2.ScrollToCaret();//滚动到光标处
        }

        public void CopyDirectory(string sourceDirName, string destDirName)
        {
            try
            {
                if (!Directory.Exists(destDirName))
                {
                    Directory.CreateDirectory(destDirName);
                    File.SetAttributes(destDirName, File.GetAttributes(sourceDirName));

                }

                if (destDirName[destDirName.Length - 1] != Path.DirectorySeparatorChar)
                    destDirName = destDirName + Path.DirectorySeparatorChar;

                string[] files = Directory.GetFiles(sourceDirName);
                foreach (string file in files)
                {
                    if (File.Exists(destDirName + Path.GetFileName(file)))
                        continue;
                    File.Copy(file, destDirName + Path.GetFileName(file), true);
                    File.SetAttributes(destDirName + Path.GetFileName(file), FileAttributes.Normal);
                }

                string[] dirs = Directory.GetDirectories(sourceDirName);
                foreach (string dir in dirs)
                {
                    CopyDirectory(dir, destDirName + Path.GetFileName(dir));
                }
            }
            catch (Exception ex)
            {
                StreamWriter sw = new StreamWriter(Application.StartupPath + "\\log.txt", true);
                sw.Write(ex.Message + "     " + DateTime.Now + "\r\n");
                sw.Close();
            }
        }

        private void addlog(string outlog)
        {

            this.richTextBox2.Focus();//获取焦点
            this.richTextBox2.Select(this.richTextBox2.TextLength, 0);//光标定位到文本最后
            this.richTextBox2.AppendText(outlog);
            this.richTextBox2.ScrollToCaret();//滚动到光标处
        }

        public void MyFileSystemWather(string path)
        {

            hstbWather = new Hashtable();
            fsWather = new FileSystemWatcher(path);
            // 是否监控子目录
            fsWather.IncludeSubdirectories = true;
            fsWather.Filter = "*.*";

            fsWather.Renamed += new RenamedEventHandler(fsWather_Renamed);
            fsWather.Changed += new FileSystemEventHandler(fsWather_Changed);
            fsWather.Created += new FileSystemEventHandler(fsWather_Created);
            fsWather.Deleted += new FileSystemEventHandler(fsWather_Deleted);


            //fsWather.EnableRaisingEvents = true;
            fsWather.NotifyFilter = NotifyFilters.Attributes | NotifyFilters.CreationTime | NotifyFilters.DirectoryName | NotifyFilters.FileName | NotifyFilters.LastAccess
                                   | NotifyFilters.LastWrite | NotifyFilters.Security | NotifyFilters.Size;

        }

        private void Start()
        {

            fsWather.EnableRaisingEvents = true;
        }

        private void Stop()
        {
            try
            {
                fsWather.EnableRaisingEvents = false;
            }
            catch
            {
                return;
            }
        }
        /*
          private void fsWather_Created(object sender, FileSystemEventArgs e)
          {
              string createdInfo = string.Format("[创建]: {0}" + "\r\n", e.FullPath);
              this.richTextBox1.Invoke(new setLogTextDelegate(addwatherlog), new object[] { e, createdInfo });
              Thread thread = new Thread(new ParameterizedThreadStart(DelFileThread));
              thread.Start((object)e.FullPath);

          }

          private void fsWather_Renamed(object sender, RenamedEventArgs e)
          {
              string createdInfo = string.Format("[命名]: {0}" + "\r\n", e.FullPath);
              this.richTextBox1.Invoke(new setLogTextDelegate(addwatherlog), new object[] { e, createdInfo });
          }



          private void fsWather_Deleted(object sender, FileSystemEventArgs e)
          {
              string createdInfo = string.Format("[删除]: {0}" + "\r\n", e.FullPath);
              this.richTextBox1.Invoke(new setLogTextDelegate(addwatherlog), new object[] { e, createdInfo });
          }

          private void fsWather_Changed(object sender, FileSystemEventArgs e)
          {
              string createdInfo = string.Format("[修改]: {0}" + "\r\n", e.FullPath);

              this.richTextBox1.Invoke(new setLogTextDelegate(addwatherlog), new object[] { e, createdInfo });
              string temppath = null;
              temppath = e.FullPath;
              temppath = temppath.Replace(foldPath, savePath);
              //文件原来存在，才处理拷贝原文件过程
              if (File.Exists(temppath))
              {
                  Thread thread = new Thread(new ParameterizedThreadStart(CopySrcFileThread));
                  thread.Start((object)e.FullPath);
              }
          }
         */
        private void fsWather_Created(object sender, FileSystemEventArgs e)
        {
            string createdInfo = string.Format("[创建]: {0}\n", e.FullPath);
            addwatherlog(e, createdInfo, Color.Green); // 使用绿色表示创建事件
            Thread thread = new Thread(new ParameterizedThreadStart(DelFileThread));
            thread.Start((object)e.FullPath);
        }

        private void fsWather_Renamed(object sender, RenamedEventArgs e)
        {
            string renamedInfo = string.Format("[命名]: {0}\n", e.FullPath);
            addwatherlog(e, renamedInfo, Color.Yellow); // 使用黄色表示重命名事件
        }

        private void fsWather_Deleted(object sender, FileSystemEventArgs e)
        {
            string deletedInfo = string.Format("[删除]: {0}\n", e.FullPath);
            addwatherlog(e, deletedInfo, Color.Red); // 使用红色表示删除事件
        }

        private void fsWather_Changed(object sender, FileSystemEventArgs e)
        {
            string changedInfo = string.Format("[修改]: {0}\n", e.FullPath);
            addwatherlog(e, changedInfo, Color.Blue); // 使用蓝色表示修改事件
            string temppath = null;
            temppath = e.FullPath;
            temppath = temppath.Replace(foldPath, savePath);
            // 文件原来存在，才处理拷贝原文件过程
            if (File.Exists(temppath))
            {
                Thread thread = new Thread(new ParameterizedThreadStart(CopySrcFileThread));
                thread.Start((object)e.FullPath);
            }
        }

        private void addwatherlog(FileSystemEventArgs e, string message, Color color)
        {
            this.richTextBox1.Invoke((MethodInvoker)delegate {
                this.richTextBox1.SelectionStart = this.richTextBox1.TextLength;
                this.richTextBox1.SelectionColor = color;
                this.richTextBox1.AppendText(message);
                this.richTextBox1.ScrollToCaret();
                this.richTextBox1.SelectionColor = this.richTextBox1.ForeColor; // 恢复默认颜色
            });
        }





        private void button1_Click(object sender, EventArgs e)
        {


            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.Description = "请选择文件路径";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                foldPath = dialog.SelectedPath;
            }
            else
            {
                return;
            }

            this.textBox1.Text = foldPath;
            MessageBox.Show("选择要备份到的文件夹");
            FolderBrowserDialog savadialog = new FolderBrowserDialog();
            savadialog.Description = "请选择文件路径";
            if (savadialog.ShowDialog() == DialogResult.OK)
            {
                savePath = savadialog.SelectedPath;
                //this.textBox1.Text = foldPath;
            }
            else
            {
                return;
            }

            this.button2.Enabled = true;
            this.button1.Enabled = false;
            addlog("监控路径为:" + foldPath + "\r\n");
            CopyDirectory(foldPath, savePath);
            addlog("备份路径为:" + savePath + "\r\n");
            MyFileSystemWather(foldPath);
            Start();

        }

        private void button2_Click(object sender, EventArgs e)
        {
            Stop();
            this.button1.Enabled = true;
            this.button2.Enabled = false;
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void richTextBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void TxtPath_TextChanged(object sender, EventArgs e)
        {

        }

        private void TxtResult_TextChanged_1(object sender, EventArgs e)
        {

        }

        private void textBox4_TextChanged_1(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {



        }



        private void button4_Click(object sender, EventArgs e)
        {
            SettingsForm settingsForm = new SettingsForm();
            settingsForm.ShowDialog();
        }
    }
}
