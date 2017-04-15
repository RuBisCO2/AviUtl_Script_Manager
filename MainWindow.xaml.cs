using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Data;
using System.IO;
using System.Text;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Controls;
using System.Linq;
using System.Text.RegularExpressions;

namespace AviUtlScriptManager
{
    public partial class MainWindow : Window
    {
		private ObservableCollection<Script> lists;
		private ObservableCollection<Script> templist;
		const string TEMP_FOLDER = "temp";
        private string _tempFolder;
		ObservableCollection<FileEntry> files = new ObservableCollection<FileEntry>();
		string crdr = Directory.GetCurrentDirectory();
		
        public MainWindow()
        {
            InitializeComponent();
			string csv_name = "script.csv";
			if (!File.Exists(csv_name)){
				MessageBox.Show("Can't open script.csv");
			}
			StreamReader reader = new StreamReader(csv_name,Encoding.GetEncoding("Shift_JIS"));
			string line;
			lists = new ObservableCollection<Script>();
			while((line = reader.ReadLine()) != null)
			{
				string[] data = line.Split(new string[]{",,"}, StringSplitOptions.None);

				lists.Add(new Script { 
					Name = data[0], Type = data[1], Path = data[2], Track0 = data[3], Track1 = data[4], Track2= data[5],
					Track3 = data[6], Check0 = data[7], TypeNum = data[8], Filter = data[9], Param = data[10]
				});
			}
			reader.Close();
            lvScripts.ItemsSource = lists;

            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(lvScripts.ItemsSource);
            view.Filter = UserFilter;
			
			templist = new ObservableCollection<Script>();
			lvClipBoard.ItemsSource = templist;
			
			TempDragSrc.ItemsSource = files;
			CreatenInitApplicationSpecificTempFolder();
        }
		
		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) 
		{
			for(int i=0;i < files.Count;i++)
			{
				File.Delete(files[i].FilePath);
			}
        }
		
		private void CreatenInitApplicationSpecificTempFolder()
        {
			_tempFolder = Path.Combine(crdr, TEMP_FOLDER);
			if (!(System.IO.Directory.Exists(_tempFolder)))
			{
				Directory.CreateDirectory(_tempFolder);
			}
        }
		
