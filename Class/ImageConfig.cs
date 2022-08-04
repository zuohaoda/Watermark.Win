﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JointWatermark.Class
{
    public class ImageConfig : ValidationBase
    {
        private bool showBrandName;
        /// <summary>
        /// 是否显示品牌名
        /// </summary>
        public bool ShowBrandName
        {
            get => showBrandName;
            set 
            {
                showBrandName = value;
                NotifyPropertyChanged(nameof(ShowBrandName));
            }
        }


        private string leftPosition1;
        /// <summary>
        /// 左侧第一行文字
        /// </summary>
        public string LeftPosition1
        {
            get => leftPosition1;
            set
            {
                leftPosition1 = value;
                NotifyPropertyChanged(nameof(LeftPosition1));
            }
        }


        private string leftPosition2;
        /// <summary>
        /// 左侧第二行文字
        /// </summary>
        public string LeftPosition2
        {
            get => leftPosition2;
            set
            {
                leftPosition2 = value;
                NotifyPropertyChanged(nameof(LeftPosition2));
            }
        }

        private string logoName;
        /// <summary>
        /// LOGO名
        /// </summary>
        public string LogoName
        {
            get=> logoName;
            set
            {
                logoName = value;
                NotifyPropertyChanged(nameof(LogoName));
            }
        }


        private string rightPosition1;
        /// <summary>
        /// 右侧第一行文字
        /// </summary>
        public string RightPosition1
        {
            get => rightPosition1;
            set
            {
                rightPosition1 = value;
                NotifyPropertyChanged(nameof(RightPosition1));
            }
        }

        private string rightPosition2;
        /// <summary>
        /// 右侧第二行文字
        /// </summary>
        public string RightPosition2
        {
            get => rightPosition2;
            set
            {
                rightPosition2 = value;
                NotifyPropertyChanged(nameof(RightPosition2));
            }
        }


        private int borderWidth;
        /// <summary>
        /// 边框宽度 (%)
        /// </summary>
        public int BorderWidth
        {
            get=> borderWidth;
            set
            {
                borderWidth = value;
                NotifyPropertyChanged(nameof(BorderWidth));
            }
        }


        private string backgroundColor;
        /// <summary>
        /// 背景底色
        /// </summary>
        public string BackgroundColor
        {
            get => backgroundColor;
            set
            {
                backgroundColor = value;
                NotifyPropertyChanged(nameof(BackgroundColor));
            }
        }

    }
}