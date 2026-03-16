namespace VinhKhanhstreetfoods.Models
{
    public class AppSettings
    {
        public string DefaultLanguage { get; set; } = "vi-VN";
        public bool EnableAudio { get; set; } = true;
        public bool EnableAutoNarration { get; set; } = true;
        public int CooldownMinutes { get; set; } = 5;
        public int TriggerRadiusMeters { get; set; } = 20;
        public double LocationUpdateIntervalSeconds { get; set; } = 5.0;
        public string TrackAsiaApiKey { get; set; } = "bca01773651908dcc9bc6320f7c16973ce";
        public bool BatteryOptimizationEnabled { get; set; } = true;
    }
}
