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
        if (Content is Grid outerGrid && outerGrid.Children.Count > 0 && outerGrid.Children[0] is Grid mainGrid)
        {
            _sidebarColumn = mainGrid.ColumnDefinitions[0];
            _gridSplitter = mainGrid.Children[1] as GridSplitter;

            // Initialize sidebar width to default
            _sidebarColumn?.Width = new GridLength(DefaultSidebarWidth);

            if (_gridSplitter != null)
            {
                _gridSplitter.DragDelta += OnGridSplitterDragDelta;
            }
        }
    }

    private void OnGridSplitterDragDelta(object? sender, VectorEventArgs e)
    {
        if (_sidebarColumn == null) return;

        var currentWidth = _sidebarColumn.Width.Value;
        var newWidth = currentWidth + e.Vector.X;

        // Clamp the width
        if (newWidth < MinSidebarWidth)
        {
            newWidth = MinSidebarWidth;
        }
        else if (newWidth > MaxSidebarWidth)
        {
            newWidth = MaxSidebarWidth;
        }

        _sidebarColumn.Width = new GridLength(newWidth);
    }
}
