using ClassHierarchyNavigator.Models;
using ClassHierarchyNavigator.Navigation;
using ClassHierarchyNavigator.ViewModels;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace ClassHierarchyNavigator.View
{
    public partial class TypeSelectionWindow : Window
    {
        public TypeSelectionWindow(
          IReadOnlyList<LeveledSymbol> candidateSymbols,
          NavigationDirection direction,
          INamedTypeSymbol targetTypeSymbol,
          string? warningText = null,
          string? statusText = null)
        {
            InitializeComponent();

            var viewModel = new TypeSelectionViewModel(
              candidateSymbols,
              direction,
              targetTypeSymbol,
              warningText,
              statusText);

            viewModel.RequestClose += HandleRequestClose;

            DataContext = viewModel;
        }

        public LeveledSymbol? SelectedSymbol { get; private set; }

        private void HandleRequestClose(object? sender, TypeSelectionCloseRequest closeRequest)
        {
            SelectedSymbol = closeRequest.SelectedSymbol;
            DialogResult = closeRequest.DialogResult;
            Close();
        }

        private void HandleMouseLeftButtonDown(object sender, MouseButtonEventArgs eventArgs)
        {
            if (eventArgs.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void HandleCloseButtonClick(object sender, RoutedEventArgs eventArgs)
        {
            Close();
        }
    }
}
