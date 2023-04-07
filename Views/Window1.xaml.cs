﻿using JointWatermark.Class;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace JointWatermark.Views
{
    /// <summary>
    /// Window1.xaml 的交互逻辑
    /// </summary>
    public partial class Window1 : Window
    {
        GeneralWatermarkProperty image;
        public Window1()
        {
            InitializeComponent();
            var c = DateTime.Now.ToString("yyyy.MM.dd HH.mm.ss");
            Init();
        }

        private void Init()
        {
            image = new GeneralWatermarkProperty();
            image.PhotoPath = "C:\\Users\\Jiang\\Pictures\\bb.jpg";
            image.StartPosition = new SixLabors.ImageSharp.Point(10, 5);
            image.PecentOfHeight = 77;
            image.PecentOfWidth = 80;
            image.EnableFixedPercent = true;
            image.Shadow = new ImageShadow(true, 200);
            image.Properties = new List<GeneralWatermarkRowProperty>
            {
                new GeneralWatermarkRowProperty()
                {
                    Name = "右侧第一行",
                    X = PositionBase.Right,
                    Y = PositionBase.Center,
                    EdgeDistanceType = EdgeDistanceType.Character,
                    EdgeDistanceCharacterX = "ABCD",
                    EdgeDistanceCharacterY = "AA",
                    Content = "cesiumcesium测试",
                    Start = WatermarkRange.BottomOfPhoto,
                    End = WatermarkRange.End,
                    IsBold = true,
                    FontSize = 35
                },
                new GeneralWatermarkRowProperty()
                {
                    Name = "右侧第二行",
                    X = PositionBase.Right,
                    Y = PositionBase.Center,
                    EdgeDistanceType = EdgeDistanceType.Character,
                    EdgeDistanceCharacterX = "ABCD",
                    EdgeDistanceCharacterY = "AA",
                    Content = "cesiumcesium测试",
                    Start = WatermarkRange.BottomOfPhoto,
                    End = WatermarkRange.End,
                    IsBold = false,
                    Color = "#cbb795"
                },
                new GeneralWatermarkRowProperty()
                {
                    X = PositionBase.Right,
                    Y = PositionBase.Center,
                    EdgeDistanceType = EdgeDistanceType.Character,
                    EdgeDistanceCharacterX = "ABCD",
                    EdgeDistanceCharacterY = "AA",
                    Content = "右侧cesiumcesium测试",
                    Start = WatermarkRange.BottomOfPhoto,
                    End = WatermarkRange.End,
                    IsBold = true,
                    FontSize = 35,
                    ImagePath = "C:\\Users\\Jiang\\Pictures\\t01a29dac4bb27f7e22.png",
                    ImagePercentOfRange = 50,
                    ContentType = ContentType.Image
                },
                new GeneralWatermarkRowProperty()
                {
                    Name = "左侧第一行",
                    X = PositionBase.Left,
                    Y = PositionBase.Center,
                    EdgeDistanceType = EdgeDistanceType.Character,
                    EdgeDistanceCharacterX = "ABCD",
                    EdgeDistanceCharacterY = "AA",
                    Content = "右侧cesiumcesium测试右侧cesiumcesium",
                    Start = WatermarkRange.BottomOfPhoto,
                    End = WatermarkRange.End,
                    IsBold = true
                },
                new GeneralWatermarkRowProperty()
                {
                    Name = "右侧第二行",
                    X = PositionBase.Left,
                    Y = PositionBase.Center,
                    EdgeDistanceType = EdgeDistanceType.Character,
                    EdgeDistanceCharacterX = "AAA",
                    EdgeDistanceCharacterY = "AA",
                    Content = "t01a29dac4bb27f7e22.png",
                    Start = WatermarkRange.BottomOfPhoto,
                    End = WatermarkRange.End,
                    ContentType = ContentType.Text,
                    RelativePositionMode = RelativePositionMode.LastRow,
                    Color = "#cbb795"
                },
                new GeneralWatermarkRowProperty()
                {
                    X = PositionBase.Right,
                    Y = PositionBase.Center,
                    EdgeDistanceType = EdgeDistanceType.Character,
                    LinePercentOfRange = 60,
                    LinePixel = 2,
                    Start = WatermarkRange.BottomOfPhoto,
                    End = WatermarkRange.End,
                    ContentType = ContentType.Line,
                    Color = "#e6e6e6",
                    RelativePositionMode = RelativePositionMode.LastRow
                },

            };

            image.ConnectionModes = new List<ConnectionMode>()
            {
                new ConnectionMode
                {
                    Ids = new List<string>(image.Properties.Select(c=>c.ID).Take(2)),
                    RowHeightMinFontPercent = 30,
                    X = PositionBase.Right,
                    Y = PositionBase.Center,
                    EdgeDistanceType = EdgeDistanceType.Character,
                    EdgeDistanceCharacterX = "ABCD",
                    EdgeDistanceCharacterY = "AA",
                    Start = WatermarkRange.BottomOfPhoto,
                    End = WatermarkRange.End
                },
                new ConnectionMode
                {
                    Ids = new List<string>(){image.Properties[5].ID },
                    LinePixel = 2,
                    X = PositionBase.Right,
                    Y = PositionBase.Center,
                    EdgeDistanceType = EdgeDistanceType.Character,
                    EdgeDistanceCharacterX = "a",
                    EdgeDistanceCharacterY = "a",
                    Start = WatermarkRange.BottomOfPhoto,
                    End = WatermarkRange.End,
                    RelativePositionMode = RelativePositionMode.LastRow
                },
                new ConnectionMode
                {
                    Ids = new List<string>(image.Properties.Select(c=>c.ID).Skip(2).Take(1)),
                    RowHeightMinFontPercent = 30,
                    X = PositionBase.Right,
                    Y = PositionBase.Center,
                    EdgeDistanceType = EdgeDistanceType.Character,
                    EdgeDistanceCharacterX = "a",
                    EdgeDistanceCharacterY = "AA",
                    Start = WatermarkRange.BottomOfPhoto,
                    End = WatermarkRange.End,
                    RelativePositionMode = RelativePositionMode.LastRow
                },
                new ConnectionMode
                {
                    Ids = new List<string>(){ image.Properties[3].ID, image.Properties[4].ID },
                    RowHeightMinFontPercent = 30,
                    X = PositionBase.Left,
                    Y = PositionBase.Center,
                    EdgeDistanceType = EdgeDistanceType.Character,
                    EdgeDistanceCharacterX = "aAAA",
                    EdgeDistanceCharacterY = "AA",
                    Start = WatermarkRange.BottomOfPhoto,
                    End = WatermarkRange.End,
                    RelativePositionMode = RelativePositionMode.Global
                }
            };

            image = Global.InitConfig().Templates.PhotoFrame;
            image.PhotoPath = "C:\\Users\\kingdee\\Pictures\\Camera Roll\\Windows10.jpg";
            image.Properties[2].ImagePath = "C:\\Users\\kingdee\\Downloads\\苹果.png";

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ImagesHelper.Current.Generation(image, this);
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            var templateConfig = new TemplateConfig(image);
            templateConfig.ShowDialog();
        }
    }
}
