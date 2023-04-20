﻿using JointWatermark.Class;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace JointWatermark.Views
{
    /// <summary>
    /// ConfigExif.xaml 的交互逻辑
    /// </summary>
    public partial class ConfigExif : Window
    {
        ConfigExifVM vm;
        public ConfigExif()
        {
            InitializeComponent();
            vm = new ConfigExifVM(this);
            DataContext = vm;
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if(sender is CheckBox check && check.DataContext is ExifInfo exif)
            {
                ExifConfigInfo item = new()
                {
                    SEQ = vm.SelectedItem.Config.Count + 1,
                    Key = exif.Key,
                    Value = exif.Value
                };
                if (!vm.SelectedItem.Config.Any(c => c.Key == item.Key))
                {
                    vm.SelectedItem.Config.Add(item);
                }
                vm.RefreshPreviewImage();
            }
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox check && check.DataContext is ExifInfo exif)
            {
                var item = vm.SelectedItem.Config.FirstOrDefault(c => c.Key == exif.Key);
                if(item!= null)
                {
                    vm.SelectedItem.Config.Remove(item);
                }
                vm.RefreshPreviewImage();
            }
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            vm.SaveConfig();
            this.DialogResult = true;
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            vm.RefreshPreviewImage();
        }
    }

    public class ConfigExifVM : ValidationBase
    {
        ConfigExif? window;
        public ConfigExifVM(Window _window)
        {
            window = _window as ConfigExif;
            var model = Global.InitConfig();
            ExifInfoList = new ObservableCollection<ExifInfo>(model.Exifs);
            ItemsSource = new ObservableCollection<LeftTextList>(model.Config);
            SelectedItem = ItemsSource[0];
            RefreshPreviewImage();
        }

        public void RefreshPreviewImage()
        {
            List<string> ls = new();
            foreach(var item in ItemsSource)
            {
                var ps = new List<string>();
                foreach(var i in item.Config)
                {
                    var c = i.Front + i.Value + i.Behind;
                    ps.Add(c);
                }
                var p = string.Join(" ", ps);
                ls.Add(p);
            }
            InitPreviewImage(ls[0], ls[1], ls[2], ls[3]);
        }

        private ObservableCollection<LeftTextList> itemsSource = new();
        public ObservableCollection<LeftTextList> ItemsSource
        {
            get => itemsSource;
            set
            {
                itemsSource = value;
                NotifyPropertyChanged(nameof(ItemsSource));
            }
        }


        private LeftTextList selectedItem = new();
        public LeftTextList SelectedItem
        {
            get => selectedItem;
            set
            {
                selectedItem = value;
                NotifyPropertyChanged(nameof(SelectedItem));
                ConfigedInfo = new ObservableCollection<ExifConfigInfo>(value.Config);
                
                foreach(var item in ExifInfoList)
                {
                    if (ConfigedInfo.Any(c => c.Key == item.Key)) item.IsSelected = true;
                    else item.IsSelected = false;
                }
            }
        }


        private ObservableCollection<ExifInfo> exifInfoList = new();
        /// <summary>
        /// 全部的的EXIF配置信息
        /// </summary>
        public ObservableCollection<ExifInfo> ExifInfoList
        {
            get=> exifInfoList;
            set
            {
                exifInfoList = value;
                NotifyPropertyChanged(nameof(exifInfoList));
            }
        }

        private ObservableCollection<ExifConfigInfo> configedInfo = new();
        /// <summary>
        /// 选中的配置信息
        /// </summary>
        public ObservableCollection<ExifConfigInfo> ConfigedInfo
        {
            get => configedInfo;
            set
            {
                configedInfo = value;
                NotifyPropertyChanged(nameof(ConfigedInfo));
            }
        }



        #region function

        public void SaveConfig()
        {
            var model = new MainModel()
            {
                Exifs = ExifInfoList.ToList(),
                Config = ItemsSource.ToList()
            };
            var json = JsonConvert.SerializeObject(model);
            Global.SaveConfig(json);
        }


        public async void InitPreviewImage(params string[] p)
        {
            var con = new ImageConfig()
            {
                LeftPosition1 = p[0],
                LeftPosition2 = p[1],
                RightPosition1 = p[2],
                RightPosition2= p[3],
                BackgroundColor = "#FFFFF",
                Row1FontColor = "#000000",
                FontFamily = "OpenSans"
            };
            var img = await ImagesHelper.Current.CreateWatermark(con);
            var imgs = ImagesHelper.Current.ImageSharpToImageSource(img);
            img.Dispose();
            window.previewImg.Source = imgs;
        }


        #endregion

    }
}
