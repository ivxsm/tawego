using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenuScript : MonoBehaviour
{
    public const string SkinPrefKey = "SelectedSkin";
    /// <summary>0 default, 1 green, 2 blue (matches Resources/Skins).</summary>
    public const int SkinCount = 3;

    GameObject _shopPanel;
    Image _previewImage;
    int _selectedSkin;

    void Start()
    {
        if (!PlayerPrefs.HasKey("Difficulty"))
            PlayerPrefs.SetInt("Difficulty", 1);
        if (!PlayerPrefs.HasKey(SkinPrefKey))
            PlayerPrefs.SetInt(SkinPrefKey, 0);
        _selectedSkin = Mathf.Clamp(PlayerPrefs.GetInt(SkinPrefKey, 0), 0, SkinCount - 1);
        BuildMenuUiIfNeeded();
        RefreshCharacterPreview();
    }

    static Font MenuFont()
    {
        var f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return f != null ? f : Font.CreateDynamicFontFromOSFont("Arial", 16);
    }

    static Sprite LoadSkinSprite(string pathWithoutExtension)
    {
        var s = Resources.Load<Sprite>(pathWithoutExtension);
        if (s != null) return s;
        var all = Resources.LoadAll<Sprite>(pathWithoutExtension);
        return all.Length > 0 ? all[0] : null;
    }

    public static Sprite SkinSpriteForIndex(int index)
    {
        return index switch
        {
            1 => LoadSkinSprite("Skins/green"),
            2 => LoadSkinSprite("Skins/blue"),
            _ => LoadSkinSprite("Skins/default")
        };
    }

    /// <summary>Transform.Find only searches direct children; pasted UI may be nested.</summary>
    static Transform FindDeepChild(Transform parent, string childName)
    {
        if (parent == null) return null;
        if (parent.name == childName) return parent;
        for (int i = 0; i < parent.childCount; i++)
        {
            var c = parent.GetChild(i);
            if (c.name == childName)
                return c;
            var d = FindDeepChild(c, childName);
            if (d != null)
                return d;
        }
        return null;
    }

    /// <summary>Prefer the Canvas that actually contains the menu shop UI (not another random Canvas).</summary>
    static Canvas GetMenuCanvas()
    {
        var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (var c in canvases)
        {
            if (c == null) continue;
            if (FindDeepChild(c.transform, "ShopButton") != null)
                return c;
        }
        return Object.FindFirstObjectByType<Canvas>();
    }

    void ResolveShopPanelReference()
    {
        if (_shopPanel != null) return;
        var canvas = GetMenuCanvas();
        if (canvas == null) return;
        var panelTr = FindDeepChild(canvas.transform, "ShopPanel");
        if (panelTr != null)
            _shopPanel = panelTr.gameObject;
    }

    /// <summary>Paste-from-Play often clears Button On Click; wire at runtime if the list is empty.</summary>
    void EnsureShopButtonWired(Transform canvasRoot)
    {
        var shopBtnTf = FindDeepChild(canvasRoot, "ShopButton");
        if (shopBtnTf == null) return;
        var btn = shopBtnTf.GetComponent<Button>();
        if (btn == null) return;
        if (btn.onClick.GetPersistentEventCount() > 0)
            return;
        btn.onClick.AddListener(OpenShop);
    }

    /// <summary>Done + skin buttons lose On Click after paste-from-Play; wire when Inspector list is empty.</summary>
    void EnsureShopPanelInnerWired()
    {
        ResolveShopPanelReference();
        if (_shopPanel == null) return;

        var closeTf = FindDeepChild(_shopPanel.transform, "CloseShop");
        if (closeTf != null)
        {
            var closeBtn = closeTf.GetComponent<Button>();
            if (closeBtn != null && closeBtn.onClick.GetPersistentEventCount() == 0)
                closeBtn.onClick.AddListener(CloseShop);
        }

        for (int i = 0; i < SkinCount; i++)
        {
            int idx = i;
            var skinTf = FindDeepChild(_shopPanel.transform, $"SkinBtn_{i}");
            if (skinTf == null) continue;
            var skinBtn = skinTf.GetComponent<Button>();
            if (skinBtn != null && skinBtn.onClick.GetPersistentEventCount() == 0)
                skinBtn.onClick.AddListener(() => SelectSkin(idx));
        }
    }

    void BuildMenuUiIfNeeded()
    {
        var canvas = GetMenuCanvas();
        if (canvas == null) return;

        Font font = MenuFont();

        const string previewPath = "PreviewFrame/CharacterPreview";
        if (canvas.transform.Find(previewPath) == null)
        {
            var frame = new GameObject("PreviewFrame");
            frame.transform.SetParent(canvas.transform, false);
            var frt = frame.AddComponent<RectTransform>();
            frt.anchorMin = new Vector2(0.5f, 1f);
            frt.anchorMax = new Vector2(0.5f, 1f);
            frt.pivot = new Vector2(0.5f, 1f);
            frt.anchoredPosition = new Vector2(0f, -140f);
            frt.sizeDelta = new Vector2(220f, 220f);
            // Frame is layout-only; character uses CharacterPreview Image.

            var prevGo = new GameObject("CharacterPreview");
            prevGo.transform.SetParent(frame.transform, false);
            var prt = prevGo.AddComponent<RectTransform>();
            prt.anchorMin = Vector2.zero;
            prt.anchorMax = Vector2.one;
            prt.offsetMin = new Vector2(10f, 10f);
            prt.offsetMax = new Vector2(-10f, -10f);
            _previewImage = prevGo.AddComponent<Image>();
            _previewImage.preserveAspect = true;
            _previewImage.color = Color.white;
        }
        else
        {
            var prevTf = canvas.transform.Find(previewPath);
            if (prevTf != null)
                _previewImage = prevTf.GetComponent<Image>();
        }

        if (FindDeepChild(canvas.transform, "ShopButton") != null)
        {
            ResolveShopPanelReference();
            EnsureShopButtonWired(canvas.transform);
            EnsureShopPanelInnerWired();
            return;
        }

        var shopBtnGo = new GameObject("ShopButton");
        shopBtnGo.transform.SetParent(canvas.transform, false);
        var sbrt = shopBtnGo.AddComponent<RectTransform>();
        sbrt.anchorMin = new Vector2(0.5f, 0.5f);
        sbrt.anchorMax = new Vector2(0.5f, 0.5f);
        sbrt.pivot = new Vector2(0.5f, 0.5f);
        sbrt.anchoredPosition = new Vector2(0f, -200f);
        sbrt.sizeDelta = new Vector2(220f, 52f);
        var sbImg = shopBtnGo.AddComponent<Image>();
        sbImg.color = new Color(0.18f, 0.42f, 0.82f, 1f);
        var sbBtn = shopBtnGo.AddComponent<Button>();
        sbBtn.onClick.AddListener(OpenShop);

        var sbTxGo = new GameObject("Text");
        sbTxGo.transform.SetParent(shopBtnGo.transform, false);
        var sbTxRt = sbTxGo.AddComponent<RectTransform>();
        sbTxRt.anchorMin = Vector2.zero;
        sbTxRt.anchorMax = Vector2.one;
        sbTxRt.offsetMin = Vector2.zero;
        sbTxRt.offsetMax = Vector2.zero;
        var sbTx = sbTxGo.AddComponent<Text>();
        sbTx.font = font;
        sbTx.fontSize = 24;
        sbTx.fontStyle = FontStyle.Bold;
        sbTx.color = Color.white;
        sbTx.alignment = TextAnchor.MiddleCenter;
        sbTx.text = "Shop";

        _shopPanel = new GameObject("ShopPanel");
        _shopPanel.transform.SetParent(canvas.transform, false);
        var panelRt = _shopPanel.AddComponent<RectTransform>();
        panelRt.anchorMin = Vector2.zero;
        panelRt.anchorMax = Vector2.one;
        panelRt.offsetMin = Vector2.zero;
        panelRt.offsetMax = Vector2.zero;
        var dim = _shopPanel.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.72f);
        dim.raycastTarget = true;

        var board = new GameObject("Board");
        board.transform.SetParent(_shopPanel.transform, false);
        var brt = board.AddComponent<RectTransform>();
        brt.anchorMin = new Vector2(0.5f, 0.5f);
        brt.anchorMax = new Vector2(0.5f, 0.5f);
        brt.sizeDelta = new Vector2(560f, 420f);
        brt.anchoredPosition = Vector2.zero;
        var bimg = board.AddComponent<Image>();
        bimg.color = new Color(0.12f, 0.13f, 0.18f, 1f);

        var titleGo = new GameObject("Title");
        titleGo.transform.SetParent(board.transform, false);
        var trt = titleGo.AddComponent<RectTransform>();
        trt.anchorMin = new Vector2(0.5f, 1f);
        trt.anchorMax = new Vector2(0.5f, 1f);
        trt.pivot = new Vector2(0.5f, 1f);
        trt.anchoredPosition = new Vector2(0f, -18f);
        trt.sizeDelta = new Vector2(520f, 48f);
        var title = titleGo.AddComponent<Text>();
        title.font = font;
        title.fontSize = 30;
        title.fontStyle = FontStyle.Bold;
        title.color = Color.white;
        title.alignment = TextAnchor.MiddleCenter;
        title.text = "Pick your character";

        string[] labels = { "Default", "Green", "Blue" };
        float startX = -200f;
        for (int i = 0; i < SkinCount; i++)
        {
            int idx = i;
            var sw = new GameObject($"SkinBtn_{i}");
            sw.transform.SetParent(board.transform, false);
            var wrt = sw.AddComponent<RectTransform>();
            wrt.anchorMin = new Vector2(0.5f, 0.55f);
            wrt.anchorMax = new Vector2(0.5f, 0.55f);
            wrt.sizeDelta = new Vector2(120f, 120f);
            wrt.anchoredPosition = new Vector2(startX + i * 200f, 0f);
            var img = sw.AddComponent<Image>();
            var sp = SkinSpriteForIndex(i);
            if (sp != null)
            {
                img.sprite = sp;
                img.color = Color.white;
            }
            else
                img.color = new Color(0.35f, 0.35f, 0.4f, 1f);

            img.preserveAspect = true;
            var btn = sw.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.95f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            btn.colors = colors;
            btn.onClick.AddListener(() => SelectSkin(idx));

            var lblGo = new GameObject("Label");
            lblGo.transform.SetParent(sw.transform, false);
            var lrt = lblGo.AddComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0f, 0f);
            lrt.anchorMax = new Vector2(1f, 0f);
            lrt.pivot = new Vector2(0.5f, 1f);
            lrt.anchoredPosition = new Vector2(0f, -6f);
            lrt.sizeDelta = new Vector2(0f, 28f);
            var lbl = lblGo.AddComponent<Text>();
            lbl.font = font;
            lbl.fontSize = 18;
            lbl.color = Color.white;
            lbl.alignment = TextAnchor.MiddleCenter;
            lbl.text = labels[i];
        }

        var closeGo = new GameObject("CloseShop");
        closeGo.transform.SetParent(board.transform, false);
        var crt = closeGo.AddComponent<RectTransform>();
        crt.anchorMin = new Vector2(0.5f, 0f);
        crt.anchorMax = new Vector2(0.5f, 0f);
        crt.pivot = new Vector2(0.5f, 0f);
        crt.anchoredPosition = new Vector2(0f, 22f);
        crt.sizeDelta = new Vector2(200f, 46f);
        closeGo.AddComponent<Image>().color = new Color(0.28f, 0.3f, 0.36f, 1f);
        var closeBtn = closeGo.AddComponent<Button>();
        closeBtn.onClick.AddListener(CloseShop);

        var cTxGo = new GameObject("Text");
        cTxGo.transform.SetParent(closeGo.transform, false);
        var cTxRt = cTxGo.AddComponent<RectTransform>();
        cTxRt.anchorMin = Vector2.zero;
        cTxRt.anchorMax = Vector2.one;
        cTxRt.offsetMin = Vector2.zero;
        cTxRt.offsetMax = Vector2.zero;
        var cTx = cTxGo.AddComponent<Text>();
        cTx.font = font;
        cTx.fontSize = 20;
        cTx.fontStyle = FontStyle.Bold;
        cTx.color = Color.white;
        cTx.alignment = TextAnchor.MiddleCenter;
        cTx.text = "Done";

        _shopPanel.SetActive(false);
    }

    void RefreshCharacterPreview()
    {
        if (_previewImage == null)
        {
            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                var t = canvas.transform.Find("PreviewFrame/CharacterPreview");
                if (t != null) _previewImage = t.GetComponent<Image>();
            }
        }
        if (_previewImage == null) return;
        var sp = SkinSpriteForIndex(_selectedSkin);
        if (sp != null)
            _previewImage.sprite = sp;
    }

    public void OpenShop()
    {
        ResolveShopPanelReference();
        if (_shopPanel != null)
            _shopPanel.SetActive(true);
    }

    public void CloseShop()
    {
        ResolveShopPanelReference();
        if (_shopPanel != null)
            _shopPanel.SetActive(false);
    }

    public void SelectSkin(int index)
    {
        _selectedSkin = Mathf.Clamp(index, 0, SkinCount - 1);
        PlayerPrefs.SetInt(SkinPrefKey, _selectedSkin);
        PlayerPrefs.Save();
        RefreshCharacterPreview();
    }

    public void SetEasyLevel()
    {
        PlayerPrefs.SetInt("Difficulty", 1);
        PlayerPrefs.Save();
        Debug.Log("Difficulty set to Easy");
    }

    public void SetHardLevel()
    {
        PlayerPrefs.SetInt("Difficulty", 2);
        PlayerPrefs.Save();
        Debug.Log("Difficulty set to Hard");
    }

    public void PlayGame()
    {
        SceneManager.LoadScene("SampleScene");
    }
}
