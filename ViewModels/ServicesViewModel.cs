using System.Collections.ObjectModel;
using System.Management;
using System.Windows;
using System.Windows.Input;
using ProcessExplorerPro.Helpers;
using ProcessExplorerPro.Models;

namespace ProcessExplorerPro.ViewModels
{
    public class ServicesViewModel : ViewModelBase
    {
        private string _searchText = string.Empty;
        private ServiceItem? _selectedService;

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    ApplyFilter();
                }
            }
        }

        public ServiceItem? SelectedService
        {
            get => _selectedService;
            set => SetProperty(ref _selectedService, value);
        }

        public ObservableCollection<ServiceItem> DisplayedServices { get; } = new();

        public ICommand StartCommand { get; }
        public ICommand StopCommand { get; }
        public ICommand RefreshCommand { get; }

        private List<ServiceItem> _allServices = new();

        public ServicesViewModel()
        {
            StartCommand = new RelayCommand(ExecuteStart, CanInteractWithService);
            StopCommand = new RelayCommand(ExecuteStop, CanInteractWithService);
            RefreshCommand = new RelayCommand(_ => LoadServices());

            LoadServices();
        }

        private void LoadServices()
        {
            try
            {
                var list = new List<ServiceItem>();
                using var searcher = new ManagementObjectSearcher(
                    "SELECT Name, DisplayName, ProcessId, State, StartMode, StartName, Description FROM Win32_Service");
                using var collection = searcher.Get();

                foreach (ManagementObject obj in collection)
                {
                    list.Add(new ServiceItem
                    {
                        Name = obj["Name"]?.ToString() ?? string.Empty,
                        DisplayName = obj["DisplayName"]?.ToString() ?? string.Empty,
                        Pid = Convert.ToInt32(obj["ProcessId"] ?? 0),
                        Status = obj["State"]?.ToString() ?? "Stopped",
                        StartType = obj["StartMode"]?.ToString() ?? "Manual",
                        LogOnAs = obj["StartName"]?.ToString() ?? "LocalSystem",
                        Description = obj["Description"]?.ToString() ?? "N/A"
                    });
                }

                _allServices = list.OrderBy(s => s.DisplayName).ToList();
                ApplyFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar serviços: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilter()
        {
            DisplayedServices.Clear();
            var filtered = _allServices.AsEnumerable();

            if (!string.IsNullOrEmpty(SearchText))
            {
                string search = SearchText.ToLowerInvariant();
                filtered = filtered.Where(s => s.Name.Contains(search, StringComparison.OrdinalIgnoreCase) || 
                                               s.DisplayName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                                               s.Pid.ToString().Contains(search));
            }

            foreach (var service in filtered)
            {
                DisplayedServices.Add(service);
            }
        }

        private bool CanInteractWithService(object? obj)
        {
            return SelectedService != null;
        }

        private void ExecuteStart(object? obj)
        {
            if (SelectedService == null) return;
            ControlService(SelectedService.Name, "StartService", "Iniciar");
        }

        private void ExecuteStop(object? obj)
        {
            if (SelectedService == null) return;
            ControlService(SelectedService.Name, "StopService", "Parar");
        }

        private void ControlService(string serviceName, string wmiMethod, string actionName)
        {
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    $"SELECT * FROM Win32_Service WHERE Name = '{serviceName}'");
                using var collection = searcher.Get();

                foreach (ManagementObject serviceObj in collection)
                {
                    var result = serviceObj.InvokeMethod(wmiMethod, null);
                    int returnCode = Convert.ToInt32(result);

                    if (returnCode == 0)
                    {
                        MessageBox.Show($"Comando de {actionName} enviado com sucesso.", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadServices();
                    }
                    else if (returnCode == 2)
                    {
                        MessageBox.Show("Acesso negado. Execute o Process Explorer Pro como Administrador para gerir serviços.", "Acesso Negado", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    else
                    {
                        MessageBox.Show($"O serviço retornou o código de erro: {returnCode}.", "Falha no Comando", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ocorreu um erro: {ex.Message}", "Erro de Gestão", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
