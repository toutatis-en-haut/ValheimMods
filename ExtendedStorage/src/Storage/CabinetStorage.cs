namespace ExtendedStorage.Storage
{
    internal class CabinetStorage
    {
        public const int TabCount = 6;
        public const int TabWidth = 5;
        public const int TabHeight = 3;
        public const int TabSlots = TabWidth * TabHeight;

        public Inventory[] Tabs { get; } = new Inventory[TabCount];
        public string[] Labels { get; } = new string[TabCount];

        public int ActiveTab { get; set; }

        public static string DefaultLabel(int index) => (index + 1).ToString();

        public string EffectiveLabel(int index)
        {
            var label = Labels[index];
            return string.IsNullOrEmpty(label) ? DefaultLabel(index) : label;
        }
    }
}
