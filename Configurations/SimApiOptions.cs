using System;

namespace SimApi.Configs
{
    public class SimApiOptions
    {
        public bool EnableCors { get; set; } = false;

        public bool EnableSimApiAuth { get; set; } = false;

        public bool EnableSimApiDoc { get; set; } = false;

        public bool EnableSimApiException { get; set; } = false;

        public bool EnableSimApiStorage { get; set; } = false;

        public bool EnableForwardHeaders { get; set; } = false;

        public bool EnableLowerUrl { get; set; } = false;

        public bool EnableLogger { get; set; } = false;

        public SimApiDocOptions SimApiDocOptions { get; set; } = new SimApiDocOptions();

        public SimApiStorageOptions SimApiStorageOptions { get; set; } = new SimApiStorageOptions();


        public void ConfigureSimApiDoc(Action<SimApiDocOptions> options = null)
        {
            options?.Invoke(SimApiDocOptions);
        }

        public void ConfigureSimApiStorage(Action<SimApiStorageOptions> options = null)
        {
            options?.Invoke(SimApiStorageOptions);
        }
    }
}