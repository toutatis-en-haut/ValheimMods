using UnityEngine;

namespace ExtendedStorage.Storage
{
    internal class CabinetContainer : MonoBehaviour
    {
        private const string LabelKeyPrefix = "es_tab"; // es_tab{n}_label
        private const string InvKeyPrefix = "es_tab";   // es_tab{n}_inv

        private ZNetView _nview;
        private Container _container;
        private bool _dirty;
        private bool _ready;

        public CabinetStorage Storage { get; private set; }

        private void Start()
        {
            _nview = GetComponent<ZNetView>();
            _container = GetComponent<Container>();

            if (_nview == null || !_nview.IsValid() || _container == null || _container.m_inventory == null)
            {
                return;
            }

            Storage = new CabinetStorage();

            // Tab 0 reuses Container.m_inventory — vanilla loads it from the
            // `items` ZDO key, so a removed mod degrades to a normal chest.
            Storage.Tabs[0] = _container.m_inventory;
            for (int i = 1; i < CabinetStorage.TabCount; i++)
            {
                Storage.Tabs[i] = new Inventory(
                    _container.m_name, null,
                    CabinetStorage.TabWidth, CabinetStorage.TabHeight);
            }

            LoadLabelsFromZDO();
            LoadAuxTabsFromZDO();

            for (int i = 0; i < CabinetStorage.TabCount; i++)
            {
                int capturedIndex = i;
                Storage.Tabs[i].m_onChanged += () => OnTabChanged(capturedIndex);
            }

            _ready = true;
        }

        private void LateUpdate()
        {
            if (!_dirty) return;
            _dirty = false;

            if (!_ready || _nview == null || !_nview.IsValid() || !_nview.IsOwner())
            {
                return;
            }

            SaveAuxTabsToZDO();
        }

        private void OnTabChanged(int tabIndex)
        {
            // Tab 0 is owned by vanilla Container.Save(); we only need to
            // persist tabs 1..N. Mark dirty regardless so multi-tab "Take All"
            // coalesces into one write.
            if (tabIndex == 0) return;
            _dirty = true;
        }

        public void SetLabel(int tabIndex, string label)
        {
            if (!_ready || _nview == null || !_nview.IsValid() || !_nview.IsOwner()) return;
            if (tabIndex < 0 || tabIndex >= CabinetStorage.TabCount) return;

            var normalized = string.IsNullOrEmpty(label) ? CabinetStorage.DefaultLabel(tabIndex) : label;
            Storage.Labels[tabIndex] = normalized;
            _nview.GetZDO().Set(LabelKey(tabIndex), normalized);
        }

        public Inventory GetTab(int tabIndex)
        {
            if (!_ready || tabIndex < 0 || tabIndex >= CabinetStorage.TabCount) return null;
            return Storage.Tabs[tabIndex];
        }

        public bool IsReady => _ready;

        private void LoadLabelsFromZDO()
        {
            var zdo = _nview.GetZDO();
            for (int i = 0; i < CabinetStorage.TabCount; i++)
            {
                Storage.Labels[i] = zdo.GetString(LabelKey(i), CabinetStorage.DefaultLabel(i));
            }
        }

        private void LoadAuxTabsFromZDO()
        {
            var zdo = _nview.GetZDO();
            for (int i = 1; i < CabinetStorage.TabCount; i++)
            {
                var b64 = zdo.GetString(InvKey(i), string.Empty);
                if (string.IsNullOrEmpty(b64)) continue;
                try
                {
                    var pkg = new ZPackage(b64);
                    Storage.Tabs[i].Load(pkg);
                }
                catch (System.Exception ex)
                {
                    ExtendedStoragePlugin.Log.LogError(
                        $"Failed to load cabinet tab {i} inventory: {ex.Message}");
                }
            }
        }

        private void SaveAuxTabsToZDO()
        {
            var zdo = _nview.GetZDO();
            for (int i = 1; i < CabinetStorage.TabCount; i++)
            {
                var pkg = new ZPackage();
                Storage.Tabs[i].Save(pkg);
                zdo.Set(InvKey(i), pkg.GetBase64());
            }
        }

        public void ForceSave()
        {
            if (!_ready || _nview == null || !_nview.IsValid() || !_nview.IsOwner()) return;
            SaveAuxTabsToZDO();
            _dirty = false;
        }

        private static string LabelKey(int i) => $"{LabelKeyPrefix}{i}_label";
        private static string InvKey(int i) => $"{InvKeyPrefix}{i}_inv";
    }
}
