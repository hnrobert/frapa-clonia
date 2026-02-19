using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Input;

namespace FrapaClonia.Views;

public partial class MainWindow : Window
{
    private const double MinSidebarWidth = 190;
    private const double MaxSidebarWidth = 280;
    private const double DefaultSidebarWidth = 240;
    private GridSplitter? _gridSplitter;
    private ColumnDefinition? _sidebarColumn;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        // Find the main layout grid (first child of the outer grid)
        if (Content is not Grid { Children.Count: > 0 } outerGrid || outerGrid.Children[0] is not Grid mainGrid) return;
        _sidebarColumn = mainGrid.ColumnDefinitions[0];
        _gridSplitter = mainGrid.Children[1] as GridSplitter;

        // Initialize sidebar width to default
        _sidebarColumn?.Width = new GridLength(DefaultSidebarWidth);

        if (_gridSplitter != null)
        {
            _gridSplitter.DragDelta += OnGridSplitterDragDelta;
        }
    }

    private void OnGridSplitterDragDelta(object? sender, VectorEventArgs e)
    {
        if (_sidebarColumn == null) return;

        var currentWidth = _sidebarColumn.Width.Value;
        var newWidth = currentWidth + e.Vector.X;

        newWidth = newWidth switch
        {
            // Clamp the width
            < MinSidebarWidth => MinSidebarWidth,
            > MaxSidebarWidth => MaxSidebarWidth,
            _ => newWidth
        };

        _sidebarColumn.Width = new GridLength(newWidth);
    }
}
