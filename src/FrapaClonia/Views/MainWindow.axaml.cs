using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

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
        PointerPressed += OnPointerPressed;
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

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // Get the source element that was clicked
        var source = e.Source as Visual;

        // Check if the clicked element or any of its ancestors is an input control
        var textBox = source?.FindAncestorOfType<TextBox>();
        var comboBox = source?.FindAncestorOfType<ComboBox>();
        var numericUpDown = source?.FindAncestorOfType<NumericUpDown>();

        // If we didn't click on an input control, focus on the main content area
        if (textBox != null || comboBox != null || numericUpDown != null) return;
        
        // Find the main content Border and focus it
        if (Content is not Grid outerGrid || outerGrid.Children[0] is not Grid mainGrid) return;
        var contentBorder = mainGrid.Children.OfType<Border>().LastOrDefault();
        contentBorder?.Focus();
    }
}