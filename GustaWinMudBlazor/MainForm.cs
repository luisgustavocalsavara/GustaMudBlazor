using System.Diagnostics;
using GustaMudBlazor.Adapters.WinForms;
using GustaMudBlazor.Models.Entities;
using GustaMudBlazor.Models.Interfaces;
using Microsoft.AspNetCore.Components.WebView.WindowsForms;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using GustaMudBlazor.Services;
using GustaMudBlazor.Services.Data;
using GustaMudBlazor.Services.Reports;
using GustaMudBlazor.Services.WinForms;
using GustaMudBlazor.Views;
using GustaMudBlazor.App.Interfaces;

namespace GustaMudBlazor;

public partial class MainForm : Form
{
    private readonly JsonSettingsService _settingsService;
    private ServiceProvider? _serviceProvider;

    private Panel? _loadingPanel;
    private Label? _loadingLabel;
    private ProgressBar? _loadingProgressBar;

    public MainForm()
    {
        InitializeComponent();

        // Lê as configurações do usuário antes de inicializar a interface
        _settingsService = new JsonSettingsService();
        WindowState = _settingsService.Current.WindowState switch
        {
            AppWindowState.Minimized => FormWindowState.Minimized,
            AppWindowState.Maximized => FormWindowState.Maximized,
            _ => FormWindowState.Normal
        };
        Resize += OnWindowResize;

        // Mostra o painel de loading por cima (BringToFront) com a cor exata do tema (Dark ou Light)
        InitializeLoadingOverlay(_settingsService.Current.IsDarkMode);

        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();
        BlazorWebView.Disposed += OnBlazorWebViewDisposed;

        // 4. Carrega o favicon.ico padrão como ícone de exemplo para os blocos iniciais e inicializa o banco SQLite
        var defaultIconDataUrl = LoadDefaultIconDataUrl();

        AppDbContext.InitializeDatabase(_serviceProvider, defaultIconDataUrl);

        BlazorWebView.Services = _serviceProvider;

        InitializeBlazorWebView();
    }

    private static string LoadDefaultIconDataUrl()
    {
        var icoPath = Path.Combine(AppContext.BaseDirectory, "wwwroot", "favicon.ico");
        try
        {
            if (File.Exists(icoPath))
            {
                var bytes = File.ReadAllBytes(icoPath);
                return $"data:image/x-icon;base64,{Convert.ToBase64String(bytes)}";
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Trace.TraceError("Could not load the default icon from '{0}': {1}", icoPath, ex);
        }
        return string.Empty;
    }

    private void InitializeLoadingOverlay(bool isDarkMode)
    {
        var bgColor = isDarkMode
            ? Color.FromArgb(26, 26, 39)      // #1a1a27 (Dark)
            : Color.FromArgb(245, 245, 247);  // #f5f5f7 (Light)

        var textColor = isDarkMode
            ? Color.FromArgb(178, 176, 191)   // #b2b0bf (Dark)
            : Color.FromArgb(51, 51, 51);     // #333333 (Light)

        BackColor = bgColor;
        BlazorWebView.BackColor = bgColor;
        BlazorWebView.WebView.DefaultBackgroundColor = bgColor;

        _loadingPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = bgColor
        };

        var centerLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = Color.Transparent
        };
        centerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        centerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        centerLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        centerLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        centerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

        _loadingLabel = new Label
        {
            Text = "Inicializando motor gráfico e banco de dados...",
            ForeColor = textColor,
            Font = new Font("Segoe UI", 11F, FontStyle.Regular),
            AutoSize = true,
            Anchor = AnchorStyles.None,
            Margin = new Padding(0, 0, 0, 12)
        };

        _loadingProgressBar = new ProgressBar
        {
            Style = ProgressBarStyle.Marquee,
            MarqueeAnimationSpeed = 25,
            Size = new Size(320, 18),
            Anchor = AnchorStyles.None
        };

        centerLayout.Controls.Add(_loadingLabel, 0, 1);
        centerLayout.Controls.Add(_loadingProgressBar, 0, 2);
        _loadingPanel.Controls.Add(centerLayout);

        Controls.Add(_loadingPanel);

        _loadingPanel.BringToFront();

    }

    private void HideLoadingOverlay()
    {
        if (_loadingPanel is null) return;

        _loadingPanel.Visible = false;
        Controls.Remove(_loadingPanel);
        _loadingPanel.Dispose();
        _loadingPanel = null;
    }

    private void ConfigureServices(IServiceCollection services)
    {
        services.AddWindowsFormsBlazorWebView();
        services.AddMudServices();

        // Banco de Dados SQLite com EF Core
        var dbPath = Path.Combine(AppContext.BaseDirectory, "app_data.db");
        services.AddDbContextFactory<AppDbContext>(options =>
            options.UseSqlite($"Data Source={dbPath}"));

        // Registro de Services (Arquitetura Component-Service-Model Direta, sem camada ViewModel)
        services.AddSingleton<ISettingsService>(_settingsService);
        services.AddSingleton<IAuthService, AuthService>();
        services.AddScoped<IBlockService, BlockService>();
        services.AddScoped<IRecipeService, RecipeService>();
        services.AddScoped<IPlcExportDataService, PlcExportDataService>();
        services.AddPlcReportGenerators();
        services.AddScoped<IFileExportService, WinFormsFileExportService>();
        services.AddScoped<IWeatherService, WeatherService>();
    }

    private void InitializeBlazorWebView()
    {
        BlazorWebView.HostPage = "wwwroot/index.html";
        BlazorWebView.RootComponents.Add<Routes>("#app");

        BlazorWebView.WebView.CoreWebView2InitializationCompleted += (s, e) =>
        {
            if (e.IsSuccess)
            {

                BlazorWebView.WebView.CoreWebView2.Settings.AreDevToolsEnabled = true;

                _loadingLabel?.Text = "Carregando interface Blazor...";
                BlazorWebView.WebView.CoreWebView2.AddHostObjectToScript(
                    "winFormHost",
                    new WinFormInterop(HideLoadingOverlayFromBlazor));
#if DEBUG
                BeginInvoke(OpenDevToolsWindow);
#endif
            }
            else
            {
                MessageBox.Show(
                    $"Falha ao inicializar o WebView2: {e.InitializationException?.Message}",
                    "Erro de Inicialização",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        };

        BlazorWebView.WebView.NavigationCompleted += (s, e) =>
        {
            if (!e.IsSuccess)
            {
                _loadingLabel?.Text = $"Falha ao carregar a interface: {e.WebErrorStatus}";
                _loadingProgressBar?.Visible = false;
            }
        };
    }

    public void HideLoadingOverlayFromBlazor()
    {
        if (InvokeRequired)
        {
            BeginInvoke(HideLoadingOverlay);
            return;
        }

        HideLoadingOverlay();
    }

    private void OpenDevToolsWindow()
    {
        try
        {
            BlazorWebView.WebView.CoreWebView2.OpenDevToolsWindow();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Não foi possível abrir as ferramentas do WebView2: {ex.Message}",
                "Erro ao abrir DevTools",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void OnWindowResize(object? sender, EventArgs e)
    {
        var state = WindowState switch
        {
            FormWindowState.Minimized => AppWindowState.Minimized,
            FormWindowState.Maximized => AppWindowState.Maximized,
            _ => AppWindowState.Normal
        };
        _settingsService.UpdateWindowState(state);
    }

    private void OnBlazorWebViewDisposed(object? sender, EventArgs e)
    {
        _serviceProvider?.Dispose();
        _serviceProvider = null;
    }

    private void MainForm_Load(object sender, EventArgs e)
    {

    }
}
