using Netly.Core;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NetlyGame : MonoBehaviour
{
    [Header("Root")]
    public GameObject loadingPanel;
    public GameObject warningPanel;
    public GameObject playerPrefab;

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
                    netly = (isClient) ? (object)new NetlyGameClient(this) : (object)new NetlyGameServer(this);
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
        portIF.text = 14000.ToString();
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
}