		private bool UserFilter(object item)
        {
            if(String.IsNullOrEmpty(txtFilter.Text))
                    return true;
            else
                    return ((item as Script).Name.IndexOf(txtFilter.Text, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private void txtFilter_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            CollectionViewSource.GetDefaultView(lvScripts.ItemsSource).Refresh();
        }
		
		private void List_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			ListViewItem item = sender as ListViewItem;
			//if (item == null || item.IsSelected)
			if (item == null)
			{
				return;
			}
			Script file = item.DataContext as Script;
			if (file != null)
            {
				string fullPath = string.Format(@"{0}\{1}", crdr, file.Path);
                DataObject dragObj = new DataObject();
                dragObj.SetFileDropList(new System.Collections.Specialized.StringCollection() { fullPath });
                DragDrop.DoDragDrop(item, dragObj, DragDropEffects.Copy);
            }
		}
		
		private void AddButton_Click(object sender, RoutedEventArgs e)
		{
			if (lvScripts.SelectedItems.Count > 0){
				foreach (Script selectscript in lvScripts.SelectedItems)
				{
					//Script selectscript = (Script)lvScripts.SelectedItems[0];
					templist.Add(selectscript);
				}
			}
		}
		
		private void RemoveButton_Click(object sender, RoutedEventArgs e)
		{
			if (lvClipBoard.SelectedItems.Count < 1) return;

			if(lvClipBoard.SelectedItems.Count == 1)
			{
				Script selectscript = (Script)lvClipBoard.SelectedItem;
				templist.Remove(selectscript);
			}
		}
		
		private void ClearButton_Click(object sender, RoutedEventArgs e)
		{
			templist.Clear();
		}
		
		private void CreateButton_Click(object sender, RoutedEventArgs e)
		{
			if(templist.Count > 0)
			{
				string tempname = "";
				string ori_name = "";
				char[] invalidChars = Path.GetInvalidFileNameChars();
				int i;
				int chk = 0;
				var exedit = new Regex(@"^exedit");
				var filter = new Regex(@"^(?<filter>.+)@filter");
				for(i=0;i < templist.Count;i++)
				{
					if(i > 0)
					{
						if(templist[i].Type == "obj")
						{
							MessageBox.Show("カスタムオブジェクトの配置に問題があります","エラーメッセージ");
							return;
						}
					}
					if(templist[0].Type == "obj"){
						chk = 1;
					}
					ori_name = templist[i].Name;
					tempname += string.Concat(ori_name.Select(c => invalidChars.Contains(c) ? '_' : c));
					if((i+1) != templist.Count)
					{
						tempname += "_";
					}
				}
				if(chk==0)
				{
					using(StreamWriter w = new StreamWriter(string.Format(@"{0}\{1}.exa", _tempFolder, tempname), false, System.Text.Encoding.GetEncoding("shift_jis")))
					{
						w.WriteLine("[vo.0]");
						w.WriteLine("_name=図形");
						w.WriteLine("サイズ=100");
						w.WriteLine("縦横比=0.0");
						w.WriteLine("ライン幅=4000");
						w.WriteLine("type=1");
						w.WriteLine("color=ffffff");
						w.WriteLine("name=");
						for(i=0;i < templist.Count;i++)
						{
							if(templist[i].Type != "filter")
							{
								w.Write("[vo.{0}]\r\n_name=アニメーション効果\r\n", i+1);
								w.Write("track0={0:f2}\r\ntrack1={1:f2}\r\ntrack2={2:f2}\r\ntrack3={3:f2}\r\n", templist[i].Track0, templist[i].Track1, templist[i].Track2, templist[i].Track3);
								if(exedit.Match(templist[i].Path).Success){
									w.Write("check0={0}\r\ntype={1}\r\nfilter=0\r\nname=\r\nparam={2}\r\n", templist[i].Check0, templist[i].TypeNum, templist[i].Param);
								} else {
									w.Write("check0={0}\r\ntype={1}\r\nfilter=0\r\nname={2}\r\nparam={3}\r\n", templist[i].Check0, templist[i].TypeNum, templist[i].Name, templist[i].Param);
								}
							} else {
								w.Write("[vo.{0}]\r\n_name={1}", i+1, filter.Match(templist[i].Name).Groups["filter"].Value);
								if(templist[i].Param != "")
								{
									w.Write("\r\n");
									string[] prms = templist[i].Param.Split(';');
									for(int j=0;j < prms.Length;j++)
									{
										if((j+1) != prms.Length)
										{
											w.Write("{0}\r\n", prms[j]);
										} else {
											w.Write("{0}", prms[j]);
										}
									}
								} else {
									w.Write("\r\n");
								}
							}
						}
						int lastnum = templist.Count + 1;
						w.Write("[vo.{0}]\r\n", lastnum);
						w.WriteLine("_name=標準描画");
						w.WriteLine("X=0.0");
						w.WriteLine("Y=0.0");
						w.WriteLine("Z=0.0");
						w.WriteLine("拡大率=100.00");
						w.WriteLine("透明度=0.0");
						w.WriteLine("回転=0.00");
						w.WriteLine("blend=0");
					}
				} else {
					using(StreamWriter w = new StreamWriter(string.Format(@"{0}\{1}.exa", _tempFolder, tempname), false, System.Text.Encoding.GetEncoding("shift_jis")))
					{
						w.Write("[vo.0]\r\n_name=カスタムオブジェクト\r\ntrack0={0:f2}\r\ntrack1={1:f2}\r\ntrack2={2:f2}\r\ntrack3={3:f2}\r\n", templist[0].Track0, templist[0].Track1, templist[0].Track2, templist[0].Track3);
						if(exedit.Match(templist[0].Path).Success){
							w.Write("check0={0}\r\ntype={1}\r\nfilter=0\r\nname=\r\nparam={2}\r\n", templist[0].Check0, templist[0].TypeNum, templist[0].Param);
						} else {
							w.Write("check0={0}\r\ntype={1}\r\nfilter=0\r\nname={2}\r\nparam={3}\r\n", templist[0].Check0, templist[0].TypeNum, templist[0].Name, templist[0].Param);
						}
						for(i=1;i < templist.Count;i++)
						{
							if(templist[i].Type != "filter")
							{
								w.Write("[vo.{0}]\r\n_name=アニメーション効果\r\n", i);
								w.Write("track0={0:f2}\r\ntrack1={1:f2}\r\ntrack2={2:f2}\r\ntrack3={3:f2}\r\n", templist[i].Track0, templist[i].Track1, templist[i].Track2, templist[i].Track3);
								if(exedit.Match(templist[i].Path).Success){
									w.Write("check0={0}\r\ntype={1}\r\nfilter=0\r\nname=\r\nparam={2}\r\n", templist[i].Check0, templist[i].TypeNum, templist[i].Param);
								} else {
									w.Write("check0={0}\r\ntype={1}\r\nfilter=0\r\nname={2}\r\nparam={3}\r\n", templist[i].Check0, templist[i].TypeNum, templist[i].Name, templist[i].Param);
								}
							} else {
								w.Write("[vo.{0}]\r\n_name={1}", i, filter.Match(templist[i].Name).Groups["filter"].Value);
								if(templist[i].Param != "")
								{
									w.Write("\r\n");
									string[] prms = templist[i].Param.Split(';');
									for(int j=0;j < prms.Length;j++)
									{
										if((j+1) != prms.Length)
										{
											w.Write("{0}\r\n", prms[j]);
										} else {
											w.Write("{0}", prms[j]);
										}
									}
								} else {
									w.Write("\r\n");
								}
							}
						}
						w.Write("[vo.{0}]\r\n", templist.Count);
						w.WriteLine("_name=標準描画");
						w.WriteLine("X=0.0");
						w.WriteLine("Y=0.0");
						w.WriteLine("Z=0.0");
						w.WriteLine("拡大率=100.00");
						w.WriteLine("透明度=0.0");
						w.WriteLine("回転=0.00");
						w.WriteLine("blend=0");
					}
				}
				string relpath = string.Format(@"{0}\{1}.exa", TEMP_FOLDER, tempname);
				files.Add(new FileEntry() { FileName = tempname, FilePath = relpath });
			}
		}
		
		private void Grid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            FrameworkElement senderElement = (sender as FrameworkElement);
            if (senderElement != null)
            {
                FileEntry file = senderElement.DataContext as FileEntry;
                if (file != null)
                {
                    string fullPath = string.Format(@"{0}\{1}", crdr, file.FilePath);
                    DataObject dragObj = new DataObject();
                    dragObj.SetFileDropList(new System.Collections.Specialized.StringCollection() { fullPath });
                    DragDrop.DoDragDrop(senderElement, dragObj, DragDropEffects.Copy);
                }
            }
        }
		
		private void BRemoveButton_Click(object sender, RoutedEventArgs e)
		{
			if (TempDragSrc.SelectedItems.Count < 1) return;
            
			if(TempDragSrc.SelectedItems.Count == 1)
			{
				FileEntry selectfile = (FileEntry)TempDragSrc.SelectedItem;
				files.Remove(selectfile);
			}
		}
		
		private void BClearButton_Click(object sender, RoutedEventArgs e)
		{
			files.Clear();
		}
    }
	
	public class Script
	{
		public string Name {get; set;}
		public string Type {get; set;}
		public string Path {get; set;}
		public string Track0 {get; set;}
		public string Track1 {get; set;}
		public string Track2 {get; set;}
		public string Track3 {get; set;}
		public string Check0 {get; set;}
		public string TypeNum {get; set;}
		public string Filter {get; set;}
		public string Param {get; set;}
	}
	
	public class FileEntry
    {
        public string FileName { get; set; }
        public string FilePath { get; set; }
    }
}