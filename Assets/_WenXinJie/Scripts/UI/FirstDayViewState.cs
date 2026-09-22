namespace WenXinJie.UI
{
    public sealed class FirstDayViewState
    {
        public string Status { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        public string NextLabel { get; set; } = "继续";
        public bool IsMenu { get; set; }
        public bool ShowActions { get; set; }
        public bool[] AvailableActions { get; set; }
    }
}
