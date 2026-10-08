using System.Collections;
using UnityEngine;
using UnityEngine.UI;


public class Main : MonoBehaviour
{
    public AutoFlip flip;
    public ArduinoBasic arduino;      // 拖入場景中掛有 ArduinoBasic 的物件
    public float flipCooldown = 0.4f; // 兩次翻頁最短間隔 (秒)，防彈跳。想更鈍就調大
    private string lastHandled = "";  // 已處理過的訊息，用來偵測「新的一次轉動」
    private float lastFlipTime = -999f;

    public AudioSource turnNextAudio;
    public AudioSource turnPerviousAudio;

    [Header("待機自動回到第一頁")]
    public float idleSeconds = 180f;      // 無人操作多久後回到第一頁 (秒)，設 0 可關閉此功能
    public float fadeDuration = 1f;       // 淡出 / 淡入各自的時間 (秒)
    public Color fadeColor = Color.black; // 淡出時蓋住畫面的顏色

    private float lastActivityTime;
    private bool isResetting = false;     // 待機重置 (淡出→回第一頁→淡入) 進行中
    private Image fadeImage;              // 執行時自動建立的全螢幕遮罩，不需手動在場景中新增

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        lastActivityTime = Time.time;
    }

    // Update is called once per frame
    void Update()
    {
        // 待機重置進行中：不接受任何操作，並把這段期間的手輪訊號標記為已讀
        if (isResetting)
        {
            if (arduino != null && !string.IsNullOrEmpty(arduino.readMessage))
                lastHandled = arduino.readMessage;
            return;
        }

        // 任何鍵盤 / 滑鼠按下都視為「有人在操作」
        bool activity = Input.anyKeyDown;

        // 鍵盤操作 (保留原本功能)
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            TryFlipRight();
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            TryFlipLeft();
        }

        // 手輪操作：輪詢 ArduinoBasic 的 readMessage (不需修改 ArduinoBasic.cs)
        // Arduino 每筆訊息格式為 方向字母 + 遞增序號，例如 "R12"、"L13"，
        // 因此連續兩次同方向 (R12→R13) 字串會不同，可被正確偵測。
        if (arduino != null && !string.IsNullOrEmpty(arduino.readMessage))
        {
            string msg = arduino.readMessage;   // 讀取最新一筆訊息
            if (msg != lastHandled)
            {
                lastHandled = msg;              // 標記已讀，避免重複處理同一筆
                activity = true;                // 手輪有轉動 (即使已在首頁/末頁) 也算有人操作
                // 防彈跳：距上次翻頁未達 flipCooldown 就略過這筆訊號
                if (Time.time - lastFlipTime >= flipCooldown)
                {
                    char dir = msg[0];          // 取第一個字元判斷方向
                    if (dir == 'R')
                    {
                        // 順時針 → 右翻 (等同 Arrow Right)；真的有翻頁才進入冷卻
                        if (TryFlipRight()) lastFlipTime = Time.time;
                    }
                    else if (dir == 'L')
                    {
                        // 逆時針 → 左翻 (等同 Arrow Left)
                        if (TryFlipLeft()) lastFlipTime = Time.time;
                    }
                }
            }
        }

        // 待機計時
        if (activity)
        {
            lastActivityTime = Time.time;
        }
        else if (idleSeconds > 0 && Time.time - lastActivityTime >= idleSeconds)
        {
            if (flip.ControledBook == null || flip.ControledBook.currentPage <= 0)
                lastActivityTime = Time.time;       // 已經在第一頁，不需重置，重新計時
            else if (!flip.IsFlipping)
                StartCoroutine(IdleReset());        // 若剛好翻頁中，等翻完下一幀再重置
        }
    }

    // 能翻才翻頁並播放音效；已在最後一頁或翻頁動畫進行中則不動作、不出聲
    bool TryFlipRight()
    {
        if (!flip.CanFlipRight) return false;
        flip.FlipRightPage();
        if (turnNextAudio != null) turnNextAudio.Play();
        return true;
    }

    // 能翻才翻頁並播放音效；已在第一頁或翻頁動畫進行中則不動作、不出聲
    bool TryFlipLeft()
    {
        if (!flip.CanFlipLeft) return false;
        flip.FlipLeftPage();
        if (turnPerviousAudio != null) turnPerviousAudio.Play();
        return true;
    }

    // 待機重置：畫面淡出 → 跳回第一頁 → 畫面淡入
    IEnumerator IdleReset()
    {
        isResetting = true;
        EnsureFadeImage();

        if (fadeImage != null)
        {
            fadeImage.transform.SetAsLastSibling();   // 確保遮罩蓋在最上層
            fadeImage.raycastTarget = true;           // 淡出入期間擋住按鈕點擊
            yield return Fade(0f, 1f);
        }

        flip.ControledBook.JumpToPage(0);

        if (fadeImage != null)
        {
            yield return Fade(1f, 0f);
            fadeImage.raycastTarget = false;
        }

        lastActivityTime = Time.time;
        isResetting = false;
    }

    IEnumerator Fade(float from, float to)
    {
        Color c = fadeColor;
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(from, to, fadeDuration > 0 ? t / fadeDuration : 1f);
            fadeImage.color = c;
            yield return null;
        }
        c.a = to;
        fadeImage.color = c;
    }

    // 第一次需要時，在書本所在的 Canvas 底下建立一張全螢幕遮罩
    void EnsureFadeImage()
    {
        if (fadeImage != null) return;
        Canvas canvas = flip.GetComponentInParent<Canvas>();
        if (canvas == null) return;

        GameObject go = new GameObject("IdleFade", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(canvas.rootCanvas.transform, false);
        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        fadeImage = go.GetComponent<Image>();
        Color c = fadeColor;
        c.a = 0f;
        fadeImage.color = c;
        fadeImage.raycastTarget = false;
    }
}
