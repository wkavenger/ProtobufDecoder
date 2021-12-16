﻿using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using ProtobufDecoder.Application.Wpf.Annotations;
using ProtobufDecoder.Tags;

namespace ProtobufDecoder.Application.Wpf.Models
{
    public class MainWindowModel : INotifyPropertyChanged
    {
        private string _inputFilePath;
        private ProtobufMessage _message;
        private string _windowTitle = "ProtobufDecoder";
        private Stream _inputFileByteStream;
        private string _outputFilePath;
        private string _statusBarText;
        private Brush _statusBarBackground = Brushes.DodgerBlue;
        private Brush _statusBarForeground = Brushes.White;
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string InputFilePath
        {
            get => _inputFilePath;
            set
            {
                if (value == _inputFilePath) return;
                _inputFilePath = value;
                OnPropertyChanged();

                WindowTitle = "ProtobufDecoder - " + value;
            }
        }

        public ProtobufMessage Message
        {
            get => _message;
            set
            {
                if (Equals(value, _message)) return;
                _message = value;
                OnPropertyChanged();
            }
        }

        public string WindowTitle
        {
            get => _windowTitle;
            set
            {
                if (value == _windowTitle) return;
                _windowTitle = value;
                OnPropertyChanged();
            }
        }

        public Stream InputFileByteStream
        {
            get => _inputFileByteStream;
            set
            {
                if (Equals(value, _inputFileByteStream)) return;
                _inputFileByteStream = value;
                OnPropertyChanged();
            }
        }

        public string OutputFilePath
        {
            get => _outputFilePath;
            set
            {
                if (value == _outputFilePath) return;
                _outputFilePath = value;
                OnPropertyChanged();
            }
        }

        public string StatusBarText
        {
            get => _statusBarText;
            private set
            {
                if (value == _statusBarText) return;
                _statusBarText = value;
                OnPropertyChanged();
            }
        }

        public Brush StatusBarBackground
        {
            get => _statusBarBackground;
            private set
            {
                if (Equals(value, _statusBarBackground)) return;
                _statusBarBackground = value;
                OnPropertyChanged();
            }
        }

        public Brush StatusBarForeground
        {
            get => _statusBarForeground;
            private set
            {
                if (Equals(value, _statusBarForeground)) return;
                _statusBarForeground = value;
                OnPropertyChanged();
            }
        }

        public void StatusBarInfo(string format, params object[] args)
        {
            StatusBarText = string.Format(format, args);
            StatusBarBackground = Brushes.DodgerBlue;
            StatusBarForeground = Brushes.White;
        }

        public void StatusBarWarning(string format, params object[] args)
        {
            StatusBarText = string.Format(format, args);
            StatusBarBackground = Brushes.DarkOrange;
            StatusBarForeground = Brushes.White;
        }

        public void StatusBarError(string format, params object[] args)
        {
            StatusBarText = string.Format(format, args);
            StatusBarBackground = Brushes.Red;
            StatusBarForeground = Brushes.White;
        }

        public MessageParseResult DecodeTag(ProtobufTag tag)
        {
            var parseResult = MessageParseResult.Failed("Can only decode a single tag");

            if (tag is ProtobufTagSingle singleTag)
            {
                if(singleTag.Value.CanDecode)
                {
                    try
                    {
                        parseResult = ProtobufParser.Parse(singleTag.Value.RawValue);

                        if (parseResult.Success)
                        {
                            var embeddedMessageTag =
                                new ProtobufTagEmbeddedMessage(singleTag, parseResult.Message.Tags.ToArray())
                                {
                                    Name = $"EmbeddedMessage{tag.Index}"
                                };

                            // Replace the existing tag with the expanded tag
                            if (singleTag.Parent is ProtobufTagRepeated repeatedTag)
                            {
                                var tagIndex = repeatedTag.Items.IndexOf(singleTag);
                                repeatedTag.Items.RemoveAt(tagIndex);
                                repeatedTag.Items.Insert(tagIndex, embeddedMessageTag);
                                OnPropertyChanged(nameof(Message));
                            }
                            else if (singleTag.Parent is ProtobufTagEmbeddedMessage embeddedTag)
                            {
                                var tagIndex = embeddedTag.Tags.IndexOf(singleTag);
                                embeddedTag.Tags.RemoveAt(tagIndex);
                                embeddedTag.Tags.Insert(tagIndex, embeddedMessageTag);
                                OnPropertyChanged(nameof(Message));
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        parseResult = MessageParseResult.Failed($"Unexpected error: {exception.Message}");
                    }
                }
                else
                {
                    parseResult = MessageParseResult.Failed($"Tag value can't be decoded");
                }
            }

            return parseResult;
        }

        public void SetTagProperty(ProtobufTag tag, string propertyName, object value)
        {
            switch (propertyName)
            {
                case nameof(ProtobufTag.Name):
                    tag.Name = (string)value;
                    OnPropertyChanged(nameof(Message));
                    break;
                case nameof(ProtobufTag.IsOptional):
                    tag.IsOptional = (bool)value;
                    OnPropertyChanged(nameof(Message));
                    break;
            }
        }
    }
}