using System;

namespace SimApi.Configs
{
    public class SimApiOptions
    {
        public bool EnableCors { get; set; } = true;
        public bool EnableSimApiAuth { get; set; } = false;
        public bool EnableSimApiDoc { get; set; } = true;
        public bool EnableSimApiException { get; set; } = true;
        public bool EnableSimApiStorage { get; set; } = false;
        public bool EnableForwardHeaders { get; set; } = true;
        public bool EnableLowerUrl { get; set; } = true;
        public bool EnableLogger { get; set; } = true;
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