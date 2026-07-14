using UnityEngine;

public class Main : MonoBehaviour
{
    public AutoFlip flip;
    public ArduinoBasic arduino;      // 拖入場景中掛有 ArduinoBasic 的物件
    public float flipCooldown = 0.4f; // 兩次翻頁最短間隔 (秒)，防彈跳。想更鈍就調大
    private string lastHandled = "";  // 已處理過的訊息，用來偵測「新的一次轉動」
    private float lastFlipTime = -999f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        // 鍵盤操作 (保留原本功能)
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            flip.FlipRightPage();
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            flip.FlipLeftPage();
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
                // 防彈跳：距上次翻頁未達 flipCooldown 就略過這筆訊號
                if (Time.time - lastFlipTime >= flipCooldown)
                {
                    char dir = msg[0];          // 取第一個字元判斷方向
                    if (dir == 'R')
                    {
                        flip.FlipRightPage();    // 順時針 → 右翻 (等同 Arrow Right)
                        lastFlipTime = Time.time;
                    }
                    else if (dir == 'L')
                    {
                        flip.FlipLeftPage();     // 逆時針 → 左翻 (等同 Arrow Left)
                        lastFlipTime = Time.time;
                    }
                }
            }
        }
    }
}
