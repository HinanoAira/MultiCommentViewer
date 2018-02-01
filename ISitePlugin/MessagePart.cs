﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SitePlugin
{
    public interface IMessagePart { }
    public interface IMessageText : IMessagePart
    {
        string Text { get; }
    }
    public interface IMessageImage : IMessagePart
    {
        /// <summary>
        /// 表示時の幅
        /// </summary>
        int Width { get; }
        /// <summary>
        /// 表示時の高さ
        /// </summary>
        int Height { get; }
        System.Drawing.Image Image { get; }
    }
    /// <summary>
    /// 指定された画像の一部を描画する用
    /// </summary>
    public interface IMessageImagePortion : IMessageImage
    {
        int SrcX { get; }
        int SrcY { get; }
        int SrcWidth { get; }
        int SrcHeight { get; }
    }
}