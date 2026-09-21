using CommunityToolkit.Mvvm.ComponentModel;

namespace Encrypz.Desktop.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _welcomeMessage = "Welcome to Encrypz Desktop!";
    }
}
