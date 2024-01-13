﻿using CommunityToolkit.Mvvm.ComponentModel;
using MvvmDialogs;
using MvvmDialogs.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;

namespace LSMEmprunts
{
    public sealed class MainWindowViewModel : ObservableObject
    {
        public static MainWindowViewModel Instance { get; } = new MainWindowViewModel();

        private MainWindowViewModel()
        {
            CurrentPageViewModel = new HomeViewModel();

            Application.Current.DispatcherUnhandledException += OnUnhandledException;
        }

        private void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            var vm = new MessageBoxViewModel
            {
                Caption = "Erreur",
                Buttons = MessageBoxButton.OK,
                Message = e.Exception.CompleteDump()
            };
            Dialogs.Add(vm);
        }

        private object _CurrentPageViewModel;
        public object CurrentPageViewModel
        {
            get => _CurrentPageViewModel;
            set
            {
                (_CurrentPageViewModel as IDisposable)?.Dispose();
                SetProperty(ref _CurrentPageViewModel, value);
            }
        }

        public ObservableCollection<IDialogViewModel> Dialogs { get; } = new ObservableCollection<IDialogViewModel>();
    }
}
