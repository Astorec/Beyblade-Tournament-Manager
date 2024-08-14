namespace BeybladeTournamentManager.ApiCalls.Google
{
    public class GoogleServiceFactory : IGoogleServiceFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public GoogleServiceFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IGoogleService Create()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                return scope.ServiceProvider.GetRequiredService<IGoogleService>();
            }
        }
    }
}