using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;

namespace BetaProyecto.Views.TabControlApp;

public partial class TabItemInicio : UserControl
{
    public TabItemInicio()
    {
        InitializeComponent();
    }
    //Metodo para cerrar el button flyout
    public void CerrarMenu_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Control control)
        {
            // 1. Buscamos hacia arriba el "FlyoutPresenter"
            // (Es la caja visual que contiene los botones del menú)
            var flyoutPresenter = control.FindAncestorOfType<FlyoutPresenter>();

            if (flyoutPresenter != null)
            {
                // 2. El padre del Presenter es el Popup. Lo cerramos.
                if (flyoutPresenter.Parent is Popup popup)
                {
                    popup.IsOpen = false;
                }
            }
        }
    }
}