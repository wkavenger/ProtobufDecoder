﻿using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using ProtobufDecoder.Application.Wpf.Annotations;
using ProtobufDecoder.Application.Wpf.Commands;
using ProtobufDecoder.Application.Wpf.Models;

namespace ProtobufDecoder.Application.Wpf.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private MessageViewModel _message;

        public MainWindowViewModel()
        {
            Model = new MainWindowModel();

            LoadFileCommand = new RelayCommand(
                _ => LoadAndDecode(Model.InputFilePath),
                _ => !string.IsNullOrEmpty(Model?.InputFilePath))
                .OnSuccess(_ => Model.StatusBarInfo(Strings.FileLoadedSuccessfully))
                .OnFailure(_ => Model.StatusBarError(Strings.FileFailedToLoad, _.Message));

            OpenFileCommand = new RelayCommand(
                _ => OpenFile(),
                _ => true);

            SaveGeneratedProtoCommand = new RelayCommand(
                _ => SaveGeneratedProtoFile(),
                _ => Model?.Message != null)
                .OnSuccess(_ => Model.StatusBarInfo(Strings.ProtoFileSavedSuccessfully))
                .OnFailure(_ => Model.StatusBarError(Strings.ProtoFileFailedToSave, _.Message));

            SaveGeneratedProtoAsCommand = new RelayCommand(
                _ => SaveGeneratedProtoFileAs(),
                _ => Model?.Message != null)
                .OnSuccess(_ => Model.StatusBarInfo(Strings.ProtoFileSavedAs, _.Message))
                .OnFailure(_ => Model.StatusBarError(Strings.ProtoFileFailedToSave, _.Message));

            CopyTagValueCommand = new RelayCommand(
                _ => ((_ as TreeView)?.SelectedItem as ProtobufTagViewModel)?.CopyTagValueToCsharpArray(),
                _ => (_ as TreeView)?.SelectedItem is ProtobufTagViewModel)
                .OnSuccess(_ => Model.StatusBarInfo(Strings.ContextMenuCopyValue));

            DecodeTagCommand = new RelayCommand(
                _ =>((_ as TreeView)?.SelectedItem as ProtobufTagViewModel)?.DecodeTag(),
                _ => (_ as TreeView)?.SelectedItem is ProtobufTagViewModel viewModel && viewModel.CanDecode)
                .OnSuccess(_ => Model.StatusBarInfo(Strings.TagDecodedSuccessfully))
                .OnSuccessWithWarnings(_ => Model.StatusBarWarning(Strings.CannotDecodeTag))
                .OnFailure(_ => Model.StatusBarError(Strings.FailedToDecodeTag, _.Message));               
        }

        public ICommand LoadFileCommand { get; }
        public ICommand OpenFileCommand { get; set; }
        public ICommand SaveGeneratedProtoCommand { get; }
        public ICommand SaveGeneratedProtoAsCommand { get; }
        public ICommand CopyTagValueCommand { get; set; }
        public ICommand DecodeTagCommand { get; set; }

        public MainWindowModel Model { get; set; }

        public MessageViewModel Message
        {
            get => _message;
            set
            {
                if (Equals(value, _message)) return;
                _message = value;
                OnPropertyChanged();
            }
        }

        private CommandResult LoadAndDecode(string inputFilePath)
        {
            try
            {
                var bytes = File.ReadAllBytes(inputFilePath);
                Model.InputFileByteStream = new MemoryStream(bytes);
                var parseResult = ProtobufParser.Parse(bytes);

                if (parseResult.Success)
                {
                    Message = new MessageViewModel(parseResult.Message);
                    Model.Message = parseResult.Message;

                    return CommandResult.Success();
                }

                return CommandResult.Failure(parseResult.FailureReason);
            }
            catch (Exception e)
            {
                return CommandResult.Failure(e.Message);
            }
        }

        private CommandResult OpenFile()
        {
            var dialog = new OpenFileDialog
            {
                RestoreDirectory = true,
                CheckFileExists = true,
                ShowReadOnly = true
            };

            var result = dialog.ShowDialog();

            if (result.HasValue && result.Value)
            {
                Model.InputFilePath = dialog.FileName;

                LoadFileCommand.Execute(null);
            }

            return CommandResult.Success();
        }

        private CommandResult SaveGeneratedProtoFile()
        {
            if (string.IsNullOrEmpty(Model.OutputFilePath))
            {
                var dialog = new SaveFileDialog
                {
                    RestoreDirectory = true,
                    AddExtension = true,
                    DefaultExt = ".proto",
                    Filter = Strings.ProtoFileType
                };

                var result = dialog.ShowDialog();

                if (!result.HasValue || !result.Value)
                {
                    return CommandResult.Success();
                }

                Model.OutputFilePath = dialog.FileName;
            }

            try
            {
                File.WriteAllText(Model.OutputFilePath, ProtobufWriter.ToString(Model.Message));
                return CommandResult.Success();
            }
            catch (Exception e)
            {
                return CommandResult.Failure(e.Message);
            }
        }

        private CommandResult SaveGeneratedProtoFileAs()
        {
            var dialog = new SaveFileDialog
            {
                RestoreDirectory = true,
                AddExtension = true,
                DefaultExt = ".proto",
                Filter = Strings.ProtoFileType
            };

            var result = dialog.ShowDialog();

            if (!result.HasValue || !result.Value)
            {
                return CommandResult.Success();
            }

            try
            {
                File.WriteAllText(dialog.FileName, ProtobufWriter.ToString(Model.Message));
                return CommandResult.Success(dialog.FileName);
            }
            catch (Exception e)
            {
                return CommandResult.Failure(e.Message);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}