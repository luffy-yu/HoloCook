using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Netly.Core;
using System;
using Byter;
using System.Collections;

public class NetlyChat : MonoBehaviour
{
    [Header("Root")]
    public GameObject loadingPanel;
    public GameObject warningPanel;
    public GameObject scrollView;
    public Transform scrollViewContent;
    public GameObject messagePrefab;

    [Header("Send")]
    public Button sendButton;
    public TMP_InputField sendIF;

    [Header("Required")]
    public Image loadingImage;
    public TextMeshProUGUI warningTitleText, warningBodyText;

    [Header("Auth")]
    public GameObject auth;
    public TMP_InputField usernameIF, ipaddressIF, portIF;
    public TMP_Dropdown mode;
    public Button button;

    internal Host host;
    private object netly;
    private bool animInvertImage;
    private float animInvertTimer;
    internal bool isClient, started;


    private void Awake()
    {
        loadingPanel.SetActive(false);
        warningPanel.SetActive(false);
        scrollView.SetActive(false);
        auth.SetActive(true);

        IEnumerator Load()
        {
            if (started is false)
            {
                started = true;
                loadingPanel.SetActive(true);
                yield return new WaitForSeconds(2);

                try
                {
                    host = new Host(ipaddressIF.text, int.Parse(portIF.text));
                    netly = (isClient) ? (object)new NetlyChatClient(this) : (object)new NetlyChatServer(this);
                }
                catch (Exception e)
                {
                    loadingPanel.SetActive(false);
                    ShowError("Invalid Host", e.Message);
                    started = false;
                }
            }
        }

        button.onClick.AddListener(() =>
        {
            StartCoroutine(Load());
        });
    }

    private void Start()
    {
        usernameIF.text = $"User-{DateTime.Now.Millisecond}";
        ipaddressIF.text = "127.0.0.1";
        portIF.text = 12000.ToString();
    }

    private void LateUpdate()
    {
        if (started is false)
        {
            isClient = mode.captionText.text.Trim().ToLower() == "client mode";
            usernameIF.gameObject.SetActive(isClient);

            button.interactable = (ipaddressIF.text.Length > 0 && portIF.text.Length > 0);

            if (isClient && usernameIF.text.Length <= 0) button.interactable = false;
        }

        if (loadingImage != null)
        {
            animInvertTimer += Time.deltaTime;
            if (animInvertTimer > 0.35f)
            {
                animInvertTimer = 0;
                animInvertImage = !animInvertImage;
            }

            loadingImage.transform.rotation = Quaternion.Euler(0, 0, (animInvertImage) ? 90 : 0);
        }
    }

    internal void ShowError(string title, string body)
    {
        warningTitleText.text = title;
        warningBodyText.text = body;
        loadingPanel.SetActive(false);
        warningPanel.SetActive(true);
    }

    internal bool PrintOnScreen(byte[] bytes, bool isMine)
    {
        // read data
        using Reader r = new Reader(bytes);
        _ = r.Read<string>(); // ID
        bool isClient = r.Read<bool>();
        string name = r.Read<string>();
        string message = r.Read<string>();

        // return if data is invalid
        if (r.Success is false) return false;

        // instantiante message prefab on scrollview
        var messageInstance = GameObject.Instantiate(messagePrefab, scrollViewContent);

        // get message text
        var text = messageInstance.GetComponentInChildren<TextMeshProUGUI>();
        var image = messageInstance.GetComponentInChildren<Image>();

        // write message on text
        text.text = $"{name}\n{message}";

        // change background to red if is server
        if (!isClient) image.color = Color.red;

        // update chat to left if message is not mine
        if (!isMine)
        {
            RectTransform rect = image.rectTransform;
            rect.localPosition = new Vector3(-rect.localPosition.x, rect.localPosition.y, rect.localPosition.z);
        }

        return true;
    }
}