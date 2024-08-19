using BeybladeTournamentManager.Config;

namespace BeybladeTournamentManager.Components.Pages.ViewModels
{
    public interface ISettingsViewModel
    {
        void SaveSettings(AppSettings settings);
        AppSettings LoadSettings();
        AppSettings GetSettings { get; }
    }
}