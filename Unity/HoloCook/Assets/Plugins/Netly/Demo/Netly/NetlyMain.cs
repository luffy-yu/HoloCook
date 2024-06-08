using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class NetlyMain : MonoBehaviour
{
    public GameObject loadingImage, loading;
    public Button loadChat, loadGame;
    private float animInvertTimer;
    private bool animInvertImage;

    private void Awake()
    {
        IEnumerator Load(int index)
        {
            loading.gameObject.SetActive(true);
            yield return new WaitForSeconds(2);

            SceneManager.LoadScene(index);
        }

        loadChat.onClick.AddListener(() =>
        {
            StartCoroutine(Load(1));
        });

        loadGame.onClick.AddListener(() =>
        {

            StartCoroutine(Load(2));
        });
    }

    private void LateUpdate()
    {

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
}
