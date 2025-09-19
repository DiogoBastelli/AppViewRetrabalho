using System.ComponentModel;

public class OpcaoFiltro : INotifyPropertyChanged
{
    public string Nome { get; set; } = ""; 

    private bool _selecionado;
    public bool Selecionado
    {
        get => _selecionado;
        set
        {
            _selecionado = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Selecionado)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
