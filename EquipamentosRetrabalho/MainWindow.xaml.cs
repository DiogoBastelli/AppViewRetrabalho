using EquipamentosRetrabalho.View;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace EquipamentosRetrabalho;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        ConteudoPrincipal.Content = new EquipamentosView(); 
    }

    private void AbrirEquipamentos_Click(object sender, RoutedEventArgs e)
    {
        ConteudoPrincipal.Content = new EquipamentosView();
    }

    private void AbrirEstatisticas_Click(object sender, RoutedEventArgs e)
    {
        ConteudoPrincipal.Content = new View.EstatisticasView();
    }
}
