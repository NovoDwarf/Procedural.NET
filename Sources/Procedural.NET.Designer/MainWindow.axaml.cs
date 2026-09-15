using Avalonia.Controls;
using Procedural.NET.Designer.ViewModels;

namespace Procedural.NET.Designer;

public sealed partial class MainWindow : Window
{
	public MainWindow()
	{
		InitializeComponent();
		DataContext = new MainWindowViewModel();
	}
}
