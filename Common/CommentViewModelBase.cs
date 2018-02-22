﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using System.ComponentModel;
using System.Windows.Media;
using System.Windows;
using SitePlugin;

namespace Common
{
    public abstract class CommentViewModelBase : ICommentViewModel
    {
        public string ConnectionName { get { return _connectionName.Name; } }

        public virtual IEnumerable<IMessagePart> NameItems { get; protected set; }

        public virtual IEnumerable<IMessagePart> MessageItems { get; protected set; }

        public virtual string Info { get; protected set; }

        public virtual string Id { get; protected set; }

        public virtual IUser User { get; set; }

        public abstract string UserId { get; }

        public bool IsInfo { get; protected set; }

        public bool IsFirstComment { get; protected set; }

        public virtual IEnumerable<IMessagePart> Thumbnail { get; protected set; }

        public FontFamily FontFamily
        {
            get
            {
                if (IsFirstComment)
                    return _options.FirstCommentFontFamily;
                else
                    return _options.FontFamily;
            }
        }

        public FontStyle FontStyle
        {
            get
            {
                if (IsFirstComment)
                    return _options.FirstCommentFontStyle;
                else
                    return _options.FontStyle;
            }
        }

        public FontWeight FontWeight
        {
            get
            {
                if (IsFirstComment)
                    return _options.FirstCommentFontWeight;
                else
                    return _options.FontWeight;
            }
        }

        public int FontSize
        {
            get
            {
                if (IsFirstComment)
                    return _options.FirstCommentFontSize;
                else
                    return _options.FontSize;
            }
        }

        public SolidColorBrush Foreground => new SolidColorBrush(_options.ForeColor);

        public SolidColorBrush Background => new SolidColorBrush(_options.BackColor);

        public virtual bool IsVisible { get; } = true;

        public ICommentProvider CommentProvider { get; protected set; }

        private readonly ConnectionName _connectionName;
        private readonly IOptions _options;
        public CommentViewModelBase(ConnectionName connectionName, IOptions options)
        {
            _connectionName = connectionName;
            _options = options;
            WeakEventManager<ConnectionName, PropertyChangedEventArgs>.AddHandler(_connectionName, nameof(_connectionName.PropertyChanged), ConnectionName_PropertyChanged);
            //WeakEventManager<IOptions, PropertyChangedEventArgs>.AddHandler(_options, nameof(_options.PropertyChanged), Options_PropertyChanged);
            _options.PropertyChanged += Options_PropertyChanged;
        }

        private void Options_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(_options.ForeColor):
                    RaisePropertyChanged(nameof(Foreground));
                    break;
                case nameof(_options.BackColor):
                    RaisePropertyChanged(nameof(Background));
                    break;
                case nameof(_options.FontFamily):
                    RaisePropertyChanged(nameof(FontFamily));
                    break;
                case nameof(_options.FontStyle):
                    RaisePropertyChanged(nameof(FontStyle));
                    break;
                case nameof(_options.FontWeight):
                    RaisePropertyChanged(nameof(FontWeight));
                    break;
                case nameof(_options.FontSize):
                    RaisePropertyChanged(nameof(FontSize));
                    break;
            }
        }

        private void ConnectionName_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(_connectionName.Name):
                    RaisePropertyChanged(nameof(ConnectionName));
                    break;
            }
        }

        #region INotifyPropertyChanged
        [NonSerialized]
        private System.ComponentModel.PropertyChangedEventHandler _propertyChanged;
        /// <summary>
        /// 
        /// </summary>
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged
        {
            add { _propertyChanged += value; }
            remove { _propertyChanged -= value; }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="propertyName"></param>
        protected void RaisePropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = "")
        {
            _propertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }

        public Task AfterCommentAdded()
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}