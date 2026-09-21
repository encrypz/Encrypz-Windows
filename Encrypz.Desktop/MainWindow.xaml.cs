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

using Encrypz.Desktop.ViewModels;

using Microsoft.Web.WebView2.Core;
using System.IO;

namespace Encrypz.Desktop;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        InitializeAsync();
    }

    async void InitializeAsync()
    {
        try 
        {
            await webView.EnsureCoreWebView2Async(null);
            
            var tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "EncrypzApp");
            var distFolder = System.IO.Path.Combine(tempDir, "dist");
            System.IO.Directory.CreateDirectory(distFolder);

            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            foreach (var resourceName in assembly.GetManifestResourceNames())
            {
                if (resourceName.StartsWith("Encrypz.Desktop.dist."))
                {
                    var logicalPath = resourceName.Substring("Encrypz.Desktop.dist.".Length);
                    var targetFile = "";
                    if (logicalPath.StartsWith("assets."))
                    {
                        var fileName = logicalPath.Substring("assets.".Length);
                        var assetsDir = System.IO.Path.Combine(distFolder, "assets");
                        System.IO.Directory.CreateDirectory(assetsDir);
                        targetFile = System.IO.Path.Combine(assetsDir, fileName);
                    }
                    else
                    {
                        targetFile = System.IO.Path.Combine(distFolder, logicalPath);
                    }

                    using var stream = assembly.GetManifestResourceStream(resourceName);
                    if (stream != null)
                    {
                        using var fileStream = System.IO.File.Create(targetFile);
                        stream.CopyTo(fileStream);
                    }
                }
            }

            webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                "app.encrypz.local", 
                distFolder, 
                CoreWebView2HostResourceAccessKind.Allow);

            webView.Source = new Uri("http://app.encrypz.local/index.html");
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(ex.ToString(), "WebView2 Init Error");
            System.IO.File.WriteAllText("webview2_crash.log", ex.ToString());
        }
    }
}